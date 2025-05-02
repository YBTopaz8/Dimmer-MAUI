using CommunityToolkit.Mvvm.Input;
using Dimmer.Services;
using Dimmer.WinUI.ViewModel;
using ListView = Microsoft.UI.Xaml.Controls.ListView;

namespace Dimmer.WinUI.Views;
public partial class HomeViewModel : BaseViewModelWin
{

    #region private fields   
    private readonly SubscriptionManager _subs;
    private readonly IMapper _mapper;
    private readonly IPlayerStateService _stateService;

    #endregion

    #region public properties
    [ObservableProperty]
    public partial CollectionView SongsCV { get; set; }
    public ListView? SongsListView { get; set; }    
    [ObservableProperty]
    public partial string? SearchText { get; set; }
    #endregion
    public HomeViewModel(
            IMapper mapper, BaseAppFlow baseAppFlow,
            AlbumsMgtFlow albumsMgtFlow,
            PlayListMgtFlow playlistsMgtFlow,
            SongsMgtFlow songsMgtFlow,
            IPlayerStateService stateService,
            ISettingsService settingsService,
            SubscriptionManager subs,
        LyricsMgtFlow lyricsMgtFlow
        ) : base(mapper, baseAppFlow, albumsMgtFlow, playlistsMgtFlow, songsMgtFlow, stateService, settingsService, subs, lyricsMgtFlow)
    {

        _mapper = mapper;
        _subs = subs;
        _stateService = stateService;
        LoadPageViewModel();
        SongsCV=new();
        TemporarilyPickedSong=new();

        SubscribeToLyricIndexChanges();
        SubscribeToSyncLyricsChanges();
        SubscribeToScanningLogs();
    }

    private static void LoadPageViewModel()
    {
        Debug.WriteLine("loaded page vm");
    }

    private void SubscribeToLyricIndexChanges()
    {
        _subs.Add(_stateService.CurrentLyric
            .DistinctUntilChanged()
            .Subscribe(l =>
            {
                if (l == null || string.IsNullOrEmpty(l.Text))
                    return;

                CurrentLyricPhrase.NowPlayingLyricsFontSize = 29;
                CurrentLyricPhrase = _mapper.Map<LyricPhraseModelView>(l);
                MainThread.BeginInvokeOnMainThread(
                   () =>
                   {
                       CurrentLyricPhrase.NowPlayingLyricsFontSize = 35;
                       SongLyricsCV.ScrollTo(CurrentLyricPhrase, null, ScrollToPosition.Center, true);
                   });
            }));
    }
    private void SubscribeToSyncLyricsChanges()
    {
        _subs.Add(_stateService.SyncLyrics
            .DistinctUntilChanged()
            .Subscribe(l =>
            {
                if (l == null || l.Count<1)
                    return;
                SynchronizedLyrics = _mapper.Map<ObservableCollection<LyricPhraseModelView>>(l);
            }));
    }
    [RelayCommand]
    public void ScrollToCurrentlyPlayingSong()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SongsCV?.ScrollTo(TemporarilyPickedSong, null, ScrollToPosition.Center, true);
        });
    }

    public void SetCurrentSong(SongModelView song)
    {
        if (song == null)
            return;
        TemporarilyPickedSong = song;
        //ScrollToCurrentlyPlayingSong();
    }

    public void PlaySongOnDoubleTap(SongModelView song)
    {
        PlaySong(song, CurrentPage.HomePage);
        var win = IPlatformApplication.Current!.Services.GetService<DimmerWin>()!;
        win.SetTitle(song);
    }
    public void SetCollectionView(CollectionView collectionView)
    {
        SongsCV = collectionView;
    }
    
    public void SetSongLyricsView(CollectionView collectionView)
    {
        SongLyricsCV = collectionView;
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
