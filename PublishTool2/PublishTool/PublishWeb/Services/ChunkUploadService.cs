using PublishWeb.Models;
using System.Collections.Concurrent;

namespace PublishWeb.Services
{
    public class ChunkUploadService
    {
        private readonly string _tempFolder;
        private readonly string _uploadFolder;
        private readonly ILogger<ChunkUploadService> _logger;

        // 内存中记录上传进度（生产环境建议用 Redis 或数据库）
        private static readonly ConcurrentDictionary<string, UploadSession> _sessions = new();

        public ChunkUploadService(ILogger<ChunkUploadService> logger)
        {
            _logger = logger;
            _tempFolder = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "Chunks");
            _uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

            Directory.CreateDirectory(_tempFolder);
            Directory.CreateDirectory(_uploadFolder);
        }

        /// <summary>
        /// 初始化上传，创建上传会话
        /// </summary>
        public async Task<InitiateUploadResponse> InitiateUploadAsync(InitiateUploadRequest request)
        {
            var uploadId = Guid.NewGuid().ToString("N");
            var expiresAt = DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds(); // 24小时过期

            var session = new UploadSession
            {
                UploadId = uploadId,
                FileName = request.FileName,
                TotalSize = request.TotalSize,
                TotalChunks = request.TotalChunks,
                SiteId = request.SiteId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresAt).UtcDateTime,
                UploadedChunks = new bool[request.TotalChunks],
                ChunkPaths = new string[request.TotalChunks]
            };

            // 创建临时目录
            var chunkDir = GetChunkDirectory(uploadId);
            Directory.CreateDirectory(chunkDir);

            _sessions.TryAdd(uploadId, session);

            _logger.LogInformation($"初始化上传: {uploadId}, 文件名: {request.FileName}, 总分片: {request.TotalChunks}");

            return await Task.FromResult(new InitiateUploadResponse
            {
                UploadId = uploadId,
                ChunkSize = 10 * 1024 * 1024, // 10MB
                ExpiresAt = expiresAt
            });
        }

        /// <summary>
        /// 上传分片
        /// </summary>
        public async Task<bool> UploadChunkAsync(string uploadId, int chunkIndex, int totalChunks, IFormFile chunk)
        {
            if (!_sessions.TryGetValue(uploadId, out var session))
            {
                throw new Exception($"上传会话不存在: {uploadId}");
            }

            if (session.IsCompleted)
            {
                throw new Exception("文件已经上传完成");
            }

            if (session.ExpiresAt < DateTime.UtcNow)
            {
                throw new Exception("上传会话已过期");
            }

            // 保存分片文件
            var chunkFileName = $"chunk_{chunkIndex:00000}.part";
            var chunkPath = Path.Combine(GetChunkDirectory(uploadId), chunkFileName);

            using (var stream = new FileStream(chunkPath, FileMode.Create))
            {
                await chunk.CopyToAsync(stream);
            }

            // 更新会话状态
            session.UploadedChunks[chunkIndex] = true;
            session.ChunkPaths[chunkIndex] = chunkPath;
            session.LastUpdateTime = DateTime.UtcNow;

            _logger.LogInformation($"上传分片 {chunkIndex + 1}/{totalChunks}: {uploadId}");

            return true;
        }

        /// <summary>
        /// 完成上传，合并所有分片
        /// </summary>
        public async Task<string> CompleteUploadAsync(string uploadId)
        {
            if (!_sessions.TryGetValue(uploadId, out var session))
            {
                throw new Exception($"上传会话不存在: {uploadId}");
            }

            // 检查是否所有分片都已上传
            for (int i = 0; i < session.TotalChunks; i++)
            {
                if (!session.UploadedChunks[i])
                {
                    throw new Exception($"分片 {i + 1} 未上传");
                }
            }

            // 生成最终文件名（避免重名）
            var finalFileName = $"{DateTime.Now:yyyyMMddHHmmss}_{session.FileName}";
            var finalPath = Path.Combine(_uploadFolder, finalFileName);

            // 合并分片
            using (var finalStream = new FileStream(finalPath, FileMode.Create))
            {
                foreach (var chunkPath in session.ChunkPaths)
                {
                    using (var chunkStream = new FileStream(chunkPath, FileMode.Open))
                    {
                        await chunkStream.CopyToAsync(finalStream);
                    }
                }
            }

            // 验证文件大小
            var finalFileInfo = new FileInfo(finalPath);
            if (finalFileInfo.Length != session.TotalSize)
            {
                throw new Exception($"文件大小不匹配: 期望 {session.TotalSize}, 实际 {finalFileInfo.Length}");
            }

            // 清理临时文件
            await CleanupAsync(uploadId);

            _logger.LogInformation($"上传完成: {uploadId}, 文件: {finalFileName}, 大小: {finalFileInfo.Length}");

            // 返回文件路径或 URL
            return finalPath;
        }

        /// <summary>
        /// 取消上传，清理临时文件
        /// </summary>
        public async Task CancelUploadAsync(string uploadId)
        {
            await CleanupAsync(uploadId);
            _logger.LogInformation($"取消上传: {uploadId}");
        }

        /// <summary>
        /// 获取上传进度
        /// </summary>
        public UploadProgress GetProgress(string uploadId)
        {
            if (!_sessions.TryGetValue(uploadId, out var session))
            {
                return null;
            }

            var uploadedCount = session.UploadedChunks.Count(c => c);
            var progress = (double)uploadedCount / session.TotalChunks * 100;

            return new UploadProgress
            {
                UploadId = uploadId,
                UploadedChunks = uploadedCount,
                TotalChunks = session.TotalChunks,
                Progress = progress,
                IsComplete = session.IsCompleted
            };
        }

        private string GetChunkDirectory(string uploadId)
        {
            return Path.Combine(_tempFolder, uploadId);
        }

        private async Task CleanupAsync(string uploadId)
        {
            if (_sessions.TryRemove(uploadId, out var session))
            {
                var chunkDir = GetChunkDirectory(uploadId);
                if (Directory.Exists(chunkDir))
                {
                    await Task.Run(() => Directory.Delete(chunkDir, true));
                }
            }
        }

        // 清理过期会话（后台任务调用）
        public async Task CleanupExpiredSessionsAsync()
        {
            var expiredSessions = _sessions.Where(x => x.Value.ExpiresAt < DateTime.UtcNow).ToList();
            foreach (var session in expiredSessions)
            {
                await CleanupAsync(session.Key);
                _logger.LogInformation($"清理过期会话: {session.Key}");
            }
        }
    }

    public class UploadSession
    {
        public string UploadId { get; set; }
        public string FileName { get; set; }
        public long TotalSize { get; set; }
        public int TotalChunks { get; set; }
        public string SiteId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public bool[] UploadedChunks { get; set; }
        public string[] ChunkPaths { get; set; }
        public bool IsCompleted => UploadedChunks?.All(x => x) == true;
    }
}
