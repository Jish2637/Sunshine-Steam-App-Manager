namespace SunshineSteamAppManager.Models;

public sealed class ManagedAppRecord
{
    public string SteamAppId { get; set; } = "";
    public string SunshineAppName { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public DateTimeOffset DateAdded { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset DateLastUpdated { get; set; } = DateTimeOffset.Now;
    public string LastKnownCommand { get; set; } = "";
    public bool CreatedByThisTool { get; set; } = true;
}
