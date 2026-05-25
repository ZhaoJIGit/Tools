using System.ComponentModel;

namespace TaskManager.Core.Models
{
    /// <summary>
    /// 任务组信息模型
    /// </summary>
    public class TaskGroupInfo : INotifyPropertyChanged
    {
        private string _taskGroup = string.Empty;
        private int _processCount;

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
        /// 进程数量
        /// </summary>
        public int ProcessCount
        {
            get => _processCount;
            set
            {
                if (_processCount != value)
                {
                    _processCount = value;
                    OnPropertyChanged(nameof(ProcessCount));
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
            return $"TaskGroup: {TaskGroup}, ProcessCount: {ProcessCount}";
        }
    }
}