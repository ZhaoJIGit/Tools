using Notes.APP.Common;
using Notes.APP.Models;
using Notes.APP.Pages;
using Notes.APP.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static Notes.APP.App;

namespace Notes.APP
{
    /// <summary>
    /// ListWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ListWindow : Window
    {
        private Point _mouseDownPosition;
        private bool _isDrawerOpen = false;
        private bool isLoad = false;
        private const int WM_COPYDATA = 0x004A;
        SystemConfigInfo SystemConfigInfo { get; set; }
        // 定义静态事件
        public static event EventHandler RefreshEvent;
        public ListWindow()
        {
            InitializeComponent();
            SystemConfigInfoService systemConfig = SystemConfigInfoService.Instance;
            var config = systemConfig.GetConfig();
            SystemConfigInfo = config;
            this.DataContext = SystemConfigInfo;

            SourceInitialized += MainWindow_SourceInitialized;
            // 默认显示 Page1
            ListFrame.Navigate(new ListPage());
            //todo 记录当前窗口大小


        }
        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source.AddHook(WndProc);
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // 启动嵌入桌面 + 自动监控
            //DesktopEmbedder.StartEmbedding(this);
        }
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            var service = LogService.Instance;
            if (msg == WM_COPYDATA)
            {
                //COPYDATASTRUCT cds = (COPYDATASTRUCT)Marshal.PtrToStructure(lParam, typeof(COPYDATASTRUCT));
                //string receivedMessage = Marshal.PtrToStringAnsi(cds.lpData, cds.cbData);
                //if (receivedMessage == "SHOW")
                //{

                //    RestoreWindow();
                //}

                handled = true;
            }
            return IntPtr.Zero;
        }

        public void RestoreWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
            this.Topmost = true;
            this.Topmost = false;
            this.Focus();
        }
        private void MainWindow_ReloadWindow(object sender, EventArgs e)
        {
            ReloadPage();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MainWindow.ReloadWindow += MainWindow_ReloadWindow;

            // 绑定数据源
            isLoad = true;

         
            isOpenRunBox.IsChecked = SystemConfigInfo.StartOpen;
            var noteService = new NoteService();
            var list = noteService.GetNotes();
            //标记完成的不在自动打开页面
            foreach (var item in list.Where(i => !i.IsDeleted && i.Fixed))
            {
                MainWindow mainWindow = new MainWindow(item);
                mainWindow.Tag = item.NoteId;
                mainWindow.Show();
            }
            isLoad = false;
            if (list.Count > 0)
            {
                this.Hide();
            }

        }
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var note = NoteModel.CreateNote();

            MainWindow mainWindow = new MainWindow(note);
            mainWindow.Tag = note.NoteId;
            mainWindow.Height = note.Height;
            mainWindow.Width = note.Width;
            mainWindow.Show();
            ReloadPage();

        }

        private DateTime _lastClickTime = DateTime.MinValue;
        private const int DoubleClickTimeLimit = 1000; // 双击时间限制，单位是毫秒
        private void TrayIcon_TrayLeftMouseUp(object sender, RoutedEventArgs e)
        {
            //var currentTime = DateTime.Now;
            //var timeDifference = currentTime - _lastClickTime;
            //if (timeDifference.TotalMilliseconds <= DoubleClickTimeLimit)
            //{
            //    // 双击事件
            //    this.Show();
            //    this.WindowState = WindowState.Normal;
            //    this.Activate();
            //}
            //// 更新上次点击时间
            //_lastClickTime = currentTime;
            var windows = Application.Current.Windows.OfType<Window>().Where(i => i.Name == "NoteDetail");
            foreach (var win in windows)
            {
                win.Activate();
                win.WindowState = WindowState.Normal;
                win.Show();
            }

        }
        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            var windows = Application.Current.Windows.OfType<Window>().FirstOrDefault(i => i.Tag == "NoteSetting");
            if (windows != null)
            {
                windows.Show();
                windows.Activate();
            }
            else
            {
                SettingWindow setting = new SettingWindow();
                setting.Tag = "NoteSetting";
                setting.Show();
            }
            //// 根据当前状态切换抽屉的打开或关闭
            //if (_isDrawerOpen)
            //{
            //    // 关闭抽屉
            //    DrawerPanel.Visibility = Visibility.Collapsed;
            //    CloseArea.Visibility = Visibility.Collapsed;  // 隐藏遮罩层
            //}
            //else
            //{
            //    // 打开抽屉
            //    DrawerPanel.Visibility = Visibility.Visible;
            //    CloseArea.Visibility = Visibility.Visible;  // 显示遮罩层
            //}

            //// 切换状态
            //_isDrawerOpen = !_isDrawerOpen;
        }
        // 触发事件
        public static void TriggerRefresh()
        {
            RefreshEvent?.Invoke(null, EventArgs.Empty);
        }
        // INotifyPropertyChanged 实现
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        // 最小化窗口
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // 关闭窗口
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide(); // 隐藏窗口
        }

        // 实现窗口拖动
        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (e.ChangedButton == MouseButton.Left)
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    _mouseDownPosition = e.GetPosition(this);
                }
                //this.DragMove(); // 拖动窗口
            }
            e.Handled = true;
        }
        private void OnTrayOpenClick(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }
        private void OnTrayExitClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Hand; // 设置鼠标指针为十字箭头
        }

        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow; // 恢复默认箭头指针
        }

        private void CloseArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 关闭抽屉
            CloseArea.Visibility = Visibility.Collapsed;  // 隐藏遮罩层
            DrawerPanel.Visibility = Visibility.Collapsed;

            _isDrawerOpen = false;
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    // 获取当前鼠标位置
                    Point currentPosition = e.GetPosition(this);
                    // 判断鼠标移动距离（避免误触）
                    if (Math.Abs(currentPosition.X - _mouseDownPosition.X) > 10 ||
                        Math.Abs(currentPosition.Y - _mouseDownPosition.Y) > 10)
                    {
                        // 退出全屏，并调整窗口位置
                        if (this.WindowState == WindowState.Maximized)
                        {
                            this.WindowState = WindowState.Normal;

                            // 计算鼠标相对位置，保持窗口位置不跳变
                            this.Top = Math.Max(0, currentPosition.Y - 25);
                            this.Left = Math.Max(0, currentPosition.X - (this.Width / 2));
                        }
                        // 执行拖拽
                        this.DragMove();
                    }
                }
                else
                {
                    // 执行拖拽
                    this.DragMove();
                }
            }
        }

        private void ResizeHandle_DragDelta(object sender, DragDeltaEventArgs e)
        {
            // 获取当前窗口
            var window = this;

            // 调整窗口的宽度和高度
            window.Width = Math.Max(window.MinWidth, window.Width + e.HorizontalChange);
            window.Height = Math.Max(window.MinHeight, window.Height + e.VerticalChange);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            ReloadPage();
        }
        private void ReloadPage()
        {
            ListWindow.TriggerRefresh();
            // 获取当前的 Frame
            //ListFrame.Navigate(null); // 先清空 Frame 内容
            //ListFrame.Navigate(new ListPage()); // 再重新加载页面
        }
        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isOpenRunBox.IsChecked = !isOpenRunBox.IsChecked;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (isOpenRunBox.IsChecked.Value && !isLoad)
            {
                SystemConfigInfoService systemConfig = SystemConfigInfoService.Instance;
                SystemConfigInfo.StartOpen = isOpenRunBox.IsChecked.Value;
                systemConfig.SaveConfig(SystemConfigInfo);
                // 当勾选框被选中时触发
                StartupManager.EnableAutoStartup();
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isOpenRunBox.IsChecked.Value && !isLoad)
            {
                SystemConfigInfoService systemConfig = SystemConfigInfoService.Instance;
                SystemConfigInfo.StartOpen = !isOpenRunBox.IsChecked.Value;
                systemConfig.SaveConfig(SystemConfigInfo);
                // 当勾选框被取消选中时触发
                StartupManager.DisableAutoStartup();
            }
        }

        private void Fixed_Click(object sender, RoutedEventArgs e)
        {
            var hostWindow = Window.GetWindow(this);
            if (hostWindow == null)
                return;
       
            // 2. 切换 Topmost
            hostWindow.Topmost = !hostWindow.Topmost;
            SystemConfigInfoService systemConfig = SystemConfigInfoService.Instance;
            SystemConfigInfo.Fixed = hostWindow.Topmost;
            systemConfig.SaveConfig(SystemConfigInfo);
            if (hostWindow.Topmost)
            {
                btnFixed.Content = "\uE840";
            }
            else
            {
                btnFixed.Content = "\uE718";
            }
            //todo 需要写入设置信息里面
        }
    }
}
