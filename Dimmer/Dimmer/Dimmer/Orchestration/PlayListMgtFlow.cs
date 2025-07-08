using Dimmer.Interfaces.Services.Interfaces;

using Microsoft.Extensions.Logging.Abstractions;

namespace Dimmer.Orchestration;

public class PlayListMgtFlow : IDisposable  // BaseAppFlow provides CurrentlyPlayingSong, etc.
{
    private readonly IMapper mapper;
    private readonly IDimmerStateService _state;
    private readonly IRepository<PlaylistModel> _playlistRepo; // For loading playlist definitions
    private readonly SubscriptionManager _subs; // For managing its own Rx subscriptions
    private readonly ILogger<PlayListMgtFlow> _logger;
    private readonly MultiPlaylistPlayer<SongModel> _multiPlayer;
    public MultiPlaylistPlayer<SongModel> MultiPlayer => _multiPlayer;
    // Local state driven by _multiPlayer or this flow's logic
    private SongModel? _currentTrackFromPlayer; // The actual SongModel from MultiPlaylistPlayer
    private int _lastActivePlaylistIndexInPlayer = -1;
    private bool _isPlayerPlayingFromMasterList = false; // If _multiPlayer is using the general "All Songs" list

    // UI-bound or informational properties
    public ObservableCollection<PlaylistModel> AllAvailablePlaylists { get; } = new(); // For UI selection
    // ActivePlaylistModel is now primarily for knowing which *PlaylistModel* context is active,
    // not necessarily the direct source of songs for _multiPlayer if playing "All Songs".
    // It's updated when _stateService.CurrentPlaylist changes.
    public PlaylistModel? ActivePlaylistModel { get; private set; }
    public SongModelView CurrentlyPlayingSong { get; private set; }

    private IEnumerable<SongModel>? _allCurrentSongsCache;
    public PlayListMgtFlow(

        IDimmerStateService state,
        IRepository<SongModel> songRepo, // For fetching song details if needed, not for queue mgmt
        IRepository<PlaylistModel> playlistRepo,
        IRepository<AlbumModel> albumRepo,  // For "Play Album"
                                            // Other repositories as needed by BaseAppFlow or specific actions
        SubscriptionManager subs,
        IMapper mapper,
        ILogger<PlayListMgtFlow> logger,
         // Removed repositories not directly used by THIS flow's core logic, assume BaseAppFlow handles them
         IRepository<UserModel> userRepo,
         IRepository<GenreModel> genreRepo,
         IRepository<DimmerPlayEvent> pdlRepo,
         ISettingsService settings,
         IFolderMgtService folderMgt,
        IRepository<AppStateModel> appstateRepo, // If BaseAppFlow needs it
        IRepository<ArtistModel> artistRepo // Added ArtistModel repository
    )
    {
        this.mapper=mapper;
        _state=state;

        _playlistRepo = playlistRepo ?? throw new ArgumentNullException(nameof(playlistRepo));
        _subs = subs ?? throw new ArgumentNullException(nameof(subs)); // Store from base or inject new
        _logger = logger ?? NullLogger<PlayListMgtFlow>.Instance;
        _multiPlayer = new MultiPlaylistPlayer<SongModel>();

        // Subscribe to MultiPlaylistPlayer events
        _multiPlayer.ItemSelectedForPlayback += OnPlayerItemSelected;
        _multiPlayer.BatchEnqueuedByQueueManager += OnPlayerBatchEnqueued;
        _multiPlayer.AllQueuesExhausted += OnPlayerAllQueuesExhausted;

        InitializeInternalSubscriptions();
        LoadInitialData();
        _logger.LogInformation("PlayListMgtFlow initialized.");
        _subs.Add(
    _state.AllCurrentSongs
          .Subscribe(list =>
          {
              _allCurrentSongsCache = list ?? Array.Empty<SongModel>(); // Ensure it's never null
              _logger.LogDebug("AllCurrentSongsCache updated with {SongCount} songs.", _allCurrentSongsCache.Count());
          })
);
        Debug.WriteLine("Done With Pl Mgt Flow");
    }

