//using WinRT;

namespace Dimmer_MAUI.ViewModels;

public partial class HomePageVM
{
    [ObservableProperty]
    public partial string? SearchPlaceHolder { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<ArtistModelView> AllArtists { get; set; } = new();
    [ObservableProperty]
    public partial ObservableCollection<AlbumModelView> AllAlbums{get;set;}= new();
    [ObservableProperty]
    public partial ObservableCollection<SongModelView> AllArtistsAlbumSongs {get;set;}= new();
    [ObservableProperty]
    public partial ObservableCollection<SongModelView> AllArtistsSongs {get;set;}= new();
    [ObservableProperty]
    public partial ObservableCollection<AlbumModelView> AllArtistsAlbums {get;set;}= new();
    [ObservableProperty]
    public partial string? SelectedArtistPageTitle { get; set; }

    [ObservableProperty]
    public partial AlbumModelView? SelectedAlbumOnArtistPage { get; set; }
    [ObservableProperty]
    public partial ArtistModelView? SelectedArtistOnArtistPage { get; set; }
    [ObservableProperty]
    public partial AlbumModelView? SelectedAlbumOnAlbumPage { get; set; }
    [ObservableProperty]
    public partial ArtistModelView? SelectedArtistOnAlbumPage { get; set; }

    [RelayCommand]
    public async Task NavigateToSpecificAlbumPageFromBtmSheet(SongModelView song)
    {
        MySelectedSong = song;
        var songAlbum = GetAlbumFromSongID(song.LocalDeviceId!).First();
        
        await NavigateToSpecificAlbumPage(songAlbum);

    }

    
    [RelayCommand]
    async Task NavigateToSpecificAlbumPage(AlbumModelView selectedAlbum)
    {
        SelectedArtistOnArtistPage = GetAllArtistsFromAlbumID(selectedAlbum.LocalDeviceId).First();
        SelectedAlbumOnArtistPage = AllAlbums.First(x => x.LocalDeviceId == selectedAlbum.LocalDeviceId);
        //SelectedAlbumOnArtistPage.NumberOfTracks = SongsMgtService.GetSongsCountFromAlbumID(selectedAlbum.LocalDeviceId);
        SelectedAlbumOnArtistPage.AlbumImagePath = SongsMgtService.AllSongs.First(x => x.AlbumName == SelectedAlbumOnArtistPage.Name).CoverImagePath;
        SelectedAlbumOnArtistPage.TotalDuration = TimeSpan
            .FromSeconds(SongsMgtService.AllSongs
            .Where(x => x.AlbumName == SelectedAlbumOnArtistPage.Name)
            .Sum(x => x.DurationInSeconds))
            .ToString(@"mm\:ss");

        await Shell.Current.GoToAsync(nameof(SpecificAlbumPage));
        await ShowSpecificArtistsSongsWithAlbum(selectedAlbum);
    }
   
    [RelayCommand]
    public async Task NavigateToArtistsPage(int? callerID=0) //0 if called by else, 1 if called by homeD or homeM
    {

        if (callerID == 0)
        {
            MySelectedSong = TemporarilyPickedSong!;
        }
       
#if WINDOWS
        await Shell.Current.GoToAsync(nameof(ArtistsPageD));
#elif ANDROID
        await Shell.Current.GoToAsync(nameof(ArtistsPageM));
        SelectedArtistOnArtistPage = GetAllArtistsFromSongID(MySelectedSong!.LocalDeviceId!).First();
        GetAllArtistAlbumFromArtistModel(SelectedArtistOnArtistPage);
#endif

    }
    public void LoadArtistSongs()
    {
        if (SelectedArtistOnArtistPage is not null && SelectedArtistOnArtistPage.Name == MySelectedSong.ArtistName)
        {
            if (AllArtistsAlbumSongs.Count > 1)
            {
                return;
            }
        }
        SelectedArtistOnArtistPage = GetAllArtistsFromSongID(MySelectedSong!.LocalDeviceId!).First();
        GetAllArtistAlbumFromArtistModel(SelectedArtistOnArtistPage);
    }
    [RelayCommand]
    public async Task NavigateToAlbumsPage(int? callerID=0) //0 if called by else, 1 if called by homeD or homeM
    {

        if (callerID == 0)
        {
            MySelectedSong = TemporarilyPickedSong!;
        }
       
#if WINDOWS
        await Shell.Current.GoToAsync(nameof(AlbumsPageD));
#elif ANDROID
        await Shell.Current.GoToAsync(nameof(AlbumPageM));
        
#endif

    }
    
    public void GetAllAlbums()
    {
        AllAlbums = PlayBackService.GetAllAlbums();
    }
    [RelayCommand]
    void GetAllArtists()
    {
        
        if (SelectedArtistOnArtistPage != null)
            SelectedArtistOnArtistPage.IsCurrentlySelected = true;
        if (SelectedAlbumOnArtistPage != null)
            SelectedAlbumOnArtistPage.IsCurrentlySelected = true;
        if (AllArtists?.Count != PlayBackService.GetAllArtists().Count)
        {
            AllArtists = PlayBackService
                .GetAllArtists()
                .OrderBy(x => x.Name)
                .ToObservableCollection();
            if (AllArtists.Count > 0)
            {
#if WINDOWS
                if(SelectedArtistOnArtistPage is not null)
                {
                    SelectedArtistOnArtistPage.IsCurrentlySelected = true;                    
                    var song = DisplayedSongs.FirstOrDefault(x => x.ArtistName == SelectedArtistOnArtistPage.Name);
                    GetAllArtistsAlbum(song: song, isFromSong: true);
                    
                }
#endif
            }
        }

    }

    public void GetAllArtistsAlbum(AlbumModelView? album = null, SongModelView? song = null, bool isFromSong = false)
    {
        //if(!SongsMgtService.AllSongs.Contains(TemporarilyPickedSong!))
        //    return;
        if (DisplayedSongs is null)
        {
            return;
        }
        if(SelectedAlbumOnArtistPage is not null)
            SelectedAlbumOnArtistPage.IsCurrentlySelected = false;
        if (isFromSong)
        {
            if (song is null)
            {
                return;
            }
            var AllArtistsFromSong = GetAllArtistsFromSongID(song.LocalDeviceId);
            SelectedArtistOnArtistPage = AllArtistsFromSong.Count > 0 ? AllArtistsFromSong.FirstOrDefault() : null;
            if (SelectedArtistOnArtistPage is not null)
            {
                SelectedArtistOnArtistPage.IsCurrentlySelected = true;
            }
            else
            {

                if (string.IsNullOrEmpty(song.FilePath))
                {
                    return;
                }
                SongsMgtService.LoadSongsFromFolderAsync(new List<string>() { song.FilePath });
                
                //GeneralStaticUtilities.ProcessFile(song);
            }
            
        }
        else if(album is not null)
        {
            SelectedAlbumOnArtistPage = album!;            
            SelectedArtistOnArtistPage = GetAllArtistsFromAlbumID(album!.LocalDeviceId).First();
            SelectedArtistOnArtistPage.IsCurrentlySelected = true;
            
            if (SelectedAlbumOnArtistPage is null)
            {
                return;
            }
        }
        else
        {
            SelectedAlbumOnArtistPage = GetAlbumFromSongID(TemporarilyPickedSong!.LocalDeviceId!).FirstOrDefault();
            if (SelectedAlbumOnArtistPage is null && AllArtists?.Count > 0)
            {
                if (DisplayedSongs.Contains(TemporarilyPickedSong))
                {
                    SelectedAlbumOnArtistPage = AllAlbums?.FirstOrDefault(x => x.Name == TemporarilyPickedSong!.AlbumName);
                }
            }
            SelectedArtistOnArtistPage = GetAllArtistsFromSongID(TemporarilyPickedSong!.LocalDeviceId!).FirstOrDefault();

        }
        if (AllArtists?.Count < 1)
        {
            AllArtists = PlayBackService.GetAllArtists()
                .OrderBy(x => x.Name)
                .ToObservableCollection();

            if (AllArtists?.Count < 1)
            {
                return;
            }
        }

        AllArtistsAlbums?.Clear();
        GetAllArtistAlbumFromArtistModel(SelectedArtistOnArtistPage);
        
        //await ShowSpecificArtistsSongsWithAlbum(SelectedAlbumOnArtistPage!);
        
    }

    [RelayCommand]
    public async Task ShowSpecificArtistsSongsWithAlbum(AlbumModelView album)
    {
        if (AllArtistsAlbums is null || AllArtistsAlbums?.Count<1)
        {
            var selectedLink = AllLinks.FirstOrDefault(link => link.AlbumId == album.LocalDeviceId);
            SelectedArtistOnArtistPage = AllArtists!.FirstOrDefault(artist => artist.LocalDeviceId == selectedLink?.ArtistId)!;

            
            List<string> allAlbums = new List<string>(); 
            if (SelectedArtistOnArtistPage != null)
            {
                allAlbums = AllLinks
                    .Where(link => link.ArtistId == SelectedArtistOnArtistPage.LocalDeviceId)
                    .Select(link => link.AlbumId)
                    .ToList();
            }

            var allAlbumsSet = new HashSet<string>(allAlbums); 
            AllArtistsAlbums = AllAlbums
                .Where(album => allAlbumsSet.Contains(album.LocalDeviceId))
                .ToObservableCollection();
        }
        else
        {
            var selArt= GetAllArtistsFromAlbumID(AllArtistsAlbums.FirstOrDefault()!.LocalDeviceId).FirstOrDefault();
            if (selArt is not null)
            {
                SelectedArtistOnArtistPage = selArt;
            }
        }
        SelectedAlbumOnArtistPage = album;
        if (SelectedAlbumOnArtistPage is null)
        {
            return;
        }
        AllArtistsAlbumSongs= GetAllSongsFromAlbumID(album.LocalDeviceId!);
        SelectedAlbumOnArtistPage.IsCurrentlySelected = true;

        var song = AllArtistsAlbumSongs!.FirstOrDefault();
        if (!string.IsNullOrEmpty(SelectedAlbumOnArtistPage.AlbumImagePath))
        {
            if (!File.Exists(SelectedAlbumOnArtistPage.AlbumImagePath))
            {
                SelectedAlbumOnArtistPage.AlbumImagePath = await LyricsManagerService.FetchAndDownloadCoverImage(song!.Title, song!.ArtistName!, song!.AlbumName!, song);
            }
        }
        PickedSong = AllArtistsAlbumSongs.FirstOrDefault()!;
    }
    
    public ObservableCollection<ArtistModelView> GetAllArtistsFromSongID(string songId)
  {
        var artistIds = AllLinks
            .Where(link => link.SongId == songId)
            .Select(link => link.ArtistId!)
            .Distinct()
            .ToHashSet();
        if (artistIds is null || artistIds.Count < 1)
        {
            var song= DisplayedSongs.FirstOrDefault(x => x.LocalDeviceId == songId);
            if (song is null)
            {
                return new ObservableCollection<ArtistModelView>();
            }
            var artName = song.ArtistName;
            
            var matchingArtist = AllArtists?.FirstOrDefault(x =>
            x.Name.Equals(artName, StringComparison.OrdinalIgnoreCase));

            return matchingArtist is not null
                ? new ObservableCollection<ArtistModelView> { matchingArtist }
                : new ObservableCollection<ArtistModelView>();
        }

        return new ObservableCollection<ArtistModelView>(
            AllArtists.Where(artist => artistIds.Contains(artist.LocalDeviceId!))
        );
    }
    public ObservableCollection<AlbumModelView> GetAlbumFromSongID(string songId)
    {
        // Find album IDs associated with the song
        var albumIds = AllLinks?
            .Where(link => link.SongId == songId)
            .Select(link => link.AlbumId)
            .Where(id => id is not null) // Filter out null IDs
            .Distinct()
            .ToHashSet();

        // Fallback if no album IDs found
        if (albumIds is null || albumIds.Count < 1)
        {
            var albumName = DisplayedSongs.FirstOrDefault(x => x.LocalDeviceId == songId)?.AlbumName;
            if (string.IsNullOrEmpty(albumName))
                return new ObservableCollection<AlbumModelView>(); // No fallback found

            var matchingAlbum = AllAlbums?.FirstOrDefault(x =>
                x.Name.Equals(albumName, StringComparison.OrdinalIgnoreCase));

            return matchingAlbum is not null
                ? new ObservableCollection<AlbumModelView> { matchingAlbum }
                : new ObservableCollection<AlbumModelView>();
        }

        // Initialize AllAlbums if null
        AllAlbums ??= SongsMgtService.AllAlbums.ToObservableCollection();

        // Filter AllAlbums based on matching IDs
        return new ObservableCollection<AlbumModelView>(
            AllAlbums.Where(album => albumIds.Contains(album.LocalDeviceId))
        );
    }

    public ObservableCollection<ArtistModelView> GetAllArtistsFromAlbumID(string albumId)
    {
        var artistIds = AllLinks
            .Where(link => link.AlbumId == albumId && link.ArtistId != null)
            .Select(link => link.ArtistId!)
            .Distinct()
            .ToHashSet();
        AllArtists = SongsMgtService.AllArtists.ToObservableCollection();
        return new ObservableCollection<ArtistModelView>(
            AllArtists.Where(artist => artistIds.Contains(artist.LocalDeviceId!))
        );
    }

    public ObservableCollection<SongModelView> GetAllSongsFromAlbumID(string albumId)
    {
        var songIds = AllLinks
            .Where(link => link.AlbumId == albumId && link.SongId != null)
            .Select(link => link.SongId!)
            .Distinct()
            .ToHashSet();

        return new ObservableCollection<SongModelView>(
            DisplayedSongs.Where(song => songIds.Contains(song.LocalDeviceId!))
        );
    }

    public ObservableCollection<AlbumModelView> GetAllAlbumsFromArtistID(string artistId)
    {
        var albumIds = AllLinks
            .Where(link => link.ArtistId == artistId && link.AlbumId != null)
            .Select(link => link.AlbumId!)
            .Distinct()
            .ToHashSet();
        AllAlbums = SongsMgtService.AllAlbums.ToObservableCollection();
        return new ObservableCollection<AlbumModelView>(
            AllAlbums.Where(album => albumIds.Contains(album.LocalDeviceId!))
        );
    }
    public ObservableCollection<SongModelView> GetAllSongsFromArtistID(string artistId)
    {
        var songIds = AllLinks
            .Where(link => link.ArtistId == artistId && link.SongId != null)
            .Select(link => link.SongId!)
            .Distinct()
            .ToHashSet();

        return new ObservableCollection<SongModelView>(
            SongsMgtService.AllSongs.Where(song => songIds.Contains(song.LocalDeviceId))
        );
    }

    public void GetAllArtistAlbumFromArtistModel(ArtistModelView artist)
    {
        //if (artist is null SelectedArtistOnArtistPage is null)
        if (artist is null)
            return;
        if (SelectedArtistOnArtistPage is not null)
            SelectedArtistOnArtistPage.IsCurrentlySelected = false;

        SelectedArtistOnArtistPage = artist;
        SelectedArtistOnArtistPage.IsCurrentlySelected = true;
        AllArtistsAlbums = GetAllAlbumsFromArtistID(SelectedArtistOnArtistPage.LocalDeviceId!);
        //await GetAlbumsFromArtistIDAsync(artist.LocalDeviceId);
        if (AllArtistsAlbums is null || AllArtistsAlbums.Count<1)
        {
            return;
        }
        foreach (var album in AllArtistsAlbums)
        {
            if (string.IsNullOrEmpty(album.AlbumImagePath))
            {
                
            }
        }
        if (AllArtistsAlbums.Count<1)
        {
            return;
        }
        SelectedArtistOnArtistPage.IsCurrentlySelected = true;
        SelectedArtistOnArtistPage.ImagePath = AllArtistsAlbums.FirstOrDefault()!.AlbumImagePath;
        AllArtistsAlbumSongs= GetAllSongsFromArtistID(artist.LocalDeviceId!);
        if (MySelectedSong is not null)
        {
            MySelectedSong.IsCurrentPlayingHighlight = true;
        }
    }

    [RelayCommand]
    public async Task SetSongCoverAsAlbumCover(SongModelView song)
    {
        var specificAlbum = AllArtistsAlbums.FirstOrDefault(x => x.Name == song.AlbumName)!;
        specificAlbum.AlbumImagePath = await LyricsManagerService.FetchAndDownloadCoverImage(song.Title, song!.ArtistName!, song!.AlbumName!, song);
        specificAlbum.NumberOfTracks = AllArtistsAlbumSongs.Count;
        SongsMgtService.UpdateAlbum(specificAlbum);
        
    }

    [RelayCommand]
    void SearchArtist(string aName)
    {
        if (string.IsNullOrEmpty(aName))
        {
            AllArtists = PlayBackService.GetAllArtists();
            return;
        }
        AllArtists = PlayBackService.GetAllArtists().Where(a => a.Name.Contains(aName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Name).ToObservableCollection();

        if (AllArtists.Count > 0)
        {
            SelectedArtistOnArtistPage = AllArtists.FirstOrDefault();
            var song = DisplayedSongs.FirstOrDefault(x => x.ArtistName == SelectedArtistOnArtistPage.Name);
            GetAllArtistsAlbum(song: song, isFromSong: true);
            
        }
        else
        {
            AllArtistsAlbumSongs?.Clear();
        }
    }

    public void ReCheckSongsBelongingToAlbum(string id)
    {
        AllArtistsAlbumSongs?.Clear();
        var alb = AllAlbums.FirstOrDefault(x => x.LocalDeviceId == id);
        //AllArtistsAlbumSongs = GetAllSongsFromAlbumID(id);
        AllArtistsAlbumSongs= GetAllSongsFromAlbumID(id);
        
        if (alb is not null)
        {
            if (alb.NumberOfTracks != AllArtistsAlbumSongs.Count)
            {
                alb.NumberOfTracks = AllArtistsAlbumSongs.Count;
                alb.TotalDuration = TimeSpan.FromSeconds(AllArtistsAlbumSongs.Sum(x => x.DurationInSeconds)).ToString(@"mm\:ss");
                SongsMgtService.UpdateAlbum(alb);
            }            
        }
       
        //need a linq fxn to go through all this collection and fill this variable
        //PopulateGroupedAlbumSongs(songs.ToList());
        GetTopCompletedPlays(AllArtistsAlbumSongs);
        /*
        TopCompleted = GetTopCompletedPlays(DisplayedSongs);
        TopSkipped = GetTopSkippedPlays(DisplayedSongs);
        OverallStats = GetOverallPlaybackStats(DisplayedSongs);
        RecentPlays = GetRecentlyPlayedSongs(DisplayedSongs, days: 30);
        LeastPlayed = GetLeastPlayedSongs(DisplayedSongs);
        RangeStatss = GetStatsInRange(DisplayedSongs, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);
        */
    }
    //// Method to populate SpecificAlbumGroupedSongs
    //public void PopulateGroupedAlbumSongs(List<SongModelView> songs)
    //{
    //    // Dictionary for manual grouping
    //    var groupDictionary = new Dictionary<string, List<SongModelView>>();

    //    // Group songs manually
    //    foreach (var song in songs)
    //    {
    //        if (song == null || string.IsNullOrWhiteSpace(song.Title))
    //        {
    //            Debug.WriteLine($"Skipping song with invalid title: {song?.Title}");
    //            continue; // Skip invalid songs
    //        }

    //        // Get the grouping key
    //        string key = GetGroupingKey(song.Title);

    //        // Add the song to the appropriate group in the dictionary
    //        if (!groupDictionary.TryGetValue(key, out List<SongModelView>? value))
    //        {
    //            value = new List<SongModelView>();
    //            groupDictionary[key] = value;
    //        }

    //        value.Add(song);
    //    }

    //    // Clear the SpecificAlbumGroupedSongs collection
    //    SpecificAlbumGroupedSongs.Clear();

    //    // Process the grouped data and add to SpecificAlbumGroupedSongs
    //    foreach (var kvp in groupDictionary)
    //    {
    //        string key = kvp.Key;
    //        List<SongModelView> groupedSongs = kvp.Value;

    //        var newGroup = new SongsGroup(
    //            groupName: key,
    //            songs: groupedSongs,
    //            description: $"{groupedSongs.Count} song(s) starting with '{key}'"
    //        );

    //        SpecificAlbumGroupedSongs.Add(key, groupedSongs);

    //    }
    //}



    /// <summary>
    /// Determines the grouping key based on the first non-whitespace character of the title.
    /// If the first character is not a letter, returns '#'.
    /// </summary>
    /// <param name="title">The title of the song.</param>
    /// <returns>A single character string representing the group name.</returns>
    private string GetGroupingKey(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "#"; // Default group for invalid or empty titles

        // Trim leading whitespace
        string trimmedTitle = title.TrimStart();

        if (string.IsNullOrEmpty(trimmedTitle))
            return "#";

        char firstChar = char.ToUpper(trimmedTitle[0]);

        // Check if the first character is a letter
        return char.IsLetter(firstChar) ? firstChar.ToString() : "#";
    }


    [ObservableProperty]
    public partial SongsGroup? SubSongGroup { get; set; }
    // Existing properties and methods

    [ObservableProperty]
    public partial ObservableCollection<SongsGroup> SpecificAlbumGroupedSongs { get; private set; } = new ObservableCollection<SongsGroup>();
    [ObservableProperty]
    public partial ObservableCollection<PlaybackStats> TopCompleted { get; private set; } = new ObservableCollection<PlaybackStats>();
    [ObservableProperty]
    public partial ObservableCollection<PlaybackStats> TopSkipped { get; private set; } = new ObservableCollection<PlaybackStats>();
    [ObservableProperty]
    public partial ObservableCollection<PlaybackStats> OverallStats { get; private set; } = new ObservableCollection<PlaybackStats>();
    [ObservableProperty]
    public partial ObservableCollection<PlaybackStats> RecentPlays { get; private set; } = new ObservableCollection<PlaybackStats>();
    
    [ObservableProperty]
    public partial ObservableCollection<PlaybackStats> LeastPlayed { get; private set; } = new ObservableCollection<PlaybackStats>();
    [ObservableProperty]
    public partial ObservableCollection<PlaybackStats> RangeStatss { get; private set; } = new ObservableCollection<PlaybackStats>();
    public void GetTopCompletedPlays(IEnumerable<SongModelView> songs)
    {
        TopCompleted = songs.Select(song =>
        {
            var completedPlays = AllPlayDataLinks
                .Where(p => p.SongId == song.LocalDeviceId && p.PlayType == 3)
                .ToList();

            return new PlaybackStats
            {
                SongId = song.LocalDeviceId,
                SongTitle = song.Title,                
                SongGenre = song.GenreName,
                TotalCompletedPlays = completedPlays.Count,
                //TotalCompletedHours = completedPlays.Sum(p => p.PositionInSeconds) / 3600.0,
                TotalCompletedHours = (song.DurationInSeconds * completedPlays.Count) / 3600.0,
                CompletedPlayTimes = completedPlays.Select(p => p.EventDate).ToList()
            };
        })
        .Where(stat => stat.TotalCompletedPlays > 0) // Only include songs with completed plays
        .OrderByDescending(stat => stat.TotalCompletedPlays)
        .ToObservableCollection();

        
    }
    public void GetTopSkippedPlays(IEnumerable<SongModelView> songs)
    {
        TopSkipped = songs.Select(song =>
        {
            var skippedPlays = AllPlayDataLinks
                .Where(p => p.SongId == song.LocalDeviceId && p.PlayType == 5) // 5 = Skipped
                .ToObservableCollection();

            return new PlaybackStats
            {
                SongId = song.LocalDeviceId,
                SongTitle = song.Title,
                SongGenre = song.GenreName,
                TotalSkips = skippedPlays.Count,
                SkipTimes = skippedPlays.Select(p => p.EventDate).ToList()
            };
        })
        .Where(stat => stat.TotalSkips > 0) // Only include songs with skips
        .OrderByDescending(stat => stat.TotalSkips)
        .ToObservableCollection();

        
    }
    public ObservableCollection<PlaybackStats> GetOverallPlaybackStats(IEnumerable<SongModelView> songs)
    {
        OverallStats = songs.Select(song =>
        {
            var allPlays = AllPlayDataLinks
                .Where(p => p.SongId == song.LocalDeviceId)
                .ToObservableCollection();

            return new PlaybackStats
            {
                SongId = song.LocalDeviceId,
                SongTitle = song.Title,
                SongGenre = song.GenreName,
                TotalPlays = allPlays.Count,
                TotalPlayHours = allPlays.Sum(p => p.PositionInSeconds) / 3600.0
            };
        })
        .Where(stat => stat.TotalPlays > 0) // Only include songs with at least one play
        .OrderByDescending(stat => stat.TotalPlays)
        .ToObservableCollection();

        return OverallStats;
    }
    public void GetRecentlyPlayedSongs(IEnumerable<SongModelView> songs, int days = 7)
    {
        var recentDate = DateTime.UtcNow.AddDays(-days);

        RecentPlays = songs.Select(song =>
        {
            var recentPlays = AllPlayDataLinks
                .Where(p => p.SongId == song.LocalDeviceId && p.EventDate >= recentDate)
                .ToObservableCollection();

            return new PlaybackStats
            {
                SongId = song.LocalDeviceId,
                SongTitle = song.Title,
                SongGenre = song.GenreName,
                TotalPlays = recentPlays.Count,
                TotalPlayHours = recentPlays.Sum(p => p.PositionInSeconds) / 3600.0
            };
        })
        .Where(stat => stat.TotalPlays > 0)
        .OrderByDescending(stat => stat.TotalPlays)
        .ToObservableCollection();

        
    }
    public void GetLeastPlayedSongs(IEnumerable<SongModelView> songs)
    {
        LeastPlayed = GetOverallPlaybackStats(songs)
            .OrderBy(stat => stat.TotalPlays)
            .ToObservableCollection();

        
    }
    public void GetStatsInRange(IEnumerable<SongModelView> songs, DateTime? startDate, DateTime? endDate, bool includeSkips = true, bool includeCompletions = true)
    {
        startDate ??= DateTime.Now.AddDays(-14);
        endDate ??= DateTime.Now;
        var stats = songs.Select(song =>
        {
            var filteredPlays = AllPlayDataLinks
                .Where(p => p.SongId == song.LocalDeviceId && p.EventDate >= startDate && p.EventDate <= endDate)
                .ToObservableCollection();

            var skippedPlays = includeSkips
                ? filteredPlays.Where(p => p.PlayType == 5).ToObservableCollection()
                : new ObservableCollection<PlayDataLink>();

            var completedPlays = includeCompletions
                ? filteredPlays.Where(p => p.WasPlayCompleted).ToObservableCollection()
                : new ObservableCollection<PlayDataLink>();

            return new PlaybackStats
            {
                SongId = song.LocalDeviceId,
                SongTitle = song.Title,
                SongGenre = song.GenreName,
                TotalSkips = skippedPlays.Count,
                SkipTimes = skippedPlays.Select(p => p.EventDate).ToList(),
                TotalCompletedPlays = completedPlays.Count,
                CompletedPlayTimes = completedPlays.Select(p => p.EventDate).ToList()
            };
        })
        .Where(stat => stat.TotalSkips > 0 || stat.TotalCompletedPlays > 0)
        .OrderByDescending(stat => stat.TotalCompletedPlays + stat.TotalSkips)
        .ToObservableCollection();

    }

}
