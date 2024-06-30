namespace Dimmer_MAUI.Views.Mobile;

public partial class PlaylistsPageM : ContentPage
{
	public PlaylistsPageM(PlaylistsPageVM playlistsPageVM)
	{
		InitializeComponent();
        PlaylistsPageVM = playlistsPageVM;
        BindingContext = playlistsPageVM;
    }

    public PlaylistsPageVM PlaylistsPageVM { get; }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {

    }
}