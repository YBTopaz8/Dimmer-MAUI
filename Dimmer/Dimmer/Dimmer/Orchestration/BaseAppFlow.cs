using Dimmer.Utilities.FileProcessorUtils;
using Syncfusion.Maui.Toolkit.NavigationDrawer;
using System.Reactive.Concurrency;

namespace Dimmer.Orchestration;

public class BaseAppFlow : IDisposable
{


    public readonly IPlayerStateService _state;
    private readonly IRepository<SongModel> _songRepo;
    private readonly IRepository<GenreModel> _genreRepo;
    private readonly IRepository<AlbumArtistGenreSongLink> _aagslRepo;
    private readonly IRepository<PlayDateAndCompletionStateSongLink> _pdlRepo;
    private readonly IRepository<PlaylistModel> _playlistRepo;
    private readonly IRepository<ArtistModel> _artistRepo;
    private readonly IRepository<AlbumModel> _albumRepo;
    private readonly ISettingsService _settings;
    private readonly IFolderMonitorService _folderMonitor;
    public readonly IMapper _mapper;
    private bool _disposed;

    public SongModelView CurrentlyPlayingSong { get; set; } = new();
    

    public bool  IsShuffleOn
        => _settings.ShuffleOn;

    public RepeatMode CurrentRepeatMode
        => _settings.RepeatMode;
    // enforce a single instance of the app flow
    public BaseAppFlow(
        IPlayerStateService state,
        IRepository<SongModel> songRepo,
        IRepository<GenreModel> genreRepo,
        IRepository<AlbumArtistGenreSongLink> aagslRepo,
        IRepository<PlayDateAndCompletionStateSongLink> pdlRepo,
        IRepository<PlaylistModel> playlistRepo,
        IRepository<ArtistModel> artistRepo,
        IRepository<AlbumModel> albumRepo,
        ISettingsService settings,
        IFolderMonitorService folderMonitor,
        IMapper mapper)
    {
        _state = state;
        _songRepo = songRepo;
        _pdlRepo = pdlRepo;
        _playlistRepo = playlistRepo;
        _artistRepo = artistRepo;
        _albumRepo = albumRepo;
        _genreRepo = genreRepo;
        _aagslRepo = aagslRepo;
        _settings = settings;
        _folderMonitor = folderMonitor;
        _mapper = mapper;

        Initialize();

    }
    public static IReadOnlyCollection<SongModel> MasterList { get; private set; }
    
    private IDisposable Initialize()
    {
        // 1) load once
        MasterList = [.. _songRepo
            .GetAll(true)];

        // 2) folder‑watch
        _folderMonitor.Start(_settings.UserMusicFoldersPreference);

        // 3) live updates, on UI‑thread if available
        var syncCtx = SynchronizationContext.Current;
        IScheduler scheduler = syncCtx != null
            ? new SynchronizationContextScheduler(syncCtx)
            : TaskPoolScheduler.Default;

        _state.SetSecondSelectdSong(MasterList.First());
        _state.SetCurrentPlaylist(Enumerable.Empty<SongModel>(), null);
        return _songRepo
            .WatchAll()
            .ObserveOn(scheduler)
            .DistinctUntilChanged()
            .Subscribe(list =>
            {
                if (list.Count == MasterList.Count)
                {
                    return;
                }
                MasterList = [.. list];
            });
    }

    public void SeekedTo(double? position)
        => UpdatePlaybackState(CurrentlyPlayingSong.LocalDeviceId, PlayType.Seeked, position);
    public void PlaySong()
        => UpdatePlaybackState(CurrentlyPlayingSong.LocalDeviceId, PlayType.Play);

    public void PauseSong()
        => UpdatePlaybackState(CurrentlyPlayingSong.LocalDeviceId, PlayType.Pause);

    public void ResumeSong()
        => UpdatePlaybackState(CurrentlyPlayingSong.LocalDeviceId, PlayType.Resume);
    
    public void PlayEnded()
        => UpdatePlaybackState(CurrentlyPlayingSong.LocalDeviceId, PlayType.Completed);

    public void UpdatePlaybackState(
        string? songId,
        PlayType type,
        double? position = null)
    {
        if(string.IsNullOrEmpty(songId))
        {
            songId = CurrentlyPlayingSong.LocalDeviceId;
        }


        var link = new PlayDateAndCompletionStateSongLink
        {
            LocalDeviceId = Guid.NewGuid().ToString(),
            SongId = songId,
            PlayType = (int)type,
            DatePlayed = DateTime.Now,
            PositionInSeconds = position ?? 0,
            WasPlayCompleted = type == PlayType.Completed
        };

        _pdlRepo.AddOrUpdate(link);
    }


    public void UpSertPlaylist(PlaylistModel model)
    {
        if (string.IsNullOrEmpty(model.LocalDeviceId))
            model.LocalDeviceId = Guid.NewGuid().ToString();
        _playlistRepo.AddOrUpdate(model);
    }

    public void UpSertArtist(ArtistModel model)
    {
        if (string.IsNullOrEmpty(model.LocalDeviceId))
            model.LocalDeviceId = Guid.NewGuid().ToString();
        _artistRepo.AddOrUpdate(model);
    }

