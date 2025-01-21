namespace Dimmer_MAUI.Views.Desktop;

public partial class PlaylistsPageD : ContentPage
{
    public HomePageVM MyViewModel { get; }

    public PlaylistsPageD(HomePageVM homePageVM)
    {
        InitializeComponent();
        BindingContext = homePageVM;
        MyViewModel = homePageVM;
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (MyViewModel.TemporarilyPickedSong is null)
        {
            return;
        }
        MyViewModel.CurrentPage = PageEnum.PlaylistsPage;
        MyViewModel.LoadFirstPlaylist();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        MyViewModel.DisplayedSongsFromPlaylist.Clear();
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        MyViewModel.CurrentQueue = 1;
        var t = (Border)sender;
        var song = t.BindingContext as SongModelView;
        MyViewModel.PlaySong(song);        
    }

    private void StateTrigger_IsActiveChanged(object sender, EventArgs e)
    {

    }
}