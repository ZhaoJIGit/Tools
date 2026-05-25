namespace TaskManager.Core.Models
{
    /// <summary>
    /// 应用程序设置
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// 缓存设置
        /// </summary>
        public CacheSettings CacheSettings { get; set; } = new();

        /// <summary>
        /// 进程设置
        /// </summary>
        public ProcessSettings ProcessSettings { get; set; } = new();

        /// <summary>
        /// UI设置
        /// </summary>
        public UiSettings UiSettings { get; set; } = new();
    }

    /// <summary>
    /// 缓存设置
    /// </summary>
    public class CacheSettings
    {
        /// <summary>
        /// 缓存目录
        /// </summary>
        public string Directory { get; set; } = "%TEMP%\\TaskManager\\Cache";

        /// <summary>
        /// JSON文件名
        /// </summary>
        public string JsonFileName { get; set; } = "TaskGroup.json";

        /// <summary>
        /// 缓存过期时间（分钟）
        /// </summary>
        public int CacheExpirationMinutes { get; set; } = 30;
    }

    /// <summary>
    /// 进程设置
    /// </summary>
    public class ProcessSettings
    {
        /// <summary>
        /// 批处理大小
        /// </summary>
        public int BatchSize { get; set; } = 10;

        /// <summary>
        /// 最大并行度
        /// </summary>
        public int MaxParallelism { get; set; } = 4;

        /// <summary>
        /// 刷新间隔（秒）
        /// </summary>
        public int RefreshIntervalSeconds { get; set; } = 5;

        /// <summary>
        /// 是否只监控 dotnet 进程
        /// </summary>
        public bool MonitorDotnetOnly { get; set; } = true;
    }

    /// <summary>
    /// UI设置
    /// </summary>
    public class UiSettings
    {
        /// <summary>
        /// 主题模式
        /// </summary>
        public string Theme { get; set; } = "Light";

        /// <summary>
        /// 是否显示系统进程
        /// </summary>
        public bool ShowSystemProcesses { get; set; } = false;

        /// <summary>
        /// 自动刷新
        /// </summary>
        public bool AutoRefresh { get; set; } = true;

        /// <summary>
        /// 窗口位置X
        /// </summary>
        public double WindowLeft { get; set; }

        /// <summary>
        /// 窗口位置Y
        /// </summary>
        public double WindowTop { get; set; }

        /// <summary>
        /// 窗口宽度
        /// </summary>
        public double WindowWidth { get; set; } = 800;

        /// <summary>
        /// 窗口高度
        /// </summary>
        public double WindowHeight { get; set; } = 600;
    }
}