using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;

namespace Dimmer_MAUI.Platforms.Windows;

public partial class NativeAudioService : INativeAudioService, INotifyPropertyChanged
{
    static NativeAudioService current;
    public static INativeAudioService Current => current ??= new NativeAudioService();
    HomePageVM? ViewModel { get; set; }
    MediaPlayer? DimmMediaPlayer { get; set; }
    MediaPlay? CurrentMedia { get; set; }
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
    }

    public double Duration => DimmMediaPlayer?.NaturalDuration.TotalSeconds ?? 0;
    public double CurrentPosition => DimmMediaPlayer?.Position.TotalSeconds ?? 0;
    public double Volume
    {
        get => DimmMediaPlayer?.Volume ?? 0;

        set
        {
            if (DimmMediaPlayer is not null)
            {
                DimmMediaPlayer.Volume = Math.Clamp(value, 0, 1);
            }
        }
    }
    public bool Muted
    {
        get => DimmMediaPlayer?.IsMuted ?? false;
        set => DimmMediaPlayer.IsMuted = value;
    }
    public double Balance { get => DimmMediaPlayer.AudioBalance; set => DimmMediaPlayer.AudioBalance = Math.Clamp(value, -1, 1); }

    public event EventHandler<bool>? IsPlayingChanged;
    public event EventHandler? PlayEnded;
    public event EventHandler? PlayNext;
    public event EventHandler? PlayPrevious;
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<long>? IsSeekedFromNotificationBar;

    public void Pause()
    {
        DimmMediaPlayer?.Pause();
        IsPlaying = false;
        IsPlayingChanged?.Invoke(this, false);
        
    }

    public void Resume(double positionInSeconds)
    {
        DimmMediaPlayer.Position = TimeSpan.FromSeconds(positionInSeconds);
        DimmMediaPlayer.Play();
        IsPlaying = true;
        
        
    }
    public void Play(bool IsFromPreviousOrNext = false)
    {
        if (DimmMediaPlayer != null)
        {
            DimmMediaPlayer.Play();
            IsPlaying = true;
            IsPlayingChanged?.Invoke(this, true);
        }
        
        
    }

    
    public void SetCurrentTime(double positionInSec)
    {
        
        if (DimmMediaPlayer == null)
        {
            return;
        }
        DimmMediaPlayer.Position = TimeSpan.FromSeconds(positionInSec);
        return ;
    }
    public void DisposeAsync()
    {
        DimmMediaPlayer?.Dispose();
        
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

    public void Initialize(SongModelView media)
    {
        CurrentMedia?.Stream?.Dispose();
        
        if (media is not null)
        {
            // Directly use the file path to create a URI for local files
            using var fileStreamm = File.OpenRead(media.FilePath);
            var memStream = new MemoryStream();
            fileStreamm.CopyTo(memStream);
            memStream.Position = 0;
            CurrentMedia = new MediaPlay()
            {
                SongId = media.LocalDeviceId,
                Name = media.Title,
                Author = media!.ArtistName!,
                Stream = memStream,
                ImagePath = media.CoverImagePath,
                DurationInMs = (long)(media.DurationInSeconds * 1000),
            };
            
        }
        try
        {
            ViewModel ??= IPlatformApplication.Current?.Services.GetService<HomePageVM>()!;
            var curMedia = MediaPlaybackItem(CurrentMedia!);
            if (curMedia == null)
                return;
            if (DimmMediaPlayer == null)
            {

                DimmMediaPlayer = new MediaPlayer()
                {
                    Source = curMedia,
                    AudioCategory = MediaPlayerAudioCategory.Media,
                };

                

                DimmMediaPlayer.CommandManager.PreviousReceived += CommandManager_PreviousReceived;
                DimmMediaPlayer.CommandManager.PreviousBehavior.EnablingRule = MediaCommandEnablingRule.Always;
                DimmMediaPlayer.CommandManager.ShuffleBehavior.EnablingRule = MediaCommandEnablingRule.Always;
                
                DimmMediaPlayer.CommandManager.NextReceived += CommandManager_NextReceived;
                DimmMediaPlayer.CommandManager.NextBehavior.EnablingRule = MediaCommandEnablingRule.Always;

                DimmMediaPlayer.CommandManager.PlayReceived += CommandManager_PlayReceived;

                DimmMediaPlayer.CommandManager.PauseReceived += CommandManager_PauseReceived;
                DimmMediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
                DimmMediaPlayer.Volume = 1;
                DimmMediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
            }
            else
            {
                Pause();
                DimmMediaPlayer.Source = curMedia;
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

    public void SkipToNext()
    {
        return;
        //throw new NotImplementedException();
    }

    public void SkipToPrevious()
    {
        return;
        //throw new NotImplementedException();
    }
}