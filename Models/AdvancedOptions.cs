namespace SunshineSteamAppManager.Models;

public sealed class AdvancedOptions
{
    public bool RemoveStaleManagedApps { get; set; }
    public bool RefreshExistingCoverArt { get; set; }
    public bool PreserveManualSunshineAppNames { get; set; } = true;
    public bool RestartSunshineAfterApply { get; set; } = true;
}
