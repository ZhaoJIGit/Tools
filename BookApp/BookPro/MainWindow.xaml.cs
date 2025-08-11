using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BookPro
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // 默认显示 Page1
            MainFrame.Navigate(new HomePage());
            // 让窗口不在任务栏显示
            this.ShowInTaskbar = false;

        }

        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //var title = this.Template.FindName("title", this) as TextBlock;
            //if (title != null)
            //{
            //    title.Background = new SolidColorBrush(Colors.Green);
            //}
            // 找到 TitleStyle Border
            //var titleBorder = (Border)Template.FindName("TitleStyle", this);

            //if (titleBorder != null)
            //{
            //    // 创建新的 SolidColorBrush
            //    var newBrush = new SolidColorBrush(Colors.Green);
            //    titleBorder.Background = newBrush; // 应用新的颜色
            //}
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Close the window when the button is clicked
        }
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Initiates dragging of the window
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized; // Minimize the window
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle between maximized and normal state
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //switch (e.Key)
            //{
            //    case Key.Enter:
            //        // 处理向右键
            //        SetColor();
            //        break;
            //    case Key.Down:
            //        Window.GetWindow(this).WindowState = WindowState.Minimized;
            //        break;
            //    case Key.Escape:
            //        Window.GetWindow(this).Close();
            //        break;
            //}
        }
        private void SetColor()
        {
            // 获取屏幕上指定位置的颜色
            //System.Windows.Point screenLocation = new System.Windows.Point(100, 100); // 可以根据需要修改位置
            //System.Drawing.Color screenColor = GetScreenColorAt(screenLocation);

            //// 将System.Drawing.Color转换为WPF的Color
            //System.Windows.Media.Color wpfColor = System.Windows.Media.Color.FromArgb(screenColor.A, screenColor.R, screenColor.G, screenColor.B);


            // 获取当前应用窗口的位置和大小
            var hwnd = new WindowInteropHelper(Window.GetWindow(this)).Handle;
            RECT rect;
            GetWindowRect(hwnd, out rect);

            // 计算下方位置（假设向下20像素的位置）
            System.Windows.Point screenLocation = new System.Windows.Point(rect.Right, rect.Bottom + 20);

            // 获取下方位置的颜色
            System.Drawing.Color screenColor = GetScreenColorAt(screenLocation);

            if (screenColor.A == 0 && screenColor.B == 0 && screenColor.G == 0 && screenColor.R == 0)
            {
                return;
            }
            // 将System.Drawing.Color转换为WPF的Color
            System.Windows.Media.Color wpfColor = System.Windows.Media.Color.FromArgb(screenColor.A, screenColor.R, screenColor.G, screenColor.B);

            // 将窗口背景色设置为获取到的颜色
            this.Background = new SolidColorBrush(wpfColor);
            // 获取当前页面所在的窗口
        }
        // 判断颜色是否为深色
        private bool IsDarkColor(System.Windows.Media.Color color)
        {
            double luminance = 0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B;
            return luminance < 128; // Luminance threshold to consider a color dark
        }
        // Windows API函数声明
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        // 获取屏幕指定位置的颜色
        public static System.Drawing.Color GetScreenColorAt(System.Windows.Point location)
        {
            using (Bitmap screenshot = new Bitmap(1, 1, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(screenshot))
                {
                    g.CopyFromScreen((int)location.X, (int)location.Y, 0, 0, new System.Drawing.Size(1, 1));
                }
                return screenshot.GetPixel(0, 0);
            }
        }

        private void TrayIcon_DoubleClick(object sender, RoutedEventArgs e)
        {
            if (this.Visibility == Visibility.Visible)
            {
                //this.Hide();
                this.Visibility= Visibility.Hidden;
            }
            else
            {
                this.Visibility = Visibility.Visible;
                this.Activate();
            
            }
        }
    }
}