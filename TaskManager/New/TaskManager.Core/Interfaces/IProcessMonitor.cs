using TaskManager.Core.Models;

namespace TaskManager.Core.Interfaces
{
    /// <summary>
    /// 进程监控服务接口
    /// </summary>
    public interface IProcessMonitor
    {
        /// <summary>
        /// 获取所有 dotnet 进程
        /// </summary>
        /// <param name="searchName">搜索名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>进程信息列表</returns>
        Task<List<ProcessInfo>> GetDotnetProcessesAsync(string searchName, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有进程的命令行缓存
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>进程ID到命令行的映射</returns>
        Task<Dictionary<int, string>> CacheCommandLinesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 终止进程
        /// </summary>
        /// <param name="processId">进程ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否成功</returns>
        Task<bool> KillProcessAsync(int processId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量终止进程
        /// </summary>
        /// <param name="processIds">进程ID列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>成功终止的进程ID列表</returns>
        Task<List<int>> KillProcessesAsync(IEnumerable<int> processIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取进程详细信息
        /// </summary>
        /// <param name="processId">进程ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>进程详细信息</returns>
        Task<ProcessDetail> GetProcessDetailAsync(int processId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 进程详细信息
    /// </summary>
    public class ProcessDetail
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string CommandLine { get; set; } = string.Empty;
        public long WorkingSet { get; set; }
        public TimeSpan TotalProcessorTime { get; set; }
        public DateTime StartTime { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
        public string WorkingDirectory { get; set; } = string.Empty;
    }
}