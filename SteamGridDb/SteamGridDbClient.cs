using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using SunshineSteamAppManager.Logging;
using SunshineSteamAppManager.Sunshine;

namespace SunshineSteamAppManager.SteamGridDb;

public sealed class SteamGridDbClient
{
    private readonly HttpClient _httpClient;
    private readonly OperationLogger _logger;

    public SteamGridDbClient(OperationLogger logger)
    {
        _logger = logger;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("SunshineSteamAppManager/1.0");
    }

    public async Task<string?> DownloadCoverAsync(
        string appId,
        string gameName,
        string apiKey,
        string coverFolder,
        bool refreshExisting,
        CancellationToken cancellationToken = default)
    {
        var targetPath = SunshineAppService.GetExpectedCoverPath(coverFolder, appId);
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            await _logger.LogAsync($"Cover skipped for {gameName} ({appId}) because no cover folder is configured.", cancellationToken);
            return null;
        }

        if (File.Exists(targetPath) && !refreshExisting)
        {
            return targetPath;
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            await _logger.LogAsync($"Cover skipped for {gameName} ({appId}) because no SteamGridDB API key is configured.", cancellationToken);
            return File.Exists(targetPath) ? targetPath : null;
        }

        try
        {
            Directory.CreateDirectory(coverFolder);
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://www.steamgriddb.com/api/v2/grids/steam/{Uri.EscapeDataString(appId)}?types=static&dimensions=600x900&formats=png");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                await _logger.LogAsync($"SteamGridDB cover lookup failed for {gameName} ({appId}): {(int)response.StatusCode} {response.ReasonPhrase}", cancellationToken);
                return File.Exists(targetPath) ? targetPath : null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var url = FindBestImageUrl(json);
            if (string.IsNullOrWhiteSpace(url))
            {
                await _logger.LogAsync($"SteamGridDB returned no PNG cover for {gameName} ({appId}).", cancellationToken);
                return File.Exists(targetPath) ? targetPath : null;
            }

            var bytes = await _httpClient.GetByteArrayAsync(url, cancellationToken);
            await File.WriteAllBytesAsync(targetPath, bytes, cancellationToken);
            await _logger.LogAsync($"Downloaded cover for {gameName} ({appId}) to {targetPath}", cancellationToken);
            return targetPath;
        }
        catch (Exception ex)
        {
            await _logger.LogAsync($"Cover download failed for {gameName} ({appId}): {ex.Message}", cancellationToken);
            return File.Exists(targetPath) ? targetPath : null;
        }
    }

    public async Task<bool> TestApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            await _logger.LogAsync("SteamGridDB key test skipped because no key was entered.", cancellationToken);
            return false;
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "https://www.steamgriddb.com/api/v2/grids/steam/10?types=static&dimensions=600x900&formats=png");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var works = response.IsSuccessStatusCode;
            await _logger.LogAsync(works ? "SteamGridDB key test passed." : $"SteamGridDB key test failed: {(int)response.StatusCode} {response.ReasonPhrase}", cancellationToken);
            return works;
        }
        catch (Exception ex)
        {
            await _logger.LogAsync($"SteamGridDB key test failed: {ex.Message}", cancellationToken);
            return false;
        }
    }

    private static string? FindBestImageUrl(string json)
    {
        var root = JsonNode.Parse(json)?.AsObject();
        if (root?["data"] is not JsonArray data)
        {
            return null;
        }

        foreach (var node in data.OfType<JsonObject>())
        {
            if (node["url"] is JsonValue value && value.TryGetValue<string>(out var url) &&
                !string.IsNullOrWhiteSpace(url) &&
                url.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }
        }

        foreach (var node in data.OfType<JsonObject>())
        {
            if (node["url"] is JsonValue value && value.TryGetValue<string>(out var url) &&
                !string.IsNullOrWhiteSpace(url))
            {
                return url;
            }
        }

        return null;
    }
}
