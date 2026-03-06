using Dimmer.Interfaces;
using Microsoft.Extensions.Logging;
using Realms;

namespace Dimmer.Orchestration;

public class BaseAppFlow : IDisposable
{
    private readonly IDimmerStateService _state;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<BaseAppFlow> _logger;
    private readonly IRepository<UserModel> _userRepo;
    private readonly IRepository<PlaylistModel> _playlistRepo;
    private readonly IRepository<ArtistModel> _artistRepo;
    private readonly IRepository<AlbumModel> _albumRepo;
    private readonly IRepository<SongModel> _songRepo;
    private readonly IFolderMgtService _folderManagementService;
    private readonly IDimmerAudioService _audioService;
    private readonly IRealmFactory _realmFactory; // Added to avoid passing in methods

    private bool _disposed;

    // Consider making this configurable if needed
    private const int MAX_EVENT_CACHE_SIZE = 100;
    private readonly Dictionary<ObjectId, (PlayType Type, DateTime Timestamp)> _lastEventCache = new();

    public UserModel? CurrentUserInstance { get; private set; }
    public AppStateModelView? AppStateSnapshot { get; private set; }
    public AchievementService AchievementService { get; set; }

    public BaseAppFlow(
        IDimmerStateService state,
        AchievementService achService,
        IDimmerAudioService audioService,
        IRepository<UserModel> userRepo,
        IRepository<PlaylistModel> playlistRepo,
        IRepository<ArtistModel> artistRepo,
        IRepository<AlbumModel> albumRepo,
        IRepository<SongModel> songRepo,
        IRepository<AppStateModel> appStateRepo, // Kept for backward compatibility
        ISettingsService settingsService,
        IFolderMgtService folderManagementService,
        ILibraryScannerService libraryScannerService, // Kept for backward compatibility
        IRealmFactory realmFactory, // Added to avoid passing in methods
        ILogger<BaseAppFlow> logger)
    {
        AchievementService = achService ?? throw new ArgumentNullException(nameof(achService));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
        _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
        _playlistRepo = playlistRepo ?? throw new ArgumentNullException(nameof(playlistRepo));
        _artistRepo = artistRepo ?? throw new ArgumentNullException(nameof(artistRepo));
        _albumRepo = albumRepo ?? throw new ArgumentNullException(nameof(albumRepo));
        _songRepo = songRepo ?? throw new ArgumentNullException(nameof(songRepo));
        // Keep these for backward compatibility but mark as obsolete if possible
        _ = appStateRepo ?? throw new ArgumentNullException(nameof(appStateRepo));
        _ = libraryScannerService ?? throw new ArgumentNullException(nameof(libraryScannerService));

        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _folderManagementService = folderManagementService ?? throw new ArgumentNullException(nameof(folderManagementService));
        _realmFactory = realmFactory ?? throw new ArgumentNullException(nameof(realmFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Removed empty method call

        _logger.LogInformation("BaseAppFlow (Coordinator & Logger) initialized.");
    }

    #region Event Logging

    public void LogApplicationEvent(PlayType playType, SongModelView? songContext = null, string? eventDetails = null, double? position = null)
    {
        // Validation logging only - no functional changes
        if (playType < PlayType.LogEvent && songContext == null)
        {
            _logger.LogWarning("Playback-related PlayType {PlayType} called without songContext.", playType);
        }
        else if (playType < PlayType.LogEvent && songContext != null)
        {
            _logger.LogDebug("Playback-related PlayType {PlayType} with song {SongTitle} logged explicitly. This is usually handled by state transitions.",
                playType, songContext.Title);
        }

        // Note: Actual event persistence is handled by UpdateDatabaseWithPlayEvent
        // This method is kept for backward compatibility and logging
    }

    public async Task UpdateDatabaseWithPlayEvent(SongModelView? songView, PlayType? type, double? position = null)
    {
        // Early returns for invalid inputs
        if (songView == null || type == null)
        {
            _logger.LogWarning("UpdateDatabaseWithPlayEvent called with null parameters: Song={HasSong}, Type={HasType}",
                songView != null, type != null);
            return;
        }

        if (songView.Id == default)
        {
            _logger.LogError("UpdateDatabaseWithPlayEvent: Invalid SongView ID (default) for PlayType {PlayType}", type);
            return;
        }

        // Deduplication logic - improved with timestamp to prevent issues
        if (_lastEventCache.TryGetValue(songView.Id, out var lastEvent))
        {
            // If it's the same event type AND it's not Favorited AND it happened recently (within 1 second)
            if (lastEvent.Type == type &&
                type != PlayType.Favorited &&
                (DateTime.UtcNow - lastEvent.Timestamp).TotalSeconds < 1)
            {
                _logger.LogDebug("Ignoring duplicate event {Type} for song {SongTitle}", type, songView.Title);
                return;
            }
        }

        // Update cache with new event
        _lastEventCache[songView.Id] = (type.Value, DateTime.UtcNow);

        // Prevent cache from growing too large
        if (_lastEventCache.Count > MAX_EVENT_CACHE_SIZE)
        {
            // Remove oldest 20 entries
            var oldestKeys = _lastEventCache
                .OrderBy(kvp => kvp.Value.Timestamp)
                .Take(20)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in oldestKeys)
            {
                _lastEventCache.Remove(key);
            }
        }

        _logger.LogDebug("Processing Event {Type} for song {SongTitle} at position {Position}",
            type, songView.Title, position ?? 0);

        // Special handling for pause events (if needed in the future)
        if (type == PlayType.Pause)
        {
            // Future expansion point
        }

        try
        {
            var realm = _realmFactory.GetRealmInstance();

            // Create the play event
            var playEvent = new DimmerPlayEvent
            {
                SongId = songView.Id,
                SongName = songView.Title,
                PlayType = (int)type,
                PlayTypeStr = type.ToString(),
                EventDate = DateTimeOffset.UtcNow,
                DatePlayed = DateTimeOffset.UtcNow,
                AudioOutputDevice = _audioService.GetCurrentAudioOutputDevice().ToAudioOutputDevice(),
                PositionInSeconds = position ?? 0,
                WasPlayCompleted = type == PlayType.Completed,
            };

            await realm.WriteAsync(() =>
            {
                // Add the event and link it to the song in one transaction
                var addedEvent = realm.Add(playEvent, true);

                var song = realm.Find<SongModel>(songView.Id);
                if (song != null)
                {
                    song.PlayHistory.Add(addedEvent);
                }
                else
                {
                    _logger.LogWarning("Song {SongId} not found when adding play event", songView.Id);
                }
            });

            _logger.LogInformation("Added play event {EventId} of type {PlayType} to history of song {SongTitle}",
                playEvent.Id, playEvent.PlayTypeStr, songView.Title);

            // Update UI state with user-friendly message
            string userFriendlyMessage = UserFriendlyLogGenerator.GetPlaybackStateMessage(type, songView, position);
            _state.SetCurrentLogMsg(userFriendlyMessage, DimmerLogLevel.Info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateDatabaseWithPlayEvent: Exception occurred. Type={PlayType}, Song={SongTitle}",
                type, songView.Title);
        }
    }

    #endregion

    #region CRUD Operations



    public ArtistModel? UpsertArtist(ArtistModel artist)
    {
        ArgumentNullException.ThrowIfNull(artist);

        var updatedArtist = _artistRepo.Upsert(artist);
        LogApplicationEvent(PlayType.LogEvent, eventDetails: $"Artist upserted: {artist.Name}");

        return updatedArtist;
    }

    public AlbumModel? UpsertAlbum(AlbumModel? album)
    {
        ArgumentNullException.ThrowIfNull(album);

        var updatedAlbum = _albumRepo.Upsert(album);
        LogApplicationEvent(PlayType.LogEvent, eventDetails: $"Album upserted: {album.Name}");

        return updatedAlbum;
    }



    #endregion

    #region IDisposable Implementation

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _logger.LogInformation("BaseAppFlow disposing.");
            _lastEventCache.Clear();
            // Note: We don't dispose owned services as they're injected
        }

        _disposed = true;
    }

    #endregion
}