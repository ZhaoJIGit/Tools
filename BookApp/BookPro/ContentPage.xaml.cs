using Newtonsoft.Json;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;


namespace BookPro
{
    /// <summary>
    /// ContentPage.xaml 的交互逻辑
    /// </summary>
    public partial class ContentPage : Page
    {
        private string[] pages;
        private int currentPage;
        private int linesPerPage = 1;
        private bool isContentRendered = false;
        private bool isPageCalculationPending = false;

        private string fileName;
        private string positionsFilePath = Path.Combine(Path.GetTempPath(), "BookPro", "Cache", "positions.json");
        private Dictionary<string, long> filePositions = new Dictionary<string, long>();
        public ContentPage(string fileName, string chapter, long position)
        {
            InitializeComponent();
            this.fileName = fileName;
            currentPage = (int)position;
            AdjustLinesPerPage();
            LoadNovel();
            isContentRendered = true;
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SetColor();
            this.Focus();  // 在页面加载时手动设置焦点
        }
        //private void LoadPositionFromJson()
        //{
        //    if (File.Exists(positionsFilePath))
        //    {
        //        string json = File.ReadAllText(positionsFilePath);
        //        var filePositions = JsonConvert.DeserializeObject<Dictionary<string, long>>(json);
        //        if (filePositions.ContainsKey(fileName))
        //        {
        //            position = filePositions[fileName];
        //        }
        //    }
        //}
        //private void LoadFile()
        //{
        //    // 读取文件内容
        //    using (StreamReader reader = new StreamReader(fileName))
        //    {
        //        // 设置文件指针到指定位置
        //        reader.BaseStream.Seek(position, SeekOrigin.Begin);

        //        // 显示文件内容
        //        FileContentTextBox.Text = reader.ReadToEnd();
        //    }
        //}

        private void LoadNovel()
        {
            if (File.Exists(fileName))
            {
                string content = File.ReadAllText(fileName, Encoding.UTF8);
                AdjustLinesPerPage();
                pages = SplitContentIntoPages(content, linesPerPage);

                if (currentPage == 0)
                {
                    if (File.Exists(positionsFilePath))
                    {
                        string positionContent = File.ReadAllText(positionsFilePath);
                        filePositions = JsonConvert.DeserializeObject<Dictionary<string, long>>(positionContent);
                        //if (int.TryParse(positionContent, out int savedPage))
                        //{
                        //    currentPage = savedPage;
                        //}
                        currentPage = (int)filePositions.Where(i => i.Key == Path.GetFileName(fileName)).FirstOrDefault().Value;
                    }
                }

                DisplayPage(currentPage);
                UpdateButtonsState(true);
            }
            else
            {
                UpdateButtonsState(false);
            }
        }
        private void YinNi_Click(object sender, RoutedEventArgs e)
        {
            SetColor();
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
            System.Windows.Point screenLocation = new System.Windows.Point(rect.Right, rect.Bottom -10);

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
            txtContent.Background = new SolidColorBrush(wpfColor);
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
            txtContent.Foreground = fontColor;


            btnPreviousPage.Background = new SolidColorBrush(wpfColor);
            btnPreviousPage.Foreground = fontColor;

            btnNextPage.Background = new SolidColorBrush(wpfColor);
            btnNextPage.Foreground = fontColor;

            btnIncreaseFont.Background = new SolidColorBrush(wpfColor);
            btnIncreaseFont.Foreground = fontColor;

            btnDecreaseFont.Background = new SolidColorBrush(wpfColor);
            btnDecreaseFont.Foreground = fontColor;

            btnBack.Background = new SolidColorBrush(wpfColor);
            btnBack.Foreground = fontColor;

            btnYinNi.Background = new SolidColorBrush(wpfColor);
            btnYinNi.Foreground = fontColor;

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
        private void txtContent_LayoutUpdated(object sender, EventArgs e)
        {
            if (isContentRendered && isPageCalculationPending)
            {
                AdjustLinesPerPage();
                ReloadContent();
                isPageCalculationPending = false;
            }
        }
        private string[] SplitContentIntoPages(string content, int linesPerPage = 1)
        {
            if (linesPerPage <= 0)
            {
                return new string[] { content };
            }

            string[] lines = content.Split(new[] { "\n" }, StringSplitOptions.None);
            lines = lines.Where(i => i.Trim() != string.Empty).ToArray();
            int pageCount = (int)Math.Ceiling((double)lines.Length / linesPerPage);
            string[] pages = new string[pageCount];

            for (int i = 0; i < pageCount; i++)
            {
                pages[i] = string.Join(Environment.NewLine, lines, i * linesPerPage, Math.Min(linesPerPage, lines.Length - i * linesPerPage));
            }

            return pages;
        }
        private void AdjustLinesPerPage()
        {
            if (txtContent.ActualHeight == 0)
            {
                isPageCalculationPending = true;
                return;
            }

            var fontSize = txtContent.FontSize;
            var lineHeight = fontSize * 1.2;
            var availableHeight = txtContent.ActualHeight - 20; // Adjust for padding/margin
            linesPerPage = 1;// (int)(availableHeight / lineHeight);
        }

        private void ReloadContent()
        {
            if (File.Exists(fileName))
            {
                string content = File.ReadAllText(fileName, Encoding.UTF8);
                pages = SplitContentIntoPages(content, linesPerPage);
                DisplayPage(currentPage);
            }
        }
        //取出40个字
        int pageSize = 25;
        int pageIndex = 0;
        bool isCurrentPage = false;
        string currentContent = "";
        private void DisplayPage(int pageNumber)
        {
            if (pages != null && pageNumber >= 0 && pageNumber < pages.Length)
            {
                if (!isCurrentPage)
                {
                    //pageIndex = 0;
                    currentContent = pages[pageNumber];
                }
               
                var length = currentContent.Length;
                if (currentContent.Length > pageSize)
                {
                    isCurrentPage = true;
                    length = pageSize;
                }
                var ss = currentContent.Length - (pageIndex * pageSize);
                if (ss > pageSize)
                {
                    length = pageSize;
                }
                else
                {
                    length = ss;
                    isCurrentPage = false;
                }
                txtContent.Text = currentContent.Substring(pageIndex * pageSize, length).TrimStart();
                filePositions[Path.GetFileName(fileName)] = pageNumber;
                SavePositionsToJson();

            }
        }
        private void SavePositionsToJson()
        {
            string json = JsonConvert.SerializeObject(filePositions);

            File.WriteAllText(positionsFilePath, json);
        }
        private void UpdateButtonsState(bool enabled)
        {
            btnPreviousPage.IsEnabled = enabled;
            btnNextPage.IsEnabled = enabled;
            btnIncreaseFont.IsEnabled = enabled;
            btnDecreaseFont.IsEnabled = enabled;
        }
        private void btnPreviousPage_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 0)
            {
                //回看需要重新计算
                if (pageIndex == 0)
                {
                    isCurrentPage = false;
                    currentPage--;
                }
                else {
                    pageIndex--;
                }
               
                DisplayPage(currentPage);
            }
        }

