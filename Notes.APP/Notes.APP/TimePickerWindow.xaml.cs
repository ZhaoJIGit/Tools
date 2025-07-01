using Notes.APP.Common;
using Notes.APP.CustomCtrls;
using Notes.APP.Models;
using Notes.APP.Services;
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
using System.Windows.Shapes;
using System.Xml.Linq;

namespace Notes.APP
{
    /// <summary>
    /// TimePickerDialog.xaml 的交互逻辑
    /// </summary>
    public partial class TimePickerWindow : Window
    {
        SystemConfigInfo config;
        private Point _mouseDownPosition;
        private NoteModel _noteModel;
        private MyMessage myMessage;

        public TimePickerWindow()
        {
            InitializeComponent();
            MessagePopupHelper popupHelper = new MessagePopupHelper(this);

            // 创建 MyMessage 实例并传入 MessagePopupHelper
            myMessage = new MyMessage(popupHelper);
        }
        public void SetNote(NoteModel note)
        {
            _noteModel = note;
            SystemConfigInfoService systemConfig = SystemConfigInfoService.Instance;
            config = systemConfig.GetConfig();
            //borderTitle.Background = config.BackGroundColor.ToSolidColorBrush();
            //pageBorder.Background = config.BackGroundColor.ToSolidColorBrush();
            //txtTitle.Foreground= config.Color.ToSolidColorBrush();
            //btnClose.Foreground = config.Color.ToSolidColorBrush();
            var now = _noteModel.NoticeTime == null ? DateTime.Now.AddMinutes(15) : _noteModel.NoticeTime.Value;
            datePicker.Text = now.ToString("yyyy-MM-dd");
            timePicker.SelectedTime = now;

            // 更新 UI 显示
            this.DataContext = config;

            // 若有控件需要赋值也可以手动更新：
            // TimePicker.Value = _note?.ReminderTime;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //TimePicker.Text = DateTime.Now.ToString();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            var service = new NoteService();
            if (datePicker.Text == null || string.IsNullOrWhiteSpace(datePicker.Text))
            {
                myMessage.ShowWarning("请选择时间");
            }
            if (timePicker.Text == null || string.IsNullOrWhiteSpace(timePicker.Text))
            {
                myMessage.ShowWarning("请选择时间");
            }
            _noteModel.NoticeTime = Convert.ToDateTime(datePicker.Text +" "+ timePicker.Text.ToString());

            if (_noteModel.NoticeTime < DateTime.Now)
            {
                myMessage.ShowWarning("提醒时间不能小于当前时间");
            }
            _noteModel.UpdateTime = DateTime.Now;
            if (service.SaveNoteNotice(_noteModel))
            {


                this.Close(); // 隐藏窗口
            }
            else
            {
                myMessage.ShowError("保存失败！");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // 隐藏窗口
        }
        // 实现窗口拖动
        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (e.ChangedButton == MouseButton.Left)
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    _mouseDownPosition = e.GetPosition(this);
                }
                //this.DragMove(); // 拖动窗口
            }
            e.Handled = true;
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Hand; // 设置鼠标指针为十字箭头
        }

        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow; // 恢复默认箭头指针
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    // 获取当前鼠标位置
                    Point currentPosition = e.GetPosition(this);
                    // 判断鼠标移动距离（避免误触）
                    if (Math.Abs(currentPosition.X - _mouseDownPosition.X) > 10 ||
                        Math.Abs(currentPosition.Y - _mouseDownPosition.Y) > 10)
                    {
                        // 退出全屏，并调整窗口位置
                        if (this.WindowState == WindowState.Maximized)
                        {
                            this.WindowState = WindowState.Normal;

                            // 计算鼠标相对位置，保持窗口位置不跳变
                            this.Top = Math.Max(0, currentPosition.Y - 25);
                            this.Left = Math.Max(0, currentPosition.X - (this.Width / 2));
                        }
                        // 执行拖拽
                        this.DragMove();
                    }
                }
                else
                {
                    // 执行拖拽
                    this.DragMove();
                }
            }
        }

        // 关闭窗口
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // 隐藏窗口
        }

    }
}
