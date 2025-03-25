

namespace Dimmer_MAUI.Utilities.Services;
public partial class PlaybackUtilsService : ObservableObject, IPlaybackUtilsService
{

    // Declare a global subject for song loading progress.
    public Subject<SongLoadProgress> SongProgressSubject { get; } = new Subject<SongLoadProgress>();

    IDimmerAudioService DimmerAudioService;
    public IObservable<ObservableCollection<SongModelView>> NowPlayingSongs => _playbackQueue.AsObservable();
    BehaviorSubject<ObservableCollection<SongModelView>> _playbackQueue = new([]);
   
    public IObservable<ObservableCollection<SongModelView>> TertiaryQueue => _tertiaryQueueSubject.AsObservable();
    BehaviorSubject<ObservableCollection<SongModelView>> _tertiaryQueueSubject = new([]);

    public IObservable<MediaPlayerState> PlayerState => _playerStateSubject.AsObservable();
    BehaviorSubject<MediaPlayerState> _playerStateSubject = new(MediaPlayerState.Stopped);
    public IObservable<PlaybackInfo> CurrentPosition => _currentPositionSubject.AsObservable();
    BehaviorSubject<PlaybackInfo> _currentPositionSubject = new(new());
    System.Timers.Timer? _positionTimer;

    [ObservableProperty]
    private partial SongModelView? ObservableCurrentlyPlayingSong { get; set; } = new();
    public SongModelView? CurrentlyPlayingSong => ObservableCurrentlyPlayingSong;
    [ObservableProperty]
    private partial SongModelView? ObservablePreviouslyPlayingSong { get; set; } = new();
    public SongModelView? PreviouslyPlayingSong => ObservablePreviouslyPlayingSong;
    [ObservableProperty]
    private partial SongModelView? ObservableNextPlayingSong { get; set; } = new();
    public SongModelView? NextPlayingSong => ObservableNextPlayingSong;

    [ObservableProperty]
    private partial int ObservableLoadingSongsProgress { get; set; }
    public int LoadingSongsProgressPercentage => ObservableLoadingSongsProgress;

    [ObservableProperty]
    private partial string TotalSongsSizes { get; set; }
    [ObservableProperty]
    public partial RepeatMode CurrentRepeatMode { get; set; }

    [ObservableProperty]
    private partial string TotalSongsDuration { get; set; }
    ISongsManagementService SongsMgtService { get; }
    public IPlaylistManagementService PlaylistManagementService { get; }
    
    [ObservableProperty]
    public partial ObservableCollection<PlaylistModelView> AllPlaylists { get; set; } 
    [ObservableProperty]
    public partial ObservableCollection<ArtistModelView> AllArtists {get;set;} = Enumerable.Empty<ArtistModelView>().ToObservableCollection();
    [ObservableProperty]
    public partial ObservableCollection<AlbumModelView> AllAlbums {get;set;}=Enumerable.Empty<AlbumModelView>().ToObservableCollection();

    [ObservableProperty]
    public partial string? SelectedPlaylistName { get; set; }

    int _currentSongIndex;

    [ObservableProperty]
    public partial bool IsShuffleOn { get; set; }

    public int CurrentRepeatCount { get; set; } = 1;
    bool isSongPlaying;

    SortingEnum CurrentSorting;
    private Lazy<HomePageVM> ViewModel { get; set; }


    partial void OnObservableCurrentlyPlayingSongChanging(SongModelView? oldValue, SongModelView? newValue)
    {
        
    }
    public PlaybackUtilsService(IDimmerAudioService DimmerAudioService, ISongsManagementService SongsMgtService,
        IPlaylistManagementService playlistManagementService, Lazy<HomePageVM> viewModel)
    {
        this.SongsMgtService = SongsMgtService;
        PlaylistManagementService = playlistManagementService;

        this.DimmerAudioService = DimmerAudioService;

        this.DimmerAudioService.PlayPrevious += DimmerAudioService_PlayPrevious;
        this.DimmerAudioService.PlayNext += DimmerAudioService_PlayNext;
        this.DimmerAudioService.IsPlayingChanged += DimmerAudioService_PlayingChanged;
        this.DimmerAudioService.PlayEnded += DimmerAudioService_PlayEnded;
        this.DimmerAudioService.IsSeekedFromNotificationBar += DimmerAudioService_IsSeekedFromNotificationBar;

        IsShuffleOn = AppSettingsService.ShuffleStatePreference.GetShuffleState();
        CurrentRepeatMode = (RepeatMode) AppSettingsService.RepeatModePreference.GetRepeatState();

        CurrentQueue = 0; //0 = main queue, 1 = playlistQ, 2 = externallyloadedsongs Queue (though QueueNumber is now driven by PlaybackSource)

        LoadLastPlayedSong();
        LoadSongsWithSorting();

        LoadFirstPlaylist();
        AllPlaylists = PlaylistManagementService.AllPlaylists?.ToObservableCollection();
        AllArtists = SongsMgtService.AllArtists?.ToObservableCollection();
        this.ViewModel = viewModel;
    }

