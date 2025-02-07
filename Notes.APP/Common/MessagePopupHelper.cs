using Notes.APP.CustomCtrls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Media;

namespace Notes.APP.Common
{
    public class MessagePopupHelper
    {
        private Popup _popupMessage;
        private Border _messageBorder;
        private StackPanel _stackPanel;
        private TextBlock _messageText;
        private TextBlock _messageIcon;

        public MessagePopupHelper(Window window)
        {
            // 创建并初始化 Popup 只在该窗口上下文中
            _popupMessage = new Popup
            {
                StaysOpen = false,
                Placement = PlacementMode.Bottom,
                PlacementTarget = window,
                Height = 30,
                HorizontalOffset = 10,
                AllowsTransparency= true,
                PopupAnimation = PopupAnimation.Fade,
                Margin = new Thickness(100),
                Opacity = 0.8,
            };

            // 创建 Border
            _messageBorder = new Border
            {
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness=new Thickness(0),
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                Padding = new Thickness(5),
                CornerRadius = new CornerRadius(10),
                Opacity = 0.8
            };

            // 创建 StackPanel 用于容纳 TextBlock
            _stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            // 创建 MessageIcon 和 MessageText
            _messageIcon = new TextBlock
            {
                FontSize = 12
            };

            _messageText = new TextBlock
            {
                FontSize = 14,
                Foreground = new SolidColorBrush(Colors.Black)
            };

            // 将 TextBlock 添加到 StackPanel 中
            _stackPanel.Children.Add(_messageIcon);
            _stackPanel.Children.Add(_messageText);

            // 将 StackPanel 设置为 Border 的内容
            _messageBorder.Child = _stackPanel;

            // 将 Border 设置为 Popup 的内容
            _popupMessage.Child = _messageBorder;
        }

        public void ShowMessage(string message, Brush background, string icon, int duration)
        {
            _messageText.Text = $"{icon} {message}";

            // 设置背景颜色
            _messageBorder.Background = background;
            _messageBorder.BorderBrush = new SolidColorBrush(Colors.Transparent);
            // 显示 Popup
            _popupMessage.IsOpen = true;

            // 设置定时器来关闭 Popup
            Task.Delay(duration).ContinueWith(_ => Application.Current.Dispatcher.Invoke(() => _popupMessage.IsOpen = false));
        }
    }
    public class MyMessage
    {
        private readonly MessagePopupHelper _popupHelper;

        /// <summary>
        /// 在窗口初始化时传入 MessagePopupHelper 实例，确保绑定 Popup
        /// </summary>
        public MyMessage(MessagePopupHelper popupHelper)
        {
            _popupHelper = popupHelper;
        }

        /// <summary>
        /// 成功提示（默认3秒，可修改）
        /// </summary>
        public void ShowSuccess(string message = "操作成功！", int duration = 2000)
        {
            ShowMessage(message, Brushes.LightGreen, "✔", duration);
        }

        /// <summary>
        /// 警告提示（默认3秒，可修改）
        /// </summary>
        public void ShowWarning(string message = "请注意！", int duration = 3000)
        {
            ShowMessage(message, Brushes.Orange, "⚠", duration);
        }

        /// <summary>
        /// 错误提示（默认5秒，可修改）
        /// </summary>
        public void ShowError(string message = "操作失败！", int duration = 5000)
        {
            ShowMessage(message, Brushes.IndianRed, "✖", duration);
        }

        /// <summary>
        /// 信息提示（默认3秒，可修改）
        /// </summary>
        public void ShowInfo(string message = "提示信息", int duration = 2000)
        {
            ShowMessage(message, Brushes.LightGray, "ℹ", duration);
        }

        /// <summary>
        /// 私有方法，显示消息
        /// </summary>
        private void ShowMessage(string message, Brush background, string icon, int duration)
        {
            _popupHelper.ShowMessage(message, background, icon, duration);
        }
    }
}
