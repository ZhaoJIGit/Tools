using Notes.APP.CustomCtrls;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Notes.APP.Common
{
    public class ConfirmDialogHelper
    {
        private Window _window;
        private ConfirmPopup _confirmPopup;
        public ConfirmDialogHelper(Window window)
        {
            _window= window;
            // 创建 ConfirmPopup 实例
            _confirmPopup = new ConfirmPopup();
            // 将 UserControl 添加到窗口
            // 直接将 _confirmPopup 添加到窗口的布局中，例如 Grid 或其他容器
            var mainGrid = _window.Content as Grid;
            if (mainGrid != null)
            {
                mainGrid.Children.Add(_confirmPopup);
            }
            // 将 UserControl 添加到窗口
            //_window.Content = _confirmPopup;
        }
        public void ShowConfirm(string message, Brush background, string icon, string confirmText, string cancelText, Action onConfirm, Action onCancel)
        {
            _confirmPopup.ShowConfirm(_window,message, background, icon, confirmText, cancelText, onConfirm, onCancel);
    
        }
    }

    public class ConfirmMessage
    {
        private readonly ConfirmDialogHelper _popupHelper;

        public ConfirmMessage(ConfirmDialogHelper popupHelper)
        {
            _popupHelper = popupHelper;
        }
        /// <summary>
        /// 显示确认对话框
        /// </summary>
        public void ShowConfirm(string message, Action onConfirm, Action onCancel = null, string confirmText = "确定", string cancelText = "取消")
        {
            _popupHelper.ShowConfirm(message, Brushes.LightBlue, "❓", confirmText, cancelText, onConfirm, onCancel);
        }
    }
}
