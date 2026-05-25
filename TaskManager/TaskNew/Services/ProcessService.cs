using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using TaskManagerNew.Models;

namespace TaskManagerNew.Services
{
    /// <summary>
    /// 进程服务
    /// </summary>
    public class ProcessService : IProcessService
    {
        private readonly ILogger<ProcessService> _logger;
        private readonly IConfigurationService _configService;
        private readonly ProcessCache _cache;
        private readonly ConcurrentDictionary<int, ProcessPerformanceData> _performanceData = new();
        private DateTime _lastPerformanceUpdate = DateTime.MinValue;

        public ProcessService(ILogger<ProcessService> logger, IConfigurationService configService)
        {
            _logger = logger;
            _configService = configService;
            _cache = new ProcessCache(TimeSpan.FromMilliseconds(configService.GetProcessManagerConfig().CacheDuration));
        }

        /// <summary>
        /// 查找进程
        /// </summary>
        public async Task<List<ProcessInfo>> FindProcessesAsync(
            string searchTerm,
            ProcessSearchMode mode,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Searching processes with term: {searchTerm}, mode: {mode}");

                var processes = await GetAllProcessesAsync(cancellationToken);
                var filteredProcesses = FilterProcesses(processes, searchTerm, mode);

                _logger.LogInformation($"Found {filteredProcesses.Count} processes matching criteria");
                return filteredProcesses;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Process search was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to find processes");
                throw;
            }
        }

        /// <summary>
        /// 获取所有进程
        /// </summary>
        public async Task<List<ProcessInfo>> GetAllProcessesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // 检查缓存
                if (_cache.TryGetValue("AllProcesses", out List<ProcessInfo>? cachedProcesses) && cachedProcesses != null)
                {
                    _logger.LogDebug("Returning cached processes");
                    return cachedProcesses;
                }

                var processes = Process.GetProcesses();
                var result = new List<ProcessInfo>();

