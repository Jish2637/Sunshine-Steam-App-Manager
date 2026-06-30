namespace SunshineSteamAppManager.Storage;

public static class AppDataPaths
{
    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SunshineSteamAppManager");

    public static string SettingsPath => Path.Combine(Root, "settings.json");
    public static string ManagedAppsPath => Path.Combine(Root, "managed-apps.json");
    public static string LogsFolder => Path.Combine(Root, "logs");
    public static string LogPath => Path.Combine(LogsFolder, "app.log");

    public static void Ensure()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(LogsFolder);
    }
}
