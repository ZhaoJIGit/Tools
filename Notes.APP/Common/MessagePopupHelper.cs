using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        private StackPanel _buttonPanel;
        private Button _confirmButton;
        private Button _cancelButton;

        public MessagePopupHelper(Window window)
        {
            // 创建 Popup
            _popupMessage = new Popup
            {
                StaysOpen = false,
                Placement = PlacementMode.Bottom,
                PlacementTarget = window,
                Height = 60,
                Width = 250,
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.Fade,
                Opacity = 0.9,
            };

            // 创建 Border
            _messageBorder = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Colors.Gray),
                Padding = new Thickness(10),
                CornerRadius = new CornerRadius(10),
                Opacity = 0.9
            };

            // 创建 StackPanel（消息内容）
            _stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            // 创建 MessageIcon 和 MessageText
            _messageIcon = new TextBlock
            {
                FontSize = 16,
                Margin = new Thickness(0, 0, 5, 0)
            };

            _messageText = new TextBlock
            {
                FontSize = 14,
                Foreground = new SolidColorBrush(Colors.Black),
                TextWrapping = TextWrapping.Wrap
            };

            // 按钮面板
            _buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0),
                Visibility = Visibility.Collapsed
            };

            // 确定按钮
            _confirmButton = new Button
            {
                Content = "确定",
                Width = 80,
                Margin = new Thickness(5),
                Background = new SolidColorBrush(Colors.LightGreen),
                Foreground = new SolidColorBrush(Colors.Black),
            };

            // 取消按钮
            _cancelButton = new Button
            {
                Content = "取消",
                Width = 80,
                Margin = new Thickness(5),
                Background = new SolidColorBrush(Colors.LightGray),
                Foreground = new SolidColorBrush(Colors.Black),
            };

            // 添加到按钮面板
            _buttonPanel.Children.Add(_confirmButton);
            _buttonPanel.Children.Add(_cancelButton);

            // 组装组件
            _stackPanel.Children.Add(_messageIcon);
            _stackPanel.Children.Add(_messageText);
            _stackPanel.Children.Add(_buttonPanel);

            _messageBorder.Child = _stackPanel;
            _popupMessage.Child = _messageBorder;
        }

        /// <summary>
        /// 显示普通消息
        /// </summary>
        public void ShowMessage(string message, Brush background, string icon, int duration)
        {
            _messageText.Text = $"{icon} {message}";
            _messageBorder.Background = background;
            _buttonPanel.Visibility = Visibility.Collapsed; // 普通消息不显示按钮

            _popupMessage.IsOpen = true;

            // 设置定时器关闭 Popup
            Task.Delay(duration).ContinueWith(_ => Application.Current.Dispatcher.Invoke(() => _popupMessage.IsOpen = false));
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        public void ShowConfirm(string message, Brush background, string icon, string confirmText, string cancelText, Action onConfirm, Action onCancel)
        {
            _messageText.Text = $"{icon} {message}";
            _messageBorder.Background = background;
            _buttonPanel.Visibility = Visibility.Visible;

            _confirmButton.Content = confirmText;
            _cancelButton.Content = cancelText;

            _confirmButton.Click += (s, e) =>
            {
                _popupMessage.IsOpen = false;
                onConfirm?.Invoke();
            };

            _cancelButton.Click += (s, e) =>
            {
                _popupMessage.IsOpen = false;
                onCancel?.Invoke();
            };

            _popupMessage.IsOpen = true;
        }
    }

    public class MyMessage
    {
        private readonly MessagePopupHelper _popupHelper;

        public MyMessage(MessagePopupHelper popupHelper)
        {
            _popupHelper = popupHelper;
        }

        public void ShowSuccess(string message = "操作成功！", int duration = 2000)
        {
            ShowMessage(message, Brushes.LightGreen, "✔", duration);
        }

        public void ShowWarning(string message = "请注意！", int duration = 3000)
        {
            ShowMessage(message, Brushes.Orange, "⚠", duration);
        }

        public void ShowError(string message = "操作失败！", int duration = 5000)
        {
            ShowMessage(message, Brushes.IndianRed, "✖", duration);
        }

        public void ShowInfo(string message = "提示信息", int duration = 2000)
        {
            ShowMessage(message, Brushes.LightGray, "ℹ", duration);
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        public void ShowConfirm(string message, Action onConfirm, Action onCancel, string confirmText = "确定", string cancelText = "取消")
        {
            _popupHelper.ShowConfirm(message, Brushes.LightBlue, "❓", confirmText, cancelText, onConfirm, onCancel);
        }

        private void ShowMessage(string message, Brush background, string icon, int duration)
        {
            _popupHelper.ShowMessage(message, background, icon, duration);
        }
    }
}
