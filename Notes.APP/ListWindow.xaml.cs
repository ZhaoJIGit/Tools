using Notes.APP.Common;
using Notes.APP.Models;
using Notes.APP.Pages;
using Notes.APP.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
        public ListWindow()
        {
            InitializeComponent();
            // 默认显示 Page1
            ListFrame.Navigate(new ListPage());

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
            isOpenRunBox.IsChecked = StartupManager.IsAutoStartupEnabled();
            var noteService = new NoteService();
            var list = noteService.GetNotes();
            foreach (var item in list.Where(i => i.Fixed))
            {
                MainWindow mainWindow = new MainWindow(item);
                mainWindow.Tag = item.NoteId;
                mainWindow.Show();
            }
            isLoad = false;

        }
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var note = NoteModel.CreateNote();
            MainWindow mainWindow = new MainWindow(note);
            mainWindow.Tag = note.NoteId;
            mainWindow.Show();
            ReloadPage();

        }
        private void TrayIcon_TrayLeftMouseUp(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            // 根据当前状态切换抽屉的打开或关闭
            if (_isDrawerOpen)
            {
                // 关闭抽屉
                DrawerPanel.Visibility = Visibility.Collapsed;
                CloseArea.Visibility = Visibility.Collapsed;  // 隐藏遮罩层
            }
            else
            {
                // 打开抽屉
                DrawerPanel.Visibility = Visibility.Visible;
                CloseArea.Visibility = Visibility.Visible;  // 显示遮罩层
            }

            // 切换状态
            _isDrawerOpen = !_isDrawerOpen;
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
            // 获取当前的 Frame
            ListFrame.Navigate(null); // 先清空 Frame 内容
            ListFrame.Navigate(new ListPage()); // 再重新加载页面
        }
        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isOpenRunBox.IsChecked = !isOpenRunBox.IsChecked;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (isOpenRunBox.IsChecked.Value && !isLoad)
            {
                // 当勾选框被选中时触发
                StartupManager.EnableAutoStartup();
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!isOpenRunBox.IsChecked.Value && !isLoad)
            {
                // 当勾选框被取消选中时触发
                StartupManager.DisableAutoStartup();
            }
        }
    }
}
