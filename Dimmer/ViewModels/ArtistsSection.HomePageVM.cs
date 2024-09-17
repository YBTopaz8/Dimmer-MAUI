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
    public async Task NavigateToArtistsPage(SongsModelView? song=null)
    {
        GetAllArtistsAlbum(song.Id, song);
#if WINDOWS
        await Shell.Current.GoToAsync(nameof(ArtistsPageD));
#elif ANDROID
        await Shell.Current.GoToAsync(nameof(AlbumPageM));
#endif
    }
    [RelayCommand]
    void GetAllAlbums()
    {
        if (AllAlbums?.Count != PlayBackService.GetAllAlbums().Count)
        {
            AllAlbums = SongsMgtService.AllAlbums.ToObservableCollection();
        }
    }
    [RelayCommand]
    void GetAllArtists()
    {
        //AllArtists?.Clear();
        if (AllArtists?.Count != PlayBackService.GetAllArtists().Count)
        {
            AllArtists = PlayBackService
                .GetAllArtists()
                .OrderBy(x => x.Name)
                .ToObservableCollection();
            if (AllArtists.Count > 0)
            {
#if WINDOWS
     SelectedArtistOnArtistPage = AllArtists.FirstOrDefault()!;
                SelectedArtistId = SelectedArtistOnArtistPage.Id;
                GetAllArtistsAlbum(SelectedArtistId);
#elif ANDROID

#endif

            }
        }
    }

    public void GetAllArtistsAlbum(ObjectId artistOrSongId, SongsModelView? song=null)
    {
        if (song is null)
        {
            SelectedArtistId = artistOrSongId;
        }
        else
        {
            (SelectedArtistId, SelectedArtistAlbumId) = SongsMgtService.GetArtistAndAlbumIdFromSongId(artistOrSongId);
            SelectedArtistOnArtistPage = AllArtists.First(x => x.Id == SelectedArtistId);
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
                SelectedArtistAlbumId = AllArtistsAlbums.First().Id;
            }
            else
            {
                SelectedAlbumOnArtistPage = AllArtistsAlbums.First();
            }
            ShowSpecificArtistsSongs();
        }
    }
    [RelayCommand]
    void ShowSpecificArtistsSongsWithAlbumId(ObjectId albumId)
    {
        AllArtistsAlbumSongs?.Clear();        
        AllArtistsAlbumSongs = PlayBackService.GetallArtistsSongsByAlbumID( albumId);
        SelectedSongToOpenBtmSheet = AllArtistsAlbumSongs.FirstOrDefault()!;
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
