using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Windows.Input;
using IPTVPlayer.App.Api;
using IPTVPlayer.App.Commands;
using IPTVPlayer.App.Epg;
using IPTVPlayer.App.Infrastructure;
using IPTVPlayer.App.Models;
using IPTVPlayer.App.Parsers;
using IPTVPlayer.App.Player;
using IPTVPlayer.App.Proxy;
using IPTVPlayer.App.Security;
using LibVLCSharp.Shared;

namespace IPTVPlayer.App.ViewModels;

public class MainViewModel : ObservableObject, IDisposable
{
    private readonly M3UPlaylistParser _playlistParser;
    private readonly XtreamCodesClient _xtreamClient;
    private readonly XmlTvEpgService _epgService;
    private readonly LibVlcPlayerService _playerService;
    private readonly LocalProxyServer _proxy;
    private readonly TokenService _tokenService;
    private readonly SecureStorageService _secureStorage;

    private string _status = "Ready";
    private string _searchText = string.Empty;
    private string _selectedCategory = "All";
    private ChannelViewModel? _selectedChannel;
    private int _volume = 90;
    private bool _useProxy;
    private PlaybackState _playbackState;

    public MainViewModel(
        M3UPlaylistParser playlistParser,
        XtreamCodesClient xtreamClient,
        XmlTvEpgService epgService,
        LibVlcPlayerService playerService,
        LocalProxyServer proxy,
        TokenService tokenService,
        SecureStorageService secureStorage)
    {
        _playlistParser = playlistParser;
        _xtreamClient = xtreamClient;
        _epgService = epgService;
        _playerService = playerService;
        _proxy = proxy;
        _tokenService = tokenService;
        _secureStorage = secureStorage;

        _proxy.Start();
        _playerService.PlaybackStateChanged += (_, state) =>
        {
            PlaybackState = state;
            Status = state.ToString();
        };

        ChannelsView = CollectionViewSource.GetDefaultView(Channels);
        ChannelsView.Filter = FilterChannel;

        ImportM3uUrlCommand = new AsyncRelayCommand(_ => ImportM3uFromUrlAsync(M3uUrl));
        ImportM3uFileCommand = new AsyncRelayCommand(_ => ImportM3uFromFileAsync(M3uFilePath));
        XtreamLoginCommand = new AsyncRelayCommand(_ => XtreamLoginAsync());
        PlayCommand = new RelayCommand(_ => PlaySelected(), _ => SelectedChannel != null);
        PauseCommand = new RelayCommand(_ => _playerService.Pause());
        StopCommand = new RelayCommand(_ => _playerService.Stop());
        ToggleFavoriteCommand = new RelayCommand(_ => ToggleFavorite());
        LoadEpgCommand = new AsyncRelayCommand(_ => LoadEpgAsync(EpgPath));
        NextChannelCommand = new RelayCommand(_ => Zap(1));
        PreviousChannelCommand = new RelayCommand(_ => Zap(-1));

        Categories.Add("All");
        Categories.Add("Favorites");
    }

    public static MainViewModel CreateDefault() => new(
        new M3UPlaylistParser(),
        new XtreamCodesClient(),
        new XmlTvEpgService(),
        new LibVlcPlayerService(),
        new LocalProxyServer(),
        new TokenService(),
        new SecureStorageService());

    public ObservableCollection<ChannelViewModel> Channels { get; } = [];
    public ObservableCollection<string> Categories { get; } = [];
    public ObservableCollection<ProgramGuideItem> CurrentEpgItems { get; } = [];

    public ICollectionView ChannelsView { get; }
    public MediaPlayer MediaPlayer => _playerService.MediaPlayer;

    public string M3uUrl { get; set; } = string.Empty;
    public string M3uFilePath { get; set; } = string.Empty;
    public string EpgPath { get; set; } = string.Empty;

