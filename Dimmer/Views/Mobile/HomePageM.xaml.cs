using Plainer.Maui.Controls;
using UraniumUI.Views;


namespace Dimmer_MAUI.Views.Mobile;

public partial class HomePageM : UraniumContentPage
{

    public HomePageVM HomePageVM { get; }
    public HomePageM(HomePageVM homePageVM)
    {
        InitializeComponent();
        this.HomePageVM = homePageVM;
        BindingContext = homePageVM;
        Shell.SetNavBarIsVisible(this, true);
    }


private void SongsColView_Loaded(object? sender, EventArgs e)
{
        SongsColView.ScrollTo(SongsColView.FindItemHandle(HomePageVM.PickedSong), DevExpress.Maui.Core.DXScrollToPosition.MakeVisible);
    }


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
    
}


private void SearchFAB_Clicked(object sender, EventArgs e)
{

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
    //if (SongsColView.SelectionMode != SelectionMode.Multiple)
    //{

    //    HomePageVM.CurrentQueue = 0;
    //    var t = (View)sender;
    //    var song = t.BindingContext as SongsModelView;
    //    HomePageVM.PlaySongCommand.Execute(song);
    //}
    //else
    //{
    //    var send = (View)sender;
    //    var song = (SongsModelView)send.BindingContext;

    //}
}


private void SingleSongCxtMenuArea_Clicked(object sender, EventArgs e)
{
        if (songsMenuBtm.State != DevExpress.Maui.Controls.BottomSheetState.Hidden)
        {
            songsMenuBtm.Show();
        }
        //await Shell.Current.GoToAsync(nameof(NowPlayingPage), true);
        //songsMenuBtm.State = DevExpress.Maui.Controls.BottomSheetState.HalfExpanded;
        //songsMenuPopup.PlacementTarget = (View)this.Content;
        //songsMenuPopup.Placement = DevExpress.Maui.Core.Placement.Bottom;
        //songsMenuPopup.IsOpen = !songsMenuPopup.IsOpen;

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

    private void DXButton_Clicked(object sender, EventArgs e)
    {
        songsMenuBtm.State = DevExpress.Maui.Controls.BottomSheetState.Hidden;
    }

    private void NPTabview_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.TabView.TabSelectionChangedEventArgs e)
    {
        if (e.NewIndex == 1)
        {
            TopPart.IsVisible = false;
        }
        else
        {
            TopPart.IsVisible = true;
        }
    }
}

