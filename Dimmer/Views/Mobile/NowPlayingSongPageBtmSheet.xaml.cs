namespace Dimmer_MAUI.Views.Mobile;

public partial class NowPlayingSongPageBtmSheet : BottomSheet
{
	public NowPlayingSongPageBtmSheet(HomePageVM homePageVM)
    {
        InitializeComponent();
        this.HomePageVM = homePageVM;
        BindingContext = homePageVM;
        this.Showing += NowPlayingSongPageBtmSheet_Showing;
    }

    private void NowPlayingSongPageBtmSheet_Showing(object? sender, EventArgs e)
    {
        if (HomePageVM.IsPlaying)
        {
            playImgBtn.IsVisible = false;
            pauseImgBtn.IsVisible = true;
        }
        else
        {
            playImgBtn.IsVisible = true;
            pauseImgBtn.IsVisible = false;
        }
    }

    public HomePageVM HomePageVM { get; }
    private async void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
    {
        await Shell.Current.GoToAsync("..", true);
    }

    private void PlayImgBtn_Clicked(object sender, EventArgs e)
    {
        HomePageVM.PauseResumeSongCommand.Execute(null);
        playImgBtn.IsVisible = false;
        pauseImgBtn.IsVisible = true;
    }

    private void PauseImgBtn_Clicked(object sender, EventArgs e)
    {
        HomePageVM.PauseResumeSongCommand.Execute(null);
        playImgBtn.IsVisible = true;
        pauseImgBtn.IsVisible = false;
    }

    private void SwipedToNext_Swiped(object sender, SwipedEventArgs e)
    {
        HomePageVM.PlayNextSongCommand.Execute(null);
    }

    private void SwipedToPrevious_Swiped(object sender, SwipedEventArgs e)
    {
        HomePageVM.PlayPreviousSongCommand.Execute(null);
    }

    private async void SwipedToDismiss_Swiped(object sender, SwipedEventArgs e)
    {
        await this.DismissAsync();
    }
}