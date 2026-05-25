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

namespace PublishDesk
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainViewModel mainView;
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            mainView = (MainViewModel)DataContext;

            mainView.Logs.CollectionChanged += async (s, e) =>
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    LogScroll.ScrollToEnd();
                }, System.Windows.Threading.DispatcherPriority.Background);
            };
        }
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await mainView.LoadSitesAsync();
        }

    }
}