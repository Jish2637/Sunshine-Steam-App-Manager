using Microsoft.Win32;

namespace SunshineSteamAppManager.Steam;

public sealed class SteamLocator
{
    public string? DetectSteamInstallPath()
    {
        foreach (var path in ReadRegistryPaths().Concat(DefaultSteamPaths()))
        {
            if (Directory.Exists(path) && File.Exists(Path.Combine(path, "steamapps", "libraryfolders.vdf")))
            {
                return path;
            }
        }

        return null;
    }

    public string? DetectLibraryFoldersPath()
    {
        var installPath = DetectSteamInstallPath();
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return null;
        }

        var libraryFolders = Path.Combine(installPath, "steamapps", "libraryfolders.vdf");
        return File.Exists(libraryFolders) ? libraryFolders : null;
    }

    private static IEnumerable<string> DefaultSteamPaths()
    {
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam");
    }

    private static IEnumerable<string> ReadRegistryPaths()
    {
        var candidates = new[]
        {
            (RegistryHive.CurrentUser, @"Software\Valve\Steam"),
            (RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam"),
            (RegistryHive.LocalMachine, @"SOFTWARE\Valve\Steam")
        };

        foreach (var (hive, subKeyPath) in candidates)
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
            using var key = baseKey.OpenSubKey(subKeyPath);
            var value = key?.GetValue("InstallPath") as string;
            if (!string.IsNullOrWhiteSpace(value))
            {
                yield return value;
            }
        }
    }
}