    private void LoadInitialData()
    {
        AllAvailablePlaylists.Clear();
        try
        {
            // Assuming GetAllAsync or similar if IRepository supports async
            var playlists = (_playlistRepo.GetAll().OrderBy(p => p.PlaylistName).ToList());
            // Ensure UI updates happen on the UI thread if AllAvailablePlaylists is bound
            // For now, assuming direct add is fine or this is called from UI thread context
            foreach (var pl in playlists)
                AllAvailablePlaylists.Add(pl);
            _logger.LogInformation("Loaded {PlaylistCount} playlists.", AllAvailablePlaylists.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load initial playlists.");
        }
    }

    private void InitializeInternalSubscriptions()
    {
        // React to playback commands from the global state

        _subs.Add(
            _state.CurrentPlayBackState
                  .DistinctUntilChanged(psi => psi.State) // Only on actual state enum change
                  .Subscribe(playbackStateInfo => HandlePlaybackCommand(playbackStateInfo.State))
        );


        // React to changes in the "Active Playlist Model" from global state
        // This is for knowing the *context*, not necessarily for immediately re-loading the player.
        _subs.Add(
            _state.CurrentPlaylist
                  .DistinctUntilChanged(p => p?.Id)
                  .Subscribe(playlistModel =>
                  {
                      ActivePlaylistModel = playlistModel;
                      _logger.LogDebug("Global active playlist model changed to: {PlaylistName}", playlistModel?.PlaylistName ?? "None");
                      // If playlistModel is null, it implies "All Songs" or a generic list is active.
                      // If it's not null, the UI might have selected a playlist.
                      // PlayListMgtFlow's PlayPlaylist method would have already loaded it.
                  })
        );

        // React to changes in the master song library IF we are currently playing from it
        _subs.Add(
            _state.AllCurrentSongs // This is the full library from DimmerStateService
                .Skip(1) // Skip initial value
                .Subscribe(latestLibrarySongs =>
                {
                    if (_isPlayerPlayingFromMasterList && (_multiPlayer.TotalCount == 0 || _currentTrackFromPlayer == null))
                    {
                        _logger.LogInformation("Master song library updated, and player was in 'All Songs' mode or empty. Reloading 'All Songs'.");
                        PlayAllSongsFromLibrary(latestLibrarySongs);
                    }
                })
        );

        // Update our own `CurrentlyPlayingSong` (from BaseAppFlow) when the state's song changes.
        // This is mainly for UI binding consistency if other things can change _stateService.CurrentSong.
        _subs.Add(
            _state.CurrentSong
                  .DistinctUntilChanged(s => s?.Id)
                  .Subscribe(songModelViewFromState =>
                  {
                      CurrentlyPlayingSong = songModelViewFromState; // BaseAppFlow.CurrentlyPlayingSong
                                                                     // If songModelViewFromState.ToModel().Id != _currentTrackFromPlayer?.Id,
                                                                     // it means something else changed the current song in the state.
                                                                     // PlayListMgtFlow should ideally be the one driving this via _multiPlayer.
                  })
        );

        _subs.Add(
            _state.IsShuffleActive
                  .DistinctUntilChanged()
                  .Subscribe(isShuffleOn =>
                  {
                      _currentShuffleStateValue = isShuffleOn; // Update the local cached value
                      _logger.LogDebug("Local _currentShuffleStateValue updated to: {IsShuffleOn}", isShuffleOn);
                  })
        );

    }

    // --- Handlers for MultiPlaylistPlayer Events ---
    private void OnPlayerItemSelected(int playlistIndex, SongModel song, int batchId, bool isPrevOrNext)
    {
        var _baseAppFlow = IPlatformApplication.Current?.Services.GetService<BaseAppFlow>();

        _currentTrackFromPlayer = song;
        _lastActivePlaylistIndexInPlayer = playlistIndex;

        _state.SetCurrentSong(song); // Update global state with the new SongModel
        _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.Playing,
            null, mapper.Map<SongModelView>(song),
            songdb: song));
        var songV = mapper.Map<SongModelView>(song);