    public void UpSertAlbum(AlbumModel model)
    {
        if (string.IsNullOrEmpty(model.LocalDeviceId))
            model.LocalDeviceId = Guid.NewGuid().ToString();
        _albumRepo.AddOrUpdate(model);
    }
    
    public void UpSertSong(SongModel model)
    {
        if (string.IsNullOrEmpty(model.LocalDeviceId))
            model.LocalDeviceId = Guid.NewGuid().ToString();
        _songRepo.AddOrUpdate(model);
    }

    public void ToggleShuffle(bool isOn)
        => _settings.ShuffleOn = isOn;

    public RepeatMode ToggleRepeatMode()
    {
        var next = (RepeatMode)(((int)_settings.RepeatMode + 1) % 3);
        _settings.RepeatMode = next;
        return next;
    }

    #region Settings Region

    List<AlbumModel>? realmAlbums { get; set; }
    List<SongModel>? realmSongs { get; set; }
    List<GenreModel>? realGenres { get; set; }
    List<ArtistModel>? realmArtists { get; set; }
    List<AlbumArtistGenreSongLink>? realmAAGSL { get; set; }
    void GetInitialValues()
    {
        MasterList = [.. _songRepo
            .GetAll()
            .OrderBy(x => x.DateCreated)];
        
        realmSongs = [.. MasterList];
        realmAlbums = [.. _albumRepo.GetAll()];
        realGenres = [..  _genreRepo.GetAll()];
        realmArtists = [.. _artistRepo.GetAll()];
        realmAAGSL = [.. _aagslRepo.GetAll()];

    }

    public LoadSongsResult? LoadSongs(List<string> folderPaths)
    {
        List<string> allFiles = MusicFileProcessor.GetAllFiles(folderPaths);
        Debug.WriteLine("Got All Files");

         if (allFiles.Count == 0)
        {
            return null;
        }

        GetInitialValues();

        // Use existing data or empty lists if null.
        List<ArtistModel> existingArtists = realmArtists ?? new List<ArtistModel>();
        List<AlbumArtistGenreSongLink> existingLinks = realmAAGSL ?? new List<AlbumArtistGenreSongLink>();
        List<AlbumModel> existingAlbums = realmAlbums ?? new List<AlbumModel>();
        List<GenreModel> existingGenres = realGenres ?? new List<GenreModel>();
        List<SongModel> oldSongs = realmSongs ?? new List<SongModel>();

        List<ArtistModel> newArtists = new List<ArtistModel>();
        List<AlbumModel> newAlbums = new List<AlbumModel>();
        List<AlbumArtistGenreSongLink> newLinks = new List<AlbumArtistGenreSongLink>();
        List<GenreModel> newGenres = new List<GenreModel>();
        List<SongModel> allSongs = new List<SongModel>();

        // Dictionaries to prevent duplicate processing.
        Dictionary<string, ArtistModel> artistDict = new Dictionary<string, ArtistModel>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, AlbumModel> albumDict = new Dictionary<string, AlbumModel>();
        Dictionary<string, GenreModel> genreDict = new Dictionary<string, GenreModel>();

        int totalFiles = allFiles.Count;
        int processedFiles = 0;

        foreach (string file in allFiles)
        {
            processedFiles++;
            if (MusicFileProcessor.IsValidFile(file))
            {
                SongModel? songData = MusicFileProcessor.ProcessFile(
                    file,
                    existingAlbums, albumDict, newAlbums, oldSongs,
                    newArtists, artistDict, newLinks, existingLinks, existingArtists,
                    newGenres, genreDict, existingGenres);

                if (songData != null)
                {
                    allSongs.Add(songData);


                    var ProcessedFiles = processedFiles;
                    var TotalFiles = totalFiles;
                    var ProgressPercent = (double)processedFiles / totalFiles * 100.0;
                    _state.SetCurrentLogMsg($"Processing {songData.Title}" +
                        $"by {songData.ArtistName} {Environment.NewLine}" +
                        $"Processed {ProcessedFiles} of {TotalFiles} files" +
                        $"Progress: {ProgressPercent:F2}%");
                    }
                }
            }

        Debug.WriteLine("All files processed.");

        if (allSongs.Count<1)
        {
            return null;
        }
        MasterList= [.. allSongs];

        _state.SetCurrentPlaylist(Enumerable.Empty<SongModel>(), null); 

        _songRepo.AddOrUpdate(allSongs);

    _genreRepo.AddOrUpdate(newGenres);
    _aagslRepo.AddOrUpdate(newLinks);
    _albumRepo.AddOrUpdate(newAlbums);
    _artistRepo.AddOrUpdate(newArtists);
        return new LoadSongsResult
        {
            Artists = newArtists,
            Albums = newAlbums,
            Links = newLinks,
            Songs = allSongs,
            Genres = newGenres
        };
    }

    #endregion

    public void Dispose()
    {
        if (_disposed)
            return;
        _state.Dispose();
        _folderMonitor.Dispose();
        _disposed = true;
    }
}
