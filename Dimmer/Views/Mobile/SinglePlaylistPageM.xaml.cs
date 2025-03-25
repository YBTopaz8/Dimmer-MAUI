namespace Dimmer_MAUI.Views.Mobile;

public partial class SinglePlaylistPageM : ContentPage
{
    NowPlayingBtmSheet btmSheet { get; set; }
    public SinglePlaylistPageM(PlaylistVM playlistsPageVM)
	{
		InitializeComponent();
        MyViewModel = playlistsPageVM;
        BindingContext = playlistsPageVM;
        btmSheet = IPlatformApplication.Current!.Services.GetService<NowPlayingBtmSheet>()!;
        //this.Attachments.Add(btmSheet);
    }

    public PlaylistVM MyViewModel { get; }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        this.Title = MyViewModel.SelectedPlaylistPageTitle;
        MyViewModel.CurrentPage = PageEnum.PlaylistsPage;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        MyViewModel.SelectedPlaylist.DisplayedSongsFromPlaylist.Clear();
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        MyViewModel.CurrentQueue = 1;
        var view = (FlexLayout)sender;
        var song = view.BindingContext as SongModelView;
        MyViewModel.PlaySong(song);
    }
}