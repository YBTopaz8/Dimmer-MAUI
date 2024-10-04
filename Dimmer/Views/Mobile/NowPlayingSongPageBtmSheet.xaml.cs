

namespace Dimmer_MAUI.Views.Mobile;

public partial class NowPlayingSongPageBtmSheet : BottomSheet
{
	public NowPlayingSongPageBtmSheet(HomePageVM homePageVM)
    {
        InitializeComponent();
        this.HomePageVM = homePageVM;
        BindingContext = homePageVM;
    }

    public HomePageVM HomePageVM { get; }
    private async void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
    {
        await Shell.Current.GoToAsync("..", true);
    }

    private async void SwipedToDismiss_Swiped(object sender, SwipedEventArgs e)
    {
        await this.DismissAsync();
    }

    private async void ShowLyricsPage_Clicked(object sender, EventArgs e)
    {
        HomePageVM.SelectedSongToOpenBtmSheet = HomePageVM.TemporarilyPickedSong;
        await this.DismissAsync();
    }

   

    //private void CoverFlowView_ItemSwiped(CardsView view, PanCardView.EventArgs.ItemSwipedEventArgs args)
    //{
    //    if (args.Direction == PanCardView.Enums.ItemSwipeDirection.Right)
    //    {
    //        HomePageVM.PlayPreviousSongCommand.Execute(null);
    //    }
    //    else
    //    {
    //        HomePageVM.PlayNextSongCommand.Execute(null);
    //    }
    //}

    private async void ShowSongAlbum_Tapped(object sender, TappedEventArgs e)
    {
        if (HomePageVM.DisplayedSongs.Count < 1)
        {
            return;
        }
        HomePageVM.SelectedSongToOpenBtmSheet = HomePageVM.TemporarilyPickedSong;
        await HomePageVM.NavigateToArtistsPage(HomePageVM.SelectedSongToOpenBtmSheet);
        await this.DismissAsync();
        
    }

    private async void PlayPauseImgBtn_Clicked(object sender, EventArgs e)
    {
        await HomePageVM.PauseResumeSong();
    }

    private void Slider_DragCompleted(object sender, EventArgs e)
    {
        HomePageVM.SeekSongPosition();
    }

}