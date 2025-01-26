using CSCore.Codecs;
using CSCore.SoundOut;
using CSCore.Streams.Effects;
using CSCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSCore.Streams;
using System.Windows.Interop;
using Microsoft.Maui.Platform;

namespace Dimmer_MAUI.Platforms.Windows;

public partial class DimmerAudioService : IDimmerAudioService, INotifyPropertyChanged, IDisposable
{
    static DimmerAudioService? current;
    public static IDimmerAudioService Current => current ??= new DimmerAudioService();

    private WasapiOut soundOut;
    private IWaveSource? currentWaveSource;
    //private IWaveSource? _nextWaveSource;
    private Equalizer? equalizer;

    private HwndSource? _hwndSource;
    
    public DimmerAudioService()
    {
        soundOut = new WasapiOut();

        Debug.WriteLine("DimmerAudioService constructor called.");
        Debug.WriteLine($"soundOut: {soundOut}, currentWaveSource: {currentWaveSource}");

    }

    private LoopStream? _loopStream;
    private bool _isLoopingSong = false;

    public bool IsLoopingSong
    {
        get => _isLoopingSong;
        set
        {
            _isLoopingSong = value;
            if (_isLoopingSong)
            {
                _loopStream = new LoopStream(currentWaveSource);
                currentWaveSource = _loopStream; // Replace currentWaveSource
                                                 // Re-initialize WasapiOut with the new looped wave source (if needed)
            }
            else
            {
                // Revert back to non-looped wave source (if needed) - more complex to implement cleanly
                // ... (You might need to store the original non-looped wave source) ...
                _loopStream?.Dispose();
                _loopStream = null;
                // ... Re-initialize WasapiOut with the original wave source ...
            }
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
    private readonly object resourceLock = new();
    private bool disposed = false;
    private SongModelView? currentMedia;
    private SemaphoreSlim semaphoreSlim = new(1, 1);
    public HomePageVM? ViewModel { get; set; }
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

    public void Play(bool s)
    {
        lock (resourceLock)
        {
            soundOut?.Play();
            IsPlaying = true;
        }
    }


    public void Stop()
    {
        lock (resourceLock)
        {
            soundOut?.Stop();
            IsPlaying = false;
        }
    }
    public void SetCurrentTime(double positionInSec)
    {
        currentWaveSource?.SetPosition(TimeSpan.FromSeconds(positionInSec));
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

        if (disposing)
        {
            // ... In Dispose() ...
            _loopStream?.Dispose();
            _loopStream = null;
            // Clean up managed resources
            soundOut?.Stop();
            soundOut?.Dispose();
            soundOut = new();

            currentWaveSource?.Dispose();
            currentWaveSource = null;

            equalizer?.Dispose();
            equalizer = null;
        }

        // Clean up unmanaged resources, if any
        disposed = true;
    }

    //private SongModelView? _nextMedia; // To store the next media to be played

    public async Task Initialize(SongModelView? media, byte[]? imageBytes) // Changed to async void - be mindful of exceptions
    {
        await PrepareMediaAsync(media); // Call the async version
    }

    private async Task PrepareMediaAsync(SongModelView? media) // Made PrepareMedia async and returning Task
    {
        Dispose();

        if (media == null || string.IsNullOrEmpty(media.FilePath))
        {
            Debug.WriteLine("Invalid media file");
            return;
        }
        currentMedia = media;
        try
        {
            // **Move audio source creation to background thread**
            IWaveSource? waveSource = await Task.Run(() => // Run the heavy lifting in a background thread
            {
                try
                {
                    var source = CodecFactory.Instance.GetCodec(media.FilePath)
                                               .ToSampleSource()
                                               .ToStereo();
                    equalizer = Equalizer.Create10BandEqualizer(source);
                    return equalizer.ToWaveSource(); // Return the WaveSource
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error creating wave source in background thread: {ex.Message}");
                    return null; // Or throw the exception to be caught outside Task.Run
                }
            });

            if (waveSource == null) // Handle potential error during wave source creation
            {
                Debug.WriteLine("Failed to create wave source.");
                return;
            }

            currentWaveSource = waveSource;

            // Initialize WasapiOut and start playback (can likely stay on calling thread now)
            soundOut = new WasapiOut();
            soundOut.Initialize(currentWaveSource);
            AttachStoppedHandler();
            Play(true); // Start playback
            RegisterMediaKeys(); // Register media keys after successful initialization

            ViewModel ??= IPlatformApplication.Current?.Services.GetService<HomePageVM>()!;

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing media: {ex.Message}");
        }
    }
    private void RegisterMediaKeys()
    {
        if (_hwndSource != null)
            return;

        // Get the dispatcher of the window and check if its on the main thread
        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(() => RegisterMediaKeys());
            return;
        }

        // Get the window from MAUI's IPlatformApplication
        var mauiWindow = IPlatformApplication.Current?.Services.GetService<DimmerWindow>();
        if (mauiWindow is null)
        {
            Debug.WriteLine($"Could not register media keys as window could not be found.");
            return;
        }

        // Get the native window handle
        var nativeWindowHandle = mauiWindow.Handler?.PlatformView as Microsoft.UI.Xaml.Window;

        if (nativeWindowHandle is null)
        {
            Debug.WriteLine($"Could not register media keys as handle could not be found.");
            return;
        }
        // Create a HwndSource to get the handle of our main window.
        var hwndSource = new HwndSource(new HwndSourceParameters
        {
            ParentWindow= nativeWindowHandle.GetWindowHandle(),
            WindowStyle = 0
        });


        _hwndSource = hwndSource;
        // Event listener for processing messages (including media key events).
        hwndSource.AddHook(WndProc);

    }
    private void UnregisterMediaKeys()
    {
        if (_hwndSource == null)
            return;

        _hwndSource.RemoveHook(WndProc);
        _hwndSource.Dispose();
        _hwndSource = null;

    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_APPCOMMAND = 0x0319;
        const int APPCOMMAND_MEDIA_PLAY_PAUSE = 0x0E0000;
        const int APPCOMMAND_MEDIA_STOP = 0x0E0001;
        const int APPCOMMAND_MEDIA_NEXTTRACK = 0x0E000B;
        const int APPCOMMAND_MEDIA_PREVIOUSTRACK = 0x0E000C;

        if (msg == WM_APPCOMMAND)
        {
            int command = (int)(lParam.ToInt64() & 0xFFFF0000) >> 16;

            switch (command)
            {
                case APPCOMMAND_MEDIA_PLAY_PAUSE:
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (IsPlaying)
                        {
                            Pause();
                        }
                        else
                        {
                            Resume(currentWaveSource?.GetPosition().TotalSeconds ?? 0);
                        }
                    });
                    handled = true;
                    break;

                case APPCOMMAND_MEDIA_STOP:
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Stop();
                    });
                    handled = true;
                    break;

