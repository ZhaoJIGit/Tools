using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;
using TaskManager.Core.Interfaces;
using TaskManager.Core.Models;
using TaskManager.Core.Services;
using TaskManager.Data.Repositories;
using TaskManager.ViewModels;
using TaskManager.Views;

namespace TaskManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly IHost _host;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(services, context.Configuration);
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.AddDebug();
                })
                .Build();
        }

        private void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // 配置绑定
            services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
            
            // 注册设置实例
            services.AddSingleton(sp =>
            {
                var settings = new AppSettings();
                configuration.GetSection("AppSettings").Bind(settings);
                return settings;
            });

            services.AddSingleton(sp => sp.GetRequiredService<AppSettings>().CacheSettings);
            services.AddSingleton(sp => sp.GetRequiredService<AppSettings>().ProcessSettings);
            services.AddSingleton(sp => sp.GetRequiredService<AppSettings>().UiSettings);

            // 注册核心服务
            services.AddSingleton<IProcessMonitor, ProcessMonitor>();
            services.AddSingleton<ITaskGroupRepository, TaskGroupRepository>();

            // 注册 ViewModels
            services.AddSingleton<MainViewModel>();

            // 注册 Views
            services.AddSingleton<MainWindow>();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _host.Services.GetRequiredService<MainViewModel>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (_host)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5));
            }

            base.OnExit(e);
        }
    }
}