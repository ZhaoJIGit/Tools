using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;

namespace TaskManagerNew.Models
{
    /// <summary>
    /// 任务群组信息模型
    /// </summary>
    public partial class TaskGroupInfo : ObservableObject
    {
        [ObservableProperty]
        private string _taskGroup = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private DateTime _createdTime = DateTime.Now;

        [ObservableProperty]
        private DateTime _lastModifiedTime = DateTime.Now;

        [ObservableProperty]
        private int _processCount;

        [ObservableProperty]
        private bool _isExpanded = true;

        [ObservableProperty]
        private bool _isMonitoringEnabled = true;

        [ObservableProperty]
        private bool _autoStartEnabled;

        [ObservableProperty]
        private string _icon = "📁";

        [ObservableProperty]
        private ObservableCollection<ProcessInfo> _processes = new();

        [ObservableProperty]
        private ObservableCollection<ScheduledTask> _scheduledTasks = new();

        /// <summary>
        /// 获取总CPU使用率
        /// </summary>
        public float TotalCpuUsage
        {
            get
            {
                float total = 0;
                foreach (var process in Processes)
                {
                    total += process.CpuUsage;
                }
                return total;
            }
        }

        /// <summary>
        /// 获取总内存使用量
        /// </summary>
        public long TotalMemoryUsage
        {
            get
            {
                long total = 0;
                foreach (var process in Processes)
                {
                    total += process.MemoryUsage;
                }
                return total;
            }
        }

        /// <summary>
        /// 获取总内存使用量格式化字符串
        /// </summary>
        public string TotalMemoryUsageFormatted => FormatBytes(TotalMemoryUsage);

        /// <summary>
        /// 获取运行中的进程数量
        /// </summary>
        public int RunningProcessCount
        {
            get
            {
                int count = 0;
                foreach (var process in Processes)
                {
                    if (process.Status == ProcessStatus.Running)
                        count++;
                }
                return count;
            }
        }

        /// <summary>
        /// 获取异常进程数量
        /// </summary>
        public int ErrorProcessCount
        {
            get
            {
                int count = 0;
                foreach (var process in Processes)
                {
                    if (process.Status == ProcessStatus.Error || process.Status == ProcessStatus.NotResponding)
                        count++;
                }
                return count;
            }
        }

        /// <summary>
        /// 添加进程到群组
        /// </summary>
        public void AddProcess(ProcessInfo process)
        {
            Processes.Add(process);
            ProcessCount = Processes.Count;
            LastModifiedTime = DateTime.Now;
        }

        /// <summary>
        /// 从群组移除进程
        /// </summary>
        public bool RemoveProcess(ProcessInfo process)
        {
            var result = Processes.Remove(process);
            if (result)
            {
                ProcessCount = Processes.Count;
                LastModifiedTime = DateTime.Now;
            }
            return result;
        }

        /// <summary>
        /// 清空群组进程
        /// </summary>
        public void ClearProcesses()
        {
            Processes.Clear();
            ProcessCount = 0;
            LastModifiedTime = DateTime.Now;
        }

        /// <summary>
        /// 查找进程
        /// </summary>
        public ProcessInfo? FindProcess(int processId)
        {
            foreach (var process in Processes)
            {
                if (process.ProcessId == processId)
                    return process;
            }
            return null;
        }

        /// <summary>
        /// 更新进程信息
        /// </summary>
        public void UpdateProcess(ProcessInfo updatedProcess)
        {
            for (int i = 0; i < Processes.Count; i++)
            {
                if (Processes[i].ProcessId == updatedProcess.ProcessId)
                {
                    Processes[i] = updatedProcess;
                    LastModifiedTime = DateTime.Now;
                    break;
                }
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
        /// 克隆任务群组
        /// </summary>
        public TaskGroupInfo Clone()
        {
            var clone = new TaskGroupInfo
            {
                TaskGroup = TaskGroup,
                Description = Description,
                CreatedTime = CreatedTime,
                LastModifiedTime = LastModifiedTime,
                ProcessCount = ProcessCount,
                IsExpanded = IsExpanded,
                IsMonitoringEnabled = IsMonitoringEnabled,
                AutoStartEnabled = AutoStartEnabled,
                Icon = Icon
            };

            foreach (var process in Processes)
            {
                clone.Processes.Add(process.Clone());
            }

            foreach (var task in ScheduledTasks)
            {
                clone.ScheduledTasks.Add(task.Clone());
            }

            return clone;
        }
    }

    /// <summary>
    /// 计划任务模型
    /// </summary>
    public partial class ScheduledTask : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private TaskType _taskType = TaskType.OneTime;

        [ObservableProperty]
        private DateTime _scheduledTime = DateTime.Now.AddHours(1);

        [ObservableProperty]
        private TimeSpan _interval = TimeSpan.FromHours(1);

        [ObservableProperty]
        private bool _isEnabled = true;

        [ObservableProperty]
        private DateTime _lastRunTime;

        [ObservableProperty]
        private DateTime _nextRunTime;

        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private string _command = string.Empty;

        [ObservableProperty]
        private string _arguments = string.Empty;

        [ObservableProperty]
        private string _workingDirectory = string.Empty;

        /// <summary>
        /// 计算下次运行时间
        /// </summary>
        public void CalculateNextRunTime()
        {
            if (!IsEnabled)
                return;

            switch (TaskType)
            {
                case TaskType.OneTime:
                    NextRunTime = ScheduledTime;
                    break;
                case TaskType.Scheduled:
                    NextRunTime = LastRunTime + Interval;
                    break;
                case TaskType.Monitor:
                    NextRunTime = DateTime.Now.Add(Interval);
                    break;
            }
        }

        /// <summary>
        /// 克隆计划任务
        /// </summary>
        public ScheduledTask Clone()
        {
            return new ScheduledTask
            {
                Name = Name,
                Description = Description,
                TaskType = TaskType,
                ScheduledTime = ScheduledTime,
                Interval = Interval,
                IsEnabled = IsEnabled,
                LastRunTime = LastRunTime,
                NextRunTime = NextRunTime,
                IsRunning = IsRunning,
                Command = Command,
                Arguments = Arguments,
                WorkingDirectory = WorkingDirectory
            };
        }
    }
}