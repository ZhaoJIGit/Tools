using PublishDesk;
using PublishDesk.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using static ApiClient;

public class MainViewModel : INotifyPropertyChanged
{
    #region 数据集合

    public ObservableCollection<SiteModel> Sites { get; set; }
        = new ObservableCollection<SiteModel>();

    public ObservableCollection<LogItem> Logs { get; set; }
        = new ObservableCollection<LogItem>();

    #endregion

    #region 当前选中站点

    private SiteModel _selectedSite;
    public SiteModel SelectedSite
    {
        get => _selectedSite;
        set
        {
            _selectedSite = value;
            OnPropertyChanged(nameof(SelectedSite));
        }
    }

    #endregion

    #region 进度 & 状态

    private double _progress;
    public double Progress
    {
        get => _progress;
        set
        {
            _progress = value;
            OnPropertyChanged(nameof(Progress));
        }
    }

    private string _statusText;
    public string StatusText
    {
        get => _statusText;
        set
        {
            _statusText = value;
            OnPropertyChanged(nameof(StatusText));
        }
    }
    private string _status;

    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged(nameof(Status));
        }
    }
    #endregion

    #region 全选

    private bool _isAllSelected;
    public bool IsAllSelected
    {
        get => _isAllSelected;
        set
        {
            _isAllSelected = value;
            OnPropertyChanged(nameof(IsAllSelected));
            SelectAll(value);
        }
    }

    #endregion

    #region 命令

    public RelayCommand AddCommand { get; }
    public RelayCommand PublishCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public ICommand UploadCommand { get; }
    public RelayCommand<SiteModel> EditCommand { get; }
    public RelayCommand<SiteModel> DeleteCommand { get; }
    #endregion

    #region 构造函数

    public MainViewModel()
    {
        AddCommand = new RelayCommand(AddSite);
        PublishCommand = new RelayCommand(BatchPublish);
        RefreshCommand = new RelayCommand(async () => await LoadSitesAsync());
        EditCommand = new RelayCommand<SiteModel>(EditSite);
        DeleteCommand = new RelayCommand<SiteModel>(DeleteSite);
        UploadCommand = new RelayCommand(async () => await Upload());
        //_ = LoadSitesAsync();
    }

    #endregion

    #region API加载站点（核心）

    public async Task LoadSitesAsync()
    {
        try
        {
            StatusText = "正在加载站点...";

            var list = await ApiClient.GetAsync<List<SiteModel>>("/api/Publish/GetSites");

            Sites.Clear();

            if (list != null)
            {
                foreach (var item in list)
                {
                    AttachSite(item);
                    Sites.Add(item);
                }
            }

            StatusText = $"加载完成，共 {Sites.Count} 个站点";
        }
        catch (Exception ex)
        {
            StatusText = "加载失败：" + ex.Message;
        }
    }

    #endregion

    #region 添加站点（API）

    private void AddSite()
    {
        OpenEditWindow(EditMode.Add, null);
    }
    private async Task UploadZip()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "ZIP文件|*.zip"
        };

        if (dlg.ShowDialog() != true)
            return;

        string filePath = dlg.FileName;

        AdddLog(new LogItem() { Message = $"开始上传文件....", LogType = LogType.Info });
        try
        {
            //using var client = new HttpClient();
            //using var form = new MultipartFormDataContent();

            //var fileStream = File.OpenRead(filePath);
            //var fileContent = new StreamContent(fileStream);

            //fileContent.Headers.ContentType =
            //    new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");

            //form.Add(fileContent, "file", Path.GetFileName(filePath));
            //form.Add(new StringContent(SelectedSite.Id ?? ""), "siteId");

            var response = await ApiClient.UploadAsync<ResultModel>(
                "api/Publish/Upload",
                filePath, SelectedSite.Id);
            if (response.success)
            {
                AdddLog(new LogItem() { Message = $"上传完成", LogType = LogType.Success });
            }
            else
            {
                AdddLog(new LogItem() { Message = $"上传失败", LogType = LogType.Error });
            }
        }
        catch (Exception ex)
        {
            AdddLog(new LogItem() { Message = $"上传失败：{ex.Message}", LogType = LogType.Error });
        }
    }


    private async Task Upload()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "ZIP文件|*.zip"
        };

        if (dlg.ShowDialog() != true)
            return;

        string filePath = dlg.FileName;
        //IsUploading = true;
        Progress = 0;
        Status = "准备上传...";

        try
        {
            var result = await ApiClient.UploadLargeFileAsync<CompleteUploadResponse>(
                filePath,
                SelectedSite.Id,
                onLog: (msg, logType) =>
                {
                    AdddLog(new LogItem() { Message = msg, LogType = logType });
                },
                onChunkComplete: (current, total, percent) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var percent = current * 100 / total;
                        Progress = percent;
                        Status = $"上传进度: {percent}% ({current}/{total})";
                        //AdddLog(new LogItem() { Message = msg, LogType = logType });
                    });
                }
            );
            if (result.Success)
            {
                AdddLog(new LogItem()
                {
                    Message = $"上传成功！",
                    LogType = LogType.Success
                });
            }
            else
            {
                AdddLog(new LogItem()
                {
                    Message = $"上传失败！",
                    LogType = LogType.Error
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"上传失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            //IsUploading = false;
        }
    }
    #endregion
    private void EditSite(SiteModel site)
    {
        if (site == null) return;

        OpenEditWindow(EditMode.Edit, site);
    }
    private async void DeleteSite(SiteModel site)
    {
        if (site == null) return;

        try
        {
            await ApiClient.PostAsync($"/api/Publish/DeleteSite/{site.Id}", null);

            Sites.Remove(site);

            AdddLog(new LogItem() { Message = $"[{DateTime.Now:HH:mm:ss}] 删除站点 {site.Name}", LogType = LogType.Success });
        }
        catch (Exception ex)
        {
            AdddLog(new LogItem() { Message = "删除失败：" + ex.Message, LogType = LogType.Error });
        }
    }
    #region 批量发布（API）

    private CancellationTokenSource _pollCts;

    private async void BatchPublish()
    {
        try
        {
            // 取消上一次轮询
            _pollCts?.Cancel();
            _pollCts = new CancellationTokenSource();

            CurrentTask = null;
            Logs.Clear();
            _logCache.Clear();
            var siteIds = Sites
                .Where(x => x.IsSelected)
                .Select(x => x.Id)
                .ToList();

            if (!siteIds.Any())
            {
                MessageBox.Show("请选择站点");
                return;
            }

            AdddLog(new LogItem
            {
                Message = $"开始批量发布，共 {siteIds.Count} 个站点",
                LogType = LogType.Info
            });

            // 调用接口
            var result = await ApiClient.PostAsync<StartDeployResponse>(
                "/api/Publish/StartDeploy",
                new { SiteIds = siteIds });

            if (result != null)
            {
                AdddLog(new LogItem
                {
                    Message = $"任务创建成功：{result.TaskId}",
                    LogType = LogType.Success
                });

                // 开始监听状态
                var token = _pollCts.Token;
                _ = StartStatusPolling(result.TaskId, token);
            }


        }
        catch (Exception ex)
        {
            AdddLog(new LogItem
            {
                Message = ex.Message,
                LogType = LogType.Error
            });
        }
    }
    public void AdddLog(LogItem log)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            Logs.Add(log);
        });
    }

    public void AdddLogFromPolling(LogItem log)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            var key = $"{log.Message}_{log.LogType}";
            if (_logCache.Add(key))
            {
                Logs.Add(log);
            }
        });
    }
    private readonly HashSet<string> _logCache
    = new();
    private DeployTask _currentTask;

    public DeployTask CurrentTask
    {
        get => _currentTask;
        set
        {
            _currentTask = value;
            OnPropertyChanged(nameof(CurrentTask));
        }
    }
    private async Task StartStatusPolling(string taskId, CancellationToken ct = default)
    {
        var maxPollCount = 600;
        var pollCount = 0;

        while (pollCount < maxPollCount)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                // 获取日志集合
                var task = await ApiClient.GetAsync<DeployTask>(
                    $"/api/Publish/GetStatus/{taskId}");

                // 没有日志
                if (task == null)
                {
                    pollCount++;
                    await Task.Delay(500);
                    if (pollCount > 10)
                    {
                        AdddLog(new LogItem { Message = "任务不存在，停止轮询", LogType = LogType.Warning });
                        break;
                    }
                    continue;
                }
                CurrentTask = task;

                foreach (var log in task.Logs)
                {
                    AdddLogFromPolling(log);
                }
                // 更新状态
                Progress = task.Progress;
                CurrentTask.Status = task.Status;
                // 最后一条日志

                    // 发布结束
                var terminalStatuses = new[] { "Success", "Failed", "Cancelled", "Unknown" };
                if (task != null && terminalStatuses.Contains(task.Status))
                {
                    break;
                }
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    AdddLog(new LogItem
                    {
                        Message = ex.Message,
                        LogType = LogType.Error,
                    });
                });

                break;
            }

            pollCount++;
            await Task.Delay(1000);
        }

        if (pollCount >= maxPollCount)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                AdddLog(new LogItem
                {
                    Message = "轮询超时（10分钟），请检查服务端状态",
                    LogType = LogType.Warning,
                });
            });
        }
    }
    #endregion

    #region 全选逻辑

    private void SelectAll(bool value)
    {
        foreach (var site in Sites)
        {
            site.IsSelected = value;
        }
    }

    #endregion

    #region 监听单个站点变化（用于同步全选）

    private void AttachSite(SiteModel site)
    {
        site.PropertyChanged += Site_PropertyChanged;
    }

    private void Site_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SiteModel.IsSelected))
        {
            UpdateSelectAllState();
        }
    }

    private void UpdateSelectAllState()
    {
        if (Sites.Count == 0) return;

        _isAllSelected = Sites.All(x => x.IsSelected);
        OnPropertyChanged(nameof(IsAllSelected));
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    private void OpenEditWindow(EditMode mode, SiteModel site)
    {
        var vm = new AddSiteViewModel(mode, site);

        var win = new PublishDesk.AddSiteWindow(mode, site)
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };

        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.DialogResult))
            {
                if (vm.DialogResult == true)
                {
                    // Add 模式才新增
                    if (mode == EditMode.Add)
                    {
                        var newSite = new SiteModel
                        {
                            Name = vm.Name,
                            SitePath = vm.SitePath,
                            FilePath = vm.FilePath
                        };

                        Sites.Add(newSite);
                        AttachSite(newSite);

                        AdddLog(new LogItem() { Message = $"[{DateTime.Now:HH:mm:ss}] 添加站点 {newSite.Name}" });
                    }
                    else
                    {
                        AdddLog(new LogItem() { Message = $"[{DateTime.Now:HH:mm:ss}] 编辑站点 {site.Name}" });
                    }

                    win.DialogResult = true;
                    win.Close();
                }
                else
                {
                    win.Close();
                }
            }
        };

        win.ShowDialog();
    }


    #endregion
}