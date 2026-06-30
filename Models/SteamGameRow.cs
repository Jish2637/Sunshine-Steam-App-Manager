namespace SunshineSteamAppManager.Models;

public sealed class SteamGameRow
{
    public bool Import { get; set; }
    public string AppId { get; set; } = "";
    public string Name { get; set; } = "";
    public string InstallPath { get; set; } = "";
    public string CoverStatus { get; set; } = "";
    public string SunshineStatus { get; set; } = "";
    public string ManifestPath { get; set; } = "";
    public string LibraryPath { get; set; } = "";
    public string InstallDir { get; set; } = "";

    public string FriendlyCoverStatus => CoverStatus switch
    {
        "Downloaded" => "Cover ready",
        "Missing" => "Cover missing",
        _ => string.IsNullOrWhiteSpace(CoverStatus) ? "Not checked" : CoverStatus
    };

    public string FriendlySunshineStatus => SunshineStatus switch
    {
        "New" => "Ready to add",
        "Existing" => "Already added",
        "Needs Repair" => "Can be fixed",
        "Ignored" => "Check setup first",
        _ => string.IsNullOrWhiteSpace(SunshineStatus) ? "Not checked" : SunshineStatus
    };

    public SteamGame ToSteamGame()
    {
        return new SteamGame
        {
            AppId = AppId,
            Name = Name,
            InstallDir = InstallDir,
            InstallPath = InstallPath,
            ManifestPath = ManifestPath,
            LibraryPath = LibraryPath
        };
    }

    public static SteamGameRow FromGame(SteamGame game)
    {
        return new SteamGameRow
        {
            AppId = game.AppId,
            Name = game.Name,
            InstallDir = game.InstallDir,
            InstallPath = game.InstallPath,
            ManifestPath = game.ManifestPath,
            LibraryPath = game.LibraryPath
        };
    }
}
