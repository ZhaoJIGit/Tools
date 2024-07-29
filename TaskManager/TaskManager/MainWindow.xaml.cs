using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TaskManager
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        private string cacheDirectory;
        private const string JsonFileName = "TaskGroup.json";
        ObservableCollection<ProcessInfo> processInfos = new ObservableCollection<ProcessInfo>();
        ObservableCollection<TaskGroupInfo> taskGroups = new ObservableCollection<TaskGroupInfo>();
        private TaskGroupInfo CurrentGroup = null;
        public event PropertyChangedEventHandler PropertyChanged;
        public string ExecutTime = "耗时：0 s";
        private SynchronizationContext syncContext;
        private static Dictionary<int, string> CommandLines = new Dictionary<int, string>();
        public MainWindow()
        {
            InitializeComponent();
            cacheDirectory = Path.Combine(Path.GetTempPath(), "TaskGroup", "Cache");
            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }
            syncContext = SynchronizationContext.Current;
            txtExecutionTime.Text = "耗时：0 s";

        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 窗口加载完成后的操作
            LoadTaskGroupJson();
        }
        //private void LoadProcessNames()
        //{
        //    // 从 JSON 文件加载保存的进程名称集合
        //    if (File.Exists(JsonFileName))
        //    {
        //        string json = File.ReadAllText(JsonFileName);
        //        processNames = JsonConvert.DeserializeObject<List<string>>(json);
        //    }
        //    else
        //    {
        //        processNames = new List<string>();
        //    }
        //}
        private void LoadTaskGroupJson()
        {
            if (File.Exists(Path.Combine(cacheDirectory, JsonFileName)))
            {
                string json = File.ReadAllText(Path.Combine(cacheDirectory, JsonFileName));
                taskGroups = JsonConvert.DeserializeObject<ObservableCollection<TaskGroupInfo>>(json);
            }
            if (taskGroups != null && taskGroups.Count > 0)
            {
                lvNames.ItemsSource = taskGroups;
                // 假设默认选中第一行
                //lvNames.SelectedIndex = 0;
                //GetProcess(taskGroups.First().TaskGroup);
            }
        }
        private async void btnTest_Click(object sender, RoutedEventArgs e) {
            await Excute();
        }
        private async Task Excute() {
            await Task.Delay(5000);
        }
        private void btnFindProcesses_Click(object sender, RoutedEventArgs e)
        {
            string searchName = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(searchName))
            {
                MessageBox.Show("请输入要查找的名称.");
                return;
            }
            var item = taskGroups.Where(i => i.TaskGroup.ToLower() == searchName.ToLower()).FirstOrDefault();
            if (item == null)
            {
                item = new TaskGroupInfo() { TaskGroup = searchName };
                taskGroups.Add(item);
                lvNames.ItemsSource = taskGroups;

                SaveTaskGroupToJson();
            }
            lvNames.SelectedIndex = taskGroups.IndexOf(item);
            CurrentGroup = item;
            GetProcess(searchName);
        }
        private void SaveTaskGroupToJson()
        {
            string json = JsonConvert.SerializeObject(taskGroups);

            File.WriteAllText(Path.Combine(cacheDirectory, JsonFileName), json);
        }

        private async void GetProcess(string searchName)
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
            await Task.Run(() => ProcessDotnetProcessesInBatchesAsync(searchName));

            syncContext.Post(_ => lvProcesses.ItemsSource = processInfos, null);
            //Dispatcher.Invoke(() => HideMask());
            await Task.Run(() => HideMask());
            // 停止计时
            stopwatch.Stop();

            // 获取经过的时间
            TimeSpan elapsedTime = stopwatch.Elapsed;
            txtExecutionTime.Text = $@"耗时：{(int)elapsedTime.TotalSeconds} s";
        }

        async Task ProcessDotnetProcessesInBatchesAsync(string searchName)
        {
            //Process[] processes = Process.GetProcesses();

            Console.WriteLine("正在运行的 dotnet.exe 进程：");

            Process[] processes = Process.GetProcessesByName("dotnet");
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
                    if (string.Equals(process.ProcessName, "dotnet", StringComparison.OrdinalIgnoreCase))
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
        private void btnCloseProcess_Click(object sender, RoutedEventArgs e)
        {
            //if (lvProcesses.SelectedItem == null)
            //{
            //    MessageBox.Show("请选择要关闭的进程.");
            //    return;
            //}
            // 关闭选定的进程
            var selectedProcesses = processInfos.Where(p => p.IsSelected).ToList();

            if (selectedProcesses == null || selectedProcesses.Count == 0)
            {
                MessageBox.Show("请选择要关闭的任务.");
                return;
            }
            List<int> unkill = new List<int>();
            foreach (var process in selectedProcesses)
            {
                try
                {
                    // 执行关闭进程的操作
                    // 查找并关闭选定的进程
                    Process processesToClose = Process.GetProcessById(process.ProcessId);
                    if (processesToClose != null)
                    {
                        processesToClose.Kill();
                        processInfos.Remove(process);
                    }
                }
                catch (Exception)
                {
                    unkill.Add(process.ProcessId);

                }
            }
            if (unkill.Count > 0)
            {
                MessageBox.Show($"进程{string.Join(",", unkill)}:未运行");
            }
            else
            {
                MessageBox.Show($"已关闭选中进程");
            }
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
 
        private void lvProcesses_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // 获取选中的项
            var selectedProcess = lvProcesses.SelectedItem as ProcessInfo;
            if (selectedProcess != null)
            {
                using (Process process = Process.GetProcessById(selectedProcess.ProcessId))
                {
                    // 获取标准输出流（如果进程已重定向）
                    StreamReader outputReader = process.StandardOutput;
                    StreamReader errorReader = process.StandardError;

                    // 读取输出流
                    string output = outputReader.ReadToEnd();
                    Console.WriteLine("标准输出:");
                    Console.WriteLine(output);

                    // 读取错误流
                    string error = errorReader.ReadToEnd();
                    if (!string.IsNullOrEmpty(error))
                    {
                        Console.WriteLine("标准错误:");
                        Console.WriteLine(error);
                    }
                }
                //Process process = Process.GetProcessById(selectedProcess.ProcessId);
                //// 获取进程的命令行参数
                //string commandLine = process.MainModule.FileName;
                //string arguments = process.StartInfo.Arguments;
                //// 检查进程是否已经启动，并重定向输出
                //if (!process.StartInfo.RedirectStandardOutput || !process.StartInfo.RedirectStandardError)
                //{
                //    Console.WriteLine("进程没有重定向输出。");
                //    return;
                //}

                //// 获取标准输出和标准错误
                //process.OutputDataReceived += new DataReceivedEventHandler((sender, e) => {
                //    if (!string.IsNullOrEmpty(e.Data))
                //    {
                //        MessageBox.Show($"标准输出：{e.Data}");
                //    }
                //});

                //process.ErrorDataReceived += new DataReceivedEventHandler((sender, e) => {
                //    if (!string.IsNullOrEmpty(e.Data))
                //    {
                //        MessageBox.Show($"错误输出：{e.Data}");
                //    }
                //});

                //// 启动异步读取
                //process.BeginOutputReadLine();
                //process.BeginErrorReadLine();

                //// 等待进程退出
                //process.WaitForExit();
                // 打开选中项对应的进程窗口
                //IntPtr mainWindowHandle = FindMainWindowHandle(selectedProcess.ProcessId);
                //if (mainWindowHandle != IntPtr.Zero)
                //{
                //    SetForegroundWindow(mainWindowHandle);
                //}
                //else
                //{
                //    //MessageBox.Show($"{output}{error}");
                //}
            }
        }
        private void lvProcesses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 在这里处理 ListView 中项的选择变化，如果需要的话
        }

        private void lvNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {           // 获取选中的项

        }

        private void lvNames_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var task = lvNames.SelectedItem as TaskGroupInfo;
            if (task != null)
            {
                CurrentGroup = task;
                GetProcess(task.TaskGroup);
            }
        }
        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var task = lvNames.SelectedItem as TaskGroupInfo;
            if (task == null) { MessageBox.Show($"群组不存在"); return; }
            MessageBoxResult result = MessageBox.Show("确定要删除此群组吗？", "确认操作", MessageBoxButton.OKCancel, MessageBoxImage.Question);

            if (result == MessageBoxResult.OK)
            {
                taskGroups.Remove(task);
                if (CurrentGroup != null)
                {
                    if (task.TaskGroup == CurrentGroup.TaskGroup)
                    {
                        processInfos.Clear();
                        CurrentGroup = null;
                    }
                }

                SaveTaskGroupToJson();
            }
        }
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
        private IntPtr FindMainWindowHandle(int processId)
        {
            Process[] processes = Process.GetProcessesByName("dotnet");
            foreach (Process proc in processes)
            {
                if (proc.Id == processId)
                {
                    return proc.MainWindowHandle;
                }
            }
            return IntPtr.Zero;
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
        private const int SW_RESTORE = 9;
        private IntPtr GetProcessMainWindowHandle(int processId)
        {
            Process process = Process.GetProcessById(processId);
            return process.MainWindowHandle;
        }

        #endregion


    }
}
