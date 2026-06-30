using System.Text.Json.Nodes;

namespace SunshineSteamAppManager.Sunshine;

public sealed class SunshineAppDocument
{
    public SunshineAppDocument(string filePath, JsonObject root, JsonArray apps)
    {
        FilePath = filePath;
        Root = root;
        Apps = apps;
    }

    public string FilePath { get; }
    public JsonObject Root { get; }
    public JsonArray Apps { get; }
}
