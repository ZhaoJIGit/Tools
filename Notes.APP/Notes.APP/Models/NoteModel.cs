using Notes.APP.Common;
using Notes.APP.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Notes.APP.Models
{
    /// <summary>
    /// 便签模型
    /// </summary>
    public class NoteModel : ConfigModel
    {
        /// <summary>
        /// 便签标识
        /// </summary>
        public string NoteId { get; set; }
        /// <summary>
        /// 便签标题
        /// </summary>
        public string? NoteName { get; set; }
        /// <summary>
        /// 一言
        /// </summary>
        public string? Hitokoto { get; set; }
        /// <summary>
        /// 便签内容
        /// </summary>
        //public string? Content { get; set; }
        private string? _content;
        public string? Content
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_content))
                {
                    return "随便写写";
                }
                else
                {
                    return _content;
                }
            }

            set
            {
                if (_content != value)
                {
                    _content = value;
                    OnPropertyChanged(nameof(Content));
                    OnPropertyChanged(nameof(ContentShort));

                }
            }
        }
        public string ContentShort
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Content))
                {
                    return "随便写写";
                }
                if (Content.Length > 50)
                {
                    return Content.Substring(0, 50);
                }
                return Content;
            }
        }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public string CreateTimeStr
        {
            get
            {
                return CreateTime.ToString("MM/dd HH:mm");
            }
        }
        /// <summary>
        /// 更新时间
        /// </summary>
        private DateTime _updateTime;
        public DateTime UpdateTime
        {
            get => _updateTime;
            set
            {
                if (_updateTime != value)
                {
                    _updateTime = value;
                    OnPropertyChanged(nameof(UpdateTime));
                }
            }
        }
        //public string UpdateTimeStr
        //{
        //    get
        //    {
        //        return UpdateTime.ToString("MM/dd HH:mm");
        //    }
        //}
        public bool _isDeleted { get; set; }
        public bool IsDeleted
        {
            get => _isDeleted;
            set
            {
                if (_isDeleted != value)
                {
                    _isDeleted = value;
                    OnPropertyChanged(nameof(IsDeleted));
                }
            }
        }

        public long _status { get; set; }
        /// <summary>
        ///  0 未完成，1 正在执行，2 已完成，3 已取消 ，4
        /// </summary>
        public long Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }
        public bool _statusTag { get; set; }
        /// <summary>
        ///  标识是否完成，或取消
        /// </summary>
        public bool StatusTag
        {
            get => _statusTag;
            set
            {
                if (_statusTag != value)
                {
                    _statusTag = value;
                    OnPropertyChanged(nameof(StatusTag));
                }
            }
        }

        public string _tags { get; set; }
        /// <summary>
        ///  标识是否完成，或取消
        /// </summary>
        public string Tags
        {
            get => _tags;
            set
            {
                if (_tags != value)
                {
                    _tags = value;
                    OnPropertyChanged(nameof(Tags));
                }
            }
        }
        public bool _isTopUp { get; set; }
        /// <summary>
        ///  置顶
        /// </summary>
        public bool IsTopUp
        {
            get => _isTopUp;
            set
            {
                if (_isTopUp != value)
                {
                    _isTopUp = value;
                    OnPropertyChanged(nameof(IsTopUp));
                }
            }
        }

        public static NoteModel CreateNote()
        {
            var note = new NoteModel();
            note.NoteId = Guid.NewGuid().ToString("n");
            note.BackgroundColor = ColorHelper.GenerateSoftRandomColor();
            note.Opacity = 50;
            note.Fixed = false;
            note.CreateTime = DateTime.Now;
            note.UpdateTime = DateTime.Now;
            note.NoteName = "";
            note.Width = 250;
            note.Height = 280;
            note.Fontsize = 14;
            note.Hitokoto = HitokotoService.Instance.GetHitokoto();
            note.IsDeleted = false;
            note.PageBackgroundColor = ColorHelper.MakeColorTransparent(note.BackgroundColor.ToColor(), 0.8).ToHexColor();
            note.Color = ColorHelper.GetColorByBackground(note.BackgroundColor);
            note.Content = "";
            note.Status = 0;
            note.StatusTag = false;
            note.Tags = "";
            note.IsTopUp = false;

            if (string.IsNullOrWhiteSpace(note.Hitokoto))
            {
                HitokotoService.Instance.GetHitokotoInfoApi();
                note.Hitokoto = HitokotoService.Instance.GetHitokoto();
            }
            Task.Run(() =>
            {
                //重新写一个
                HitokotoService.Instance.GetHitokotoInfoApi();
            });
            return note;
        }

    }
    public class ConfigModel : INotifyPropertyChanged
    {
        /// <summary>
        /// 字体大小
        /// </summary>
        private double? _fontsize;
        public double? Fontsize
        {
            get => _fontsize;
            set
            {
                if (_fontsize != value)
                {
                    _fontsize = value;
                    OnPropertyChanged(nameof(Fontsize));
                }
            }
        }
        /// <summary>
        /// 字体色
        /// </summary>
        private string? _color;
        public string? Color
        {
            get => _color;
            set
            {
                if (_color != value)
                {
                    _color = value;
                    OnPropertyChanged(nameof(Color));
                    //Color = ColorHelper.GetColorByBackground(Color);
                    ColorChanged?.Invoke(Color); // 触发事件通知
                }
            }
        }
        /// <summary>
        /// 背景色
        /// </summary>
        private string? _backgroundColor;
        public string? BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                if (_backgroundColor != value)
                {
                    _backgroundColor = value;
                    OnPropertyChanged(nameof(BackgroundColor));
                    //Color = ColorHelper.GetColorByBackground(BackgroundColor);
                    BackgroundColorChanged?.Invoke(BackgroundColor); // 触发事件通知
                }
            }
        }
        /// <summary>
        /// 背景色
        /// </summary>
        private string? _pageBackgroundColor;
        public string? PageBackgroundColor
        {
            get => _pageBackgroundColor;
            set
            {
                if (_pageBackgroundColor != value)
                {
                    _pageBackgroundColor = value;
                    OnPropertyChanged(nameof(PageBackgroundColor));
                    //BackgroundColorChanged?.Invoke(PageBackgroundColor); // 触发事件通知 
                }
            }
        }
        /// <summary>
        /// 透明度
        /// </summary>
        private double _opacity;
        public double Opacity
        {
            get => _opacity;
            set
            {
                if (_opacity != value)
                {
                    _opacity = value;
                    OnPropertyChanged(nameof(Opacity));
                }
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public event Action<string> BackgroundColorChanged;
        public event Action<string> ColorChanged;

        private double _xAxis { get; set; }
        public double XAxis
        {
            get => _xAxis;
            set
            {
                if (_xAxis != value)
                {
                    _xAxis = value;
                    OnPropertyChanged(nameof(XAxis));
                }
            }
        }
        private double _yAxis { get; set; }
        public double YAxis
        {
            get => _yAxis;
            set
            {
                if (_yAxis != value)
                {
                    _yAxis = value;
                    OnPropertyChanged(nameof(YAxis));
                }
            }
        }
        private double _height { get; set; }
        public double Height
        {
            get => _height;
            set
            {
                if (_height != value)
                {
                    _height = value;
                    OnPropertyChanged(nameof(Height));
                }
            }
        }
        private double _width { get; set; }
        public double Width
        {
            get => _width;
            set
            {
                if (_width != value)
                {
                    _width = value;
                    OnPropertyChanged(nameof(Width));
                }
            }
        }
        private bool _fixed;
        public bool Fixed
        {
            get => _fixed;
            set
            {
                if (_fixed != value)
                {
                    _fixed = value;
                    OnPropertyChanged(nameof(Fixed));
                }
            }
        }
        //private bool _isTopUp;
        //public bool IsTopUp
        //{
        //    get => _isTopUp;
        //    set
        //    {
        //        if (_isTopUp != value)
        //        {
        //            _isTopUp = value;
        //            OnPropertyChanged(nameof(IsTopUp));
        //        }
        //    }
        //}
    }

}
