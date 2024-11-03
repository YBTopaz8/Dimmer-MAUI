//using Plainer.Maui.Controls;
using DevExpress.Maui.CollectionView;
using DevExpress.Maui.Editors;
using System.Diagnostics;
using UraniumUI.Views;
using Timer = System.Timers.Timer;


namespace Dimmer_MAUI.Views.Mobile;

public partial class HomePageM : ContentPage
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


    private void SpecificSong_Tapped(object sender, TappedEventArgs e)
    {
        HomePageVM.CurrentQueue = 0;
        var view = (FlexLayout)sender;
        var song = view.BindingContext as SongsModelView;
        HomePageVM.PlaySongCommand.Execute(song);
    }

    //private void SongsColView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    //{
    //    if (currentSelectionMode == SelectionMode.Multiple)
    //    {
    //        //HomePageVM.HandleMultiSelect(SongsColView, e);
    //        return;
    //    }
    //    if (SongsColView.IsLoaded)
    //    {
    //        SongsColView.ScrollTo(SongsColView.FindItemHandle(HomePageVM.PickedSong), DevExpress.Maui.Core.DXScrollToPosition.MakeVisible);


    //    }
    //}


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
    

    private void StatefulContentView_LongPressed(object sender, EventArgs e)
    {
        ToggleMultiSelect_Clicked(sender, e);
    }


    private void SingleSongCxtMenuArea_Clicked(object sender, EventArgs e)
    {        
        var s = (View)sender;
        var song = (SongsModelView)s.BindingContext;
        HomePageVM.SetContextMenuSong(song);
        if (songsMenuBtm.State == DevExpress.Maui.Controls.BottomSheetState.Hidden)
        {
            songsMenuBtm.Show();
        }
    }

    private void SongsColView_Tap(object sender, DevExpress.Maui.CollectionView.CollectionViewGestureEventArgs e)
    {
        HomePageVM.CurrentQueue = 0;
        HomePageVM.PlaySongCommand.Execute(e.Item as SongsModelView);
    }

    private void SongsColView_LongPress(object sender, DevExpress.Maui.CollectionView.CollectionViewGestureEventArgs e)
    { 
        
        var s = (View)sender;
        var song = (SongsModelView)e.Item;
        HomePageVM.SetContextMenuSong(song);
        if (songsMenuBtm.State == DevExpress.Maui.Controls.BottomSheetState.Hidden)
        {
            songsMenuBtm.Show();
        }
    }

    private void ShareSong_Clicked(object sender, EventArgs e)
    {
        CloseBtmSheet();
    }

    private void CloseBtmSheet()
    {
        songsMenuBtm.State = DevExpress.Maui.Controls.BottomSheetState.Hidden;
    }


    private void ShowFilterUIImgBtm_Clicked(object sender, EventArgs e)
    {
        SearchSongPopUp.Show();
        
        //SearchSongPopUp.IsOpen = true;
    }

    private async void GotoArtistBtn_Clicked(object sender, EventArgs e)
    {
        await HomePageVM.NavigateToArtistsPage(0);
        CloseBtmSheet();
    }



    private void NowPlayingBtn_TapPressed(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
        
    }

    private void NowPlayingBtn_TapReleased(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
        NowPlayingBtmSheet.Show();
        
        return;       
    }

    private async void ShowSongDetails_Tap(object sender, DevExpress.Maui.CollectionView.SwipeItemTapEventArgs e)
    {
        var song = (SongsModelView)e.Item;

        HomePageVM.SelectedSongToOpenBtmSheet = song;

        await HomePageVM.NavToNowPlayingPage();
    }
    ObservableCollection<DevExpress.Maui.CollectionView.SortDescription> Sorts;
    private void SortSongsChip_Tap(object sender, HandledEventArgs e)
    {
        var chip = (DevExpress.Maui.Editors.Chip)sender;
        SongsColView.SortDescriptions.Clear();
        switch (chip.TapCommandParameter)
        {
            case "0":
                SongsColView.SortDescriptions.Add(new DevExpress.Maui.CollectionView.SortDescription() { FieldName = "Title", SortOrder = DevExpress.Maui.Core.DataSortOrder.Ascending});
                break;
            case "1":
                SongsColView.SortDescriptions.Add(new DevExpress.Maui.CollectionView.SortDescription() { FieldName = "Title", SortOrder = DevExpress.Maui.Core.DataSortOrder.Descending });
                break;
            case "2":
                SongsColView.SortDescriptions.Add(new DevExpress.Maui.CollectionView.SortDescription() { FieldName = "DateAdded", SortOrder = DevExpress.Maui.Core.DataSortOrder.Ascending });
                break;
            case "3":
                SongsColView.SortDescriptions.Add(new DevExpress.Maui.CollectionView.SortDescription() { FieldName = "DateAdded", SortOrder = DevExpress.Maui.Core.DataSortOrder.Descending });
                break;
            default:
                break;
        }
        //var commandParam = 
    }
}


