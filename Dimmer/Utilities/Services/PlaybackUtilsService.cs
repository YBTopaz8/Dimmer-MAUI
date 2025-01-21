

namespace Dimmer_MAUI.Utilities.Services;
public partial class PlaybackUtilsService : ObservableObject, IPlaybackUtilsService
{

    INativeAudioService AudioService;
    public IObservable<ObservableCollection<SongModelView>> NowPlayingSongs => _playbackQueue.AsObservable();
    BehaviorSubject<ObservableCollection<SongModelView>> _playbackQueue = new([]);
   
    
    public IObservable<ObservableCollection<SongModelView>> SecondaryQueue => _secondaryQueueSubject.AsObservable();
    BehaviorSubject<ObservableCollection<SongModelView>> _secondaryQueueSubject = new([]);
    public IObservable<ObservableCollection<SongModelView>> TertiaryQueue => _tertiaryQueueSubject.AsObservable();
    BehaviorSubject<ObservableCollection<SongModelView>> _tertiaryQueueSubject = new([]);

    public IObservable<MediaPlayerState> PlayerState => _playerStateSubject.AsObservable();
    BehaviorSubject<MediaPlayerState> _playerStateSubject = new(MediaPlayerState.Stopped);
    public IObservable<PlaybackInfo> CurrentPosition => _currentPositionSubject.AsObservable();
    BehaviorSubject<PlaybackInfo> _currentPositionSubject = new(new());
    System.Timers.Timer? _positionTimer;

    
    private SongModelView ObservableCurrentlyPlayingSong { get; set; } = new();
    public SongModelView CurrentlyPlayingSong => ObservableCurrentlyPlayingSong;
    [ObservableProperty]
    private partial SongModelView? ObservablePreviouslyPlayingSong { get; set; } = new();
    public SongModelView PreviouslyPlayingSong => ObservablePreviouslyPlayingSong;
    [ObservableProperty]
    private partial SongModelView? ObservableNextPlayingSong { get; set; } = new();
    public SongModelView NextPlayingSong => ObservableNextPlayingSong;

    [ObservableProperty]
    private partial int ObservableLoadingSongsProgress { get; set; }
    public int LoadingSongsProgressPercentage => ObservableLoadingSongsProgress;

    [ObservableProperty]
    string totalSongsSizes;
    [ObservableProperty]
    string totalSongsDuration;
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
    [ObservableProperty]
    public partial int CurrentRepeatMode { get; set; }
    public int CurrentRepeatCount { get; set; } = 1;
    bool isSongPlaying;

    SortingEnum CurrentSorting;
    private Lazy<HomePageVM> ViewModel { get; set; }

    public PlaybackUtilsService(INativeAudioService AudioService, ISongsManagementService SongsMgtService,
        IPlaylistManagementService playlistManagementService, Lazy<HomePageVM> viewModel)
    {
        this.SongsMgtService = SongsMgtService;
        PlaylistManagementService = playlistManagementService;

        this.AudioService = AudioService;

        this.AudioService.PlayPrevious += AudioService_PlayPrevious;
        this.AudioService.PlayNext += AudioService_PlayNext;
        this.AudioService.IsPlayingChanged += AudioService_PlayingChanged;
        this.AudioService.PlayEnded += AudioService_PlayEnded;
        this.AudioService.IsSeekedFromNotificationBar += AudioService_IsSeekedFromNotificationBar;

        IsShuffleOn = AppSettingsService.ShuffleStatePreference.GetShuffleState();
        CurrentRepeatMode = AppSettingsService.RepeatModePreference.GetRepeatState();

        CurrentQueue = 0; //0 = main queue, 1 = playlistQ, 2 = externallyloadedsongs Queue

        LoadLastPlayedSong();
        LoadSongsWithSorting();

        LoadFirstPlaylist();
        AllPlaylists = PlaylistManagementService.AllPlaylists?.ToObservableCollection();
        AllArtists = SongsMgtService.AllArtists?.ToObservableCollection();
        this.ViewModel = viewModel;
    }

    private void AudioService_IsSeekedFromNotificationBar(object? sender, long e)
    {
        currentPositionInSec = e/1000;
    }


    #region Setups/Loadings Region


    public bool LoadSongsFromFolder(List<string> folderPaths)
    {
        SongsMgtService.LoadSongsFromFolderAsync(folderPaths);

        return true;
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
        var sortedSongs = AppSettingsService.ApplySorting(songss, CurrentSorting);
        
        _playbackQueue.OnNext(sortedSongs);
        ToggleShuffle(IsShuffleOn);
    }

