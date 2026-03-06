using IPTVPlayer.App.Models;
using LibVLCSharp.Shared;

namespace IPTVPlayer.App.Player;

public class LibVlcPlayerService : IDisposable
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _mediaPlayer;

    public event EventHandler<PlaybackState>? PlaybackStateChanged;

    public LibVlcPlayerService()
    {
        Core.Initialize();
        _libVlc = new LibVLC("--network-caching=800", "--clock-jitter=0", "--clock-synchro=0", "--avcodec-hw=any");
        _mediaPlayer = new MediaPlayer(_libVlc) { EnableHardwareDecoding = true };

        _mediaPlayer.Buffering += (_, _) => PlaybackStateChanged?.Invoke(this, PlaybackState.Buffering);
        _mediaPlayer.Playing += (_, _) => PlaybackStateChanged?.Invoke(this, PlaybackState.Playing);
        _mediaPlayer.Paused += (_, _) => PlaybackStateChanged?.Invoke(this, PlaybackState.Paused);
        _mediaPlayer.Stopped += (_, _) => PlaybackStateChanged?.Invoke(this, PlaybackState.Stopped);
        _mediaPlayer.EncounteredError += (_, _) => PlaybackStateChanged?.Invoke(this, PlaybackState.Error);
    }

    public MediaPlayer MediaPlayer => _mediaPlayer;

    public void Play(string streamUrl)
    {
        if (string.IsNullOrWhiteSpace(streamUrl))
        {
            return;
        }

        using var media = new Media(_libVlc, new Uri(streamUrl));
        media.AddOption(":network-caching=1000");
        media.AddOption(":live-caching=1000");
        _mediaPlayer.Play(media);
    }

    public void Pause() => _mediaPlayer.Pause();

    public void Stop() => _mediaPlayer.Stop();

    public void SetVolume(int value) => _mediaPlayer.Volume = Math.Clamp(value, 0, 200);

    public void Dispose()
    {
        _mediaPlayer.Dispose();
        _libVlc.Dispose();
    }
}
