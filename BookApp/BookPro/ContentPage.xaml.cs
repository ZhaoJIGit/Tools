using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;


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
        private void DisplayPage(int pageNumber)
        {
            if (pages != null && pageNumber >= 0 && pageNumber < pages.Length)
            {
                txtContent.Text = pages[pageNumber];
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
                currentPage--;
                DisplayPage(currentPage);
            }
        }

        private void btnNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage < pages.Length - 1)
            {
                currentPage++;
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


    }
}
