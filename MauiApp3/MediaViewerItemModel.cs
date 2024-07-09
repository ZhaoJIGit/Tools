using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp3
{
    public class MediaViewerItemModel
    {
        public string Url { get; set; }
        /// <summary>
        /// 是否是图片
        /// </summary>
        public bool IsImage { get; set; }

        public bool Show { get; set; } = false;
    }

    public class MediaViewerTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// 图片模板
        /// </summary>
        public DataTemplate ImageTemplate { get; set; }
        /// <summary>
        /// 视频模板
        /// </summary>
        public DataTemplate VideoTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            return ((MediaViewerItemModel)item).IsImage ? ImageTemplate : VideoTemplate;
        }
    }

    public class MediaViewerModel
    {
        /// <summary>
        /// 视频
        /// </summary>
        public MediaSource? MediaSource { get; set; }
        /// <summary>
        /// 图片
        /// </summary>
        public ImageSource? ImageSource { get; set; }

        public bool IsImage { get; set; }

        public bool IsVideo { get; set; }
    }
}
