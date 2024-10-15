using OxyPlot.Series;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
using System.Windows.Threading;
using System.Xml.Linq;
using TaskMGPro.Common;
using TaskMGPro.Helper;
using TaskMGPro.Models;
using TaskMGPro.Services;
using OxyPlot.Axes;

namespace TaskMGPro.Pages
{
    /// <summary>
    /// HomePage.xaml 的交互逻辑
    /// </summary>
    public partial class HomePage : BasePage
    {
        private Random _random;
        private DispatcherTimer _timer;
        public ObservableCollection<double> Values { get; set; } = new ObservableCollection<double>();

        public HomePage()
        {
            InitializeComponent();
            RefreshData();
            InitChat();
        }
        private void InitChat() {
            _random = new Random();

            // 初始化 PlotModel
            PlotModel = new PlotModel { Title = "我的图表" };

            // 初始化 LineSeries
            LineSeries = new LineSeries();
            PlotModel.Series.Add(LineSeries); // 确保 LineSeries 在 PlotModel 中

            // 设置坐标轴
            var xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                MajorGridlineStyle = LineStyle.Solid,
                IsAxisVisible = true,
                IsZoomEnabled = false // 禁用 X 轴缩放
            };
            var yAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                MajorGridlineStyle = LineStyle.Solid,
                IsAxisVisible = true,
                IsZoomEnabled = false // 禁用 X 轴缩放
            };

            // 将坐标轴添加到模型中
            PlotModel.Axes.Add(xAxis);
            PlotModel.Axes.Add(yAxis);

            Values = new ObservableCollection<double>();
            DataContext = this;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += UpdateData;
            _timer.Start();
        }
        public PlotModel PlotModel { get; set; }
        public LineSeries LineSeries { get; set; }

        private void UpdateData(object sender, EventArgs e)
        {
            double newValue = _random.Next(0, 100);
            Values.Add(newValue);
            LineSeries.Points.Add(new DataPoint(Values.Count - 1, newValue));

            if (Values.Count > 10)
            {
                Values.RemoveAt(0);
                LineSeries.Points.RemoveAt(0);

                // 更新每个点的 X 值
                for (int i = 0; i < LineSeries.Points.Count; i++)
                {
                    LineSeries.Points[i] = new DataPoint(i, LineSeries.Points[i].Y);
                }
            }

            // 确保在更新后无效化图表
            PlotModel.InvalidatePlot(true);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.Background = BackgroundColor.ToBrushColor();
        }

        private void BtnGroup_Click(object sender, RoutedEventArgs e)
        {
            // 创建弹出框
            PopupWindow popup = new PopupWindow(new BaseWindow() { Height = this.Height, Width = this.Width });
            // 创建MyPage实例并传递参数
            GroupPage page = new GroupPage();
            page.PageClosed += MyPage_PageClosed;
            // 在弹窗中加载指定的Page
            popup.LoadPageWithParameters(page);
            // 显示弹出框
            var result = popup.ShowDialog();

        }
        private void MyPage_PageClosed(object? sender, PupupWindowEventArgs<bool> e)
        {
            if (e.Data)
            {
                RefreshData();
            }
        }
        private void RefreshData()
        {
            var groupList = GroupService.GetGroupList();
            listGroupData.ItemsSource = groupList;
        }




    }


}
