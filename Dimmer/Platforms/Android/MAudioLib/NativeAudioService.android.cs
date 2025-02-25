using Android.Media;
using Dimmer_MAUI.Platforms.Android.CurrentActivity;
using Java.Lang;
using Exception = Java.Lang.Exception;

namespace Dimmer_MAUI.Platforms.Android.MAudioLib;

public class DimmerAudioService : IDimmerAudioService, INotifyPropertyChanged
{
    static DimmerAudioService current;
    public static IDimmerAudioService Current => current ??= new DimmerAudioService();
    HomePageVM ViewModel { get; set; }
    IAudioActivity instance;
    double volume = 1;
    double balance = 0;
    bool muted = false;
    MediaPlay CurrentMedia { get; set; }
    private MediaPlayer? mediaPlayer
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
                Debug.WriteLine("media player is null in duration");
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
            SetVolume(volume = value);
        }
    }
    //public double Balance { get => balance; set { balance = value; SetVolume(Volume, balance = value); } }
    public bool Muted { get => muted; set => SetMuted(value); }

    public event EventHandler<bool>? IsPlayingChanged;
    public event EventHandler? PlayEnded;
    public event EventHandler? NotificationTapped;
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<long>? IsSeekedFromNotificationBar;
    public event EventHandler? PlayPrevious;
    public event EventHandler? PlayNext;

    public void Pause()
    {        
        instance.Binder.GetMediaPlayerService().Pause();        
        IsPlaying = false;
        ViewModel.SetPlayerState(MediaPlayerState.Playing);
        ViewModel.SetPlayerState(MediaPlayerState.ShowPlayBtn);
        
    }
    public void Resume(double positionInSeconds)
    {
        var posInMs = positionInSeconds * 1000;
        
        instance.Binder.GetMediaPlayerService().Play();
        instance.Binder.GetMediaPlayerService().Seek((int)posInMs);    
        
    }

    public void Play(bool IsFromPreviousOrNext = false)
    {
            instance.Binder.GetMediaPlayerService().Play();
     
        IsPlaying = true;
        ViewModel.SetPlayerState(MediaPlayerState.Playing);
        ViewModel.SetPlayerState(MediaPlayerState.ShowPauseBtn);
    }

    void SetMuted(bool value)
    {
        muted = value;
        if (value)
            mediaPlayer.SetVolume(0, 0);
        else
            SetVolume(volume);
        
    }
    void SetVolume(double volume)
    {
        mediaPlayer?.SetVolume((float)volume, (float)volume);
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


    }


    public void SetCurrentTime(double positionInSeconds)
    {
        if (mediaPlayer is null)
        {
            Debug.WriteLine("no media");
            return;
        }

        // Get duration (replace with your actual method)
        double duration = mediaPlayer.Duration; // You must implement this method or property

        // Validate input, ensuring it's within a valid range.
        double clampedPosition = Java.Lang.Math.Clamp(positionInSeconds, 0, duration);
        var posInMs = (int)(clampedPosition * 1000);

        try
        {
            instance.Binder.GetMediaPlayerService().Seek(posInMs);
            IsPlaying = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error seeking: {ex.Message}");
            IsPlaying = false; // Ensure IsPlaying is false if the seek fails
                               // Consider other error handling, like showing an error message
        }
    }


    public void Dispose()
    {
        instance.Binder?.GetMediaPlayerService().Stop();
        
    }

    public void Initialize(SongModelView? media, byte[]? ImageBytes=null)
    {
        ViewModel ??= IPlatformApplication.Current!.Services.GetService<HomePageVM>()!;
        CurrentMedia = new();
        if (CurrentMedia is not null)
        {
            if (media is not null)
            {
                CurrentMedia = new MediaPlay()
                {
                    SongId = media.LocalDeviceId!,
                    Name = media.Title!,
                    Author = media!.ArtistName!,
                    URL = media.FilePath!,
                    ImagePath = media.CoverImagePath,
                    DurationInMs = (long)(media.DurationInSeconds * 1000),
                };
            }

            if (instance == null)
            {
                var activity = CrossCurrentActivity.Current;
                instance = (activity.Activity as IAudioActivity)!;
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
            mediaPlayerService.IsSeekedFromNotificationBar -= MediaPlayerService_IsSeekedFromNotificationBar;

            // Subscribe to events
            mediaPlayerService.IsPlayingChanged += IsPlayingChanged;
            mediaPlayerService.TaskPlayEnded += PlayEnded;
            mediaPlayerService.TaskPlayNext += PlayNext;
            mediaPlayerService.TaskPlayPrevious += PlayPrevious;
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
        if (isPlaying)
        {
            this.Play();
            this.SetCurrentTime(CurrentPosition);
        }
        else
        {
            this.Pause();
        }
     
        IsPlayingChanged?.Invoke(this, isPlaying);
    }

    public void ApplyEqualizerSettings(float[] bands)
    {
        throw new NotImplementedException();
    }

    public void ApplyEqualizerPreset(EqualizerPresetName presetName)
    {
        throw new NotImplementedException();
    }

    //public AudioDeviceModel? GetCurrentOutputDeviceAsync()
    //{
    //    throw new NotImplementedException();
    //}

    public Task<bool> SetOutputDeviceAsync(string deviceId)
    {
        throw new NotImplementedException();
    }
}
