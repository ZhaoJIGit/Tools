using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskManagerNew.Models;

namespace TaskManagerNew.Services
{
    /// <summary>
    /// 任务管理服务
    /// </summary>
    public class TaskManagerService : ITaskManagerService
    {
        private readonly ILogger<TaskManagerService> _logger;
        private readonly IProcessService _processService;
        private readonly IConfigurationService _configService;
        private readonly string _dataDirectory;
        private readonly string _taskGroupsFile;
        private readonly ConcurrentDictionary<string, TaskGroupInfo> _taskGroups = new();
        private Timer? _autoSaveTimer;
        private Timer? _monitoringTimer;

        public TaskManagerService(
            ILogger<TaskManagerService> logger,
            IProcessService processService,
            IConfigurationService configService)
        {
            _logger = logger;
            _processService = processService;
            _configService = configService;

            // 初始化数据目录
            _dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TaskManagerPro",
                "Data");

            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
            }

            _taskGroupsFile = Path.Combine(_dataDirectory, "TaskGroups.json");

            // 加载任务群组
            LoadTaskGroups();

            // 启动自动保存定时器
            var config = _configService.GetProcessManagerConfig();
            _autoSaveTimer = new Timer(_ => SaveTaskGroups(), null, 
                config.AutoSaveInterval, config.AutoSaveInterval);

            // 启动监控定时器
            if (config.EnablePerformanceMonitoring)
            {
                _monitoringTimer = new Timer(async _ => await UpdateMonitoringAsync(), null,
                    config.PerformanceUpdateInterval, config.PerformanceUpdateInterval);
            }