    private void DimmerAudioService_IsSeekedFromNotificationBar(object? sender, long e)
    {
        currentPositionInSec = e/1000;
    }


    #region Setups/Loadings Region


    public async Task<bool> LoadSongsFromFolder(List<string> folderPaths)
    {
        // Subscribe to the subject.
        SongProgressSubject.Subscribe(progress =>
        {
            ViewModel.Value.UpdateLatestScanData(progress);
        });
        await SongsMgtService.LoadSongsFromFolderAsync(folderPaths, SongProgressSubject);

        return true;
    }

    public ObservableCollection<SongModelView> ApplySorting(ObservableCollection<SongModelView> colToSort, SortingEnum mode, List<PlayDataLink> allPlayDataLinks)
    {
        IEnumerable<SongModelView> sortedSongs = colToSort;

        switch (mode)
        {
            case SortingEnum.TitleAsc:
                sortedSongs = colToSort.OrderBy(x => x.Title).ToObservableCollection();
                break;
            case SortingEnum.TitleDesc:
                sortedSongs = colToSort.OrderByDescending(x => x.Title).ToObservableCollection();
                break;
            case SortingEnum.ArtistNameAsc:
                sortedSongs = colToSort.OrderBy(x => x.ArtistName).ToObservableCollection();
                break;
            case SortingEnum.ArtistNameDesc:
                sortedSongs = colToSort.OrderByDescending(x => x.ArtistName).ToObservableCollection();
                break;
            case SortingEnum.DateAddedAsc:
                sortedSongs = colToSort.OrderBy(x => x.DateCreated).ToObservableCollection();
                break;
            case SortingEnum.DateAddedDesc:
                sortedSongs = colToSort.OrderByDescending(x => x.DateCreated).ToObservableCollection();
                break;
            case SortingEnum.DurationAsc:
                sortedSongs = colToSort.OrderBy(x => x.DurationInSeconds).ToObservableCollection();
                break;
            case SortingEnum.DurationDesc:
                sortedSongs = colToSort.OrderByDescending(x => x.DurationInSeconds).ToObservableCollection();
                break;
            case SortingEnum.YearAsc:
                sortedSongs = colToSort.OrderBy(x => x.ReleaseYear); // Corrected: Use ReleaseYear
                break;
            case SortingEnum.YearDesc:
                sortedSongs = colToSort.OrderByDescending(x => x.ReleaseYear); // Corrected: Use ReleaseYear
                break;

            // --- Play Count Sorting ---
            case SortingEnum.NumberOfTimesPlayedAsc:
                sortedSongs = colToSort.OrderBy(song => allPlayDataLinks.Count(link => link.SongId == song.LocalDeviceId));
                break;
            case SortingEnum.NumberOfTimesPlayedDesc:
                sortedSongs = colToSort.OrderByDescending(song => allPlayDataLinks.Count(link => link.SongId == song.LocalDeviceId));
                break;

            // --- Skipped Sorting ---
            case SortingEnum.MostSkippedAsc:
                sortedSongs = colToSort.OrderBy(song => allPlayDataLinks.Count(link => link.SongId == song.LocalDeviceId && !link.WasPlayCompleted));
                break;
            case SortingEnum.MostSkippedDesc:
                sortedSongs = colToSort.OrderByDescending(song => allPlayDataLinks.Count(link => link.SongId == song.LocalDeviceId && !link.WasPlayCompleted));
                break;

            // --- Played Completely Sorting ---
            case SortingEnum.MostPlayedCompletelyAsc:
                sortedSongs = colToSort.OrderBy(song => allPlayDataLinks.Count(link => link.SongId == song.LocalDeviceId && link.WasPlayCompleted));
                break;
            case SortingEnum.MostPlayedCompletelyDesc:
                sortedSongs = colToSort.OrderByDescending(song => allPlayDataLinks.Count(link => link.SongId == song.LocalDeviceId && link.WasPlayCompleted));
                break;

            // --- Played Incompletely Sorting ---
            case SortingEnum.MostPlayedIncompletelyAsc:
                sortedSongs = colToSort.OrderBy(song => allPlayDataLinks.Count(link => link.SongId == song.LocalDeviceId && !link.WasPlayCompleted));
                break;
            case SortingEnum.MostPlayedIncompletelyDesc:
                sortedSongs = colToSort.OrderByDescending(song => allPlayDataLinks.Count(link => link.SongId == song.LocalDeviceId && !link.WasPlayCompleted));
                break;


            case SortingEnum.RatingAsc:
                sortedSongs = colToSort.OrderBy(x => x.Rating);
                break;
            case SortingEnum.RatingDesc:
                sortedSongs = colToSort.OrderByDescending(x => x.Rating);
                break;


            default:
                break;
        }
        AppSettingsService.SortingModePreference.SetSortingPref(mode);
        return sortedSongs.ToObservableCollection(); // *Now* the sorting happens, all at once.
    }

