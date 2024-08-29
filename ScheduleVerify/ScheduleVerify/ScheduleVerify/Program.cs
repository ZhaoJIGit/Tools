namespace ScheduleVerify
{
    internal class Program
    {
        static void Main(string[] args)
        {
            List<Schedule> list = new List<Schedule>();
            //list.Add(new Schedule()
            //{
            //    ScheduleType = 0,
            //    StartDate = DateTime.Now,
            //    StartTime = new TimeSpan(7, 0, 0),
            //    EndDate = DateTime.Now.AddMonths(1),
            //    EndTime = new TimeSpan(10, 30, 0)
            //});
            //list.Add(new Schedule()
            //{
            //    ScheduleType = 1,
            //    StartDate = DateTime.Now,
            //    StartTime = new TimeSpan(8, 0, 0),
            //    EndDate = DateTime.Now.AddMonths(1),
            //    EndTime = new TimeSpan(9, 30, 0)
            //});
            //list.Add(new Schedule()
            //{
            //    ScheduleType = 2,
            //    StartDate = DateTime.Now,
            //    StartTime = new TimeSpan(10, 0, 0),
            //    EndDate = DateTime.Now.AddMonths(1),
            //    EndTime = new TimeSpan(13, 30, 0)
            //});
            //list.Add(new Schedule()
            //{
            //    ScheduleType = 3,
            //    StartDate = DateTime.Now,
            //    StartTime = new TimeSpan(10, 0, 0),
            //    EndDate = DateTime.Now.AddMonths(1),
            //    EndTime = new TimeSpan(13, 30, 0)
            //});
            //list.Add(new Schedule()
            //{
            //    ScheduleType = 4,
            //    StartDate = DateTime.Now,
            //    StartTime = new TimeSpan(10, 0, 0),
            //    EndDate = DateTime.Now.AddMonths(1),
            //    EndTime = new TimeSpan(13, 30, 0)
            //});
            //list.Add(new Schedule()
            //{
            //    ScheduleType = 5,
            //    StartDate = DateTime.Now,
            //    StartTime = new TimeSpan(15, 0, 0),
            //    EndDate = DateTime.Now.AddMonths(1),
            //    EndTime = new TimeSpan(17, 30, 0)
            //});
        
            //list.Add(new Schedule()
            //{
            //    ScheduleType = 3,
            //    StartDate = DateTime.Now,
            //    StartTime = new TimeSpan(13, 0, 0),
            //    EndDate = DateTime.Now.AddMonths(1),
            //    EndTime = new TimeSpan(13, 30, 0)
            //});
            //list.Add(new Schedule()
            //{
            //    ScheduleType = 4,
            //    StartDate = DateTime.Now,
            //    StartTime = new TimeSpan(18, 0, 0),
            //    EndDate = DateTime.Now.AddMonths(1),
            //    EndTime = new TimeSpan(20, 30, 0)
            //});
            //list.Add(new Schedule()
            //{
            //    ScheduleType = 1,
            //    StartDate = DateTime.Now,
            //    StartTime = new TimeSpan(20, 0, 0),
            //    EndDate = DateTime.Now.AddMonths(1),
            //    EndTime = new TimeSpan(23, 30, 0)
            //});
            //list.Add(new Schedule()
            //{
            //    ScheduleType = 5,
            //    StartDate = Convert.ToDateTime("2024-02-29"),
            //    StartTime = new TimeSpan(9, 31, 0),
            //    EndDate = DateTime.Now.AddYears(10),
            //    EndTime = new TimeSpan(9, 45, 0)
            //});
            //list.Add(new Schedule()
            //{
            //    ScheduleType = 3,
            //    StartDate = DateTime.Now,
            //    StartTime = new TimeSpan(14, 0, 0),
            //    EndDate = DateTime.Now.AddMonths(1),
            //    EndTime = new TimeSpan(14, 30, 0)
            //});

            list.Add(new Schedule()
            {
                ScheduleType = 2,
                StartDate = DateTime.Now.AddDays(1),
                StartTime = new TimeSpan(17, 0, 0),
                EndDate = DateTime.Now.AddMonths(100),
                EndTime = new TimeSpan(19, 30, 0)
            });

            //验证时间部分是否重复
            var repeatTime = new List<Schedule>();
            var current = new Schedule()
            {
                ScheduleType = 4,
                StartDate = DateTime.Now,
                StartTime = new TimeSpan(17, 35, 0),
                EndDate = DateTime.Now.AddYears(10),
                EndTime = new TimeSpan(17, 40, 0)
            };
            foreach (var schedule in list)
            {
                //验证已有的日程，开始或者结束时间是否在当前日程之中
                //--这一步可以在数据查询以往日程中使用，判定未来日期
                //只查询之后的日期，不校验已经过去的日程
                if ((current.EndTime > schedule.StartTime && current.StartTime < schedule.EndTime))
                {
                    repeatTime.Add(schedule);
                }
            }
            var result = VerifySchedule(list, current);
            Console.WriteLine("日程计算结果：" + result.Result + " 日期:[" + result.RepeatDate + "]");
        }
        private static (bool Result, DateTime RepeatDate) VerifySchedule(List<Schedule> schedules, Schedule current)
        {
            if (schedules == null || schedules.Count == 0) { return (false, DateTime.Now); }

            var nextDates = new List<DateTime>();
            var result = (false, DateTime.Now);

            // 并行处理日程列表
            //Parallel.ForEach(schedules, (schedule, state) =>
            //{
            //    nextDates = CheckNextDate(current,schedule );
            //    if (nextDates != null && nextDates.Count > 0)
            //    {
            //        result = (true, nextDates[0]);
            //        // 找到冲突后立即停止
            //        state.Stop();
            //    }
            //});
            foreach (var schedule in schedules)
            {
                nextDates = CheckNextDate(current, schedule);
                if (nextDates != null && nextDates.Count > 0)
                {
                    result = (true, nextDates[0]);
                    continue;
                }
            }
            return result;
        }

        private static bool HasConflict(Schedule newSchedule, Schedule existingSchedule)
        {
            // 直接比较两个日程的日期范围是否重叠
            return newSchedule.EndDate < existingSchedule.StartDate || newSchedule.StartDate > existingSchedule.EndDate;
        }

        private static List<DateTime> CheckNextDate(Schedule newSchedule, Schedule existingSchedule)
        {
            // 如果没有冲突，则返回空列表
            //if (!HasConflict(newSchedule, existingSchedule))
            //    return new List<DateTime>();

            // 获取两个日程的所有日期
            var newDates = GenerateFutureDates(newSchedule);
            var existingDates = GenerateFutureDates(existingSchedule);

            // 返回冲突的日期列表
            return newDates.Intersect(existingDates).ToList();
        }
        private static HashSet<DateTime> GenerateFutureDates(Schedule schedule)
        {
            var days = new HashSet<DateTime>();
            DateTime currentDate = schedule.StartDate.Date;
            DateTime endDate = schedule.EndDate.Date;

            // 如果是2月29日并且ScheduleType是每年（5）
            bool isLeapYearStart = currentDate.Month == 2 && currentDate.Day == 29 && schedule.ScheduleType == 5;

            while (currentDate <= endDate)
            {
                // 如果是工作日类型，跳过周末
                if (schedule.ScheduleType == 1 &&
                    (currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday))
                {
                    currentDate = currentDate.AddDays(1);
                    continue;
                }

                // 如果是闰年的2月29日，且仅在闰年执行
                if (isLeapYearStart)
                {
                    if (DateTime.IsLeapYear(currentDate.Year))
                    {
                        days.Add(currentDate);
                    }
                    currentDate = currentDate.AddYears(1);
                    continue;
                }

                // 添加符合条件的日期
                days.Add(currentDate);

                // 根据ScheduleType增加日期
                switch (schedule.ScheduleType)
                {
                    case 0: // 每日
                        currentDate = currentDate.AddDays(1);
                        break;
                    case 1: // 工作日（已处理跳过周末的逻辑）
                        currentDate = currentDate.AddDays(1);
                        break;
                    case 2: // 每周
                        currentDate = currentDate.AddDays(7);
                        break;
                    case 3: // 每两周
                        currentDate = currentDate.AddDays(14);
                        break;
                    case 4: // 每月
                        currentDate = currentDate.AddMonths(1);
                        break;
                    case 5: // 每年
                        currentDate = currentDate.AddYears(1);
                        break;
                }
            }

            return days;
        }

    }

    public class Schedule
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        /// <summary>
        /// 0 每天,1每个工作日，2 每周,3，每2周，4 每月，5 每年
        /// </summary>
        public int ScheduleType { get; set; }
    }
}
