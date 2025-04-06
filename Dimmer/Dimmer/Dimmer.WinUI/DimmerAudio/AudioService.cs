
using Dimmer.Interfaces;
using Dimmer.Utilities.Events;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;
using Windows.Media.Core;
using Windows.Media.Effects;
using Windows.Media.Playback;
using Windows.Media;
using Windows.Storage.Streams;
using Microsoft.UI.Dispatching;
using Windows.Foundation.Collections;
using System.Windows.Media;
using Dimmer.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;


namespace Dimmer.WinUI.DimmerAudio;


public partial class AudioService : IDimmerAudioService, INotifyPropertyChanged, IDisposable
{
    private SystemMediaTransportControls _smtc;
   
    
    private IWavePlayer? _soundOut; 
    private WaveStream? _reader;   
    private SampleChannel? _sampleChannel; 

    
    private bool _isPlaying;
    private bool _isDisposed = false;
    private float _nonMutedVolume = 1.0f; 
    private bool _isMutedManual = false; 
    private bool _isPaused = false; 

    
    static AudioService? current;
    public static IDimmerAudioService Current => current ??= new AudioService();

    
    SongModelView? CurrentMedia { get; set; }

    
    public event EventHandler<bool>? IsPlayingChanged;
    public event EventHandler<PlaybackEventArgs>? PlayEnded;
    public event EventHandler? PlayNext; 
    public event EventHandler? PlayPrevious; 
    public event EventHandler? PlayStopAndShowWindow; 
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<long>? IsSeekedFromNotificationBar; 

    public AudioService()
    {
    
        var _mp = new Windows.Media.Playback.MediaPlayer();
        _smtc = _mp.SystemMediaTransportControls;

        
        _smtc.IsPlayEnabled = true;
        _smtc.IsPauseEnabled = true;
        _smtc.IsNextEnabled = true; 
        _smtc.IsPreviousEnabled = true; 
        _smtc.ButtonPressed += Smts_ButtonPressed;

        
        _smtc.PlaybackStatus = MediaPlaybackStatus.Closed; 
        _smtc.DisplayUpdater.Type = MediaPlaybackType.Music; 
        _smtc.DisplayUpdater.Update(); 
        
    }

