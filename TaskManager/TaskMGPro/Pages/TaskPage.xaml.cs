using OxyPlot.Series;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using TaskMGPro.Common;
using TaskMGPro.Helper;
using TaskMGPro.Models;
using TaskMGPro.Services;
using OxyPlot.Axes;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Management;
using System.IO;


namespace TaskMGPro.Pages
{
    /// <summary>
    /// HomePage.xaml 的交互逻辑
    /// </summary>
    public partial class TaskPage : BasePage
    {
        private Random _random;
        private DispatcherTimer _timer;
        public ObservableCollection<double> Values { get; set; } = new ObservableCollection<double>();
        public ObservableCollection<LogInfo> Logs { get; set; } = new ObservableCollection<LogInfo>();

        ObservableCollection<ProcessInfo> processInfos = new ObservableCollection<ProcessInfo>();
        ObservableCollection<GroupInfo> taskGroups = new ObservableCollection<GroupInfo>();
        private GroupInfo CurrentGroup = null;
        public event PropertyChangedEventHandler PropertyChanged;
        public string ExecutTime = "耗时：0 s";
        private SynchronizationContext syncContext;
        private static Dictionary<int, string> CommandLines = new Dictionary<int, string>();

        public TaskPage(GroupInfo currentGroup)
        {
            CurrentGroup = currentGroup;
            InitializeComponent();
            syncContext = SynchronizationContext.Current;
            RefreshData();
            InitProcess(currentGroup.Title, 0);
            
        }
        private void InitProcess(string title, int processId)
        {
            //_random = new Random();
            if (PlotModel != null)
            {
                PlotView.Model.Title = title;
                LineSeries.Points.Clear();
                Values.Clear();
                // 通知图表重绘
                PlotView.Model.InvalidatePlot(true);
            }
            else
            {
                // 初始化 PlotModel
                PlotModel = new PlotModel { Title = title };
                // 初始化 LineSeries
                LineSeries = new LineSeries();
                PlotModel.Series.Add(LineSeries); // 确保 LineSeries 在 PlotModel 中

                // 设置坐标轴
                var xAxis = new LinearAxis
                {
                    Title = "/秒",
                    Position = AxisPosition.Bottom,
                    MajorGridlineStyle = LineStyle.Solid,
                    IsAxisVisible = true,
                    IsZoomEnabled = false // 禁用 X 轴缩放
                };
                var yAxis = new LinearAxis
                {
                    Title = "Mb",
                    Position = AxisPosition.Left,
                    MajorGridlineStyle = LineStyle.Solid,
                    IsAxisVisible = true,
                    IsZoomEnabled = false, // 禁用 X 轴缩放
                    //Minimum = 0, // 固定 Y 轴的最小值
                    //Maximum = 100 // 固定 Y 轴的最大值
                };

                // 将坐标轴添加到模型中
                PlotModel.Axes.Add(xAxis);
                PlotModel.Axes.Add(yAxis);
                Values = new ObservableCollection<double>();
            }

            DataContext = this;
            if (processId > 0)
            {
                Task.Run(() => GetProcessData(processId));
            }
            //_timer = new DispatcherTimer
            //{
            //    Interval = TimeSpan.FromSeconds(1)
            //};
            //_timer.Tick += UpdateData;
            //_timer.Start();

        }

        public PlotModel PlotModel { get; set; }


        public LineSeries LineSeries { get; set; }

        private void UpdateMemory(double value)
        {
            Values.Add(value);
            LineSeries.Points.Add(new DataPoint(Values.Count - 1, value));

            if (Values.Count > 10)
            {
                Values.RemoveAt(0);
                LineSeries.Points.RemoveAt(0);

                // 更新每个点的 X 值
                for (int i = 0; i < LineSeries.Points.Count; i++)
                {
                    LineSeries.Points[i] = new DataPoint(i, LineSeries.Points[i].Y);
                }
            }

            // 确保在更新后无效化图表
            PlotModel.InvalidatePlot(true);

        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.Background = BackgroundColor.ToBrushColor();
        }

        private void BtnGroup_Click(object sender, RoutedEventArgs e)
        {
            // 创建弹出框
            PopupWindow popup = new PopupWindow(new BaseWindow() { Height = this.Height, Width = this.Width });
            // 创建MyPage实例并传递参数
            GroupPage page = new GroupPage();
            page.PageClosed += MyPage_PageClosed;
            // 在弹窗中加载指定的Page
            popup.LoadPageWithParameters(page);
            // 显示弹出框
            var result = popup.ShowDialog();

        }
        private void MyPage_PageClosed(object? sender, PupupWindowEventArgs<bool> e)
        {
            if (e.Data)
            {
                RefreshData();
            }
        }
        private void RefreshData()
        {

            GetProcess();
        }

