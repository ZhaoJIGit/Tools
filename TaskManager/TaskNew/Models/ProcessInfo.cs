using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel;

namespace TaskManagerNew.Models
{
    /// <summary>
    /// 进程信息模型
    /// </summary>
    public partial class ProcessInfo : ObservableObject
    {
        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private int _processId;

        [ObservableProperty]
        private string _taskGroup = string.Empty;

        [ObservableProperty]
        private string _taskName = string.Empty;

        [ObservableProperty]
        private string _commandLine = string.Empty;

        [ObservableProperty]
        private string _workingDirectory = string.Empty;

        [ObservableProperty]
        private ProcessStatus _status = ProcessStatus.Running;

        [ObservableProperty]
        private float _cpuUsage;

        [ObservableProperty]
        private long _memoryUsage;

        [ObservableProperty]
        private long _peakMemoryUsage;

        [ObservableProperty]
        private DateTime _startTime;

        [ObservableProperty]
        private TimeSpan _totalProcessorTime;

        [ObservableProperty]
        private ProcessPriority _priority = ProcessPriority.Normal;

        [ObservableProperty]
        private string _userName = string.Empty;

        [ObservableProperty]
        private int _threadCount;

        [ObservableProperty]
        private int _handleCount;

        [ObservableProperty]
        private bool _isDotNetProcess;

        [ObservableProperty]
        private string _dotNetVersion = string.Empty;

        /// <summary>
        /// 进程图标路径
        /// </summary>
        [ObservableProperty]
        private string _iconPath = string.Empty;

        /// <summary>
        /// 进程描述
        /// </summary>
        [ObservableProperty]
        private string _description = string.Empty;

        /// <summary>
        /// 公司名称
        /// </summary>
        [ObservableProperty]
        private string _company = string.Empty;

        /// <summary>
        /// 文件版本
        /// </summary>
        [ObservableProperty]
        private string _fileVersion = string.Empty;

        /// <summary>
        /// 产品版本
        /// </summary>
        [ObservableProperty]
        private string _productVersion = string.Empty;

        /// <summary>
        /// 是否响应
        /// </summary>
        [ObservableProperty]
        private bool _isResponding = true;

        /// <summary>
        /// 父进程ID
        /// </summary>
        [ObservableProperty]
        private int _parentProcessId;

        /// <summary>
        /// 会话ID
        /// </summary>
        [ObservableProperty]
        private int _sessionId;

        /// <summary>
        /// 获取进程显示名称
        /// </summary>
        public string DisplayName => !string.IsNullOrEmpty(TaskName) ? TaskName : $"进程 {ProcessId}";

        /// <summary>
        /// 获取内存使用量格式化字符串
        /// </summary>
        public string MemoryUsageFormatted => FormatBytes(MemoryUsage);

        /// <summary>
        /// 获取峰值内存使用量格式化字符串
        /// </summary>
        public string PeakMemoryUsageFormatted => FormatBytes(PeakMemoryUsage);

        /// <summary>
        /// 获取运行时间
        /// </summary>
        public TimeSpan RunningTime => DateTime.Now - StartTime;

        /// <summary>
        /// 获取运行时间格式化字符串
        /// </summary>
        public string RunningTimeFormatted
        {
            get
            {
                var time = RunningTime;
                if (time.TotalDays >= 1)
                    return $"{(int)time.TotalDays}d {time.Hours}h";
                if (time.TotalHours >= 1)
                    return $"{(int)time.TotalHours}h {time.Minutes}m";
                if (time.TotalMinutes >= 1)
                    return $"{(int)time.TotalMinutes}m {time.Seconds}s";
                return $"{(int)time.TotalSeconds}s";
            }
        }

        /// <summary>
        /// 获取CPU使用率格式化字符串
        /// </summary>
        public string CpuUsageFormatted => $"{CpuUsage:F1}%";

        /// <summary>
        /// 获取状态图标
        /// </summary>
        public string StatusIcon
        {
            get
            {
                return Status switch
                {
                    ProcessStatus.Running => "✅",
                    ProcessStatus.Stopped => "⏹️",
                    ProcessStatus.NotResponding => "⚠️",
                    ProcessStatus.Error => "❌",
                    _ => "❓"
                };
            }
        }

        /// <summary>
        /// 获取优先级图标
        /// </summary>
        public string PriorityIcon
        {
            get
            {
                return Priority switch
                {
                    ProcessPriority.RealTime => "⚡",
                    ProcessPriority.High => "🔥",
                    ProcessPriority.AboveNormal => "⬆️",
                    ProcessPriority.Normal => "➡️",
                    ProcessPriority.BelowNormal => "⬇️",
                    ProcessPriority.Idle => "😴",
                    _ => "➡️"
                };
            }
        }

        /// <summary>
        /// 格式化字节大小
        /// </summary>
        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            
            return $"{len:F2} {sizes[order]}";
        }

        /// <summary>
        /// 更新性能数据
        /// </summary>
        public void UpdatePerformanceData(float cpuUsage, long memoryUsage, long peakMemoryUsage)
        {
            CpuUsage = cpuUsage;
            MemoryUsage = memoryUsage;
            PeakMemoryUsage = peakMemoryUsage;
        }

        /// <summary>
        /// 更新进程状态
        /// </summary>
        public void UpdateStatus(ProcessStatus status, bool isResponding = true)
        {
            Status = status;
            IsResponding = isResponding;
        }

        /// <summary>
        /// 克隆进程信息
        /// </summary>
        public ProcessInfo Clone()
        {
            return new ProcessInfo
            {
                IsSelected = IsSelected,
                ProcessId = ProcessId,
                TaskGroup = TaskGroup,
                TaskName = TaskName,
                CommandLine = CommandLine,
                WorkingDirectory = WorkingDirectory,
                Status = Status,
                CpuUsage = CpuUsage,
                MemoryUsage = MemoryUsage,
                PeakMemoryUsage = PeakMemoryUsage,
                StartTime = StartTime,
                TotalProcessorTime = TotalProcessorTime,
                Priority = Priority,
                UserName = UserName,
                ThreadCount = ThreadCount,
                HandleCount = HandleCount,
                IsDotNetProcess = IsDotNetProcess,
                DotNetVersion = DotNetVersion,
                IconPath = IconPath,
                Description = Description,
                Company = Company,
                FileVersion = FileVersion,
                ProductVersion = ProductVersion,
                IsResponding = IsResponding,
                ParentProcessId = ParentProcessId,
                SessionId = SessionId
            };
        }
    }
}