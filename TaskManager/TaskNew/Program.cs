using System;
using System.Windows;

namespace TaskManagerNew
{
    /// <summary>
    /// 应用程序入口点
    /// </summary>
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                // 设置异常处理
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                
                // 创建并运行应用程序
                var app = new App();
                app.InitializeComponent();
                app.Run();
            }
            catch (Exception ex)
            {
                HandleFatalError(ex);
            }
        }

        /// <summary>
        /// 处理未捕获的异常
        /// </summary>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                HandleFatalError(ex);
            }
        }

        /// <summary>
        /// 处理致命错误
        /// </summary>
        private static void HandleFatalError(Exception ex)
        {
            try
            {
                // 记录错误日志
                string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 致命错误: {ex}\n";
                string logFile = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "TaskManagerPro",
                    "error.log");
                
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logFile)!);
                System.IO.File.AppendAllText(logFile, logMessage);
                
                // 显示错误对话框
                MessageBox.Show(
                    $"应用程序发生致命错误:\n\n{ex.Message}\n\n详细信息已记录到日志文件。",
                    "Task Manager Pro - 错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
                // 如果连错误处理都失败了，至少显示一个简单的消息
                MessageBox.Show(
                    "应用程序发生致命错误，无法继续运行。",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            
            Environment.Exit(1);
        }
    }
}