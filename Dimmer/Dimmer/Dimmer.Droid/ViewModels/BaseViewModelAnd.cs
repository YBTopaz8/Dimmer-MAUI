using AutoMapper;
using CommunityToolkit.Mvvm.ComponentModel;
using DevExpress.Maui.CollectionView;
using Dimmer.Data.ModelView;
using Dimmer.Interfaces;
using Dimmer.Orchestration;
using Dimmer.Services;
using Dimmer.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.ViewModels;
public partial class BaseViewModelAnd : BaseViewModel, IDisposable
{
    [ObservableProperty]
    public partial int CurrentQueue { get; set; }
    private readonly SubscriptionManager _subs;

    [ObservableProperty]
    public partial ObservableCollection<SongModelView>? DisplayedSongs { get; set; }

    [ObservableProperty]
    public partial DXCollectionView SongLyricsCV { get; set; }

    [ObservableProperty]
    public partial List<SongModelView>? FilteredSongs { get; set; }
    private readonly IPlayerStateService _stateService;

    private readonly IMapper _mapper;
    public BaseViewModelAnd(IMapper mapper,
        AlbumsMgtFlow albumsMgtFlow,
        PlayListMgtFlow playlistsMgtFlow,
        SongsMgtFlow songsMgtFlow,
        IPlayerStateService stateService,
        ISettingsService settingsService,
        SubscriptionManager subs,
        LyricsMgtFlow lyricsMgtFlow
    ) : base(mapper, albumsMgtFlow, playlistsMgtFlow, songsMgtFlow, stateService, settingsService, subs, lyricsMgtFlow)
    {
        _mapper = mapper;
        _stateService = stateService;
        _subs = subs;

        ResetDisplayedMasterList();
        SubscribeToLyricIndexChanges();
        SongLyricsCV = new DXCollectionView();
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
                        var s = SongLyricsCV.FindItemHandle(CurrentLyricPhrase);
                        var ind = SongLyricsCV.GetItemVisibleIndex(s);
                        SongLyricsCV.ScrollTo(ind, DevExpress.Maui.Core.DXScrollToPosition.Start);

                    });


            }));
    }
    private void ResetDisplayedMasterList()
    {
        // Initialize displayed songs to the full master list
        if (MasterListOfSongs != null)
            DisplayedSongs = MasterListOfSongs;

    }

}
