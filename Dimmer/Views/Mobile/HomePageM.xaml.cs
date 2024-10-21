using Plainer.Maui.Controls;
using UraniumUI.Views;


namespace Dimmer_MAUI.Views.Mobile;

public partial class HomePageM : UraniumContentPage
{

    NowPlayingBtmSheet btmSheet { get; set; }
    public HomePageM(HomePageVM homePageVM)
    {
        InitializeComponent();
        this.HomePageVM = homePageVM;
        BindingContext = homePageVM;
        SongsColView.Loaded += SongsColView_Loaded;
        btmSheet = IPlatformApplication.Current.Services.GetService<NowPlayingBtmSheet>();
        this.Attachments.Add(btmSheet);

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
        Shell.SetNavBarIsVisible(this, false);
        Shell.SetTabBarIsVisible(this, true);
#endif
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (btmSheet.IsPresented)
        {
            btmSheet.IsPresented = false;
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
                HomePageVM.TemporarilyPickedSong = HomePageVM.PickedSong;
            }
            else
            {
                HomePageVM.SearchSongCommand.Execute(string.Empty);
                
                await Task.Delay(500);
                if (SongsColView.IsLoaded)
                {
                    SongsColView.ScrollTo(HomePageVM.TemporarilyPickedSong, ScrollToPosition.Start, animate: false);
                }
            }
        }
    }

    private void SearchFAB_Clicked(object sender, EventArgs e)
    {
        if (TitleSearchView.IsVisible)
        {
            HideSearchView_Clicked(sender, e);
        }
        SongsColView.ScrollTo(HomePageVM.TemporarilyPickedSong, position: ScrollToPosition.Center, animate: false);
    }

    private void SpecificSong_Tapped(object sender, TappedEventArgs e)
    {
        HomePageVM.CurrentQueue = 0;
        var view = (FlexLayout)sender;
        var song = view.BindingContext as SongsModelView;
        HomePageVM.PlaySongCommand.Execute(song);
    }

    private void SongsColView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if(currentSelectionMode == SelectionMode.Multiple)
        {
            HomePageVM.HandleMultiSelect(SongsColView, e);
            return;
        }
        if (SongsColView.IsLoaded)
        {
            SongsColView.ScrollTo(HomePageVM.PickedSong, ScrollToPosition.Center, animate: false);
        }
    }

    EntryView searchSongTextField;
    private async void ShowSearchView_Clicked(object sender, EventArgs e)
    {
        TitleSearchView.IsVisible = true;
        SearchSongSB.Focus();
        var searchSongTextField = SearchSongSB.Content as EntryView;
        _ = await searchSongTextField!.ShowKeyboardAsync();
    }

    private async void HideSearchView_Clicked(object sender, EventArgs e)
    {
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

    private async void ShowFolderSelectorImgBtn_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.ShowPopupAsync(new ScanFoldersPopup(HomePageVM));
    }
    protected override bool OnBackButtonPressed()
    {
        if (btmSheet.IsPresented)
        {
            btmSheet.IsPresented = false;
            return true;
        }
        return true;
    }

    private void CancelMultiSelect_Clicked(object sender, EventArgs e)
    {
        ToggleMultiSelect_Clicked(sender, e);
    }

    SelectionMode currentSelectionMode;
    public void ToggleMultiSelect_Clicked(object sender, EventArgs e)
    {
        HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);

        if (sender is StatefulContentView send)
        {
            SongsColView.SelectedItems.Add(send.CommandParameter);
        }
        switch (SongsColView.SelectionMode)
        {
            case SelectionMode.None:
            case SelectionMode.Single:
                SongsColView.SelectionMode = SelectionMode.Multiple;
                NormalMiniUtilFABs.IsVisible = false;
                MultiSelectMiniUtilFABs.IsVisible = true;
                HomePageVM.EnableContextMenuItems = false;

                Debug.WriteLine("Now Multi Select");
                break;
            case SelectionMode.Multiple:
                SongsColView.SelectionMode = SelectionMode.Single;
                SongsColView.SelectedItems.Clear();
                HomePageVM.HandleMultiSelect(SongsColView);
                NormalMiniUtilFABs.IsVisible = true;
                MultiSelectMiniUtilFABs.IsVisible = false;
                HomePageVM.EnableContextMenuItems = true;
                Debug.WriteLine("Back To None");
                break;
            default:
                break;
            
        }
        currentSelectionMode = SongsColView.SelectionMode;
    }

    private DateTime _lastTapTime = DateTime.MinValue;
    private const int DoubleTapTime = 300; // in milliseconds
    private const int LongPressTime = 500; // in milliseconds
    private bool _isDoubleTap = false;
    private bool _isLongPress = false;
    private void StatefulContentView_Tapped(object sender, EventArgs e)
    {
        var send = (StatefulContentView)sender!;
        switch (currentSelectionMode)
        {
            case SelectionMode.None:
            case SelectionMode.Single:
                HomePageVM.OpenSingleSongOptionsBtmSheet((SongsModelView)send.CommandParameter);
                break;
            case SelectionMode.Multiple:
                

                break;
            default:
                break;
        }

        //var currentTime = DateTime.Now;
        //var timeSinceLastTap = (currentTime - _lastTapTime).TotalMilliseconds;

        //// Double tap detection
        //if (timeSinceLastTap < DoubleTapTime)
        //{
        //    _isDoubleTap = true;
        //    Debug.WriteLine("Double tap detected at: " + DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString());
        //}
        

        //_lastTapTime = currentTime;
    }

    private void StatefulContentView_LongPressed(object sender, EventArgs e)
    {
        ToggleMultiSelect_Clicked(sender, e);
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        if (SongsColView.SelectionMode != SelectionMode.Multiple)
        {

            HomePageVM.CurrentQueue = 0;
            var t = (HorizontalStackLayout)sender;
            var song = t.BindingContext as SongsModelView;
            HomePageVM.PlaySongCommand.Execute(song);
        }
        else
        {
            var send = (HorizontalStackLayout)sender;
            var song = (SongsModelView)send.BindingContext;

            if (SongsColView.SelectedItems.Contains(song))
            {
                SongsColView.SelectedItems.Remove(song);
                Debug.WriteLine($"Removed: {song.Title}");
            }
            else
            {
                SongsColView.SelectedItems.Add(song);
                Debug.WriteLine($"Added: {song.Title}");
            }

        }
    }
}
