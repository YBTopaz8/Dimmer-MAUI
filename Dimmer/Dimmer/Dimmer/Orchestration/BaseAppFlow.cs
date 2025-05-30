using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Dimmer.Interfaces;
using Dimmer.Interfaces.Services;
using Dimmer.Interfaces.IServices;
using Dimmer.Data.Models;
using Dimmer.Utilities.Enums;
using static Dimmer.Utilities.AppUtils;
using Dimmer.Utilities.Extensions;

namespace Dimmer.Orchestration;

public class BaseAppFlow : IDisposable
{

    protected readonly IDimmerStateService _state;
    protected readonly IMapper _mapper;
    private readonly IRepository<DimmerPlayEvent> _playEventRepo;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<BaseAppFlow> _logger;


    private readonly IRepository<UserModel> _userRepo;
    private readonly IRepository<PlaylistModel> _playlistRepo;
    private readonly IRepository<ArtistModel> _artistRepo;
    private readonly IRepository<AlbumModel> _albumRepo;
    private readonly IRepository<SongModel> _songRepo;
    private readonly IRepository<AppStateModel> _appStateRepo;


    private readonly IFolderMgtService _folderManagementService;

    private readonly ILibraryScannerService _libraryScannerService;


    private readonly CompositeDisposable _subscriptions = new();
    private bool _disposed;


    public SongModelView? CurrentSongSnapshot { get; private set; }
    public UserModel? CurrentUserInstance { get; private set; }
    public AppStateModelView? AppStateSnapshot { get; private set; }


    public BaseAppFlow(
        IDimmerStateService state,
        IMapper mapper,
        IRepository<DimmerPlayEvent> playEventRepo,
        IRepository<UserModel> userRepo,
        IRepository<PlaylistModel> playlistRepo,
        IRepository<ArtistModel> artistRepo,
        IRepository<AlbumModel> albumRepo,
        IRepository<SongModel> songRepo,
        IRepository<AppStateModel> appStateRepo,
        ISettingsService settingsService,
        IFolderMgtService folderManagementService,
        ILibraryScannerService libraryScannerService,
        SubscriptionManager inheritedSubs,
        ILogger<BaseAppFlow> logger)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _playEventRepo = playEventRepo ?? throw new ArgumentNullException(nameof(playEventRepo));
        _userRepo = userRepo;
        _playlistRepo = playlistRepo;
        _artistRepo = artistRepo;
        _albumRepo = albumRepo;
        _songRepo = songRepo;
        _appStateRepo = appStateRepo;
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _folderManagementService = folderManagementService ?? throw new ArgumentNullException(nameof(folderManagementService));
        _libraryScannerService = libraryScannerService ?? throw new ArgumentNullException(nameof(libraryScannerService));
        _logger = logger ?? NullLogger<BaseAppFlow>.Instance;
        // Subscribe to state for local snapshots if needed and for logging.
        _subscriptions.Add(
           _state.CurrentSong
               .Subscribe(songView =>
               {
                   CurrentSongSnapshot = songView;
                   _logger.LogTrace("BaseAppFlow: CurrentSongSnapshot updated to: {SongTitle}", CurrentSongSnapshot?.Title ?? "None");
               }, ex => _logger.LogError(ex, "Error subscribing to CurrentSong for snapshot."))
        );
        _subscriptions.Add(
           _state.CurrentUser
               .Subscribe(userView =>
               {
                   CurrentUserInstance = userView.ToModel(_mapper); // Assuming ToModel extension exists
                   _logger.LogTrace("BaseAppFlow: CurrentUserInstance updated to: {UserName}", CurrentUserInstance?.UserName ?? "None");
               }, ex => _logger.LogError(ex, "Error subscribing to CurrentUser for snapshot."))
        );
        _subscriptions.Add(
          _state.ApplicationSettingsState
              .Subscribe(appStateView =>
              {
                  AppStateSnapshot = appStateView;
                  _logger.LogTrace("BaseAppFlow: AppStateSnapshot updated.");
              }, ex => _logger.LogError(ex, "Error subscribing to ApplicationSettingsState for snapshot."))
       );


