using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using PublishTool.Models;
using System.IO;
using System.Windows;
using System.Xml.Linq;

namespace PublishTool
{
    /// <summary>
    /// AddSiteWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AddSiteWindow : Window
    {
        private string JsonFileName = "SiteData.json";
        private SiteModel _model;
        private List<SiteModel> _list;
        private bool _isEdit = false;

        public AddSiteWindow(List<SiteModel> list)
        {
            InitializeComponent();
            DataContext = _model;
            _list = list;
        }

        public AddSiteWindow(List<SiteModel> list, SiteModel model) : this(list)
        {
            _model = model;
            _isEdit = true;
            DataContext = _model;
            LoadData();
        }
        private void LoadData()
        {
            if (_model == null) return;

            txtName.Text = _model.Name;
            txtFilePath.Text = _model.FilePath;
            txtSitePath.Text = _model.SitePath;
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();

            dialog.IsFolderPicker = true;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string path = dialog.FileName;

                txtSitePath.Text = path;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();

            dialog.IsFolderPicker = true;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string path = dialog.FileName;

                txtFilePath.Text = path;
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_isEdit)
                {
                    // 修改模式
                    _model.Name = txtName.Text;
                    _model.FilePath = txtFilePath.Text;
                    _model.SitePath = txtSitePath.Text;
                }
                else
                {
                    // 新增模式
                    _list.Add(new SiteModel
                    {
                        Name = txtName.Text,
                        FilePath = txtFilePath.Text,
                        SitePath = txtSitePath.Text
                    });
                }

                File.WriteAllText(
                    "SiteData.json",
                    Newtonsoft.Json.JsonConvert.SerializeObject(_list, Newtonsoft.Json.Formatting.Indented));

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
