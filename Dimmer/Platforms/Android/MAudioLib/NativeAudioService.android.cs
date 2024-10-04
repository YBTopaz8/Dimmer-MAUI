using Stream = Android.Media.Stream;
using Android.Media;
using Dimmer_MAUI.Platforms.Android.CurrentActivity;

namespace Dimmer_MAUI.Platforms.Android.MAudioLib;

public class NativeAudioService : INativeAudioService
{
    static NativeAudioService current;
    public static INativeAudioService Current => current ??= new NativeAudioService();
    IAudioActivity instance;
    double volume = 1;
    double balance = 0;
    bool muted = false;
    //private MediaPlayer mediaPlayer => instance != null &&
    //    instance.Binder.GetMediaPlayerService() != null ?
    //    instance.Binder.GetMediaPlayerService().mediaPlayer : null;
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

    public bool IsPlaying
    {
        get
        {
            return mediaPlayer?.IsPlaying ?? false;
        }
        

    }

    public double Duration
    {
        get
        {
            if (mediaPlayer == null)
                Console.WriteLine("media player is null");
            return mediaPlayer?.Duration / 1000 ?? 0;
        }
    }

    double _currentPosInMs;
    public double CurrentPosition
    {
        get
        {
            // Return the current position in seconds (convert from milliseconds)
            return mediaPlayer?.CurrentPosition / 1000.0 ?? _currentPosInMs / 1000.0;
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
    public void InitializeAsync(string audioURI)
    {
        Task.Run(() => InitializeAsync(new MediaPlay() { URL = audioURI }));
    }

    public Task PauseAsync()
    {
        if (IsPlaying)
        {
            return instance.Binder.GetMediaPlayerService().Pause();
        }

        return Task.CompletedTask;
    }

    public async Task PlayAsync(double position = 0, bool IsFromUser = false)
    {
        var posInMs = (int)(position * Duration * 1000);
        await instance.Binder.GetMediaPlayerService().Play((int)posInMs);
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

        mediaPlayer?.SetVolume((float)1, (float)1);

        Stream streamType = Stream.Music;
        if (instance == null)
        {
            var activity = CrossCurrentActivity.Current;
            instance = activity.Activity as IAudioActivity;
        }
        var aManager = instance.Binder.GetMediaPlayerService().ExposeAudioManager();

        volume = Math.Max(0, Math.Min(15, volume));
        int scaledVolume = (int)Math.Round(volume);
        aManager.SetStreamVolume(streamType, scaledVolume, VolumeNotificationFlags.RemoveSoundAndVibrate);

        //volume = Math.Clamp(volume, 0, 1);
        //balance = Math.Clamp(balance, -1, 1);

        //// Using the "constant power pan rule." See: http://www.rs-met.com/documents/tutorials/PanRules.pdf
        //var left = Math.Cos((Math.PI * (balance + 1)) / 4) * volume;
        //var right = Math.Sin((Math.PI * (balance + 1)) / 4) * volume;

        //mediaPlayer?.SetVolume((float)left, (float)right);
        return Task.CompletedTask;
    }

    public async Task<bool> SetCurrentTime(double position)
    {
        //position = (position) * Duration;
        var posInMs = (int)(position * Duration * 1000);
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

    public async Task InitializeAsync(MediaPlay media)
    {
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

        if (instance.Binder.GetMediaPlayerService().mediaPlayer == null)
            Console.WriteLine("MediaPlayer is null");
        instance.Binder.GetMediaPlayerService().IsPlayingChanged += IsPlayingChanged;
        instance.Binder.GetMediaPlayerService().TaskPlayEnded += PlayEnded;
        instance.Binder.GetMediaPlayerService().TaskPlayNext += PlayNext;
        instance.Binder.GetMediaPlayerService().TaskPlayPrevious += PlayPrevious;
        this.instance.Binder.GetMediaPlayerService().PlayingChanged += (object sender, bool isPlaying) =>
        {
            Task.Run(async () => {
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
        };
        //if(media.Image!=null) instance.Binder.GetMediaPlayerService().Cover= await GetImageBitmapFromUrl(media.Image);
        //else instance.Binder.GetMediaPlayerService().Cover = null;
        instance.Binder.GetMediaPlayerService().mediaPlay = media;
    }

}
