using System.IO;
using Newtonsoft.Json;
using TaskManager.Models;

namespace TaskManager.Services;

public class JsonStorageService
{
    private readonly string _cacheDirectory;
    private const string JsonFileName = "TaskGroup.json";

    public JsonStorageService()
    {
        _cacheDirectory = Path.Combine(Path.GetTempPath(), "TaskGroup", "Cache");
        Directory.CreateDirectory(_cacheDirectory);
    }

    public string CacheDirectory => _cacheDirectory;

    public List<TaskGroupInfo> LoadTaskGroups()
    {
        string filePath = Path.Combine(_cacheDirectory, JsonFileName);
        if (!File.Exists(filePath))
            return new List<TaskGroupInfo>();

        string json = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<List<TaskGroupInfo>>(json) ?? new List<TaskGroupInfo>();
    }

    public void SaveTaskGroups(List<TaskGroupInfo> groups)
    {
        string json = JsonConvert.SerializeObject(groups);
        File.WriteAllText(Path.Combine(_cacheDirectory, JsonFileName), json);
    }
}
