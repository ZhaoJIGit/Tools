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
using System.Windows.Shapes;
using TaskMGPro.Models;

namespace TaskMGPro
{
    /// <summary>
    /// PopupWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PopupWindow : Window
    {
        public PopupWindow()
        {
            InitializeComponent();
            BaseWindow baseWindow = new BaseWindow();
            this.Height = baseWindow.Height;
            this.Width = baseWindow.Width;
        }
        public PopupWindow(BaseWindow baseWindow)
        {
            InitializeComponent();
            this.Height = baseWindow.Height;
            this.Width = baseWindow.Width;
        }
        // 打开弹窗时，加载指定的Page，并传递参数
        public void LoadPageWithParameters(Page page)
        {
            frame.Navigate(page);
        }
    }
}
