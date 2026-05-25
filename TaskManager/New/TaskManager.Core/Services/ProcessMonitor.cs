using System.Diagnostics;
using System.Management;
using TaskManager.Core.Interfaces;
using TaskManager.Core.Models;

namespace TaskManager.Core.Services
{
    /// <summary>
    /// 进程监控服务实现
    /// </summary>
    public class ProcessMonitor : IProcessMonitor, IDisposable
    {
        private readonly ILogger<ProcessMonitor> _logger;
        private readonly ProcessSettings _settings;
        private Dictionary<int, string> _commandLineCache = new();
        private DateTime _lastCacheUpdate = DateTime.MinValue;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);
        private bool _disposed;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志器</param>
        /// <param name="settings">进程设置</param>
        public ProcessMonitor(ILogger<ProcessMonitor> logger, ProcessSettings settings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// 获取所有 dotnet 进程
        /// </summary>
        public async Task<List<ProcessInfo>> GetDotnetProcessesAsync(string searchName, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("开始搜索 dotnet 进程，搜索名称: {SearchName}", searchName);

                var processes = new List<ProcessInfo>();
                var dotnetProcesses = Process.GetProcessesByName("dotnet");

                if (dotnetProcesses.Length == 0)
                {
                    _logger.LogInformation("未找到 dotnet 进程");
                    return processes;
                }

                // 更新命令行缓存
                await UpdateCommandLineCacheAsync(cancellationToken);

                // 分批处理
                var batches = SplitIntoBatches(dotnetProcesses.ToList(), _settings.BatchSize);
                var tasks = new List<Task<List<ProcessInfo>>>();

                foreach (var batch in batches)
                {
                    tasks.Add(Task.Run(() => ProcessBatch(batch, searchName, cancellationToken), cancellationToken));
                }

                var results = await Task.WhenAll(tasks);
                foreach (var result in results)
                {
                    processes.AddRange(result);
                }

                _logger.LogInformation("找到 {Count} 个匹配的 dotnet 进程", processes.Count);
                return processes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取 dotnet 进程时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 获取所有进程的命令行缓存
        /// </summary>
        public async Task<Dictionary<int, string>> CacheCommandLinesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("开始缓存命令行参数");

                var commandLines = new Dictionary<int, string>();
                await Task.Run(() =>
                {
                    using var searcher = new ManagementObjectSearcher("SELECT ProcessId, CommandLine FROM Win32_Process");
                    using var collection = searcher.Get();

                    foreach (ManagementObject obj in collection)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        try
                        {
                            int processId = Convert.ToInt32(obj["ProcessId"]);
                            string? commandLine = obj["CommandLine"]?.ToString();
                            
                            if (!string.IsNullOrEmpty(commandLine))
                            {
                                commandLines[processId] = commandLine;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "处理进程信息时发生错误");
                        }
                    }
                }, cancellationToken);

                _commandLineCache = commandLines;
                _lastCacheUpdate = DateTime.UtcNow;
                
                _logger.LogInformation("已缓存 {Count} 个进程的命令行参数", commandLines.Count);
                return commandLines;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "缓存命令行参数时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 终止进程
        /// </summary>
        public async Task<bool> KillProcessAsync(int processId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("尝试终止进程: {ProcessId}", processId);

                return await Task.Run(() =>
                {
                    try
                    {
                        using var process = Process.GetProcessById(processId);
                        process.Kill();
                        
                        // 等待进程退出
                        if (process.WaitForExit(5000))
                        {
                            _logger.LogInformation("成功终止进程: {ProcessId}", processId);
                            return true;
                        }
                        else
                        {
                            _logger.LogWarning("进程 {ProcessId} 未在指定时间内退出", processId);
                            return false;
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        _logger.LogWarning(ex, "进程 {ProcessId} 不存在", processId);
                        return false;
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogWarning(ex, "进程 {ProcessId} 已退出", processId);
                        return false;
                    }
                    catch (System.ComponentModel.Win32Exception ex)
                    {
                        _logger.LogError(ex, "没有权限终止进程 {ProcessId}", processId);
                        return false;
                    }
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "终止进程 {ProcessId} 时发生错误", processId);
                throw;
            }
        }

        /// <summary>
        /// 批量终止进程
        /// </summary>
        public async Task<List<int>> KillProcessesAsync(IEnumerable<int> processIds, CancellationToken cancellationToken = default)
        {
            var killedProcesses = new List<int>();
            var tasks = new List<Task<bool>>();

            foreach (var processId in processIds)
            {
                tasks.Add(KillProcessAsync(processId, cancellationToken));
            }

            var results = await Task.WhenAll(tasks);
            
            for (int i = 0; i < processIds.Count(); i++)
            {
                if (results[i])
                {
                    killedProcesses.Add(processIds.ElementAt(i));
                }
            }

            _logger.LogInformation("成功终止 {Count} 个进程", killedProcesses.Count);
            return killedProcesses;
        }

        /// <summary>
        /// 获取进程详细信息
        /// </summary>
        public async Task<ProcessDetail> GetProcessDetailAsync(int processId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("获取进程详细信息: {ProcessId}", processId);

                return await Task.Run(() =>
                {
                    using var process = Process.GetProcessById(processId);
                    
                    string commandLine = GetCommandLineFromCache(processId);
                    
                    return new ProcessDetail
                    {
                        ProcessId = process.Id,
                        ProcessName = process.ProcessName,
                        CommandLine = commandLine,
                        WorkingSet = process.WorkingSet64,
                        TotalProcessorTime = process.TotalProcessorTime,
                        StartTime = process.StartTime,
                        ThreadCount = process.Threads.Count,
                        HandleCount = process.HandleCount,
                        WorkingDirectory = process.StartInfo.WorkingDirectory ?? string.Empty
                    };
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取进程 {ProcessId} 详细信息时发生错误", processId);
                throw;
            }
        }

        #region 私有方法

        private async Task UpdateCommandLineCacheAsync(CancellationToken cancellationToken)
        {
            if (DateTime.UtcNow - _lastCacheUpdate > _cacheExpiration || _commandLineCache.Count == 0)
            {
                await CacheCommandLinesAsync(cancellationToken);
            }
        }

        private List<ProcessInfo> ProcessBatch(List<Process> processes, string searchName, CancellationToken cancellationToken)
        {
            var result = new List<ProcessInfo>();

            foreach (var process in processes)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    string commandLine = GetCommandLineFromCache(process.Id);
                    
                    if (string.IsNullOrEmpty(commandLine))
                        continue;

                    if (commandLine.IndexOf(searchName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        result.Add(new ProcessInfo
                        {
                            ProcessId = process.Id,
                            TaskGroup = searchName,
                            TaskName = commandLine
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "处理进程 {ProcessId} 时发生错误", process.Id);
                }
            }

            return result;
        }

        private string GetCommandLineFromCache(int processId)
        {
            if (_commandLineCache.TryGetValue(processId, out var commandLine))
            {
                return commandLine ?? string.Empty;
            }
            return string.Empty;
        }

        private static List<List<T>> SplitIntoBatches<T>(List<T> source, int batchSize)
        {
            var batches = new List<List<T>>();
            for (int i = 0; i < source.Count; i += batchSize)
            {
                batches.Add(source.Skip(i).Take(batchSize).ToList());
            }
            return batches;
        }

        #endregion

        #region IDisposable 实现

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _commandLineCache.Clear();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}