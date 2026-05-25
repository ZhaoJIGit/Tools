using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.ViewModels;

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();
}

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ProcessService _processService;
    private readonly JsonStorageService _jsonService;

    public ObservableCollection<ProcessInfo> ProcessInfos { get; } = new();
    public ObservableCollection<TaskGroupInfo> TaskGroups { get; } = new();

    private TaskGroupInfo? _currentGroup;
    public TaskGroupInfo? CurrentGroup
    {
        get => _currentGroup;
        set
        {
            _currentGroup = value;
            OnPropertyChanged();
        }
    }

    private string _executionTime = "耗时：0 s";
    public string ExecutionTime
    {
        get => _executionTime;
        set
        {
            _executionTime = value;
            OnPropertyChanged();
        }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public ICommand SearchCommand { get; }
    public ICommand KillSelectedCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel(ProcessService processService, JsonStorageService jsonService)
    {
        _processService = processService;
        _jsonService = jsonService;

        SearchCommand = new RelayCommand(async () => await SearchProcessesAsync(""));
        KillSelectedCommand = new RelayCommand(KillSelectedProcesses);
    }

    public void LoadTaskGroups()
    {
        var groups = _jsonService.LoadTaskGroups();
        TaskGroups.Clear();
        foreach (var g in groups)
            TaskGroups.Add(g);
    }

    public void AddTaskGroup(string name)
    {
        var item = new TaskGroupInfo { TaskGroup = name };
        TaskGroups.Add(item);
        SaveGroups();
        CurrentGroup = item;
    }

    public void DeleteTaskGroup(TaskGroupInfo? task)
    {
        if (task == null) return;

        TaskGroups.Remove(task);
        if (CurrentGroup?.TaskGroup == task.TaskGroup)
        {
            ProcessInfos.Clear();
            CurrentGroup = null;
        }
        SaveGroups();
    }

    private void SaveGroups()
    {
        _jsonService.SaveTaskGroups(TaskGroups.ToList());
    }

    public async Task SearchProcessesAsync(string searchName)
    {
        IsLoading = true;
        ProcessInfos.Clear();

        var stopwatch = Stopwatch.StartNew();

        try
        {
            bool isDirectorySearch = searchName.Contains("\\") || searchName.Contains("/");
            List<ProcessInfo> results;

            if (isDirectorySearch)
                results = await _processService.FindProcessesByDirectoryAsync(searchName);
            else
                results = await _processService.FindDotnetProcessesAsync(searchName);

            foreach (var p in results)
                ProcessInfos.Add(p);
        }
        finally
        {
            stopwatch.Stop();
            ExecutionTime = $"耗时：{(int)stopwatch.Elapsed.TotalSeconds} s";
            IsLoading = false;
        }
    }

    public void KillSelectedProcesses()
    {
        var selected = ProcessInfos.Where(p => p.IsSelected).ToList();
        if (selected.Count == 0)
        {
            System.Windows.MessageBox.Show("请选择要关闭的任务.");
            return;
        }

        var failed = new List<int>();
        foreach (var p in selected)
        {
            try
            {
                _processService.KillProcess(p.ProcessId);
                ProcessInfos.Remove(p);
            }
            catch
            {
                failed.Add(p.ProcessId);
            }
        }

        if (failed.Count > 0)
            System.Windows.MessageBox.Show($"进程 {string.Join(",", failed)}: 未运行或无法关闭");
        else
            System.Windows.MessageBox.Show("已关闭选中进程");
    }

    public void SelectAll(bool select)
    {
        foreach (var item in ProcessInfos)
            item.IsSelected = select;
    }

    protected void OnPropertyChanged(string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
