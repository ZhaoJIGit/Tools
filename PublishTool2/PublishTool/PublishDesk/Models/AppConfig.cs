using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PublishDesk.Models
{
    public class AppConfig
    {
        public ApiConfig Api { get; set; }
        public UploadConfig Upload { get; set; }
        public class ApiConfig
        {
            public string BaseUrl { get; set; }
            public int TimeoutMinutes { get; set; } = 30;
            public bool EnableProxy { get; set; } = false;
        }
        public class UploadConfig
        {
            public int ChunkSize { get; set; } = 10 * 1024 * 1024;  // 10MB（字节）
            public long MaxFileSize { get; set; } = 512 * 1024 * 1024; // 512MB（字节）
            public string TempFolder { get; set; } = "Temp/Chunks";
            public string UploadFolder { get; set; } = "Uploads";
            public int SessionExpiryHours { get; set; } = 24;

            // 辅助属性：获取 MB 为单位的分片大小
            public int ChunkSizeMB => ChunkSize / (1024 * 1024);

            // 辅助属性：获取 MB 为单位的最大文件大小
            public double MaxFileSizeMB => MaxFileSize / (1024.0 * 1024.0);
        }
        public static AppConfig Load()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

            var json = File.ReadAllText(path);

            return JsonSerializer.Deserialize<AppConfig>(json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
    }
    public enum EditMode
    {
        Add,
        Edit
    }
    public class ResultModel{
       public bool success { get; set; }
        public string message { get; set; }
    }
}
