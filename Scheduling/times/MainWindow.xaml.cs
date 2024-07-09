using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Windows;
using Newtonsoft.Json;
namespace times
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string cacheDirectory;
        static Dictionary<string, Holiday>? _dicHolidays = new Dictionary<string, Holiday>();
        private string holidayFile = "holiday.json";
        private int currentYear = DateTime.Now.Year;
        public MainWindow()
        {
            InitializeComponent();
            cacheDirectory = Path.Combine(Path.GetTempPath(), "Holiday", "Cache");

            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }



        }
        private void CalculateButton_Click(object sender, RoutedEventArgs e)
        {

            if (StartDatePicker.SelectedDate == null)
            {
                MessageBox.Show("请选择一个开始日期。");
                return;
            }

            DateTime startDate = StartDatePicker.SelectedDate.Value;
            if (!int.TryParse(GroupCountTextBox.Text, out int numGroups) || numGroups <= 0)
            {
                MessageBox.Show("请输入有效的每月组数。");
                return;
            }

            if (!int.TryParse(StartGroupTextBox.Text, out int startGroup) || startGroup <= 0 || startGroup > numGroups)
            {
                MessageBox.Show("请输入有效的开始组。");
                return;
            }

            if (!int.TryParse(TargetGroupTextBox.Text, out int targetGroup) || targetGroup <= 0 || targetGroup > numGroups)
            {
                MessageBox.Show("请输入有效的目标组。");
                return;
            }
            // 在同步方法中调用异步方法
            currentYear = StartDatePicker.SelectedDate.Value.Year;
            LoadHolidayJson();
            if (_dicHolidays == null || _dicHolidays.Count == 0)
            {
                Task<bool> resultTask = Task.Run(async () => await GetHolidays());
                bool result = resultTask.Result; // 同步等待异步方法完成，并获取结果
            }

            int generateMonths = GenerateMonthsComboBox.SelectedIndex + 1; // 获取选择的生成月数
            List<Tuple<DateTime, string>> targetGroupDates = CalculateGroupDates(startDate, numGroups, startGroup, targetGroup, generateMonths);

            ResultListBox.Items.Clear();
            foreach (var tuple in targetGroupDates)
            {
                ResultListBox.Items.Add($"{tuple.Item1.ToShortDateString()} ({tuple.Item2})");
            }
        }
        private List<Tuple<DateTime, string>> CalculateGroupDates(DateTime startDate, int numGroups, int startGroup, int targetGroup, int generateMonths)
        {
            List<Tuple<DateTime, string>> groupDates = new List<Tuple<DateTime, string>>();
            DateTime currentDate = startDate;
            int currentGroup = startGroup;

            // 计算未来2个月的日期
            DateTime endDate = startDate.AddMonths(generateMonths);

            while (currentDate < endDate)
            {
                if (currentGroup == targetGroup)
                {
                    string dayOfWeek = currentDate.ToString("dddd", new CultureInfo("zh-CN")); // 获取中文星期

                    if (_dicHolidays != null && _dicHolidays.Count > 0)
                    {
                        if (_dicHolidays.TryGetValue(currentDate.ToString("MM-dd"), out var date) && currentDate.Year == currentYear)
                        {
                            groupDates.Add(new Tuple<DateTime, string>(currentDate, $@"{dayOfWeek}-{date.name}-{(date.holiday ? "放假" : "上班")}"));

                        }
                        else
                        {
                            groupDates.Add(new Tuple<DateTime, string>(currentDate, dayOfWeek));
                        }
                    }
                    else
                    {
                        groupDates.Add(new Tuple<DateTime, string>(currentDate, dayOfWeek));
                    }

                }

                // 移动到下一天
                currentDate = currentDate.AddDays(1);
                currentGroup = (currentGroup % numGroups) + 1;
            }

            return groupDates;
        }
        private async Task<bool> GetHolidays()
        {
            if (_dicHolidays != null && _dicHolidays.Count > 0) { return true; }
            string apiUrl = $@"https://timor.tech/api/holiday/year/{currentYear}"; // 替换为实际的接口地址
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();

                        File.WriteAllText(Path.Combine(cacheDirectory, holidayFile), responseBody);
                        var result = JsonConvert.DeserializeObject<Result>(responseBody);
                        if (result != null)
                        {
                            _dicHolidays = result.holiday;
                        }

                    }
                    else
                    {
                    }
                }
            }
            catch (Exception ex)
            {
            }
            return true;
        }
        private void LoadHolidayJson()
        {
            holidayFile = $@"holiday_{currentYear}.json";
            if (File.Exists(Path.Combine(cacheDirectory, holidayFile)))
            {
                string json = File.ReadAllText(Path.Combine(cacheDirectory, holidayFile));
                var data = JsonConvert.DeserializeObject<Result>(json);
                if (data != null)
                {
                    _dicHolidays = data.holiday;
                }
            }
        }
    }
    public class Holiday
    {
        public bool holiday { get; set; }
        public string name { get; set; }
        public int wage { get; set; }
        public string date { get; set; }
        public int rest { get; set; }
        public bool after { get; set; }
        public string target { get; set; }
    }
    public class Result
    {
        public int code { get; set; }
        public Dictionary<string, Holiday> holiday { get; set; }

    }
}