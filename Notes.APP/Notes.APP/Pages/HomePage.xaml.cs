using Notes.APP.Common;
using Notes.APP.Models;
using Notes.APP.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Notes.APP.Pages
{
    /// <summary>
    /// HomePage.xaml 的交互逻辑
    /// </summary>
    public partial class HomePage : BasePage
    {
        private NoteModel? pageModel;
        private DispatcherTimer _timer;
        private bool isUpdate = false;
        private string _lastText;
        public HomePage()
        {
            InitializeComponent();


        }
        private void InitializeTimer()
        {
            // 初始化计时器
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2) // 设置延迟时间为 2 秒
            };

            // 计时器到期时执行保存操作
            _timer.Tick += (sender, e) =>
            {
                SaveText(_lastText);
                _timer.Stop();  // 停止计时器
            };
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeTimer();
            isUpdate = false;

            pageModel = _ParentWindow.DataContext as NoteModel;
            txtContent.Text = pageModel.Content;
            _lastText = pageModel.Content;
            SetBackgroundColor(pageModel?.BackgroundColor);
            if (_ParentWindow != null)
            {
                pageModel.BackgroundColorChanged += OnBackgroundColorChanged; // 订阅事件
            }
            isUpdate = true;
        }

        private void OnBackgroundColorChanged(string newColor)
        {
            SetBackgroundColor(newColor);
        }
        private void SetBackgroundColor(string color)
        {

            //pageConent.Background = pageModel.PageBackgroundColor.ToSolidColorBrush();
            txtContent.Background = pageModel.PageBackgroundColor.ToSolidColorBrush();
            txtContent.Foreground = ColorHelper.GetColorByBackground(color).ToSolidColorBrush();
        }
       
        // 保存文本的操作
        private void SaveText(string text)
        {

            if (_NoteService.SaveNote(pageModel))
            {
                _Message.ShowSuccess("自动保存成功！");
                var win = Window.GetWindow(this) as MainWindow;
                win?.ChangedTextEvent();
            }
            else
            {
                _Message.ShowError();
            }
        }

        private void txtContent_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                switch (e.Key)
                {
                    case Key.Add:
                        // 处理向上键
                        txtContent.FontSize += 1;
                        break;
                    case Key.Subtract:
                        // 处理向下键
                        if (txtContent.FontSize > 1)
                        {
                            txtContent.FontSize -= 1;
                        }
                        break;
                }
            }
                
        }

        private void txtContent_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 获取当前文本框内容
            _lastText = ((TextBox)sender).Text;

            if (_timer == null || pageModel == null)
            {
                return;
            }
            pageModel.Content = _lastText;

            // 如果计时器已经在运行，停止并重启计时器
            if (_timer.IsEnabled)
            {
                _timer.Stop();
            }
            if (isUpdate)
            {
                _timer.Start(); // 启动计时器
            }
        }

       
    }
}
