using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TaskManagerNew.Models;
using TaskManagerNew.Services;

namespace TaskManagerNew.ViewModels
{
    /// <summary>
    /// 主视图模型
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        private readonly ILogger<MainViewModel> _logger;
        private readonly IProcessService _processService;
        private readonly ITaskManagerService _taskManagerService;
        private readonly IConfigurationService _configService;
        private CancellationTokenSource? _searchCancellationTokenSource;
        private Timer? _refreshTimer;

        public MainViewModel(
            ILogger<MainViewModel> logger,
            IProcessService processService,
            ITaskManagerService taskManagerService,
            IConfigurationService configService)
        {
            _logger = logger;
            _processService = processService;
            _taskManagerService = taskManagerService;
            _configService = configService;

            Initialize();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        private async void Initialize()
        {
            // 加载任务群组
            LoadTaskGroups();

            // 初始加载所有进程
            await RefreshAllProcessesAsync();

            // 设置自动刷新
            var config = _configService.GetProcessManagerConfig();
            if (config.RefreshInterval > 0)
            {
                _refreshTimer = new Timer(async _ => await RefreshProcessesAsync(), null, 
                    config.RefreshInterval, config.RefreshInterval);
            }

            _logger.LogInformation("MainViewModel initialized");
        }

        #region 属性

        [ObservableProperty]
        private ObservableCollection<ProcessInfo> _processes = new();

        [ObservableProperty]
        private ObservableCollection<TaskGroupInfo> _taskGroups = new();

        [ObservableProperty]
        private ProcessInfo? _selectedProcess;

        [ObservableProperty]
        private TaskGroupInfo? _selectedTaskGroup;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private ProcessSearchMode _selectedSearchMode = ProcessSearchMode.ByName;

        [ObservableProperty]
        private bool _isSearching;

        [ObservableProperty]
        private bool _isRefreshing;

        [ObservableProperty]
        private string _statusMessage = "就绪";

        [ObservableProperty]
        private int _totalProcessCount;

        [ObservableProperty]
        private int _selectedProcessCount;

        [ObservableProperty]
        private float _totalCpuUsage;

        [ObservableProperty]
        private long _totalMemoryUsage;

        [ObservableProperty]
        private string _totalMemoryUsageFormatted = "0 B";

        [ObservableProperty]
        private bool _showSystemProcesses;

        [ObservableProperty]
        private bool _groupByTask = true;

        [ObservableProperty]
        private bool _autoRefresh = true;

        [ObservableProperty]
        private string _theme = "Dark";

        #endregion

        #region 命令

        /// <summary>
        /// 搜索进程命令
        /// </summary>
        [RelayCommand]
        private async Task SearchProcessesAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                StatusMessage = "请输入搜索内容";
                return;
            }

            // 取消之前的搜索
            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource = new CancellationTokenSource();

            IsSearching = true;
            StatusMessage = $"正在搜索: {SearchText}...";

