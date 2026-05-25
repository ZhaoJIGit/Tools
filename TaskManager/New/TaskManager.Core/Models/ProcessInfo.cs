using System.ComponentModel;

namespace TaskManager.Core.Models
{
    /// <summary>
    /// 进程信息模型
    /// </summary>
    public class ProcessInfo : INotifyPropertyChanged
    {
        private bool _isSelected;
        private int _processId;
        private string _taskGroup = string.Empty;
        private string _taskName = string.Empty;

        /// <summary>
        /// 是否选中
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        /// <summary>
        /// 进程ID
        /// </summary>
        public int ProcessId
        {
            get => _processId;
            set
            {
                if (_processId != value)
                {
                    _processId = value;
                    OnPropertyChanged(nameof(ProcessId));
                }
            }
        }

        /// <summary>
        /// 任务组名称
        /// </summary>
        public string TaskGroup
        {
            get => _taskGroup;
            set
            {
                if (_taskGroup != value)
                {
                    _taskGroup = value;
                    OnPropertyChanged(nameof(TaskGroup));
                }
            }
        }

        /// <summary>
        /// 任务名称（命令行）
        /// </summary>
        public string TaskName
        {
            get => _taskName;
            set
            {
                if (_taskName != value)
                {
                    _taskName = value;
                    OnPropertyChanged(nameof(TaskName));
                }
            }
        }

        /// <summary>
        /// 属性变更事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 触发属性变更通知
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 重写 ToString 方法
        /// </summary>
        public override string ToString()
        {
            return $"ProcessId: {ProcessId}, TaskGroup: {TaskGroup}, TaskName: {TaskName}";
        }
    }
}