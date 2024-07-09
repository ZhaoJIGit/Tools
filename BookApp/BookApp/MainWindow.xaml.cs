using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;

namespace BookApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string[] pages;
        private int currentPage;
        private int linesPerPage;
        private string tempFilePath = Path.Combine(Path.GetTempPath(), "current_novel.txt");
        private string positionFilePath = Path.Combine(Path.GetTempPath(), "novel_position.txt");
        private bool isContentRendered = false;
        private bool isPageCalculationPending = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            AdjustLinesPerPage();
            LoadNovel();
            isContentRendered = true;
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

        private void LoadNovel()
        {
            if (File.Exists(tempFilePath))
            {
                string content = File.ReadAllText(tempFilePath, Encoding.UTF8);
                AdjustLinesPerPage();
                pages = SplitContentIntoPages(content, linesPerPage);

                if (File.Exists(positionFilePath))
                {
                    string positionContent = File.ReadAllText(positionFilePath);
                    if (int.TryParse(positionContent, out int savedPage))
                    {
                        currentPage = savedPage;
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

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string content = File.ReadAllText(openFileDialog.FileName, Encoding.UTF8);
                File.WriteAllText(tempFilePath, content, Encoding.UTF8);
                AdjustLinesPerPage();
                pages = SplitContentIntoPages(content, linesPerPage);
                currentPage = 0;
                DisplayPage(currentPage);

                File.WriteAllText(positionFilePath, currentPage.ToString());
                UpdateButtonsState(true);
            }
        }

        private string[] SplitContentIntoPages(string content, int linesPerPage)
        {
            if (linesPerPage <= 0)
            {
                return new string[] { content };
            }

            string[] lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            int pageCount = (int)Math.Ceiling((double)lines.Length / linesPerPage);
            string[] pages = new string[pageCount];

            for (int i = 0; i < pageCount; i++)
            {
                pages[i] = string.Join(Environment.NewLine, lines, i * linesPerPage, Math.Min(linesPerPage, lines.Length - i * linesPerPage));
            }

            return pages;
        }

        private void DisplayPage(int pageNumber)
        {
            if (pages != null && pageNumber >= 0 && pageNumber < pages.Length)
            {
                txtContent.Text = pages[pageNumber];
                File.WriteAllText(positionFilePath, pageNumber.ToString());
            }
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

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (isContentRendered)
            {
                AdjustLinesPerPage();
                ReloadContent();
            }
        }
        private void btnClearCache_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }

            if (File.Exists(positionFilePath))
            {
                File.Delete(positionFilePath);
            }

            txtContent.Clear();
            UpdateButtonsState(false);
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
            linesPerPage = (int)(availableHeight / lineHeight);
        }

        private void ReloadContent()
        {
            if (File.Exists(tempFilePath))
            {
                string content = File.ReadAllText(tempFilePath, Encoding.UTF8);
                pages = SplitContentIntoPages(content, linesPerPage);
                DisplayPage(currentPage);
            }
        }

        private void UpdateButtonsState(bool enabled)
        {
            btnPreviousPage.IsEnabled = enabled;
            btnNextPage.IsEnabled = enabled;
            btnIncreaseFont.IsEnabled = enabled;
            btnDecreaseFont.IsEnabled = enabled;
        }
    }
}