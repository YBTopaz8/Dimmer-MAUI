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
        SongsColView.ScrollTo(SongsColView.FindItemHandle(HomePageVM.PickedSong), DevExpress.Maui.Core.DXScrollToPosition.MakeVisible);
    }

    public HomePageVM HomePageVM { get; }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        HomePageVM.CurrentPage = PageEnum.MainPage;
        //HomePageVM.AssignCV(SongsColView);
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
    

    private void SearchFAB_Clicked(object sender, EventArgs e)
    {
        if (TitleSearchView.IsVisible)
        {
            HideSearchView_Clicked(sender, e);
        }
        SongsColView.ScrollTo(SongsColView.FindItemHandle(HomePageVM.TemporarilyPickedSong), DevExpress.Maui.Core.DXScrollToPosition.MakeVisible);
        
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
            //HomePageVM.HandleMultiSelect(SongsColView, e);
            return;
        }
        if (SongsColView.IsLoaded)
        {
            SongsColView.ScrollTo(SongsColView.FindItemHandle(HomePageVM.PickedSong), DevExpress.Maui.Core.DXScrollToPosition.MakeVisible);

            
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
        SongsColView.ScrollTo(SongsColView.FindItemHandle(HomePageVM.TemporarilyPickedSong), DevExpress.Maui.Core.DXScrollToPosition.MakeVisible);

        
    }

    private void SwipeGestureRecognizer_SwipedUp(object sender, SwipedEventArgs e)
    {
        if (SongsColView.IsLoaded)
        {
            SongsColView.ScrollTo(0);
        }
    }
    private void SwipeGestureRecognizer_SwipedDown(object sender, SwipedEventArgs e)
    {
        if (SongsColView.IsLoaded)
        {            
            var col = SongsColView.ItemsSource as ObservableCollection<SongsModelView>;
            var fItem = col.Last();
            SongsColView.ScrollTo(SongsColView.FindItemHandle(fItem), DevExpress.Maui.Core.DXScrollToPosition.MakeVisible);
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
        if (SongsColView.SelectionMode == SelectionMode.Multiple)
        {
            SongsColView.SelectionMode = SelectionMode.Single;            
            //HomePageVM.HandleMultiSelect(SongsColView);
            NormalMiniUtilFABs.IsVisible = true;
            MultiSelectMiniUtilFABs.IsVisible = false;
            HomePageVM.EnableContextMenuItems = true;
            Debug.WriteLine("Back To None");
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
            //SongsColView.SelectedItems.Add(send.CommandParameter);
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
   

        //var currentTime = DateTime.Now;
        //var timeSinceLastTap = (currentTime - _lastTapTime).TotalMilliseconds;

        //// Double tap detection
        //if (timeSinceLastTap < DoubleTapTime)
        //{
        //    _isDoubleTap = true;
        //    Debug.WriteLine("Double tap detected at: " + DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString());
        //}
        

        //_lastTapTime = currentTime;
    

    private void StatefulContentView_LongPressed(object sender, EventArgs e)
    {
        ToggleMultiSelect_Clicked(sender, e);
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {

    }
    

    private void StatefulContentView_Pressed(object sender, EventArgs e)
    {
        if (SongsColView.SelectionMode != SelectionMode.Multiple)
        {

            HomePageVM.CurrentQueue = 0;
            var t = (View)sender;
            var song = t.BindingContext as SongsModelView;
            HomePageVM.PlaySongCommand.Execute(song);
        }
        else
        {
            var send = (View)sender;
            var song = (SongsModelView)send.BindingContext;

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
                //HomePageVM.SearchSongCommand.Execute(searchText); //TODO: FIX THIS
                HomePageVM.TemporarilyPickedSong = HomePageVM.PickedSong;
            }
            else
            {
                //HomePageVM.SearchSongCommand.Execute(string.Empty);

                await Task.Delay(500);
                if (SongsColView.IsLoaded)
                {
                    SongsColView.ScrollTo(SongsColView.FindItemHandle(HomePageVM.TemporarilyPickedSong), DevExpress.Maui.Core.DXScrollToPosition.MakeVisible);

                    
                }
            }
        }
    }

    private void SingleSongStateFullContent_Clicked(object sender, EventArgs e)
    {
        songsMenuPopup.PlacementTarget = (View)this.Content;
        songsMenuPopup.Placement = DevExpress.Maui.Core.Placement.Bottom;
        songsMenuPopup.IsOpen = !songsMenuPopup.IsOpen;

    }

    private void SongsColView_Tap(object sender, DevExpress.Maui.CollectionView.CollectionViewGestureEventArgs e)
    {
        HomePageVM.CurrentQueue = 0;
        HomePageVM.PlaySongCommand.Execute(e.Item as SongsModelView);
    }

    private void SongsColView_LongPress(object sender, DevExpress.Maui.CollectionView.CollectionViewGestureEventArgs e)
    {
        SongsColView.SelectionMode = SelectionMode.Multiple;
    }

    private void SongsColView_FilteringUIFormShowing(object sender, DevExpress.Maui.Core.FilteringUIFormShowingEventArgs e)
    {

    }
}
