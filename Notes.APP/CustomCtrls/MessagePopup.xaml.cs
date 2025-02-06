using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Notes.APP.CustomCtrls
{
    /// <summary>
    /// MessagePopup.xaml 的交互逻辑
    /// </summary>
    public partial class MessagePopup : UserControl
    {
        public MessagePopup()
        {
            InitializeComponent();
        }
        /// <summary>
        /// 显示提示框
        /// </summary>
        /// <param name="message">提示消息</param>
        /// <param name="type">消息类型（success, warning, error）</param>
        /// <param name="duration">显示时长（毫秒）</param>
        //public void ShowMessage(string message, Brush background, string icon, int duration = 2000)
        //{
        //    MessageText.Text = message;
        //    MessageBorder.Background = background;
        //    MessageIcon.Text = icon;

        //    PopupMessage.IsOpen = true;
        //    //Task.Delay(duration).ContinueWith(_ => Dispatcher.Invoke(() => PopupMessage.IsOpen = false));
        //}
        public void ShowMessage(string message, Brush background, string icon, int duration = 2000)
        {
            MessageText.Text = message;
            MessageBorder.Background = background;
            MessageIcon.Text = icon; // 成功图标

            // 获取当前窗口的位置
            var mainWindow = Application.Current.MainWindow;
            var windowTop = mainWindow.Top;
            var windowHeight = mainWindow.ActualHeight;

            // 设置 Popup 位置，显示在当前窗口的上方
            PopupMessage.PlacementTarget = mainWindow; // 设置 Popup 目标为窗口
            //PopupMessage.Placement = PlacementMode.Top; // 设置 Popup 显示在目标的上方
            PopupMessage.VerticalOffset = 0; // 距离窗口顶部的偏移量，确保 Popup 不受 Page 布局影响

            PopupMessage.IsOpen = true;
            Task.Delay(duration).ContinueWith(_ => Dispatcher.Invoke(() => PopupMessage.IsOpen = false));
        }
        /// <summary>
        /// 设置消息框样式
        /// </summary>
        private void SetMessageStyle(string type)
        {
            switch (type.ToLower())
            {
                case "success":
                    MessageBorder.Background = Brushes.LightGreen;
                    MessageIcon.Text = "✔"; // 成功图标
                    break;
                case "warning":
                    MessageBorder.Background = Brushes.Orange;
                    MessageIcon.Text = "⚠"; // 警告图标
                    break;
                case "error":
                    MessageBorder.Background = Brushes.IndianRed;
                    MessageIcon.Text = "✖"; // 错误图标
                    break;
                default:
                    MessageBorder.Background = Brushes.LightGray;
                    MessageIcon.Text = "ℹ"; // 默认信息
                    break;
            }
        }
    }
}
