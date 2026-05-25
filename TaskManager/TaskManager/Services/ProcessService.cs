using System.IO;
using System.Management;
using TaskManager.Models;

namespace TaskManager.Services;

public class ProcessService
{
    public async Task<List<ProcessInfo>> FindDotnetProcessesAsync(string searchName, CancellationToken ct = default)
    {
        var results = new List<ProcessInfo>();
        var commandLines = await GetDotnetCommandLinesAsync(ct);

        foreach (var (pid, cmdLine) in commandLines)
        {
            if (cmdLine.IndexOf(searchName, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            results.Add(new ProcessInfo
            {
                ProcessId = pid,
                TaskGroup = searchName,
                TaskName = cmdLine
            });
        }

        return results;
    }

    public async Task<List<ProcessInfo>> FindProcessesByDirectoryAsync(string directory, CancellationToken ct = default)
    {
        var results = new List<ProcessInfo>();
        var commandLines = await GetDotnetCommandLinesAsync(ct);

        foreach (var (pid, cmdLine) in commandLines)
        {
            string dllPath = ExtractDllPathFromCommandLine(cmdLine);
            if (string.IsNullOrEmpty(dllPath))
                continue;

            string actualDir = Path.IsPathRooted(dllPath)
                ? Path.GetDirectoryName(dllPath) ?? directory
                : directory;

            if (!string.Equals(actualDir, directory, StringComparison.OrdinalIgnoreCase))
                continue;

            results.Add(new ProcessInfo
            {
                ProcessId = pid,
                TaskGroup = directory,
                TaskName = $"{cmdLine} [DLL: {dllPath}]"
            });
        }

        return results;
    }

    private static async Task<List<(int Pid, string CmdLine)>> GetDotnetCommandLinesAsync(CancellationToken ct)
    {
        var result = new List<(int, string)>();

        await Task.Run(() =>
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name = 'dotnet.exe'");
            using var objects = searcher.Get();

            foreach (ManagementBaseObject obj in objects)
            {
                if (ct.IsCancellationRequested) break;

                int pid = Convert.ToInt32(obj["ProcessId"]);
                string cmdLine = obj["CommandLine"]?.ToString() ?? string.Empty;
                result.Add((pid, cmdLine));
            }
        }, ct);

        return result;
    }

    public void KillProcess(int processId)
    {
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(processId);
            process.Kill();
        }
        catch (ArgumentException)
        {
            // 进程已不存在
        }
    }

    private static string ExtractDllPathFromCommandLine(string cmd)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            cmd, @"dotnet\s+(""[^""]+\.dll""|[^\s]+\.dll)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return match.Success ? match.Groups[1].Value.Trim('"') : "";
    }
}
