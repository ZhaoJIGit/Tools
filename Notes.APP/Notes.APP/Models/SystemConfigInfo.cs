using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notes.APP.Models
{
    /// <summary>
    /// 系统配置信息
    /// </summary>
    public class SystemConfigInfo
    {
        public long Id { get; set; }
        /// <summary>
        /// 开机启动
        /// </summary>
        public bool StartOpen { get; set; }
        /// <summary>
        /// 字体颜色
        /// </summary>
        public string Color { get; set; }
        /// <summary>
        /// 背景颜色
        /// </summary>
        public string BackGroundColor { get; set; }
        /// <summary>
        /// 字体
        /// </summary>
        public string FontStyle { get; set; }
        /// <summary>
        /// 固定桌面
        /// </summary>
        public bool Fixed { get; set; }

    }
}
