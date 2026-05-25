using DeployService.Services;
using PublishWeb.Helper;
using PublishWeb.Models;
using System.Threading.Channels;

namespace PublishWeb.Services
{
    public class DeployRequestMessage
    {
        public string TaskId { get; set; }
        public DeployRequest Request { get; set; }
    }

    public class DeployBackgroundService : BackgroundService
    {
        private readonly Channel<DeployRequestMessage> _channel;
        private readonly ILogger<DeployBackgroundService> _logger;

        public DeployBackgroundService(ILogger<DeployBackgroundService> logger)
        {
            _channel = Channel.CreateUnbounded<DeployRequestMessage>();
            _logger = logger;
        }

        public void Enqueue(string taskId, DeployRequest req)
        {
            _channel.Writer.TryWrite(new DeployRequestMessage { TaskId = taskId, Request = req });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var msg in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    ExecuteDeploy(msg.TaskId, msg.Request);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Deploy background task failed: {TaskId}", msg.TaskId);
                }
            }
        }

        private static void Update(string taskId, int progress, string message, LogType type = LogType.Info)
        {
            if (!DeployTaskStore.Tasks.TryGetValue(taskId, out var task))
                return;

            lock (task)
            {
                task.Progress = progress;
                task.Message = message;
                task.LogType = type;

                task.Logs.Add(new LogItem
                {
                    Id = Interlocked.Increment(ref task._logIdSeed),
                    Message = message,
                    LogType = type
                });
            }
        }

        private void ExecuteDeploy(string taskId, DeployRequest req)
        {
            var task = DeployTaskStore.Tasks[taskId];

            try
            {
                if (req == null || req.SiteIds == null || req.SiteIds.Count == 0)
                {
                    Update(taskId, 10, " 站点列表为空", LogType.Error);
                    task.Status = "Failed";
                    return;
                }

                var hasError = false;
                foreach (var siteId in req.SiteIds)
                {
                    try
                    {
                        var sites = SiteConfigService.GetSites();
                        var site = sites.FirstOrDefault(x => x.Id == siteId);
                        if (site == null)
                        {
                            Update(taskId, 10, $" 站点不存在: {siteId}", LogType.Error);
                            hasError = true;
                            continue;
                        }

                        Update(taskId, 10, $"[{site.Name}] 停止应用程序池");
                        IisHelper.StopAppPool(site.Name, (msg, type) => Update(taskId, task.Progress, msg, type));

                        Update(taskId, 30, $"[{site.Name}] 开始发布");
                        IisHelper.CopyDirectory(site.FilePath, site.SitePath, (msg, type) => Update(taskId, task.Progress, $"[{site.Name}] " + msg, type));

                        Update(taskId, 80, $"[{site.Name}] 启动应用程序池");
                        IisHelper.StartAppPool(site.Name, (msg, type) => Update(taskId, task.Progress, msg, type));

                        Update(taskId, 100, $"[{site.Name}] 启动完成", LogType.Success);
                    }
                    catch (Exception ex)
                    {
                        Update(taskId, 100, $"[{siteId}] 发布失败: {ex.Message}", LogType.Error);
                        hasError = true;
                    }
                }
                if (hasError)
                {
                    task.Status = "Failed";
                }

            }
            catch (Exception ex)
            {
                task.Status = "Failed";
                task.Message = ex.Message;
                Update(taskId, 100, $" 发布异常 {ex.Message}", LogType.Error);
            }
            finally
            {
                ScheduleTaskRemove(taskId);
            }
        }

        private static void ScheduleTaskRemove(string taskId)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(5));
                DeployTaskStore.Tasks.TryRemove(taskId, out _);
            });
        }
    }
}
