using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static Android.Provider.MediaStore.Audio;

namespace Dimmer.Views;
/// <summary>
/// The music player page.
/// </summary>
public partial class MusicPlayerPage : ContentPage
{
    // Example Bindable Properties
    /// <summary>
    /// The track title property.
    /// </summary>
    public static readonly BindableProperty TrackTitleProperty =
        BindableProperty.Create(nameof(TrackTitle), typeof(string), typeof(MusicPlayerPage), "No Track Playing");



    private void OnMauiButtonClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("MAUI XAML Button Clicked!");
        DisplayAlert("MAUI XAML", "Button inside XAML content was clicked!", "OK");
    }

    /// <summary>
    /// Gets or sets the track title.
    /// </summary>
    /// 

    public string TrackTitle
    {
        get => (string)GetValue(TrackTitleProperty);
        set => SetValue(TrackTitleProperty, value);
    }

    /// <summary>
    /// The artist name property.
    /// </summary>
    public static readonly BindableProperty ArtistNameProperty =
        BindableProperty.Create(nameof(ArtistName), typeof(string), typeof(MusicPlayerPage), "Unknown Artist");

    /// <summary>
    /// Gets or sets the artist name.
    /// </summary>
    public string ArtistName
    {
        get => (string)GetValue(ArtistNameProperty);
        set => SetValue(ArtistNameProperty, value);
    }

    /// <summary>
    /// The album art source property.
    /// </summary>
    public static readonly BindableProperty AlbumArtSourceProperty =
        BindableProperty.Create(nameof(AlbumArtSource), typeof(ImageSource), typeof(MusicPlayerPage), null);

    /// <summary>
    /// Gets or sets the album art source.
    /// </summary>
    public ImageSource AlbumArtSource
    {
        get => (ImageSource)GetValue(AlbumArtSourceProperty);
        set => SetValue(AlbumArtSourceProperty, value);
    }

    /// <summary>
    /// Is playing property.
    /// </summary>
    public static readonly BindableProperty IsPlayingProperty =
        BindableProperty.Create(nameof(IsPlaying), typeof(bool), typeof(MusicPlayerPage), false, propertyChanged: OnIsPlayingChanged);

    /// <summary>
    /// On is playing changed.
    /// </summary>
    /// <param name="bindable">The bindable.</param>
    /// <param name="oldValue">The old value.</param>
    /// <param name="newValue">The new value.</param>
    private static void OnIsPlayingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var page = (MusicPlayerPage)bindable;
        Debug.WriteLine($"MusicPlayerPage: IsPlaying changed to: {page.IsPlaying}");
        if (page.IsPlaying)
        {
            page.StartProgressSimulation();
        }
        else
        {
            page.StopProgressSimulation();
        }
    }
    // --- Commands ---
    /// <summary>
    /// Gets the play pause command.
    /// </summary>
    public ICommand PlayPauseCommand { get; }
    /// <summary>
    /// Gets the next track command.
    /// </summary>
    public ICommand NextTrackCommand { get; }
    /// <summary>
    /// Gets the previous track command.
    /// </summary>
    public ICommand PreviousTrackCommand { get; }
    /// <summary>
    /// Gets the seek command.
    /// </summary>
    public ICommand SeekCommand { get; } // Will take a double parameter (new progress from 0.0 to 1.0)
    /// <summary>
    /// Gets or sets a value indicating whether playing.
    /// </summary>
    public bool IsPlaying
    {
        get => (bool)GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }


    // --- Command Implementations ---
    /// <summary>
    /// Execute play pause.
    /// </summary>
    private void ExecutePlayPause()
    {
        IsPlaying = !IsPlaying;
        Debug.WriteLine($"MusicPlayerPage: PlayPauseCommand - IsPlaying is now {IsPlaying}");
    }

    /// <summary>
    /// Execute next track.
    /// </summary>
    private void ExecuteNextTrack()
    {
        _currentTrackIndex = (_currentTrackIndex + 1) % _playlist.Count;
        LoadTrack(_currentTrackIndex);
        Debug.WriteLine("MusicPlayerPage: NextTrackCommand executed");
    }

    /// <summary>
    /// Execute previous track.
    /// </summary>
    private void ExecutePreviousTrack()
    {
        _currentTrackIndex = (_currentTrackIndex - 1 + _playlist.Count) % _playlist.Count;
        LoadTrack(_currentTrackIndex);
        Debug.WriteLine("MusicPlayerPage: PreviousTrackCommand executed");
    }

    /// <summary>
    /// Execute the seek.
    /// </summary>
    /// <param name="newProgressFraction">The new progress fraction.</param>
    private void ExecuteSeek(double newProgressFraction) // newProgressFraction is 0.0 to 1.0
    {
        if (_simulatedDurationSeconds > 0)
        {
            CurrentProgress = Math.Clamp(newProgressFraction, 0.0, 1.0);
            _simulatedCurrentSeconds = _simulatedDurationSeconds * CurrentProgress;
            UpdateDisplayTimes(); // Update display after seek
            Debug.WriteLine($"MusicPlayerPage: SeekCommand - New progress {CurrentProgress * 100:F1}%");
        }
    }

    // --- Helper Methods & Track Simulation ---
    /// <summary>
    /// Load the track.
    /// </summary>
    /// <param name="trackIndex">The track index.</param>
    private void LoadTrack(int trackIndex)
    {
        if (trackIndex < 0 || trackIndex >= _playlist.Count)
            return;

        var track = _playlist[trackIndex];

        bool wasPlaying = IsPlaying; // Preserve playing state
        if (IsPlaying)
            IsPlaying = false; // Stop current playback simulation before loading new track

        TrackTitle = track.Title;
        ArtistName = track.Artist;
        AlbumArtSource = !string.IsNullOrEmpty(track.ImageName) ? ImageSource.FromFile(track.ImageName) : null;

        _simulatedDurationSeconds = track.Duration;
        _simulatedCurrentSeconds = 0; // Reset progress for new track
        CurrentProgress = 0.0;

        UpdateDisplayTimes();

        if (wasPlaying)
            IsPlaying = true; // Resume playback if it was playing

        Debug.WriteLine($"MusicPlayerPage: Loaded track '{TrackTitle}'");
    }

    /// <summary>
    /// Update display times.
    /// </summary>
    private void UpdateDisplayTimes()
    {
        DurationDisplay = FormatTimeSpan(TimeSpan.FromSeconds(_simulatedDurationSeconds));
        CurrentTimeDisplay = FormatTimeSpan(TimeSpan.FromSeconds(_simulatedCurrentSeconds));
    }

    /// <summary>
    /// Format time span.
    /// </summary>
    /// <param name="time">The time.</param>
    /// <returns>A string</returns>
    private static string FormatTimeSpan(TimeSpan time)
    {
        return $"{(int)time.TotalMinutes}:{time.Seconds:D2}";
    }
    // --- Private fields for simulation ---
    /// <summary>
    /// The progress timer.
    /// </summary>
    private Timer? _progressTimer;
    /// <summary>
    /// The simulated current seconds.
    /// </summary>
    private double _simulatedCurrentSeconds = 0;
    /// <summary>
    /// The simulated duration seconds.
    /// </summary>
    private double _simulatedDurationSeconds = 0;
    /// <summary>
    /// The current track index.
    /// </summary>
    private int _currentTrackIndex = 0;
    /// <summary>
    /// The playlist.
    /// </summary>
    private readonly List<(string Title, string Artist, string ImageName, int Duration)> _playlist = new()
        {
            ("MAUI Magic Anthem", "The .NET Band", "dotnet_bot.png", 220), // Duration in seconds
            ("Cross-Platform Groove", "The Compilers", "icon_maui.png", 185),
            ("Code Symphony No. 5", "Stack Overflow", "dotnet_bot.png", 300),
            ("Pixel Perfect Polka", "UI Masters", null, 150) // Example with no image
        };
    /// <summary>
    /// Start progress simulation.
    /// </summary>
    private void StartProgressSimulation()
    {
        StopProgressSimulation(); // Ensure any existing timer is stopped

        if (_simulatedDurationSeconds <= 0)
            return; // No duration, nothing to play

        // If current progress is at or beyond the end, handle track end (e.g., go to next)
        if (_simulatedCurrentSeconds >= _simulatedDurationSeconds)
        {
            // Optionally auto-play next track or stop
            // For now, just reset to beginning of current track if manually started at end
            _simulatedCurrentSeconds = 0;
            CurrentProgress = 0;
            UpdateDisplayTimes();
            // ExecuteNextTrack(); // Uncomment to auto-play next
            // return;
        }

        _progressTimer = new Timer(TimerCallback, null, TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(1));
        Debug.WriteLine("MusicPlayerPage: Progress simulation started.");
    }

    /// <summary>
    /// Stop progress simulation.
    /// </summary>
    private void StopProgressSimulation()
    {
        _progressTimer?.Dispose();
        _progressTimer = null;
        Debug.WriteLine("MusicPlayerPage: Progress simulation stopped.");
    }

    /// <summary>
    /// Timers the callback.
    /// </summary>
    /// <param name="state">The state.</param>
    private void TimerCallback(object state)
    {
        if (!IsPlaying || _simulatedDurationSeconds <= 0)
        {
            MainThread.BeginInvokeOnMainThread(StopProgressSimulation);
            return;
        }

        _simulatedCurrentSeconds++;

        if (_simulatedCurrentSeconds >= _simulatedDurationSeconds)
        {
            _simulatedCurrentSeconds = _simulatedDurationSeconds; // Cap at duration

            // UI updates must be on the main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CurrentProgress = 1.0;
                UpdateDisplayTimes();
                IsPlaying = false; // Auto-pause at end
                Debug.WriteLine("MusicPlayerPage: Track finished simulation.");
                StopProgressSimulation();
                // Optionally: ExecuteNextTrack(); // Auto-play next track
            });
        }
        else
        {
            // UI updates must be on the main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CurrentProgress = _simulatedCurrentSeconds / _simulatedDurationSeconds;
                UpdateDisplayTimes();
            });
        }
    }



    /// <summary>
    /// The current progress property.
    /// </summary>
    public static readonly BindableProperty CurrentProgressProperty =
        BindableProperty.Create(nameof(CurrentProgress), typeof(double), typeof(MusicPlayerPage), 0.0,
                                validateValue: (_, value) => (double)value >= 0.0 && (double)value <= 1.0);

    /// <summary>
    /// Gets or sets the current progress.
    /// </summary>
    public double CurrentProgress // Represents progress from 0.0 to 1.0
    {
        get => (double)GetValue(CurrentProgressProperty);
        set => SetValue(CurrentProgressProperty, value);
    }

    /// <summary>
    /// The duration display property.
    /// </summary>
    public static readonly BindableProperty DurationDisplayProperty =
        BindableProperty.Create(nameof(DurationDisplay), typeof(string), typeof(MusicPlayerPage), "0:00");

    /// <summary>
    /// Gets or sets the duration display.
    /// </summary>
    public string DurationDisplay
    {
        get => (string)GetValue(DurationDisplayProperty);
        set => SetValue(DurationDisplayProperty, value);
    }

    /// <summary>
    /// The current time display property.
    /// </summary>
    public static readonly BindableProperty CurrentTimeDisplayProperty =
        BindableProperty.Create(nameof(CurrentTimeDisplay), typeof(string), typeof(MusicPlayerPage), "0:00");

    /// <summary>
    /// Gets or sets the current time display.
    /// </summary>
    public string CurrentTimeDisplay
    {
        get => (string)GetValue(CurrentTimeDisplayProperty);
        set => SetValue(CurrentTimeDisplayProperty, value);
    }

    // Add other properties for artwork, play/pause state, etc.
    // Add Commands for play, pause, next, prev actions
    /// <summary>
    /// Test the method.
    /// </summary>
    public static void TestMethod()
    {
        // This is just a placeholder for any test method you might want to implement.
        // You can call this method from your native view or from the MAUI page.
        Console.WriteLine("Test method called!");
    }


    /// <summary>
    /// On appearing.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        // If you want it to start playing (or resume) when page appears:
        // if (IsPlaying) StartProgressSimulation();
    }


    /// <summary>
    /// On disappearing.
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Decide if you want to stop simulation when page disappears
        // StopProgressSimulation(); // Uncomment if music should only play when page is visible
        Debug.WriteLine("MusicPlayerPage: OnDisappearing. Player state remains for background play simulation.");
    }

    // --- Constructor & Initialization ---
    /// <summary>
    /// Initializes a new instance of the <see cref="MusicPlayerPage"/> class.
    /// </summary>
    public MusicPlayerPage()
    {
        InitializeComponent();
        // Initialize Commands
        PlayPauseCommand = new Command(ExecutePlayPause);
        NextTrackCommand = new Command(ExecuteNextTrack);
        PreviousTrackCommand = new Command(ExecutePreviousTrack);
        SeekCommand = new Command<double>(ExecuteSeek);

        // Load the first track
        LoadTrack(_currentTrackIndex);

        // Title for the page itself (optional)
        // Title = "Native Music Player";
    }

}