using System.Text.Json;
using SunshineSteamAppManager.Models;

namespace SunshineSteamAppManager.Storage;

public sealed class SettingsStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        AppDataPaths.Ensure();

        if (!File.Exists(AppDataPaths.SettingsPath))
        {
            return AppSettings.CreateDefault();
        }

        try
        {
            await using var stream = File.OpenRead(AppDataPaths.SettingsPath);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, cancellationToken);
            return Normalize(settings ?? AppSettings.CreateDefault());
        }
        catch
        {
            return AppSettings.CreateDefault();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        AppDataPaths.Ensure();
        await using var stream = File.Create(AppDataPaths.SettingsPath);
        await JsonSerializer.SerializeAsync(stream, Normalize(settings), JsonOptions, cancellationToken);
    }

    private static AppSettings Normalize(AppSettings settings)
    {
        var defaults = AppSettings.CreateDefault();

        settings.SteamInstallPath = string.IsNullOrWhiteSpace(settings.SteamInstallPath)
            ? defaults.SteamInstallPath
            : settings.SteamInstallPath;
        settings.SteamLibraryFoldersPath = string.IsNullOrWhiteSpace(settings.SteamLibraryFoldersPath)
            ? defaults.SteamLibraryFoldersPath
            : settings.SteamLibraryFoldersPath;
        settings.SunshineAppsJsonPath = string.IsNullOrWhiteSpace(settings.SunshineAppsJsonPath)
            ? defaults.SunshineAppsJsonPath
            : settings.SunshineAppsJsonPath;
        settings.CoverOutputFolder = string.IsNullOrWhiteSpace(settings.CoverOutputFolder)
            ? defaults.CoverOutputFolder
            : settings.CoverOutputFolder;
        settings.AdvancedOptions ??= new AdvancedOptions();

        return settings;
    }
}
