using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManager
{
    /// <summary>
    /// 进程数据模型
    /// </summary>
    public class ProcessInfo : INotifyPropertyChanged
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        public string TaskName { get; set; }
        /// <summary>
        /// 任务群组
        /// </summary>
        public string TaskGroup { get; set; }
        /// <summary>
        /// 进程Id
        /// </summary>
        public int ProcessId { get; set; }
        /// <summary>
        /// 是否选中
        /// </summary>
        private bool isSelected;
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class TaskGroupInfo { 
        public string TaskGroup { get; set; }

    }
}
