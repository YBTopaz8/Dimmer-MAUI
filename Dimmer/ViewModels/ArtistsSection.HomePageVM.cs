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
        var songAlbum = GetAlbumFromSongId(song.Id);
        
        await NavigateToSpecificAlbumPage(songAlbum);

    }

    
    [RelayCommand]
    async Task NavigateToSpecificAlbumPage(AlbumModelView selectedAlbum)
    {
        SelectedArtistOnArtistPage = GetArtistFromAlbumId(selectedAlbum.Id);
        SelectedAlbumOnArtistPage = AllAlbums.First(x => x.Id == selectedAlbum.Id);
        SelectedAlbumOnArtistPage.NumberOfTracks = SongsMgtService.GetSongsCountFromAlbumID(selectedAlbum.Id);
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
        //GetAllArtistsAlbum(SelectedSongToOpenBtmSheet.Id, SelectedSongToOpenBtmSheet);
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

    public async void GetAllArtistsAlbum(AlbumModelView? album = null, SongModelView? song = null, bool isFromSong = false)
    {
        if(!SongsMgtService.AllSongs.Contains(TemporarilyPickedSong!))
            return;
        if(SelectedAlbumOnArtistPage is not null)
            SelectedAlbumOnArtistPage.IsCurrentlySelected = false;
        if (isFromSong)
        {
            if (song is null)
            {
                return;
            }
            SelectedArtistOnArtistPage = AllArtists.FirstOrDefault(x => x.Id == AllLinks!.FirstOrDefault(x => x.SongId == song!.Id)!.ArtistId)!;
            SelectedArtistOnArtistPage.IsCurrentlySelected = true;
            
        }
        else
        {
            SelectedAlbumOnArtistPage = album!;
            
            SelectedArtistOnArtistPage = AllArtists.FirstOrDefault(x => x.Id == AllLinks!.FirstOrDefault(x => x.AlbumId == album!.Id)!.ArtistId)!;
            SelectedArtistOnArtistPage.IsCurrentlySelected = true;
            SelectedAlbumOnArtistPage.IsCurrentlySelected = true;
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
            var selectedLink = AllLinks!.FirstOrDefault(link => link.AlbumId == album.Id);
            SelectedArtistOnArtistPage = AllArtists!.FirstOrDefault(artist => artist.Id == selectedLink?.ArtistId)!;

            
            List<ObjectId> allAlbums = new List<ObjectId>(); 
            if (SelectedArtistOnArtistPage != null)
            {
                allAlbums = AllLinks!
                    .Where(link => link.ArtistId == SelectedArtistOnArtistPage.Id)
                    .Select(link => link.AlbumId)
                    .ToList();
            }

            var allAlbumsSet = new HashSet<ObjectId>(allAlbums); 
            AllArtistsAlbums = AllAlbums
                .Where(album => allAlbumsSet.Contains(album.Id))
                .ToObservableCollection();
        }
        else
        {
            SelectedArtistOnArtistPage = GetArtistFromAlbumId(AllArtistsAlbums.FirstOrDefault()!.Id);
        }
        SelectedAlbumOnArtistPage = album;
        if (SelectedAlbumOnArtistPage is null)
        {
            return;
        }
        await GetSongsFromAlbumId(album.Id);
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
    public async Task GetAlbumsFromArtistAsync(ObjectId artistId)
    {
        // Use asynchronous processing with deferred execution
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            // Create a set of album IDs linked to the artist
            var albumIds = new HashSet<ObjectId>(
                AllLinks!.Where(link => link.ArtistId == artistId).Select(link => link.AlbumId)
            );

            // Filter albums efficiently using the pre-built HashSet
            var albums = AllAlbums.Where(album => albumIds.Contains(album.Id));

            // Convert the result to an observable collection for UI binding
            AllArtistsAlbums = albums.ToObservableCollection();
        });
    }

    public async void LoadSongsFromArtistId(ObjectId artistId)
    {

        // Ensure the operation runs on the main thread
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            // Pre-build a HashSet of song IDs related to the album for quick lookup
            var songIds = new HashSet<ObjectId>(
                AllLinks!.Where(link => link.ArtistId == artistId).Select(link => link.SongId)
            );

            // Filter songs using the HashSet for O(1) lookups
            var songs = SongsMgtService.AllSongs.Where(song => songIds.Contains(song.Id));

            AllArtistsAlbumSongs = songs.ToObservableCollection();
        });
        
    }


    public async Task GetSongsFromAlbumId(ObjectId albumId)
    {
        // Ensure the operation runs on the main thread
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            // Pre-build a HashSet of song IDs related to the album for quick lookup
            var songIds = new HashSet<ObjectId>(
                AllLinks!.Where(link => link.AlbumId == albumId).Select(link => link.SongId)
            );

            // Filter songs using the HashSet for O(1) lookups
            var songs = SongsMgtService.AllSongs.Where(song => songIds.Contains(song.Id));

            AllArtistsAlbumSongs = songs.ToObservableCollection();
        });

    }


    private ArtistModelView GetArtistFromAlbumId(ObjectId albumId)
    {
        return AllArtists.FirstOrDefault(x => x.Id == AllLinks!.FirstOrDefault(x => x.AlbumId == albumId)!.ArtistId)!;
    }
    private ArtistModelView GetArtistFromSongId(ObjectId songId)
    {
        return AllArtists.FirstOrDefault(x => x.Id == AllLinks!.FirstOrDefault(x => x.SongId == songId)!.ArtistId)!;
    }
    private AlbumModelView GetAlbumFromSongId(ObjectId songId)
    {
        return AllAlbums.FirstOrDefault(x => x.Id == AllLinks!.FirstOrDefault(x => x.SongId == songId)!.AlbumId)!;
    }
    private IEnumerable<AlbumModelView> GetAlbumsFromArtistId(ObjectId artistId)
    {
        var albumIds = AllLinks!.Where(link => link.ArtistId == artistId).Select(link => link.AlbumId).Distinct();
        var Albums= AllAlbums.Where(album => albumIds.Contains(album.Id));
        AllArtistsAlbums = Albums.ToObservableCollection();
        return Albums;
    }

    public async Task GetAllArtistAlbumFromArtist(ArtistModelView artist)
    {
        SelectedArtistOnArtistPage = artist;
        await GetAlbumsFromArtistAsync(artist.Id);
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
        SelectedArtistOnArtistPage.ImagePath = AllArtistsAlbums.FirstOrDefault()!.AlbumImagePath;
        LoadSongsFromArtistId(artist.Id);
        
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
