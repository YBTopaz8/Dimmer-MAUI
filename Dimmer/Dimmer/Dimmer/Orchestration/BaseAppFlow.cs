using System.Reactive.Disposables;

using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.Utilities.Events;
using Dimmer.Utilities.Extensions;

using Microsoft.Extensions.Logging.Abstractions;

using static Dimmer.Utilities.AppUtils;

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
    private readonly IDimmerAudioService audioService;


    public BaseAppFlow(
        IDimmerStateService state,
        IMapper mapper,
       IDimmerAudioService _audioService,
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
        audioService= _audioService   ?? throw new ArgumentNullException(nameof(audioService));
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
                   CurrentUserInstance = userView.ToModel(_mapper);
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

        _subscriptions.Add(
        Observable.FromEventPattern<PlaybackEventArgs>(h => audioService.PlayEnded += h, h => audioService.PlayEnded -= h)
            .Subscribe(evt =>
            {
                UpdateDatabaseWithPlayEvent(evt.EventArgs.MediaSong, PlayType.Completed, 0);
            },
                       ex => _logger.LogError(ex, "Error in IsPlayingChanged subscription."))
    );



        InitializeFolderEventReactions();

        _logger.LogInformation("BaseAppFlow (Coordinator & Logger) initialized.");
    }


    private PlaybackStateInfo? _previousPlaybackStateForLogging;



    private void LogPlaybackTransition(PlaybackStateInfo currentPsi)
    {
        var previousPsi = _previousPlaybackStateForLogging;
        _previousPlaybackStateForLogging = currentPsi;

        SongModelView? songForEvent = currentPsi.SongView ?? _mapper.Map<SongModelView>(currentPsi.Songdb);
        if (songForEvent == null && (currentPsi.State != DimmerPlaybackState.PlayCompleted && currentPsi.State != DimmerPlaybackState.Opening))
        {
            songForEvent = CurrentSongSnapshot;
        }

        if (previousPsi == null)
        {
            if (currentPsi.State == DimmerPlaybackState.Playing && songForEvent != null)
            {
                //UpdateDatabaseWithPlayEvent(songForEvent, PlayType.Play, currentPsi.ContextSongPositionSeconds);
            }
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
                //UpdateDatabaseWithPlayEvent(songToLogWithPlayType, playTypeToLog.Value, positionForLog);
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


        if (curr.State == DimmerPlaybackState.Playing || curr.State == DimmerPlaybackState.Resumed)
        {
            if ((prev.State == DimmerPlaybackState.PausedDimmer || prev.State == DimmerPlaybackState.PausedUser) && prevSongId == currSongId)
                return PlayType.Resume;
            if (prev.State != DimmerPlaybackState.Playing || prevSongId != currSongId)
                return PlayType.Play;
        }

        else if (curr.State == DimmerPlaybackState.PausedDimmer || curr.State == DimmerPlaybackState.PausedUser)
        {
            if ((prev.State == DimmerPlaybackState.Playing || prev.State == DimmerPlaybackState.Resumed) && prevSongId == currSongId)
                return PlayType.Pause;
        }

        else if (curr.State == DimmerPlaybackState.PlayCompleted)
        {


            if ((prev.State == DimmerPlaybackState.Playing || prev.State == DimmerPlaybackState.Resumed) && prevSongId == currSongId)
                return PlayType.Completed;
        }

        bool isPrevPlayingOrResumed = prev.State == DimmerPlaybackState.Playing || prev.State == DimmerPlaybackState.Resumed;
        bool isCurrNextCommand = curr.State == DimmerPlaybackState.PlayNextUI || curr.State == DimmerPlaybackState.PlayNextUser;
        bool isCurrPrevCommand = curr.State == DimmerPlaybackState.PlayPreviousUI || curr.State == DimmerPlaybackState.PlayPreviousUser;


        if (isPrevPlayingOrResumed && (isCurrNextCommand || isCurrPrevCommand))
        {



            if (prevSongId != null)
            {

                var songInCommandContext = curr.SongView ?? _mapper.Map<SongModelView>(curr.Songdb);
                if (songInCommandContext == null || songInCommandContext.Id != prevSongId)
                {
                    return PlayType.Skipped;
                }
            }
        }


        if ((curr.State == DimmerPlaybackState.Playing || curr.State == DimmerPlaybackState.Resumed) &&
            isCurrPrevCommand &&
            prevSongId != currSongId &&
            currSongId != null)
        {
            return PlayType.Previous;
        }

        if (prev.State == DimmerPlaybackState.Playing &&
            (curr.State == DimmerPlaybackState.Playing || curr.State == DimmerPlaybackState.Resumed) &&
            prevSongId == currSongId &&

            curr.ContextSongPositionSeconds.HasValue && curr.ContextSongPositionSeconds.Value < 3.0 &&
            prev.ContextSongPositionSeconds.HasValue && prev.ContextSongPositionSeconds.Value >= 3.0)
        {
            return PlayType.Restarted;
        }

        if (curr.ExtraParameter is PlayType explicitPlayType && explicitPlayType >= PlayType.LogEvent)
        {
            return explicitPlayType;
        }

        _logger.LogTrace("No specific PlayType determined for transition from {PreviousState} (Song: {PreviousSong}) to {CurrentState} (Song: {CurrentSong})",
            prev.State, prev.SongView?.Title ?? prev.Songdb?.Title ?? "N/A",
            curr.State, songForCurrPsi?.Title ?? "N/A");
        return null;
    }


    private void InitializeFolderEventReactions()
    {

        _subscriptions.Add(
            _state.CurrentPlayBackState
            .Where(psi => psi.State == DimmerPlaybackState.FolderAdded)
               .Subscribe(folderPath =>
                {
                    if (folderPath == null)
                        return;
                    _logger.LogInformation("BaseAppFlow: Detected FolderAdded state for {Path}. Triggering folder preference update and scan.", folderPath);


                    Task.Run(async () =>
                    {
                        try
                        {
                            _logger.LogInformation("BaseAppFlow: FolderAdded -> {Path}", folderPath);
                            if (folderPath.ExtraParameter is string pathh)
                            {
                                await _libraryScannerService.ScanLibrary(new List<string> { pathh });
                            }

                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error during folder scan.");
                        }
                    });


                }, ex => _logger.LogError(ex, "Error processing FolderAdded state."))
        );

        _subscriptions.Add(
            _state.CurrentPlayBackState
            .Where(psi => psi.State == DimmerPlaybackState.FolderAdded)
               .Subscribe(folderPath =>
                {
                    if (folderPath == null)
                        return;
                    _logger.LogInformation("BaseAppFlow: Detected FolderAdded state for {Path}. Triggering folder preference update and scan.", folderPath);


                    Task.Run(async () =>
                    {
                        try
                        {

                            var folderPathList = folderPath.ExtraParameter as List<string>;
                            await _libraryScannerService.ScanLibrary(folderPathList);


                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error during folder scan.");
                        }
                    });


                }, ex => _logger.LogError(ex, "Error processing FolderAdded state."))
        );

        _subscriptions.Add(
            _state.CurrentPlayBackState
                .Where(psi => psi.State == DimmerPlaybackState.FolderRemoved && psi.ExtraParameter is string)
                .Subscribe(folderPath =>
                {
                    if (folderPath == null)
                        return;
                    _logger.LogInformation("BaseAppFlow: Detected FolderRemoved state for {Path}. Ensuring folder is removed from watch and considering rescan.", folderPath);





                    Task.Run(async () => await _libraryScannerService.ScanLibrary(_settingsService.UserMusicFoldersPreference?.ToList() ?? new List<string>()));
                }, ex => _logger.LogError(ex, "Error processing FolderRemoved state."))
        );
    }



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

        }
        if (playType < PlayType.LogEvent && songContext != null)
        {
            _logger.LogDebug("LogApplicationEvent: Playback-related PlayType {PlayType} with song {SongTitle} logged explicitly. This is usually handled by state transitions.", playType, songContext.Title);
        }


        //UpdateDatabaseWithPlayEvent(songContext, playType, position, eventDetails);



    }
    public void UpdateDatabaseWithPlayEvent(SongModelView? songView, PlayType? type, double? position = null)
    {
        if (type is null)
            return;
        if (songView is null || songView.Id == default)
        {
            _logger.LogError("UpdateDatabaseWithPlayEvent: Invalid SongView provided for PlayType {PlayType}. Event cannot be logged.", type);
            return;
        }

        try
        {
            // Step 1: Create and save the play event FIRST.
            // This is a simple, standalone operation.
            var playEvent = new DimmerPlayEvent
            {
                // The event ID is generated by the repo's Create method.
                SongId = songView.Id,
                SongName = songView.Title,
                PlayType = (int)type,
                PlayTypeStr = type.ToString(),
                EventDate = DateTimeOffset.UtcNow,
                DatePlayed = DateTimeOffset.UtcNow,
                PositionInSeconds = position ?? 0,
                WasPlayCompleted = type == PlayType.Completed,
            };
            // Use the IRepository<DimmerPlayEvent> to create it.
            var savedPlayEvent = _playEventRepo.Create(playEvent);

            // Step 2: Now, update the SongModel to link to the new event.
            // This uses the super-safe Update(id, action) method.
            bool wasSongUpdated = _songRepo.Update(songView.Id, liveSong =>
            {
                // 'liveSong' is the managed object, safe to modify here.
                liveSong.PlayHistory.Add(savedPlayEvent);

                //// If the song was completed, we can also update its play count, etc.
                //if (type == PlayType.Completed)
                //{
                //    liveSong.PlayCount++;
                //    liveSong.LastPlayed = DateTimeOffset.UtcNow;
                //}
            });

            if (wasSongUpdated)
            {
                _logger.LogInformation("Added play event {EventId} to history of song {SongTitle}", savedPlayEvent.Id, songView.Title);
            }
            else
            {
                // This can happen if the song was deleted between the time the view was loaded and now.
                _logger.LogWarning("Could not find song with ID {SongId} to add play event history.", songView.Id);
            }

            // Step 3: Update the UI state.
            string userFriendlyMessage = UserFriendlyLogGenerator.GetPlaybackStateMessage(type, songView, position);
            _state.SetCurrentLogMsg(new AppLogModel { Log = userFriendlyMessage, ViewSongModel = songView });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateDatabaseWithPlayEvent: Exception occurred. Type={PlayType}, Song={SongTitle}", type, songView.Title);
        }
    }

    public void ToggleShuffle(bool isOn)
    {
        _logger.LogDebug("ToggleShuffle called with: {IsOn}", isOn);
        _settingsService.ShuffleOn = isOn;
        _state.SetShuffleActive(isOn);
    }

    public void ToggleRepeatMode()
    {
        var currentMode = _settingsService.RepeatMode;
        var enumValues = Enum.GetValues(typeof(RepeatMode)).Cast<RepeatMode>().ToList();
        int currentIndex = enumValues.IndexOf(currentMode);
        RepeatMode nextMode = enumValues[(currentIndex + 1) % enumValues.Count];

        _logger.LogDebug("ToggleRepeatMode: From {CurrentMode} to {NextMode}", currentMode, nextMode);
        _settingsService.RepeatMode = nextMode;
        _state.SetRepeatMode(nextMode);
    }





    public UserModel UpsertUserAsync(UserModel user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));
        var updatedUser = _userRepo.Upsert(user);
        LogApplicationEvent(PlayType.LogEvent, eventDetails: $"User upserted: {user.UserName ?? user.Id.ToString()}");
        if (CurrentUserInstance?.Id == updatedUser.Id || CurrentUserInstance == null)
        {
            CurrentUserInstance = updatedUser;
            _state.SetCurrentUser(_mapper.Map<UserModelView>(updatedUser));
        }
        return updatedUser;
    }

    public PlaylistModel UpsertPlaylist(PlaylistModel playlist)
    {
        if (playlist == null)
            throw new ArgumentNullException(nameof(playlist));



        var updatedPlaylist = _playlistRepo.Upsert(playlist);
        LogApplicationEvent(PlayType.LogEvent, eventDetails: $"Playlist upserted: {playlist.PlaylistName}");
        return updatedPlaylist;
    }

    public ArtistModel UpsertArtist(ArtistModel artist)
    {
        if (artist == null)
            throw new ArgumentNullException(nameof(artist));
        var updatedArtist = _artistRepo.Upsert(artist);
        LogApplicationEvent(PlayType.LogEvent, eventDetails: $"Artist upserted: {artist.Name}");
        return updatedArtist;
    }

    public AlbumModel UpsertAlbum(AlbumModel album)
    {
        if (album == null)
            throw new ArgumentNullException(nameof(album));
        var updatedAlbum = _albumRepo.Upsert(album);
        LogApplicationEvent(PlayType.LogEvent, eventDetails: $"Album upserted: {album.Name}");
        return updatedAlbum;
    }

    public SongModel UpsertSong(SongModel song)
    {
        if (song == null)
            throw new ArgumentNullException(nameof(song));
        var updatedSong = _songRepo.Upsert(song);
        LogApplicationEvent(PlayType.LogEvent, _mapper.Map<SongModelView>(updatedSong), eventDetails: $"Song upserted: {song.Title}");
        return updatedSong;
    }


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


        }
        _disposed = true;
    }
}