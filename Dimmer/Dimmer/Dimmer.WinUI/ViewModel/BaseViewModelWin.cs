﻿using Dimmer.Services;
using Dimmer.WinUI.Utils.Helpers;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using static Vanara.PInvoke.Shell32;

namespace Dimmer.WinUI.ViewModel;

public partial class BaseViewModelWin : BaseViewModel, IDisposable
{
    [ObservableProperty]
    public partial int CurrentQueue { get; set; }
    [ObservableProperty]
    public partial bool IsMainViewVisible { get; set; } = true;
    private readonly SubscriptionManager _subs;

    [ObservableProperty]
    public partial ObservableCollection<SongModelView>? DisplayedSongs { get; set; }

    [ObservableProperty]
    public partial CollectionView? SongLyricsCV { get; set; }

    private readonly IDimmerStateService _stateService;

    private readonly IMapper _mapper;

    

    private TrayIconHelper? _trayIconHelper;

    public BaseViewModelWin(
        IMapper mapper, BaseAppFlow baseAppFlow,
        AlbumsMgtFlow albumsMgtFlow,
        PlayListMgtFlow playlistsMgtFlow,
        SongsMgtFlow songsMgtFlow,
        IDimmerStateService stateService,
        ISettingsService settingsService,
        SubscriptionManager subs,
        LyricsMgtFlow lyricsMgtFlow
    ) : base(mapper, baseAppFlow, albumsMgtFlow, playlistsMgtFlow, songsMgtFlow, stateService, settingsService, subs, lyricsMgtFlow)
    {
        _mapper = mapper;
        _stateService = stateService;
        _subs = subs;


        if (AppUtils.IsUserFirstTimeOpening)
        {
            IsMainViewVisible = false;
            return;
        }

        ResetDisplayedMasterList();
        SubscribeToLyricIndexChanges();

        SubscribeToPosition();


    }
    private void SubscribeToPosition()
    {

        
        _subs.Add(SongsMgtFlow.Position
            .Synchronize(SynchronizationContext.Current!)
        .Subscribe(pos =>
        {
            if (pos == 0)
            {
                TaskbarList.SetProgressState(PlatUtils.DimmerHandle, (TaskbarButtonProgressState)TBPFLAG.TBPF_NORMAL);

                return;
            }
                CurrentPositionInSeconds = pos;
                var duration = SongsMgtFlow.CurrentlyPlayingSong?.DurationInSeconds ?? 1;
                CurrentPositionPercentage = pos / duration;
                MainThread.BeginInvokeOnMainThread(
               () =>
               {
                   TaskbarList.SetProgressValue(PlatUtils.DimmerHandle, (ulong)CurrentPositionPercentage*100, 100);
               });

        }));
    }
    private void SubscribeToLyricIndexChanges()
    {
        _subs.Add(_stateService.CurrentLyric
            .DistinctUntilChanged()
            .Subscribe(l =>
            {
                if (l == null)
                    return;
                CurrentLyricPhrase = _mapper.Map<LyricPhraseModelView>(l);
                MainThread.BeginInvokeOnMainThread(
                    () =>
                    {
                        SongLyricsCV?.ScrollTo(CurrentLyricPhrase, null, ScrollToPosition.Center, true);
                    });


            }));
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


    public void Dispose()
    {
        // if you registered any additional subscriptions here, dispose them
    }
}
