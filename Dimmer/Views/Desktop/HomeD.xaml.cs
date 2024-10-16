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

    int countt;

    public HomePageVM HomePageVM { get; }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        HomePageVM.CurrentPage = PageEnum.MainPage;
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

    private void ScrollToSong_Clicked(object sender, EventArgs e)
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
    int coon;
    private void SongsColView_Loaded(object sender, EventArgs e)
    {
        Debug.WriteLine("refreshes " + coon++);
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

    private async void ShowArtistSongs_Clicked(object sender, EventArgs e)
    {
        var send = (MenuFlyoutItem)sender;
        var song = send.BindingContext! as SongsModelView;
        await HomePageVM.NavigateToArtistsPage(song);
    }


    bool isPointerEntered;
    private void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {
        var send = (Grid)sender;
        var song = send.BindingContext! as SongsModelView;
        HomePageVM.SetContextMenuSong(song);
        isPointerEntered = true;
    }

    private void SongsColView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SongsColView.IsLoaded && !isPointerEntered)
        {
            SongsColView.ScrollTo(HomePageVM.PickedSong, null, ScrollToPosition.Center, animate: false);
        }
    }

    private void PointerGestureRecognizer_PointerExited(object sender, PointerEventArgs e)
    {
        isPointerEntered = false;
    }
}