    public string XtreamServer { get; set; } = string.Empty;
    public string XtreamUsername { get; set; } = string.Empty;
    public string XtreamPassword { get; set; } = string.Empty;

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ChannelsView.Refresh();
            }
        }
    }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                ChannelsView.Refresh();
            }
        }
    }

    public ChannelViewModel? SelectedChannel
    {
        get => _selectedChannel;
        set
        {
            if (SetProperty(ref _selectedChannel, value))
            {
                ((RelayCommand)PlayCommand).RaiseCanExecuteChanged();
                LoadCurrentAndNextProgram();
            }
        }
    }

    public int Volume
    {
        get => _volume;
        set
        {
            if (SetProperty(ref _volume, value))
            {
                _playerService.SetVolume(value);
            }
        }
    }

    public bool UseProxy
    {
        get => _useProxy;
        set => SetProperty(ref _useProxy, value);
    }

    public PlaybackState PlaybackState
    {
        get => _playbackState;
        private set => SetProperty(ref _playbackState, value);
    }

    public ICommand ImportM3uUrlCommand { get; }
    public ICommand ImportM3uFileCommand { get; }
    public ICommand XtreamLoginCommand { get; }
    public ICommand PlayCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }
    public ICommand LoadEpgCommand { get; }
    public ICommand NextChannelCommand { get; }
    public ICommand PreviousChannelCommand { get; }

    private async Task ImportM3uFromUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        Status = "Loading M3U from URL...";
        var channels = await _playlistParser.ParseFromUrlAsync(url);
        MergeChannels(channels);
        Status = $"Loaded {channels.Count} channels from URL";
    }

    private async Task ImportM3uFromFileAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            Status = "M3U file not found.";
            return;
        }

        Status = "Loading M3U file...";
        var channels = await _playlistParser.ParseFromFileAsync(path);
        MergeChannels(channels);
        Status = $"Loaded {channels.Count} channels from file";
    }

    private async Task XtreamLoginAsync()
    {
        var credentials = new XtreamCredentials
        {
            ServerUrl = XtreamServer,
            Username = XtreamUsername,
            Password = XtreamPassword
        };

        await _secureStorage.SaveSecretAsync("xtream_server", XtreamServer);
        await _secureStorage.SaveSecretAsync("xtream_username", XtreamUsername);
        await _secureStorage.SaveSecretAsync("xtream_password", XtreamPassword);

        Status = "Loading Xtream content...";
        var liveTask = _xtreamClient.LoadLiveChannelsAsync(credentials);
        var moviesTask = _xtreamClient.LoadMoviesAsync(credentials);
        var seriesTask = _xtreamClient.LoadSeriesAsync(credentials);

        await Task.WhenAll(liveTask, moviesTask, seriesTask);

        MergeChannels(liveTask.Result.Concat(moviesTask.Result).Concat(seriesTask.Result).ToList());
        Status = "Xtream content loaded";
    }

    private async Task LoadEpgAsync(string source)
    {
        IReadOnlyList<ProgramGuideItem> epg;
        if (Uri.IsWellFormedUriString(source, UriKind.Absolute))
        {
            epg = await _epgService.LoadFromUrlAsync(source);
        }
        else if (File.Exists(source))
        {
            epg = await _epgService.LoadFromFileAsync(source);
        }
        else
        {
            Status = "EPG source invalid";
            return;
        }

        _allProgramItems = epg.ToList();
        LoadCurrentAndNextProgram();
        Status = $"EPG loaded: {_allProgramItems.Count} entries";
    }

    private List<ProgramGuideItem> _allProgramItems = [];

    private void LoadCurrentAndNextProgram()
    {
        CurrentEpgItems.Clear();
        if (SelectedChannel == null)
        {
            return;
        }

        var now = DateTime.Now;
        var list = _allProgramItems
            .Where(x => x.ChannelId.Equals(SelectedChannel.EpgChannelId, StringComparison.OrdinalIgnoreCase)
                     || x.ChannelId.Equals(SelectedChannel.Name, StringComparison.OrdinalIgnoreCase))
            .Where(x => x.End >= now)
            .OrderBy(x => x.Start)
            .Take(2)
            .ToList();

        foreach (var item in list)
        {
            CurrentEpgItems.Add(item);
        }
    }

    private void MergeChannels(IReadOnlyList<Channel> channels)
    {
        Channels.Clear();
        Categories.Clear();
        Categories.Add("All");
        Categories.Add("Favorites");

        foreach (var channel in channels.Select(c => new ChannelViewModel(c)))
        {
            Channels.Add(channel);
            if (!Categories.Contains(channel.Group))
            {
                Categories.Add(channel.Group);
            }
        }

        ChannelsView.Refresh();
    }

    private bool FilterChannel(object obj)
    {
        if (obj is not ChannelViewModel channel)
        {
            return false;
        }

        if (SelectedCategory == "Favorites" && !channel.IsFavorite)
        {
            return false;
        }

        if (SelectedCategory != "All" && SelectedCategory != "Favorites" && !string.Equals(channel.Group, SelectedCategory, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(SearchText) && !channel.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private void PlaySelected()
    {
        if (SelectedChannel == null)
        {
            return;
        }

        var streamUrl = SelectedChannel.StreamUrl;
        if (UseProxy)
        {
            var token = _tokenService.GenerateToken(SelectedChannel.Id, TimeSpan.FromHours(1));
            streamUrl = _proxy.BuildProxyUrl(SelectedChannel.StreamUrl, token);
        }

        _playerService.Play(streamUrl);
        Status = $"Playing {SelectedChannel.Name}";
    }

    private void ToggleFavorite()
    {
        if (SelectedChannel == null)
        {
            return;
        }

        SelectedChannel.IsFavorite = !SelectedChannel.IsFavorite;
        ChannelsView.Refresh();
    }


    public void Dispose()
    {
        _playerService.Dispose();
        _proxy.Dispose();
    }
    private void Zap(int delta)
    {
        var visible = ChannelsView.Cast<ChannelViewModel>().ToList();
        if (visible.Count == 0)
        {
            return;
        }

        var index = SelectedChannel == null ? 0 : visible.IndexOf(SelectedChannel);
        if (index < 0)
        {
            index = 0;
        }

        var next = (index + delta + visible.Count) % visible.Count;
        SelectedChannel = visible[next];
        PlaySelected();
    }
}
