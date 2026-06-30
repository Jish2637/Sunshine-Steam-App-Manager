using System.Text.Json;
using SunshineSteamAppManager.Models;

namespace SunshineSteamAppManager.Storage;

public sealed class ManagedStateStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task<ManagedState> LoadAsync(CancellationToken cancellationToken = default)
    {
        AppDataPaths.Ensure();

        if (!File.Exists(AppDataPaths.ManagedAppsPath))
        {
            return new ManagedState();
        }

        try
        {
            await using var stream = File.OpenRead(AppDataPaths.ManagedAppsPath);
            return await JsonSerializer.DeserializeAsync<ManagedState>(stream, JsonOptions, cancellationToken)
                ?? new ManagedState();
        }
        catch
        {
            return new ManagedState();
        }
    }

    public async Task SaveAsync(ManagedState state, CancellationToken cancellationToken = default)
    {
        AppDataPaths.Ensure();
        await using var stream = File.Create(AppDataPaths.ManagedAppsPath);
        await JsonSerializer.SerializeAsync(stream, state, JsonOptions, cancellationToken);
    }
}
