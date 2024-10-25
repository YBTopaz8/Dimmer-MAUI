namespace Dimmer_MAUI.ViewModels;

public partial class HomePageVM
{

    [ObservableProperty]
    ObservableCollection<ArtistModelView> allArtists;
    [ObservableProperty]
    ObservableCollection<AlbumModelView> allAlbums;
    [ObservableProperty]
    ObservableCollection<SongsModelView> allArtistsAlbumSongs;
    [ObservableProperty]
    ObservableCollection<SongsModelView> allArtistsSongs;
    [ObservableProperty]
    ObservableCollection<AlbumModelView> allArtistsAlbums;
    [ObservableProperty]
    string selectedArtistPageTitle;

    [ObservableProperty]
    public ObjectId selectedArtistAlbumId;
    [ObservableProperty]
    ObjectId selectedArtistId;
    [ObservableProperty]
    AlbumModelView selectedAlbumOnArtistPage;
    [ObservableProperty]
    ArtistModelView selectedArtistOnArtistPage;

    [RelayCommand]
    public async Task NavigateToSpecificAlbumPageFromBtmSheet(SongsModelView? song)
    {

        SelectedSongToOpenBtmSheet = song;
        (var songsArtistId, var songsAlbumId) = SongsMgtService.GetArtistAndAlbumIdFromSongId(song.Id);
        await NavigateToSpecificAlbumPage(songsAlbumId);

    }

    
    [RelayCommand]
    async Task NavigateToSpecificAlbumPage(ObjectId selectedAlbumId)
    {
        SelectedAlbumOnArtistPage = AllAlbums.First(x => x.Id == selectedAlbumId);
        SelectedAlbumOnArtistPage.NumberOfTracks = SongsMgtService.GetSongsCountFromAlbumID(selectedAlbumId);
        SelectedAlbumOnArtistPage.AlbumImagePath = DisplayedSongs.First(x => x.AlbumName == SelectedAlbumOnArtistPage.Name).CoverImagePath;
        SelectedAlbumOnArtistPage.TotalDuration = TimeSpan
            .FromSeconds(DisplayedSongs
            .Where(x => x.AlbumName == SelectedAlbumOnArtistPage.Name)
            .Sum(x => x.DurationInSeconds))
            .ToString(@"mm\:ss");

        ShowSpecificArtistsSongsWithAlbumId(selectedAlbumId);
        await Shell.Current.GoToAsync(nameof(SpecificAlbumPage));
    }
   
    [RelayCommand]
    public async Task NavigateToArtistsPage(int callerID) //0 if called by else, 1 if called by homeD
    {

        if (callerID == 0)
        {
            SelectedSongToOpenBtmSheet = TemporarilyPickedSong;
        }
       
        GetAllArtistsAlbum(SelectedSongToOpenBtmSheet.Id, SelectedSongToOpenBtmSheet);
#if WINDOWS
        await Shell.Current.GoToAsync(nameof(ArtistsPageD));
#elif ANDROID
        await Shell.Current.GoToAsync(nameof(AlbumPageM));
#endif
    }
    [RelayCommand]
    void GetAllAlbums()
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
                    SelectedArtistId = SelectedArtistOnArtistPage.Id;
                    GetAllArtistsAlbum(SelectedArtistId, TemporarilyPickedSong);
                }
