using UraniumUI.Material.Attachments;

namespace Dimmer_MAUI.Views.Mobile;

public partial class NowPlayingBottomPage : BottomSheetView
{
	public NowPlayingBottomPage(HomePageVM homePageVM)
    {
        InitializeComponent();
        this.HomePageVM = homePageVM;
        BindingContext = homePageVM;
        
        CloseOnTapOutside = false;
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

    
}