using System.Diagnostics;
using SunshineSteamAppManager.Logging;

namespace SunshineSteamAppManager.Services;

public sealed class SunshineProcessService
{
    private readonly OperationLogger _logger;

    public SunshineProcessService(OperationLogger logger)
    {
        _logger = logger;
    }

    public async Task RestartAsync(CancellationToken cancellationToken = default)
    {
        var processes = Process.GetProcessesByName("sunshine");
        if (processes.Length == 0)
        {
            await _logger.LogAsync("Sunshine restart was requested, but no running sunshine process was found.", cancellationToken);
            return;
        }

        var executablePath = TryGetExecutablePath(processes[0]);
        foreach (var process in processes)
        {
            try
            {
                process.Kill();
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync($"Failed to stop Sunshine process {process.Id}: {ex.Message}", cancellationToken);
            }
        }

        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
        {
            await _logger.LogAsync("Sunshine was stopped, but its executable path could not be found for restart.", cancellationToken);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = true
        });
        await _logger.LogAsync("Sunshine process restarted by user request.", cancellationToken);
    }

    private static string? TryGetExecutablePath(Process process)
    {
        try
        {
            return process.MainModule?.FileName;
        }
        catch
        {
            return null;
        }
    }
}
