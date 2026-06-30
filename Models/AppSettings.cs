namespace SunshineSteamAppManager.Models;

public sealed class AppSettings
{
    public string SteamInstallPath { get; set; } = "";
    public string SteamLibraryFoldersPath { get; set; } = "";
    public string SunshineAppsJsonPath { get; set; } = "";
    public string CoverOutputFolder { get; set; } = "";
    public string SteamGridDbApiKey { get; set; } = "";
    public AdvancedOptions AdvancedOptions { get; set; } = new();

    public static AppSettings CreateDefault()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        return new AppSettings
        {
            SteamInstallPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Steam"),
            SteamLibraryFoldersPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Steam",
                "steamapps",
                "libraryfolders.vdf"),
            SunshineAppsJsonPath = Path.Combine(programFiles, "Sunshine", "config", "apps.json"),
            CoverOutputFolder = Path.Combine(programFiles, "Sunshine", "config", "covers"),
            AdvancedOptions = new AdvancedOptions()
        };
    }
}
