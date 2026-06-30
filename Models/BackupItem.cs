namespace SunshineSteamAppManager.Models;

public sealed class BackupItem
{
    public string FilePath { get; set; } = "";
    public DateTime LastWriteTime { get; set; }
    public long Length { get; set; }

    public override string ToString()
    {
        return $"{Path.GetFileName(FilePath)} ({LastWriteTime:g}, {Length:N0} bytes)";
    }
}
