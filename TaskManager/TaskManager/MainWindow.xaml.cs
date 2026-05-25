using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TaskManager.Models;
using TaskManager.Services;
using TaskManager.ViewModels;

namespace TaskManager;

public partial class MainWindow
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        var processService = new ProcessService();
        var jsonService = new JsonStorageService();
        _viewModel = new MainViewModel(processService, jsonService);
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        DataContext = _viewModel;

        lvNames.ItemsSource = _viewModel.TaskGroups;
        lvProcesses.ItemsSource = _viewModel.ProcessInfos;

        _viewModel.TaskGroups.CollectionChanged += (_, _) => UpdateCounts();
        _viewModel.ProcessInfos.CollectionChanged += (_, _) => UpdateCounts();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
    }

    private void UpdateCounts()
    {
        txtGroupCount.Text = _viewModel.TaskGroups.Count.ToString();
        txtProcessCount.Text = _viewModel.ProcessInfos.Count.ToString();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _viewModel.LoadTaskGroups();
        UpdateCounts();
    }

    private async void btnFindProcesses_Click(object sender, RoutedEventArgs e)
    {
        string searchName = txtSearch.Text.Trim();
        if (string.IsNullOrEmpty(searchName))
        {
            MessageBox.Show("请输入要查找的名称.");
            return;
        }

        var existing = _viewModel.TaskGroups.FirstOrDefault(
            g => g.TaskGroup.Equals(searchName, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
        {
            _viewModel.AddTaskGroup(searchName);
        }
        else
        {
            _viewModel.CurrentGroup = existing;
        }

        if (_viewModel.CurrentGroup != null)
            lvNames.SelectedIndex = _viewModel.TaskGroups.IndexOf(_viewModel.CurrentGroup);

        await _viewModel.SearchProcessesAsync(searchName);
    }

    private async void lvNames_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var task = lvNames.SelectedItem as TaskGroupInfo;
        if (task == null) return;

        _viewModel.CurrentGroup = task;
        await _viewModel.SearchProcessesAsync(task.TaskGroup);
    }

    private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var task = lvNames.SelectedItem as TaskGroupInfo;
        if (task == null)
        {
            MessageBox.Show("群组不存在");
            return;
        }

        var result = MessageBox.Show("确定要删除此群组吗？", "确认操作",
            MessageBoxButton.OKCancel, MessageBoxImage.Question);

        if (result == MessageBoxResult.OK)
            _viewModel.DeleteTaskGroup(task);
    }

    private void chkSelectAll_Checked(object sender, RoutedEventArgs e)
    {
        _viewModel.SelectAll(true);
    }

    private void chkSelectAll_Unchecked(object sender, RoutedEventArgs e)
    {
        _viewModel.SelectAll(false);
    }

    private void btnCloseProcess_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.KillSelectedProcesses();
    }

    private void lvProcesses_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
    }

    private void lvProcesses_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }

    private void lvNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }
}
