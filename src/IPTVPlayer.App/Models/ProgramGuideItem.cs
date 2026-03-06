namespace IPTVPlayer.App.Models;

public class ProgramGuideItem
{
    public string ChannelId { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
