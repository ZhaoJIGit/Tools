using TaskManager.Core.Models;

namespace TaskManager.Core.Interfaces
{
    /// <summary>
    /// 任务组仓库接口
    /// </summary>
    public interface ITaskGroupRepository
    {
        /// <summary>
        /// 获取所有任务组
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>任务组列表</returns>
        Task<List<TaskGroupInfo>> GetAllTaskGroupsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 保存任务组
        /// </summary>
        /// <param name="taskGroups">任务组列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task SaveTaskGroupsAsync(IEnumerable<TaskGroupInfo> taskGroups, CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加任务组
        /// </summary>
        /// <param name="taskGroup">任务组</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task AddTaskGroupAsync(TaskGroupInfo taskGroup, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除任务组
        /// </summary>
        /// <param name="taskGroupName">任务组名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否成功删除</returns>
        Task<bool> DeleteTaskGroupAsync(string taskGroupName, CancellationToken cancellationToken = default);

        /// <summary>
        /// 查找任务组
        /// </summary>
        /// <param name="taskGroupName">任务组名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>任务组信息</returns>
        Task<TaskGroupInfo?> FindTaskGroupAsync(string taskGroupName, CancellationToken cancellationToken = default);
    }
}