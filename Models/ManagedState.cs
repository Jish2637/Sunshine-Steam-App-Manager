namespace SunshineSteamAppManager.Models;

public sealed class ManagedState
{
    public List<ManagedAppRecord> Apps { get; set; } = new();

    public ManagedAppRecord? Find(string steamAppId)
    {
        return Apps.FirstOrDefault(app => string.Equals(app.SteamAppId, steamAppId, StringComparison.OrdinalIgnoreCase));
    }

    public void Upsert(string steamAppId, string name, string imagePath, string command)
    {
        var existing = Find(steamAppId);
        if (existing is null)
        {
            Apps.Add(new ManagedAppRecord
            {
                SteamAppId = steamAppId,
                SunshineAppName = name,
                ImagePath = imagePath,
                DateAdded = DateTimeOffset.Now,
                DateLastUpdated = DateTimeOffset.Now,
                LastKnownCommand = command,
                CreatedByThisTool = true
            });
            return;
        }

        existing.SunshineAppName = name;
        existing.ImagePath = imagePath;
        existing.DateLastUpdated = DateTimeOffset.Now;
        existing.LastKnownCommand = command;
        existing.CreatedByThisTool = true;
    }

    public void Remove(string steamAppId)
    {
        Apps.RemoveAll(app => string.Equals(app.SteamAppId, steamAppId, StringComparison.OrdinalIgnoreCase));
    }
}
