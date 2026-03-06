using IPTVPlayer.App.Infrastructure;
using IPTVPlayer.App.Models;

namespace IPTVPlayer.App.ViewModels;

public class ChannelViewModel : ObservableObject
{
    private readonly Channel _channel;

    public ChannelViewModel(Channel channel)
    {
        _channel = channel;
    }

    public string Id => _channel.Id;
    public string Name => _channel.Name;
    public string Group => _channel.Group;
    public string LogoUrl => _channel.LogoUrl;
    public string StreamUrl => _channel.StreamUrl;
    public string SourceType => _channel.SourceType;
    public string EpgChannelId => _channel.EpgChannelId;

    public bool IsFavorite
    {
        get => _channel.IsFavorite;
        set
        {
            if (_channel.IsFavorite != value)
            {
                _channel.IsFavorite = value;
                RaisePropertyChanged();
            }
        }
    }
}
