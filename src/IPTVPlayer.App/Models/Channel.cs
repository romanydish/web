namespace IPTVPlayer.App.Models;

public class Channel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = "Ungrouped";
    public string LogoUrl { get; set; } = string.Empty;
    public string StreamUrl { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }
    public string SourceType { get; set; } = "M3U";
    public string EpgChannelId { get; set; } = string.Empty;
}
