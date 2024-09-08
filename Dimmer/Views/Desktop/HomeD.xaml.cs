namespace Dimmer_MAUI.Views.Desktop;

public partial class HomeD : UraniumContentPage
{
    public HomeD(HomePageVM homePageVM)
    {
        InitializeComponent();
        HomePageVM = homePageVM;
        this.BindingContext = homePageVM;

        MediaPlayBackCW.BindingContext = homePageVM;

    }

    public HomePageVM HomePageVM { get; }


    private void SongsColView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SongsColView.IsLoaded)
        {
            SongsColView.ScrollTo(HomePageVM.PickedSong, ScrollToPosition.Center, animate: false);
        }
        //This crashes the app :(
    }


    DateTime lastKeyStroke;
    private async void SearchSongSB_TextChanged(object sender, TextChangedEventArgs e)
    {
        lastKeyStroke = DateTime.Now;
        var thisKeyStroke = lastKeyStroke;
        await Task.Delay(750);
        if (thisKeyStroke == lastKeyStroke)
        {
            var searchText = e.NewTextValue;
            if (searchText.Length >= 2)
            {
                HomePageVM.SearchSongCommand.Execute(searchText);
            }
            else
            {
                HomePageVM.SearchSongCommand.Execute(string.Empty);

                await Task.Delay(1500);
                if (SongsColView.IsLoaded)
                {
                    SongsColView.ScrollTo(HomePageVM.PickedSong, ScrollToPosition.Center, animate: true);
                }
            }
        }
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (HomePageVM.PickedSong is null)
            {
                HomePageVM.PickedSong = HomePageVM.TemporarilyPickedSong;
            }
            SongsColView.ScrollTo(HomePageVM.PickedSong, position: ScrollToPosition.Center, animate: false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error when scrolling "+ex.Message);
        }
    }


    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        
    }
    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        HomePageVM.SwitchViewNowPlayingPageCommand.Execute(0);
        HomePageVM.IsOnLyricsSyncMode = false;
    }
    private void SongsColView_Loaded(object sender, EventArgs e)
    {
        if (SongsColView.IsLoaded)
        {
            SongsColView.ScrollTo(HomePageVM.TemporarilyPickedSong, null, ScrollToPosition.Center, animate: false);
            SongsColView.SelectedItem = HomePageVM.TemporarilyPickedSong;
        }
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        HomePageVM.CurrentQueue = 0;
        var t = (Grid)sender;
        var song = t.BindingContext as SongsModelView;
        HomePageVM.PlaySongCommand.Execute(song);
    }

    private void MenuFlyoutItem_Clicked(object sender, EventArgs e)
    {
        SearchSongSB.Focus();
    }

    private void StateTrigger_IsActiveChanged(object sender, EventArgs e)
    {
       
    }


    private async void ShowArtistSongs_Clicked(object sender, EventArgs e)
    {
        var send = (MenuFlyoutItem)sender;
        var song = send.BindingContext! as SongsModelView;
        await HomePageVM.NavigateToArtistsPage(song.Id);
    }
}