    public void LoadSongsWithSorting(ObservableCollection<SongModelView>? songss = null, bool isFromSearch = false)
    {
        if (songss == null || songss.Count < 1)
        {
            if (SongsMgtService.AllSongs is null)
            {
                return;
            }
            songss = SongsMgtService.AllSongs.ToObservableCollection();
        }
        CurrentSorting = AppSettingsService.SortingModePreference.GetSortingPref();
        ObservableCollection<SongModelView> sortedSongs = ApplySorting(songss, CurrentSorting, SongsMgtService.AllPlayDataLinks);

        _playbackQueue.OnNext(sortedSongs);
        ToggleShuffle(IsShuffleOn);
    }

    public void FullRefresh()
    {

        if (SongsMgtService.AllSongs is null)
        {
            return;
        }

        ObservableCollection<SongModelView> songss = SongsMgtService.AllSongs.ToObservableCollection();
       
        CurrentSorting = AppSettingsService.SortingModePreference.GetSortingPref();
        ObservableCollection<SongModelView> sortedSongs = ApplySorting(songss, CurrentSorting, SongsMgtService.AllPlayDataLinks);
        
        _playbackQueue.OnNext(sortedSongs);

        
        
        ToggleShuffle(IsShuffleOn);
    }

    public SongModelView? lastPlayedSong { get; set; }

    private void LoadLastPlayedSong()
    {
        if (SongsMgtService.AllSongs is null)
        {
            return;
        }
        string? lastPlayedSongID = AppSettingsService.LastPlayedSongSettingPreference.GetLastPlayedSong();
        if (lastPlayedSongID is not null)
        {
            lastPlayedSong = SongsMgtService.AllSongs.FirstOrDefault(x => x.LocalDeviceId == (string)lastPlayedSongID);
            
            if (lastPlayedSong is null)
            {
                return;
            }
            ObservableCurrentlyPlayingSong = lastPlayedSong!;
            //DimmerAudioService.Initialize(ObservableCurrentlyPlayingSong);
            //_nowPlayingSubject.OnNext(ObservableCurrentlyPlayingSong);
            _currentSongIndex = SongsMgtService.AllSongs.IndexOf(ObservableCurrentlyPlayingSong);
        }

        _playerStateSubject.OnNext(MediaPlayerState.Initialized);
    }

    public static ObservableCollection<SongModelView> CheckCoverImage(ObservableCollection<SongModelView> col) 
    {
        foreach (SongModelView item in col)
        {
            item.CoverImagePath = GetCoverImagePath(item.FilePath);

        }
        

        return col;
    }
    public static string? GetCoverImagePath(string filePath)
    {
        Track LoadTrack = new Track(filePath);
        byte[]? coverImage = null;

        if (LoadTrack.EmbeddedPictures?.Count > 0)
        {
            string? mimeType = LoadTrack.EmbeddedPictures?.FirstOrDefault()?.MimeType;
            if (mimeType == "image/jpg" || mimeType == "image/jpeg" || mimeType == "image/png")
            {
                coverImage = LoadTrack.EmbeddedPictures?.FirstOrDefault()?.PictureData;
            }
        }

        if(coverImage is not null)
        {
            return LyricsService.SaveOrGetCoverImageToFilePath(filePath, coverImage);
        }

        if (coverImage is null || coverImage.Length < 1)
        {
            string fileNameWithoutExtension = Path.GetFileName(filePath);

#if ANDROID && NET9_0
            string folderPath = Path.Combine(FileSystem.AppDataDirectory, "CoverImagesDimmer"); // Use AppDataDirectory for Android compatibility
#elif WINDOWS && NET9_0
        string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "DimmerDB", "CoverImagesDimmer");
#endif


            // Ensure the directory exists
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }


