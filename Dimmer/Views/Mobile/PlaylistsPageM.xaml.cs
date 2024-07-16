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

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        PlaylistsPageVM.UpdatePlayLists();
    }
}