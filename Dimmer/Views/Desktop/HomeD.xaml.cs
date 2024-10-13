using System.Diagnostics;

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
    protected override void OnAppearing()
    {
        base.OnAppearing();
        HomePageVM.CurrentPage = PageEnum.MainPage;
        if (SongsColView.IsLoaded)
        {
            SongsColView.ScrollTo(HomePageVM.TemporarilyPickedSong, ScrollToPosition.Start, animate: false);
        }
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

                await Task.Delay(500);
                if (SongsColView.IsLoaded)
                {
                    SongsColView.ScrollTo(HomePageVM.PickedSong, ScrollToPosition.Start, animate: true);
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
            SongsColView.ScrollTo(HomePageVM.TemporarilyPickedSong, position: ScrollToPosition.Center, animate: false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error when scrolling "+ex.Message);
        }
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
        HomePageVM.SwitchViewNowPlayingPageCommand.Execute(0);
        HomePageVM.IsOnLyricsSyncMode = false;
    }
    private void SongsColView_Loaded(object sender, EventArgs e)
    {
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

    private async void ShowArtistSongs_Clicked(object sender, EventArgs e)
    {
        var send = (MenuFlyoutItem)sender;
        var song = send.BindingContext! as SongsModelView;
        await HomePageVM.NavigateToArtistsPage(song);
    }

    private void PointerGestureRecognizer_PointerPressed(object sender, PointerEventArgs e)
    {

#if WINDOWS

#endif
    }

    private void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {
        var send = (Grid)sender;
        var song = send.BindingContext! as SongsModelView;
        HomePageVM.SetContextMenuSong(song);
    }

    private void SongsColView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SongsColView.IsLoaded)
        {
            
            SongsColView.ScrollTo(HomePageVM.TemporarilyPickedSong, null, ScrollToPosition.Start, animate: false);
        }

    }
}