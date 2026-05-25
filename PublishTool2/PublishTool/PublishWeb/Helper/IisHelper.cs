using Microsoft.Web.Administration;
using PublishWeb.Models;
using System.IO.Compression;

namespace PublishWeb.Helper
{
    public static class IisHelper
    {
        public static void StopAppPool(
    string name,
    Action<string, LogType>? log = null)
        {
            using var serverManager = new ServerManager();

            var pool = serverManager.ApplicationPools[name];

            if (pool == null)
            {
                log?.Invoke($" AppPool不存在：{name}", LogType.Error);
                throw new Exception($"AppPool不存在：{name}");
            }

            if (pool.State == ObjectState.Stopped)
            {
                log?.Invoke($" AppPool已处于停止状态：{name}", LogType.Info);
                return;
            }

            log?.Invoke($" 正在停止 AppPool：{name}", LogType.Info);

            pool.Stop();

            // 等待真正停止
            for (int i = 0; i < 20; i++)
            {
                Thread.Sleep(500);

                using var sm = new ServerManager();
                var currentPool = sm.ApplicationPools[name];
                if (currentPool == null)
                {
                    log?.Invoke($"...等待停止中（{i + 1}/20），AppPool已不存在", LogType.Warning);
                    continue;
                }

                log?.Invoke($"...等待停止中（{i + 1}/20），当前状态：{currentPool.State}", LogType.Warning);

                if (currentPool.State == ObjectState.Stopped)
                {
                    log?.Invoke($" AppPool已停止：{name}", LogType.Info);
                    return;
                }
            }

            log?.Invoke($" AppPool停止超时：{name}", LogType.Error);

            throw new Exception("AppPool停止超时");
        }

        public static void StartAppPool(
    string name,
    Action<string,LogType>? log = null)
        {
            using var serverManager = new ServerManager();

            var pool = serverManager.ApplicationPools[name];

            if (pool == null)
            {
                log?.Invoke($" AppPool不存在：{name}", LogType.Error);
                throw new Exception($"AppPool不存在：{name}");
            }

            if (pool.State == ObjectState.Started)
            {
                log?.Invoke($" AppPool已运行：{name}", LogType.Info);
                return;
            }

            log?.Invoke($" 正在启动 AppPool：{name}", LogType.Info);

            pool.Start();

            for (int i = 0; i < 20; i++)
            {
                Thread.Sleep(500);

                using var sm = new ServerManager();
                var currentPool = sm.ApplicationPools[name];
                if (currentPool == null)
                {
                    log?.Invoke($"...等待启动中（{i + 1}/20），AppPool已不存在", LogType.Warning);
                    continue;
                }

                log?.Invoke($"...等待启动中（{i + 1}/20），当前状态：{currentPool.State}", LogType.Warning);

                if (currentPool.State == ObjectState.Started)
                {
                    log?.Invoke($" AppPool已启动：{name}", LogType.Info);
                    return;
                }
            }

            log?.Invoke($" AppPool启动超时：{name}", LogType.Error);

            throw new Exception("AppPool启动超时");
        }


        public static void DeployFromZipInPlace(
     string zipPath,
     string sitePath,
     Action<string,LogType>? log = null)
        {
            if (!File.Exists(zipPath))
            {
                log?.Invoke($" ZIP不存在：{zipPath}", LogType.Error);
                throw new Exception("ZIP不存在");
            }

            log?.Invoke($" 开始部署：{zipPath}", LogType.Info);

            string tempDir = Path.Combine(
                Path.GetDirectoryName(zipPath)!,
                "_temp_" + Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(tempDir);

            log?.Invoke($" 临时目录：{tempDir}", LogType.Info);

            // 解压到临时目录（关键！！！）
            System.IO.Compression.ZipFile.ExtractToDirectory(
                zipPath,
                tempDir);

            log?.Invoke(" 解压完成，开始复制到站点", LogType.Info);

            CopyDirectory(tempDir, sitePath, log);

            log?.Invoke(" 文件复制完成", LogType.Info);

            // 删除 ZIP
            try
            {
                File.Delete(zipPath);
                log?.Invoke(" ZIP已删除", LogType.Info);
            }
            catch (Exception ex)
            {
                log?.Invoke($" ZIP删除失败：{ex.Message}", LogType.Info);
            }

            // 删除临时目录
            try
            {
                Directory.Delete(tempDir, true);
                log?.Invoke(" 临时目录已清理", LogType.Info);
            }
            catch (Exception ex)
            {
                log?.Invoke($" 临时目录删除失败：{ex.Message}", LogType.Info);
            }
        }
        public static void CopyDirectory(
     string sourceDir,
     string targetDir,
     Action<string,LogType>? log = null)
        {
            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);
            var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
            log?.Invoke($" 开始拷贝文件 {files.Count()}", LogType.Info);
            foreach (var file in files)
            {
                string relativePath = Path.GetRelativePath(sourceDir, file);

                string targetFile = Path.Combine(targetDir, relativePath);

                string? dir = Path.GetDirectoryName(targetFile);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.Copy(file, targetFile, true);

                //log?.Invoke($" {relativePath}", LogType.Info);
            }
            log?.Invoke($" 拷贝文件完成 {files.Count()}", LogType.Info);

        }
    }


}