            try
            {
                var processes = await _processService.FindProcessesAsync(
                    SearchText,
                    SelectedSearchMode,
                    _searchCancellationTokenSource.Token);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Processes.Clear();
                    foreach (var process in processes)
                    {
                        Processes.Add(process);
                    }

                    UpdateStatistics();
                    StatusMessage = $"找到 {processes.Count} 个进程";
                });
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "搜索已取消";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索进程失败");
                StatusMessage = $"搜索失败: {ex.Message}";
            }
            finally
            {
                IsSearching = false;
            }
        }

        /// <summary>
        /// 刷新进程命令
        /// </summary>
        [RelayCommand]
        private async Task RefreshProcessesAsync()
        {
            if (!AutoRefresh || IsRefreshing)
                return;

            IsRefreshing = true;
            StatusMessage = "正在刷新进程列表...";

            try
            {
                // 更新性能数据
                await _processService.UpdatePerformanceDataAsync();

                // 如果当前有选中的任务群组，则刷新该群组的进程
                if (SelectedTaskGroup != null)
                {
                    await RefreshTaskGroupProcessesAsync(SelectedTaskGroup.TaskGroup);
                }
                else if (!string.IsNullOrEmpty(SearchText))
                {
                    // 重新执行搜索
                    await SearchProcessesAsync();
                }
                else
                {
                    // 刷新所有进程
                    await RefreshAllProcessesAsync();
                }

                StatusMessage = "刷新完成";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新进程失败");
                StatusMessage = $"刷新失败: {ex.Message}";
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        /// <summary>
        /// 创建任务群组命令
        /// </summary>
        [RelayCommand]
        private void CreateTaskGroup()
        {
            try
            {
                var groupName = $"群组_{TaskGroups.Count + 1}";
                var group = _taskManagerService.CreateTaskGroup(groupName, "新建任务群组");
                
                TaskGroups.Add(group);
                SelectedTaskGroup = group;
                
                StatusMessage = $"已创建任务群组: {groupName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建任务群组失败");
                StatusMessage = $"创建失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 删除任务群组命令
        /// </summary>
        [RelayCommand]
        private void DeleteTaskGroup()
        {
            if (SelectedTaskGroup == null)
            {
                StatusMessage = "请先选择要删除的任务群组";
                return;
            }

            var result = MessageBox.Show(
                $"确定要删除任务群组 '{SelectedTaskGroup.TaskGroup}' 吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var groupName = SelectedTaskGroup.TaskGroup;
                    var success = _taskManagerService.DeleteTaskGroup(groupName);
                    
                    if (success)
                    {
                        TaskGroups.Remove(SelectedTaskGroup);
                        SelectedTaskGroup = null;
                        Processes.Clear();
                        StatusMessage = $"已删除任务群组: {groupName}";
                    }
                    else
                    {
                        StatusMessage = $"删除任务群组失败: {groupName}";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "删除任务群组失败");
                    StatusMessage = $"删除失败: {ex.Message}";
                }
            }
        }

        /// <summary>
        /// 关闭选中进程命令
        /// </summary>
        [RelayCommand]
        private async Task CloseSelectedProcessesAsync()
        {
            var selectedProcesses = Processes.Where(p => p.IsSelected).ToList();
            if (selectedProcesses.Count == 0)
            {
                StatusMessage = "请先选择要关闭的进程";
                return;
            }

            var result = MessageBox.Show(
                $"确定要关闭 {selectedProcesses.Count} 个选中的进程吗？",
                "确认关闭",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                StatusMessage = $"正在关闭 {selectedProcesses.Count} 个进程...";

                try
                {
                    var processIds = selectedProcesses.Select(p => p.ProcessId).ToList();
                    var failedProcesses = await _processService.KillProcessesAsync(processIds);

                    if (failedProcesses.Count == 0)
                    {
                        // 从列表中移除已关闭的进程
                        foreach (var process in selectedProcesses)
                        {
                            Processes.Remove(process);
                        }

                        // 如果当前有选中的任务群组，也从群组中移除
                        if (SelectedTaskGroup != null)
                        {
                            foreach (var process in selectedProcesses)
                            {
                                _taskManagerService.RemoveProcessFromGroup(SelectedTaskGroup.TaskGroup, process.ProcessId);
                            }
                        }

                        UpdateStatistics();
                        StatusMessage = $"已成功关闭 {selectedProcesses.Count} 个进程";
                    }
                    else
                    {
                        StatusMessage = $"成功关闭 {selectedProcesses.Count - failedProcesses.Count} 个进程，{failedProcesses.Count} 个进程关闭失败";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "关闭进程失败");
                    StatusMessage = $"关闭失败: {ex.Message}";
                }
            }
        }

        /// <summary>
        /// 全选命令
        /// </summary>
        [RelayCommand]
        private void SelectAllProcesses()
        {
            foreach (var process in Processes)
            {
                process.IsSelected = true;
            }
            UpdateSelectedCount();
        }

        /// <summary>
        /// 取消全选命令
        /// </summary>
        [RelayCommand]
        private void DeselectAllProcesses()
        {
            foreach (var process in Processes)
            {
                process.IsSelected = false;
            }
            UpdateSelectedCount();
        }

        /// <summary>
        /// 添加选中进程到当前群组命令
        /// </summary>
        [RelayCommand]
        private async Task AddSelectedToGroupAsync()
        {
            if (SelectedTaskGroup == null)
            {
                StatusMessage = "请先选择目标任务群组";
                return;
            }

            var selectedProcesses = Processes.Where(p => p.IsSelected).ToList();
            if (selectedProcesses.Count == 0)
            {
                StatusMessage = "请先选择要添加的进程";
                return;
            }

            StatusMessage = $"正在添加 {selectedProcesses.Count} 个进程到群组...";
            int successCount = 0;

            try
            {
                foreach (var process in selectedProcesses)
                {
                    var success = await _taskManagerService.AddProcessToGroupAsync(
                        SelectedTaskGroup.TaskGroup,
                        process.ProcessId);

                    if (success)
                    {
                        successCount++;
                        process.TaskGroup = SelectedTaskGroup.TaskGroup;
                    }
                }

                StatusMessage = $"成功添加 {successCount} 个进程到群组 '{SelectedTaskGroup.TaskGroup}'";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加进程到群组失败");
                StatusMessage = $"添加失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 清除缓存命令
        /// </summary>
        [RelayCommand]
        private void ClearCache()
        {
            try
            {
                _processService.ClearCache();
                StatusMessage = "缓存已清除";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除缓存失败");
                StatusMessage = $"清除缓存失败: {ex.Message}";
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 加载任务群组
        /// </summary>
        private void LoadTaskGroups()
        {
            try
            {
                var groups = _taskManagerService.GetTaskGroups();
                TaskGroups.Clear();
                
                foreach (var group in groups)
                {
                    TaskGroups.Add(group);
                }

                _logger.LogInformation($"Loaded {TaskGroups.Count} task groups");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load task groups");
            }
        }

        /// <summary>
        /// 刷新任务群组进程
        /// </summary>
        private async Task RefreshTaskGroupProcessesAsync(string groupName)
        {
            var group = _taskManagerService.GetTaskGroup(groupName);
            if (group == null)
                return;

            Processes.Clear();
            
            foreach (var process in group.Processes)
            {
                // 获取最新的进程信息
                var updatedProcess = await _processService.GetProcessInfoAsync(process.ProcessId);
                if (updatedProcess != null)
                {
                    updatedProcess.TaskGroup = groupName;
                    Processes.Add(updatedProcess);
                }
            }

            UpdateStatistics();
        }

        /// <summary>
        /// 刷新所有进程
        /// </summary>
        private async Task RefreshAllProcessesAsync()
        {
            var processes = await _processService.GetAllProcessesAsync();
            
            Processes.Clear();
            foreach (var process in processes)
            {
                Processes.Add(process);
            }

            UpdateStatistics();
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics()
        {
            TotalProcessCount = Processes.Count;
            UpdateSelectedCount();
            
            TotalCpuUsage = Processes.Sum(p => p.CpuUsage);
            TotalMemoryUsage = Processes.Sum(p => p.MemoryUsage);
            TotalMemoryUsageFormatted = FormatBytes(TotalMemoryUsage);
        }

        /// <summary>
        /// 更新选中数量
        /// </summary>
        private void UpdateSelectedCount()
        {
            SelectedProcessCount = Processes.Count(p => p.IsSelected);
        }

        /// <summary>
        /// 格式化字节大小
        /// </summary>
        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            
            return $"{len:F2} {sizes[order]}";
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 任务群组选择变化
        /// </summary>
        partial void OnSelectedTaskGroupChanged(TaskGroupInfo? value)
        {
            if (value != null)
            {
                _ = RefreshTaskGroupProcessesAsync(value.TaskGroup);
            }
        }

        /// <summary>
        /// 进程选择变化
        /// </summary>
        partial void OnSelectedProcessChanged(ProcessInfo? value)
        {
            // 可以在这里实现进程详情显示逻辑
        }

        /// <summary>
        /// 自动刷新变化
        /// </summary>
        partial void OnAutoRefreshChanged(bool value)
        {
            if (value && _refreshTimer != null)
            {
                var config = _configService.GetProcessManagerConfig();
                _refreshTimer.Change(config.RefreshInterval, config.RefreshInterval);
            }
            else
            {
                _refreshTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        #endregion

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _searchCancellationTokenSource?.Cancel();
            _refreshTimer?.Dispose();
            _taskManagerService.Dispose();
        }
    }
}