using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using MediaPlayer = Windows.Media.Playback.MediaPlayer;

namespace Dimmer_MAUI.Platforms.Windows;

public partial class DimmerAudioService : IDimmerAudioService, INotifyPropertyChanged, IDisposable
{
    static DimmerAudioService? current;
    public static IDimmerAudioService Current => current ??= new DimmerAudioService();

    HomePageVM? ViewModel { get; set; }
    MediaPlayer? mediaPlayer;
    MediaPlay? CurrentMedia { get; set; }
    public DimmerAudioService()
    {
    }

    private bool isPlaying;
    public bool IsPlaying
    {
        get
        {
            return isPlaying;
        }

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


    public double Duration => mediaPlayer?.NaturalDuration.TotalSeconds ?? 0;
    public double CurrentPosition => mediaPlayer?.Position.TotalSeconds ?? 0;
    public double Volume
    {
        get
        {
            return mediaPlayer?.Volume ?? 0;
        }

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
        get
        {
            return mediaPlayer?.IsMuted ?? false;
        }

        set
        {
            if (mediaPlayer is null)
            {
                return;
            }
            mediaPlayer.IsMuted = value;
        }
    }
    public double Balance
    {
        get
        {
            return 0; // CSCore does not natively support balance
        }

        set { /* Implement if necessary using custom processing */ }
    }
    private readonly object resourceLock = new();
    private bool disposed = false;
    private SemaphoreSlim semaphoreSlim = new(1, 1);
    
    public event EventHandler<bool>? IsPlayingChanged;
    public event EventHandler? PlayEnded;
    public event EventHandler? PlayNext;
    public event EventHandler? PlayPrevious;
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<long>? IsSeekedFromNotificationBar;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Pause()
    {
        mediaPlayer?.Pause();
        IsPlaying = false;
        IsPlayingChanged?.Invoke(this, false);
        
    }

    public void Resume(double positionInSeconds)
    {
        if (mediaPlayer is null)
        {
            return;
        }   
        mediaPlayer.Position = TimeSpan.FromSeconds(positionInSeconds);
        mediaPlayer.Play();
        IsPlaying = true;
    }

    public void Play(bool s)
    {
        if (mediaPlayer != null)
        {
            mediaPlayer.Play();
            IsPlaying = true;
            IsPlayingChanged?.Invoke(this, true);
        }
    }


    public void SetCurrentTime(double positionInSec)
    {
        if (mediaPlayer == null)
        {
            return;
        }
        mediaPlayer.Position = TimeSpan.FromSeconds(positionInSec);
    }


    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
            return; // Prevent multiple disposals


        // Clean up unmanaged resources, if any
        disposed = true;
    }


    private MediaPlaybackItem? MediaPlaybackItem(MediaPlay media)
    {
        try
        {
            if (media == null || media.Stream == null)
                return null;

            // Copy the byte stream to an InMemoryRandomAccessStream
            var randomAccessStream = new InMemoryRandomAccessStream();
            using (var writer = new DataWriter(randomAccessStream.GetOutputStreamAt(0)))
            {
                var buffer = new byte[media.Stream.Length];
                _ = media.Stream.Read(buffer, 0, buffer.Length);
                writer.WriteBytes(buffer);
                writer.StoreAsync().GetResults();
            }

            // Create MediaSource from the InMemoryRandomAccessStream
            var mediaSource = MediaSource.CreateFromStream(randomAccessStream, string.Empty);
            var mediaItem = new MediaPlaybackItem(mediaSource);

            // Set properties for the media item
            var props = mediaItem.GetDisplayProperties();
            props.Type = MediaPlaybackType.Music;
            if (!string.IsNullOrEmpty(media.Name))
                props.MusicProperties.Title = media.Name;
            if (!string.IsNullOrEmpty(media.Author))
                props.MusicProperties.Artist = media.Author;

            // Set the thumbnail if available
            if (media.ImageBytes != null)
            {
                var thumbnailStream = new InMemoryRandomAccessStream();
                using (var writer = new DataWriter(thumbnailStream.GetOutputStreamAt(0)))
                {
                    writer.WriteBytes(media.ImageBytes);
                    writer.StoreAsync().GetResults();
                }
                props.Thumbnail = RandomAccessStreamReference.CreateFromStream(thumbnailStream);
            }
            mediaItem.AutoLoadedDisplayProperties = AutoLoadedDisplayPropertyKind.Music;

            mediaItem.ApplyDisplayProperties(props);
            return mediaItem;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Something happened here {ex.Message} inter {ex.InnerException?.Message}, stack {ex.StackTrace}, source {ex.Source}");
            return null;
        }
    }


    //private SongModelView? _nextMedia; // To store the next media to be played

    public void Initialize(SongModelView? media, byte[]? ImageBytes) // Changed to async void - be mindful of exceptions
    {
        CurrentMedia?.Stream?.Dispose();
        if (media is not null)
        {
            var memStream = new MemoryStream();
            // Directly use the file path to create a URI for local files
            if (media.FilePath is not  null)
            {
                using var fileStreamm = File.OpenRead(media.FilePath);                
                fileStreamm.CopyTo(memStream);
                memStream.Position = 0;
            }
            CurrentMedia = new MediaPlay()
            {
                SongId = media.LocalDeviceId!,
                Name = media.Title!,
                Author = media!.ArtistName!,
                Stream = memStream,
                ImageBytes = ImageBytes is not null ? ImageBytes : null,
                DurationInMs = (long)(media.DurationInSeconds * 1000),
            };

        }
        try
        {
            ViewModel ??= IPlatformApplication.Current?.Services.GetService<HomePageVM>()!;
            var curMedia = MediaPlaybackItem(CurrentMedia!);
            if (curMedia == null)
                return;
            if (mediaPlayer == null)
            {

                mediaPlayer = new MediaPlayer()
                {
                    Source = curMedia,
                    AudioCategory = MediaPlayerAudioCategory.Media,
                };



                mediaPlayer.CommandManager.PreviousReceived += CommandManager_PreviousReceived;
                mediaPlayer.CommandManager.PreviousBehavior.EnablingRule = MediaCommandEnablingRule.Always;
                mediaPlayer.CommandManager.ShuffleBehavior.EnablingRule = MediaCommandEnablingRule.Always;

                mediaPlayer.CommandManager.NextReceived += CommandManager_NextReceived;
                mediaPlayer.CommandManager.NextBehavior.EnablingRule = MediaCommandEnablingRule.Always;

                mediaPlayer.CommandManager.PlayReceived += CommandManager_PlayReceived;
                
                mediaPlayer.CommandManager.PauseReceived += CommandManager_PauseReceived;
                mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
                //mediaPlayer.Volume = 1;
                mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
            }
            else
            {
                Pause();
                mediaPlayer.Source = curMedia;
            }
        }
        catch (Exception)
        {
            Shell.Current.DisplayAlert("Oops! An Error Occured!", "This is a very very rare error but doesn't affect the app much, Carry On :D", "OK Thanks");
        }
    }

    private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        Debug.WriteLine(args.Error);
        Debug.WriteLine(args.ErrorMessage);
        Debug.WriteLine(args.ExtendedErrorCode);


    }

    private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
    {
        IsPlaying = false;
        PlayEnded?.Invoke(sender, EventArgs.Empty);
        //PlayNext?.Invoke(sender, EventArgs.Empty);
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

    public void ApplyEqualizerSettings(float[] bands)
    {
        throw new NotImplementedException();
    }

    public void ApplyEqualizerPreset(EqualizerPresetName presetName)
    {
        throw new NotImplementedException();
    }
}
    
