


using Dimmer.Orchestration;
using Dimmer.UIUtils;
using Dimmer.Utilities.Enums;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media;

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

    [ObservableProperty]
    public partial CollectionView? LyricsCollectionView { get; set; }

    [ObservableProperty]
    public partial bool IsOnSearchMode { get; set; }
    [ObservableProperty]
    public partial int CurrentQueue { get; set; }
    [ObservableProperty]
    public partial string? SearchText { get; set; }
    [ObservableProperty]
    public partial double CurrentPositionInSecondsUI { get; set; } = 0;
    [ObservableProperty]
    public partial double CurrentPositionPercentageUI { get; set; } = 0;
    #endregion


    public HomeViewModel(SongsMgtFlow songsMgt, IMapper mapper, IDimmerAudioService dimmerAudioService) : base(mapper, songsMgt, dimmerAudioService)
    {

        _mapper = mapper;
        LoadPageViewModel();
    }

    private void LoadPageViewModel()
    {
        CurrentPositionInSecondsUI=base.CurrentPositionInSeconds;
        CurrentPositionPercentageUI=base.CurrentPositionPercentage;
        if (base.MasterSongs is not null)
        {
            DisplayedSongs = [.. MasterSongs];
        }
    }

    public new void SubscribeToCurrentlyPlayingSong()
    {
        
        SongsMgtFlow.CurrentSong.DistinctUntilChanged()
            .Subscribe(song =>
        {
            if (song is not null)
            {
                TemporarilyPickedSong = _mapper.Map<SongModelView>(song);                
            }
        });
    }

    public async Task PlaySongOnDoubleTap(SongModelView song)
    {
        await PlaySong(song);
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
    public static void PointerExited(View mySelectedView)
    {
        GeneralViewUtil.PointerOffView(mySelectedView);
    }
}
