using Android.Media;
using AndroidX.Media3.ExoPlayer;
using Dimmer_MAUI.Platforms.Android.CurrentActivity;
using Java.Lang;

namespace Dimmer_MAUI.Platforms.Android.MAudioLib;

public class NativeAudioService : INativeAudioService, INotifyPropertyChanged
{
    static NativeAudioService current;
    public static INativeAudioService Current
    {
        get
        {
            return current is null ? new NativeAudioService() : current;
        }
    }
    
    HomePageVM ViewModel { get; set; }
    
    double volume = 1;
    double balance = 0;
    bool muted = false;
    MediaPlay CurrentMedia { get; set; }
    private IExoPlayer MediaPlayer
    {
        get
        {
            if (MService is null)
            {
                MService = new MediaPlayerService();
                return MService.mediaPlayer;
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
                Console.WriteLine(MService.mediaPlayer.CurrentMediaItem.MediaMetadata.Station);
                IsPlayingChanged?.Invoke(MService.mediaPlayer.CurrentMediaItem.MediaMetadata.Station, value);
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
            if (MediaPlayer == null)
                Debug.WriteLine("media player is null in duration");
            return MediaPlayer?.Duration / 1000 ?? 0;
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
                return MediaPlayer?.CurrentPosition / 1000.0 ?? _currentPosInMs / 1000.0;
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
    //public bool Muted { get => muted; set => SetMuted(value); }

    public event EventHandler<bool>? IsPlayingChanged;
    public event EventHandler? PlayEnded;
    public event EventHandler? PlayNext;
    public event EventHandler? PlayPrevious;
    public event EventHandler? NotificationTapped;
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<long>? IsSeekedFromNotificationBar;

    public void Pause()
    {        
        MService.mediaPlayer.Pause();   
        IsPlaying = false;
        IsPlayingChanged?.Invoke(this, false);
        
        
    }
    public void Resume(double positionInSeconds)
    {
        
        long positionInMilliseconds = (long)Java.Lang.Math.Round(positionInSeconds * 1000);

        MService.mediaPlayer.SeekTo(positionInMilliseconds);
        MService.mediaPlayer.Play();

        IsPlaying = true;
        IsPlayingChanged?.Invoke(this, true);



    }

    public void Play(bool IsFromPreviousOrNext = false)
    {
        MService.mediaPlayer.Play();
        IsPlaying = true;

        IsPlayingChanged?.Invoke(this, true);

    }

    public void SkipToNext()
    {
        MService.SkipToNextMediaItem();
    }
    public void SkipToPrevious()
    {
        MService.SkipToPreviousMediaItem();
    }
    //void SetMuted(bool value)
    //{
    //    muted = value;
    //    if (value)
    //        MediaPlayer.SetVolume(0, 0);
    //    else
    //        SetVolume(volume, balance);
    //    return Task.CompletedTask;
    //}
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

    public void SetCurrentTime(double positionInSeconds)
    {
        var posInMs = (int)(positionInSeconds * 1000);
        
        MService.Seek(posInMs);
        IsPlaying = true;
        return ;

    }

    public Task DisposeAsync()
    {
        
        return Task.CompletedTask;
    }
    MediaPlayerService? MService { get; set; }
    public NativeAudioService()
    {

        if (MService is not null)
        {
            return;
        }
        MService = new MediaPlayerService();
        



        // Unsubscribe before subscribing to prevent duplicate handlers
        MService.IsPlayingChanged -= IsPlayingChanged;
        MService.TaskPlayEnded -= PlayEnded;
        MService.TaskPlayNext -= PlayNext;
        MService.TaskPlayPrevious -= PlayPrevious;

        // Subscribe to events
        MService.IsPlayingChanged += IsPlayingChanged;
        MService.TaskPlayEnded += PlayEnded;
        MService.TaskPlayNext += PlayNext;
        MService.TaskPlayPrevious += PlayPrevious;

        MService.PlayingChanged -= OnPlayingChanged; // Unsubscribe if previously subscribed
        MService.PlayingChanged += OnPlayingChanged;
        MService.IsSeekedFromNotificationBar += MediaPlayerService_IsSeekedFromNotificationBar;
    }
    
    public void Initialize(SongModelView Song)
    {
        //ViewModel = Platform.AppContext.GetSystemService(
        
        MService.PrepareAndPlayMediaPlayer(Song);
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
                this.Play();
                this.SetCurrentTime(CurrentPosition);
            }
            else
            {
                this.Pause();
            }
        });
        IsPlayingChanged?.Invoke(this, isPlaying);
    }

}