        _logger.LogInformation("Player selected: '{SongTitle}' from PlaylistIndex {PlaylistIndex}, Batch {BatchId}.",
            song.Title, playlistIndex, batchId);
    }

    private void OnPlayerBatchEnqueued(int playlistIndex, int batchId, IReadOnlyList<SongModel> batch)
    {
        _logger.LogDebug("Player enqueued Batch {BatchId} ({BatchCount} items) from PIdx {PlaylistIndex}.",
            batchId, batch.Count, playlistIndex);
    }

    private void OnPlayerAllQueuesExhausted()
    {
        _logger.LogInformation("Player: All queues exhausted.");
        _currentTrackFromPlayer = null;
        _state.SetCurrentSong(null); // Clear current song in global state
        _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.PlayCompleted, null, null, null)); // Or EndedAll
    }

    // --- Handler for Global Playback Commands ---
    private void HandlePlaybackCommand(DimmerPlaybackState command)
    {
        bool currentShuffleState = _currentShuffleStateValue; // Get current shuffle state

        switch (command)
        {
            case DimmerPlaybackState.PlayCompleted: // Song ended, play next

                var ee = _multiPlayer.Next(randomizeSourcePlaylist: currentShuffleState);
                _state.SetCurrentState(new(DimmerPlaybackState.PlaylistPlay, null, mapper.Map<SongModelView>(ee), ee)); // Update state with next song

                break;
            case DimmerPlaybackState.PlayNextUser:

            case DimmerPlaybackState.PlayNextUI:

                var e = _multiPlayer.Next(randomizeSourcePlaylist: currentShuffleState);

                _state.SetCurrentState(new(DimmerPlaybackState.Playing, null, mapper.Map<SongModelView>(e), e)); // Update state with next song
                break;

            case DimmerPlaybackState.PlayPreviousUI:
            case DimmerPlaybackState.PlayPreviousUser:
                var previousSong = _multiPlayer.Previous(randomizeSourcePlaylist: false); // Typically don't shuffle on previous
                _state.SetCurrentState(new(DimmerPlaybackState.Playing, null, mapper.Map<SongModelView>(previousSong), previousSong)); // Update state with previous song
                break;

            case DimmerPlaybackState.ShuffleRequested: // User explicitly requested shuffle toggle
                bool newShuffleState = !currentShuffleState;
                _state.SetShuffleActive(newShuffleState); // Update global shuffle state
                if (newShuffleState && _multiPlayer.TotalCount > 0) // If shuffle turned ON and items exist
                {
                    ApplyShuffleToCurrentPlayerAndPlay();
                }
                break;

            case DimmerPlaybackState.Loading: // E.g., app is preparing, ensure player is cued if possible
                if (_currentTrackFromPlayer != null) // If we had a song, re-assert it
                {
                    _state.SetCurrentSong(_currentTrackFromPlayer);
                    _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.Playing, null, null, _currentTrackFromPlayer));
                }
                else if (_multiPlayer.TotalCount > 0) // If player has songs but nothing current
                {
                    _multiPlayer.Next(currentShuffleState); // Try to play something
                }
                break;

            // States that this flow might not directly act upon for track changes:
            case DimmerPlaybackState.Opening:
            case DimmerPlaybackState.Playing: // This state is usually a result, not a command here

            case DimmerPlaybackState.PausedDimmer:
            case DimmerPlaybackState.Error:
            case DimmerPlaybackState.Failed:
            case DimmerPlaybackState.RefreshStats: // UI concern
                _logger.LogDebug("Playback command {Command} received, no direct track change action by PlayListMgtFlow.", command);
                break;

            default:
                _logger.LogWarning("Unhandled playback command in PlayListMgtFlow: {Command}", command);
                break;
        }
    }

    // --- Public Methods to Initiate Playback (called by UI commands/ViewModels) ---

    public void PlayPlaylist(PlaylistModel playlist, int startIndex = 0)
    {
        if (playlist == null || playlist.SongsInPlaylist == null || !playlist.SongsInPlaylist.Any())
        {
            _logger.LogWarning("Attempt to play empty/null playlist: {PlaylistName}", playlist?.PlaylistName ?? "Unknown");
            ClearPlayerAndStopState();
            return;
        }
        _logger.LogInformation("Playing playlist: {PlaylistName}", playlist.PlaylistName);
        LoadSongsIntoPlayer(playlist.SongsInPlaylist, startIndex, isMasterList: false);
        _state.SetCurrentPlaylist(playlist); // Signal active playlist model to global state
    }

    public void PlayAlbum(AlbumModel album, int startIndex = 0)
    {
        if (album == null || album.SongsInAlbum == null || !album.SongsInAlbum.Any())
        {
            _logger.LogWarning("Attempt to play empty/null album: {AlbumName}", album?.Name ?? "Unknown");
            ClearPlayerAndStopState();
            return;
        }
        _logger.LogInformation("Playing album: {AlbumName}", album.Name);
        LoadSongsIntoPlayer(album.SongsInAlbum, startIndex, isMasterList: false);
        _state.SetCurrentPlaylist(null); // No specific "PlaylistModel" for an album
    }

    public void PlayAllSongsFromLibrary(IEnumerable<SongModel>? librarySongs = null, int startIndex = 0)
    {
        var songsToPlay = librarySongs ?? _allCurrentSongsCache;
        // Get current full library
        if (!songsToPlay.Any()) // No need to check for null if _allCurrentSongsCache is initialized to empty.
        {
            _logger.LogWarning("Attempt to play all songs, but the library is empty.");
            ClearPlayerAndStopState();
            return;
        }
        _logger.LogInformation("Playing all songs from library.");
        LoadSongsIntoPlayer(songsToPlay, startIndex, isMasterList: true);
        _state.SetCurrentPlaylist(null); // Signal no specific playlist model is active
    }

    public void PlayGenericSongList(IEnumerable<SongModel> songs, int startIndex = 0, string? listName = null)
    {
        if (songs == null || !songs.Any())
        {
            _logger.LogWarning("Attempt to play an empty generic song list.");
            ClearPlayerAndStopState();
            return;
        }
        _logger.LogInformation("Playing generic song list: {ListName}", listName ?? "Unnamed List");
        LoadSongsIntoPlayer(songs, startIndex, isMasterList: false); // Treat as not master list unless specified
        _state.SetCurrentPlaylist(null);
    }


    // --- Internal Player Loading Logic ---
    private void LoadSongsIntoPlayer(IEnumerable<SongModel> songs, int startIndex, bool isMasterList)
    {
        _isPlayerPlayingFromMasterList = isMasterList;
        var songList = songs.ToList(); // Materialize
        startIndex = Math.Clamp(startIndex, 0, Math.Max(0, songList.Count - 1));

        var newQueue = new QueueManager<SongModel>();
        newQueue.Initialize(songList, startIndex);

        _multiPlayer.Clear(); // Clear previous queues
        _multiPlayer.AddPlaylist(newQueue);

        if (newQueue.Current != null)
        {
            // This will trigger OnPlayerItemSelected, which updates global state
            OnPlayerItemSelected(0, newQueue.Current, newQueue.CurrentBatchId, false); // Manually invoke for the first item
        }
        else if (songList.Count!=0) // Should not happen if Initialize works
        {
            _logger.LogError("Queue initialized with songs but Current is null. This is an issue in QueueManager or list processing.");
            ClearPlayerAndStopState();
        }
        else // No songs after all
        {
            ClearPlayerAndStopState();
        }
    }

    // --- Playlist Mixing ---
    public void AddPlaylistToMix(PlaylistModel playlist, bool startPlayingFromIt = false, bool shuffleIt = false)
    {
        if (playlist?.SongsInPlaylist == null || !playlist.SongsInPlaylist.Any())
        {
            _logger.LogWarning("Cannot add empty playlist {PlaylistName} to mix.", playlist?.PlaylistName ?? "Unknown");
            return;
        }
        if (_multiPlayer.Playlists.Count >= 3) // Your arbitrary limit
        {
            _logger.LogWarning("Max 3 playlists in mix reached (app limit).");
            return;
        }

        var newQueue = new QueueManager<SongModel>();
        newQueue.Initialize([.. playlist.SongsInPlaylist]);
        if (shuffleIt)
            newQueue.ShuffleQueueInPlace();

        int newPlaylistIndex = _multiPlayer.AddPlaylist(newQueue);
        if (newPlaylistIndex == -1)
        {
            _logger.LogError("Failed to add playlist {PlaylistName} to MultiPlayer.", playlist.PlaylistName);
            return;
        }
        _logger.LogInformation("Added playlist '{PlaylistName}' to mix at index {Index}. Total: {Count}",
            playlist.PlaylistName, newPlaylistIndex, _multiPlayer.Playlists.Count);

        if (startPlayingFromIt && newQueue.Current != null)
        {
            OnPlayerItemSelected(newPlaylistIndex, newQueue.Current, newQueue.CurrentBatchId, false);
        }
        // If this is the very first queue added to an empty player, start playing from it.
        else if (_multiPlayer.Playlists.Count == 1 && _multiPlayer.TotalCount > 0 && _currentTrackFromPlayer == null && newQueue.Current != null)
        {
            OnPlayerItemSelected(newPlaylistIndex, newQueue.Current, newQueue.CurrentBatchId, false);
        }
    }

    public void RemovePlaylistFromMix(int playlistIndexInPlayer)
    {
        if (playlistIndexInPlayer < 0 || playlistIndexInPlayer >= _multiPlayer.Playlists.Count)
        {
            _logger.LogWarning("Invalid index {Index} for RemovePlaylistFromMix.", playlistIndexInPlayer);
            return;
        }

        bool wasPlayingFromThisQueue = (_lastActivePlaylistIndexInPlayer == playlistIndexInPlayer);
        _multiPlayer.RemovePlaylistAt(playlistIndexInPlayer);
        _logger.LogInformation("Removed playlist at index {Index} from mix. Remaining: {Count}",
            playlistIndexInPlayer, _multiPlayer.Playlists.Count);

        // Adjust _lastActivePlaylistIndexInPlayer if needed
        if (_lastActivePlaylistIndexInPlayer == playlistIndexInPlayer)
            _lastActivePlaylistIndexInPlayer = -1;
        else if (_lastActivePlaylistIndexInPlayer > playlistIndexInPlayer)
            _lastActivePlaylistIndexInPlayer--;

        if (wasPlayingFromThisQueue || _currentTrackFromPlayer == null || _multiPlayer.TotalCount == 0)
        {
            if (_multiPlayer.TotalCount > 0)
            {
                // Try to resume from a sensible queue or just play next
                bool currentShuffleState = _currentShuffleStateValue;
                if (_lastActivePlaylistIndexInPlayer != -1 && _lastActivePlaylistIndexInPlayer < _multiPlayer.Playlists.Count &&
                    _multiPlayer.Playlists[_lastActivePlaylistIndexInPlayer].Current != null)
                {
                    var qm = _multiPlayer.Playlists[_lastActivePlaylistIndexInPlayer];
                    OnPlayerItemSelected(_lastActivePlaylistIndexInPlayer, qm.Current!, qm.CurrentBatchId, false);
                }
                else
                {
                    _multiPlayer.Next(currentShuffleState); // Fallback
                }
            }
            else
            {
                ClearPlayerAndStopState();
            }
        }
    }

    // --- Shuffle Control ---
    public void ApplyShuffleToCurrentPlayerAndPlay()
    {
        if (_multiPlayer.TotalCount == 0)
        {
            _logger.LogInformation("No active queues to shuffle.");
            return;
        }

        SongModel? songToTryAndKeepCurrent = _currentTrackFromPlayer;
        int originalPlaylistIndexOfSong = _lastActivePlaylistIndexInPlayer;

        _multiPlayer.ShuffleAllPlaylists(); // This shuffles each internal QueueManager
        _logger.LogInformation("Applied shuffle to all active queues in MultiPlayer.");

        SongModel? songAfterShuffle = null;
        int indexOfPlaylistAfterShuffle = -1;

        // Attempt to find the previously current song or a song from its original queue
        if (songToTryAndKeepCurrent != null && originalPlaylistIndexOfSong != -1 && originalPlaylistIndexOfSong < _multiPlayer.Playlists.Count)
        {
            var originalQueue = _multiPlayer.Playlists[originalPlaylistIndexOfSong];
            if (originalQueue.Current?.Id == songToTryAndKeepCurrent.Id) // QM's shuffle might keep it current
            {
                songAfterShuffle = originalQueue.Current;
                indexOfPlaylistAfterShuffle = originalPlaylistIndexOfSong;
            }
            else if (originalQueue.Current != null) // If not, just take whatever is current in that queue now
            {
                songAfterShuffle = originalQueue.Current;
                indexOfPlaylistAfterShuffle = originalPlaylistIndexOfSong;
            }
        }

        // Fallback: if no specific song found, pick from any non-empty queue
        if (songAfterShuffle == null)
        {
            for (int i = 0; i < _multiPlayer.Playlists.Count; i++)
            {
                var qm = _multiPlayer.Playlists[i];
                if (qm.Current != null)
                {
                    songAfterShuffle = qm.Current;
                    indexOfPlaylistAfterShuffle = i;
                    break;
                }
            }
        }

    }
    private bool _currentShuffleStateValue = false;
    private bool disposedValue;

    // --- Utility ---
    private void ClearPlayerAndStopState()
    {
        _multiPlayer.Clear();
        _currentTrackFromPlayer = null;
        _lastActivePlaylistIndexInPlayer = -1;
        _isPlayerPlayingFromMasterList = false;

        _state.SetCurrentSong(null); // Update global state
        _state.SetCurrentPlaylist(null);
        _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.PausedDimmer, null, null, null));
        _logger.LogInformation("Player cleared and playback state stopped.");
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue=true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~PlayListMgtFlow()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}