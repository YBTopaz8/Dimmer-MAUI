

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

    private async void ImageButton_Clicked(object sender, EventArgs e)
    {
        HomePageVM.SelectedSongToOpenBtmSheet = HomePageVM.TemporarilyPickedSong;
        await this.DismissAsync();
    }

   

    private void CoverFlowView_ItemSwiped(CardsView view, PanCardView.EventArgs.ItemSwipedEventArgs args)
    {
        if (args.Direction == PanCardView.Enums.ItemSwipeDirection.Right)
        {
            HomePageVM.PlayPreviousSongCommand.Execute(null);
        }
        else
        {
            HomePageVM.PlayNextSongCommand.Execute(null);
        }
    }
}