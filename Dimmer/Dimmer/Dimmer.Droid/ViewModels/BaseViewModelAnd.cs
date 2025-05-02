using Dimmer.Data.ModelView;
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
    public partial ObservableCollection<string>? ScanningLogs { get; set; }
    [ObservableProperty]
    public partial string? LatestScanningLog { get; set; }

    [ObservableProperty]
    public partial DXCollectionView SongLyricsCV { get; set; }

    [ObservableProperty]
    public partial List<SongModelView>? FilteredSongs { get; set; }
    private readonly IPlayerStateService _stateService;

    private readonly IMapper _mapper;
    public BaseViewModelAnd(IMapper mapper,
        BaseAppFlow baseAppFlow,
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
        _stateService = stateService;
        _subs = subs;

        ResetDisplayedMasterList();
        SubscribeToLyricIndexChanges();
        SongLyricsCV = new DXCollectionView();

        SubscribeToScanningLogs();
    }

    private void SubscribeToScanningLogs()
    {
        _subs.Add(_stateService.LatestDeviceLog.DistinctUntilChanged()            
            .Subscribe(log =>
            {
                if (log == null || string.IsNullOrEmpty(log.Log))
                    return;
                LatestScanningLog = log.Log;
                ScanningLogs ??= new ObservableCollection<string>();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (ScanningLogs.Count > 10)
                        ScanningLogs.RemoveAt(0);
                    ScanningLogs.Add(log.Log);
                });
            }));
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



}
