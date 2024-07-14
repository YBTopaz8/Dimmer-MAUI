using AudioUnit;
using AVFoundation;
using Foundation;

namespace MauiAudio;

public class NativeAudioService : INativeAudioService
{
    static NativeAudioService current;
    public static INativeAudioService Current => current ??= new NativeAudioService();
    //AVPlayer avPlayer;
    AVAudioPlayer avPlayer;
    float volume = 1;
    public bool IsPlaying => avPlayer != null
        ? avPlayer.Playing
        : false;

    public double CurrentPosition => avPlayer?.CurrentTime ?? 0;
    public double Duration => avPlayer?.Duration ?? 0;

    public double Volume { get => volume; set { volume = (float)Math.Clamp(value, 0, 1); avPlayer.Volume = volume; } }
    public bool Muted { get => avPlayer?.Volume == 0; set { if (value) avPlayer.Volume = 0; else avPlayer.Volume = volume; } }
    public double Balance { get => avPlayer?.Pan ?? 0; set => avPlayer.Pan = (float)Math.Clamp(value, -1, 1); }

    public event EventHandler<bool> IsPlayingChanged;
    public event EventHandler PlayEnded;
    public event EventHandler PlayNext;
    public event EventHandler PlayPrevious;
    public event EventHandler NotificationTapped;

    public void InitializeAsync(string audioURI)
    {
        InitializeAsync(new MediaPlay() { URL = audioURI });
    }

    public Task PauseAsync()
    {
        avPlayer?.Pause();

        return Task.CompletedTask;
    }

    public Task PlayAsync(double position = 0)
    {
        avPlayer.PlayAtTime(position);
        avPlayer?.Play();
        return Task.CompletedTask;
    }

    public Task<bool> SetCurrentTime(double value)
    {
        if (avPlayer is null)
        {
            return Task.FromResult(false);
        }
        avPlayer.PlayAtTime(value);
        return Task.FromResult(true);
    }

    public Task DisposeAsync()
    {
        avPlayer?.Dispose();
        return Task.CompletedTask;
    }

    public async Task InitializeAsync(MediaPlay media)
    {

        if (avPlayer != null)
        {
            await PauseAsync();
        }
        if(media.Stream!=null){
            // Using Stream
            var data = NSData.FromStream(media.Stream)?? throw new Exception("Unable to convert audioStream to NSData.");
            avPlayer = AVAudioPlayer.FromData(data)
            ?? throw new Exception("Unable to create AVAudioPlayer from data.");
        }
        else{
            // Using URL
            NSUrl fileURL = new NSUrl(media.URL);
            avPlayer = AVAudioPlayer.FromUrl(fileURL);
        }
        avPlayer.FinishedPlaying += OnPlayerFinishedPlaying;
    }
    void OnPlayerFinishedPlaying(object? sender, AVStatusEventArgs e)
    {
        PlayEnded?.Invoke(this, e);
        PlayNext?.Invoke(this, e);
    }
}