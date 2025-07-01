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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Notes.APP.CustomCtrls
{
    /// <summary>
    /// TimePickerUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class TimePickerUserControl : UserControl
    {
        /// <summary>
        /// 
        /// </summary>
        public TimePickerUserControl()
        {
            InitializeComponent();
            this.DataContext = this;

            HourList.ItemsSource = Enumerable.Range(0, 24).Select(i => i.ToString("D2")).ToList();
            MinuteList.ItemsSource = Enumerable.Range(0, 60).Select(i => i.ToString("D2")).ToList();
        }

        public string SelectedTime
        {
            get
            {
                string hour = HourList.SelectedItem?.ToString() ?? "00";
                string minute = MinuteList.SelectedItem?.ToString() ?? "00";
                return $"{hour}:{minute}";
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                var parts = value.Split(':');
                if (parts.Length != 2) return;

                if (int.TryParse(parts[0], out int hour))
                {
                    HourList.SelectedItem = hour.ToString("D2");
                }

                if (int.TryParse(parts[1], out int minute))
                {
                    MinuteList.SelectedItem = minute.ToString("D2");
                }
            }
        }

        public static readonly DependencyProperty ItemBackgroundProperty = DependencyProperty.Register(
            nameof(ItemBackground), typeof(Brush), typeof(TimePickerUserControl), new PropertyMetadata(Brushes.White));

        public Brush ItemBackground
        {
            get => (Brush)GetValue(ItemBackgroundProperty);
            set => SetValue(ItemBackgroundProperty, value);
        }

        public static readonly DependencyProperty ItemForegroundProperty = DependencyProperty.Register(
            nameof(ItemForeground), typeof(Brush), typeof(TimePickerUserControl), new PropertyMetadata(Brushes.Black));

        public Brush ItemForeground
        {
            get => (Brush)GetValue(ItemForegroundProperty);
            set => SetValue(ItemForegroundProperty, value);
        }

        public static readonly DependencyProperty ControlWidthProperty = DependencyProperty.Register(
            nameof(ControlWidth), typeof(double), typeof(TimePickerUserControl), new PropertyMetadata(200.0));

        public double ControlWidth
        {
            get => (double)GetValue(ControlWidthProperty);
            set => SetValue(ControlWidthProperty, value);
        }

        public static readonly DependencyProperty ControlHeightProperty = DependencyProperty.Register(
            nameof(ControlHeight), typeof(double), typeof(TimePickerUserControl), new PropertyMetadata(150.0));

        public double ControlHeight
        {
            get => (double)GetValue(ControlHeightProperty);
            set => SetValue(ControlHeightProperty, value);
        }

        private void HourList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ScrollToSelectedItemWithAnimation(HourList);
        }

        private void MinuteList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ScrollToSelectedItemWithAnimation(MinuteList);
        }

        private void ScrollToSelectedItemWithAnimation(ListBox listBox)
        {
            if (listBox.SelectedIndex < 0) return;

            var scrollViewer = FindScrollViewer(listBox);
            if (scrollViewer == null) return;

            double itemHeight = 30;
            double targetOffset = listBox.SelectedIndex * itemHeight;

            DoubleAnimation animation = new DoubleAnimation
            {
                To = targetOffset,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            scrollViewer.BeginAnimation(ScrollViewerBehavior.VerticalOffsetProperty, animation);
        }

        private ScrollViewer? FindScrollViewer(DependencyObject obj)
        {
            if (obj is ScrollViewer viewer) return viewer;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                var result = FindScrollViewer(child);
                if (result != null) return result;
            }

            return null;
        }
    }

    public static class ScrollViewerBehavior
    {
        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.RegisterAttached(
                "VerticalOffset",
                typeof(double),
                typeof(ScrollViewerBehavior),
                new PropertyMetadata(0.0, OnVerticalOffsetChanged));

        public static void SetVerticalOffset(DependencyObject target, double value)
            => target.SetValue(VerticalOffsetProperty, value);

        public static double GetVerticalOffset(DependencyObject target)
            => (double)target.GetValue(VerticalOffsetProperty);

        private static void OnVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer viewer)
            {
                viewer.ScrollToVerticalOffset((double)e.NewValue);
            }
        }
    }
}
