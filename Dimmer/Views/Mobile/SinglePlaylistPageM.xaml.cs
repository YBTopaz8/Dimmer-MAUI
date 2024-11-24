namespace Dimmer_MAUI.Views.Mobile;

public partial class SinglePlaylistPageM : ContentPage
{
    NowPlayingBtmSheet btmSheet { get; set; }
    public SinglePlaylistPageM(HomePageVM playlistsPageVM, HomePageVM homePageVM)
	{
		InitializeComponent();
        HomePageVM = homePageVM;
        BindingContext = playlistsPageVM;
        btmSheet = IPlatformApplication.Current!.Services.GetService<NowPlayingBtmSheet>()!;
        //this.Attachments.Add(btmSheet);
    }

    public HomePageVM HomePageVM { get; }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        this.Title = HomePageVM.SelectedPlaylistPageTitle;
        HomePageVM.CurrentPage = PageEnum.PlaylistsPage;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        HomePageVM.DisplayedSongsFromPlaylist.Clear();
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        HomePageVM.CurrentQueue = 1;
        var view = (FlexLayout)sender;
        var song = view.BindingContext as SongModelView;
        HomePageVM.PlaySongCommand.Execute(song);
    }
}