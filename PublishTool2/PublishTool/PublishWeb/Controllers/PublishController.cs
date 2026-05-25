using DeployService.Services;
using Microsoft.AspNetCore.Mvc;
using PublishWeb.Helper;
using PublishWeb.Models;
using PublishWeb.Services;
using System.IO.Compression;

namespace PublishWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PublishController : ControllerBase
    {
        private readonly ChunkUploadService _uploadService;
        private readonly ILogger<PublishController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly DeployBackgroundService _deployService;
        public PublishController(
            IWebHostEnvironment env,
            ChunkUploadService uploadService,
            ILogger<PublishController> logger,
            DeployBackgroundService deployService)
        {
            _env = env;
            _uploadService = uploadService;
            _logger = logger;
            _deployService = deployService;
        }

        /// <summary>
        /// 获取站点列表
        /// </summary>
        [HttpGet("GetSites")]
        public IActionResult GetSites()
        {
            return Ok(
                SiteConfigService.GetSites());
        }
        /// <summary>
        /// 新增站点
        /// </summary>
        [HttpPost("AddSite")]
        public IActionResult AddSite(
          [FromBody] SiteModel model)
        {
            bool result =
                SiteConfigService.AddSite(
                    model,
                    out string message);

            return Ok(new
            {
                success = result,
                message
            });
        }
        /// <summary>
        /// 修改站点
        /// </summary>
        [HttpPost("UpdateSite")]
        public IActionResult UpdateSite(
    [FromBody] SiteModel model)
        {
            bool result =
                SiteConfigService.UpdateSite(
                    model,
                    out string message);

            return Ok(new
            {
                success = result,
                message
            });
        }
        /// <summary>
        /// 删除站点
        /// </summary>
        [HttpPost("DeleteSite/{id}")]
        public IActionResult DeleteSite(
         string id)
        {
            bool result =
                SiteConfigService.DeleteSite(
                    id,
                    out string message);

            return Ok(new
            {
                success = result,
                message
            });
        }
        /// <summary>
        /// 上传ZIP
        /// </summary>
        /// <summary>
        /// 上传ZIP并解压到指定目录
        /// </summary>
        [HttpPost("Upload")]
        public async Task<IActionResult> Upload(
          IFormFile file,
         [FromForm] string siteId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "未选择文件"
                    });
                }

                var sites = SiteConfigService.GetSites();

                var site = sites.FirstOrDefault(x => x.Id == siteId);

                if (site == null)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "站点不存在"
                    });
                }

                // 🚀 1. Cache目录（统一存ZIP）
                string cacheDir = Path.Combine(
                    AppContext.BaseDirectory,
                    "Files",
                    "Cache");

                if (!Directory.Exists(cacheDir))
                {
                    Directory.CreateDirectory(cacheDir);
                }

                var safeFileName = Path.GetFileName(file.FileName);
                string zipPath = Path.Combine(
                    cacheDir,
                    $"{Guid.NewGuid():N}_{safeFileName}");

                // 🚀 2. 保存ZIP
                using (var stream = new FileStream(zipPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // 🚀 3. 解压到临时目录，再交换到目标目录
                string extractPath = site.FilePath;

                if (!Directory.Exists(extractPath))
                {
                    Directory.CreateDirectory(extractPath);
                }

                string tempDir = Path.Combine(
                    Path.GetTempPath(),
                    "PublishTool_Extract_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                ExtractZipSafely(zipPath, tempDir);

                // 清空目标目录
                ClearDirectory(extractPath);

                // 复制临时目录到目标目录
                foreach (var entry in Directory.GetFileSystemEntries(tempDir))
                {
                    var name = Path.GetFileName(entry);
                    var target = Path.Combine(extractPath, name);
                    if (Directory.Exists(entry))
                    {
                        Directory.Move(entry, target);
                    }
                    else
                    {
                        System.IO.File.Move(entry, target);
                    }
                }

                // 清理临时目录
                try { Directory.Delete(tempDir, true); } catch { }

                // 🚀 4. 删除ZIP
                try
                {
                    System.IO.File.Delete(zipPath);
                }
                catch (Exception ex)
                {
                    // 不影响主流程
                    return Ok(new
                    {
                        success = true,
                        message = $"上传成功，但ZIP删除失败：{ex.Message}"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "上传并解压成功"
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// 开始发布
        /// </summary>
        [HttpPost("StartDeploy")]
        public IActionResult StartDeploy([FromBody] DeployRequest req)
        {
            if (req == null || req.SiteIds == null || req.SiteIds.Count == 0)
            {
                return Ok(new { success = false, message = "站点列表为空" });
            }

            var task = new DeployTask();
            DeployTaskStore.Tasks[task.TaskId] = task;

            _deployService.Enqueue(task.TaskId, req);

            return Ok(new
            {
                taskId = task.TaskId,
                status = task.Status
            });
        }
        [HttpGet("GetStatus/{taskId}")]
        public IActionResult GetStatus(string taskId)
        {
            if (!DeployTaskStore.Tasks.TryGetValue(taskId, out var task))
            {
                return Ok(null);
            }
            return Ok(task);
        }



        /// <summary>
        /// 1. 初始化上传
        /// </summary>
        [HttpPost("init")]
        public async Task<ActionResult<InitiateUploadResponse>> InitiateUpload([FromBody] InitiateUploadRequest request)
        {
            try
            {
                var result = await _uploadService.InitiateUploadAsync(request);
                //Update(task.TaskId, 10, " 站点不存在", LogType.Error);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化上传失败");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 2. 上传分片
        /// </summary>
        [HttpPost("chunk")]
        [Consumes("multipart/form-data")]  // 添加这一行
        [RequestSizeLimit(15 * 1024 * 1024)] // 限制单个分片最大 15MB
        public async Task<IActionResult> UploadChunk(
            [FromForm] string uploadId,
            [FromForm] int chunkIndex,
            [FromForm] int totalChunks,
             IFormFile chunk)
        {
            try
            {
                if (chunk == null || chunk.Length == 0)
                {
                    return BadRequest(new { error = "分片文件不能为空" });
                }

                var result = await _uploadService.UploadChunkAsync(uploadId, chunkIndex, totalChunks, chunk);
                return Ok(new { success = true, chunkIndex });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"上传分片失败: {uploadId}, 分片 {chunkIndex}");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 3. 完成上传，合并文件
        /// </summary>
        [HttpPost("complete")]
        public async Task<ActionResult<object>> CompleteUpload([FromBody] CompleteUploadRequest request)
        {
            try
            {
                var sites = SiteConfigService.GetSites();

                var site = sites.FirstOrDefault(x => x.Id == request.SiteId);
                if (site == null)
                {
                    return BadRequest(new { error = "站点不存在" });
                }

                var zipPath = await _uploadService.CompleteUploadAsync(request.UploadId);
                string extractPath = site.FilePath;

                if (!Directory.Exists(extractPath))
                {
                    Directory.CreateDirectory(extractPath);
                }

                string tempDir = Path.Combine(
                    Path.GetTempPath(),
                    "PublishTool_Extract_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                ExtractZipSafely(zipPath, tempDir);

                ClearDirectory(extractPath);

                foreach (var entry in Directory.GetFileSystemEntries(tempDir))
                {
                    var name = Path.GetFileName(entry);
                    var target = Path.Combine(extractPath, name);
                    if (Directory.Exists(entry))
                    {
                        Directory.Move(entry, target);
                    }
                    else
                    {
                        System.IO.File.Move(entry, target);
                    }
                }

                try { Directory.Delete(tempDir, true); } catch { }

                // 🚀 4. 删除ZIP
                try
                {
                    System.IO.File.Delete(zipPath);
                }
                catch (Exception ex)
                {
                    // 不影响主流程
                    return Ok(new
                    {
                        success = true,
                        message = $"上传成功，但ZIP删除失败：{ex.Message}"
                    });
                }

                // 返回文件信息（可以根据需要自定义）
                return Ok(new
                {
                    success = true,
                    uploadId = request.UploadId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"完成上传失败: {request.UploadId}");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 4. 取消上传
        /// </summary>
        [HttpPost("cancel")]
        public async Task<IActionResult> CancelUpload([FromBody] CompleteUploadRequest request)
        {
            try
            {
                await _uploadService.CancelUploadAsync(request.UploadId);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 5. 获取上传进度
        /// </summary>
        [HttpGet("progress/{uploadId}")]
        public ActionResult<UploadProgress> GetProgress(string uploadId)
        {
            var progress = _uploadService.GetProgress(uploadId);
            if (progress == null)
            {
                return NotFound(new { error = "上传会话不存在" });
            }
            return Ok(progress);
        }



        private static void ExtractZipSafely(string zipPath, string targetDir)
        {
            using var archive = System.IO.Compression.ZipFile.OpenRead(zipPath);
            foreach (var entry in archive.Entries)
            {
                var fullName = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                var resolvedPath = Path.GetFullPath(Path.Combine(targetDir, fullName));

                if (!resolvedPath.StartsWith(Path.GetFullPath(targetDir), StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"ZIP条目存在路径遍历攻击: {entry.FullName}");
                }

                var destDir = Path.GetDirectoryName(resolvedPath);
                if (!string.IsNullOrEmpty(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                if (!string.IsNullOrEmpty(entry.Name))
                {
                    entry.ExtractToFile(resolvedPath, overwrite: true);
                }
            }
        }

        private void ClearDirectory(string path)
        {
            if (!Directory.Exists(path))
                return;

            foreach (var file in Directory.GetFiles(path))
            {
                System.IO.File.Delete(file);
            }

            foreach (var dir in Directory.GetDirectories(path))
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