        private void btnNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage < pages.Length - 1)
            {
                if (!isCurrentPage)
                {
                    currentPage++;
                }
                DisplayPage(currentPage);
            }
        }

        private void btnIncreaseFont_Click(object sender, RoutedEventArgs e)
        {
            txtContent.FontSize += 1;
            AdjustLinesPerPage();
            ReloadContent();
        }

        private void btnDecreaseFont_Click(object sender, RoutedEventArgs e)
        {
            if (txtContent.FontSize > 1)
            {
                txtContent.FontSize -= 1;
                AdjustLinesPerPage();
                ReloadContent();
            }
        }
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ChapterPage(fileName));
        }
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (isContentRendered)
            {
                AdjustLinesPerPage();
                ReloadContent();
            }
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Add:
                    // 处理向上键
                    txtContent.FontSize += 1;
                    AdjustLinesPerPage();
                    ReloadContent();
                    break;
                case Key.Subtract:
                    // 处理向下键
                    if (txtContent.FontSize > 1)
                    {
                        txtContent.FontSize -= 1;
                        AdjustLinesPerPage();
                        ReloadContent();
                    }
                    break;
                case Key.Left:
                    // 处理向左键
                    if (currentPage > 0)
                    {
                        //回看需要重新计算
                        if (pageIndex == 0)
                        {
                            isCurrentPage = false;
                            currentPage--;
                        }
                        else
                        {
                            pageIndex--;
                            isCurrentPage = true;
                        }

                        DisplayPage(currentPage);
                    }
                    break;
                case Key.Right:
                    // 处理向右键
                    if (currentPage < pages.Length - 1)
                    {
                        if (!isCurrentPage)
                        {
                            pageIndex = 0;
                            currentPage++;
                        }
                        else {
                            pageIndex++;
                        }
                        DisplayPage(currentPage);
                    }
                    break;
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
                    NavigationService.Navigate(new ChapterPage(fileName));
                    break;
            }
            this.Focus();

        }
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Initiates dragging of the window
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Window.GetWindow(this).DragMove();
            }
        }
    }
}
