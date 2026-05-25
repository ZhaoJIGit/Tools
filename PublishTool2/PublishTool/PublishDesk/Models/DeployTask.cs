using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PublishDesk.Models
{
    public class DeployTask
    {
        public string TaskId { get; set; } = Guid.NewGuid().ToString();

        public string Status { get; set; } = "Running";

        public int Progress { get; set; }
        public List<LogItem> Logs { get; set; } = new();

        public string Message { get; set; }
        public LogType LogType { get; set; } = LogType.Info; //
        public DateTime CreateTime { get; set; } = DateTime.Now;
        // 颜色
        
    }
    public class LogItem
    {
        public long Id { get; set; }
        public DateTime Time { get; set; } = DateTime.Now;

        public string Message { get; set; }

        public LogType LogType { get; set; }
        public Brush Foreground
        {
            get
            {
                return LogType switch
                {
                    LogType.Success => Brushes.LimeGreen,
                    LogType.Error => Brushes.Red,
                    LogType.Warning => Brushes.Orange,
                    LogType.Info => Brushes.DeepSkyBlue,
                    _ => Brushes.White
                };
            }
        }
    }
    public class StartDeployResponse
    {
        public string TaskId { get; set; }

        public string Status { get; set; }
    }
}
