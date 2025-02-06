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
        private bool _isDrawerOpen = false;

        public ListWindow()
        {
            InitializeComponent();
            // 默认显示 Page1
            ListFrame.Navigate(new ListPage());
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var noteService = new NoteService();
            var list = noteService.GetNotes();
            foreach (var item in list)
            {
                MainWindow mainWindow = new MainWindow(item);
                mainWindow.Tag= item.NoteId;
                mainWindow.Show();
            }
           
        }
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var note = NoteModel.CreateNote();
            MainWindow mainWindow = new MainWindow(note);
            mainWindow.Tag = note.NoteId;
            mainWindow.Show();
        }
        private void TrayIcon_TrayLeftMouseUp(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StackPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }
        /// <summary>
        /// 查看列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StackPanel_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
        {

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
                this.DragMove(); // 拖动窗口
            }
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

        }

        private void ResizeHandle_DragDelta(object sender, DragDeltaEventArgs e)
        {
            // 获取当前窗口
            var window = this;

            // 调整窗口的宽度和高度
            window.Width = Math.Max(window.MinWidth, window.Width + e.HorizontalChange);
            window.Height = Math.Max(window.MinHeight, window.Height + e.VerticalChange);
        }

      
    }
}
