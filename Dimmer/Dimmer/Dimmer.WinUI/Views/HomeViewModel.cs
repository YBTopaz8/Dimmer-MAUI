


using Dimmer.Orchestration;
using Dimmer.UIUtils;
using Dimmer.Utilities.Enums;
using System.Collections.ObjectModel;

namespace Dimmer.WinUI.Views;
public partial class HomeViewModel : BaseViewModel
{

    #region private fields    
    
    private readonly IMapper _mapper;
    #endregion

    #region public properties
    [ObservableProperty]
    public partial CollectionView? SongsCV { get; set; }


    [ObservableProperty]
    public partial ObservableCollection<SongModelView>? DisplayedSongs { get; set; }
    [ObservableProperty]
    public partial List<SongModelView>? FilteredSongs { get; set; }
    #endregion


    [ObservableProperty]
    public partial bool IsOnSearchMode { get; set; }
    [ObservableProperty]
    public partial CurrentPage CurrentlySelectedPage { get; set; }
    [ObservableProperty]
    public partial int CurrentQueue { get; set; }
    [ObservableProperty]
    public partial string SearchText { get; set; }

    public HomeViewModel(SongsMgtFlow songsMgt, IMapper mapper, IDimmerAudioService dimmerAudioService) : base(mapper, songsMgt, dimmerAudioService)
    {
        
        _mapper = mapper;
        //SubscribeToIsPlaying();
        //SubscribeToCurrentPosition();
        DisplayedSongs = new ObservableCollection<SongModelView>(MasterSongs);
        CurrentPositionInSecondsUI=base.CurrentPositionInSeconds;
        CurrentPositionPercentageUI=base.CurrentPositionPercentage;
    }

    [ObservableProperty]
    public partial double CurrentPositionInSecondsUI { get; set; } = 0;
    [ObservableProperty]
    public partial double CurrentPositionPercentageUI { get; set; } = 0;
    public new void SubscribeToCurrentlyPlayingSong()
    {
        
        SongsMgtFlow.CurrentSong.DistinctUntilChanged()
            .Subscribe(song =>
        {
            if (song is not null)
            {
                TemporarilyPickedSong = _mapper.Map<SongModelView>(song);
                //SongsCV?.ScrollTo(TemporarilyPickedSong);
            }
        });
    }

    public void PlaySongOnDoubleTap(SongModelView song)
    {
        PlaySong(song);
    }
    public void SetCollectionView(CollectionView collectionView)
    {
        SongsCV = collectionView;
    }

    public void PointerEntered(SongModelView song, View mySelectedView)
    {
        GeneralViewUtil.PointerOnView(mySelectedView);
        SetSelectedSong(song);
    }
    public void PointerExited(View mySelectedView)
    {
        GeneralViewUtil.PointerOffView(mySelectedView);
    }
}
