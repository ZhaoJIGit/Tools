using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Windows;
using TaskManagerNew.Services;
using TaskManagerNew.ViewModels;
using TaskManagerNew.Views;

namespace TaskManagerNew
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;

        public App()
        {
            // 配置依赖注入
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// 配置服务
        /// </summary>
        private void ConfigureServices(IServiceCollection services)
        {
            // 添加日志
            services.AddLogging(configure =>
            {
                configure.AddConsole();
                configure.SetMinimumLevel(LogLevel.Information);
            });

            // 添加配置服务
            services.AddSingleton<IConfigurationService, ConfigurationService>();

            // 添加进程服务
            services.AddSingleton<IProcessService, ProcessService>();

            // 添加任务管理服务
            services.AddSingleton<ITaskManagerService, TaskManagerService>();

            // 添加视图模型
            services.AddSingleton<MainViewModel>();

            // 添加主窗口
            services.AddSingleton<MainWindow>();
        }

        /// <summary>
        /// 应用程序启动
        /// </summary>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                // 获取主窗口并显示
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"应用程序启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        /// <summary>
        /// 应用程序退出
        /// </summary>
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            // 清理资源
            var mainViewModel = _serviceProvider.GetService<MainViewModel>();
            mainViewModel?.Dispose();

            var taskManagerService = _serviceProvider.GetService<ITaskManagerService>();
            taskManagerService?.Dispose();
        }
    }
}