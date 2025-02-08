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

namespace Notes.APP.CustomCtrls
{
    /// <summary>
    /// ConfirmPopup.xaml 的交互逻辑
    /// </summary>
    public partial class ConfirmPopup : UserControl
    {
        public ConfirmPopup()
        {
            InitializeComponent();
        }
        public void ShowConfirm(Window window ,string message, Brush background, string icon, string confirmText, string cancelText, Action onConfirm, Action onCancel)
        {
            messageText.Text = $"{message}";
            messageIcon.Text = icon;
            messageIcon.Foreground = background;
            buttonPanel.Visibility = Visibility.Visible;
            // 设置 Popup 位置
            
            var position = window.PointToScreen(new Point(window.Width / 2, window.Height / 2));
            popupMessage.HorizontalOffset = position.X - popupMessage.Width / 2;
            popupMessage.VerticalOffset = position.Y - popupMessage.Height / 2;
            confirmButton.Content = confirmText;
            cancelButton.Content = cancelText;

            confirmButton.Click += (s, e) =>
            {
                popupMessage.IsOpen = false;
                onConfirm?.Invoke();
            };

            cancelButton.Click += (s, e) =>
            {
                popupMessage.IsOpen = false;
                onCancel?.Invoke();
            };

            popupMessage.IsOpen = true;
        }
    }
}
