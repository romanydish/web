using System.Text.RegularExpressions;
using IPTVPlayer.App.Models;

namespace IPTVPlayer.App.Parsers;

public class M3UPlaylistParser
{
    private static readonly Regex HeaderRegex = new("#EXTINF:-1(?<meta>.*?),(?<name>.*)$", RegexOptions.Compiled);
    private static readonly Regex MetaRegex = new("(?<key>[a-zA-Z0-9\-]+)=\"(?<value>.*?)\"", RegexOptions.Compiled);

    public async Task<IReadOnlyList<Channel>> ParseFromUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient();
        var data = await httpClient.GetStringAsync(url, cancellationToken);
        return Parse(data);
    }

    public async Task<IReadOnlyList<Channel>> ParseFromFileAsync(string path, CancellationToken cancellationToken = default)
    {
        var data = await File.ReadAllTextAsync(path, cancellationToken);
        return Parse(data);
    }

    public IReadOnlyList<Channel> Parse(string text)
    {
        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var channels = new List<Channel>();

        for (var i = 0; i < lines.Length; i++)
        {
            if (!lines[i].StartsWith("#EXTINF", StringComparison.OrdinalIgnoreCase) || i + 1 >= lines.Length)
            {
                continue;
            }

            var infoLine = lines[i];
            var streamLine = lines[i + 1].Trim();
            var channel = ParseChannel(infoLine, streamLine);
            if (channel != null)
            {
                channels.Add(channel);
            }

            i++;
        }

        return channels;
    }

    private static Channel? ParseChannel(string infoLine, string streamUrl)
    {
        if (!Uri.TryCreate(streamUrl, UriKind.Absolute, out _))
        {
            return null;
        }

        var match = HeaderRegex.Match(infoLine);
        if (!match.Success)
        {
            return new Channel { Name = streamUrl, StreamUrl = streamUrl };
        }

        var metadataRaw = match.Groups["meta"].Value;
        var name = match.Groups["name"].Value.Trim();
        var metadata = ParseMetadata(metadataRaw);

        return new Channel
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Unnamed Channel" : name,
            StreamUrl = streamUrl,
            Group = metadata.GetValueOrDefault("group-title", "Ungrouped"),
            LogoUrl = metadata.GetValueOrDefault("tvg-logo", string.Empty),
            EpgChannelId = metadata.GetValueOrDefault("tvg-id", string.Empty),
            SourceType = "M3U"
        };
    }

    private static Dictionary<string, string> ParseMetadata(string metadataRaw)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match item in MetaRegex.Matches(metadataRaw))
        {
            result[item.Groups["key"].Value] = item.Groups["value"].Value;
        }

        return result;
    }
}
