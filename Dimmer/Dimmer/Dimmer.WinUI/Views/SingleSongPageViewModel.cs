


using Dimmer.Orchestration;
using Dimmer.UIUtils;
using Dimmer.Utilities.Enums;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Dimmer.WinUI.Views;
public partial class SingleSongPageViewModel : BaseViewModel
{
    #region private fields   
    #endregion


    #region public properties
    [ObservableProperty]
    public partial CollectionView? SongLyricsCV { get; set; }

    #endregion
    public SingleSongPageViewModel(SongsMgtFlow songsMgt, IMapper mapper, IDimmerAudioService dimmerAudioService) : base(mapper, songsMgt, dimmerAudioService)
    {
        LoadPageViewModel();
    }

    private void LoadPageViewModel()
    {
        throw new NotImplementedException();
    }
    public void SetCollectionView(CollectionView collectionView)
    {
        SongLyricsCV = collectionView;
    }
    public void UnSetCollectionView()
    {
        SongLyricsCV = null;
    }
}
