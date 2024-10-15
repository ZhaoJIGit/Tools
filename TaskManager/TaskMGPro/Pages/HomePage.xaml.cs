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

        private Random _random1;
        private DispatcherTimer _timer1;
        public ObservableCollection<double> Values1 { get; set; } = new ObservableCollection<double>();


        private Random _random2;
        private DispatcherTimer _timer2;
        public ObservableCollection<double> Values2 { get; set; } = new ObservableCollection<double>();

        public HomePage()
        {
            InitializeComponent();
            RefreshData();
            InitChat();
            InitChat1();
            InitChat2();

        }
        private void InitChat()
        {
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
                IsZoomEnabled = false, // 禁用 X 轴缩放
                Minimum = 0, // 固定 Y 轴的最小值
                Maximum = 100 // 固定 Y 轴的最大值
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
        private void InitChat1()
        {
            _random1 = new Random();

            // 初始化 PlotModel
            PlotModel1 = new PlotModel { Title = "我的图表" };

            // 初始化 LineSeries
            LineSeries1 = new LineSeries();
            PlotModel1.Series.Add(LineSeries1); // 确保 LineSeries 在 PlotModel 中

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
                Minimum = 0, // 固定 Y 轴的最小值
                Maximum = 100, // 固定 Y 轴的最大值
                IsZoomEnabled = false // 禁用 X 轴缩放
            };

            // 将坐标轴添加到模型中
            PlotModel1.Axes.Add(xAxis);
            PlotModel1.Axes.Add(yAxis);

            Values1 = new ObservableCollection<double>();
            DataContext = this;
            _timer1 = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer1.Tick += UpdateData1;
            _timer1.Start();
        }
        private void InitChat2()
        {
            _random2 = new Random();

            // 初始化 PlotModel
            PlotModel2 = new PlotModel { Title = "我的图表" };

            // 初始化 LineSeries
            LineSeries2 = new LineSeries();
            PlotModel2.Series.Add(LineSeries2); // 确保 LineSeries 在 PlotModel 中

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
                IsZoomEnabled = false, // 禁用 X 轴缩放
                Minimum = 0, // 固定 Y 轴的最小值
                Maximum = 100 // 固定 Y 轴的最大值
            };

            // 将坐标轴添加到模型中
            PlotModel2.Axes.Add(xAxis);
            PlotModel2.Axes.Add(yAxis);

            Values2 = new ObservableCollection<double>();
            DataContext = this;
            _timer2 = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer2.Tick += UpdateData2;
            _timer2.Start();
        }
        public PlotModel PlotModel { get; set; }
        public PlotModel PlotModel1 { get; set; }
        public PlotModel PlotModel2 { get; set; }

        public LineSeries LineSeries { get; set; }
        public LineSeries LineSeries1 { get; set; }
        public LineSeries LineSeries2 { get; set; }


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
        private void UpdateData1(object sender, EventArgs e)
        {
            double newValue = _random1.Next(0, 100);
            Values1.Add(newValue);
            LineSeries1.Points.Add(new DataPoint(Values1.Count - 1, newValue));

            if (Values1.Count > 10)
            {
                Values1.RemoveAt(0);
                LineSeries1.Points.RemoveAt(0);

                // 更新每个点的 X 值
                for (int i = 0; i < LineSeries1.Points.Count; i++)
                {
                    LineSeries1.Points[i] = new DataPoint(i, LineSeries1.Points[i].Y);
                }
            }

            // 确保在更新后无效化图表
            PlotModel1.InvalidatePlot(true);

        }
        private void UpdateData2(object sender, EventArgs e)
        {
            double newValue = _random2.Next(0, 100);
            Values2.Add(newValue);
            LineSeries2.Points.Add(new DataPoint(Values2.Count - 1, newValue));

            if (Values2.Count > 10)
            {
                Values2.RemoveAt(0);
                LineSeries2.Points.RemoveAt(0);

                // 更新每个点的 X 值
                for (int i = 0; i < LineSeries2.Points.Count; i++)
                {
                    LineSeries2.Points[i] = new DataPoint(i, LineSeries2.Points[i].Y);
                }
            }

            // 确保在更新后无效化图表
            PlotModel2.InvalidatePlot(true);

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
