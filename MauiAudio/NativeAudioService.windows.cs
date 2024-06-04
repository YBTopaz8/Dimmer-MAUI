using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media;
using Windows.Storage.Streams;
namespace MauiAudio;

public class NativeAudioService : INativeAudioService
{
    static NativeAudioService current;
    public static INativeAudioService Current => current ??= new NativeAudioService();
    MediaPlayer mediaPlayer;

    public bool IsPlaying => mediaPlayer != null
        && mediaPlayer.CurrentState == MediaPlayerState.Playing;
    public double Duration => mediaPlayer?.NaturalDuration.TotalSeconds ?? 0;
    public double CurrentPosition => mediaPlayer?.Position.TotalSeconds ?? 0;

    public double Volume
    {
        get => mediaPlayer?.Volume ?? 0;


        set => mediaPlayer.Volume = Math.Clamp(value, 0, 1);
    }
    public bool Muted
    {
        get => mediaPlayer?.IsMuted ?? false;
        set => mediaPlayer.IsMuted = value;
    }
    public double Balance { get => mediaPlayer.AudioBalance; set => mediaPlayer.AudioBalance = Math.Clamp(value, -1, 1); }

    public event EventHandler<bool> IsPlayingChanged;
    public event EventHandler PlayEnded;
    public event EventHandler PlayNext;
    public event EventHandler PlayPrevious;

    public async Task InitializeAsync(string audioURI)
    {
        await InitializeAsync(new MediaPlay() { URL = audioURI });
    }

    public Task PauseAsync()
    {
        mediaPlayer?.Pause();
        return Task.CompletedTask;
    }

    public Task PlayAsync(double position = 0)
    {
        if (mediaPlayer != null)
        {
            mediaPlayer.Position = TimeSpan.FromSeconds(position);
            mediaPlayer.Play();
        }

        return Task.CompletedTask;
    }

    public Task SetCurrentTime(double value)
    {
        if (mediaPlayer != null)
        {
            mediaPlayer.Position = TimeSpan.FromSeconds(value);
        }
        return Task.CompletedTask;
    }
    public Task DisposeAsync()
    {
        mediaPlayer?.Dispose();
        return Task.CompletedTask;
    }
    private MediaPlaybackItem mediaPlaybackItem(MediaPlay media)
    {
        var mediaItem = new MediaPlaybackItem(media.Stream == null ? MediaSource.CreateFromUri(new Uri(media.URL)) : MediaSource.CreateFromStream(media.Stream?.AsRandomAccessStream(),string.Empty));
        var props = mediaItem.GetDisplayProperties();
        
        props.Type = MediaPlaybackType.Music;
        if (media.Name != null) props.MusicProperties.Title = media.Name;
        if (media.Author != null) props.MusicProperties.Artist = media.Author;
        if (media.Image != null)
            props.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(media.Image));
        mediaItem.ApplyDisplayProperties(props);
        return mediaItem;
    }
    public async Task InitializeAsync(MediaPlay media)
    {
        if (mediaPlayer == null)
        {
            mediaPlayer = new MediaPlayer
            {
                Source = mediaPlaybackItem(media),
                AudioCategory = MediaPlayerAudioCategory.Media
            };
            mediaPlayer.CommandManager.PreviousReceived += CommandManager_PreviousReceived;
            mediaPlayer.CommandManager.NextReceived += CommandManager_NextReceived;
            mediaPlayer.CommandManager.PauseReceived += CommandManager_PauseReceived;
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
        }
        else
        {
            await PauseAsync();
            mediaPlayer.Source = mediaPlaybackItem(media);
        }
    }
    private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
    {
        PlayEnded?.Invoke(sender, EventArgs.Empty);
        PlayNext?.Invoke(sender, EventArgs.Empty);
    }
    private void CommandManager_NextReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerNextReceivedEventArgs args)
    {
        PlayNext?.Invoke(sender, EventArgs.Empty);
    }
    private void CommandManager_PreviousReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPreviousReceivedEventArgs args)
    {
        PlayPrevious?.Invoke(sender, EventArgs.Empty);
    }
    private void CommandManager_PauseReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPauseReceivedEventArgs args)
    {
        IsPlayingChanged?.Invoke(sender, IsPlaying);
    }
}
