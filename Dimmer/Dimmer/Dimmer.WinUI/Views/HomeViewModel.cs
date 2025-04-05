


using Dimmer.UIUtils;

namespace Dimmer.WinUI.Views;
public partial class HomeViewModel : BaseViewModel
{
    [ObservableProperty]
    public partial CollectionView SongsCV { get; set; }
    public readonly BaseViewModel _base;
    private readonly IMapper _mapper;
    public HomeViewModel(BaseViewModel baseVm, IMapper mapper) : base(baseVm.BaseAppFlow, mapper)
    {
        _base = baseVm;
        _mapper= mapper;

    }

    public void PlaySongOnDoubleTap(SongModelView song)
    {
        base.SetSelectedSong(song);       
        base.PlaySong(song);
    }
    public void SetCollectionView(CollectionView collectionView)
    {
        SongsCV = collectionView;        
    }

    public void PointerEntered(SongModelView song, View mySelectedView)
    {
        Debug.WriteLine(song.GetType());

        GeneralViewUtil.PointerOnView(mySelectedView);
        base.SetSelectedSong(MySelectedSong);
    }
    public void PointerExited(View mySelectedView)
    {
        GeneralViewUtil.PointerOffView(mySelectedView);        
    }
}
