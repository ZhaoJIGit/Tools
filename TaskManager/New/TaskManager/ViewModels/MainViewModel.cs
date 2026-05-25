using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using TaskManager.Core.Interfaces;
using TaskManager.Core.Models;

namespace TaskManager.ViewModels
{
    /// <summary>
    /// 主视图模型
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly IProcessMonitor _processMonitor;
        private readonly ITaskGroupRepository _taskGroupRepository;
        private readonly ILogger<MainViewModel> _logger;
        private readonly AppSettings _settings;

        private string _searchText = string.Empty;
        private string _statusMessage = "就绪";
        private bool _isBusy;
        private TaskGroupInfo? _selectedTaskGroup;
        private ProcessInfo? _selectedProcess;

        /// <summary>
        /// 搜索文本
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// 是否忙碌
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        /// <summary>
        /// 选中的任务组
        /// </summary>
        public TaskGroupInfo? SelectedTaskGroup
        {
            get => _selectedTaskGroup;
            set => SetProperty(ref _selectedTaskGroup, value);
        }

        /// <summary>
        /// 选中的进程
        /// </summary>
        public ProcessInfo? SelectedProcess
        {
            get => _selectedProcess;
            set => SetProperty(ref _selectedProcess, value);
        }

        /// <summary>
        /// 任务组列表
        /// </summary>
        public ObservableCollection<TaskGroupInfo> TaskGroups { get; } = new();

        /// <summary>
        /// 进程列表
        /// </summary>
        public ObservableCollection<ProcessInfo> Processes { get; } = new();

        /// <summary>
        /// 搜索命令
        /// </summary>
        public ICommand SearchCommand { get; }

        /// <summary>
        /// 刷新命令
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// 终止进程命令
        /// </summary>
        public ICommand KillProcessCommand { get; }

        /// <summary>
        /// 添加任务组命令
        /// </summary>
        public ICommand AddTaskGroupCommand { get; }

        /// <summary>
        /// 删除任务组命令
        /// </summary>
        public ICommand DeleteTaskGroupCommand { get; }

