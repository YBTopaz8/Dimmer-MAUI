//using WinRT;

namespace Dimmer_MAUI.ViewModels;

public partial class HomePageVM
{

    [ObservableProperty]
    ObservableCollection<ArtistModelView> allArtists;
    [ObservableProperty]
    ObservableCollection<AlbumModelView> allAlbums;
    [ObservableProperty]
    ObservableCollection<SongModelView> allArtistsAlbumSongs;
    [ObservableProperty]
    ObservableCollection<SongModelView> allArtistsSongs;
    [ObservableProperty]
    ObservableCollection<AlbumModelView> allArtistsAlbums;
    [ObservableProperty]
    string selectedArtistPageTitle;

    [ObservableProperty]
    AlbumModelView selectedAlbumOnArtistPage;
    [ObservableProperty]
    ArtistModelView selectedArtistOnArtistPage;

    [RelayCommand]
    public async Task NavigateToSpecificAlbumPageFromBtmSheet(SongModelView song)
    {
        SelectedSongToOpenBtmSheet = song;
        var songAlbum = GetAlbumFromSongId(song.LocalDeviceId);
        
        await NavigateToSpecificAlbumPage(songAlbum);

    }

    
    [RelayCommand]
    async Task NavigateToSpecificAlbumPage(AlbumModelView selectedAlbum)
    {
        SelectedArtistOnArtistPage = GetArtistFromAlbumId(selectedAlbum.LocalDeviceId);
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
#endif
        //GetAllArtistsAlbum(SelectedSongToOpenBtmSheet.LocalDeviceId, SelectedSongToOpenBtmSheet);
    }
    
    public void GetAllAlbums()
    {
        AllAlbums = PlayBackService.GetAllAlbums();
    }
    [RelayCommand]
    void GetAllArtists()
    {
        AllLinks = SongsMgtService.AllLinks.ToList();
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

    public async void GetAllArtistsAlbum(AlbumModelView? album = null, SongModelView? song = null, bool isFromSong = false)
    {
        //if(!SongsMgtService.AllSongs.Contains(TemporarilyPickedSong!))
        //    return;
        if(SelectedAlbumOnArtistPage is not null)
            SelectedAlbumOnArtistPage.IsCurrentlySelected = false;
        if (isFromSong)
        {
            if (song is null)
            {
                return;
            }
            
            SelectedArtistOnArtistPage = GetArtistFromSongId(song.LocalDeviceId);
            if (SelectedArtistOnArtistPage is not null)
            {
                SelectedArtistOnArtistPage.IsCurrentlySelected = true;
            }
            
        }
        else if(album is not null)
        {
            SelectedAlbumOnArtistPage = album!;            
            SelectedArtistOnArtistPage = GetArtistFromAlbumId(album!.LocalDeviceId);
            SelectedArtistOnArtistPage.IsCurrentlySelected = true;
            
            if (SelectedAlbumOnArtistPage is null)
            {
                return;
            }
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
        await GetAllArtistAlbumFromArtist(SelectedArtistOnArtistPage);
        
        //await ShowSpecificArtistsSongsWithAlbum(SelectedAlbumOnArtistPage!);
        
    }

    [RelayCommand]
    public async Task ShowSpecificArtistsSongsWithAlbum(AlbumModelView album)
    {
        if (AllArtistsAlbums is null)
        {
            var selectedLink = AllLinks!.FirstOrDefault(link => link.AlbumId == album.LocalDeviceId);
            SelectedArtistOnArtistPage = AllArtists!.FirstOrDefault(artist => artist.LocalDeviceId == selectedLink?.ArtistId)!;

            
            List<string> allAlbums = new List<string>(); 
            if (SelectedArtistOnArtistPage != null)
            {
                allAlbums = AllLinks!
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
            SelectedArtistOnArtistPage = GetArtistFromAlbumId(AllArtistsAlbums.FirstOrDefault()!.LocalDeviceId);
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
    public async Task GetAlbumsFromArtistIDAsync(string artistId)
    {
        // Use asynchronous processing with deferred execution
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            // Create a set of album IDs linked to the artist
            var albumIds = new HashSet<string>(
                AllLinks!.Where(link => link.ArtistId == artistId).Select(link => link.AlbumId)
            );

            // Filter albums efficiently using the pre-built HashSet
            var albums = AllAlbums.Where(album => albumIds.Contains(album.LocalDeviceId));

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
                AllLinks!.Where(link => link.ArtistId == artistId).Select(link => link.SongId)
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
                AllLinks!.Where(link => link.AlbumId == albumId).Select(link => link.SongId)
            );

            // Filter songs using the HashSet for O(1) lookups
            var songs = SongsMgtService.AllSongs.Where(song => songIds.Contains(song.LocalDeviceId!));

            AllArtistsAlbumSongs = songs.ToObservableCollection();
        });

    }

    private Dictionary<string, string> AlbumToArtistMap =>
    AllLinks!.ToDictionary(link => link.AlbumId, link => link.ArtistId);

    private ArtistModelView GetArtistFromAlbumId(string albumId)
    {
        if (AlbumToArtistMap.TryGetValue(albumId, out var artistId))
        {
            return AllArtists.FirstOrDefault(x => x.LocalDeviceId == artistId)!;
        }
        return null!;
    }
    private Dictionary<string, string> SongToArtistMap =>
       AllLinks!.ToDictionary(link => link.SongId, link => link.ArtistId);

    private ArtistModelView GetArtistFromSongId(string songId)
    {
        if (SongToArtistMap.TryGetValue(songId, out var artistId))
        {
            return AllArtists.FirstOrDefault(x => x.LocalDeviceId == artistId)!;
        }
        return null!;
    }
    private Dictionary<string, string> SongToAlbumMap =>
        AllLinks!.ToDictionary(link => link.SongId, link => link.AlbumId);

    private AlbumModelView GetAlbumFromSongId(string songId)
    {
        if (SongToAlbumMap.TryGetValue(songId, out var albumId))
        {
            return AllAlbums.FirstOrDefault(x => x.LocalDeviceId == albumId)!;
        }
        return null!;
    }

    private Dictionary<string, HashSet<string>> ArtistToAlbumMap =>
        AllLinks!
            .GroupBy(link => link.ArtistId)
            .ToDictionary(group => group.Key, group => group.Select(link => link.AlbumId).ToHashSet());

    private IEnumerable<AlbumModelView> GetAlbumsFromArtistId(string artistId)
    {
        if (ArtistToAlbumMap.TryGetValue(artistId, out var albumIds))
        {
            var albums = AllAlbums.Where(album => albumIds.Contains(album.LocalDeviceId));
            AllArtistsAlbums = albums.ToObservableCollection();
            return albums;
        }
        return Enumerable.Empty<AlbumModelView>();
    }

    public async Task GetAllArtistAlbumFromArtist(ArtistModelView artist)
    {
        if (artist is null)
            return;
        SelectedArtistOnArtistPage = artist;
         await GetAlbumsFromArtistIDAsync(artist.LocalDeviceId);
        if (AllArtistsAlbums is null)
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
        SelectedArtistOnArtistPage.ImagePath = AllArtistsAlbums.FirstOrDefault()!.AlbumImagePath;
        LoadSongsFromArtistId(artist.LocalDeviceId);
        
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
        AllArtists = PlayBackService.GetAllArtists()
    .Where(a => a.Name.Contains(aName, StringComparison.OrdinalIgnoreCase))
    .OrderBy(x => x.Name)
    .ToObservableCollection();

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
