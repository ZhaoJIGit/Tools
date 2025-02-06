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
    public partial class HomePage : Page
    {
        private NoteModel? pageModel;
        private DispatcherTimer _timer;
        private MyMessage myMessage;
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
            var parentWindow = Window.GetWindow(this);
            MessagePopupHelper popupHelper = new MessagePopupHelper(parentWindow);
            // 创建 MyMessage 实例并传入 MessagePopupHelper
            myMessage = new MyMessage(popupHelper);
            pageModel = parentWindow.DataContext as NoteModel;
            txtConent.Text = pageModel.Content;
            _lastText = pageModel.Content;
            SetBackgroundColor(pageModel?.BackgroundColor);
            if (parentWindow != null)
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
            txtConent.Background = pageModel.PageBackgroundColor.ToSolidColorBrush();
            txtConent.Foreground = ColorHelper.GetColorByBackground(color).ToSolidColorBrush();
        }

        private void txtConent_TextChanged(object sender, TextChangedEventArgs e)
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
        // 保存文本的操作
        private void SaveText(string text)
        {
            var service = new NoteService();
            if (service.SaveNote(pageModel))
            {
                myMessage.ShowSuccess("自动保存成功！");
            }
            else
            {
                myMessage.ShowError();
            }
        }
    }
}
