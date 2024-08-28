

namespace Dimmer_MAUI.Views.Mobile;

public partial class PlaylistMenuBtmSheet : BottomSheet
{
	public PlaylistMenuBtmSheet(HomePageVM homePageVM)
	{
		InitializeComponent();
		HasBackdrop = true;
        HomePageVM = homePageVM;
		BindingContext = homePageVM;
    }

    public HomePageVM HomePageVM { get; }
}