        InitializePlaybackEventLogging();
        InitializeFolderEventReactions(); // New method to handle reactions to folder events from state

        _logger.LogInformation("BaseAppFlow (Coordinator & Logger) initialized.");
    }

    // --- Playback Event Logging to Database (Core Responsibility) ---
    private PlaybackStateInfo? _previousPlaybackStateForLogging;

    private void InitializePlaybackEventLogging()
    {
        _subscriptions.Add(
            _state.CurrentPlayBackState
                // .ObserveOn(Scheduler.Default) // Consider if DB ops are blocking
                .Subscribe(
                    currentPsi => LogPlaybackTransition(currentPsi),
                    ex => _logger.LogError(ex, "Error in CurrentPlayBackState logging subscription.")
                )
        );
        _logger.LogInformation("BaseAppFlow: Playback event logging initialized.");
    }

    private void LogPlaybackTransition(PlaybackStateInfo currentPsi)
    {
        var previousPsi = _previousPlaybackStateForLogging;
        _previousPlaybackStateForLogging = currentPsi;

        SongModelView? songForEvent = currentPsi.SongView ?? _mapper.Map<SongModelView>(currentPsi.Songdb);
        if (songForEvent == null && (currentPsi.State != DimmerPlaybackState.Stopped && currentPsi.State != DimmerPlaybackState.Opening))
        {
            songForEvent = CurrentSongSnapshot; // Fallback to local snapshot
        }

        if (previousPsi == null)
        {
            if (currentPsi.State == DimmerPlaybackState.Playing && songForEvent != null)
            {
                UpdateDatabaseWithPlayEvent(songForEvent, PlayType.Play, currentPsi.ContextSongPositionSeconds);
            }
            return;
        }

        if (songForEvent == null && currentPsi.State < DimmerPlaybackState.FolderAdded) // Only log playback types if song exists
        {
            _logger.LogDebug("LogPlaybackTransition: No song context for playback state {CurrentState} from {PreviousState}. Skipping log.", currentPsi.State, previousPsi.State);
            return;
        }

        PlayType? playTypeToLog = DeterminePlayType(previousPsi, currentPsi, songForEvent);
        double? positionForLog = (playTypeToLog == PlayType.Seeked || playTypeToLog == PlayType.Pause) ? currentPsi.ContextSongPositionSeconds : null;

        if (playTypeToLog.HasValue)
        {
            SongModelView? songToLogWithPlayType = songForEvent;
            if (playTypeToLog == PlayType.Skipped || playTypeToLog == PlayType.Completed)
            {
                songToLogWithPlayType = previousPsi.SongView ?? _mapper.Map<SongModelView>(previousPsi.Songdb);
                positionForLog = previousPsi.ContextSongPositionSeconds;
            }

            if (songToLogWithPlayType != null || playTypeToLog >= PlayType.LogEvent)
            {
                UpdateDatabaseWithPlayEvent(songToLogWithPlayType, playTypeToLog.Value, positionForLog, currentPsi.ExtraParameter as string);
            }
            else
            {
                _logger.LogWarning("Could not determine song context for PlayType {PlayType}. Prev: {PState} ({PSong}), Curr: {CState} ({CSong})",
                   playTypeToLog, previousPsi.State, previousPsi.SongView?.Title ?? "N/A", currentPsi.State, songForEvent?.Title ?? "N/A");
            }
        }
    }
    private PlayType? DeterminePlayType(PlaybackStateInfo prev, PlaybackStateInfo curr, SongModelView? songForCurrPsi)
    {
        var prevSongId = prev.SongView?.Id ?? prev.Songdb?.Id;
        var currSongId = songForCurrPsi?.Id;

        // --- Playing / Resuming ---
        if (curr.State == DimmerPlaybackState.Playing || curr.State == DimmerPlaybackState.Resumed)
        {
            if ((prev.State == DimmerPlaybackState.PausedUI || prev.State == DimmerPlaybackState.PausedUser) && prevSongId == currSongId)
                return PlayType.Resume;
            if (prev.State != DimmerPlaybackState.Playing || prevSongId != currSongId) // Started new song, or from non-playing state
                return PlayType.Play;
        }
        // --- Paused ---
        else if (curr.State == DimmerPlaybackState.PausedUI || curr.State == DimmerPlaybackState.PausedUser)
        {
            if ((prev.State == DimmerPlaybackState.Playing || prev.State == DimmerPlaybackState.Resumed) && prevSongId == currSongId)
                return PlayType.Pause;
        }
        // --- Ended (Naturally) ---
        else if (curr.State == DimmerPlaybackState.Ended)
        {
            // 'Ended' state is set by SongsMgtFlow with the song that just ended.
            // So, we check if previous state was playing *that same song*.
            if ((prev.State == DimmerPlaybackState.Playing || prev.State == DimmerPlaybackState.Resumed) && prevSongId == currSongId) // ensure song context matches what ended
                return PlayType.Completed;
        }

        bool isPrevPlayingOrResumed = prev.State == DimmerPlaybackState.Playing || prev.State == DimmerPlaybackState.Resumed;
        bool isCurrNextCommand = curr.State == DimmerPlaybackState.PlayNextUI || curr.State == DimmerPlaybackState.PlayNextUser;
        bool isCurrPrevCommand = curr.State == DimmerPlaybackState.PlayPreviousUI || curr.State == DimmerPlaybackState.PlayPreviousUser;

        // --- Skipped (due to Next/Previous command causing a *different* song to be cued) ---
        if (isPrevPlayingOrResumed && (isCurrNextCommand || isCurrPrevCommand))
        {
            // A skip implies the song changed or is about to change due to user action.
            // The new song will subsequently get a Play/Previous event.
            // We log 'Skipped' for the song that *was* playing.
            if (prevSongId != null) // Ensure there was a song to be skipped
            {
                // If the command also provides the *next* song context and it's different, it's definitely a skip.
                var songInCommandContext = curr.SongView ?? _mapper.Map<SongModelView>(curr.Songdb);
                if (songInCommandContext == null || songInCommandContext.Id != prevSongId)
                {
                    return PlayType.Skipped;
                }
            }
        }

        // --- Previous (Song that *starts* as a result of a previous command) ---
        if ((curr.State == DimmerPlaybackState.Playing || curr.State == DimmerPlaybackState.Resumed) &&
            isCurrPrevCommand &&
            prevSongId != currSongId && // Ensure it's a different song that started
            currSongId != null)
        {
            return PlayType.Previous;
        }
        // --- Restarted ---
        if (prev.State == DimmerPlaybackState.Playing &&
            (curr.State == DimmerPlaybackState.Playing || curr.State == DimmerPlaybackState.Resumed) &&
            prevSongId == currSongId &&
            // Corrected boolean logic for nullable doubles:
            curr.ContextSongPositionSeconds.HasValue && curr.ContextSongPositionSeconds.Value < 3.0 && // CORRECTED
            prev.ContextSongPositionSeconds.HasValue && prev.ContextSongPositionSeconds.Value >= 3.0)  // CORRECTED
        {
            return PlayType.Restarted;
        }
        // --- Log specific events passed through PlaybackStateInfo.ExtraParameter ---
        if (curr.ExtraParameter is PlayType explicitPlayType && explicitPlayType >= PlayType.LogEvent)
        {
            return explicitPlayType;
        }

        _logger.LogTrace("No specific PlayType determined for transition from {PreviousState} (Song: {PreviousSong}) to {CurrentState} (Song: {CurrentSong})",
            prev.State, prev.SongView?.Title ?? prev.Songdb?.Title ?? "N/A",
            curr.State, songForCurrPsi?.Title ?? "N/A");
        return null;
    }

    // --- Reactions to Folder Management Events from State (Delegates to Services) ---
    private void InitializeFolderEventReactions()
    {
        _subscriptions.Add(
            _state.CurrentPlayBackState
                .Where(psi => psi.State == DimmerPlaybackState.FolderAdded && psi.ExtraParameter is string)
                .Select(psi => psi.ExtraParameter as string)
                .Subscribe(async folderPath =>
                {
                    if (folderPath == null)
                        return;
                    _logger.LogInformation("BaseAppFlow: Detected FolderAdded state for {Path}. Triggering folder preference update and scan.", folderPath);
                    // This path should already be added to settings by FolderMgtService, which then set this state.
                    // BaseAppFlow's role here is to ensure LibraryScannerService processes it.
                    // However, FolderMgtService should ideally call LibraryScannerService directly.
                    // For now, let's assume FolderMgtService sets the state, and BaseAppFlow reacts if necessary.
                    // This specific reaction might be redundant if FolderMgtService calls scanner.
                    // If FolderMgtService *only* updates settings & state, then this is needed:
                    // _folderManagementService.AddFolderToWatch(folderPath); // If not already done
                    await _libraryScannerService.ScanSpecificPathsAsync(new List<string> { folderPath }, isIncremental: false);
                }, ex => _logger.LogError(ex, "Error processing FolderAdded state."))
        );

        _subscriptions.Add(
            _state.CurrentPlayBackState
                .Where(psi => psi.State == DimmerPlaybackState.FolderRemoved && psi.ExtraParameter is string)
                .Subscribe(async folderPath => // async void is okay for top-level event handlers if errors are logged
                {
                    if (folderPath == null)
                        return;
                    _logger.LogInformation("BaseAppFlow: Detected FolderRemoved state for {Path}. Ensuring folder is removed from watch and considering rescan.", folderPath);
                    // _folderManagementService.RemoveFolderFromWatch(folderPath); // If not already done by the originator
                    // After removing a folder, you might want to trigger a scan to remove its songs from the library.
                    // This is complex as it requires identifying songs *only* from that folder.
                    // A full rescan of remaining folders is safer but less efficient.
                    // For now, just log; actual song removal from DB is a bigger feature.
                    await _libraryScannerService.ScanLibraryAsync(_settingsService.UserMusicFoldersPreference?.ToList() ?? new List<string>()); // Re-scan all configured folders
                }, ex => _logger.LogError(ex, "Error processing FolderRemoved state."))
        );
    }


    // --- Public Methods for Explicit Logging (Called by ViewModels or other services) ---
    public void LogSeekEvent(SongModelView? song, double seekedToPositionSeconds)
    {
        if (song == null)
        { _logger.LogWarning("LogSeekEvent called with null song."); return; }
        UpdateDatabaseWithPlayEvent(song, PlayType.Seeked, seekedToPositionSeconds);
        _logger.LogInformation("Seek event logged for {SongTitle} to {Position}s", song.Title, seekedToPositionSeconds);
    }

    public void LogApplicationEvent(PlayType playType, SongModelView? songContext = null, string? eventDetails = null, double? position = null)
    {
        if (playType < PlayType.LogEvent && songContext == null)
        {
            _logger.LogWarning("LogApplicationEvent: Playback-related PlayType {PlayType} called without songContext.", playType);
            // return; // Or allow if certain playback types can be context-less.
        }
        if (playType < PlayType.LogEvent && songContext != null)
        { // If it's a playback type, it should be handled by state transitions.
            _logger.LogDebug("LogApplicationEvent: Playback-related PlayType {PlayType} with song {SongTitle} logged explicitly. This is usually handled by state transitions.", playType, songContext.Title);
        }

        UpdateDatabaseWithPlayEvent(songContext, playType, position, eventDetails);
        // _logger.LogInformation("Application event {PlayType} logged. Details: {Details}", playType, eventDetails ?? "N/A"); // UpdateDatabaseWithPlayEvent already logs
    }

    private void UpdateDatabaseWithPlayEvent(SongModelView? songView, PlayType type, double? position = null, string? eventDetails = null)
    {
        if (songView == null && type < PlayType.LogEvent)
        {
            _logger.LogWarning("UpdateDatabaseWithPlayEvent: SongView is null for playback PlayType {PlayType}.", type);
            return;
        }

        SongModel? songDb = null;
        if (songView != null)
        {
            songDb = _mapper.Map<SongModel>(songView);
            if (songDb == null && type < PlayType.LogEvent)
            {
                _logger.LogError("UpdateDatabaseWithPlayEvent: Failed to map SongModelView to SongModel for PlayType {PlayType} with SongId {SongId}.", type, songView.Id);
                return;
            }
        }

        var playEvent = new DimmerPlayEvent
        {
            Id = ObjectId.GenerateNewId(),
            SongId = songDb?.Id,
            Song = songDb, // Store direct reference if your DB schema supports it
            PlayType = (int)type,
            PlayTypeStr = type.ToString(),
            EventDate = DateTime.UtcNow,

            DatePlayed = DateTimeOffset.UtcNow,
            PositionInSeconds = position ?? ((type == PlayType.Completed && songDb?.DurationInSeconds > 0) ? songDb.DurationInSeconds : 0),
            WasPlayCompleted = type == PlayType.Completed,
        };

        try
        {
            _playEventRepo.AddOrUpdate(playEvent);
            _logger.LogInformation("Logged DimmerPlayEvent: Type={PlayType}, SongId={SongId}, Pos={Pos}, Details={Details}",
                type, playEvent.SongId?.ToString() ?? "N/A", playEvent.PositionInSeconds, eventDetails ?? "N/A");

            // The log message to state service should be more generic or also triggered by the logger itself.
            string userFriendlyMessage = UserFriendlyLogGenerator.GetPlaybackStateMessage(type, songView, position);
            _state.SetCurrentLogMsg(new AppLogModel { Log = userFriendlyMessage, ViewSongModel = songView, AppSongModel = songDb });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save DimmerPlayEvent. Type: {PlayType}, SongId: {SongId}", type, playEvent.SongId?.ToString() ?? "N/A");
        }
    }


    // --- Facade Methods for Settings (Interacting with IDimmerStateService and ISettingsService) ---
    public void ToggleShuffle(bool isOn)
    {
        _logger.LogDebug("ToggleShuffle called with: {IsOn}", isOn);
        _settingsService.ShuffleOn = isOn; // Persist setting
        _state.SetShuffleActive(isOn);      // Update global reactive state
    }

    public void ToggleRepeatMode() // Now cycles through RepeatMode enum
    {
        var currentMode = _settingsService.RepeatMode;
        var enumValues = Enum.GetValues(typeof(RepeatMode)).Cast<RepeatMode>().ToList();
        int currentIndex = enumValues.IndexOf(currentMode);
        RepeatMode nextMode = enumValues[(currentIndex + 1) % enumValues.Count];

        _logger.LogDebug("ToggleRepeatMode: From {CurrentMode} to {NextMode}", currentMode, nextMode);
        _settingsService.RepeatMode = nextMode; // Persist setting
        _state.SetRepeatMode(nextMode);         // Update global reactive state
    }

    // --- Facade Methods for Data CUD (Consider moving to dedicated Data Services) ---
    // These methods now operate on instance data or use repositories directly.
    // They log an application event.

    public async Task<UserModel> UpsertUserAsync(UserModel user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));
        var updatedUser = _userRepo.AddOrUpdate(user);
        LogApplicationEvent(PlayType.LogEvent, eventDetails: $"User upserted: {user.UserName ?? user.Id.ToString()}");
        if (CurrentUserInstance?.Id == updatedUser.Id || CurrentUserInstance == null)
        {
            CurrentUserInstance = updatedUser; // Update local snapshot
            _state.SetCurrentUser(_mapper.Map<UserModelView>(updatedUser)); // Update global state
        }
        return updatedUser;
    }

    public PlaylistModel UpsertPlaylist(PlaylistModel playlist)
    {
        if (playlist == null)
            throw new ArgumentNullException(nameof(playlist));
        var updatedPlaylist = _playlistRepo.AddOrUpdate(playlist);
        LogApplicationEvent(PlayType.LogEvent, eventDetails: $"Playlist upserted: {playlist.PlaylistName}");
        return updatedPlaylist;
    }

    public ArtistModel UpsertArtist(ArtistModel artist)
    {
        if (artist == null)
            throw new ArgumentNullException(nameof(artist));
        var updatedArtist = _artistRepo.AddOrUpdate(artist);
        LogApplicationEvent(PlayType.LogEvent, eventDetails: $"Artist upserted: {artist.Name}");
        return updatedArtist;
    }

    public AlbumModel UpsertAlbum(AlbumModel album)
    {
        if (album == null)
            throw new ArgumentNullException(nameof(album));
        var updatedAlbum = _albumRepo.AddOrUpdate(album);
        LogApplicationEvent(PlayType.LogEvent, eventDetails: $"Album upserted: {album.Name}");
        return updatedAlbum;
    }

    public SongModel UpsertSong(SongModel song)
    {
        if (song == null)
            throw new ArgumentNullException(nameof(song));
        var updatedSong = _songRepo.AddOrUpdate(song);
        LogApplicationEvent(PlayType.LogEvent, _mapper.Map<SongModelView>(updatedSong), eventDetails: $"Song upserted: {song.Title}");
        return updatedSong;
    }

    public async Task UpsertSongNoteAsync(ObjectId songId, UserNoteModel note)
    {
        if (note == null)
            throw new ArgumentNullException(nameof(note));
        // BatchUpdate might not be async in your IRepository.
        // If it's synchronous, wrap in Task.Run for an async signature if needed,
        // but be careful with Realm instances across threads if not using IRealmFactory inside.
        // Assuming BatchUpdate is synchronous for now, or you adapt IRepository.
        await Task.Run(() => _songRepo.BatchUpdate(realm =>
        {
            var song = realm.Find<SongModel>(songId);
            if (song != null)
            {
                // If UserNotes is IList, find and update or add.
                var existingNote = song.UserNotes.FirstOrDefault(un => un.Id == note.Id);
                if (existingNote != null)
                {
                    // Update existingNote properties from note
                    // This requires UserNoteModel to be an EmbeddedObject and for its properties to be settable.
                    // If it's just adding, and Id ensures uniqueness or you handle replacement:
                    // song.UserNotes.Remove(existingNote);
                    // song.UserNotes.Add(note);
                    // For simplicity, let's assume we are adding or replacing if ID matches (Realm Embedded might not allow PK update on embedded)
                    // A safer way for embedded is to remove and re-add if updating complex embedded objects.
                    // For simple add:
                    if (existingNote == null)
                        song.UserNotes.Add(note);
                    else
                    { /* update logic for existingNote */ }

                }
                else
                {
                    song.UserNotes.Add(note);
                }
            }
            else
            {
                _logger.LogWarning("UpsertSongNoteAsync: Song with Id {SongId} not found.", songId);
            }
        }));
        // Log after the write completes
        var songView = _mapper.Map<SongModelView>(_songRepo.GetById(songId)); // Fetch for logging context
        LogApplicationEvent(PlayType.LogEvent, songView, $"Note upserted for song ID: {songId}");
    }


    // --- Methods from old BaseAppFlow that are now largely delegated or removed ---
    // Initialize(): Replaced by IAppInitializerService, called externally.
    // InitAllMasterLists(): Part of IAppInitializerService's responsibility (loading into _state).
    // LoadUser(): Part of IAppInitializerService.
    // SubscribeToStateChanges() for folder events: Now handled by InitializeFolderEventReactions,
    //                                             which reacts to state set by IFolderMgtService.
    // AddFolderToPath(): Should be part of IFolderMgtService, which updates ISettingsService.
    // LoadSongs(): Moved to ILibraryScannerService.

    // --- Disposal ---
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;
        if (disposing)
        {
            _logger.LogInformation("BaseAppFlow (Coordinator & Logger) disposing.");
            _subscriptions.Dispose();
            // Injected services (_state, _mapper, repositories, etc.) are typically managed
            // by the DI container, so this class shouldn't dispose them unless it created them.
        }
        _disposed = true;
    }
}