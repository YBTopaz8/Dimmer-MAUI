﻿using Dimmer.WinUI.ViewModel;
using ListView = Microsoft.UI.Xaml.Controls.ListView;

namespace Dimmer.WinUI.Views;
public partial class HomeViewModel : BaseViewModelWin
{

    #region private fields   

    #endregion

    #region public properties
    [ObservableProperty]
    public partial CollectionView? SongsCV { get; set; }
    public ListView? SongsListView { get; set; }    
    [ObservableProperty]
    public partial string? SearchText { get; set; }
    #endregion


    public HomeViewModel(SongsMgtFlow songsMgt, IMapper mapper, IDimmerAudioService dimmerAudioService) : base(mapper, songsMgt, dimmerAudioService)
    {

        LoadPageViewModel();
    }

    private void LoadPageViewModel()
    {
    }


    public async Task PlaySongOnDoubleTap(SongModelView song)
    {
        await PlaySong(song);
    }
    public void SetCollectionView(CollectionView collectionView)
    {
        SongsCV = collectionView;
    }
    
    public void SetLyricsView(ListView colView)
    {
        SongsListView = colView;
    }
    
    public void ScrollAfterAppearing(CollectionView collectionView)
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
