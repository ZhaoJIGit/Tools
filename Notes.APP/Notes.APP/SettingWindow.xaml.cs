using Notes.APP.Common;
using Notes.APP.Models;
using Notes.APP.Services;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace Notes.APP
{
    /// <summary>
    /// SettingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingWindow : Window
    {
        private Point _mouseDownPosition;
        SystemConfigInfo SystemConfigInfo { get; set; }
        public SettingWindow()
        {
            InitializeComponent();
            SystemConfigInfoService systemConfig = SystemConfigInfoService.Instance;
            var config = systemConfig.GetConfig();
            SystemConfigInfo = config;
            this.DataContext = SystemConfigInfo;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            isOpenRunBox.IsChecked = SystemConfigInfo.StartOpen;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // 关闭窗口
        }

        private void ResizeHandle_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {

        }

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

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (isOpenRunBox.IsChecked.Value)
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
            if (!isOpenRunBox.IsChecked.Value)
            {
                SystemConfigInfoService systemConfig = SystemConfigInfoService.Instance;
                SystemConfigInfo.StartOpen = isOpenRunBox.IsChecked.Value;
                systemConfig.SaveConfig(SystemConfigInfo);
                // 当勾选框被取消选中时触发
                StartupManager.DisableAutoStartup();
            }
        }

        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isOpenRunBox.IsChecked = !isOpenRunBox.IsChecked;
        }
    }
}
