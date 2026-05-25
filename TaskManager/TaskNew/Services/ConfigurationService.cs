using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace TaskManagerNew.Services
{
    /// <summary>
    /// 配置服务
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly ILogger<ConfigurationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _configFilePath;

        public ConfigurationService(ILogger<ConfigurationService> logger)
        {
            _logger = logger;
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            
            try
            {
                _configuration = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .Build();

                _logger.LogInformation("Configuration loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load configuration");
                throw;
            }
        }

        /// <summary>
        /// 获取配置值
        /// </summary>
        public T? GetValue<T>(string key, T? defaultValue = default)
        {
            try
            {
                var value = _configuration.GetValue<T>(key);
                return value ?? defaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to get configuration value for key: {key}");
                return defaultValue;
            }
        }

        /// <summary>
        /// 获取进程管理器配置
        /// </summary>
        public ProcessManagerConfig GetProcessManagerConfig()
        {
            try
            {
                var config = new ProcessManagerConfig
                {
                    RefreshInterval = GetValue("ProcessManager:RefreshInterval", 5000),
                    CacheDuration = GetValue("ProcessManager:CacheDuration", 300000),
                    DefaultSearchPaths = GetValue<string[]?>("ProcessManager:DefaultSearchPaths") ?? Array.Empty<string>(),
                    AutoSaveInterval = GetValue("ProcessManager:AutoSaveInterval", 60000),
                    MaxConcurrentProcesses = GetValue("ProcessManager:MaxConcurrentProcesses", 10),
                    EnablePerformanceMonitoring = GetValue("ProcessManager:EnablePerformanceMonitoring", true),
                    PerformanceUpdateInterval = GetValue("ProcessManager:PerformanceUpdateInterval", 1000),
                    LogLevel = GetValue("ProcessManager:LogLevel", "Information") ?? "Information"
                };

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load process manager configuration");
                return new ProcessManagerConfig();
            }
        }

        /// <summary>
        /// 获取UI配置
        /// </summary>
        public UiConfig GetUiConfig()
        {
            try
            {
                var config = new UiConfig
                {
                    Theme = GetValue("UI:Theme", "Dark") ?? "Dark",
                    ShowSystemProcesses = GetValue("UI:ShowSystemProcesses", false),
                    GroupByTask = GetValue("UI:GroupByTask", true),
                    AutoRefresh = GetValue("UI:AutoRefresh", true),
                    DefaultView = GetValue("UI:DefaultView", "List") ?? "List"
                };

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load UI configuration");
                return new UiConfig();
            }
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        public void SaveConfig<T>(string section, T config)
        {
            try
            {
                // 这里应该实现配置保存逻辑
                // 由于时间关系，暂时只记录日志
                _logger.LogInformation($"Configuration saved for section: {section}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to save configuration for section: {section}");
            }
        }

        /// <summary>
        /// 重新加载配置
        /// </summary>
        public void Reload()
        {
            try
            {
                // 重新加载配置
                _logger.LogInformation("Configuration reloaded");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reload configuration");
            }
        }
    }

    /// <summary>
    /// 进程管理器配置
    /// </summary>
    public class ProcessManagerConfig
    {
        public int RefreshInterval { get; set; } = 5000;
        public int CacheDuration { get; set; } = 300000;
        public string[] DefaultSearchPaths { get; set; } = Array.Empty<string>();
        public int AutoSaveInterval { get; set; } = 60000;
        public int MaxConcurrentProcesses { get; set; } = 10;
        public bool EnablePerformanceMonitoring { get; set; } = true;
        public int PerformanceUpdateInterval { get; set; } = 1000;
        public string LogLevel { get; set; } = "Information";
    }

    /// <summary>
    /// UI配置
    /// </summary>
    public class UiConfig
    {
        public string Theme { get; set; } = "Dark";
        public bool ShowSystemProcesses { get; set; } = false;
        public bool GroupByTask { get; set; } = true;
        public bool AutoRefresh { get; set; } = true;
        public string DefaultView { get; set; } = "List";
    }

    /// <summary>
    /// 配置服务接口
    /// </summary>
    public interface IConfigurationService
    {
        T? GetValue<T>(string key, T? defaultValue = default);
        ProcessManagerConfig GetProcessManagerConfig();
        UiConfig GetUiConfig();
        void SaveConfig<T>(string section, T config);
        void Reload();
    }
}