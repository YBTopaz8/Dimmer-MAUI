

using CommunityToolkit.Mvvm.Input;
using Dimmer.Data.ModelView;
using Dimmer.DimmerLive.Interfaces;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace Dimmer.Views;
public partial class HomePageViewModel : BaseViewModelAnd
{
    #region private fields   
    private readonly SubscriptionManager _subs;
    private readonly IMapper _mapper;
    private readonly IDimmerStateService _stateService;

    #endregion

    [ObservableProperty]
    public partial DXCollectionView SongsCV { get; set; }


    [ObservableProperty]
    public partial string? SearchText { get; set; }
    public HomePageViewModel
        (IMapper mapper,
        BaseAppFlow baseAppFlow,
        IDimmerLiveStateService dimmerLiveStateService,
        AlbumsMgtFlow albumsMgtFlow,
        PlayListMgtFlow playlistsMgtFlow,
        SongsMgtFlow songsMgtFlow,
        IDimmerStateService stateService,
        ISettingsService settingsService,
        SubscriptionManager subs,
        LyricsMgtFlow lyricsMgtFlow,
        IFolderMgtService folderMgtService
    ) : base(mapper, baseAppFlow, dimmerLiveStateService, albumsMgtFlow, playlistsMgtFlow, songsMgtFlow, stateService, settingsService, subs, lyricsMgtFlow, folderMgtService)
    {
            _mapper = mapper;
        _subs = subs;
        _stateService = stateService;
        LoadPageViewModel();
        SongsCV=new();
        TemporarilyPickedSong=new();


        SubscribeToLyricIndexChanges();
        SubscribeToSyncLyricsChanges();
    }
    public void LoadPageViewModel()
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
                       
                       int itemHandle = SongLyricsCV.FindItemHandle(TemporarilyPickedSong);
                       SongLyricsCV.ScrollTo(itemHandle, DevExpress.Maui.Core.DXScrollToPosition.Start);
                       
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
            int itemHandle = SongsCV.FindItemHandle(TemporarilyPickedSong);
            SongsCV?.ScrollTo(itemHandle,DevExpress.Maui.Core.DXScrollToPosition.Start);
            HapticFeedback.Perform(HapticFeedbackType.LongPress);
        });
    }


    public void SetCollectionView(DXCollectionView collectionView)
    {
        SongsCV = collectionView;
    }

    public void SetSongLyricsView(DXCollectionView collectionView)
    {
        SongLyricsCV = collectionView;
    }

}
