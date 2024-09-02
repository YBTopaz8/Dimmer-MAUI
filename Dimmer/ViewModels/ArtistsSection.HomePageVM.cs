namespace Dimmer_MAUI.ViewModels;

public partial class HomePageVM
{
    [ObservableProperty]
    ObservableCollection<ArtistModelView> allArtists;
    [ObservableProperty]
    ObservableCollection<AlbumModelView> allAlbums;
    [ObservableProperty]
    ObservableCollection<SongsModelView> allArtistslbumSongs;
    [ObservableProperty]
    ObservableCollection<AlbumModelView> allArtistsAlbums;
    [ObservableProperty]
    string selectedArtistPageTitle;

    [ObservableProperty]
    public ObjectId selectedArtistAlbumId;
    [ObservableProperty]
    ObjectId selectedArtistId;
    [RelayCommand]
    void GetAllArtists()
    {
        //AllArtists?.Clear();
        if (AllArtists?.Count != PlayBackUtilsService.GetAllArtists().Count)
        {
            AllArtists = PlayBackUtilsService.GetAllArtists().OrderBy(x => x.Name).ToObservableCollection();
            if (AllArtists.Count > 0)
            {
                SelectedArtistId = AllArtists.FirstOrDefault()!.Id;
            }
            GetAllArtistsAlbum(SelectedArtistId);
        }        
    }

    [RelayCommand]
    void GetAllArtistsAlbum(ObjectId artistId)
    {
        
        if (AllArtists?.Count < 1)
        {
            AllArtists = PlayBackUtilsService.GetAllArtists().OrderBy(x => x.Name).ToObservableCollection();
            if (AllArtists?.Count < 1)
            {
                return;
            }
        }

        SelectedArtistId = artistId == ObjectId.Empty ? AllArtists!.FirstOrDefault()!.Id : artistId;
        
        AllArtistsAlbums?.Clear();
        AllArtistsAlbums = PlayBackUtilsService.GetAllArtistsAlbums(SelectedArtistId).ToObservableCollection();
        if (AllArtistsAlbums.Count > 0)
        {
            SelectedArtistAlbumId = AllArtistsAlbums.FirstOrDefault()!.Id;
            ShowSpecificArtistsSongs(SelectedArtistAlbumId);
        }
    }
    [RelayCommand]
    void ShowSpecificArtistsSongs(ObjectId albumId)
    {
        AllArtistslbumSongs?.Clear();
        
        AllArtistslbumSongs = PlayBackUtilsService.GetallArtistsSongsById( albumId,SelectedArtistId);
    }

    
    public async Task SetSongCoverAsAlbumCover(SongsModelView ss)
    {
        var specificAlbum = AllArtistsAlbums.FirstOrDefault(x => x.Id == SelectedArtistAlbumId)!;
        specificAlbum.AlbumImagePath = await LyricsManagerService.FetchAndDownloadCoverImage(TemporarilyPickedSong);
        specificAlbum.NumberOfTracks = AllArtistslbumSongs.Count;
        SongsMgtService.UpdateAlbum(specificAlbum);


        AllArtistsAlbums = PlayBackUtilsService.GetAllArtistsAlbums(SelectedArtistId).OrderBy(x => x.Name).ToObservableCollection();
    }
}