        private void listGroupData_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var process = lvProcesses.SelectedItem as ProcessInfo;
            if (process==null) { return; }
            InitProcess(process.TaskName, process.ProcessId);
            Logs.Clear();
            txtLog.Text = "";
            Logs = GetFiles(process.TaskName);
            txtLogList.ItemsSource = Logs;
        }
        private ObservableCollection<LogInfo> GetFiles(string keyword)
        {

            var files = Directory.GetFiles(CurrentGroup.LogAddress, "*.*", SearchOption.AllDirectories);
            ObservableCollection<LogInfo> result = new ObservableCollection<LogInfo>();
            foreach (var file in files)
            {
                // 获取文件名
                string fileName = System.IO.Path.GetFileName(file);
                var list = keyword.Split(" ");
                // 判断文件名是否包含关键字
                foreach (var item in list)
                {
                    if (string.IsNullOrWhiteSpace(item)) { continue; }
                    if (fileName.Contains(item, StringComparison.OrdinalIgnoreCase)) // 忽略大小写
                    {
                        var fileInfo = new FileInfo(file);
                        result.Add(new LogInfo() { Name = fileName, Path = file , UpdatedTime= fileInfo .LastWriteTime});
                    }
                    //result.Add(new LogInfo() { Name = fileName, Path = file });
                }
            }
            return result;
        }
        private void GetProcessData(int id)
        {
            // 查找指定名称的进程
            Process process = Process.GetProcessById(id);

            if (process == null)
            {
                Console.WriteLine($@"未找到【{id}】进程。");
                return;
            }

            // 打印进程基本信息
            Console.WriteLine("进程名称: " + process.ProcessName);
            Console.WriteLine("进程ID: " + process.Id);

            // CPU 时间
            TimeSpan cpuTime = process.TotalProcessorTime;
            Console.WriteLine("CPU 使用时间: " + cpuTime.TotalMilliseconds + " 毫秒");

            // 工作集（内存占用）
            long memoryUsage = process.WorkingSet64;
            Console.WriteLine("内存使用: " + memoryUsage / 1024 / 1024 + " MB");

            // 虚拟内存大小
            long virtualMemory = process.VirtualMemorySize64;
            Console.WriteLine("虚拟内存使用: " + virtualMemory / 1024 / 1024 + " MB");

            // 句柄数
            Console.WriteLine("句柄数: " + process.HandleCount);

            // 线程数
            Console.WriteLine("线程数: " + process.Threads.Count);

            // 优先级
            Console.WriteLine("优先级: " + process.BasePriority);

            // 持续监控，每秒刷新一次
            while (!process.HasExited)
            {
                Console.WriteLine("CPU 使用时间: " + process.TotalProcessorTime.TotalMilliseconds + " 毫秒");
                Console.WriteLine("内存使用: " + process.WorkingSet64 / 1024 / 1024 + " MB");
                UpdateMemory(process.WorkingSet64 / 1024 / 1024);
                System.Threading.Thread.Sleep(1000);  // 每秒刷新一次
            }
        }
        private void listGroupData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void chkSelectAll_Checked(object sender, RoutedEventArgs e)
        {
            // 全选复选框被选中时，设置所有项为选中状态
            foreach (var item in processInfos)
            {
                item.IsSelected = true;
            }
        }

