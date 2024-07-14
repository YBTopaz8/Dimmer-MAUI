using Android.Graphics;
using Android.Media;
using MauiAudio.Platforms.Android;
using MauiAudio.Platforms.Android.CurrentActivity;
using System.Diagnostics;
using AndroidApp = Android.App;

namespace MauiAudio;

public class NativeAudioService : INativeAudioService
{
    static NativeAudioService current;
    public static INativeAudioService Current => current ??= new NativeAudioService();
    IAudioActivity instance;
    double volume = 1;
    double balance = 0;
    bool muted=false;
    //private MediaPlayer mediaPlayer => instance != null &&
    //    instance.Binder.GetMediaPlayerService() != null ?
    //    instance.Binder.GetMediaPlayerService().mediaPlayer : null;
    private MediaPlayer mediaPlayer
    {
        get
        {
            if (instance != null && instance.Binder.GetMediaPlayerService() != null)
            {
                return instance.Binder.GetMediaPlayerService().mediaPlayer;
            }
            else
            {
                return new MediaPlayer();
                //return null;
            }
        }
    }
    public bool IsPlaying => mediaPlayer?.IsPlaying ?? false;
    public double Duration=>mediaPlayer?.Duration/1000 ?? 0;
    public double CurrentPosition => mediaPlayer?.CurrentPosition / 1000 ?? 0;

    public double Volume 
    { 
        get => volume; 
        set 
        { 
            volume = value; 
            SetVolume(volume = value, Balance); 
        } 
    }
    public double Balance { get => balance; set { balance = value;SetVolume(Volume, balance = value); } }
    public bool Muted { get => muted; set => SetMuted(value); }

    public event EventHandler<bool> IsPlayingChanged;
    public event EventHandler PlayEnded;
    public event EventHandler PlayNext;
    public event EventHandler PlayPrevious;
    public event EventHandler NotificationTapped;
    public void InitializeAsync(string audioURI)
    {
        InitializeAsync(new MediaPlay() { URL=audioURI });
    }

    public Task PauseAsync()
    {
        if (IsPlaying)
        {
            return instance.Binder.GetMediaPlayerService().Pause();
        }

        return Task.CompletedTask;
    }

    public async Task PlayAsync(double position = 0)
    {
        await instance.Binder.GetMediaPlayerService().Play();
        await instance.Binder.GetMediaPlayerService().Seek((int)position * 1000);
    }

    Task SetMuted(bool value)
    {
        muted = value;
        if (value)
            mediaPlayer.SetVolume(0, 0);
        else SetVolume(volume,balance);
        return Task.CompletedTask;
    }
    Task SetVolume(double volume, double balance)
    {

        mediaPlayer?.SetVolume((float)1, (float)1);
        Android.Media.Stream streamType = Android.Media.Stream.Music;
        var aManager = instance.Binder.GetMediaPlayerService().ExposeAudioManager();
        
        
        Debug.WriteLine($"Normal volume {volume}");
        aManager.SetStreamVolume(streamType, (int)volume, VolumeNotificationFlags.RemoveSoundAndVibrate);
        
        //volume = Math.Clamp(volume, 0, 1);
        //balance = Math.Clamp(balance, -1, 1);

        //// Using the "constant power pan rule." See: http://www.rs-met.com/documents/tutorials/PanRules.pdf
        //var left = Math.Cos((Math.PI * (balance + 1)) / 4) * volume;
        //var right = Math.Sin((Math.PI * (balance + 1)) / 4) * volume;

        //mediaPlayer?.SetVolume((float)left, (float)right);
        return Task.CompletedTask;
    }

    public async Task<bool> SetCurrentTime(double position) //will edit this so it can return something if media is not initialized;
    {
        if (mediaPlayer is null)
        {
            Debug.WriteLine("no media");
            return false;
        }
        await instance.Binder.GetMediaPlayerService().Seek((int)position * 1000);
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
        instance.Binder.GetMediaPlayerService().IsPlayingChanged += IsPlayingChanged;
        instance.Binder.GetMediaPlayerService().TaskPlayEnded += PlayEnded;
        instance.Binder.GetMediaPlayerService().TaskPlayNext += PlayNext;
        instance.Binder.GetMediaPlayerService().TaskPlayPrevious += PlayPrevious;
        instance.Binder.GetMediaPlayerService().TaskNotificationTapped += NotificationTapped;
        this.instance.Binder.GetMediaPlayerService().PlayingChanged += (object sender, bool isPlaying) =>
        {
            Task.Run(async () => {
                if (isPlaying)
                {
                    await this.PlayAsync(CurrentPosition);
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
