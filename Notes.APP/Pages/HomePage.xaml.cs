using Notes.APP.Common;
using Notes.APP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Notes.APP.Pages
{
    /// <summary>
    /// HomePage.xaml 的交互逻辑
    /// </summary>
    public partial class HomePage : Page
    {
        private NoteModel? pageModel;
        public HomePage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var parentWindow = Window.GetWindow(this);
            pageModel = parentWindow.DataContext as NoteModel;
            SetBackgroundColor(pageModel?.BackgroundColor);
            if (parentWindow != null)
            {
                pageModel.BackgroundColorChanged += OnBackgroundColorChanged; // 订阅事件
            }
        }
        private void OnBackgroundColorChanged(string newColor)
        {
            SetBackgroundColor(newColor);
        }
        private void SetBackgroundColor(string color) {

            //pageConent.Background = pageModel.PageBackgroundColor.ToSolidColorBrush();
            txtConent.Background = pageModel.PageBackgroundColor.ToSolidColorBrush();
            txtConent.Foreground = ColorHelper.GetColorByBackground(color).ToSolidColorBrush();
        }
    }
}
