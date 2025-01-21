using CSCore.Codecs;
using CSCore.SoundOut;
using CSCore.Streams.Effects;
using CSCore;


namespace Dimmer_MAUI.Platforms.Windows;


public partial class NativeAudioService : INativeAudioService, INotifyPropertyChanged
{
    static NativeAudioService? current;
    public static INativeAudioService Current => current ??= new NativeAudioService();
    HomePageVM? ViewModel { get; set; }

    private ISoundOut? soundOut;
    private IWaveSource? currentWaveSource;
    private Equalizer? equalizer;

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

    public double Duration => currentWaveSource?.GetLength().TotalSeconds ?? 0;
    public double CurrentPosition => currentWaveSource?.GetPosition().TotalSeconds ?? 0;

    public double Volume
    {
        get => soundOut?.Volume ?? 0;
        set
        {
            if (soundOut != null)
            {
                soundOut.Volume = Math.Clamp((float)value, 0f, 1f);
            }
        }
    }

    public double Balance
    {
        get => 0; // CSCore does not natively support balance
        set { /* Implement if necessary using custom processing */ }
    }

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
        soundOut?.Pause();
        IsPlaying = false;
    }

    public void Resume(double positionInSeconds)
    {
        if (currentWaveSource != null)
        {
            currentWaveSource.SetPosition(TimeSpan.FromSeconds(positionInSeconds));
        }
        soundOut?.Play();
        IsPlaying = true;
    }

    public void Play(bool IsFromPreviousOrNext = false)
    {
        soundOut?.Play();
        IsPlaying = true;
    }

    public void Stop()
    {
        soundOut?.Stop();
        IsPlaying = false;
    }

    public void SetCurrentTime(double positionInSec)
    {
        currentWaveSource?.SetPosition(TimeSpan.FromSeconds(positionInSec));
    }

    public void Dispose()
    {
        soundOut?.Dispose();
        currentWaveSource?.Dispose();
        equalizer?.Dispose();
    }
    SongModelView? currentMedia;
    public void Initialize(SongModelView? media, byte[]? imageBytes)
    {
        Dispose(); // Clean up any existing playback

        if (media == null || string.IsNullOrEmpty(media.FilePath))
        {
            Debug.WriteLine("Invalid media file");
            return;
        }
        currentMedia = media;
        try
        {
            // Create the WaveSource
            var waveSource = CodecFactory.Instance.GetCodec(media.FilePath)
                               .ToSampleSource()
                               
                               .ToStereo(); // Ensure it's stereo for effects

            // Add Equalizer
            equalizer = Equalizer.Create10BandEqualizer(waveSource);
            currentWaveSource = equalizer.ToWaveSource(16); // Convert back to WaveSource

            // Initialize the SoundOut (playback engine)
            soundOut = new WasapiOut();
            soundOut.Initialize(currentWaveSource);

            soundOut.Stopped += (s, e) =>
            {
                // Check if the playback ended naturally
                bool isCompleted = currentWaveSource?.GetPosition() >= currentWaveSource?.GetLength();
                Debug.WriteLine($"Playback stopped. Natural completion: {isCompleted}");


                // Invoke PlayEnded only if it completed naturally
                if (isCompleted && media == currentMedia)
                {
                    // Set playback state
                    IsPlaying = false;
                    PlayEnded?.Invoke(this, EventArgs.Empty);
                }
            };


            ViewModel ??= IPlatformApplication.Current?.Services.GetService<HomePageVM>()!;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing media: {ex.Message}");
        }
    }

    public void ApplyEqualizerSettings(float[] bands)
    {
        if (equalizer != null && bands.Length == equalizer.SampleFilters.Count)
        {
            for (int i = 0; i < bands.Length; i++)
            {
                equalizer.SampleFilters[i].AverageGainDB = bands[i];
            }
        }
    }
}