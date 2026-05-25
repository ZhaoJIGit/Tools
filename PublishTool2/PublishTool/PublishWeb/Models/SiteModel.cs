using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PublishWeb.Models
{
    public static class DeployTaskStore
    {
        public static ConcurrentDictionary<string, DeployTask> Tasks = new();
    }
    public class SiteModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; }

        public string FilePath { get; set; }

        public string SitePath { get; set; }


        public bool IsSelected { get; set; }

    }
    public class DeployTask
    {
        public long _logIdSeed = 0;

        public string TaskId { get; set; } = Guid.NewGuid().ToString("N");

        public string Status { get; set; } = "Running";

        public int Progress { get; set; }
        public List<LogItem> Logs { get; set; } = new();
        public string Message { get; set; }
        public LogType LogType { get; set; } //
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
    public class LogItem
    {
        public long Id { get; set; }
        public DateTime Time { get; set; } = DateTime.Now;

        public string Message { get; set; }

        public LogType LogType { get; set; }
    }
    public class DeployRequest
    {
        /// <summary>
        /// 站点ID（强烈推荐用Id，不用Name）
        /// </summary>
        public List<string> SiteIds { get; set; }

    }
    public enum LogType
    {
        Info,
        Success,
        Warning,
        Error
    }
    public class InitiateUploadRequest
    {
        public string FileName { get; set; }
        public long TotalSize { get; set; }
        public int TotalChunks { get; set; }
        public string SiteId { get; set; }
    }

    public class InitiateUploadResponse
    {
        public string UploadId { get; set; }
        public int ChunkSize { get; set; }
        public long ExpiresAt { get; set; }
    }

    public class UploadChunkRequest
    {
        public string UploadId { get; set; }
        public int ChunkIndex { get; set; }
        public int TotalChunks { get; set; }
        public IFormFile Chunk { get; set; }
    } 
    public class CompleteUploadRequest
    {
        public string SiteId { get; set; }
        public string UploadId { get; set; }
    }

    public class UploadProgress
    {
        public string UploadId { get; set; }
        public int UploadedChunks { get; set; }
        public int TotalChunks { get; set; }
        public double Progress { get; set; }
        public bool IsComplete { get; set; }
    }
}
