using System.ComponentModel;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;

namespace Dimmer_MAUI.Platforms.Windows;

public class NativeAudioService : INativeAudioService, INotifyPropertyChanged
{
    static NativeAudioService current;
    public static INativeAudioService Current => current ??= new NativeAudioService();
    MediaPlayer mediaPlayer;

    private bool isPlaying;
    public bool IsPlaying
    {
        get => isPlaying;
        set
        {
            if (isPlaying != value)
            {
                isPlaying = value;
                IsPlayingChanged?.Invoke(this, value);
                OnPropertyChanged(nameof(IsPlaying));
            }
        }
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    //public bool IsPlaying
    //{
    //    get
    //    {
    //        return mediaPlayer != null
    //    && mediaPlayer.CurrentState == MediaPlayerState.Playing;
    //    }
    //}

    public double Duration => mediaPlayer?.NaturalDuration.TotalSeconds ?? 0;
    public double CurrentPosition => mediaPlayer?.Position.TotalSeconds ?? 0;

    public double Volume
    {
        get => mediaPlayer?.Volume ?? 0;

        set
        {
            if (mediaPlayer is not null)
            {
                mediaPlayer.Volume = Math.Clamp(value, 0, 1);
            }            
        }
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
    public event PropertyChangedEventHandler PropertyChanged;

    public void InitializeAsync(string audioURI)
    {
        Task.Run(() => InitializeAsync(new MediaPlay() { URL = audioURI }));
    }

    public Task PauseAsync()
    {
        mediaPlayer?.Pause();
        IsPlaying = false;
        return Task.CompletedTask;
    }

    public Task PlayAsync(double position = 0)
    {
        if (mediaPlayer != null)
        {
            mediaPlayer.Position = TimeSpan.FromSeconds(position);
            mediaPlayer.Play();
            IsPlaying = true;
        }

        return Task.CompletedTask;
    }

    public Task<bool> SetCurrentTime(double value)
    {
        if (mediaPlayer == null)
        {
            return Task.FromResult(false);
        }
        mediaPlayer.Position = TimeSpan.FromSeconds(value);
        return Task.FromResult(true);
    }
    public Task DisposeAsync()
    {
        mediaPlayer?.Dispose();
        return Task.CompletedTask;
    }
    private MediaPlaybackItem mediaPlaybackItem(MediaPlay media)
    {
        var mediaItem = new MediaPlaybackItem(media.Stream == null ? MediaSource.CreateFromUri(new Uri(media.URL)) : MediaSource.CreateFromStream(media.Stream?.AsRandomAccessStream(), string.Empty));
        var props = mediaItem.GetDisplayProperties();

        props.Type = MediaPlaybackType.Music;

        if (media.Name != null)
            props.MusicProperties.Title = media.Name;
        if (media.Author != null)
            props.MusicProperties.Artist = media.Author;
        if (media.ImageBytes != null)
        {
            props.Thumbnail = ConvertToRandomAccessStreamReference(media.ImageBytes); // ConvertToRandomAccessStreamReference(media.ImageBytes);
            //props.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(media.Image));
        }

        mediaItem.ApplyDisplayProperties(props);
        return mediaItem;
    }

    private RandomAccessStreamReference ConvertToRandomAccessStreamReference(byte[] imageData)
    {
        using var stream = new MemoryStream(imageData);
        var randomAccessStream = new InMemoryRandomAccessStream();
        var writer = new DataWriter(randomAccessStream.GetOutputStreamAt(0));
        writer.WriteBytes(imageData);
        writer.StoreAsync().GetResults();
        return RandomAccessStreamReference.CreateFromStream(randomAccessStream);
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
            mediaPlayer.CommandManager.PreviousBehavior.EnablingRule = MediaCommandEnablingRule.Always;

            mediaPlayer.CommandManager.NextReceived += CommandManager_NextReceived;
            mediaPlayer.CommandManager.NextBehavior.EnablingRule = MediaCommandEnablingRule.Always;

            mediaPlayer.CommandManager.PlayReceived += CommandManager_PlayReceived;
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
        IsPlayingChanged?.Invoke(sender, false);
    }
    private void CommandManager_PlayReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPlayReceivedEventArgs args)
    {
        IsPlayingChanged?.Invoke(sender, true);
    }

   
}

