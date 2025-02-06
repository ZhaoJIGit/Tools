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
        /// 便签内容
        /// </summary>
        public string? Content { get; set; }

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
        public DateTime UpdateTime { get; set; } = DateTime.Now;
        public string UpdateTimeStr
        {
            get
            {
                return UpdateTime.ToString("MM/dd HH:mm");
            }
        }
        public bool IsDeleted { get; set; }
        public static NoteModel CreateNote()
        {
            var note = new NoteModel();
            note.NoteId = Guid.NewGuid().ToString("n");
            note.BackgroundColor = ColorHelper.GenerateRandomColor();
            note.Opacity = 50;
            note.Fixed = true;
            note.CreateTime = DateTime.Now;
            note.UpdateTime = DateTime.Now;
            note.NoteName = "";
            note.Width = 250;
            note.Height = 280;
            note.IsDeleted = false;
            note.PageBackgroundColor = ColorHelper.MakeColorTransparent(note.BackgroundColor.ToColor(), 0.8).ToHexColor();
            note.Color = ColorHelper.GetColorByBackground(note.BackgroundColor);
            note.Content = "";
            return note;
        }

    }
    public class ConfigModel : INotifyPropertyChanged
    {
        /// <summary>
        /// 字体色
        /// </summary>
        public string? _color;
        public string? Color
        {
            get => _color;
            set
            {
                if (_color != value)
                {
                    _color = value;
                    OnPropertyChanged(nameof(Color));
                }
            }
        }
        /// <summary>
        /// 背景色
        /// </summary>
        public string? _backgroundColor;
        public string? BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                if (_backgroundColor != value)
                {
                    _backgroundColor = value;
                    OnPropertyChanged(nameof(BackgroundColor));
                    Color = ColorHelper.GetColorByBackground(BackgroundColor);
                    BackgroundColorChanged?.Invoke(BackgroundColor); // 触发事件通知
                }
            }
        }
        /// <summary>
        /// 背景色
        /// </summary>
        public string? _pageBackgroundColor;
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
       public bool Fixed {
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

    }

}
