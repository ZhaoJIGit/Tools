using Notes.APP.Common;
using Notes.APP.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace Notes.APP.Models
{
    public class BasePage : Page
    {
        public MyMessage _Message;
        public ConfirmMessage _ConfirmMessage;

        public Window? _ParentWindow;
        public NoteService _NoteService;
        public BasePage()
        {
            // 监听页面加载事件
            this.Loaded += BasePage_Loaded; 
        }
        /// <summary>
        /// 页面加载完成后执行的操作
        /// </summary>
        private void BasePage_Loaded(object sender, RoutedEventArgs e)
        {
            _NoteService = NoteService.Instance; // 使用单例;
            _ParentWindow = Window.GetWindow(this);
            MessagePopupHelper popupHelper = new MessagePopupHelper(_ParentWindow);
            // 创建 MyMessage 实例并传入 MessagePopupHelper
            _Message = new MyMessage(popupHelper);

            ConfirmDialogHelper confirmDialogHelper = new ConfirmDialogHelper(_ParentWindow);
            _ConfirmMessage = new ConfirmMessage(confirmDialogHelper);
        }
    }
}
