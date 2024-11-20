namespace Dimmer_MAUI.Views.Desktop;

public partial class PlaylistsPageD : ContentPage
{
    public HomePageVM HomePageVM { get; }

    public PlaylistsPageD(HomePageVM homePageVM)
    {
        InitializeComponent();
        BindingContext = homePageVM;
        HomePageVM = homePageVM;
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (HomePageVM.TemporarilyPickedSong is null)
        {
            return;
        }
        HomePageVM.CurrentPage = PageEnum.PlaylistsPage;
        HomePageVM.LoadFirstPlaylist();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        HomePageVM.DisplayedSongsFromPlaylist.Clear();
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        HomePageVM.CurrentQueue = 1;
        var t = (Border)sender;
        var song = t.BindingContext as SongModelView;
        HomePageVM.PlaySongCommand.Execute(song);        
    }

    private void StateTrigger_IsActiveChanged(object sender, EventArgs e)
    {

    }
}