                case APPCOMMAND_MEDIA_NEXTTRACK:
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        PlayNext?.Invoke(this, EventArgs.Empty);
                    });
                    handled = true;
                    break;
                case APPCOMMAND_MEDIA_PREVIOUSTRACK:
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        PlayPrevious?.Invoke(this, EventArgs.Empty);
                    });
                    handled = true;
                    break;
            }

        }
        return IntPtr.Zero;
    }
    private void AttachStoppedHandler() // Helper method to attach Stopped handler (for cleaner code)
    {
        if (soundOut != null)
        {
            soundOut.Stopped += async (s, e) => // Re-attach the Stopped handler to the *new* soundOut
            {
                lock (resourceLock)
                {
                    if (disposed)
                    {
                        IsPlaying = false;
                        PlayEnded?.Invoke(this, EventArgs.Empty);
                        return;
                    }

                    IsPlaying = false;
                    PlayEnded?.Invoke(this, EventArgs.Empty); // Normal "Play Ended"
                }
            };
        }
    }


    private void DisposeCurrentPlaybackResources() // Helper method to dispose soundOut, currentWaveSource, etc.
    {
        soundOut?.Stop();
        soundOut?.Dispose();
        soundOut = null;

        currentWaveSource?.Dispose();
        currentWaveSource = null;

        equalizer?.Dispose(); // Dispose equalizer as well in transition
        equalizer = null;
    }




    // Placeholder - Implement your playlist logic to get the next song
    private SongModelView? GetNextMediaItemFromPlaylist()
    {

        //if (ViewModel?.CurrentPlaylist == null || ViewModel.CurrentPlaylist.Count <= 1 || currentMediaItem == null)
        //{
        //    return null; // No playlist or only one song, or no current song
        //}

        //// --- Example Playlist Logic (Sequential Playback) ---
        //int currentIndex = ViewModel.CurrentPlaylist.IndexOf(currentMediaItem);
        //if (currentIndex != -1 && currentIndex < ViewModel.CurrentPlaylist.Count - 1)
        //{
        //    return ViewModel.CurrentPlaylist[currentIndex + 1]; // Get the next song in the list
        //}
        //else
        //{
        //    return null; // Reached end of playlist (or error) - you might want to loop back to the beginning here if desired
        //}
        return null;
    }


