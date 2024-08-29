using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
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
    }
}