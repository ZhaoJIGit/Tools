using HandyControl.Interactivity;
using Microsoft.Web.Administration;
using Newtonsoft.Json;
using PublishTool.Models;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace PublishTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const string JsonFile = "SiteData.json";
        private List<SiteModel> _list = new List<SiteModel>();
        private SiteModel _selectedSite;

        private double _progress;

        private string _progressText;

        public SiteModel SelectedSite
        {
            get => _selectedSite;
            set
            {
                _selectedSite = value;
                OnPropertyChanged();
            }
        }
        private string _currentFile;

        public string CurrentFile
        {
            get => _currentFile;
            set
            {
                _currentFile = value;

                OnPropertyChanged(nameof(CurrentFile));
            }
        }
        private bool _isPublishing;
        public double Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged();
            }
        }

        public string ProgressText
        {
            get => _progressText;
            set
            {
                _progressText = value;
                OnPropertyChanged();
            }
        }
        private string _logText;

        public string LogText
        {
            get => _logText;
            set
            {
                _logText = value;
                OnPropertyChanged();
            }
        }
        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            LoadSiteList();
        }
        /// <summary>
        /// 添加日志
        /// </summary>
        private void AddLog(
     string message,
     LogType type = LogType.Info)
        {
            Dispatcher.Invoke(() =>
            {
                Brush color = Brushes.White;

                switch (type)
                {
                    case LogType.Success:
                        color = Brushes.LimeGreen;
                        break;

                    case LogType.Warning:
                        color = Brushes.Orange;
                        break;

                    case LogType.Error:
                        color = Brushes.Red;
                        break;

                    default:
                        color = Brushes.Black;
                        break;
                }

                Paragraph paragraph = new Paragraph();

                Run run = new Run(
                    $"[{DateTime.Now:HH:mm:ss}] {message}");

                run.Foreground = color;

                paragraph.Margin = new Thickness(0);

                paragraph.Inlines.Add(run);

                LogRichTextBox.Document.Blocks.Add(paragraph);

                // 自动滚到底部
                LogRichTextBox.ScrollToEnd();
            });
        }
        /// <summary>
        /// 加载站点列表
        /// </summary>
        private void LoadSiteList()
        {
            try
            {
                if (!File.Exists(JsonFile))
                {
                    return;
                }

                string json = File.ReadAllText(JsonFile);

               _list =
                    JsonConvert.DeserializeObject<List<SiteModel>>(json);

                SiteListBox.ItemsSource = _list;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 左侧选择
        /// </summary>
        private void SiteListBox_SelectionChanged(
            object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (SiteListBox.SelectedItem is SiteModel site)
            {
                SelectedSite = site;
            }
        }

        /// <summary>
        /// 发布
        /// </summary>
        private async void Publish_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSite == null)
            {
                MessageBox.Show("请选择站点");

                return;
            }

            try
            {
                LogText = "";

                Progress = 0;

                AddLog("开始发布");

                ProgressText = "正在停止 IIS 应用池...";

                AddLog("停止 IIS 应用池");

                await StopAppPool(SelectedSite.Name);

                //KillAppPoolProcess(SelectedSite.Name);
                Progress = 20;

                AddLog("应用池已停止");

                ProgressText = "正在复制文件...";

                AddLog("开始复制文件");

                await CopyFiles();

                Progress = 80;

                AddLog("文件复制完成");

                ProgressText = "正在启动 IIS 应用池...";

                AddLog("启动 IIS 应用池");

                await StartAppPool(SelectedSite.Name);

                Progress = 100;

                ProgressText = "发布完成";

                AddLog("发布完成",LogType.Success);

                MessageBox.Show("发布成功");
            }
            catch (Exception ex)
            {
                ProgressText = "发布失败";

                AddLog($"发布失败：{ex.Message}", LogType.Error);

                MessageBox.Show(ex.Message);
            }
        }
        private async void BatchPublish_Click(
    object sender,
    RoutedEventArgs e)
        {
            var selectedSites =
                _list.Where(x => x.IsSelected).ToList();

            if (!selectedSites.Any())
            {
                MessageBox.Show("请选择站点");
                return;
            }
            if (_isPublishing)
            {
                MessageBox.Show("正在发布中...");
                return;
            }
            try
            {
                _isPublishing = true;

                foreach (var site in selectedSites)
                {
                    SelectedSite = site;

                    AddLog("");
                    AddLog("=================================");
                    AddLog($"开始发布站点：{site.Name}", LogType.Warning);
                    AddLog("=================================");

                    Progress = 0;

                    await PublishSite(site);

                    AddLog($"站点发布完成：{site.Name}", LogType.Success);
                }

                MessageBox.Show("全部发布完成");
            }
            catch (Exception ex)
            {
                AddLog(ex.Message);

                MessageBox.Show(ex.Message);
            }
            finally
            {
                _isPublishing = false;
            }
        }
        #region IIS
        private async Task PublishSite(SiteModel site)
        {
            ProgressText = $"正在停止：{site.Name}";

            await StopAppPool(site.Name);

            Progress = 10;

            AddLog("应用程序池已停止");

            await CopyFilesAsync(
                site.FilePath,
                site.SitePath);

            Progress = 90;

            AddLog("文件复制完成", LogType.Success);

            await StartAppPool(site.Name);

            Progress = 100;

            ProgressText = "发布完成";

            AddLog("应用程序池启动成功", LogType.Success);
        }
        /// <summary>
        /// 停止应用池
        /// </summary>
        private async Task StopAppPoolPS(string poolName)
        {
            await Task.Run(() =>
            {
                string script = $@"
Import-Module WebAdministration

$state = (Get-WebAppPoolState -Name '{poolName}').Value

if ($state -eq 'Started')
{{
    Stop-WebAppPool -Name '{poolName}'
}}
";

                ExecutePowerShell(script);
            });
        }

        /// <summary>
        /// 启动应用池
        /// </summary>
        private async Task StartAppPoolPS(string poolName)
        {
            await Task.Run(() =>
            {
                string script = $@"
Import-Module WebAdministration

$state = (Get-WebAppPoolState -Name '{poolName}').Value

if ($state -ne 'Started')
{{
    Start-WebAppPool -Name '{poolName}'
}}
";

                ExecutePowerShell(script);
            });
        }
        private async Task StopAppPoolOld(string poolName)
        {
            await Task.Run(() =>
            {
                using (ServerManager serverManager = new ServerManager())
                {
                    var pool = serverManager.ApplicationPools[poolName];

                    if (pool == null)
                    {
                        throw new Exception($"应用程序池不存在：{poolName}");
                    }

                    AddLog($"当前应用程序池状态：{pool.State}");

                    if (pool.State == ObjectState.Started)
                    {
                        pool.Stop();

                        AddLog("应用程序池已停止");
                    }
                    else
                    {
                        AddLog("应用程序池已经是停止状态");
                    }
                }
            });
        }
        private async Task StartAppPool(string poolName)
        {
            await Task.Run(() =>
            {
                using (ServerManager serverManager = new ServerManager())
                {
                    var pool = serverManager.ApplicationPools[poolName];

                    if (pool == null)
                    {
                        throw new Exception($"应用程序池不存在：{poolName}");
                    }

                    AddLog($"当前应用程序池状态：{pool.State}",LogType.Warning);

                    if (pool.State != ObjectState.Started)
                    {
                        pool.Start();

                        AddLog("应用程序池已启动", LogType.Success);
                        AddLog($"当前应用程序池状态：{pool.State}", LogType.Warning);
                    }
                    else
                    {
                        AddLog("应用程序池已经是启动状态");
                    }
                }
            });
        }
        private async Task StopAppPool(string poolName)
        {
            await Task.Run(async () =>
            {
                using (ServerManager serverManager = new ServerManager())
                {
                    var pool = serverManager.ApplicationPools[poolName];

                    if (pool == null)
                    {
                        throw new Exception($"应用程序池不存在：{poolName}");
                    }

                    AddLog($"当前状态：{pool.State}", LogType.Warning);

                    if (pool.State == ObjectState.Started)
                    {
                        AddLog("正在停止应用程序池...");

                        pool.Stop();

                        // 等待真正停止
                        int retry = 0;

                        while (pool.State != ObjectState.Stopped)
                        {
                            await Task.Delay(1000);

                            retry++;

                            pool = serverManager.ApplicationPools[poolName];

                            AddLog($"等待应用程序池停止...({retry})");

                            if (retry >= 30)
                            {
                                AddLog("应用程序池停止超时", LogType.Error);
                                throw new Exception($"应用程序池停止超时");
                                //KillAppPoolProcess(poolName);
                            }
                        }

                        AddLog("应用程序池已停止");
                    }
                    else
                    {
                        AddLog("应用程序池已经停止");
                    }
                }

                // 再额外等待2秒
                await Task.Delay(2000);
                 
            });
        }
        #endregion
        private void KillAppPoolProcess(string poolName)
        {
            using (ServerManager serverManager = new ServerManager())
            {
                var workerProcesses = serverManager.WorkerProcesses;

                foreach (WorkerProcess worker in workerProcesses)
                {
                    if (worker.AppPoolName.Equals(poolName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            Process process =
                                Process.GetProcessById(worker.ProcessId);

                            AddLog($"结束进程 PID：{worker.ProcessId}");

                            process.Kill(true);

                            AddLog("进程已结束");
                        }
                        catch (Exception ex)
                        {
                            AddLog(ex.Message);
                        }
                    }
                }
            }
        }
        #region 文件复制

        /// <summary>
        /// 复制文件
        /// </summary>
        private async Task CopyFiles()
        {
            await Task.Run(() =>
            {
                CopyDirectory(
                    SelectedSite.FilePath,
                    SelectedSite.SitePath);
            });
        }
        private async Task CopyFilesAsync(
    string sourceDir,
    string targetDir)
        {
            await Task.Run(() =>
            {
                if (!Directory.Exists(sourceDir))
                {
                    throw new Exception($"源目录不存在：{sourceDir}");
                }

                var files = Directory.GetFiles(
                    sourceDir,
                    "*",
                    SearchOption.AllDirectories);

                int total = files.Length;

                int current = 0;

                foreach (var file in files)
                {
                    current++;

                    // 相对路径
                    string relativePath =
                        Path.GetRelativePath(sourceDir, file);

                    // 目标文件
                    string targetFile =
                        Path.Combine(targetDir, relativePath);

                    // 目标目录
                    string targetFolder =
                        Path.GetDirectoryName(targetFile);

                    // 创建目录
                    if (!Directory.Exists(targetFolder))
                    {
                        Directory.CreateDirectory(targetFolder);

                        AddLog($"创建目录：{targetFolder}");
                    }

                    // 更新UI
                    Dispatcher.Invoke(() =>
                    {
                        CurrentFile = relativePath;

                        Progress =
                            10 + ((double)current / total * 80);

                        ProgressText =
                            $"正在复制文件 ({current}/{total})";
                    });

                    // 复制文件
                    CopyFileWithRetry(file, targetFile);

                    AddLog($"复制文件：{relativePath}");
                }
            });
        }
        private void CopyFileWithRetry(
    string sourceFile,
    string targetFile,
    int retryCount = 3)
        {
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    File.Copy(sourceFile, targetFile, true);

                    return;
                }
                catch (IOException)
                {
                    if (i == retryCount - 1)
                    {
                        throw;
                    }

                    AddLog($"文件占用重试：{Path.GetFileName(targetFile)}");

                    Thread.Sleep(1000);
                }
            }
        }
        /// <summary>
        /// 递归复制目录
        /// </summary>
        private void CopyDirectory(string sourceDir, string targetDir)
        {
            if (!Directory.Exists(sourceDir))
            {
                throw new Exception($"发布目录不存在：{sourceDir}");
            }

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);

                AddLog($"创建目录：{targetDir}");
            }

            // 文件
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);

                string targetFile =
                    Path.Combine(targetDir, fileName);

                File.Copy(file, targetFile, true);

                AddLog($"复制文件：{fileName}");
            }

            // 子目录
            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(dir);

                string targetSubDir =
                    Path.Combine(targetDir, dirName);

                CopyDirectory(dir, targetSubDir);
            }
        }

        #endregion

        #region PowerShell

        /// <summary>
        /// 执行 PowerShell
        /// </summary>
        private async Task ExecutePowerShell(string command)
        {
            await Task.Run(() =>
            {
                using Process process = new Process();

                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments =
                        $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process.Start();

                string output =
                    process.StandardOutput.ReadToEnd();

                string error =
                    process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (!string.IsNullOrWhiteSpace(error))
                {
                    throw new Exception(error);
                }
            });
        }

        #endregion

        #region Notify

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(name));
        }

        #endregion

        private void AddSite_Click(object sender, RoutedEventArgs e)
        {
            var win = new AddSiteWindow(_list);
            win.Owner = this;

            if (win.ShowDialog() == true)
            {
                LoadSiteList();
            }
        }
        private void SiteListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (SiteListBox.SelectedItem is SiteModel site)
            {
                OpenEditWindow(site);
            }
        }
        
        private void EditSite_Click(object sender, RoutedEventArgs e)
        {
            if (SiteListBox.SelectedItem is SiteModel site)
            {
                OpenEditWindow(site);
            }
        }
        private void DeleteSite_Click(object sender, RoutedEventArgs e)
        {
            if (SiteListBox.SelectedItem is not SiteModel site)
                return;

            if (MessageBox.Show(
                $"确认删除 {site.Name} ?",
                "提示",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            var list = SiteListBox.ItemsSource as List<SiteModel>;
            if (list == null) return;

            list.Remove(site);

            SaveList(list);
            LoadSiteList();
        }
        private void SelectAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in _list)
            {
                item.IsSelected = true;
            }

            SiteListBox.Items.Refresh();
        }
        private void SelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var item in _list)
            {
                item.IsSelected = false;
            }

            SiteListBox.Items.Refresh();
        }
        private void SaveList(List<SiteModel> list)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(list, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(JsonFile, json);
        }
        private void OpenEditWindow(SiteModel model)
        {
            var win = new AddSiteWindow(_list, _selectedSite);
            win.Owner = this;

            if (win.ShowDialog() == true)
            {
                LoadSiteList();
            }
        }
    }


}