using The49.Maui.BottomSheet;

namespace Dimmer_MAUI.Views.Mobile;

public partial class NowPlayingBtmPage : BottomSheet
{
	public NowPlayingBtmPage(HomePageVM homePageVM)
	{
		InitializeComponent();
        HomePageVM = homePageVM;
        this.BindingContext = homePageVM;
    }

    public HomePageVM HomePageVM { get; }

    private void syncCol_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            SyncedLyricsColView.ScrollTo(HomePageVM.CurrentLyricPhrase);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message + " When scrolling");
        }

    }

    private async void BringNowPlayBtmSheetDownBtn_Clicked(object sender, TouchEventArgs e)
    {
        await this.DismissAsync();
    }

    
}