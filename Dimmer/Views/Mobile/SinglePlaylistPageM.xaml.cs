namespace Dimmer_MAUI.Views.Mobile;

public partial class SinglePlaylistPageM : ContentPage
{
	public SinglePlaylistPageM(PlaylistsPageVM playlistsPageVM)
	{
		InitializeComponent();
        PlaylistsPageVM = playlistsPageVM;
        BindingContext = playlistsPageVM;
    }

    public PlaylistsPageVM PlaylistsPageVM { get; }
}