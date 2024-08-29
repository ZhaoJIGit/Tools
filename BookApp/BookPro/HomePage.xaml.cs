using Newtonsoft.Json;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Navigation;

namespace BookPro
{
    /// <summary>
    /// HomePage.xaml 的交互逻辑
    /// </summary>
    public partial class HomePage : Page
    {
        private string cacheDirectory; // 临时文件目录路径，请替换为你的临时文件目录
        private Dictionary<string, long> filePositions = new Dictionary<string, long>();
        private string positionsFilePath = "positions.json";
        public HomePage()
        {
            InitializeComponent();

            //FileList.MouseLeftButtonDown += FileList_MouseLeftButtonDown;
            //FileList.MouseRightButtonDown += FileList_MouseRightButtonDown;

            cacheDirectory = Path.Combine(Path.GetTempPath(), "BookPro", "Cache");

            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }
            LoadFilesFromCache();
            LoadPositionsFromJson();


        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SetColor();
        }
        private void LoadPositionsFromJson()
        {
            if (File.Exists(Path.Combine(cacheDirectory, positionsFilePath)))
            {
                string json = File.ReadAllText(Path.Combine(cacheDirectory, positionsFilePath));
                filePositions = JsonConvert.DeserializeObject<Dictionary<string, long>>(json);
            }
        }
        private void LoadFilesFromCache()
        {

            if (Directory.Exists(cacheDirectory))
            {
                string[] files = Directory.GetFiles(cacheDirectory, "*.txt");
                foreach (string file in files)
                {
                    // 将 ListBoxItem 添加到 ListBox 中
                    FileList.Items.Add(Path.GetFileName(file));
                }
            }
        }
        private void ImportTxt_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                string fileName = openFileDialog.SafeFileName;
                string filePath = openFileDialog.FileName;

                // 将文件复制到临时目录
                string tempFilePath = Path.Combine(cacheDirectory, fileName);
                File.Copy(filePath, tempFilePath, true);

                // 将文件名添加到 ListBox 中
                FileList.Items.Add(fileName);
                // 添加到位置记录中，默认位置为 0
                filePositions[fileName] = 0;
                SavePositionsToJson();
            }
        }
        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.SelectedItem == null) { return; }
            string fileName = FileList.SelectedItem.ToString();
            // Handle open button click logic
            NavigationService.Navigate(new ChapterPage(Path.Combine(cacheDirectory, fileName)));
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.SelectedItem == null) { return; }
            var fileName = FileList.SelectedItem.ToString();
            //string fileName = item.Content?.ToString();
            // Handle delete button click logicFileList.Items.Remove(fileName);
            // 删除关联的临时文件
            string filePath = Path.Combine(cacheDirectory, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            filePositions.Remove(fileName);
            FileList.Items.Clear();
            foreach (var file in filePositions)
            {
                FileList.Items.Add(file.Key);
            }
            SavePositionsToJson();
        }

        private void SavePositionsToJson()
        {
            string json = JsonConvert.SerializeObject(filePositions);

            File.WriteAllText(Path.Combine(cacheDirectory, positionsFilePath), json);
        }

        private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void YinNi_Click(object sender, RoutedEventArgs e)
        {
            // 获取屏幕上指定位置的颜色
            //System.Windows.Point screenLocation = new System.Windows.Point(100, 100); // 可以根据需要修改位置
            //System.Drawing.Color screenColor = GetScreenColorAt(screenLocation);

            //// 将System.Drawing.Color转换为WPF的Color
            //System.Windows.Media.Color wpfColor = System.Windows.Media.Color.FromArgb(screenColor.A, screenColor.R, screenColor.G, screenColor.B);
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
            FileList.Background = new SolidColorBrush(wpfColor);
            // 获取当前页面所在的窗口
            Window parentWindow = Window.GetWindow(this);
            // 根据颜色亮度决定字体颜色
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
            FileList.Foreground = fontColor;

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
    }
}
