using System.Text.Json;
using IPTVPlayer.App.Models;

namespace IPTVPlayer.App.Api;

public class XtreamCodesClient
{
    private readonly HttpClient _httpClient;

    public XtreamCodesClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<IReadOnlyList<Channel>> LoadLiveChannelsAsync(XtreamCredentials credentials, CancellationToken cancellationToken = default)
    {
        var baseUri = BuildBase(credentials, "get_live_streams");
        using var response = await _httpClient.GetAsync(baseUri, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseXtreamChannels(json, credentials, "live");
    }

    public async Task<IReadOnlyList<Channel>> LoadMoviesAsync(XtreamCredentials credentials, CancellationToken cancellationToken = default)
    {
        var baseUri = BuildBase(credentials, "get_vod_streams");
        using var response = await _httpClient.GetAsync(baseUri, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseXtreamChannels(json, credentials, "movie");
    }

    public async Task<IReadOnlyList<Channel>> LoadSeriesAsync(XtreamCredentials credentials, CancellationToken cancellationToken = default)
    {
        var baseUri = BuildBase(credentials, "get_series");
        using var response = await _httpClient.GetAsync(baseUri, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseXtreamChannels(json, credentials, "series");
    }

    private static string BuildBase(XtreamCredentials credentials, string action)
    {
        var server = credentials.ServerUrl.TrimEnd('/');
        return $"{server}/player_api.php?username={Uri.EscapeDataString(credentials.Username)}&password={Uri.EscapeDataString(credentials.Password)}&action={action}";
    }

    private static IReadOnlyList<Channel> ParseXtreamChannels(string json, XtreamCredentials credentials, string type)
    {
        var channels = new List<Channel>();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            return channels;
        }

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var streamId = item.TryGetProperty("stream_id", out var idProp) ? idProp.ToString() : item.GetProperty("series_id").ToString();
            var name = item.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : "Unknown";
            var group = item.TryGetProperty("category_name", out var groupProp) ? groupProp.GetString() : type.ToUpperInvariant();
            var icon = item.TryGetProperty("stream_icon", out var iconProp) ? iconProp.GetString() : string.Empty;

            var extension = type == "movie" ? "mp4" : "m3u8";
            var streamUrl = BuildStreamUrl(credentials, type, streamId, extension);

            channels.Add(new Channel
            {
                Id = streamId,
                Name = name ?? "Unnamed",
                Group = group ?? "Ungrouped",
                LogoUrl = icon ?? string.Empty,
                StreamUrl = streamUrl,
                SourceType = "Xtream"
            });
        }

        return channels;
    }

    private static string BuildStreamUrl(XtreamCredentials credentials, string type, string streamId, string extension)
    {
        var server = credentials.ServerUrl.TrimEnd('/');
        return type switch
        {
            "live" => $"{server}/live/{credentials.Username}/{credentials.Password}/{streamId}.{extension}",
            "movie" => $"{server}/movie/{credentials.Username}/{credentials.Password}/{streamId}.{extension}",
            _ => $"{server}/series/{credentials.Username}/{credentials.Password}/{streamId}.{extension}"
        };
    }
}
