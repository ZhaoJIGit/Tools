using Microsoft.Win32;
using Notes.APP.Common;
using Notes.APP.Models;
using Notes.APP.Pages;
using Notes.APP.Services;
using System;
using System.ComponentModel;
using System.IO;
using System.Printing;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Notes.APP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // 定义静态事件
        public static event EventHandler ReloadWindow;
        public event Action<string>? ColorChanged;
        private Point _mouseDownPosition;
        private bool _isDrawerOpen = false;
        private NoteModel _noteModel;
        private MyMessage myMessage;
        // 记录折叠前的高度（像素）
        private double previousMiddleRowHeight = 200; // 默认200
        // 标识中间行是否已经折叠
        private bool isCollapsed = false;
        public MainWindow(NoteModel noteModel)
        {
            InitializeComponent();
            _noteModel = noteModel;
            this.DataContext = _noteModel;
            // 创建并初始化 MessagePopupHelper
            MessagePopupHelper popupHelper = new MessagePopupHelper(this);

            // 创建 MyMessage 实例并传入 MessagePopupHelper
            myMessage = new MyMessage(popupHelper);

            // 默认显示 Page1
            var page = new HomePage();
            ColorChanged += page.OnColorChanged;
            MainFrame.Navigate(page);
        }
        public void ReloadData()
        {
            var model = NoteService.Instance.GetNote(_noteModel.NoteId);
            if (model!=null) {
                _noteModel = model;
                this.DataContext = model;
                ReloadPage();
            }
        }
        private void ReloadPage() {
            if (_noteModel.Fixed)
            {
                btnFixed.Content = "\uE840";
            }
            else
            {
                btnFixed.Content = "\uE718";
            }
            this.Topmost = _noteModel.Fixed;
            //isTopUpBox.IsChecked = _noteModel.IsTopUp;

            pageBorder.Background = _noteModel.BackgroundColor.ToSolidColorBrush();
            MyColorPicker.SelectedColor = _noteModel.BackgroundColor.ToColor();
            MyFontColorPicker.SelectedColor = _noteModel.Color.ToColor();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 设置 DataContext
            var service = new NoteService();
            //_noteModel = service.SelectNote("b2a642c94a654175b455cb2337b1012d");
            if (_noteModel == null)
            {
                MessageBox.Show("便签不存在！");
                return;
            }
            ReloadPage();

            this.Width = _noteModel.Width;
            this.Height = _noteModel.Height;
            if (_noteModel.XAxis > 0 || _noteModel.YAxis > 0)
            {
                this.Left = _noteModel.XAxis;
                this.Top = _noteModel.YAxis;
            }
            // 窗口加载后，记录初始高度（此时布局已完成）
            previousMiddleRowHeight = gridContent.ActualHeight;
        }
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            this.Topmost = _noteModel.Fixed;
            _noteModel.XAxis = this.Left;
            _noteModel.YAxis = this.Top;
            SaveNote();
        }
        // INotifyPropertyChanged 实现
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        // 最小化窗口
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_noteModel.StatusTag)
            {
                this.Close();
            }
            else
            {
                this.Hide(); // 隐藏窗口
            }
            //this.WindowState = WindowState.Minimized;
        }

        // 最大化或还原窗口
        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
                ((Button)sender).Content = "🗗"; // 还原图标
            }
            else
            {
                this.WindowState = WindowState.Normal;
                ((Button)sender).Content = "□"; // 最大化图标
            }
        }

        // 关闭窗口
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            //this.Hide(); // 隐藏窗口
            this.Close();
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
            }
            e.Handled = true;
        }

        //gpt 帮我写一个wpf的按钮事件，需要实现点击按钮导出文件，将指定内容写入文件中，最后弹出选择保存地址
        private void Export_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Create a SaveFileDialog to choose file location
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                DefaultExt = ".txt",
                FileName = _noteModel?.NoteName?.Length == 0 ? "ExportedFile.txt" : _noteModel?.NoteName
            };

            // Show dialog and check if user clicked OK
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Specify content to be written
                    string content = _noteModel?.Content;

                    // Write content to selected file
                    File.WriteAllText(saveFileDialog.FileName, content);

                    myMessage.ShowSuccess("文件导出成功！");

                }
                catch (Exception ex)
                {
                    myMessage.ShowError("文件导出失败！");
                }
            }
        }

        private void More_Click(object sender, RoutedEventArgs e)
        {
            // 根据当前状态切换抽屉的打开或关闭
            if (_isDrawerOpen)
            {
                // 关闭抽屉
                DrawerPanel.Visibility = Visibility.Collapsed;
                CloseArea.Visibility = Visibility.Collapsed;  // 隐藏遮罩层
            }
            else
            {
                // 打开抽屉
                DrawerPanel.Visibility = Visibility.Visible;
                CloseArea.Visibility = Visibility.Visible;  // 显示遮罩层
            }

            // 切换状态
            _isDrawerOpen = !_isDrawerOpen;
        }

        private void CloseArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 关闭抽屉
            CloseArea.Visibility = Visibility.Collapsed;  // 隐藏遮罩层
            DrawerPanel.Visibility = Visibility.Collapsed;

            _isDrawerOpen = false;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_noteModel != null)
            {
                //_noteModel.Opacity = e.NewValue;
                _noteModel.BackgroundColor = ColorHelper.MakeColorTransparent(_noteModel.BackgroundColor.ToColor(), e.NewValue).ToHexColor();

                SaveNote();
            }
        }
        private void SaveNote()
        {
            var service = new NoteService();
            if (service.SaveNote(_noteModel))
            {
                Console.WriteLine("保存成功");
                // 触发事件，通知 Window A
                ReloadWindow?.Invoke(this, EventArgs.Empty);
            }
        }
        public void ChangedTextEvent()
        {
            ReloadWindow?.Invoke(this, EventArgs.Empty);
        }
        private void ResizeHandle_DragDelta(object sender, DragDeltaEventArgs e)
        {
            // 获取当前窗口
            var window = this;

            // 调整窗口的宽度和高度
            window.Width = Math.Max(window.MinWidth, window.Width + e.HorizontalChange);
            if (isCollapsed)
            {
                _noteModel.Width = window.Width;
            }
            else
            {
                window.Height = Math.Max(window.MinHeight, window.Height + e.VerticalChange);

                _noteModel.Height = window.Height;
                _noteModel.Width = window.Width;
            }
            SaveNote();
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
                // 获取当前窗口位置
                _noteModel.XAxis = this.Left;
                _noteModel.YAxis = this.Top;
                SaveNote();
            }
        }
        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Hand; // 设置鼠标指针为十字箭头
        }

        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow; // 恢复默认箭头指针
        }

        private void ColorCanvas_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e != null && e.NewValue != null)
            {
                Color colorWithOpacity = ColorHelper.MakeColorTransparent(e.NewValue.Value, 0.7);
                _noteModel.PageBackgroundColor = colorWithOpacity.ToHexColor();
                _noteModel.BackgroundColor = e.NewValue.Value.ToHexColor();
                // _noteModel.Color = ColorHelper.GetColorByBackground(_noteModel.BackgroundColor);
                SaveNote();
            }
        }

        private void OnTrayOpenClick(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void OnTrayExitClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            if (this.WindowState == WindowState.Minimized)
            {
                this.Hide(); // 隐藏窗口
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            //TrayIcon.Dispose(); // 清理托盘图标资源
            base.OnClosed(e);
        }

        private void TrayIcon_TrayLeftMouseUp(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StackPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ConfirmDialogHelper confirmDialogHelper = new ConfirmDialogHelper(this);
            ConfirmMessage confirmMessage = new ConfirmMessage(confirmDialogHelper);
            confirmMessage.ShowConfirm("确认删除吗？", () =>
            {
                // 删除逻辑
                var service = new NoteService();
                service.DeleteNote(_noteModel!.NoteId);
                this.Close();
                // 触发事件，通知 Window A
                ReloadWindow?.Invoke(this, EventArgs.Empty);
            });

        }
        /// <summary>
        /// 查看列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StackPanel_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
        {
            var windows = Application.Current.Windows.OfType<Window>().FirstOrDefault(i => i.Name == "listWindow");
            if (windows != null)
            {
                windows.Show();
                windows.WindowState = WindowState.Normal;
                windows.Activate();
            }
        }

        private void Fix_Click(object sender, RoutedEventArgs e)
        {
            _noteModel.Fixed = !_noteModel.Fixed;
            if (_noteModel.Fixed)
            {
                btnFixed.Content = "\uE840";
            }
            else
            {

                btnFixed.Content = "\uE718";
            }
            this.Topmost = _noteModel.Fixed;
            SaveNote();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.D) &&
         ((Keyboard.Modifiers & ModifierKeys.Windows) == ModifierKeys.Windows))
            {
                // 如果按下的是 Win + D，取消默认处理
                e.Handled = true;
            }
        }

        private void CollapseButton_Click(object sender, RoutedEventArgs e)
        {
            if (isCollapsed)
            {
                // 展开操作
                // 构造展开动画：从 0 像素到之前记录的高度
                var expandAnimation = new GridLengthAnimation
                {
                    From = new GridLength(0, GridUnitType.Pixel),
                    To = new GridLength(previousMiddleRowHeight, GridUnitType.Pixel),
                    Duration = new Duration(TimeSpan.FromSeconds(0.3))
                };

                expandAnimation.Completed += (s, a) =>
                {
                    // 动画完成后，取消动画并恢复为自适应模式
                    gridContent.BeginAnimation(RowDefinition.HeightProperty, null);
                    gridContent.Height = new GridLength(1, GridUnitType.Star);
                };

                gridContent.BeginAnimation(RowDefinition.HeightProperty, expandAnimation);
                btnCollapse.Content = "\uE70D"; // 更新按钮图标
                isCollapsed = false;
            }
            else
            {
                // 折叠操作
                // 记录当前行高度（用作展开时的目标高度）
                previousMiddleRowHeight = gridContent.ActualHeight;
                // 为了动画，先将 Height 固定为像素值
                gridContent.Height = new GridLength(previousMiddleRowHeight, GridUnitType.Pixel);

                // 构造折叠动画：从当前高度到 0
                var collapseAnimation = new GridLengthAnimation
                {
                    From = new GridLength(previousMiddleRowHeight, GridUnitType.Pixel),
                    To = new GridLength(1, GridUnitType.Pixel),
                    Duration = new Duration(TimeSpan.FromSeconds(0.3))
                };

                gridContent.BeginAnimation(RowDefinition.HeightProperty, collapseAnimation);
                btnCollapse.Content = "\uE70E"; // 更新按钮图标
                isCollapsed = true;
            }
        }

        private void TextButton_Click(object sender, RoutedEventArgs e)
        {
            // 创建便签窗口实例
            var note = NoteModel.CreateNote();
            MainWindow stickyNoteWindow = new MainWindow(note);
            stickyNoteWindow.Tag = note.NoteId;
            stickyNoteWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            stickyNoteWindow.Height = note.Height;
            stickyNoteWindow.Width = note.Width;
            // 打开便签窗口
            stickyNoteWindow.Show();

            // 触发事件，通知 Window A
            ReloadWindow?.Invoke(this, EventArgs.Empty);

        }

        private void FontColorCanvas_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e != null && e.NewValue != null)
            {
                Color colorWithOpacity = ColorHelper.MakeColorTransparent(e.NewValue.Value, 1);
                _noteModel.Color = colorWithOpacity.ToHexColor();
                SaveNote();
                ColorChanged?.Invoke(_noteModel.Color);
            }
        }

        //private void PreviewSlider_SizeChanged(object sender, SizeChangedEventArgs e)
        //{
        //    if (_noteModel != null)
        //    {
        //        _noteModel.Opacity = e.NewSize.Width;
        //        _noteModel.BackgroundColor= ColorHelper.MakeColorTransparent(_noteModel.BackgroundColor.ToColor(), e.NewSize.Width).ToHexColor();
        //        SaveNote();
        //    }
        //}

        private void PreviewSlider_SizeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_noteModel != null)
            {
                _noteModel.Opacity = e.NewValue;
                _noteModel.BackgroundColor = ColorHelper.MakeColorTransparent(_noteModel.BackgroundColor.ToColor(), e.NewValue).ToHexColor();
                _noteModel.PageBackgroundColor = _noteModel.BackgroundColor;
                SaveNote();
            }
        }
    }
    /// <summary>
    /// 自定义 GridLength 动画类，用于动画 RowDefinition.Height 属性
    /// </summary>
    public class GridLengthAnimation : AnimationTimeline
    {
        public override Type TargetPropertyType => typeof(GridLength);

        public GridLength From
        {
            get { return (GridLength)GetValue(FromProperty); }
            set { SetValue(FromProperty, value); }
        }
        public static readonly DependencyProperty FromProperty =
            DependencyProperty.Register("From", typeof(GridLength), typeof(GridLengthAnimation));

        public GridLength To
        {
            get { return (GridLength)GetValue(ToProperty); }
            set { SetValue(ToProperty, value); }
        }
        public static readonly DependencyProperty ToProperty =
            DependencyProperty.Register("To", typeof(GridLength), typeof(GridLengthAnimation));

        public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
        {
            // 取得 From 与 To 中的 Value 值
            double fromVal = ((GridLength)this.From).Value;
            double toVal = ((GridLength)this.To).Value;
            double progress = animationClock.CurrentProgress.Value;
            double currentVal = (toVal - fromVal) * progress + fromVal;
            return new GridLength(currentVal, GridUnitType.Pixel);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new GridLengthAnimation();
        }
    }
}