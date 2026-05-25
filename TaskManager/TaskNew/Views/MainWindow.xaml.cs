using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using TaskManagerNew.ViewModels;

namespace TaskManagerNew.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // 保存配置和数据
                var result = MessageBox.Show(
                    "确定要退出应用程序吗？",
                    "确认退出",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
                else
                {
                    // 清理资源
                    _viewModel.Dispose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"退出时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 显示加载遮罩
        /// </summary>
        public void ShowLoading()
        {
            Dispatcher.Invoke(() =>
            {
                LoadingOverlay.Visibility = Visibility.Visible;
            });
        }

        /// <summary>
        /// 隐藏加载遮罩
        /// </summary>
        public void HideLoading()
        {
            Dispatcher.Invoke(() =>
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            });
        }
    }
}