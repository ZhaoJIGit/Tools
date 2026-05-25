using Microsoft.Extensions.Logging;
using PublishWeb.Models;
using System.Text.Json;

namespace DeployService.Services;

public static class SiteConfigService
{
    private static ILogger _logger;

    public static void SetLogger(ILogger logger)
    {
        _logger = logger;
    }

    private static readonly object _lock = new();

    private static string JsonPath =>
        Path.Combine(
            AppContext.BaseDirectory,
            "Files", "SiteData.json");

    public static List<SiteModel> GetSites()
    {
        lock (_lock)
        {
            try
            {
                if (!File.Exists(JsonPath))
                {
                    return new List<SiteModel>();
                }

                using var fileStream = new FileStream(
                 JsonPath,
                 FileMode.Open,
                 FileAccess.Read,
                 FileShare.ReadWrite,
                 4096,
                 FileOptions.SequentialScan);

                using var reader = new StreamReader(fileStream);
                string json = reader.ReadToEnd();

                if (string.IsNullOrWhiteSpace(json))
                {
                    return new List<SiteModel>();
                }

                return JsonSerializer.Deserialize<List<SiteModel>>(json)
                       ?? new List<SiteModel>();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "读取站点配置文件失败: {JsonPath}", JsonPath);
                return new List<SiteModel>();
            }
        }
    }

    /// <summary>
    /// 保存站点
    /// </summary>
    public static void SaveSites(
        List<SiteModel> list)
    {
        lock (_lock)
        {
            string json =
                JsonSerializer.Serialize(
                    list,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

            File.WriteAllText(
                JsonPath,
                json);
        }
    }

    /// <summary>
    /// 新增站点
    /// </summary>
    public static bool AddSite(
        SiteModel model,
        out string message)
    {
        lock (_lock)
        {
            try
            {
                var list = GetSites();

                model.Id =
                    Guid.NewGuid().ToString();

                list.Add(model);

                SaveSites(list);

                message = "新增成功";

                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;

                return false;
            }
        }
    }

    /// <summary>
    /// 修改站点
    /// </summary>
    public static bool UpdateSite(
        SiteModel model,
        out string message)
    {
        lock (_lock)
        {
            try
            {
                var list = GetSites();

                var site =
                    list.FirstOrDefault(x =>
                        x.Id == model.Id);

                if (site == null)
                {
                    message = "站点不存在";

                    return false;
                }

                site.Name = model.Name;
                site.FilePath = model.FilePath;
                site.SitePath = model.SitePath;
                site.IsSelected = model.IsSelected;

                SaveSites(list);

                message = "修改成功";

                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;

                return false;
            }
        }
    }

    /// <summary>
    /// 删除站点
    /// </summary>
    public static bool DeleteSite(
        string id,
        out string message)
    {
        lock (_lock)
        {
            try
            {
                var list = GetSites();

                var site =
                    list.FirstOrDefault(x =>
                        x.Id == id);

                if (site == null)
                {
                    message = "站点不存在";

                    return false;
                }

                list.Remove(site);

                SaveSites(list);

                message = "删除成功";

                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;

                return false;
            }
        }
    }
}