        /// <summary>
        /// 查看进程详情命令
        /// </summary>
        public ICommand ViewProcessDetailCommand { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public MainViewModel(
            IProcessMonitor processMonitor,
            ITaskGroupRepository taskGroupRepository,
            ILogger<MainViewModel> logger,
            AppSettings settings)
        {
            _processMonitor = processMonitor ?? throw new ArgumentNullException(nameof(processMonitor));
            _taskGroupRepository = taskGroupRepository ?? throw new ArgumentNullException(nameof(taskGroupRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            // 初始化命令
            SearchCommand = new AsyncRelayCommand(ExecuteSearchAsync, CanExecuteSearch);
            RefreshCommand = new AsyncRelayCommand(ExecuteRefreshAsync);
            KillProcessCommand = new AsyncRelayCommand(ExecuteKillProcessAsync, CanExecuteKillProcess);
            AddTaskGroupCommand = new AsyncRelayCommand(ExecuteAddTaskGroupAsync, CanExecuteAddTaskGroup);
            DeleteTaskGroupCommand = new AsyncRelayCommand(ExecuteDeleteTaskGroupAsync, CanExecuteDeleteTaskGroup);
            ViewProcessDetailCommand = new AsyncRelayCommand(ExecuteViewProcessDetailAsync, CanExecuteViewProcessDetail);

            // 加载初始数据
            _ = LoadInitialDataAsync();
        }

        /// <summary>
        /// 加载初始数据
        /// </summary>
        private async Task LoadInitialDataAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "正在加载任务组...";

                var taskGroups = await _taskGroupRepository.GetAllTaskGroupsAsync();
                
                TaskGroups.Clear();
                foreach (var taskGroup in taskGroups)
                {
                    TaskGroups.Add(taskGroup);
                }

                StatusMessage = $"已加载 {TaskGroups.Count} 个任务组";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载初始数据时发生错误");
                StatusMessage = $"加载失败: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 执行搜索
        /// </summary>
        private async Task ExecuteSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                StatusMessage = "请输入搜索内容";
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = $"正在搜索: {SearchText}";

                // 查找或创建任务组
                var taskGroup = await FindOrCreateTaskGroupAsync(SearchText);
                if (taskGroup == null)
                    return;

                SelectedTaskGroup = taskGroup;

                // 搜索进程
                var processes = await _processMonitor.GetDotnetProcessesAsync(SearchText);
                
                Processes.Clear();
                foreach (var process in processes)
                {
                    Processes.Add(process);
                }

                // 更新任务组的进程数量
                taskGroup.ProcessCount = Processes.Count;
                await SaveTaskGroupsAsync();

                StatusMessage = $"找到 {Processes.Count} 个进程";
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "搜索已取消";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索进程时发生错误");
                StatusMessage = $"搜索失败: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 是否可以执行搜索
        /// </summary>
        private bool CanExecuteSearch()
        {
            return !IsBusy && !string.IsNullOrWhiteSpace(SearchText);
        }

        /// <summary>
        /// 执行刷新
        /// </summary>
        private async Task ExecuteRefreshAsync()
        {
            if (SelectedTaskGroup == null)
            {
                StatusMessage = "请先选择任务组";
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = $"正在刷新: {SelectedTaskGroup.TaskGroup}";

                var processes = await _processMonitor.GetDotnetProcessesAsync(SelectedTaskGroup.TaskGroup);
                
                Processes.Clear();
                foreach (var process in processes)
                {
                    Processes.Add(process);
                }

                SelectedTaskGroup.ProcessCount = Processes.Count;
                await SaveTaskGroupsAsync();

                StatusMessage = $"刷新完成，找到 {Processes.Count} 个进程";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新进程时发生错误");
                StatusMessage = $"刷新失败: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 执行终止进程
        /// </summary>
        private async Task ExecuteKillProcessAsync()
        {
            var selectedProcesses = Processes.Where(p => p.IsSelected).ToList();
            if (selectedProcesses.Count == 0)
            {
                StatusMessage = "请选择要终止的进程";
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = $"正在终止 {selectedProcesses.Count} 个进程...";

                var processIds = selectedProcesses.Select(p => p.ProcessId).ToList();
                var killedProcesses = await _processMonitor.KillProcessesAsync(processIds);

                // 从列表中移除已终止的进程
                foreach (var processId in killedProcesses)
                {
                    var process = Processes.FirstOrDefault(p => p.ProcessId == processId);
                    if (process != null)
                    {
                        Processes.Remove(process);
                    }
                }

                if (SelectedTaskGroup != null)
                {
                    SelectedTaskGroup.ProcessCount = Processes.Count;
                    await SaveTaskGroupsAsync();
                }

                StatusMessage = $"已终止 {killedProcesses.Count} 个进程";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "终止进程时发生错误");
                StatusMessage = $"终止失败: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 是否可以终止进程
        /// </summary>
        private bool CanExecuteKillProcess()
        {
            return !IsBusy && Processes.Any(p => p.IsSelected);
        }

        /// <summary>
        /// 执行添加任务组
        /// </summary>
        private async Task ExecuteAddTaskGroupAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                StatusMessage = "请输入任务组名称";
                return;
            }

            try
            {
                var taskGroup = new TaskGroupInfo { TaskGroup = SearchText };
                await _taskGroupRepository.AddTaskGroupAsync(taskGroup);
                
                TaskGroups.Add(taskGroup);
                SelectedTaskGroup = taskGroup;
                
                StatusMessage = $"已添加任务组: {SearchText}";
                SearchText = string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加任务组时发生错误");
                StatusMessage = $"添加失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 是否可以添加任务组
        /// </summary>
        private bool CanExecuteAddTaskGroup()
        {
            return !IsBusy && !string.IsNullOrWhiteSpace(SearchText);
        }

        /// <summary>
        /// 执行删除任务组
        /// </summary>
        private async Task ExecuteDeleteTaskGroupAsync()
        {
            if (SelectedTaskGroup == null)
            {
                StatusMessage = "请选择要删除的任务组";
                return;
            }

            try
            {
                var taskGroupName = SelectedTaskGroup.TaskGroup;
                var success = await _taskGroupRepository.DeleteTaskGroupAsync(taskGroupName);
                
                if (success)
                {
                    TaskGroups.Remove(SelectedTaskGroup);
                    SelectedTaskGroup = null;
                    Processes.Clear();
                    
                    StatusMessage = $"已删除任务组: {taskGroupName}";
                }
                else
                {
                    StatusMessage = $"删除任务组失败: {taskGroupName}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除任务组时发生错误");
                StatusMessage = $"删除失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 是否可以删除任务组
        /// </summary>
        private bool CanExecuteDeleteTaskGroup()
        {
            return !IsBusy && SelectedTaskGroup != null;
        }

        /// <summary>
        /// 执行查看进程详情
        /// </summary>
        private async Task ExecuteViewProcessDetailAsync()
        {
            if (SelectedProcess == null)
            {
                StatusMessage = "请选择要查看的进程";
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = $"正在获取进程 {SelectedProcess.ProcessId} 的详细信息...";

                var detail = await _processMonitor.GetProcessDetailAsync(SelectedProcess.ProcessId);
                
                // 这里可以打开详情对话框或显示在界面上
                StatusMessage = $"进程 {detail.ProcessName} (ID: {detail.ProcessId}) - 内存: {detail.WorkingSet / 1024 / 1024} MB";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取进程详情时发生错误");
                StatusMessage = $"获取详情失败: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 是否可以查看进程详情
        /// </summary>
        private bool CanExecuteViewProcessDetail()
        {
            return !IsBusy && SelectedProcess != null;
        }

        #region 私有辅助方法

        private async Task<TaskGroupInfo?> FindOrCreateTaskGroupAsync(string taskGroupName)
        {
            var existingTaskGroup = TaskGroups.FirstOrDefault(t => 
                string.Equals(t.TaskGroup, taskGroupName, StringComparison.OrdinalIgnoreCase));

            if (existingTaskGroup != null)
                return existingTaskGroup;

            // 创建新任务组
            var newTaskGroup = new TaskGroupInfo { TaskGroup = taskGroupName };
            await _taskGroupRepository.AddTaskGroupAsync(newTaskGroup);
            TaskGroups.Add(newTaskGroup);
            
            return newTaskGroup;
        }

        private async Task SaveTaskGroupsAsync()
        {
            await _taskGroupRepository.SaveTaskGroupsAsync(TaskGroups);
        }

        #endregion
    }

    /// <summary>
    /// 异步 RelayCommand 实现
    /// </summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        public event EventHandler? CanExecuteChanged;

        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return !_isExecuting && (_canExecute?.Invoke() ?? true);
        }

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
                return;

            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();

                await _execute();
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}