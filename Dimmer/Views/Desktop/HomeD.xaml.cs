namespace Dimmer_MAUI.Views.Desktop;

public partial class HomeD : UraniumContentPage
{
    public HomeD(HomePageVM homePageVM)
    {
        InitializeComponent();
        HomePageVM = homePageVM;
        this.BindingContext = homePageVM;

        VolumeSlider.Value = 1;

    }

    public HomePageVM HomePageVM { get; }


    private void SongsColView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SongsColView.IsLoaded)
        {
            SongsColView.ScrollTo(HomePageVM.TemporarilyPickedSong, ScrollToPosition.Center, animate: false);
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
            //SongsColView.ScrollTo(HomePageVM.PickedSong, ScrollToPosition.Center, animate: false);
            SongsColView.ScrollTo(HomePageVM.TemporarilyPickedSong, position: ScrollToPosition.Center, animate: false);
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
    }
    private void SongsColView_Loaded(object sender, EventArgs e)
    {
        if (SongsColView.IsLoaded)
        {
            SongsColView.ScrollTo(HomePageVM.TemporarilyPickedSong, ScrollToPosition.Center, animate: false);
            SongsColView.SelectedItem = HomePageVM.TemporarilyPickedSong;
        }
    }

    
}