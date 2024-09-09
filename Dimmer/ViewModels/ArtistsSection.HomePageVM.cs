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
    void GetAllArtists()
    {
        //AllArtists?.Clear();
        if (AllArtists?.Count != PlayBackUtilsService.GetAllArtists().Count)
        {
            AllArtists = PlayBackUtilsService
                .GetAllArtists()
                .OrderBy(x => x.Name)
                .ToObservableCollection();
            if (AllArtists.Count > 0)
            {
                SelectedArtistOnArtistPage = AllArtists.FirstOrDefault()!;
                SelectedArtistId = SelectedArtistOnArtistPage.Id;
                GetAllArtistsAlbum(SelectedArtistId);
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
            AllArtists = PlayBackUtilsService.GetAllArtists()
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
            ShowSpecificArtistsSongs(SelectedArtistAlbumId);
        }
    }
    [RelayCommand]
    void ShowSpecificArtistsSongs(ObjectId albumId)
    {
        AllArtistsAlbumSongs?.Clear();        
        AllArtistsAlbumSongs = PlayBackUtilsService.GetallArtistsSongsById( albumId,SelectedArtistId);
        SelectedSongToOpenBtmSheet = AllArtistsAlbumSongs.FirstOrDefault()!;
    }
    [RelayCommand]
    public async Task SetSongCoverAsAlbumCover()
    {
        var specificAlbum = AllArtistsAlbums.FirstOrDefault(x => x.Id == SelectedArtistAlbumId)!;
        specificAlbum.AlbumImagePath = await LyricsManagerService.FetchAndDownloadCoverImage(TemporarilyPickedSong);
        specificAlbum.NumberOfTracks = AllArtistsAlbumSongs.Count;
        SongsMgtService.UpdateAlbum(specificAlbum);


        AllArtistsAlbums = SongsMgtService.GetAlbumsFromArtistOrSongID(SelectedArtistId).OrderBy(x => x.Name).ToObservableCollection();
    }

    [RelayCommand]
    void SearchArtist(string aName)
    {
        if (string.IsNullOrEmpty(aName))
        {
            AllArtists = PlayBackUtilsService.GetAllArtists();
            return;
        }
        AllArtists = PlayBackUtilsService.GetAllArtists()
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
