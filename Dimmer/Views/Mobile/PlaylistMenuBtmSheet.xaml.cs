namespace Dimmer_MAUI.Views.Mobile;

public partial class PlaylistMenuBtmSheet : BottomSheet
{
	public PlaylistMenuBtmSheet(PlaylistsPageVM playlistsPageVM)
	{
		InitializeComponent();
		HasBackdrop = true;
        PlaylistsPageVM = playlistsPageVM;
		BindingContext = playlistsPageVM;
    }

    public PlaylistsPageVM PlaylistsPageVM { get; }
}