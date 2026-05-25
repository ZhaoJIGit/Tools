using Newtonsoft.Json;
using TaskManager.Core.Interfaces;
using TaskManager.Core.Models;

namespace TaskManager.Data.Repositories
{
    /// <summary>
    /// 任务组仓库实现（JSON文件存储）
    /// </summary>
    public class TaskGroupRepository : ITaskGroupRepository, IDisposable
    {
        private readonly ILogger<TaskGroupRepository> _logger;
        private readonly CacheSettings _settings;
        private readonly string _cacheDirectory;
        private readonly string _jsonFilePath;
        private readonly SemaphoreSlim _fileLock = new(1, 1);
        private bool _disposed;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志器</param>
        /// <param name="settings">缓存设置</param>
        public TaskGroupRepository(ILogger<TaskGroupRepository> logger, CacheSettings settings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            // 解析缓存目录
            _cacheDirectory = Environment.ExpandEnvironmentVariables(_settings.Directory);
            _jsonFilePath = Path.Combine(_cacheDirectory, _settings.JsonFileName);

            EnsureCacheDirectory();
        }

        /// <summary>
        /// 获取所有任务组
        /// </summary>
        public async Task<List<TaskGroupInfo>> GetAllTaskGroupsAsync(CancellationToken cancellationToken = default)
        {
            await _fileLock.WaitAsync(cancellationToken);
            try
            {
                if (!File.Exists(_jsonFilePath))
                {
                    _logger.LogInformation("任务组文件不存在，返回空列表");
                    return new List<TaskGroupInfo>();
                }

                var json = await File.ReadAllTextAsync(_jsonFilePath, cancellationToken);
                var taskGroups = JsonConvert.DeserializeObject<List<TaskGroupInfo>>(json) ?? new List<TaskGroupInfo>();

                _logger.LogInformation("从文件加载了 {Count} 个任务组", taskGroups.Count);
                return taskGroups;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "解析任务组JSON文件时发生错误");
                // 返回空列表而不是抛出异常
                return new List<TaskGroupInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取任务组文件时发生错误");
                throw;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <summary>
        /// 保存任务组
        /// </summary>
        public async Task SaveTaskGroupsAsync(IEnumerable<TaskGroupInfo> taskGroups, CancellationToken cancellationToken = default)
        {
            if (taskGroups == null)
                throw new ArgumentNullException(nameof(taskGroups));

            await _fileLock.WaitAsync(cancellationToken);
            try
            {
                var json = JsonConvert.SerializeObject(taskGroups.ToList(), Formatting.Indented);
                await File.WriteAllTextAsync(_jsonFilePath, json, cancellationToken);

                _logger.LogInformation("保存了 {Count} 个任务组到文件", taskGroups.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存任务组到文件时发生错误");
                throw;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <summary>
        /// 添加任务组
        /// </summary>
        public async Task AddTaskGroupAsync(TaskGroupInfo taskGroup, CancellationToken cancellationToken = default)
        {
            if (taskGroup == null)
                throw new ArgumentNullException(nameof(taskGroup));

            var taskGroups = await GetAllTaskGroupsAsync(cancellationToken);
            
            // 检查是否已存在
            var existing = taskGroups.FirstOrDefault(t => 
                string.Equals(t.TaskGroup, taskGroup.TaskGroup, StringComparison.OrdinalIgnoreCase));
            
            if (existing != null)
            {
                _logger.LogWarning("任务组 '{TaskGroup}' 已存在", taskGroup.TaskGroup);
                return;
            }

            taskGroups.Add(taskGroup);
            await SaveTaskGroupsAsync(taskGroups, cancellationToken);
            
            _logger.LogInformation("添加了新任务组: {TaskGroup}", taskGroup.TaskGroup);
        }

        /// <summary>
        /// 删除任务组
        /// </summary>
        public async Task<bool> DeleteTaskGroupAsync(string taskGroupName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(taskGroupName))
                throw new ArgumentException("任务组名称不能为空", nameof(taskGroupName));

            var taskGroups = await GetAllTaskGroupsAsync(cancellationToken);
            var countBefore = taskGroups.Count;
            
            taskGroups.RemoveAll(t => string.Equals(t.TaskGroup, taskGroupName, StringComparison.OrdinalIgnoreCase));
            
            if (taskGroups.Count < countBefore)
            {
                await SaveTaskGroupsAsync(taskGroups, cancellationToken);
                _logger.LogInformation("删除了任务组: {TaskGroup}", taskGroupName);
                return true;
            }

            _logger.LogWarning("未找到要删除的任务组: {TaskGroup}", taskGroupName);
            return false;
        }

        /// <summary>
        /// 查找任务组
        /// </summary>
        public async Task<TaskGroupInfo?> FindTaskGroupAsync(string taskGroupName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(taskGroupName))
                throw new ArgumentException("任务组名称不能为空", nameof(taskGroupName));

            var taskGroups = await GetAllTaskGroupsAsync(cancellationToken);
            var taskGroup = taskGroups.FirstOrDefault(t => 
                string.Equals(t.TaskGroup, taskGroupName, StringComparison.OrdinalIgnoreCase));

            if (taskGroup != null)
            {
                _logger.LogDebug("找到任务组: {TaskGroup}", taskGroupName);
            }
            else
            {
                _logger.LogDebug("未找到任务组: {TaskGroup}", taskGroupName);
            }

            return taskGroup;
        }

        #region 私有方法

        private void EnsureCacheDirectory()
        {
            try
            {
                if (!Directory.Exists(_cacheDirectory))
                {
                    Directory.CreateDirectory(_cacheDirectory);
                    _logger.LogInformation("创建缓存目录: {CacheDirectory}", _cacheDirectory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建缓存目录时发生错误: {CacheDirectory}", _cacheDirectory);
                throw;
            }
        }

        #endregion

        #region IDisposable 实现

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _fileLock?.Dispose();
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