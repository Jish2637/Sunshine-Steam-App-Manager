using SunshineSteamAppManager.Storage;

namespace SunshineSteamAppManager.Logging;

public sealed class OperationLogger
{
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public event EventHandler<string>? MessageLogged;

    public async Task LogAsync(string message, CancellationToken cancellationToken = default)
    {
        AppDataPaths.Ensure();

        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss} {Sanitize(message)}";
        MessageLogged?.Invoke(this, line);

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(AppDataPaths.LogPath, line + Environment.NewLine, cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public void Log(string message)
    {
        _ = LogAsync(message);
    }

    private static string Sanitize(string message)
    {
        return message.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
    }
}
