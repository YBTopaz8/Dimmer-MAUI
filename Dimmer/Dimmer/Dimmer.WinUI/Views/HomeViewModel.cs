


using Dimmer.UIUtils;
using System.Collections.ObjectModel;

namespace Dimmer.WinUI.Views;
public partial class HomeViewModel : ObservableObject
{

    #region private fields
    public BaseViewModel BaseVM;
    private readonly IMapper _mapper;
    #endregion

    #region public properties
    [ObservableProperty]
    public partial CollectionView? SongsCV { get; set; }


    [ObservableProperty]
    public partial ObservableCollection<SongModelView>? DisplayedSongs { get; set; }
    #endregion



    public HomeViewModel(BaseViewModel baseVm, IMapper mapper) 
    {
        BaseVM = baseVm;
        _mapper= mapper;
        BaseVM.SubscribeToCurrentlyPlayingSong();
        DisplayedSongs = BaseVM.MasterSongs;
    }


    public void PlaySongOnDoubleTap(SongModelView song)
    {     
        BaseVM.PlaySong(song);
    }
    public void SetCollectionView(CollectionView collectionView)
    {
        SongsCV = collectionView;        
    }

    public void PointerEntered(SongModelView song, View mySelectedView)
    {
        GeneralViewUtil.PointerOnView(mySelectedView);
        BaseVM.SetSelectedSong(song);
    }
    public void PointerExited(View mySelectedView)
    {
        GeneralViewUtil.PointerOffView(mySelectedView);        
    }
}
