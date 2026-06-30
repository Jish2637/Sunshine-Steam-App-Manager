namespace SunshineSteamAppManager.Models;

public sealed class SunshineAppRow
{
    public int Index { get; set; }
    public string AppName { get; set; } = "";
    public string Command { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public bool IsSteamUriApp { get; set; }
    public bool IsManagedByThisTool { get; set; }
    public bool NeedsRepair { get; set; }
    public string SteamAppId { get; set; } = "";
}
