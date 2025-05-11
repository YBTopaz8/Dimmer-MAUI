using Dimmer.Services;

namespace Dimmer.ViewModel;
public partial class BaseAlbumViewModel : ObservableObject
{
    private readonly IMapper _mapper;
    private readonly BaseViewModel baseViewModel;

    private readonly IDimmerStateService _stateService;
    private readonly ISettingsService _settingsService;
    private readonly SubscriptionManager _subs;
    [ObservableProperty]
    public partial ObservableCollection<AlbumModelView>? SelectedAlbumsCol { get; internal set; }
    [ObservableProperty]
    public partial ObservableCollection<SongModelView>? SelectedAlbumsSongs { get; internal set; }
    [ObservableProperty]
    public partial AlbumModelView? SelectedAlbum { get; internal set; }
    [ObservableProperty]
    public partial SongModelView SelectedSong { get; set; }

    public AlbumsMgtFlow AlbumsMgtFlow { get; }
    public PlayListMgtFlow PlaylistsMgtFlow { get; }
    public SongsMgtFlow SongsMgtFlow { get; }

    public BaseAlbumViewModel(
            IMapper mapper, BaseViewModel baseViewModel,AlbumsMgtFlow albumsMgtFlow,PlayListMgtFlow playlistsMgtFlow,
            SongsMgtFlow songsMgtFlow,IDimmerStateService stateService, ISettingsService settingsService,
            SubscriptionManager subs) 
    {

        this.baseViewModel=baseViewModel;
        _mapper = mapper;
            AlbumsMgtFlow = albumsMgtFlow;
            PlaylistsMgtFlow = playlistsMgtFlow;
            SongsMgtFlow = songsMgtFlow;
            _stateService = stateService;
            _settingsService = settingsService;
            _subs = subs;
        SubscribeToAlbumListChanges();
        SelectedSong=new();


    }
    public BaseAlbumViewModel()
    {

    }
    public void SetSelectedSong(SongModelView song)
    {
        if (song == null)
            return;
        SelectedSong =song;
        //ScrollToCurrentlyPlayingSong();
    }
    private void SubscribeToAlbumListChanges()
    {
        _subs.Add(
            AlbumsMgtFlow.SpecificAlbums
                .Subscribe(albums =>
                {
                    if (albums == null)
                        return;
                    if (albums.Count > 0)
                    {
                        SelectedAlbumsCol = _mapper.Map<ObservableCollection<AlbumModelView>>(albums);
                    
                        SelectedAlbum = SelectedAlbumsCol[0];

                        SelectedAlbumsSongs = SongsMgtFlow.GetSongsByAlbumId(SelectedAlbum.LocalDeviceId!)
                            .Select(s => _mapper.Map<SongModelView>(s))
                            .ToObservableCollection();
                    }
                })
        );
    }

    public void GetAlbumForSpecificSong(SongModelView song)
    {
        AlbumsMgtFlow.GetAlbumsBySongId(song.LocalDeviceId);
    }

    public void PlaySong(SongModelView song)
    {
        SelectedSong.IsCurrentPlayingHighlight = false;
        SelectedSong = song;
        baseViewModel.PlaySong(song, CurrentPage.SpecificAlbumPage, SelectedAlbumsSongs);
        
    }
}
