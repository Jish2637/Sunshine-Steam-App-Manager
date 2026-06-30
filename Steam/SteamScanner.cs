using SunshineSteamAppManager.Logging;
using SunshineSteamAppManager.Models;

namespace SunshineSteamAppManager.Steam;

public sealed class SteamScanner
{
    private readonly OperationLogger _logger;

    public SteamScanner(OperationLogger logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<SteamGame>> ScanAsync(string libraryFoldersPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(libraryFoldersPath) || !File.Exists(libraryFoldersPath))
        {
            throw new FileNotFoundException("Steam libraryfolders.vdf was not found.", libraryFoldersPath);
        }

        var libraryPaths = await ReadLibraryPathsAsync(libraryFoldersPath, cancellationToken);
        var games = new List<SteamGame>();

        foreach (var libraryPath in libraryPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var steamAppsFolder = Path.Combine(libraryPath, "steamapps");
            if (!Directory.Exists(steamAppsFolder))
            {
                await _logger.LogAsync($"Steam library skipped because steamapps folder is missing: {steamAppsFolder}", cancellationToken);
                continue;
            }

            foreach (var manifestPath in Directory.EnumerateFiles(steamAppsFolder, "appmanifest_*.acf"))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var game = await ReadManifestAsync(manifestPath, libraryPath, cancellationToken);
                if (game is not null)
                {
                    games.Add(game);
                }
            }
        }

        return games
            .GroupBy(game => game.AppId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(game => game.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private async Task<IReadOnlyList<string>> ReadLibraryPathsAsync(string libraryFoldersPath, CancellationToken cancellationToken)
    {
        var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var steamAppsFolder = Path.GetDirectoryName(libraryFoldersPath);
        var steamRoot = steamAppsFolder is null ? null : Directory.GetParent(steamAppsFolder)?.FullName;

        if (!string.IsNullOrWhiteSpace(steamRoot))
        {
            libraries.Add(Path.GetFullPath(steamRoot));
        }

        try
        {
            var document = await VdfParser.ParseFileAsync(libraryFoldersPath, cancellationToken);
            var root = document.GetObject("libraryfolders") ?? document;

            foreach (var (_, value) in root.Values)
            {
                var path = value.ObjectValue?.GetString("path") ?? value.StringValue;
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                libraries.Add(Path.GetFullPath(path));
            }
        }
        catch (Exception ex)
        {
            await _logger.LogAsync($"Failed to parse Steam libraryfolders.vdf: {ex.Message}", cancellationToken);
            throw;
        }

        await _logger.LogAsync($"Found {libraries.Count} Steam library folder(s).", cancellationToken);
        return libraries.ToList();
    }

    private async Task<SteamGame?> ReadManifestAsync(string manifestPath, string libraryPath, CancellationToken cancellationToken)
    {
        try
        {
            var document = await VdfParser.ParseFileAsync(manifestPath, cancellationToken);
            var state = document.GetObject("AppState") ?? document;
            var appId = state.GetString("appid") ?? Path.GetFileNameWithoutExtension(manifestPath).Replace("appmanifest_", "", StringComparison.OrdinalIgnoreCase);
            var name = state.GetString("name");
            var installDir = state.GetString("installdir") ?? "";

            if (string.IsNullOrWhiteSpace(appId))
            {
                await _logger.LogAsync($"Steam manifest skipped because it has no appid: {manifestPath}", cancellationToken);
                return null;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                await _logger.LogAsync($"Steam manifest skipped because it has no game name: {manifestPath}", cancellationToken);
                return null;
            }

            return new SteamGame
            {
                AppId = appId.Trim(),
                Name = name.Trim(),
                InstallDir = installDir.Trim(),
                InstallPath = string.IsNullOrWhiteSpace(installDir)
                    ? ""
                    : Path.Combine(libraryPath, "steamapps", "common", installDir.Trim()),
                ManifestPath = manifestPath,
                LibraryPath = libraryPath
            };
        }
        catch (Exception ex)
        {
            await _logger.LogAsync($"Broken Steam manifest skipped: {manifestPath}. {ex.Message}", cancellationToken);
            return null;
        }
    }
}
