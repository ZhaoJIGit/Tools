namespace TaskManagerNew.Models
{
    /// <summary>
    /// 进程搜索模式
    /// </summary>
    public enum ProcessSearchMode
    {
        /// <summary>
        /// 按名称搜索
        /// </summary>
        ByName,
        
        /// <summary>
        /// 按目录搜索
        /// </summary>
        ByDirectory,
        
        /// <summary>
        /// 按PID搜索
        /// </summary>
        ByPid,
        
        /// <summary>
        /// 全部进程
        /// </summary>
        All
    }

    /// <summary>
    /// 进程状态
    /// </summary>
    public enum ProcessStatus
    {
        /// <summary>
        /// 运行中
        /// </summary>
        Running,
        
        /// <summary>
        /// 已停止
        /// </summary>
        Stopped,
        
        /// <summary>
        /// 无响应
        /// </summary>
        NotResponding,
        
        /// <summary>
        /// 异常
        /// </summary>
        Error
    }

    /// <summary>
    /// 进程优先级
    /// </summary>
    public enum ProcessPriority
    {
        /// <summary>
        /// 实时
        /// </summary>
        RealTime,
        
        /// <summary>
        /// 高
        /// </summary>
        High,
        
        /// <summary>
        /// 高于正常
        /// </summary>
        AboveNormal,
        
        /// <summary>
        /// 正常
        /// </summary>
        Normal,
        
        /// <summary>
        /// 低于正常
        /// </summary>
        BelowNormal,
        
        /// <summary>
        /// 空闲
        /// </summary>
        Idle
    }

    /// <summary>
    /// 任务类型
    /// </summary>
    public enum TaskType
    {
        /// <summary>
        /// 一次性任务
        /// </summary>
        OneTime,
        
        /// <summary>
        /// 定时任务
        /// </summary>
        Scheduled,
        
        /// <summary>
        /// 监控任务
        /// </summary>
        Monitor
    }
}