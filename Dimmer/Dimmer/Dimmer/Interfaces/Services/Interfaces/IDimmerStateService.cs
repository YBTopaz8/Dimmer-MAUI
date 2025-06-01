// Assuming Dimmer.Data.Models and Dimmer.Utilities.Enums are accessible
// using Dimmer.Platform; // For Window if it's a specific type


namespace Dimmer.Interfaces.Services.Interfaces;

public interface IDimmerStateService : IDisposable
{
    // --- Observables for Core Playback State ---
    IObservable<SongModelView?> CurrentSong { get; }
    IObservable<PlaybackStateInfo> CurrentPlayBackState { get; }
    IObservable<bool> IsPlaying { get; } // Derived from CurrentPlayBackState
    IObservable<PlaylistModel?> CurrentPlaylist { get; } // The active PlaylistModel context
    IObservable<bool> IsShuffleActive { get; }
    IObservable<RepeatMode> CurrentRepeatMode { get; } // Added for repeat mode state
    IObservable<TimeSpan> CurrentSongPosition { get; } // Current playback position
    IObservable<TimeSpan> CurrentSongDuration { get; } // Duration of the current song

    // --- Observables for Library & App Context ---
    IObservable<IReadOnlyList<SongModel>> AllCurrentSongs { get; } // Represents the whole library
    IObservable<UserModelView?> CurrentUser { get; } // Current logged-in user
    IObservable<AppStateModelView?> ApplicationSettingsState { get; } // General app settings/preferences
    IObservable<CurrentPage> CurrentPage { get; }
    IObservable<IReadOnlyList<Window>> CurrentlyOpenWindows { get; }

    // --- Observables for UI/Feedback ---
    IObservable<AppLogModel> LatestDeviceLog { get; }
    IObservable<IReadOnlyList<AppLogModel>> DailyLatestDeviceLogs { get; }
    IObservable<LyricPhraseModel?> CurrentLyric { get; }
    IObservable<IReadOnlyList<LyricPhraseModel>> SyncLyrics { get; }
    IObservable<double> DeviceVolume { get; } // Volume (0.0 to 1.0)
    IObservable<IReadOnlyList<DimmerPlayEvent>> AllPlayHistory { get; }
    ReadOnlyCollection<SongModel> AllCurrentSongsInDB { get; }

    // --- Methods to Update State ---
    void LoadAllSongs(IEnumerable<SongModel> songs);
    void SetCurrentSong(SongModel? song);
    void SetCurrentState(PlaybackStateInfo state);
    void SetCurrentPlaylist(PlaylistModel? playlist);
    void SetShuffleActive(bool isShuffleOn);
    void SetRepeatMode(RepeatMode repeatMode); // Added
    void SetCurrentSongPosition(TimeSpan position); // Added
    void SetCurrentSongDuration(TimeSpan duration); // Added
    void SetCurrentUser(UserModelView? user); // Added
    void SetApplicationSettingsState(AppStateModelView? appState); // Added

    void SetCurrentLogMsg(AppLogModel logMessage);
    void SetDeviceVolume(double volume);
    void AddWindow(Window window);
    void RemoveWindow(Window window);
    void SetCurrentPage(CurrentPage page);
    void SetSyncLyrics(IEnumerable<LyricPhraseModel>? lyrics);
    void SetCurrentLyric(LyricPhraseModel? lyric);
    void LoadAllPlayHistory(IEnumerable<DimmerPlayEvent> events);

    // Removed:
    // - IObservable<SongModel?> SecondSelectedSong
    // - void SetSecondSelectdSong(SongModel? song)
    // - SongModelView? CurrentSongValue (consumers subscribe or use FirstAsync().Wait() on CurrentSong)
}


