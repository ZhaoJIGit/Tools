using Newtonsoft.Json;
using System.IO;
using System.Windows;
using System.Windows.Controls;
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
        private string positionsFilePath =  "positions.json" ;

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
            if (FileList.SelectedItem==null) { return; }
            string fileName = FileList.SelectedItem.ToString();
            // Handle open button click logic
            NavigationService.Navigate(new ChapterPage(Path.Combine(cacheDirectory, fileName)));
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileList.SelectedItem == null) { return; }
            var fileName =  FileList.SelectedItem.ToString();
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
    }
}
