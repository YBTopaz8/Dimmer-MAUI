﻿


namespace Dimmer.WinUI.Views;
public partial class SingleSongPageViewModel : BaseViewModel
{
    #region private fields   
    #endregion


    #region public properties
    [ObservableProperty]
    public partial CollectionView? SongLyricsCV { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<LyricsDownloadContent>? CurrentListOfDownloadedLyrics { get; internal set; }
    #endregion
    public SingleSongPageViewModel(SongsMgtFlow songsMgt, IMapper mapper, IDimmerAudioService dimmerAudioService) : base(mapper, songsMgt, dimmerAudioService)
    {
        LoadPageViewModel();
    }

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
