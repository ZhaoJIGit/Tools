using PublishDesk.Models;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public static class ApiClient
{
    private static readonly HttpClient _client;

    static ApiClient()
    {
        var config = AppConfig.Load();

        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            AllowAutoRedirect = true,
            MaxConnectionsPerServer = 10,
            UseProxy = config.Api.EnableProxy,
            UseDefaultCredentials = true
        };

        if (!config.Api.EnableProxy)
        {
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
        }

        _client = new HttpClient(handler)
        {
            BaseAddress = new Uri(config.Api.BaseUrl),
            Timeout = TimeSpan.FromMinutes(config.Api.TimeoutMinutes > 0 ? config.Api.TimeoutMinutes : 30)
        };

        // 添加默认请求头
        _client.DefaultRequestHeaders.Add("Accept", "application/json");
        _client.DefaultRequestHeaders.Add("User-Agent", "PublishDesk-Client");
    }

    public static async Task<T> GetAsync<T>(string url)
    {
        var res = await _client.GetAsync(url);
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(json))
        {
            return default(T);
        }

        return JsonSerializer.Deserialize<T>(json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
    }

    public static async Task PostAsync(string url, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var res = await _client.PostAsync(url, content);
        res.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// POST 泛型返回
    /// </summary>
    public static async Task<T> PostAsync<T>(
        string url,
        object data)
    {
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var res = await _client.PostAsync(url, content);
        res.EnsureSuccessStatusCode();

        var resultJson = await res.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(resultJson))
            return default;

        return JsonSerializer.Deserialize<T>(
            resultJson,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
    }

    private static async Task WaitFileReady(string filePath)
    {
        int retryCount = 0;
        while (retryCount < 30) // 最多等待 15 秒
        {
            try
            {
                using var stream = File.Open(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);  // 改成 FileShare.Read，允许其他进程读取

                if (stream.Length > 0)
                {
                    await Task.Delay(100); // 额外等待确保文件稳定
                    return;
                }
            }
            catch (IOException)
            {
                // 文件正在被使用，继续等待
            }
            catch (UnauthorizedAccessException)
            {
                // 没有权限访问
                throw new Exception($"无法访问文件: {filePath}");
            }

            await Task.Delay(500);
            retryCount++;
        }

        throw new Exception($"文件未就绪: {filePath}");
    }

    public static async Task<T> UploadAsync<T>(
        string url,
        string filePath,
        string siteId)
    {
        try
        {
            Console.WriteLine($"开始上传文件: {filePath}");

            // 等待文件就绪
            await WaitFileReady(filePath);

            // 检查文件大小
            var fileInfo = new FileInfo(filePath);
            Console.WriteLine($"文件大小: {fileInfo.Length / 1024.0 / 1024.0:F2} MB");

            // 如果文件太大（比如超过 100MB），建议使用流式上传而不是读入内存
            if (fileInfo.Length > 100 * 1024 * 1024) // 100MB
            {
                Console.WriteLine("文件较大，使用流式上传...");
                return await UploadLargeFileAsync<T>(url, filePath, siteId);
            }

            // 小文件：读入内存
            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
            Console.WriteLine($"文件已读入内存，大小: {fileBytes.Length} bytes");

            using var form = new MultipartFormDataContent();

            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            fileContent.Headers.ContentLength = fileBytes.Length;

            form.Add(fileContent, "file", Path.GetFileName(filePath));
            form.Add(new StringContent(siteId ?? ""), "siteId");

            // 使用 CancellationToken 来更好地控制超时
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(30));

            Console.WriteLine($"发送请求到: {url}");
            var response = await _client.PostAsync(url, form, cts.Token);

            Console.WriteLine($"响应状态码: {(int)response.StatusCode}");
            var responseText = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"响应内容长度: {responseText?.Length ?? 0}");

            response.EnsureSuccessStatusCode();

            return string.IsNullOrWhiteSpace(responseText)
                ? default
                : JsonSerializer.Deserialize<T>(
                    responseText,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
        }
        catch (TaskCanceledException ex)
        {
            throw new Exception($"上传超时（30分钟）。文件可能过大或网络不稳定。", ex);
        }
        catch (HttpRequestException ex) when (ex.InnerException != null)
        {
            throw new Exception($"网络请求失败: {ex.InnerException.Message}", ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"上传失败: {ex.GetType().Name} - {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"内部异常: {ex.InnerException.Message}");
            }
            throw;
        }
    }

    // 大文件流式上传（避免内存溢出）
    private static async Task<T> UploadLargeFileAsync<T>(
        string url,
        string filePath,
        string siteId)
    {
        using var form = new MultipartFormDataContent();

        // 使用 StreamContent 而不是 ByteArrayContent，避免一次性加载到内存
        var fileStream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920, // 80KB 缓冲区
            true); // 异步 I/O

        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");

        form.Add(fileContent, "file", Path.GetFileName(filePath));
        form.Add(new StringContent(siteId ?? ""), "siteId");

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(60)); // 大文件给更长时间

        var response = await _client.PostAsync(url, form, cts.Token);
        var responseText = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();

        // 注意：fileStream 会在 response 处理完后由 GC 回收，或者可以显式释放
        // 但由于使用了 StreamContent，建议不手动释放，让它随 form 一起释放

        return string.IsNullOrWhiteSpace(responseText)
            ? default
            : JsonSerializer.Deserialize<T>(
                responseText,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
    }


    /// <summary>
    /// 分片上传大文件（带进度回调）
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="url">API地址</param>
    /// <param name="filePath">文件路径</param>
    /// <param name="siteId">站点ID</param>
    /// <param name="onProgress">进度消息回调</param>
    /// <param name="onChunkComplete">分片完成回调 (当前分片, 总分片, 百分比)</param>
    /// <param name="chunkSizeMB">分片大小（MB），默认10MB</param>
    public static async Task<T> UploadLargeFileAsync<T>(
        string filePath,
        string siteId,
        Action<string, LogType> onLog = null,
        Action<int, int, int> onChunkComplete = null,
        int chunkSizeMB = 10)
    {
        var chunkSize = chunkSizeMB * 1024 * 1024;
        var fileInfo = new FileInfo(filePath);
        var totalSize = fileInfo.Length;
        var totalChunks = (int)Math.Ceiling((double)totalSize / chunkSize);

        onLog?.Invoke($"开始处理文件: {fileInfo.Name}", LogType.Info);
        onLog?.Invoke($"文件大小: {totalSize / 1024.0 / 1024.0:F2} MB", LogType.Info);
        onLog?.Invoke($"分片大小: {chunkSizeMB} MB, 总分片数: {totalChunks}", LogType.Info);

        // 1. 初始化上传
        onLog?.Invoke("正在初始化上传...", LogType.Info);
        var initData = new
        {
            FileName = fileInfo.Name,
            TotalSize = totalSize,
            TotalChunks = totalChunks,
            SiteId = siteId
        };

        var initResponse = await PostAsync<InitiateUploadResponse>(  "/api/Publish/init", initData);
        var uploadId = initResponse.UploadId;
        onLog?.Invoke($"初始化成功，UploadId: {uploadId}", LogType.Info);

        try
        {
            // 2. 上传所有分片
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);

            for (int i = 0; i < totalChunks; i++)
            {
                var offset = i * chunkSize;
                var currentChunkSize = (int)Math.Min(chunkSize, totalSize - offset);
                var buffer = new byte[currentChunkSize];

                fileStream.Seek(offset, SeekOrigin.Begin);
                await fileStream.ReadAsync(buffer, 0, currentChunkSize);

                // 上传分片（带重试）
                await UploadChunkWithRetryAsync(uploadId, i, totalChunks, buffer, 3);

                var percent = (i + 1) * 100 / totalChunks;
                onLog?.Invoke($"分片 {i + 1}/{totalChunks} 上传完成 ({percent}%)", LogType.Success);
                onChunkComplete?.Invoke(i + 1, totalChunks, percent);
            }

            // 3. 完成上传
            onLog?.Invoke("所有分片上传完成，正在合并文件...", LogType.Info);
            onLog?.Invoke($"uploadId  {uploadId},siteId {siteId}", LogType.Info);

            var result = await PostAsync<T>("/api/Publish/complete", new { UploadId = uploadId, SiteId= siteId });
            onLog?.Invoke("合并完成，上传成功！", LogType.Success);
            return result;
        }
        catch (Exception ex)
        {
            onLog?.Invoke($"上传失败: {ex.Message}", LogType.Error);
            await CancelUploadAsync(uploadId);
            throw;
        }

    }
    /// <summary>
    /// 上传单个分片（支持重试）
    /// </summary>
    private static async Task UploadChunkWithRetryAsync(
        string uploadId,
        int chunkIndex,
        int totalChunks,
        byte[] data,
        int maxRetries = 3)
    {
        Exception lastException = null;

        for (int retry = 0; retry < maxRetries; retry++)
        {
            try
            {
                using var form = new MultipartFormDataContent();

                var chunkContent = new ByteArrayContent(data);
                chunkContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                form.Add(chunkContent, "chunk", $"chunk_{chunkIndex:00000}");
                form.Add(new StringContent(uploadId), "uploadId");
                form.Add(new StringContent(chunkIndex.ToString()), "chunkIndex");
                form.Add(new StringContent(totalChunks.ToString()), "totalChunks");

                var response = await _client.PostAsync("/api/Publish/chunk", form);
                response.EnsureSuccessStatusCode();

                return; // 成功
            }
            catch (Exception ex) when (retry < maxRetries - 1)
            {
                lastException = ex;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, retry));
                await Task.Delay(delay);
            }
        }

        throw new Exception($"分片 {chunkIndex + 1} 上传失败，已重试 {maxRetries} 次", lastException);
    }

    /// <summary>
    /// 取消上传
    /// </summary>
    private static async Task CancelUploadAsync(string uploadId)
    {
        try
        {
            await PostAsync<object>("/api/Publish/cancel", new { UploadId = uploadId });
        }
        catch
        {
            // 忽略清理失败
        }
    }

}
// 辅助类
public class InitiateUploadResponse
{
    public string UploadId { get; set; }
    public int ChunkSize { get; set; }
    public long ExpiresAt { get; set; }
}

public class CompleteUploadResponse
{
    public bool Success { get; set; }
    public string FileName { get; set; }
    public string FileUrl { get; set; }
}