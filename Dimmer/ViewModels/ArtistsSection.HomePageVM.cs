//using WinRT;

namespace Dimmer_MAUI.ViewModels;

public partial class HomePageVM
{

    [ObservableProperty]
    ObservableCollection<ArtistModelView> allArtists=new();
    [ObservableProperty]
    ObservableCollection<AlbumModelView> allAlbums= new();
    [ObservableProperty]
    ObservableCollection<SongModelView> allArtistsAlbumSongs = new();
    [ObservableProperty]
    ObservableCollection<SongModelView> allArtistsSongs = new();
    [ObservableProperty]
    ObservableCollection<AlbumModelView> allArtistsAlbums = new();
    [ObservableProperty]
    string? selectedArtistPageTitle;

    [ObservableProperty]
    AlbumModelView? selectedAlbumOnArtistPage;
    [ObservableProperty]
    ArtistModelView? selectedArtistOnArtistPage;

    [RelayCommand]
    public async Task NavigateToSpecificAlbumPageFromBtmSheet(SongModelView song)
    {
        SelectedSongToOpenBtmSheet = song;
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
            SelectedSongToOpenBtmSheet = TemporarilyPickedSong!;
        }
       
#if WINDOWS
        await Shell.Current.GoToAsync(nameof(ArtistsPageD));
#elif ANDROID
        await Shell.Current.GoToAsync(nameof(ArtistsPageM));
        SelectedArtistOnArtistPage = GetAllArtistsFromSongID(SelectedSongToOpenBtmSheet!.LocalDeviceId!).First();
        GetAllArtistAlbumFromArtist(SelectedArtistOnArtistPage);
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
        GetAllArtistAlbumFromArtist(SelectedArtistOnArtistPage);
        
        //await ShowSpecificArtistsSongsWithAlbum(SelectedAlbumOnArtistPage!);
        
    }

    [RelayCommand]
    public async Task ShowSpecificArtistsSongsWithAlbum(AlbumModelView album)
    {
        if (AllArtistsAlbums is null)
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
            SelectedArtistOnArtistPage = GetAllArtistsFromAlbumID(AllArtistsAlbums.FirstOrDefault()!.LocalDeviceId).First();
        }
        SelectedAlbumOnArtistPage = album;
        if (SelectedAlbumOnArtistPage is null)
        {
            return;
        }
        await GetSongsFromAlbumId(album.LocalDeviceId);
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
    partial void OnAllAlbumsChanging(ObservableCollection<AlbumModelView>? oldValue, ObservableCollection<AlbumModelView> newValue)
    {
        Debug.WriteLine($"Old alb {oldValue?.Count} new {newValue?.Count}");
    }
    public async Task GetAlbumsFromArtistIDAsync(string artistId)
    {
        // Use asynchronous processing with deferred execution
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            // Create a set of album IDs linked to the artist
            var albumIds = new HashSet<string>(
                 AllLinks.Where(link => link.ArtistId == artistId).Select(link => link.AlbumId)
            );
            if (AllAlbums is null)
            {
                AllAlbums = SongsMgtService.AllAlbums.ToObservableCollection();
            }
            // Filter albums efficiently using the pre-built HashSet
            var albums = SongsMgtService.AllAlbums.Where(album => albumIds.Contains(album.LocalDeviceId));

            // Convert the result to an observable collection for UI binding
            AllArtistsAlbums = albums.ToObservableCollection();
        });
    }

    public async void LoadSongsFromArtistId(string artistId)
    {

        // Ensure the operation runs on the main thread
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            // Pre-build a HashSet of song IDs related to the album for quick lookup
            var songIds = new HashSet<string>(
                AllLinks.Where(link => link.ArtistId == artistId).Select(link => link.SongId)
            );

            // Filter songs using the HashSet for O(1) lookups
            var songs = SongsMgtService.AllSongs.Where(song => songIds.Contains(song.LocalDeviceId!));

            AllArtistsAlbumSongs = songs.ToObservableCollection();
        });
        
    }


    public async Task GetSongsFromAlbumId(string albumId)
    {
        // Ensure the operation runs on the main thread
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            // Pre-build a HashSet of song IDs related to the album for quick lookup
            var songIds = new HashSet<string>(
                AllLinks.Where(link => link.AlbumId == albumId).Select(link => link.SongId)!
            );

            // Filter songs using the HashSet for O(1) lookups
            var songs = SongsMgtService.AllSongs.Where(song => songIds.Contains(song.LocalDeviceId!));

            AllArtistsAlbumSongs = songs.ToObservableCollection();
        });

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
            DisplayedSongs.Where(song => songIds.Contains(song.LocalDeviceId))
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
            DisplayedSongs.Where(song => songIds.Contains(song.LocalDeviceId))
        );
    }

    public void GetAllArtistAlbumFromArtist(ArtistModelView artist)
    {
        if (artist is null && SelectedArtistOnArtistPage is null)
            return;
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
        SelectedSongToOpenBtmSheet.IsCurrentPlayingHighlight = true;
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
}
