using System.Configuration;
using System.Data;
using System.Windows;
using TaskMGPro.Helper;

namespace TaskMGPro
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 在应用程序启动时初始化数据库
            SQLiteHelper databaseHelper = new SQLiteHelper(); // 确保路径正确
        }
    }

}
