using Android.Media;
using Dimmer_MAUI.Platforms.Android.CurrentActivity;
using Java.Lang;

namespace Dimmer_MAUI.Platforms.Android.MAudioLib;

public class NativeAudioService : INativeAudioService, INotifyPropertyChanged
{
    static NativeAudioService current;
    public static INativeAudioService Current => current ??= new NativeAudioService();
    HomePageVM ViewModel { get; set; }
    IAudioActivity instance;
    double volume = 1;
    double balance = 0;
    bool muted = false;
    MediaPlay CurrentMedia { get; set; }
    private MediaPlayer mediaPlayer
    {
        get
        {
            if (instance != null)
            {
                var service = instance.Binder.GetMediaPlayerService();
                if (service != null)
                {
                    return service.mediaPlayer;
                }
            }
            return null;
        }
    }

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

    public double Duration
    {
        get
        {
            if (mediaPlayer == null)
                Console.WriteLine("media player is null in duration");
            return mediaPlayer?.Duration / 1000 ?? 0;
        }
    }

    double _currentPosInMs;
    public double CurrentPosition
    {
        get
        {
            try
            {
                // Return the current position in seconds if available
                return mediaPlayer?.CurrentPosition / 1000.0 ?? _currentPosInMs / 1000.0;
            }
            catch (IllegalStateException ex)
            {
                // If mediaPlayer is in an invalid state, return 0 or a fallback value
                return 0.0;
            }
        }
    }

    public double Volume
    {
        get => volume;
        set
        {
            volume = value;
            SetVolume(volume = value, Balance);
        }
    }
    public double Balance { get => balance; set { balance = value; SetVolume(Volume, balance = value); } }
    public bool Muted { get => muted; set => SetMuted(value); }

    public event EventHandler<bool> IsPlayingChanged;
    public event EventHandler PlayEnded;
    public event EventHandler PlayNext;
    public event EventHandler PlayPrevious;
    public event EventHandler NotificationTapped;
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<long> IsSeekedFromNotificationBar;

    public Task PauseAsync()
    {        
        instance.Binder.GetMediaPlayerService().Pause();        
        IsPlaying = false;
        ViewModel.SetPlayerState(MediaPlayerState.Playing);
        ViewModel.SetPlayerState(MediaPlayerState.ShowPlayBtn);
        return Task.CompletedTask;
    }
    public async Task ResumeAsync(double positionInSeconds)
    {
        var posInMs = positionInSeconds * 1000;
        
        await instance.Binder.GetMediaPlayerService().Play();
        await instance.Binder.GetMediaPlayerService().Seek((int)posInMs);    
        
    }

    public async Task PlayAsync(bool IsFromPreviousOrNext = false)
    {
        int posInMs = 0;
        if(ViewModel.TemporarilyPickedSong is not null)
        {
            if (CurrentMedia.SongId == ViewModel.TemporarilyPickedSong.Id)
            {
                posInMs = (int)ViewModel.CurrentPositionInSeconds * 1000;
            }
            if (IsFromPreviousOrNext)
            {
                posInMs = 0;
            }
            await instance.Binder.GetMediaPlayerService().Play(posInMs);
        }
        else
        {
            await instance.Binder.GetMediaPlayerService().Play();
        }
        IsPlaying = true;
        ViewModel.SetPlayerState(MediaPlayerState.Playing);
        ViewModel.SetPlayerState(MediaPlayerState.ShowPauseBtn);
    }

    Task SetMuted(bool value)
    {
        muted = value;
        if (value)
            mediaPlayer.SetVolume(0, 0);
        else
            SetVolume(volume, balance);
        return Task.CompletedTask;
    }
    Task SetVolume(double volume, double balance)
    {

        //mediaPlayer?.SetVolume((float)1, (float)1);

        //Stream streamType = Stream.Music;
        //if (instance == null)
        //{
        //    var activity = CrossCurrentActivity.Current;
        //    instance = activity.Activity as IAudioActivity;
        //}
        //var aManager = instance.Binder.GetMediaPlayerService().ExposeAudioManager();

        //volume = Math.Max(0, Math.Min(15, volume));
        //int scaledVolume = (int)Math.Round(volume);
        //aManager.SetStreamVolume(streamType, scaledVolume, VolumeNotificationFlags.RemoveSoundAndVibrate);

        return Task.CompletedTask;
    }

    public async Task<bool> SetCurrentTime(double positionInSeconds)
    {
        var posInMs = (int)(positionInSeconds * 1000);
        if (mediaPlayer is null)
        {
            Debug.WriteLine("no media");
            return false;
        }
        await instance.Binder.GetMediaPlayerService().Seek(posInMs);
        return true;

    }

    public Task DisposeAsync()
    {
        instance.Binder?.GetMediaPlayerService().Stop();
        return Task.CompletedTask;
    }

    public async Task InitializeAsync(SongsModelView media, byte[]? ImageBytes=null)
    {
        ViewModel ??= IPlatformApplication.Current.Services.GetService<HomePageVM>();
        CurrentMedia = new();
        if (CurrentMedia is not null)
        {
            if (media is not null)
            {
                CurrentMedia = new MediaPlay()
                {
                    SongId = media.Id,
                    Name = media.Title,
                    Author = media!.ArtistName!,
                    URL = media.FilePath,
                    ImagePath = media.CoverImagePath,
                    DurationInMs = (long)(media.DurationInSeconds * 1000),
                };
            }

            if (instance == null)
            {
                var activity = CrossCurrentActivity.Current;
                instance = activity.Activity as IAudioActivity;
            }
            else
            {
                instance.Binder.GetMediaPlayerService().isCurrentEpisode = false;
                instance.Binder.GetMediaPlayerService().UpdatePlaybackStateStopped();
            }

            var mediaPlayerService = instance.Binder.GetMediaPlayerService();
            mediaPlayerService.mediaPlay = CurrentMedia;

            if (mediaPlayerService.mediaPlayer == null)
            {
                mediaPlayerService.DoInitialInitialization();
            }

            // Unsubscribe before subscribing to prevent duplicate handlers
            mediaPlayerService.IsPlayingChanged -= IsPlayingChanged;
            mediaPlayerService.TaskPlayEnded -= PlayEnded;
            mediaPlayerService.TaskPlayNext -= PlayNext;
            mediaPlayerService.TaskPlayPrevious -= PlayPrevious;

            // Subscribe to events
            mediaPlayerService.IsPlayingChanged += IsPlayingChanged;
            mediaPlayerService.TaskPlayEnded += PlayEnded;
            mediaPlayerService.TaskPlayNext += PlayNext;
            mediaPlayerService.TaskPlayPrevious += PlayPrevious;

            mediaPlayerService.PlayingChanged -= OnPlayingChanged; // Unsubscribe if previously subscribed
            mediaPlayerService.PlayingChanged += OnPlayingChanged;
            mediaPlayerService.IsSeekedFromNotificationBar += MediaPlayerService_IsSeekedFromNotificationBar;
        }
    }

    private void MediaPlayerService_IsSeekedFromNotificationBar(object? sender, long e)
    {
        this.IsSeekedFromNotificationBar?.Invoke(this, e);
    }

    private void OnPlayingChanged(object sender, bool isPlaying)
    {
        Task.Run(async () =>
        {
            if (isPlaying)
            {
                await this.PlayAsync();
                await this.SetCurrentTime(CurrentPosition);
            }
            else
            {
                await this.PauseAsync();
            }
        });
        IsPlayingChanged?.Invoke(this, isPlaying);
    }

}
