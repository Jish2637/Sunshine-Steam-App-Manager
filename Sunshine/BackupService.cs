using SunshineSteamAppManager.Models;

namespace SunshineSteamAppManager.Sunshine;

public sealed class BackupService
{
    public string CreateBackupPath(string appsJsonPath)
    {
        var folder = Path.GetDirectoryName(appsJsonPath);
        var fileName = Path.GetFileName(appsJsonPath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        return Path.Combine(folder ?? "", $"{fileName}.backup-{timestamp}");
    }

    public async Task<string> CreateBackupAsync(string appsJsonPath, string? proposedPath = null, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(appsJsonPath))
        {
            throw new FileNotFoundException("Cannot create a backup because apps.json does not exist.", appsJsonPath);
        }

        var backupPath = proposedPath ?? CreateBackupPath(appsJsonPath);
        backupPath = EnsureUnique(backupPath);
        await using var source = File.OpenRead(appsJsonPath);
        await using var destination = File.Create(backupPath);
        await source.CopyToAsync(destination, cancellationToken);
        return backupPath;
    }

    public async Task RestoreAsync(string appsJsonPath, string backupPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(backupPath))
        {
            throw new FileNotFoundException("Backup file was not found.", backupPath);
        }

        var directory = Path.GetDirectoryName(appsJsonPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var source = File.OpenRead(backupPath);
        await using var destination = File.Create(appsJsonPath);
        await source.CopyToAsync(destination, cancellationToken);
    }

    public IReadOnlyList<BackupItem> ListBackups(string appsJsonPath)
    {
        var folder = Path.GetDirectoryName(appsJsonPath);
        var fileName = Path.GetFileName(appsJsonPath);

        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return Array.Empty<BackupItem>();
        }

        return Directory.EnumerateFiles(folder, $"{fileName}.backup-*")
            .Select(path => new FileInfo(path))
            .OrderByDescending(info => info.LastWriteTime)
            .Select(info => new BackupItem
            {
                FilePath = info.FullName,
                LastWriteTime = info.LastWriteTime,
                Length = info.Length
            })
            .ToList();
    }

    private static string EnsureUnique(string path)
    {
        if (!File.Exists(path))
        {
            return path;
        }

        for (var index = 1; index < 1000; index++)
        {
            var candidate = $"{path}-{index}";
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }

        return $"{path}-{Guid.NewGuid():N}";
    }
}