                // 使用并行处理，限制并发数
                var config = _configService.GetProcessManagerConfig();
                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Math.Min(config.MaxConcurrentProcesses, Environment.ProcessorCount),
                    CancellationToken = cancellationToken
                };

                await Parallel.ForEachAsync(processes, options, async (process, ct) =>
                {
                    try
                    {
                        var info = await CreateProcessInfoAsync(process, ct);
                        if (info != null)
                        {
                            lock (result)
                            {
                                result.Add(info);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to process PID {process.Id}");
                    }
                });

                // 更新缓存
                _cache.Set("AllProcesses", result);

                _logger.LogInformation($"Found {result.Count} processes");
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Get processes was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get processes");
                throw;
            }
        }

        /// <summary>
        /// 获取所有.NET进程
        /// </summary>
        public async Task<List<ProcessInfo>> GetDotNetProcessesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var allProcesses = await GetAllProcessesAsync(cancellationToken);
                var dotnetProcesses = allProcesses.Where(p => p.IsDotNetProcess).ToList();
                
                _logger.LogInformation($"Found {dotnetProcesses.Count} .NET processes");
                return dotnetProcesses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get .NET processes");
                throw;
            }
        }

        /// <summary>
        /// 获取进程详细信息
        /// </summary>
        public async Task<ProcessInfo?> GetProcessInfoAsync(int processId, CancellationToken cancellationToken = default)
        {
            try
            {
                // 检查缓存
                if (_cache.TryGetValue($"Process_{processId}", out ProcessInfo? cachedInfo) && cachedInfo != null)
                {
                    _logger.LogDebug($"Returning cached info for PID {processId}");
                    return cachedInfo;
                }

                var process = Process.GetProcessById(processId);
                var info = await CreateProcessInfoAsync(process, cancellationToken);

                if (info != null)
                {
                    _cache.Set($"Process_{processId}", info);
                }

                return info;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, $"Process with PID {processId} not found");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get info for PID {processId}");
                return null;
            }
        }

        /// <summary>
        /// 关闭进程
        /// </summary>
        public async Task<bool> KillProcessAsync(int processId, bool force = false, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Attempting to kill process {processId} (force: {force})");

                var process = Process.GetProcessById(processId);

                if (force)
                {
                    process.Kill();
                }
                else
                {
                    process.CloseMainWindow();
                    
                    // 等待进程正常退出
                    var exited = await Task.Run(() => process.WaitForExit(5000), cancellationToken);
                    if (!exited)
                    {
                        process.Kill();
                        _logger.LogWarning($"Process {processId} did not exit gracefully, forced kill");
                    }
                }

                // 从缓存中移除
                _cache.Remove($"Process_{processId}");
                _performanceData.TryRemove(processId, out _);

                _logger.LogInformation($"Successfully killed process {processId}");
                return true;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, $"Process {processId} not found");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to kill process {processId}");
                return false;
            }
        }

        /// <summary>
        /// 批量关闭进程
        /// </summary>
        public async Task<List<int>> KillProcessesAsync(IEnumerable<int> processIds, bool force = false, CancellationToken cancellationToken = default)
        {
            var failedProcesses = new List<int>();
            var tasks = new List<Task>();

            foreach (var processId in processIds)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var success = await KillProcessAsync(processId, force, cancellationToken);
                    if (!success)
                    {
                        lock (failedProcesses)
                        {
                            failedProcesses.Add(processId);
                        }
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks);
            return failedProcesses;
        }

        /// <summary>
        /// 更新进程性能数据
        /// </summary>
        public async Task UpdatePerformanceDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var config = _configService.GetProcessManagerConfig();
                if (!config.EnablePerformanceMonitoring)
                    return;

                // 限制更新频率
                if ((DateTime.Now - _lastPerformanceUpdate).TotalMilliseconds < config.PerformanceUpdateInterval)
                    return;

                var processes = await GetAllProcessesAsync(cancellationToken);
                var updateTasks = new List<Task>();

                foreach (var processInfo in processes)
                {
                    updateTasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            var process = Process.GetProcessById(processInfo.ProcessId);
                            UpdatePerformanceData(processInfo, process);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Failed to update performance data for PID {processInfo.ProcessId}");
                        }
                    }, cancellationToken));
                }

                await Task.WhenAll(updateTasks);
                _lastPerformanceUpdate = DateTime.Now;

                _logger.LogDebug($"Updated performance data for {processes.Count} processes");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update performance data");
            }
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
            _performanceData.Clear();
            _logger.LogInformation("Process cache cleared");
        }

        /// <summary>
        /// 创建进程信息
        /// </summary>
        private async Task<ProcessInfo?> CreateProcessInfoAsync(Process process, CancellationToken cancellationToken)
        {
            try
            {
                var commandLine = await GetCommandLineAsync(process, cancellationToken);
                if (string.IsNullOrEmpty(commandLine))
                    return null;

                var info = new ProcessInfo
                {
                    ProcessId = process.Id,
                    TaskName = process.ProcessName,
                    CommandLine = commandLine,
                    StartTime = process.StartTime,
                    TotalProcessorTime = process.TotalProcessorTime,
                    ThreadCount = process.Threads.Count,
                    HandleCount = process.HandleCount,
                    IsResponding = process.Responding
                };

                // 获取工作目录
                info.WorkingDirectory = GetWorkingDirectory(process);

                // 获取用户名
                info.UserName = GetProcessUserName(process);

                // 检查是否为.NET进程
                info.IsDotNetProcess = IsDotNetProcess(process);

                // 获取.NET版本
                if (info.IsDotNetProcess)
                {
                    info.DotNetVersion = GetDotNetVersion(commandLine);
                }

                // 获取文件信息
                await UpdateFileInfoAsync(info, process, cancellationToken);

                // 更新性能数据
                UpdatePerformanceData(info, process);

                return info;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to create process info for PID {process.Id}");
                return null;
            }
        }

        /// <summary>
        /// 获取命令行参数
        /// </summary>
        private async Task<string> GetCommandLineAsync(Process process, CancellationToken cancellationToken)
        {
            try
            {
                return await Task.Run(() =>
                {
                    using var searcher = new ManagementObjectSearcher(
                        $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}");
                    
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return obj["CommandLine"]?.ToString() ?? string.Empty;
                    }
                    
                    return string.Empty;
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to get command line for PID {process.Id}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 获取工作目录
        /// </summary>
        private string GetWorkingDirectory(Process process)
        {
            try
            {
                return process.StartInfo.WorkingDirectory ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 获取进程用户名
        /// </summary>
        private string GetProcessUserName(Process process)
        {
            try
            {
                return process.StartInfo.UserName ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 检查是否为.NET进程
        /// </summary>
        private bool IsDotNetProcess(Process process)
        {
            try
            {
                // 检查进程名称是否为dotnet
                if (process.ProcessName.Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                    return true;

                // 尝试获取命令行来检查
                try
                {
                    using var searcher = new ManagementObjectSearcher(
                        $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}");
                    
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var commandLine = obj["CommandLine"]?.ToString() ?? string.Empty;
                        // 检查命令行是否包含.NET相关参数
                        return commandLine.Contains("dotnet", StringComparison.OrdinalIgnoreCase) ||
                               commandLine.Contains(".dll", StringComparison.OrdinalIgnoreCase) ||
                               commandLine.Contains(".exe", StringComparison.OrdinalIgnoreCase);
                    }
                }
                catch
                {
                    // 如果WMI查询失败，回退到基本检查
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取.NET版本
        /// </summary>
        private string GetDotNetVersion(string commandLine)
        {
            try
            {
                // 从命令行中提取.NET版本信息
                // 这里可以根据实际需要实现更精确的版本检测
                return "NET 8.0";
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// 更新文件信息
        /// </summary>
        private async Task UpdateFileInfoAsync(ProcessInfo info, Process process, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Run(() =>
                {
                    var mainModule = process.MainModule;
                    if (mainModule != null)
                    {
                        var fileVersionInfo = FileVersionInfo.GetVersionInfo(mainModule.FileName);
                        
                        info.Description = fileVersionInfo.FileDescription ?? string.Empty;
                        info.Company = fileVersionInfo.CompanyName ?? string.Empty;
                        info.FileVersion = fileVersionInfo.FileVersion ?? string.Empty;
                        info.ProductVersion = fileVersionInfo.ProductVersion ?? string.Empty;
                    }
                }, cancellationToken);
            }
            catch
            {
                // 忽略无法获取文件信息的异常
            }
        }

        /// <summary>
        /// 更新性能数据
        /// </summary>
        private void UpdatePerformanceData(ProcessInfo info, Process process)
        {
            try
            {
                var currentTime = DateTime.Now;
                var processId = process.Id;

                if (!_performanceData.TryGetValue(processId, out var previousData))
                {
                    previousData = new ProcessPerformanceData
                    {
                        LastUpdateTime = currentTime,
                        PreviousTotalProcessorTime = process.TotalProcessorTime
                    };
                    _performanceData[processId] = previousData;
                }

                // 计算CPU使用率
                var timeDiff = (currentTime - previousData.LastUpdateTime).TotalSeconds;
                if (timeDiff > 0)
                {
                    var processorTimeDiff = (process.TotalProcessorTime - previousData.PreviousTotalProcessorTime).TotalMilliseconds;
                    info.CpuUsage = (float)(processorTimeDiff / (timeDiff * 1000) * 100); // 百分比
                }

                // 更新内存使用量
                info.MemoryUsage = process.WorkingSet64;
                info.PeakMemoryUsage = process.PeakWorkingSet64;

                // 更新性能数据记录
                previousData.LastUpdateTime = currentTime;
                previousData.PreviousTotalProcessorTime = process.TotalProcessorTime;
            }
            catch
            {
                // 忽略性能数据更新异常
            }
        }

        /// <summary>
        /// 筛选进程
        /// </summary>
        private List<ProcessInfo> FilterProcesses(List<ProcessInfo> processes, string searchTerm, ProcessSearchMode mode)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return processes;

            return mode switch
            {
                ProcessSearchMode.ByName => processes
                    .Where(p => p.TaskName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                               p.CommandLine.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                               p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                               (p.IsDotNetProcess && searchTerm.Equals("dotnet", StringComparison.OrdinalIgnoreCase)))
                    .ToList(),
                ProcessSearchMode.ByDirectory => processes
                    .Where(p => p.WorkingDirectory.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                               p.CommandLine.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList(),
                ProcessSearchMode.ByPid => processes
                    .Where(p => p.ProcessId.ToString().Contains(searchTerm))
                    .ToList(),
                ProcessSearchMode.All => processes
                    .Where(p => p.TaskName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                               p.CommandLine.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                               p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                               p.WorkingDirectory.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                               p.ProcessId.ToString().Contains(searchTerm))
                    .ToList(),
                _ => processes
            };
        }

        /// <summary>
        /// 进程性能数据
        /// </summary>
        private class ProcessPerformanceData
        {
            public DateTime LastUpdateTime { get; set; }
            public TimeSpan PreviousTotalProcessorTime { get; set; }
        }
    }

    /// <summary>
    /// 进程服务接口
    /// </summary>
    public interface IProcessService
    {
        Task<List<ProcessInfo>> FindProcessesAsync(string searchTerm, ProcessSearchMode mode, CancellationToken cancellationToken = default);
        Task<List<ProcessInfo>> GetAllProcessesAsync(CancellationToken cancellationToken = default);
        Task<List<ProcessInfo>> GetDotNetProcessesAsync(CancellationToken cancellationToken = default);
        Task<ProcessInfo?> GetProcessInfoAsync(int processId, CancellationToken cancellationToken = default);
        Task<bool> KillProcessAsync(int processId, bool force = false, CancellationToken cancellationToken = default);
        Task<List<int>> KillProcessesAsync(IEnumerable<int> processIds, bool force = false, CancellationToken cancellationToken = default);
        Task UpdatePerformanceDataAsync(CancellationToken cancellationToken = default);
        void ClearCache();
    }
}