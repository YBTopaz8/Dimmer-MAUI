using WinRT;

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
    AlbumModelView selectedAlbumOnArtistPage;
    [ObservableProperty]
    ArtistModelView selectedArtistOnArtistPage;

    [RelayCommand]
    public async Task NavigateToSpecificAlbumPageFromBtmSheet(SongsModelView? song)
    {
        SelectedSongToOpenBtmSheet = song;
        (var songsArtistId, var songsAlbum) = SongsMgtService.GetArtistAndAlbumIdFromSongId(song.Id);
        await NavigateToSpecificAlbumPage(songsAlbum);

    }

    
    [RelayCommand]
    async Task NavigateToSpecificAlbumPage(AlbumModelView selectedAlbum)
    {
        SelectedAlbumOnArtistPage = AllAlbums.First(x => x.Id == selectedAlbum.Id);
        SelectedAlbumOnArtistPage.NumberOfTracks = SongsMgtService.GetSongsCountFromAlbumID(selectedAlbum.Id);
        SelectedAlbumOnArtistPage.AlbumImagePath = DisplayedSongs.First(x => x.AlbumName == SelectedAlbumOnArtistPage.Name).CoverImagePath;
        SelectedAlbumOnArtistPage.TotalDuration = TimeSpan
            .FromSeconds(DisplayedSongs
            .Where(x => x.AlbumName == SelectedAlbumOnArtistPage.Name)
            .Sum(x => x.DurationInSeconds))
            .ToString(@"mm\:ss");

        await ShowSpecificArtistsSongsWithAlbum(selectedAlbum);
        await Shell.Current.GoToAsync(nameof(SpecificAlbumPage));
    }
   
    [RelayCommand]
    public async Task NavigateToArtistsPage(int? callerID=0) //0 if called by else, 1 if called by homeD or homeM
    {

        if (callerID == 0)
        {
            SelectedSongToOpenBtmSheet = TemporarilyPickedSong;
        }
       
#if WINDOWS
        await Shell.Current.GoToAsync(nameof(ArtistsPageD));
#elif ANDROID
        await Shell.Current.GoToAsync(nameof(AlbumPageM));
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

    public async void GetAllArtistsAlbum(AlbumModelView? album = null, SongsModelView? song = null, bool isFromSong = false)
    {
        if(SelectedAlbumOnArtistPage is not null)
            SelectedAlbumOnArtistPage.IsCurrentlySelected = false;
        if (isFromSong)
        {
            SelectedArtistOnArtistPage = SongsMgtService.GetArtistFromSongId(song.Id);            
            SelectedArtistOnArtistPage.IsCurrentlySelected = true;
            
        }
        else
        {
            SelectedAlbumOnArtistPage = album;
            
            SelectedArtistOnArtistPage = SongsMgtService.GetArtistFromAlbumId(album.Id);
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
        AllArtistsAlbums = SongsMgtService.GetAlbumsFromArtistOrSongID(SelectedArtistOnArtistPage.Id).ToObservableCollection();
        if (SelectedAlbumOnArtistPage is null)
        {
            SelectedAlbumOnArtistPage = AllArtistsAlbums.FirstOrDefault(x => x.Name == song.AlbumName);
        }
        AllArtistsAlbumSongs = PlayBackService.GetAllArtistsAlbumSongsAlbumID(SelectedAlbumOnArtistPage.Id);
        await ShowSpecificArtistsSongsWithAlbum(SelectedAlbumOnArtistPage);
        
    }
    [RelayCommand]
    public async Task ShowSpecificArtistsSongsWithAlbum(AlbumModelView album)
    {
        if (AllArtistsAlbums is null)
        {
            SelectedArtistOnArtistPage = SongsMgtService.GetArtistFromAlbumId(album.Id);
            if(SelectedArtistOnArtistPage != null)
                AllArtistsAlbums = SongsMgtService.GetAlbumsFromArtistOrSongID(SelectedArtistOnArtistPage.Id).ToObservableCollection();

        }
        SelectedArtistOnArtistPage = SongsMgtService.GetArtistFromAlbumId(album.Id);
        SelectedAlbumOnArtistPage = album;
        SelectedAlbumOnArtistPage.IsCurrentlySelected = true;
        
        AllArtistsAlbumSongs?.Clear();
        AllArtistsAlbumSongs = PlayBackService.GetAllArtistsAlbumSongsAlbumID(album.Id);
        var song = DisplayedSongs.FirstOrDefault(x => x.AlbumName == album.Name);
        if (!string.IsNullOrEmpty(SelectedAlbumOnArtistPage.AlbumImagePath))
        {
            if (!File.Exists(SelectedAlbumOnArtistPage.AlbumImagePath))
            {
                SelectedAlbumOnArtistPage.AlbumImagePath = await LyricsManagerService.FetchAndDownloadCoverImage(song.Title, song.ArtistName, song.AlbumName, song);

            }
        }
        PickedSong = AllArtistsAlbumSongs.FirstOrDefault()!;
    }

    public async Task GetAllArtistAlbumFromArtist(ArtistModelView artist)
    {
        var allArtAlbum = SongsMgtService.GetAlbumsFromArtistOrSongID(artist.Id);
        AllArtistsAlbums = allArtAlbum.ToObservableCollection();
        await ShowSpecificArtistsSongsWithAlbum(AllArtistsAlbums.FirstOrDefault()!);
    }

    [RelayCommand]
    public async Task SetSongCoverAsAlbumCover(SongsModelView song)
    {
        var specificAlbum = AllArtistsAlbums.FirstOrDefault(x => x.Name == song.AlbumName)!;
        specificAlbum.AlbumImagePath = await LyricsManagerService.FetchAndDownloadCoverImage(song.Title, song!.ArtistName!, song!.AlbumName!, song);
        specificAlbum.NumberOfTracks = AllArtistsAlbumSongs.Count;
        SongsMgtService.UpdateAlbum(specificAlbum);
        AllArtistsAlbums = SongsMgtService.GetAlbumsFromArtistOrSongID(SelectedArtistOnArtistPage.Id)
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