public void ApplyEqualizerSettings(float[] bands)
{
    if (equalizer == null || bands.Length != equalizer.SampleFilters.Count)
        throw new ArgumentException("Invalid equalizer settings.");

    for (int i = 0; i < bands.Length; i++)
    {
        equalizer.SampleFilters[i].AverageGainDB = bands[i];
    }
}


    #region Equalizer Presets

    private static readonly Dictionary<EqualizerPresetName, float[]> EqualizerPresets = new Dictionary<EqualizerPresetName, float[]>
{
    { EqualizerPresetName.Flat,      new float[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } }, // Flat EQ
    { EqualizerPresetName.Rock,      new float[] { 4, 2, 0, 2, 4, 2, 0, 0, 2, 4 } }, // Rock - Boosted mids and highs
    { EqualizerPresetName.Pop,       new float[] { 2, 3, 1, 0, 1, 2, 3, 2, 1, 0 } }, // Pop - Emphasized bass and treble slightly
    { EqualizerPresetName.Classical, new float[] { 0, 0, 2, 4, 5, 4, 2, 0, 0, 0 } }, // Classical - Emphasized mids for clarity
    { EqualizerPresetName.Jazz,      new float[] { 1, 2, 3, 2, 1, 0, 1, 2, 3, 2 } }, // Jazz - Warm, slightly boosted mids and highs
    { EqualizerPresetName.Dance,     new float[] { 5, 3, 0, 0, 2, 4, 3, 2, 1, 0 } }, // Dance - Heavy bass and boosted highs
    { EqualizerPresetName.BassBoost, new float[] { 8, 6, 4, 2, 0, 0, 0, 0, 0, 0 } }, // Bass Boost - Extreme bass boost
    { EqualizerPresetName.TrebleBoost, new float[] { 0, 0, 0, 0, 0, 0, 2, 4, 6, 8 } }  // Treble Boost - Extreme treble boost
};

    public void ApplyEqualizerPreset(EqualizerPresetName presetName)
    {
        if (equalizer != null && EqualizerPresets.TryGetValue(presetName, out float[]? value))
        {
            ApplyEqualizerSettings(value);
        }
        else
        {
            Debug.WriteLine($"Equalizer not initialized or Preset '{presetName}' not found.");
        }
    }

    public List<string> GetEqualizerPresetNames()
    {
        return EqualizerPresets.Keys.Select(key => key.ToString()).ToList();
    }

    #endregion Equalizer Presets
    
}