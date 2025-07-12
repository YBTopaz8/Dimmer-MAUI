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


        //InitializeFolderEventReactions();

        _logger.LogInformation("BaseAppFlow (Coordinator & Logger) initialized.");
    }


    private PlaybackStateInfo? _previousPlaybackStateForLogging;


    private void InitializeFolderEventReactions()
    {
        //_subscriptions.Add(
        //    _stateService.CurrentPlayBackState
        //        .Where(psi => psi.State == DimmerPlaybackState.FolderAdded)
        //        .Select(psi => psi.ExtraParameter as string)
        //        .Subscribe(async folderPath =>
        //        {
        //            if (folderPath == null)
        //                return;
        //            _logger.LogInformation("BaseAppFlow: Detected FolderAdded state for {Path}. Triggering folder preference update and scan.", folderPath);



        //            await _libraryScannerService.ScanLibrary(new List<string> { folderPath });

        //        }, ex => _logger.LogError(ex, "Error processing FolderAdded state."))
        //);
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
        if (type ==PlayType.Pause)
        {

        }
        if (type is null)
        {
            return;
        }


        ObjectId? songObjectId = null;

        if (songView != null)
        {
            if (songView.Id != default)
            {
                songObjectId = songView.Id;
            }

        }

        try
        {


            _songRepo.BatchUpdate(realm =>
            {
                SongModel? managedSong = null;
                if (songObjectId.HasValue && songObjectId.Value != default(ObjectId))
                {
                    managedSong = realm.Find<SongModel>(songObjectId.Value);
                }

                if (managedSong == null && songView != null && type < PlayType.LogEvent)
                {
                    _logger.LogWarning("UpdateDatabaseWithPlayEvent (in BatchUpdate): SongModel with Id {SongId} not found. Play event will be created but not added to song history.", songObjectId, type);

                    managedSong = _mapper.Map<SongModel>(songView);
                    managedSong.Id = songObjectId.Value;
                    realm.Add(managedSong, update: true);
                }


                var playEvent = new DimmerPlayEvent
                {

                    SongId = managedSong?.Id,
                    SongName = songView?.Title ?? managedSong?.Title,
                    PlayType = (int)type,
                    PlayTypeStr = type.ToString(),
                    EventDate = DateTimeOffset.UtcNow,
                    DatePlayed = DateTimeOffset.UtcNow,
                    PositionInSeconds = position ?? ((type == PlayType.Completed && managedSong?.DurationInSeconds > 0) ? managedSong.DurationInSeconds : 0),
                    WasPlayCompleted = type == PlayType.Completed,

                };
                var managedPlayEvent = realm.Add(playEvent);


                if (managedSong != null && managedSong.IsValid)
                {
                    managedSong.PlayHistory.Add(managedPlayEvent);
                    _logger.LogInformation("Added play event {EventId} to history of song {SongTitle} (within BatchUpdate)", managedPlayEvent.Id, managedSong.Title);
                }

                string userFriendlyMessage = UserFriendlyLogGenerator.GetPlaybackStateMessage(type, songView, position);
                _state.SetCurrentLogMsg(new AppLogModel { Log = userFriendlyMessage, ViewSongModel = songView });


            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateDatabaseWithPlayEvent: Exception during BatchUpdate. Type={PlayType}, Song={SongTitle}", type, songView?.Title ?? "N/A");
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
        var updatedUser = _userRepo.AddOrUpdate(user);
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




        await Task.Run(() => _songRepo.BatchUpdate(realm =>
        {
            var song = realm.Find<SongModel>(songId);
            if (song != null)
            {

                var existingNote = song.UserNotes.FirstOrDefault(un => un.Id == note.Id);
                if (existingNote != null)
                {








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

        var songView = _mapper.Map<SongModelView>(_songRepo.GetById(songId));
        LogApplicationEvent(PlayType.LogEvent, songView, $"Note upserted for song ID: {songId}");
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