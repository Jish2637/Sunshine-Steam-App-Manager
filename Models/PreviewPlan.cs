namespace SunshineSteamAppManager.Models;

public sealed class PreviewPlan
{
    public List<string> AppsToAdd { get; } = new();
    public List<string> AppsToUpdate { get; } = new();
    public List<string> AppsToRepair { get; } = new();
    public List<string> CoversToDownload { get; } = new();
    public List<string> StaleManagedApps { get; } = new();
    public string ProposedBackupPath { get; set; } = "";

    public bool HasChanges =>
        AppsToAdd.Count > 0 ||
        AppsToUpdate.Count > 0 ||
        AppsToRepair.Count > 0 ||
        StaleManagedApps.Count > 0;
}