#endif
            }
        }
    }

    public void GetAllArtistsAlbum(ObjectId artistOrSongId, SongsModelView? song = null)
    {
        if(SelectedAlbumOnArtistPage is not null)
        {
            SelectedAlbumOnArtistPage.IsCurrentlySelected = false;
        }
        if(SelectedArtistOnArtistPage is not null)
        {
            AllArtistsAlbums.First(x => x.Id == SelectedAlbumOnArtistPage!.Id).IsCurrentlySelected = false;
            SelectedArtistOnArtistPage.IsCurrentlySelected = false;
        }
        if (song is null)
        {
            SelectedArtistId = artistOrSongId;
        }
        else
        {
            (SelectedArtistId, SelectedArtistAlbumId) = SongsMgtService.GetArtistAndAlbumIdFromSongId(song.Id);
            SelectedArtistOnArtistPage = AllArtists.FirstOrDefault(x => x.Id == SelectedArtistId);
            SelectedArtistOnArtistPage.IsCurrentlySelected = true;
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
        AllArtistsAlbums = SongsMgtService.GetAlbumsFromArtistOrSongID(SelectedArtistId).ToObservableCollection();
        if (AllArtistsAlbums.Count > 0)
        {
            if (song is null)
            {                
                SelectedAlbumOnArtistPage = AllArtistsAlbums.First();
                SelectedAlbumOnArtistPage.IsCurrentlySelected = true;
                SelectedArtistOnArtistPage = AllArtists.First(x=>x.Id == artistOrSongId);
            }
            else
            {
                SelectedAlbumOnArtistPage = AllArtistsAlbums.First();
                
            }
            SelectedArtistOnArtistPage.IsCurrentlySelected = true;
            ShowSpecificArtistsSongs();
        }
    }
    [RelayCommand]
    void ShowSpecificArtistsSongsWithAlbumId(ObjectId albumId)
    {
        if (AllArtistsAlbums is null)
        {
            SelectedArtistOnArtistPage = SongsMgtService.GetArtistFromAlbumId(albumId);
            if(SelectedArtistOnArtistPage != null)
                AllArtistsAlbums = SongsMgtService.GetAlbumsFromArtistOrSongID(SelectedArtistOnArtistPage.Id).ToObservableCollection();

        }
        if (AllArtistsAlbums is not null)
        {
            if (SelectedAlbumOnArtistPage is not null)
                AllArtistsAlbums.First(x => x.Id == SelectedAlbumOnArtistPage.Id).IsCurrentlySelected = false;
        }

        SelectedArtistOnArtistPage = SongsMgtService.GetArtistFromAlbumId(albumId);
        SelectedArtistOnArtistPage.IsCurrentlySelected = true;
        SelectedAlbumOnArtistPage.IsCurrentlySelected = false;
        SelectedAlbumOnArtistPage = AllAlbums.First(x => x.Id == albumId);
        SelectedAlbumOnArtistPage.IsCurrentlySelected = true;
        AllArtistsAlbums.First(x=>x.Id==albumId).IsCurrentlySelected = true;
        AllArtistsAlbumSongs?.Clear();
        AllArtistsAlbumSongs = PlayBackService.GetAllArtistsAlbumSongsAlbumID(albumId);
        PickedSong = AllArtistsAlbumSongs.FirstOrDefault()!;
    }

    [RelayCommand]
    void ShowSpecificArtistsSongs()
    {
        AllArtistsSongs?.Clear();
        var songss = PlayBackService.GetallArtistsSongsByArtistId(SelectedArtistId);
        AllArtistsAlbumSongs = songss;
    }

    [RelayCommand]
    public async Task SetSongCoverAsAlbumCover()
    {
        var specificAlbum = AllArtistsAlbums.FirstOrDefault(x => x.Id == SelectedArtistAlbumId)!;
        specificAlbum.AlbumImagePath = await LyricsManagerService.FetchAndDownloadCoverImage(TemporarilyPickedSong);
        specificAlbum.NumberOfTracks = AllArtistsAlbumSongs.Count;
        SongsMgtService.UpdateAlbum(specificAlbum);
        AllArtistsAlbums = SongsMgtService.GetAlbumsFromArtistOrSongID(SelectedArtistId)
            .OrderBy(x => x.Name)
            .ToObservableCollection();
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
            SelectedArtistAlbumId = AllArtists.FirstOrDefault().Id;
            GetAllArtistsAlbum(SelectedArtistAlbumId);
        }
        else
        {
            AllArtistsAlbumSongs?.Clear();
        }
    }
}