            _logger.LogInformation("TaskManagerService initialized");
        }

        /// <summary>
        /// 获取所有任务群组
        /// </summary>
        public ObservableCollection<TaskGroupInfo> GetTaskGroups()
        {
            return new ObservableCollection<TaskGroupInfo>(_taskGroups.Values);
        }

        /// <summary>
        /// 获取任务群组
        /// </summary>
        public TaskGroupInfo? GetTaskGroup(string groupName)
        {
            return _taskGroups.TryGetValue(groupName, out var group) ? group : null;
        }

        /// <summary>
        /// 创建任务群组
        /// </summary>
        public TaskGroupInfo CreateTaskGroup(string groupName, string description = "")
        {
            if (_taskGroups.ContainsKey(groupName))
            {
                throw new ArgumentException($"Task group '{groupName}' already exists");
            }

            var group = new TaskGroupInfo
            {
                TaskGroup = groupName,
                Description = description
            };

            _taskGroups[groupName] = group;
            SaveTaskGroups();

            _logger.LogInformation($"Created task group: {groupName}");
            return group;
        }

        /// <summary>
        /// 更新任务群组
        /// </summary>
        public bool UpdateTaskGroup(TaskGroupInfo group)
        {
            if (!_taskGroups.ContainsKey(group.TaskGroup))
                return false;

            _taskGroups[group.TaskGroup] = group;
            group.LastModifiedTime = DateTime.Now;
            SaveTaskGroups();

            _logger.LogInformation($"Updated task group: {group.TaskGroup}");
            return true;
        }

        /// <summary>
        /// 删除任务群组
        /// </summary>
        public bool DeleteTaskGroup(string groupName)
        {
            if (!_taskGroups.ContainsKey(groupName))
                return false;

            _taskGroups.TryRemove(groupName, out _);
            SaveTaskGroups();

            _logger.LogInformation($"Deleted task group: {groupName}");
            return true;
        }

        /// <summary>
        /// 添加进程到任务群组
        /// </summary>
        public async Task<bool> AddProcessToGroupAsync(string groupName, int processId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_taskGroups.TryGetValue(groupName, out var group))
                    return false;

                var processInfo = await _processService.GetProcessInfoAsync(processId, cancellationToken);
                if (processInfo == null)
                    return false;

                // 检查进程是否已在群组中
                if (group.FindProcess(processId) != null)
                    return false;

                processInfo.TaskGroup = groupName;
                group.AddProcess(processInfo);
                SaveTaskGroups();

                _logger.LogInformation($"Added process {processId} to group {groupName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to add process {processId} to group {groupName}");
                return false;
            }
        }

        /// <summary>
        /// 从任务群组移除进程
        /// </summary>
        public bool RemoveProcessFromGroup(string groupName, int processId)
        {
            if (!_taskGroups.TryGetValue(groupName, out var group))
                return false;

            var process = group.FindProcess(processId);
            if (process == null)
                return false;

            var result = group.RemoveProcess(process);
            if (result)
            {
                SaveTaskGroups();
                _logger.LogInformation($"Removed process {processId} from group {groupName}");
            }

            return result;
        }

        /// <summary>
        /// 查找进程所在群组
        /// </summary>
        public string? FindProcessGroup(int processId)
        {
            foreach (var group in _taskGroups.Values)
            {
                if (group.FindProcess(processId) != null)
                    return group.TaskGroup;
            }

            return null;
        }

        /// <summary>
        /// 搜索任务群组
        /// </summary>
        public List<TaskGroupInfo> SearchTaskGroups(string searchTerm)
        {
            return _taskGroups.Values
                .Where(g => g.TaskGroup.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                           g.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// 获取群组统计信息
        /// </summary>
        public TaskGroupStatistics GetGroupStatistics(string groupName)
        {
            if (!_taskGroups.TryGetValue(groupName, out var group))
                throw new ArgumentException($"Task group '{groupName}' not found");

            var stats = new TaskGroupStatistics
            {
                GroupName = groupName,
                TotalProcesses = group.ProcessCount,
                RunningProcesses = group.RunningProcessCount,
                ErrorProcesses = group.ErrorProcessCount,
                TotalCpuUsage = group.TotalCpuUsage,
                TotalMemoryUsage = group.TotalMemoryUsage,
                CreatedTime = group.CreatedTime,
                LastModifiedTime = group.LastModifiedTime
            };

            return stats;
        }

        /// <summary>
        /// 获取所有群组统计信息
        /// </summary>
        public List<TaskGroupStatistics> GetAllGroupStatistics()
        {
            var stats = new List<TaskGroupStatistics>();

            foreach (var groupName in _taskGroups.Keys)
            {
                stats.Add(GetGroupStatistics(groupName));
            }

            return stats;
        }

        /// <summary>
        /// 执行群组操作
        /// </summary>
        public async Task<GroupOperationResult> ExecuteGroupOperationAsync(
            string groupName, 
            GroupOperation operation, 
            CancellationToken cancellationToken = default)
        {
            if (!_taskGroups.TryGetValue(groupName, out var group))
                return new GroupOperationResult { Success = false, Message = $"Group '{groupName}' not found" };

            var result = new GroupOperationResult
            {
                GroupName = groupName,
                Operation = operation,
                ProcessResults = new List<ProcessOperationResult>()
            };

            try
            {
                switch (operation)
                {
                    case GroupOperation.StartAll:
                        // 这里可以实现启动所有进程的逻辑
                        result.Message = "Start all operation not implemented yet";
                        break;

                    case GroupOperation.StopAll:
                        var processIds = group.Processes.Select(p => p.ProcessId).ToList();
                        var failedProcesses = await _processService.KillProcessesAsync(processIds, false, cancellationToken);
                        
                        result.Success = failedProcesses.Count == 0;
                        result.Message = failedProcesses.Count == 0 
                            ? "All processes stopped successfully" 
                            : $"Failed to stop {failedProcesses.Count} processes";
                        
                        foreach (var processId in processIds)
                        {
                            result.ProcessResults.Add(new ProcessOperationResult
                            {
                                ProcessId = processId,
                                Success = !failedProcesses.Contains(processId),
                                Message = failedProcesses.Contains(processId) ? "Failed to stop" : "Stopped successfully"
                            });
                        }
                        break;

                    case GroupOperation.RestartAll:
                        result.Message = "Restart all operation not implemented yet";
                        break;

                    case GroupOperation.MonitorAll:
                        // 启用/禁用监控
                        group.IsMonitoringEnabled = !group.IsMonitoringEnabled;
                        UpdateTaskGroup(group);
                        
                        result.Success = true;
                        result.Message = group.IsMonitoringEnabled 
                            ? "Monitoring enabled for all processes" 
                            : "Monitoring disabled for all processes";
                        break;

                    default:
                        result.Message = $"Unknown operation: {operation}";
                        break;
                }

                _logger.LogInformation($"Executed group operation '{operation}' on group '{groupName}': {result.Message}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Failed to execute operation: {ex.Message}";
                _logger.LogError(ex, $"Failed to execute group operation '{operation}' on group '{groupName}'");
            }

            return result;
        }

        /// <summary>
        /// 保存任务群组
        /// </summary>
        public void SaveTaskGroups()
        {
            try
            {
                var groups = _taskGroups.Values.Select(g => g.Clone()).ToList();
                var json = JsonConvert.SerializeObject(groups, Formatting.Indented);
                File.WriteAllText(_taskGroupsFile, json);

                _logger.LogDebug($"Saved {groups.Count} task groups to {_taskGroupsFile}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save task groups");
            }
        }

        /// <summary>
        /// 加载任务群组
        /// </summary>
        private void LoadTaskGroups()
        {
            try
            {
                if (!File.Exists(_taskGroupsFile))
                {
                    _logger.LogInformation("No task groups file found, starting with empty groups");
                    return;
                }

                var json = File.ReadAllText(_taskGroupsFile);
                var groups = JsonConvert.DeserializeObject<List<TaskGroupInfo>>(json);

                if (groups != null)
                {
                    foreach (var group in groups)
                    {
                        _taskGroups[group.TaskGroup] = group;
                    }

                    _logger.LogInformation($"Loaded {groups.Count} task groups from {_taskGroupsFile}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load task groups");
            }
        }

        /// <summary>
        /// 更新监控数据
        /// </summary>
        private async Task UpdateMonitoringAsync()
        {
            try
            {
                await _processService.UpdatePerformanceDataAsync();

                // 更新所有启用监控的群组
                foreach (var group in _taskGroups.Values.Where(g => g.IsMonitoringEnabled))
                {
                    // 这里可以实现监控逻辑，比如检查进程状态、发送通知等
                }

                _logger.LogDebug("Monitoring data updated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update monitoring data");
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _autoSaveTimer?.Dispose();
            _monitoringTimer?.Dispose();
            SaveTaskGroups();
        }
    }

    /// <summary>
    /// 群组操作类型
    /// </summary>
    public enum GroupOperation
    {
        StartAll,
        StopAll,
        RestartAll,
        MonitorAll
    }

    /// <summary>
    /// 群组操作结果
    /// </summary>
    public class GroupOperationResult
    {
        public bool Success { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public GroupOperation Operation { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ProcessOperationResult> ProcessResults { get; set; } = new();
    }

    /// <summary>
    /// 进程操作结果
    /// </summary>
    public class ProcessOperationResult
    {
        public int ProcessId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 任务群组统计信息
    /// </summary>
    public class TaskGroupStatistics
    {
        public string GroupName { get; set; } = string.Empty;
        public int TotalProcesses { get; set; }
        public int RunningProcesses { get; set; }
        public int ErrorProcesses { get; set; }
        public float TotalCpuUsage { get; set; }
        public long TotalMemoryUsage { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime LastModifiedTime { get; set; }
    }

    /// <summary>
    /// 任务管理服务接口
    /// </summary>
    public interface ITaskManagerService : IDisposable
    {
        ObservableCollection<TaskGroupInfo> GetTaskGroups();
        TaskGroupInfo? GetTaskGroup(string groupName);
        TaskGroupInfo CreateTaskGroup(string groupName, string description = "");
        bool UpdateTaskGroup(TaskGroupInfo group);
        bool DeleteTaskGroup(string groupName);
        Task<bool> AddProcessToGroupAsync(string groupName, int processId, CancellationToken cancellationToken = default);
        bool RemoveProcessFromGroup(string groupName, int processId);
        string? FindProcessGroup(int processId);
        List<TaskGroupInfo> SearchTaskGroups(string searchTerm);
        TaskGroupStatistics GetGroupStatistics(string groupName);
        List<TaskGroupStatistics> GetAllGroupStatistics();
        Task<GroupOperationResult> ExecuteGroupOperationAsync(string groupName, GroupOperation operation, CancellationToken cancellationToken = default);
        void SaveTaskGroups();
    }
}