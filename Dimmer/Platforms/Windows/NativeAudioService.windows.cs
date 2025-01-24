using CSCore.Codecs;
using CSCore.SoundOut;
using CSCore.Streams.Effects;
using CSCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSCore.Streams;

namespace Dimmer_MAUI.Platforms.Windows;


public partial class DimmerAudioService : IDimmerAudioService, INotifyPropertyChanged, IDisposable
{
    static DimmerAudioService? current;
    public static IDimmerAudioService Current => current ??= new DimmerAudioService();
    HomePageVM? ViewModel { get; set; }

    private WasapiOut soundOut;
    private IWaveSource? currentWaveSource;
    private IWaveSource? _nextWaveSource;
    private Equalizer? equalizer;

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

    public event EventHandler<bool>? IsPlayingChanged;
    public event EventHandler? PlayEnded;
    public event EventHandler? PlayNext;
    public event EventHandler? PlayPrevious;
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<long>? IsSeekedFromNotificationBar;
    private readonly object resourceLock = new object();

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

    private bool disposed = false;
    
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

    SongModelView? currentMedia;
    private SongModelView? _nextMedia; // To store the next media to be played

    private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

    public async Task InitializeAsync(SongModelView? media, byte[]? imageBytes)
    {
        await PrepareMediaAsync(media); // Use the new async method
    }
    private async Task PrepareMediaAsync(SongModelView? media) // Make Initialize/Prepare async
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
            currentWaveSource = equalizer.ToWaveSource(); // Convert back to WaveSource

            await Task.Run(() =>
            {
                soundOut = new WasapiOut();
                soundOut.Initialize(currentWaveSource);
                AttachStoppedHandler(); // Attach handler *inside* the Task.Run
                soundOut.Play(); // Start playback *inside* the Task.Run
            });

            soundOut.Stopped += async (s, e) =>
            {
                await semaphoreSlim.WaitAsync(); // Acquire the semaphore
                try
                {
                    if (disposed)
                    {
                        // Set playback state
                        IsPlaying = false;
                        PlayEnded?.Invoke(this, EventArgs.Empty);
                        return; // Avoid operating on disposed objects
                    }

                    // --- Gapless Transition Logic ---
                    if (_nextWaveSource != null && _nextMedia != null) // Check if next song is pre-loaded
                    {
                        Debug.WriteLine("Starting gapless transition to next song.");

                        // Dispose of resources from the previous song
                        Stop(); // Stop current playback
                        DisposeCurrentPlaybackResources(); // Helper method to dispose soundOut, currentWaveSource, etc.

                        // Switch to the pre-loaded next song
                        currentMedia = _nextMedia;
                        currentWaveSource = _nextWaveSource;

                        
                        soundOut = new WasapiOut();
                        soundOut.Initialize(currentWaveSource);
                        AttachStoppedHandler(); // Attach handler inside the Task.Run
                        soundOut.Play(); // Start playback inside the Task.Run
                        

                        PlayNext?.Invoke(this, EventArgs.Empty); // Trigger "Play Next" event
                    }
                    else
                    {
                        IsPlaying = false;
                        PlayEnded?.Invoke(this, EventArgs.Empty); // Normal "Play Ended" when no next song
                    }
                }
                finally
                {
                    semaphoreSlim.Release(); // Always release the semaphore
                }

                // After starting the next song (or if no next song), prepare the *next* next song in the background
                PrepareNextMedia(); // Start pre-loading the *next* song asynchronously
            };


            ViewModel ??= IPlatformApplication.Current?.Services.GetService<HomePageVM>()!;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing media: {ex.Message}");
        }
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

                    // --- Gapless Transition Logic ---
                    if (_nextWaveSource != null && _nextMedia != null) // Check if next song is pre-loaded
                    {
                        Debug.WriteLine("Starting gapless transition to next song.");

                        // 1. Dispose of resources from the *previous* song
                        Stop(); // Stop current playback (already stopped by event, but just in case)
                        DisposeCurrentPlaybackResources(); // Helper method to dispose soundOut, currentWaveSource, etc.

                        // 2. Switch to the pre-loaded next song
                        currentMedia = _nextMedia;
                        currentWaveSource = _nextWaveSource;
                        soundOut = new WasapiOut(); // Create a new WasapiOut instance
                        soundOut.Initialize(currentWaveSource);
                        AttachStoppedHandler(); // Re-attach the Stopped handler to the *new* soundOut

                        // Clear the "next" song variables
                        _nextMedia = null;
                        _nextWaveSource = null;

                        // 3. Start playback of the next song
                        Play(true); // Resume playback immediately

                        PlayNext?.Invoke(this, EventArgs.Empty); // If you want to trigger "Play Next" event
                    }
                    else
                    {
                        IsPlaying = false;
                        PlayEnded?.Invoke(this, EventArgs.Empty); // Normal "Play Ended" when no next song
                    }
                }

                // After starting the next song (or if no next song), prepare the *next* next song in the background
                PrepareNextMedia(); // Start pre-loading the *next* song asynchronously
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


    private void PrepareNextMedia() // Asynchronously pre-load the next song
    {
        // --- Logic to determine the *next* song in your playlist ---
        // This is a placeholder - you need to implement your playlist handling
        SongModelView? nextMedia = GetNextMediaItemFromPlaylist(); // Replace with your playlist logic

        if (nextMedia != null && nextMedia != _nextMedia) // Check if we have a valid next song and avoid re-loading the same one
        {
            Debug.WriteLine($"Pre-loading next media: {nextMedia.FilePath}");
            try
            {
                // Prepare WaveSource for the *next* song (but don't initialize WasapiOut yet)
                var nextWaveSourceBase = CodecFactory.Instance.GetCodec(nextMedia.FilePath)
                                                   .ToSampleSource()
                                                   .ToStereo();
                var nextEqualizer = Equalizer.Create10BandEqualizer(nextWaveSourceBase); // Create a *new* equalizer for the next song if needed
                _nextWaveSource = nextEqualizer.ToWaveSource(); // Store the WaveSource for the next song
                _nextMedia = nextMedia; // Store the next media info

                // --- Optional: Implement buffering of _nextWaveSource here if possible with CSCore ---
                // (This is more advanced and might require deeper CSCore stream manipulation)
                // For now, we are just pre-loading the WaveSource.
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error pre-loading next media: {ex.Message}");
                _nextMedia = null; // Clear next media if pre-loading fails
                _nextWaveSource = null;
            }
        }
        else
        {
            _nextMedia = null; // Clear next media if no valid next song
            _nextWaveSource = null;
            Debug.WriteLine("No valid next media to pre-load.");
        }
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
        if (equalizer != null && bands.Length == equalizer.SampleFilters.Count)
        {
            for (int i = 0; i < bands.Length; i++)
            {
                equalizer.SampleFilters[i].AverageGainDB = bands[i];
            }
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