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
    HomePageVM ViewModel { get; set; }
    MediaPlayer mediaPlayer;
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
    public event EventHandler<long> IsSeekedFromNotificationBar;

    public void InitializeAsync(string audioURI)
    {
        ViewModel ??= IPlatformApplication.Current.Services.GetService<HomePageVM>();
    }

    public Task PauseAsync()
    {
        mediaPlayer?.Pause();
        IsPlaying = false;
        IsPlayingChanged.Invoke(this, false);
        return Task.CompletedTask;
    }

    public Task ResumeAsync(double positionInSeconds)
    {
        mediaPlayer.Position = TimeSpan.FromSeconds(positionInSeconds);
        mediaPlayer.Play();
        IsPlaying = true;
        return Task.CompletedTask;
    }
    public Task PlayAsync(bool IsFromPreviousOrNext = false)
    {
        double position = 0;

        if (CurrentMedia.SongId != ViewModel.TemporarilyPickedSong.Id)
        {
            position = 0;
            mediaPlayer.Position = TimeSpan.FromSeconds(position);
        }
        if (IsFromPreviousOrNext)
        {
            position = 0;
            mediaPlayer.Position = TimeSpan.FromSeconds(position);
        }

        if (mediaPlayer != null)
        {
            mediaPlayer.Play();
            IsPlaying = true;
            IsPlayingChanged.Invoke(this, true);
        }

        return Task.CompletedTask;
    }

    
    public Task<bool> SetCurrentTime(double positionInSec)
    {
        
        if (mediaPlayer == null)
        {
            return Task.FromResult(false);
        }
        mediaPlayer.Position = TimeSpan.FromSeconds(positionInSec);
        return Task.FromResult(true);
    }
    public Task DisposeAsync()
    {
        mediaPlayer?.Dispose();
        return Task.CompletedTask;
    }
    private MediaPlaybackItem MediaPlaybackItem(MediaPlay media)
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
                media.Stream.Read(buffer, 0, buffer.Length);
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

            mediaItem.ApplyDisplayProperties(props);
            return mediaItem;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Something happened here {ex.Message} inter {ex.InnerException.Message}, stack {ex.StackTrace}, source {ex.Source}");
            return null;
        }
    }

    private RandomAccessStreamReference ConvertToRandomAccessStreamReference(byte[] imageData)
    {
        try
        {
            // Create a new InMemoryRandomAccessStream
            var randomAccessStream = new InMemoryRandomAccessStream();

            // Use DataWriter to write the byte array to the stream
            using (var writer = new DataWriter(randomAccessStream.GetOutputStreamAt(0)))
            {
                writer.WriteBytes(imageData);
                writer.StoreAsync().GetResults();
                writer.DetachStream();  // Detach to ensure it remains open for RandomAccessStreamReference
            }

            // Return a RandomAccessStreamReference from the InMemoryRandomAccessStream
            return RandomAccessStreamReference.CreateFromStream(randomAccessStream);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Something happened here {ex.Message} inter {ex.InnerException.Message}, stack {ex.StackTrace}, source {ex.Source}");
            throw;
        }
    }

    public async Task InitializeAsync(SongsModelView? media, byte[]? ImageBytes)
    {
        CurrentMedia?.Stream?.Dispose();
        if (media is not null)
        {
            // Directly use the file path to create a URI for local files
            using var fileStreamm = File.OpenRead(media.FilePath);
            var memStream = new MemoryStream();
            await fileStreamm.CopyToAsync(memStream);
            memStream.Position = 0;
            CurrentMedia = new MediaPlay()
            {
                SongId = media.Id,
                Name = media.Title,
                Author = media!.ArtistName!,
                Stream = memStream,
                ImageBytes = ImageBytes,
                DurationInMs = (long)(media.DurationInSeconds * 1000),
            };

        }
        try
        {
            ViewModel ??= IPlatformApplication.Current.Services.GetService<HomePageVM>();
            var curMedia = MediaPlaybackItem(CurrentMedia);
            if (curMedia == null)
                return;
            if (mediaPlayer == null)
            {
                mediaPlayer = new MediaPlayer
                {
                    Source = curMedia,
                    AudioCategory = MediaPlayerAudioCategory.Media
                };

                mediaPlayer.CommandManager.PreviousReceived += CommandManager_PreviousReceived;
                mediaPlayer.CommandManager.PreviousBehavior.EnablingRule = MediaCommandEnablingRule.Always;
                mediaPlayer.CommandManager.ShuffleBehavior.EnablingRule = MediaCommandEnablingRule.Always;
                mediaPlayer.CommandManager.AutoRepeatModeBehavior.EnablingRule = MediaCommandEnablingRule.Always;
                mediaPlayer.CommandManager.PositionBehavior.EnablingRule = MediaCommandEnablingRule.Always;
                mediaPlayer.CommandManager.RateBehavior.EnablingRule = MediaCommandEnablingRule.Always;
                mediaPlayer.CommandManager.RewindBehavior.EnablingRule = MediaCommandEnablingRule.Always;

                mediaPlayer.CommandManager.NextReceived += CommandManager_NextReceived;
                mediaPlayer.CommandManager.NextBehavior.EnablingRule = MediaCommandEnablingRule.Always;

                mediaPlayer.CommandManager.PlayReceived += CommandManager_PlayReceived;

                mediaPlayer.CommandManager.PauseReceived += CommandManager_PauseReceived;
                mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;

            }
            else
            {
                await PauseAsync();
                mediaPlayer.Source = curMedia;
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Oops! An Error Occured!", "This is a very very rare error but doesn't affect the app much, Carry On :D", "OK Thanks");
        }
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

}