using Dimmer.Services;

namespace Dimmer.WinUI.Views;
public partial class SingleSongPageViewModel : BaseViewModel
{
    public SingleSongPageViewModel(IMapper mapper, BaseAppFlow baseAppFlow, AlbumsMgtFlow albumsMgtFlow, PlayListMgtFlow playlistsMgtFlow, SongsMgtFlow songsMgtFlow, IDimmerStateService stateService, ISettingsService settingsService, SubscriptionManager subs,
        LyricsMgtFlow lyricsMgtFlow) : base(mapper, baseAppFlow, albumsMgtFlow, playlistsMgtFlow, songsMgtFlow, stateService, settingsService, subs, lyricsMgtFlow)
    {

        LoadPageViewModel();
    }
    #region private fields   
    #endregion


    #region public properties
    [ObservableProperty]
    public partial CollectionView? SongLyricsCV { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<LyricsDownloadContent>? CurrentListOfDownloadedLyrics { get; internal set; }
    #endregion
   

    private void LoadPageViewModel()
    {
        
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
