namespace PublishWeb.Services
{
    public class UploadCleanupService : BackgroundService
    {
        private readonly ChunkUploadService _uploadService;
        private readonly ILogger<UploadCleanupService> _logger;

        public UploadCleanupService(ChunkUploadService uploadService, ILogger<UploadCleanupService> logger)
        {
            _uploadService = uploadService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
                    await _uploadService.CleanupExpiredSessionsAsync();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "清理过期上传会话失败");
                }
            }
        }
    }
}
