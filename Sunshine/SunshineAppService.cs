using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using SunshineSteamAppManager.Models;

namespace SunshineSteamAppManager.Sunshine;

public sealed class SunshineAppService
{
    private static readonly Regex SteamUriRegex = new(
        @"steam://rungameid/(?<appid>\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task<SunshineAppDocument> LoadOrCreateAsync(string appsJsonPath, CancellationToken cancellationToken = default)
    {
        JsonObject root;

        if (File.Exists(appsJsonPath))
        {
            var text = await File.ReadAllTextAsync(appsJsonPath, cancellationToken);
            root = JsonNode.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text)?.AsObject()
                ?? new JsonObject();
        }
        else
        {
            root = new JsonObject();
        }

        if (root["apps"] is not JsonArray apps)
        {
            apps = new JsonArray();
            root["apps"] = apps;
        }

        if (root["env"] is not JsonObject)
        {
            root["env"] = new JsonObject();
        }

        return new SunshineAppDocument(appsJsonPath, root, apps);
    }

    public async Task WriteAsync(SunshineAppDocument document, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(document.FilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (document.Root["env"] is not JsonObject)
        {
            document.Root["env"] = new JsonObject();
        }

        var json = document.Root.ToJsonString(JsonOptions);
        await File.WriteAllTextAsync(document.FilePath, json + Environment.NewLine, cancellationToken);
    }

    public JsonObject CreateGeneratedSteamApp(SteamGame game, string imagePath)
    {
        return new JsonObject
        {
            ["auto-detach"] = true,
            ["cmd"] = game.SteamUri,
            ["elevated"] = false,
            ["exclude-global-prep-cmd"] = false,
            ["exit-timeout"] = 5,
            ["image-path"] = imagePath,
            ["name"] = game.Name,
            ["output"] = "",
            ["wait-all"] = true
        };
    }

    public void ApplyGeneratedSteamFields(JsonObject app, SteamGame game, string imagePath, bool preserveExistingName)
    {
        app["auto-detach"] = true;
        app["cmd"] = game.SteamUri;
        app["elevated"] = false;
        app["exclude-global-prep-cmd"] = false;
        app["exit-timeout"] = 5;
        app["image-path"] = imagePath;
        app["name"] = preserveExistingName && !string.IsNullOrWhiteSpace(GetString(app, "name"))
            ? GetString(app, "name")
            : game.Name;
        app["output"] = "";
        app["wait-all"] = true;

        app.Remove("detached");
        app.Remove("prep-cmd");
        app.Remove("working-dir");
    }

    public (int Index, JsonObject App)? FindMatchingApp(JsonArray apps, string appId, ManagedState managedState)
    {
        var steamUri = $"steam://rungameid/{appId}";

        for (var index = 0; index < apps.Count; index++)
        {
            if (apps[index] is JsonObject app && TryGetSteamAppId(app, out var existingAppId) &&
                string.Equals(existingAppId, appId, StringComparison.OrdinalIgnoreCase))
            {
                return (index, app);
            }
        }

        var record = managedState.Find(appId);
        if (record is null)
        {
            return null;
        }

        for (var index = 0; index < apps.Count; index++)
        {
            if (apps[index] is not JsonObject app)
            {
                continue;
            }

            var cmd = GetString(app, "cmd");
            var name = GetString(app, "name");
            if (string.Equals(cmd, record.LastKnownCommand, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(cmd, steamUri, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, record.SunshineAppName, StringComparison.CurrentCultureIgnoreCase))
            {
                return (index, app);
            }
        }

        return null;
    }

    public bool NeedsRepair(JsonObject app, SteamGame? game, AppSettings settings)
    {
        if (!TryGetSteamAppId(app, out var appId) && game is null)
        {
            return false;
        }

        var expectedAppId = game?.AppId ?? appId;
        if (string.IsNullOrWhiteSpace(expectedAppId))
        {
            return false;
        }

        var expectedUri = $"steam://rungameid/{expectedAppId}";

        if (!string.Equals(GetString(app, "cmd"), expectedUri, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (ContainsSteamUri(app["detached"], expectedAppId))
        {
            return true;
        }

        if (app.ContainsKey("prep-cmd") || app.ContainsKey("working-dir"))
        {
            return true;
        }

        if (!HasBoolean(app, "auto-detach", true) ||
            !HasBoolean(app, "wait-all", true) ||
            !HasBoolean(app, "elevated", false) ||
            !HasBoolean(app, "exclude-global-prep-cmd", false))
        {
            return true;
        }

        if (!HasNumber(app, "exit-timeout", 5))
        {
            return true;
        }

        if (!string.Equals(GetString(app, "output"), "", StringComparison.Ordinal))
        {
            return true;
        }

        if (game is not null && !settings.AdvancedOptions.PreserveManualSunshineAppNames &&
            !string.Equals(GetString(app, "name"), game.Name, StringComparison.CurrentCulture))
        {
            return true;
        }

        var expectedCover = GetExpectedCoverPath(settings.CoverOutputFolder, expectedAppId);
        if (File.Exists(expectedCover) &&
            !string.Equals(GetString(app, "image-path"), expectedCover, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    public PreviewPlan BuildPreview(
        IEnumerable<SteamGame> selectedGames,
        IReadOnlyCollection<SteamGame> installedGames,
        SunshineAppDocument document,
        ManagedState managedState,
        AppSettings settings,
        string proposedBackupPath)
    {
        var plan = new PreviewPlan { ProposedBackupPath = proposedBackupPath };
        var refreshCovers = settings.AdvancedOptions.RefreshExistingCoverArt;
        var hasApiKey = !string.IsNullOrWhiteSpace(settings.SteamGridDbApiKey);

        foreach (var game in selectedGames)
        {
            var coverPath = GetExpectedCoverPath(settings.CoverOutputFolder, game.AppId);
            if (hasApiKey && (refreshCovers || !File.Exists(coverPath)))
            {
                plan.CoversToDownload.Add($"{game.Name} ({game.AppId})");
            }

            var match = FindMatchingApp(document.Apps, game.AppId, managedState);
            if (match is null)
            {
                plan.AppsToAdd.Add($"{game.Name} ({game.AppId})");
                continue;
            }

            if (NeedsRepair(match.Value.App, game, settings))
            {
                plan.AppsToRepair.Add($"{game.Name} ({game.AppId})");
            }
            else
            {
                plan.AppsToUpdate.Add($"{game.Name} ({game.AppId})");
            }
        }

        if (settings.AdvancedOptions.RemoveStaleManagedApps)
        {
            var installedIds = installedGames.Select(game => game.AppId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var record in managedState.Apps.Where(record => record.CreatedByThisTool && !installedIds.Contains(record.SteamAppId)))
            {
                plan.StaleManagedApps.Add($"{record.SunshineAppName} ({record.SteamAppId})");
            }
        }

        return plan;
    }

    public IReadOnlyList<SunshineAppRow> BuildRows(SunshineAppDocument document, ManagedState managedState, AppSettings settings)
    {
        var rows = new List<SunshineAppRow>();
        for (var index = 0; index < document.Apps.Count; index++)
        {
            if (document.Apps[index] is not JsonObject app)
            {
                continue;
            }

            TryGetSteamAppId(app, out var appId);
            var isManaged = !string.IsNullOrWhiteSpace(appId) && managedState.Find(appId) is not null;
            rows.Add(new SunshineAppRow
            {
                Index = index,
                AppName = GetString(app, "name") ?? "",
                Command = GetString(app, "cmd") ?? "",
                ImagePath = GetString(app, "image-path") ?? "",
                IsSteamUriApp = !string.IsNullOrWhiteSpace(appId),
                IsManagedByThisTool = isManaged,
                NeedsRepair = !string.IsNullOrWhiteSpace(appId) && NeedsRepair(app, null, settings),
                SteamAppId = appId ?? ""
            });
        }

        return rows;
    }

    public IReadOnlyList<string> RemoveStaleManagedApps(
        SunshineAppDocument document,
        ManagedState managedState,
        IReadOnlyCollection<SteamGame> installedGames)
    {
        var removed = new List<string>();
        var installedIds = installedGames.Select(game => game.AppId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var index = document.Apps.Count - 1; index >= 0; index--)
        {
            if (document.Apps[index] is not JsonObject app ||
                !TryGetSteamAppId(app, out var appId) ||
                string.IsNullOrWhiteSpace(appId) ||
                installedIds.Contains(appId))
            {
                continue;
            }

            var record = managedState.Find(appId);
            if (record is not { CreatedByThisTool: true })
            {
                continue;
            }

            removed.Add($"{GetString(app, "name") ?? record.SunshineAppName} ({appId})");
            document.Apps.RemoveAt(index);
            managedState.Remove(appId);
        }

        return removed;
    }

    public void RemoveManagedAppsByIndex(SunshineAppDocument document, ManagedState managedState, IEnumerable<int> indexes)
    {
        foreach (var index in indexes.OrderByDescending(value => value))
        {
            if (index < 0 || index >= document.Apps.Count || document.Apps[index] is not JsonObject app)
            {
                continue;
            }

            if (!TryGetSteamAppId(app, out var appId) || string.IsNullOrWhiteSpace(appId))
            {
                continue;
            }

            var record = managedState.Find(appId);
            if (record is not { CreatedByThisTool: true })
            {
                continue;
            }

            document.Apps.RemoveAt(index);
            managedState.Remove(appId);
        }
    }

    public void RepairSunshineRows(SunshineAppDocument document, ManagedState managedState, IEnumerable<SunshineAppRow> rows, AppSettings settings)
    {
        foreach (var row in rows)
        {
            if (row.Index < 0 || row.Index >= document.Apps.Count || document.Apps[row.Index] is not JsonObject app)
            {
                continue;
            }

            if (!row.IsSteamUriApp || string.IsNullOrWhiteSpace(row.SteamAppId))
            {
                continue;
            }

            var name = string.IsNullOrWhiteSpace(row.AppName) ? $"Steam {row.SteamAppId}" : row.AppName;
            var coverPath = File.Exists(GetExpectedCoverPath(settings.CoverOutputFolder, row.SteamAppId))
                ? GetExpectedCoverPath(settings.CoverOutputFolder, row.SteamAppId)
                : (GetString(app, "image-path") ?? "");

            var game = new SteamGame
            {
                AppId = row.SteamAppId,
                Name = name
            };

            ApplyGeneratedSteamFields(app, game, coverPath, settings.AdvancedOptions.PreserveManualSunshineAppNames);
            managedState.Upsert(row.SteamAppId, GetString(app, "name") ?? name, coverPath, game.SteamUri);
        }
    }

    public static string GetExpectedCoverPath(string coverFolder, string appId)
    {
        return string.IsNullOrWhiteSpace(coverFolder)
            ? ""
            : Path.Combine(coverFolder, $"steam_{appId}.png");
    }

    public static bool TryGetSteamAppId(JsonObject app, out string? appId)
    {
        var command = GetString(app, "cmd");
        if (TryExtractSteamAppId(command, out appId))
        {
            return true;
        }

        if (TryExtractSteamAppIdFromNode(app["detached"], out appId))
        {
            return true;
        }

        appId = null;
        return false;
    }

    public static string? GetString(JsonObject app, string propertyName)
    {
        return app.TryGetPropertyValue(propertyName, out var value) && value is JsonValue jsonValue &&
            jsonValue.TryGetValue<string>(out var text)
            ? text
            : null;
    }

    private static bool TryExtractSteamAppId(string? text, out string? appId)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            appId = null;
            return false;
        }

        var match = SteamUriRegex.Match(text);
        appId = match.Success ? match.Groups["appid"].Value : null;
        return match.Success;
    }

    private static bool TryExtractSteamAppIdFromNode(JsonNode? node, out string? appId)
    {
        switch (node)
        {
            case null:
                appId = null;
                return false;
            case JsonValue value when value.TryGetValue<string>(out var text):
                return TryExtractSteamAppId(text, out appId);
            case JsonArray array:
                foreach (var child in array)
                {
                    if (TryExtractSteamAppIdFromNode(child, out appId))
                    {
                        return true;
                    }
                }
                break;
            case JsonObject obj:
                foreach (var (_, child) in obj)
                {
                    if (TryExtractSteamAppIdFromNode(child, out appId))
                    {
                        return true;
                    }
                }
                break;
        }

        appId = null;
        return false;
    }

    private static bool ContainsSteamUri(JsonNode? node, string appId)
    {
        return TryExtractSteamAppIdFromNode(node, out var foundAppId) &&
            string.Equals(foundAppId, appId, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasBoolean(JsonObject app, string propertyName, bool expected)
    {
        return app.TryGetPropertyValue(propertyName, out var value) &&
            value is JsonValue jsonValue &&
            jsonValue.TryGetValue<bool>(out var actual) &&
            actual == expected;
    }

    private static bool HasNumber(JsonObject app, string propertyName, int expected)
    {
        return app.TryGetPropertyValue(propertyName, out var value) &&
            value is JsonValue jsonValue &&
            jsonValue.TryGetValue<int>(out var actual) &&
            actual == expected;
    }
}

