using Dimmer.Data.ModelView;
using Dimmer.DimmerLive.Interfaces;
using Dimmer.Utilities.Enums;
using Dimmer.ViewModel;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace Dimmer.ViewModels;
public partial class BaseViewModelAnd : BaseViewModel, IDisposable
{
    [ObservableProperty]
    public partial int CurrentQueue { get; set; }
    [ObservableProperty]
    public partial int SelectedItemIndexMobile { get; set; }
    private readonly SubscriptionManager _subs;

    [ObservableProperty]
    public partial ObservableCollection<SongModelView>? DisplayedSongs { get; set; }
    

    [ObservableProperty]
    public partial DXCollectionView SongLyricsCV { get; set; }

    [ObservableProperty]
    public partial List<SongModelView>? FilteredSongs { get; set; }
    private readonly IDimmerStateService _stateService;

    private readonly IMapper _mapper;
    public BaseViewModelAnd(IMapper mapper,
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
    ) : base(mapper, baseAppFlow,dimmerLiveStateService,  albumsMgtFlow, playlistsMgtFlow, songsMgtFlow, stateService, settingsService, subs, lyricsMgtFlow,folderMgtService)
    {
        _mapper = mapper;
        _stateService = stateService;
        _subs = subs;

        ResetDisplayedMasterList();
        SubscribeToLyricIndexChanges();
        SongLyricsCV = new DXCollectionView();

        SubscribeToScanningLogs();
    }


    private void SubscribeToLyricIndexChanges()
    {
        _subs.Add(_stateService.CurrentLyric
            .DistinctUntilChanged()
            .Subscribe(l =>
            {
                
                if (l == null || SongLyricsCV is null)
                    return;
                CurrentLyricPhrase = _mapper.Map<LyricPhraseModelView>(l);
                MainThread.BeginInvokeOnMainThread(
                    () =>
                    {
                        var s = SongLyricsCV.FindItemHandle(CurrentLyricPhrase);
                        var ind = SongLyricsCV.GetItemVisibleIndex(s);
                        SongLyricsCV.ScrollTo(ind, DevExpress.Maui.Core.DXScrollToPosition.Start);

                    });


            }));
    }
    public async Task SelectSongFromFolderAndroid()
    {
        PermissionStatus status = await Permissions.RequestAsync<CheckPermissions>();

        await SelectSongFromFolder();
    }

    public void ResetDisplayedMasterList()
    {

        // Initialize displayed songs to the full master list
        if (BaseAppFlow.MasterList!= null)
        {
            var e = _mapper.Map<ObservableCollection<SongModelView>>(BaseAppFlow.MasterList);
            DisplayedSongs = [.. e];
        }

    }

    public void LoadAndPlaySongTapped(SongModelView song)
    {
        PlaySong(song, CurrentPage.HomePage);

    }


}
