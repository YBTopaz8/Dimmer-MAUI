using Plainer.Maui.Controls;


namespace Dimmer_MAUI.Views.Mobile;

public partial class HomePageM : UraniumContentPage
{
    NowPlayingBtmSheet btmSheet {  get; set; }
    public HomePageM(HomePageVM homePageVM)
    {
        InitializeComponent();
        this.HomePageVM = homePageVM;
        BindingContext = homePageVM;
        SongsColView.Loaded += SongsColView_Loaded;
        btmSheet = IPlatformApplication.Current.Services.GetService<NowPlayingBtmSheet>();
        this.Attachments.Add(btmSheet);

        Shell.SetNavBarIsVisible(this, true                                                                                                                                                                                                                    );
        Shell.SetTabBarIsVisible(this, true);

        SettingsBtmSheet.Header.HeightRequest = 0;
    }

    private void SongsColView_Loaded(object? sender, EventArgs e)
    {
        SongsColView.ScrollTo(HomePageVM.PickedSong, ScrollToPosition.Center, animate: false);
    }

    public HomePageVM HomePageVM { get; }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        HomePageVM.CurrentPage = PageEnum.MainPage;
        if (SongsColView.IsLoaded)
        {
            SongsColView.ScrollTo(HomePageVM.PickedSong, ScrollToPosition.Center, animate: false);
        }
#if ANDROID
        PermissionStatus status = await Permissions.RequestAsync<CheckPermissions>();
#endif
    }
    private void SaveViewButton_Clicked(object sender, EventArgs e)
    { //to capture views into a png , will be useful later for saving
        
        //var image = await btmcontrols.CaptureAsync();
        //var savePath = Path.Combine("/storage/emulated/0/Documents", "test.png");
        //using Stream fileStream = File.OpenWrite(savePath);
        //await image.CopyToAsync(fileStream, ScreenshotFormat.Png);

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
                HomePageVM.TemporarilyPickedSong = HomePageVM.PickedSong;
            }
            else
            {
                HomePageVM.SearchSongCommand.Execute(string.Empty);
                SongsColView.SelectedItem = HomePageVM.TemporarilyPickedSong;
            }
        }        
    }

    private void SearchFAB_Clicked(object sender, EventArgs e)
    {
        if (TitleSearchView.IsVisible)
        {
            HideSearchView_Clicked(sender, e);
        }
        SongsColView.ScrollTo(HomePageVM.TemporarilyPickedSong, position:ScrollToPosition.Center, animate: false);
    }

    //HomePageVM.LoadSongCoverImage();

    private void SpecificSong_Tapped(object sender, TappedEventArgs e)
    {
        HomePageVM.CurrentQueue = 0;
        var view = (FlexLayout)sender;
        var song = view.BindingContext as SongsModelView;
        HomePageVM.PlaySongCommand.Execute(song);
    }

    private void SongsColView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SongsColView.IsLoaded)
        {
            SongsColView.ScrollTo(HomePageVM.PickedSong, ScrollToPosition.Center, animate: false);
        }
    }

    EntryView searchSongTextField;
    private async void ShowSearchView_Clicked(object sender, EventArgs e)
    {
        NormalTitleView.IsVisible = false;
        TitleSearchView.IsVisible = true;
        SearchSongSB.Focus();
        var searchSongTextField = SearchSongSB.Content as EntryView;
        _ = await searchSongTextField!.ShowKeyboardAsync();
    }

    private async void HideSearchView_Clicked(object sender, EventArgs e)
    {
        NormalTitleView.IsVisible = true;
        TitleSearchView.IsVisible = false;
        SearchSongSB.Unfocus();
         searchSongTextField = SearchSongSB.Content as EntryView;
        _ = await searchSongTextField!.HideKeyboardAsync();
        SongsColView.ScrollTo(HomePageVM.TemporarilyPickedSong, position: ScrollToPosition.Center, animate: false);
    }

    private void SwipeGestureRecognizer_SwipedUp(object sender, SwipedEventArgs e)
    {
        if (SongsColView.IsLoaded)
        {
            var col = SongsColView.ItemsSource as ObservableCollection<SongsModelView>;
            var lItem = col.First();
            SongsColView.ScrollTo(lItem, ScrollToPosition.Center, animate: false);
        }
    }
    private void SwipeGestureRecognizer_SwipedDown(object sender, SwipedEventArgs e)
    {
        if (SongsColView.IsLoaded)
        {
            var col = SongsColView.ItemsSource as ObservableCollection<SongsModelView>;
            var fItem = col.Last();
            SongsColView.ScrollTo(fItem, ScrollToPosition.Center, animate: false);
        }
    }

    private void ImageButton_Clicked(object sender, EventArgs e)
    {
        SettingsBtmSheet.IsPresented = !SettingsBtmSheet.IsPresented;
    }
    protected override bool OnBackButtonPressed()
    {
        if (btmSheet.IsPresented)
        {
            btmSheet.IsPresented = false;
            return true;
        }
        return base.OnBackButtonPressed();
    }
}
