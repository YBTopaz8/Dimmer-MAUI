
//using Dimmer.DimmerLive.Interfaces;
using ListView = Microsoft.UI.Xaml.Controls.ListView;

namespace Dimmer.WinUI.Views;
public partial class HomeViewModel : BaseViewModelWin
{

    #region private fields   
    private readonly SubscriptionManager _subs;
    private readonly IFolderMgtService folderMgtService;
    private readonly IFilePicker filePicker;
    private readonly IMapper _mapper;
    private readonly IDimmerStateService _stateService;

    #endregion

    #region public properties
    [ObservableProperty]
    public partial CollectionView SongsCV { get; set; }
    public ListView? SongsListView { get; set; }    
    [ObservableProperty]
    public partial string? SearchText { get; set; }
    #endregion
    public HomeViewModel(IMapper mapper,
        BaseAppFlow baseAppFlow,
        IDimmerLiveStateService dimmerLiveStateService,
        AlbumsMgtFlow albumsMgtFlow,
        PlayListMgtFlow playlistsMgtFlow,
        SongsMgtFlow songsMgtFlow,
        IDimmerStateService stateService,
        ISettingsService settingsService,
        SubscriptionManager subs,
        LyricsMgtFlow lyricsMgtFlow,
        IFolderMgtService folderMgtService,
        IFilePicker filePicker
    ) : base(mapper, baseAppFlow, dimmerLiveStateService, albumsMgtFlow, playlistsMgtFlow, songsMgtFlow, stateService, settingsService, subs, lyricsMgtFlow, folderMgtService, filePicker)
    {

        _mapper = mapper;
        _subs = subs;
        this.folderMgtService=folderMgtService;
        this.filePicker=filePicker;
        _stateService = stateService;
        LoadPageViewModel();
        SongsCV=new();
        TemporarilyPickedSong=new();

        SubscribeToLyricIndexChanges();
        SubscribeToSyncLyricsChanges();
        SubscribeToScanningLogs();
    }

    private void LoadPageViewModel()
    {
        var e = folderMgtService.AllFolders.ToList();
        ;
        Debug.WriteLine("loaded page ViewModel");
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

    public async void PlaySongOnDoubleTap(SongModelView song)
    {
      await  PlaySong(song, CurrentPage.HomePage);
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
