using System.Diagnostics;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //// 创建 PerformanceCounter 来监控 CPU 使用率
            //PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            //// 创建 PerformanceCounter 来监控可用内存
            //PerformanceCounter memCounter = new PerformanceCounter("Memory", "Available MBytes");

            //while (true)
            //{
            //    Console.WriteLine("CPU Usage: {0}%", cpuCounter.NextValue());
            //    Console.WriteLine("Available Memory: {0} MB", memCounter.NextValue());
            //    System.Threading.Thread.Sleep(1000);  // 每秒更新一次
            //}

            // 查找指定名称的进程
            Process[] processes = Process.GetProcessesByName("PerfWatson2");

            if (processes.Length == 0)
            {
                Console.WriteLine("未找到 'notepad' 进程。");
                return;
            }

            Process process = processes[0]; // 获取第一个找到的进程

            // 打印进程基本信息
            Console.WriteLine("进程名称: " + process.ProcessName);
            Console.WriteLine("进程ID: " + process.Id);

            // CPU 时间
            TimeSpan cpuTime = process.TotalProcessorTime;
            Console.WriteLine("CPU 使用时间: " + cpuTime.TotalMilliseconds + " 毫秒");

            // 工作集（内存占用）
            long memoryUsage = process.WorkingSet64;
            Console.WriteLine("内存使用: " + memoryUsage / 1024 / 1024 + " MB");

            // 虚拟内存大小
            long virtualMemory = process.VirtualMemorySize64;
            Console.WriteLine("虚拟内存使用: " + virtualMemory / 1024 / 1024 + " MB");

            // 句柄数
            Console.WriteLine("句柄数: " + process.HandleCount);

            // 线程数
            Console.WriteLine("线程数: " + process.Threads.Count);

            // 优先级
            Console.WriteLine("优先级: " + process.BasePriority);

            // 持续监控，每秒刷新一次
            while (!process.HasExited)
            {
                Console.WriteLine("CPU 使用时间: " + process.TotalProcessorTime.TotalMilliseconds + " 毫秒");
                Console.WriteLine("内存使用: " + process.WorkingSet64 / 1024 / 1024 + " MB");
                System.Threading.Thread.Sleep(1000);  // 每秒刷新一次
            }
        }
    }
}