    public void FullRefresh()
    {
       
       var songss = SongsMgtService.AllSongs.ToObservableCollection();
       
        CurrentSorting = AppSettingsService.SortingModePreference.GetSortingPref();
        var sortedSongs = AppSettingsService.ApplySorting(songss, CurrentSorting);
        
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
        var lastPlayedSongID = AppSettingsService.LastPlayedSongSettingPreference.GetLastPlayedSong();
        if (lastPlayedSongID is not null)
        {
            lastPlayedSong = SongsMgtService.AllSongs.FirstOrDefault(x => x.LocalDeviceId == (string)lastPlayedSongID);
            
            if (lastPlayedSong is null)
            {
                return;
            }
            ObservableCurrentlyPlayingSong = lastPlayedSong!;
            //audioService.Initialize(ObservableCurrentlyPlayingSong);
            //_nowPlayingSubject.OnNext(ObservableCurrentlyPlayingSong);
            _currentSongIndex = SongsMgtService.AllSongs.IndexOf(ObservableCurrentlyPlayingSong);
        }

        _playerStateSubject.OnNext(MediaPlayerState.Initialized);
    }

    public static ObservableCollection<SongModelView> CheckCoverImage(ObservableCollection<SongModelView> col) 
    {
        foreach (var item in col)
        {
            item.CoverImagePath = GetCoverImagePath(item.FilePath);

        }
        

        return col;
    }
    public static string? GetCoverImagePath(string filePath)
    {
        var LoadTrack = new Track(filePath);
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


            string[] imageFiles = Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.jpg", SearchOption.TopDirectoryOnly)
                .Concat(Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.jpeg", SearchOption.TopDirectoryOnly))
                .Concat(Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.png", SearchOption.TopDirectoryOnly))
                .ToArray();

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
    byte[]? GetCoverImage(string filePath, bool isToGetByteArrayImages)
    {
        var LoadTrack = new Track(filePath);
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


            string[] imageFiles = Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.jpg", SearchOption.TopDirectoryOnly)
                .Concat(Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.jpeg", SearchOption.TopDirectoryOnly))
                .Concat(Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.png", SearchOption.TopDirectoryOnly))
                .ToArray();

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
        _secondaryQueueSubject.OnNext(value: songs.ToObservableCollection());
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


        var list = _playbackQueue.Value;

        // Find the current index of the song
        int currentIndex = list.IndexOf(ObservableCurrentlyPlayingSong);

        if (currentIndex >= 0 && currentIndex < list.Count - 1)
        {
            // Move to the next index
            var nextSong = list[currentIndex + 1];

            // Perform operations with `nextSong` as needed
            Console.WriteLine($"Moving to next song: {nextSong.Title}");

            // Optionally, remove the current song if needed
             list.Insert((currentIndex+1), song); 

            // Update the playback queue
            _playbackQueue.OnNext(new ObservableCollection<SongModelView>(list));
        }
        else
        {
            Console.WriteLine("No next song available or invalid current song.");
        }
    }
    public void RemoveSongFromQueue(SongModelView song)
    {
        var list = _playbackQueue.Value;
        list.Remove(song);
        _playbackQueue.OnNext(list);
    }

    #region Region Search

    Dictionary<string, string> normalizationCache = new();
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
            var filteredSongs = SongsMgtService.AllSongs
                .Where(s => s.Rating >= Rating);

            // Step 2: Apply additional filters from selectedFilters if any
            if (selectedFilters?.Count > 0)
            {
                filteredSongs = filteredSongs.Where(s => selectedFilters.Contains("Artist")
                                                        || selectedFilters.Contains("Album")
                                                        || selectedFilters.Contains("Genre"));
            }

            // Step 3: Perform the search with normalization and comparison on the filtered list
            SearchedSongsList = filteredSongs
                .Where(s => NormalizeAndCache(s.Title).ToLowerInvariant().Contains(normalizedSearchTerm)
                            || (s.ArtistName != null && NormalizeAndCache(s.ArtistName).ToLowerInvariant().Contains(normalizedSearchTerm))
                            || (s.AlbumName != null && NormalizeAndCache(s.AlbumName).ToLowerInvariant().Contains(normalizedSearchTerm)))
                .ToList();

            // Step 4: Load the results with sorting
            Debug.WriteLine(SearchedSongsList.Count + "Search Count");
            LoadSongsWithSorting(SearchedSongsList.ToObservableCollection(), true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }


    private string NormalizeAndCache(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Retrieve from cache if already normalized
        if (normalizationCache.TryGetValue(text, out string? cachedValue))
        {
            return cachedValue;
        }

        // Normalize the string (spaces are preserved)
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            // Retain characters that are not non-spacing marks
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        // Convert back to Form C and cache the result
        var result = stringBuilder.ToString().Normalize(NormalizationForm.FormC);
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
        var firstPlaylist = PlaylistManagementService?.AllPlaylists?.ToList();
        if (firstPlaylist is not null && firstPlaylist.Count > 0)
        {           
            SelectedPlaylistName = firstPlaylist.First().Name;
            GetSongsFromPlaylistID(firstPlaylist.FirstOrDefault().LocalDeviceId);
        }
    }

    public void AddSongToPlayListWithPlayListID(SongModelView song, PlaylistModelView playlistModel)
    {
        var anyExistingPlaylist = PlaylistManagementService.AllPlaylists.FirstOrDefault(x=>x.Name == playlistModel.Name);
        if (anyExistingPlaylist is not null)
        {
            playlistModel = anyExistingPlaylist;
        }
        else
        {         
            playlistModel.Name = playlistModel.Name;
         
            playlistModel.TotalSongsCount = 1;
            playlistModel.TotalDuration = song.DurationInSeconds;
            playlistModel.TotalSize = song.FileSize;
        
        }
        var newPlaylistSongLinkByUserManual = new PlaylistSongLink()
        {            
            PlaylistId = playlistModel.LocalDeviceId,
            SongId = song.LocalDeviceId,
        };

        PlaylistManagementService.UpdatePlayList(playlistModel, newPlaylistSongLinkByUserManual, true);
        
        //GetAllPlaylists(); it's called in HomePageVM but let's see
    }

    public void RemoveSongFromPlayListWithPlayListID(SongModelView song, string playlistID)
    {
        var playlists = PlaylistManagementService.GetPlaylists();
        var specificPlaylist = playlists.FirstOrDefault(x => x.LocalDeviceId == playlistID);
        if (specificPlaylist is not null)
        {
            var songsInPlaylist = _secondaryQueueSubject.Value;
            songsInPlaylist.Remove(song);
            _secondaryQueueSubject.OnNext(songsInPlaylist);
            specificPlaylist.TotalSongsCount -= 1;
            specificPlaylist.TotalDuration -= song.DurationInSeconds;
            specificPlaylist.TotalSize -= song.FileSize;
        }

        PlaylistManagementService.UpdatePlayList(specificPlaylist, IsRemoveSong: true);
    }

    public List<SongModelView> GetSongsFromPlaylistID(string playlistID)
    {
        try
        {
            var specificPlaylist = PlaylistManagementService.AllPlaylists.FirstOrDefault(x => x.LocalDeviceId == playlistID);
            var songsIdsFromPL = new HashSet<string>(PlaylistManagementService.GetSongsIDsFromPlaylistID(specificPlaylist.LocalDeviceId));
            var songsFromPlaylist = SongsMgtService.AllSongs;
            List<SongModelView> songsinpl = new();
            songsinpl?.Clear();
            foreach (var song in songsFromPlaylist)
            {
                if (song is null)
                {
                    continue;
                }
                if (songsIdsFromPL.Contains(song.LocalDeviceId))
                {
                    songsinpl.Add(song);
                }
            }
            SelectedPlaylistName = specificPlaylist.Name;
            
            if (songsinpl is null)
            {
                return Enumerable.Empty<SongModelView>().ToList();
            }
            _secondaryQueueSubject.OnNext(songsinpl.ToObservableCollection());
            return songsinpl;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting songs from playlist: {ex.Message}");
            return Enumerable.Empty<SongModelView>().ToList();
        }

    }

    public ObservableCollection<PlaylistModelView> GetAllPlaylists()
    {
        PlaylistManagementService.GetPlaylists();
        AllPlaylists = new ObservableCollection<PlaylistModelView>(PlaylistManagementService.AllPlaylists);
        return AllPlaylists;
    }

    public ObservableCollection<ArtistModelView> GetAllArtists()
    {
        SongsMgtService.GetArtists();
        if (SongsMgtService.AllArtists is null)
            return Enumerable.Empty<ArtistModelView>().ToObservableCollection();

        AllArtists = new ObservableCollection<ArtistModelView>(SongsMgtService.AllArtists);
        return AllArtists;
    }
    public ObservableCollection<AlbumModelView> GetAllAlbums()
    {        
        AllAlbums = new ObservableCollection<AlbumModelView>(SongsMgtService.AllAlbums);
        return AllAlbums;
    }
    


    public bool DeletePlaylistThroughID(string playlistID)
    {
        //TODO: DO THIS TOO
        throw new NotImplementedException();
    }

    public void DeleteSongFromHomePage(SongModelView song)
    {
        // Get the current list from the subject
        var list = _playbackQueue.Value.ToList();

        // Find the index of the song using its unique ID
        var index = list.FindIndex(x => x.LocalDeviceId == song.LocalDeviceId); // Assuming each song has a unique Id

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
        var list = _playbackQueue.Value;

        // Filter out all songs in the passed collection
        list = list.Where(x => !songs.Any(s => s.LocalDeviceId == x.LocalDeviceId)).ToObservableCollection();
        
        _playbackQueue.OnNext(list.ToObservableCollection());        

        // Delete the songs from the database
        SongsMgtService.MultiDeleteSongFromDB(songs);

        // Update the _nowPlayingSubject with the latest list of songs
        _playbackQueue.OnNext(SongsMgtService.AllSongs.ToObservableCollection());
    }

}