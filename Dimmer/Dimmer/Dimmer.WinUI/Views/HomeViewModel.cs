using CommunityToolkit.Mvvm.Input;
using Dimmer.Services;
using Dimmer.WinUI.ViewModel;
using ListView = Microsoft.UI.Xaml.Controls.ListView;

namespace Dimmer.WinUI.Views;
public partial class HomeViewModel : BaseViewModelWin
{

    #region private fields   

    #endregion

    #region public properties
    [ObservableProperty]
    public partial CollectionView SongsCV { get; set; }
    public ListView? SongsListView { get; set; }    
    [ObservableProperty]
    public partial string? SearchText { get; set; }
    #endregion
    public HomeViewModel(
            IMapper mapper,
            AlbumsMgtFlow albumsMgtFlow,
            PlayListMgtFlow playlistsMgtFlow,
            SongsMgtFlow songsMgtFlow,
            IPlayerStateService stateService,
            ISettingsService settingsService,
            SubscriptionManager subs
        ) : base(mapper, albumsMgtFlow, playlistsMgtFlow, songsMgtFlow, stateService, settingsService, subs)
    { 
    

        LoadPageViewModel();
        SongsCV=new();
        TemporarilyPickedSong=new();
    }

    private static void LoadPageViewModel()
    {
        Debug.WriteLine("loaded page vm");
    }

    [RelayCommand]
    public void ScrollToCurrentlyPlayingSong()
    {
        SongsCV?.ScrollTo(TemporarilyPickedSong, null, ScrollToPosition.Center,true);
    }

    public void PlaySongOnDoubleTap(SongModelView song)
    {
        PlaySong(song);
    }
    public void SetCollectionView(CollectionView collectionView)
    {
        SongsCV = collectionView;
    }
    
    public void SetLyricsView(ListView colView)
    {
        SongsListView = colView;
    }
    

    public void PointerEntered(SongModelView song, View mySelectedView)
    {
        GeneralViewUtil.PointerOnView(mySelectedView);
        //SetSelectedSong(song);
    }
    public static void PointerExited(View mySelectedView)
    {
        GeneralViewUtil.PointerOffView(mySelectedView);
    }
}
