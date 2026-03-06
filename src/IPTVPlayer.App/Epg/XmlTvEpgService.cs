using System.Xml.Linq;
using IPTVPlayer.App.Models;

namespace IPTVPlayer.App.Epg;

public class XmlTvEpgService
{
    public async Task<IReadOnlyList<ProgramGuideItem>> LoadFromFileAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        var doc = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
        return Parse(doc);
    }

    public async Task<IReadOnlyList<ProgramGuideItem>> LoadFromUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient();
        await using var stream = await client.GetStreamAsync(url, cancellationToken);
        var doc = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
        return Parse(doc);
    }

    public IReadOnlyList<ProgramGuideItem> Parse(XDocument doc)
    {
        var programs = new List<ProgramGuideItem>();
        foreach (var programme in doc.Descendants("programme"))
        {
            var channel = programme.Attribute("channel")?.Value ?? string.Empty;
            var startRaw = programme.Attribute("start")?.Value ?? string.Empty;
            var stopRaw = programme.Attribute("stop")?.Value ?? string.Empty;

            var start = ParseXmlTvDate(startRaw);
            var stop = ParseXmlTvDate(stopRaw);

            programs.Add(new ProgramGuideItem
            {
                ChannelId = channel,
                Start = start,
                End = stop,
                Title = programme.Element("title")?.Value ?? "Untitled",
                Description = programme.Element("desc")?.Value ?? string.Empty
            });
        }

        return programs;
    }

    private static DateTime ParseXmlTvDate(string value)
    {
        if (DateTimeOffset.TryParseExact(value, "yyyyMMddHHmmss zzz", null, System.Globalization.DateTimeStyles.None, out var dto))
        {
            return dto.LocalDateTime;
        }

        if (DateTime.TryParse(value, out var dt))
        {
            return dt;
        }

        return DateTime.MinValue;
    }
}
