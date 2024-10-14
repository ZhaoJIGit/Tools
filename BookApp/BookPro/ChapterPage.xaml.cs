using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using static System.Net.Mime.MediaTypeNames;

namespace BookPro
{
    /// <summary>
    /// ChapterPage.xaml 的交互逻辑
    /// </summary>
    public partial class ChapterPage : Page
    {
        private string cacheDirectory;
        private string fileName;
        private Dictionary<string, int> chapterPositions = new Dictionary<string, int>();
        private string positionsFilePath = "positions.json";
        private Dictionary<string, long> filePositions = new Dictionary<string, long>();

        public ChapterPage(string fileName)
        {
            InitializeComponent();
            this.fileName = fileName;
            cacheDirectory = Path.Combine(Path.GetTempPath(), "BookPro", "Cache");
            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }
            LoadChapterPositions();
            LoadPositionsFromJson();
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus();
            SetColor();
        }
        private void SetColor()
        {
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
            LineListBox.Background = new SolidColorBrush(wpfColor);
            // 获取当前页面所在的窗口
            Window parentWindow = Window.GetWindow(this);
            System.Windows.Media.Brush fontColor = IsDarkColor(wpfColor) ? System.Windows.Media.Brushes.White : System.Windows.Media.Brushes.Black;

            if (parentWindow != null)
            {
                // 设置窗口背景色
                parentWindow.Background = new SolidColorBrush(wpfColor); // 或者设置为任何你想要的颜色
                var title = parentWindow.Template.FindName("title", parentWindow) as TextBlock;
                var btnclose = parentWindow.Template.FindName("btnClose", parentWindow) as Button;
                var btnFD = parentWindow.Template.FindName("btnFD", parentWindow) as Button;
                var btnSX = parentWindow.Template.FindName("btnSX", parentWindow) as Button;
                if (title != null)
                {
                    title.Background = new SolidColorBrush(wpfColor);
                    btnclose.Background = new SolidColorBrush(wpfColor);
                    btnFD.Background = new SolidColorBrush(wpfColor);
                    btnSX.Background = new SolidColorBrush(wpfColor);

                    btnclose.Foreground = fontColor;
                    btnFD.Foreground = fontColor;
                    btnSX.Foreground = fontColor;
                }
            }
            // 根据颜色亮度决定字体颜色
            LineListBox.Foreground = fontColor;

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
        private void LoadPositionsFromJson()
        {
            if (File.Exists(Path.Combine(cacheDirectory, positionsFilePath)))
            {
                string json = File.ReadAllText(Path.Combine(cacheDirectory, positionsFilePath));
                if (!string.IsNullOrWhiteSpace(json))
                {
                    filePositions = JsonConvert.DeserializeObject<Dictionary<string, long>>(json);
                }
            }
        }
        private void ContinueReadingButton_Click(object sender, RoutedEventArgs e)
        {
            long lineNumber = filePositions[LineListBox.SelectedItem.ToString()];
            NavigationService.Navigate(new ContentPage(fileName, LineListBox.SelectedItem?.ToString(), lineNumber));
        }
        private void LoadChapterPositions()
        {
            try
            {

                string content = File.ReadAllText(fileName, Encoding.UTF8);

                string[] lines = content.Split(new[] { "\n" }, StringSplitOptions.None);
                int lineNumber = 0;
                Regex regex = new Regex(@"第\d");

                foreach (string line in lines.Where(i => i.Trim() != string.Empty))
                {
                    //Match match = regex.Match(line);
                    if (line.TrimStart().StartsWith("☆、") || (line.TrimStart().StartsWith("第") && (line.TrimStart().Contains("章") || line.TrimStart().Contains("卷"))))
                    {
                        string chapterName = line.Trim();
                        chapterPositions[chapterName] = lineNumber;
                        LineListBox.Items.Add(chapterName);
                    }
                    lineNumber++;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法加载章节信息：" + ex.Message);
            }
        }
        private void LineListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 获取选中的行号
            int selectedIndex = LineListBox.SelectedIndex;
            if (selectedIndex >= 0)
            {
                // 获取选中的行内容
                string chapter = (string)LineListBox.SelectedItem;

                // 获取选中的行号
                long position = chapterPositions.ContainsKey(chapter) ? chapterPositions[chapter] : 0;

                // 跳转到 Page3 并传递文件名、行内容和行位置
                NavigationService.Navigate(new ContentPage(fileName, chapter, position));
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new HomePage());
        }
        private void Jump_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LineListBox.SelectedIndex = Convert.ToInt32(txt.Text);
            }
            catch (Exception)
            {
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    // 处理向右键
                    SetColor();
                    break;
                case Key.Down:
                    Window.GetWindow(this).WindowState = WindowState.Minimized;
                    break;
                case Key.Up:
                    Window.GetWindow(this).WindowState = WindowState.Normal;
                    break;
                case Key.Escape:
                    Window.GetWindow(this).Close();
                    break;
                case Key.Back:
                    NavigationService.Navigate(new HomePage());
                    break;
            }
            this.Focus();
        }
    }
}
