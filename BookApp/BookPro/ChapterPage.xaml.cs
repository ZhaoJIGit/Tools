using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
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
                    if (line.TrimStart().StartsWith("☆、") ||( line.TrimStart().StartsWith("第") && (line.TrimStart().Contains("章")|| line.TrimStart().Contains("卷"))))
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
    }
}
