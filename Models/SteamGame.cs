namespace SunshineSteamAppManager.Models;

public sealed class SteamGame
{
    public string AppId { get; set; } = "";
    public string Name { get; set; } = "";
    public string InstallDir { get; set; } = "";
    public string InstallPath { get; set; } = "";
    public string ManifestPath { get; set; } = "";
    public string LibraryPath { get; set; } = "";

    public string SteamUri => $"steam://rungameid/{AppId}";
}