    private void Smts_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
    {
        InvokeOnMainThread(() => {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    Debug.WriteLine("SMTC Play pressed");
                    
                    if (_isPaused || _soundOut?.PlaybackState == PlaybackState.Stopped)
                    {
                        Play(true); 
                    }
                    break;
                case SystemMediaTransportControlsButton.Stop:
                    Debug.WriteLine("SMTC Stop pressed");
                    
                    _soundOut?.Stop(); 

                    IsPlaying = false; 
                    PlayStopAndShowWindow?.Invoke(this,EventArgs.Empty); 
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    Debug.WriteLine("SMTC Pause pressed");
                    Pause();
                    break;

                case SystemMediaTransportControlsButton.Next:
                    Debug.WriteLine("SMTC Next pressed");
                    PlayNext?.Invoke(this, EventArgs.Empty); 
                    break;

                case SystemMediaTransportControlsButton.Previous:
                    Debug.WriteLine("SMTC Previous pressed");
                    PlayPrevious?.Invoke(this, EventArgs.Empty); 
                    break;

                
                default:
                    break;
            }
        });
    }

    

    public bool IsPlaying
    {
        get => _isPlaying;
        private set
        {
            
            
            if (SetProperty(ref _isPlaying, value, nameof(IsPlaying)))
            {
                
                InvokeOnMainThread(() => IsPlayingChanged?.Invoke(this, _isPlaying));
            }
        }
    }

    
    public double Duration => _reader?.TotalTime.TotalSeconds ?? 0;
    public double CurrentPosition => _reader?.CurrentTime.TotalSeconds ?? 0;

    
    public double Volume
    {
        get => (double)(_sampleChannel?.Volume ?? _nonMutedVolume); 
        set
        {
            var clampedValue = (float)Math.Clamp(value, 0.0, 1.0);

            
            _nonMutedVolume = clampedValue;

            
            if (_sampleChannel != null && !_isMutedManual)
            {
                _sampleChannel.Volume = clampedValue;
            }
            
            OnPropertyChanged(); 
        }
    }

    
    public bool Muted
    {
        get => _isMutedManual;
        set
        {
            if (_isMutedManual == value)
                return; 

            _isMutedManual = value;

            if (_sampleChannel != null)
            {
                if (_isMutedManual)
                {
                    
                    _sampleChannel.Volume = 0f;
                }
                else
                {
                    
                    _sampleChannel.Volume = _nonMutedVolume;
                }
            }
            OnPropertyChanged(); 
        }
    }

    public double Balance
    {
        get => 0; 
        set { /* Not implemented */ }
    }

    

    public void Pause()
    {
        if (_soundOut?.PlaybackState == PlaybackState.Playing)
        {
            _soundOut.Pause(); 
            IsPlaying = false;
            _isPaused = true;
            // --- Add SMTC Update ---
            _smtc.PlaybackStatus = MediaPlaybackStatus.Paused;
            _smtc.IsPlayEnabled = true;
            _smtc.IsPauseEnabled = false;
            _smtc.DisplayUpdater.Update();
            // --- End SMTC Update ---
        }
    }

    
    public void Resume(double positionInSeconds)
    {
        if (_soundOut != null && _reader != null)
        {
            SetCurrentTime(positionInSeconds); 
            Play(true); 
        }
    }

    
    public void Play(bool s) 
    {
        if (_soundOut != null && _reader != null)
        {
            
            if (_soundOut.PlaybackState == PlaybackState.Stopped || _soundOut.PlaybackState == PlaybackState.Paused)
            {
                try
                {
                    _soundOut.Play();
                    IsPlaying = true;
                    _isPaused = false;

                    _smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
                    _smtc.IsPlayEnabled = false;
                    _smtc.IsPauseEnabled = true;
                    _smtc.DisplayUpdater.Update();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"NAudio: Error starting playback: {ex.Message}");
                    
                }
            }
        }
        else
        {
            Debug.WriteLine("AudioService (NAudio): Play called but soundOut or reader is null.");
        }
    }

    public void SetCurrentTime(double positionInSec)
    {
        if (_reader != null)
        {
            try
            {
                var targetPosition = TimeSpan.FromSeconds(positionInSec);
                
                if (targetPosition < TimeSpan.Zero)
                    targetPosition = TimeSpan.Zero;
                if (targetPosition > _reader.TotalTime)
                    targetPosition = _reader.TotalTime;

                _reader.CurrentTime = targetPosition;

                OnPropertyChanged(nameof(CurrentPosition));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"NAudio: Error seeking: {ex.Message}");
                
            }
        }
    }

    

    
    public void Initialize(SongModelView? media, byte[]? ImageBytes)
    {
        
        CleanupResources();
        IsPlaying = false; 
        _isPaused = false;

        _soundOut?.Stop(); 
        _soundOut?.Dispose(); 
        if (media == null || string.IsNullOrEmpty(media.FilePath))
        {
            Debug.WriteLine("AudioService (NAudio): Initialize called with null media or FilePath.");
            CurrentMedia = null;
            UpdatePlaybackProperties(); 

            UpdateSmtcMetadata(null, null); 
            _smtc.PlaybackStatus = MediaPlaybackStatus.Closed;
            _smtc.DisplayUpdater.Update();
            return;
        }

        
        CurrentMedia = media;

        try
        {
            

            _reader = new AudioFileReader(media.FilePath);

            
            
            
            _sampleChannel = new SampleChannel(_reader, true); 
            _sampleChannel.Volume = _isMutedManual ? 0f : _nonMutedVolume; 

            
            _soundOut = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 100); 


            
            _soundOut.Init(_sampleChannel);

            
            _soundOut.PlaybackStopped += SoundOut_PlaybackStopped;

            
            UpdateSmtcMetadata(media, ImageBytes);
            _smtc.PlaybackStatus = MediaPlaybackStatus.Paused; 
            _smtc.IsPlayEnabled = true; 
            _smtc.IsPauseEnabled = false;
            _smtc.DisplayUpdater.Update();
            
            UpdatePlaybackProperties();

        }
        catch (FileNotFoundException fnfEx)
        {
            Debug.WriteLine($"NAudio: File not found for {media.FilePath}: {fnfEx.Message}");
            HandleInitializationError("File not found.");
        }
        catch (System.Runtime.InteropServices.COMException comEx)
        {
            Debug.WriteLine($"NAudio: COM Error initializing {media.FilePath} (likely unsupported format/codec): {comEx.Message}");
            HandleInitializationError("Unsupported format or missing codec.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"NAudio: Error initializing playback for {media.FilePath}: {ex.Message}");
            HandleInitializationError("Error loading audio.");
        }
    }


    
    private void UpdateSmtcMetadata(SongModelView? media, byte[]? imageBytes)
    {
        
        InvokeOnMainThread(async () => {
            if (media != null)
            {
                _smtc.DisplayUpdater.Type = MediaPlaybackType.Music;
                _smtc.DisplayUpdater.MusicProperties.Title = media.Title ?? "Unknown Title";
                _smtc.DisplayUpdater.MusicProperties.Artist = media.ArtistName ?? "Unknown Artist";
                _smtc.DisplayUpdater.MusicProperties.AlbumTitle = media.AlbumName ?? string.Empty; 

                if (imageBytes != null && imageBytes.Length > 0)
                {
                    try
                    {
                        using (var stream = new InMemoryRandomAccessStream())
                        {
                            using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
                            {
                                writer.WriteBytes(imageBytes);
                                await writer.StoreAsync();
                            }
                            _smtc.DisplayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromStream(stream);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"SMTC: Error setting thumbnail: {ex.Message}");
                        _smtc.DisplayUpdater.Thumbnail = null; 
                    }
                }
                else
                {
                    _smtc.DisplayUpdater.Thumbnail = null;
                }
            }
            else 
            {
                _smtc.DisplayUpdater.MusicProperties.Title = "";
                _smtc.DisplayUpdater.MusicProperties.Artist = "";
                _smtc.DisplayUpdater.MusicProperties.AlbumTitle = "";
                _smtc.DisplayUpdater.Thumbnail = null;
            }
            _smtc.DisplayUpdater.Update(); 
        });
    }

    
    private void HandleInitializationError(string userMessage)
    {
        CleanupResources();
        CurrentMedia = null;
        UpdatePlaybackProperties();
        
        
        
    }


    
    private void UpdatePlaybackProperties()
    {
        OnPropertyChanged(nameof(Duration));
        OnPropertyChanged(nameof(CurrentPosition));
    }

    
    private void SoundOut_PlaybackStopped(object? sender, StoppedEventArgs e)
    {
        
        if (_isDisposed)
            return;

        
        bool reachedEnd = (_reader != null && _reader.Position >= _reader.Length);
        bool stoppedManually = !_isPlaying && !_isPaused; 

        
        bool wasError = e.Exception != null;

        
        InvokeOnMainThread(() =>
        {
            
            if (_isDisposed)
                return;

            MediaPlaybackStatus finalStatus = MediaPlaybackStatus.Stopped; // Default assumption

            IsPlaying = false; 
            _isPaused = false; 

            if (wasError)
            {
                Debug.WriteLine($"NAudio: Playback stopped due to error: {e.Exception?.Message}");
                finalStatus = MediaPlaybackStatus.Closed; 

            }
            
            
            else if (reachedEnd && !stoppedManually)
            {
                PlaybackEventArgs args = new PlaybackEventArgs();
                args.MediaSong = CurrentMedia;
                PlayEnded?.Invoke(this,  args);
                Debug.WriteLine("NAudio: Playback naturally ended.");
                finalStatus = MediaPlaybackStatus.Stopped; // Or Paused if you want it ready to replay

                SetCurrentTime(0);
            }
            else
            {
                finalStatus = MediaPlaybackStatus.Stopped;
                Debug.WriteLine($"NAudio: Playback stopped (Position: {_reader?.Position}, Length: {_reader?.Length}, ReachedEnd: {reachedEnd}, StoppedManuallyOrPaused: {stoppedManually || _isPaused}).");
            }

            // --- Add SMTC Update ---
            _smtc.PlaybackStatus = finalStatus;
            _smtc.IsPlayEnabled = true; // Generally allow playing again after stop
            _smtc.IsPauseEnabled = false;
            _smtc.DisplayUpdater.Update();
            // --- End SMTC Update ---
        });
    }

    

    public void ApplyEqualizerSettings(float[] bands)
    {
        
        
        
        
        
        
        
        
        Debug.WriteLine("NAudio: ApplyEqualizerSettings - Not Implemented");
        throw new NotImplementedException("NAudio Equalizer requires rebuilding the audio chain.");
    }

    public void ApplyEqualizerPreset(EqualizerPresetName presetName)
    {
        Debug.WriteLine("NAudio: ApplyEqualizerPreset - Not Implemented");
        throw new NotImplementedException("NAudio Equalizer requires rebuilding the audio chain.");
    }

    

    private void CleanupResources()
    {
        
        if (_soundOut != null)
        {
            try
            { _soundOut.Stop(); }
            catch { /* Ignore */ }
            
            _soundOut.PlaybackStopped -= SoundOut_PlaybackStopped;
        }

        
        try
        { _soundOut?.Dispose(); }
        catch (Exception ex) { Debug.WriteLine($"NAudio: Error disposing soundOut: {ex.Message}"); }
        _soundOut = null;

        
        
        _sampleChannel = null;

        
        try
        { _reader?.Dispose(); }
        catch (Exception ex) { Debug.WriteLine($"NAudio: Error disposing reader: {ex.Message}"); }
        _reader = null;

        
        IsPlaying = false; 
        _isPaused = false;
        InvokeOnMainThread(() => {
            if (!_isDisposed && _smtc != null) // Check again inside lambda
            {
                _smtc.PlaybackStatus = MediaPlaybackStatus.Closed;
                _smtc.IsPlayEnabled = false;
                _smtc.IsPauseEnabled = false;
                _smtc.IsNextEnabled = false; // Disable all when closed
                _smtc.IsPreviousEnabled = false;
                UpdateSmtcMetadata(null, null); // Ensure display is cleared
                                                // Don't call Update() here IF UpdateSmtcMetadata already does
            }
        });

        UpdatePlaybackProperties();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            
            CleanupResources();
        }

        
        _isDisposed = true;
        Debug.WriteLine("AudioService (NAudio) Disposed");
    }

    

    
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        InvokeOnMainThread(() =>
        {
            if (!_isDisposed) 
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        });
    }

    
    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;

        backingStore = value;
        
        OnPropertyChanged(propertyName);
        return true;
    }

    
    private void InvokeOnMainThread(Action action)
    {
        if (_isDisposed)
            return; 

        if (MainThread.IsMainThread)
        {
            action();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(action);
        }
    }

    public void SetCurrentMedia(SongModelView media)
    {
        CurrentMedia = media;
        throw new NotImplementedException();
    }
}