        private void chkSelectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            // 全选复选框取消选中时，设置所有项为未选中状态
            foreach (var item in processInfos)
            {
                item.IsSelected = false;
            }
        }
        private void txtLogList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var log = txtLogList.SelectedItem as LogInfo;
            if (log == null) { return; }

            using (FileStream fs = new FileStream(log.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader reader = new StreamReader(fs))
                {
                    txtLog.Text = reader.ReadToEnd();
                }
            }
        }

        #region 获取进程
        private async void GetProcess()
        {
            // 显示遮罩和进度条
            // 显示遮罩层，确保在主线程上执行
            await Task.Run(() => ShowMask());
            // 创建 Stopwatch 实例
            Stopwatch stopwatch = new Stopwatch();

            // 开始计时
            stopwatch.Start();
            // 执行耗时操作，例如模拟一个耗时的任务
            Dispatcher.Invoke(() =>
            {
                processInfos.Clear();
            });
            await Task.Run(() => ProcessDotnetProcessesInBatchesAsync(CurrentGroup.Title));

            syncContext.Post(_ => lvProcesses.ItemsSource = processInfos, null);
            //Dispatcher.Invoke(() => HideMask());
            await Task.Run(() => HideMask());
            // 停止计时
            stopwatch.Stop();

            // 获取经过的时间
            TimeSpan elapsedTime = stopwatch.Elapsed;
        }

        async Task ProcessDotnetProcessesInBatchesAsync(string searchName)
        {
            //Process[] processes = Process.GetProcesses();

            Console.WriteLine("正在运行的 dotnet.exe 进程：");

            Process[] processes = Process.GetProcessesByName(CurrentGroup.Type);
            if (processes.Length > 0)
            {
                GetCommandLines();
            }
            // 拆分成多个批次
            var batches = SplitListIntoBatches(processes.ToList(), 5);

            // 使用多个线程处理每个批次
            //Parallel.ForEach(batches, batch =>
            //{
            //    Excute(batch, searchName);
            //});
            // 异步处理每个批次
            int i = 0;
            int size = 10;
            bool isRun = true;
            do
            {
                var tasks = batches.Skip(i * size).Take(size).Select(batch => Task.Run(() => Excute(batch, searchName)));
                if (tasks.Count() <= 0)
                {
                    isRun = false; break;
                }
                await Task.WhenAll(tasks);
                i++;
            } while (isRun);

            //Dispatcher.Invoke(() => lvProcesses.ItemsSource = processInfos);
        }
        void Excute(List<Process> processes, string searchName)
        {
            foreach (var process in processes)
            {
                try
                {

                    // 检查进程名称是否为 dotnet.exe
                    if (string.Equals(process.ProcessName, CurrentGroup.Type, StringComparison.OrdinalIgnoreCase))
                    {
                        // 异步获取命令行参数
                        string commandLine = GetCommandLine(process);

                        // 检查命令行参数是否包含特定的参数或标识
                        if (commandLine.IndexOf(searchName, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            // 使用线程安全的方式添加到集合中
                            lock (processInfos)
                            {
                                if (processInfos.Where(i => i.ProcessId == process.Id).Count() > 0) { continue; }
                                Dispatcher.Invoke(() =>
                                {
                                    processInfos.Add(new ProcessInfo() { ProcessId = process.Id, TaskGroup = searchName, TaskName = commandLine });
                                });

                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 捕获异常，例如访问权限不足或进程已终止
                    Console.WriteLine($"无法访问进程 {process.ProcessName}: {ex.Message}");
                }
                finally
                {
                }
            }
        }
        // 拆分列表的方法
        static List<List<T>> SplitListIntoBatches<T>(List<T> source, int batchSize)
        {
            var batches = new List<List<T>>();
            for (int i = 0; i < source.Count; i += batchSize)
            {
                batches.Add(source.Skip(i).Take(batchSize).ToList());
            }
            return batches;
        }
        // 获取进程的命令行参数
        private static string GetCommandLine(Process process)
        {
            if (CommandLines.Count > 0)
            {
                if (CommandLines.TryGetValue(process.Id, out string? commandline))
                {
                    return commandline;
                }
            }
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}"))
            {
                using (ManagementObjectCollection objects = searcher.Get())
                {
                    foreach (ManagementBaseObject obj in objects)
                    {
                        return obj["CommandLine"]?.ToString();
                    }
                }
            }
            return string.Empty;
        }
        private static void GetCommandLines()
        {
            // 一次性查询所有进程的命令行参数
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT ProcessId, CommandLine FROM Win32_Process"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    int processId = Convert.ToInt32(obj["ProcessId"]);
                    string commandLine = obj["CommandLine"]?.ToString();
                    CommandLines[processId] = commandLine;
                }
            }
        }
        #endregion
        // 显示遮罩层
        private void ShowMask()
        {
            Dispatcher.Invoke(() => maskBorder.Visibility = Visibility.Visible);
        }

        // 隐藏遮罩层
        private void HideMask()
        {
            Dispatcher.Invoke(() => maskBorder.Visibility = Visibility.Collapsed);
        }

        #region Windows API 调用

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);



        #endregion


    }


}