            string[] imageFiles =
            [
                .. Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.jpg", SearchOption.TopDirectoryOnly)
,
                .. Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.jpeg", SearchOption.TopDirectoryOnly),
                .. Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.png", SearchOption.TopDirectoryOnly),
            ];

            if (imageFiles.Length > 0)
            {                
                return imageFiles.ToString();
            }
        }

        if (coverImage is null)
        {
            return null;
        }

        return null;
    }
    byte[]? GetCoverImage(string? filePath, bool isToGetByteArrayImages)
    {
        Track LoadTrack = new Track(filePath);
        byte[]? coverImage = null;

        if (LoadTrack.EmbeddedPictures?.Count > 0)
        {
            string? mimeType = LoadTrack.EmbeddedPictures?.FirstOrDefault()?.MimeType;
            if (mimeType == "image/jpg" || mimeType == "image/jpeg" || mimeType == "image/png")
            {
                coverImage = LoadTrack.EmbeddedPictures?.FirstOrDefault()?.PictureData;
            }
        }


        if (coverImage is null || coverImage.Length < 1)
        {
            string fileNameWithoutExtension = Path.GetFileName(filePath);
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DimmerDB", "CoverImagesDimmer");
            // Ensure the directory exists
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }


            string[] imageFiles =
            [
                .. Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.jpg", SearchOption.TopDirectoryOnly)
,
                .. Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.jpeg", SearchOption.TopDirectoryOnly),
                .. Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.png", SearchOption.TopDirectoryOnly),
            ];

            if (imageFiles.Length > 0)
            {
                coverImage = File.ReadAllBytes(imageFiles[0]);

                if (!string.IsNullOrEmpty(ObservableCurrentlyPlayingSong.CoverImagePath))
                {
                    ObservableCurrentlyPlayingSong.CoverImagePath = imageFiles[0];

                }
                return coverImage;
            }
        }

        if (coverImage is null)
        {
            _playerStateSubject.OnNext(MediaPlayerState.CoverImageDownload);
        }

        return coverImage;
    }
    #endregion



    //string PreviouslyLoadedPlaylist;
    public int CurrentQueue { get; set; } = 0; //0 = main queue, 1 = playlistQ, 2 = externallyloadedsongs Queue

    public void UpdateCurrentQueue(IList<SongModelView> songs, int QueueNumber = 1) //0 = main queue, 1 = playlistQ, 2 = externallyloadedsongs Queue
    {
        CurrentQueue = QueueNumber;
        _playbackQueue.OnNext(value: songs.ToObservableCollection());
    }
    public void UpdateSongToFavoritesPlayList(SongModelView song)
    {
        if (song is not null)
        {
            SongsMgtService.UpdateSongDetails(song);
        }
    }
    public void AddSongToQueue(SongModelView song)
    {
        //ObservableCollection<SongModelView>? list = _playbackQueue.Value;
        //list.Add(song);
        //_playbackQueue.OnNext(list);


        ObservableCollection<SongModelView> list = _playbackQueue.Value;

        // Find the current index of the song
        int currentIndex = list.IndexOf(ObservableCurrentlyPlayingSong);

        if (currentIndex >= 0 && currentIndex < list.Count - 1)
        {
            // Move to the next index
            SongModelView nextSong = list[currentIndex + 1];

            // Perform operations with `nextSong` as needed
            Console.WriteLine($"Moving to next song: {nextSong.Title}");

            // Optionally, remove the current song if needed
             list.Insert((currentIndex+1), song); 

            // Update the playback queue
            _playbackQueue.OnNext([.. list]);
        }
        else
        {
            Console.WriteLine("No next song available or invalid current song.");
        }
    }
    public void RemoveSongFromQueue(SongModelView song)
    {
        ObservableCollection<SongModelView> list = _playbackQueue.Value;
        list.Remove(song);
        _playbackQueue.OnNext(list);
    }

    #region Region Search

    Dictionary<string, string> normalizationCache = [];
    List<SongModelView> SearchedSongsList;

    public void SearchSong(string songTitleOrArtistName, List<string>? selectedFilters, int Rating)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(songTitleOrArtistName) && selectedFilters?.Count == 0)
            {
                ResetSearch();
                return;
            }

            // Normalize the search term
            string normalizedSearchTerm = NormalizeAndCache(songTitleOrArtistName ?? string.Empty).ToLowerInvariant();

            // Clear the search result list
            SearchedSongsList?.Clear();

            // Step 1: Start with all songs and apply the rating filter
            IEnumerable<SongModelView> filteredSongs = SongsMgtService.AllSongs
                .Where(s => s.Rating >= Rating);

            // Step 2: Apply additional filters from selectedFilters if any
            if (selectedFilters?.Count > 0)
            {
                filteredSongs = filteredSongs.Where(s => selectedFilters.Contains("Artist")
                                                        || selectedFilters.Contains("Album")
                                                        || selectedFilters.Contains("Genre"));
            }

            // Step 3: Perform the search with normalization and comparison on the filtered list
            SearchedSongsList = [.. filteredSongs
                .Where(s => NormalizeAndCache(s.Title).ToLowerInvariant().Contains(normalizedSearchTerm)
                            || (s.ArtistName != null && NormalizeAndCache(s.ArtistName).ToLowerInvariant().Contains(normalizedSearchTerm))
                            || (s.AlbumName != null && NormalizeAndCache(s.AlbumName).ToLowerInvariant().Contains(normalizedSearchTerm)))];

            // Step 4: Load the results with sorting
            Debug.WriteLine(SearchedSongsList.Count + "Search Count");
            LoadSongsWithSorting(SearchedSongsList.ToObservableCollection(), true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }


    private string NormalizeAndCache(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // Retrieve from cache if already normalized
        if (normalizationCache.TryGetValue(text, out string? cachedValue))
        {
            return cachedValue;
        }

        // Normalize the string (spaces are preserved)
        string normalizedString = text.Normalize(NormalizationForm.FormD);
        StringBuilder stringBuilder = new StringBuilder();

        foreach (char c in normalizedString)
        {
            // Retain characters that are not non-spacing marks
            UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        // Convert back to Form C and cache the result
        string result = stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        normalizationCache[text] = result;
        return result;
    }

    void ResetSearch()
    {
        SearchedSongsList?.Clear();
        LoadSongsWithSorting();
    }


    #endregion

    public void LoadFirstPlaylist()
    {
        List<PlaylistModelView>? firstPlaylist = PlaylistManagementService?.AllPlaylists?.ToList();
        if (firstPlaylist is not null && firstPlaylist.Count > 0)
        {           
            SelectedPlaylistName = firstPlaylist.FirstOrDefault().Name;
            GetSongsFromPlaylistID(firstPlaylist.FirstOrDefault().LocalDeviceId);
        }
    }

    public void AddSongsToPlaylist(List<string> songIDs, PlaylistModelView playlistModel, bool IsExistingPL = false)
    {
        if (!IsExistingPL)
        {
            PlaylistManagementService.AddSongsToPlaylist(playlistModel, songIDs);
            return;
        }
        PlaylistModelView? anyExistingPlaylist = PlaylistManagementService.AllPlaylists.FirstOrDefault(x=>x.Name == playlistModel.Name);
        if (anyExistingPlaylist is not null)
        {
            playlistModel = anyExistingPlaylist;
        }
        else
        {         
            playlistModel.Name = playlistModel.Name;
        }
        PlaylistSongLink newPlaylistSongLinkByUserManual = new PlaylistSongLink()
        {
            PlaylistId = playlistModel.LocalDeviceId,            
        };

        PlaylistManagementService.AddSongsToPlaylist(playlistModel, songIDs);
        
    }

    public void RemoveSongFromPlayListWithPlayListID(List<string> songIDs, PlaylistModelView playlistModel, bool IsExistingPL = true)
    {
        if (IsExistingPL)
        {
            PlaylistManagementService.RemoveSongsFromPlaylist(playlistModel.LocalDeviceId!, songIDs);
            return;
        }
        PlaylistModelView? anyExistingPlaylist = PlaylistManagementService.AllPlaylists.FirstOrDefault(x => x.Name == playlistModel.Name);
        if (anyExistingPlaylist is not null)
        {
            playlistModel = anyExistingPlaylist;
        }
        else
        {
            playlistModel.Name = playlistModel.Name;
        }
        PlaylistSongLink newPlaylistSongLinkByUserManual = new PlaylistSongLink()
        {
            PlaylistId = playlistModel.LocalDeviceId,
        };

        PlaylistManagementService.RemoveSongsFromPlaylist(playlistModel.LocalDeviceId!, songIDs);

      
    }
    public List<SongModelView> GetSongsFromPlaylistID(string playlistID)
    {
        try
        {
            PlaylistModelView? specificPlaylist = PlaylistManagementService.AllPlaylists
                .FirstOrDefault(x => x.LocalDeviceId == playlistID);
            if (specificPlaylist is null)
            {
                return new List<SongModelView>();
            }

            HashSet<string> songsIdsFromPL = new HashSet<string>(
                PlaylistManagementService.GetSongIdsForPlaylist(specificPlaylist.LocalDeviceId));

            List<SongModelView>? songsFromPlaylist = SongsMgtService.AllSongs;
            if (songsFromPlaylist is null)
            {
                return new List<SongModelView>();
            }

            // Materialize the filtered list first.
            List<SongModelView> songsInPlaylist = songsFromPlaylist
                .Where(song => song != null && songsIdsFromPL.Contains(song.LocalDeviceId))
                .ToList();

            SelectedPlaylistName = specificPlaylist.Name;
            // Create a new ObservableCollection from the list instead of using ToObservableCollection()
            _playbackQueue.OnNext(new ObservableCollection<SongModelView>(songsInPlaylist));

            return songsInPlaylist;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting songs from playlist: {ex.Message}");
            return new List<SongModelView>();
        }
    }



    public ObservableCollection<PlaylistModelView> GetAllPlaylists()
    {
        PlaylistManagementService.GetPlaylists();
        AllPlaylists = [.. PlaylistManagementService.AllPlaylists];
        return AllPlaylists;
    }

    public ObservableCollection<ArtistModelView> GetAllArtists()
    {
        SongsMgtService.GetArtists();
        if (SongsMgtService.AllArtists is null)
            return Enumerable.Empty<ArtistModelView>().ToObservableCollection();

        AllArtists = [.. SongsMgtService.AllArtists];
        return SongsMgtService.AllArtists.ToObservableCollection();
    }
    public ObservableCollection<AlbumModelView> GetAllAlbums()
    {        
        AllAlbums = [.. SongsMgtService.AllAlbums];
        return AllAlbums.ToObservableCollection();
        ;
    }
    


    public bool DeletePlaylistThroughID(string playlistID)
    {


        PlaylistModelView? pl = PlaylistManagementService.AllPlaylists?.First(x => x.LocalDeviceId == playlistID!);
        PlaylistManagementService.AllPlaylists?.Remove(pl);

        return true;
    }

    public void DeleteSongFromHomePage(SongModelView song)
    {
        // Get the current list from the subject
        List<SongModelView> list = _playbackQueue.Value.ToList();

        // Find the index of the song using its unique ID
        int index = list.FindIndex(x => x.LocalDeviceId == song.LocalDeviceId); // Assuming each song has a unique Id

        if (index != -1) // If the song was found
        {
            list.RemoveAt(index); // Remove the song at the found index

            // Push the updated list back into the subject
            _playbackQueue.OnNext(list.ToObservableCollection());
        }

        SongsMgtService.DeleteSongFromDB(song);
        _playbackQueue.OnNext(SongsMgtService.AllSongs.ToObservableCollection());

    }
    public void MultiDeleteSongFromHomePage(ObservableCollection<SongModelView> songs)
    {
        // Get the current list from the subject
        ObservableCollection<SongModelView> list = _playbackQueue.Value;

        // Filter out all songs in the passed collection
        list = list.Where(x => !songs.Any(s => s.LocalDeviceId == x.LocalDeviceId)).ToObservableCollection();
        
        _playbackQueue.OnNext(list.ToObservableCollection());        

        // Delete the songs from the database
        SongsMgtService.MultiDeleteSongFromDB(songs);

        // Update the _nowPlayingSubject with the latest list of songs
        _playbackQueue.OnNext(SongsMgtService.AllSongs.ToObservableCollection());
    }

   
}