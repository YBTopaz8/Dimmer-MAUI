//using Plainer.Maui.Controls;

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


        if (HomePageVM.TemporarilyPickedSong is null)
        {
            return;
        }


        HomePageVM.CurrentPage = PageEnum.MainPage;

#if ANDROID

        PermissionStatus status = await Permissions.RequestAsync<CheckPermissions>();
        Shell.SetNavBarIsVisible(this, false);
        Shell.SetTabBarIsVisible(this, true);
#endif
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        HomePageVM.NowPlayBtmSheetState = DevExpress.Maui.Controls.BottomSheetState.Hidden;
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
            var col = SongsColView.ItemsSource as ObservableCollection<SongModelView>;
            var fItem = col.Last();
            SongsColView.ScrollTo(SongsColView.FindItemHandle(fItem), DevExpress.Maui.Core.DXScrollToPosition.MakeVisible);
        }
    }

    private async void ShowFolderSelectorImgBtn_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPageM));
    }
    protected override bool OnBackButtonPressed()
    {
        
        return true;
    }



    SelectionMode currentSelectionMode;
   

    private DateTime _lastTapTime = DateTime.MinValue;
    private const int DoubleTapTime = 300; // in milliseconds
    private const int LongPressTime = 500; // in milliseconds
    

    private void SingleSongCxtMenuArea_Clicked(object sender, EventArgs e)
    {        
        var s = (View)sender;
        var song = (SongModelView)s.BindingContext;
        HomePageVM.SetContextMenuSong(song);
        if (SongsMenuBtm.State == DevExpress.Maui.Controls.BottomSheetState.Hidden)
        {
            SongsMenuBtm.Show();
        }
    }

    private async void SongsColView_Tap(object sender, DevExpress.Maui.CollectionView.CollectionViewGestureEventArgs e)
    {
        HomePageVM.CurrentQueue = 0;
        if (HomePageVM.IsOnSearchMode)
        {
            HomePageVM.CurrentQueue = 1;
            HomePageVM.filteredSongs = filteredSongs;
        }
        await HomePageVM.PlaySong(e.Item as SongModelView);
    }

    private void SongsColView_LongPress(object sender, DevExpress.Maui.CollectionView.CollectionViewGestureEventArgs e)
    { 
        
        var s = (View)sender;
        var song = (SongModelView)e.Item;
        HomePageVM.SetContextMenuSong(song);
        if (SongsMenuBtm.State == DevExpress.Maui.Controls.BottomSheetState.Hidden)
        {
            SongsMenuBtm.Show();
        }
    }

    private void ShareSong_Clicked(object sender, EventArgs e)
    {
        CloseBtmSheet();
    }

    private void CloseBtmSheet()
    {
        SongsMenuBtm.State = DevExpress.Maui.Controls.BottomSheetState.Hidden;
    }


    private void ShowFilterUIImgBtm_Clicked(object sender, EventArgs e)
    {
        SearchSongPopUp.Show();
        
        //SearchSongPopUp.IsOpen = true;
    }

    private async void GotoArtistBtn_Clicked(object sender, EventArgs e)
    {
        await HomePageVM.NavigateToArtistsPage(1);
        CloseBtmSheet();
    }


    private void OnLongPressElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        _isLongPressed = true;
        SongsColView.ScrollTo(SongsColView.FindItemHandle(HomePageVM.TemporarilyPickedSong), DevExpress.Maui.Core.DXScrollToPosition.Start);        
    }
    private async void ShowSongDetails_Tap(object sender, DevExpress.Maui.CollectionView.SwipeItemTapEventArgs e)
    {
        var song = (SongModelView)e.Item;

        HomePageVM.SelectedSongToOpenBtmSheet = song;

        await HomePageVM.NavToSingleSongShell();
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

    List<SongModelView>? filteredSongs=new();
    private void SongTitleTextEdit_TextChanged(object sender, EventArgs e)
    {
        var searchBar = (TextEdit)sender;
        var txt = searchBar.Text;

        if (!string.IsNullOrEmpty(txt))
        {
            if (txt.Length >= 1)
            {
                HomePageVM.IsOnSearchMode = true;
                // Setting the FilterString for SongsColView
                SongsColView.FilterString = $"Contains([Title], '{SongTitleTextEdit.Text}')";
                filteredSongs?.Clear();

                // Apply the filter to the DisplayedSongs collection
                filteredSongs = HomePageVM.DisplayedSongs!
                    .Where(item => item.Title.Contains(SongTitleTextEdit.Text, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            }
            else
            {
                HomePageVM.IsOnSearchMode = false;
                SongsColView.FilterString = string.Empty;

            }
        }
    }
    private void ArtistNameTextEdit_TextChanged(object sender, EventArgs e)
    {
        var searchBar = (TextEdit)sender;
        var txt = searchBar.Text;

        if (!string.IsNullOrEmpty(txt))
        {
            if (txt.Length >= 1)
            {
                HomePageVM.IsOnSearchMode = true;
                // Setting the FilterString for SongsColView
                SongsColView.FilterString = $"Contains([ArtistName], '{ArtistNameTextEdit.Text}')";
                filteredSongs?.Clear();

                // Apply the filter to the DisplayedSongs collection
                filteredSongs = HomePageVM.DisplayedSongs!
                    .Where(item => item.ArtistName!.Contains(ArtistNameTextEdit.Text, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            }
            else
            {
                HomePageVM.IsOnSearchMode = false;
                SongsColView.FilterString = string.Empty;

            }
        }
    }
    private void AlbumNameTextEdit_TextChanged(object sender, EventArgs e)
    {
        var searchBar = (TextEdit)sender;
        var txt = searchBar.Text;

        if (!string.IsNullOrEmpty(txt))
        {
            if (txt.Length >= 1)
            {
                HomePageVM.IsOnSearchMode = true;
                // Setting the FilterString for SongsColView
                SongsColView.FilterString = $"Contains([AlbumName], '{AlbumNameTextEdit.Text}')";
                filteredSongs?.Clear();

                // Apply the filter to the DisplayedSongs collection
                filteredSongs = HomePageVM.DisplayedSongs
                    .Where(item => item.AlbumName.Contains(AlbumNameTextEdit.Text, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            }
            else
            {
                HomePageVM.IsOnSearchMode = false;
                SongsColView.FilterString = string.Empty;

            }
        }
    }
    private void ClearSearch_Clicked(object sender, EventArgs e)
    {
        SongsColView.FilterString = string.Empty;
        SongTitleTextEdit.Text = string.Empty;
    }
    

    private System.Timers.Timer _longPressTimer;
    private bool _isLongPressed;

    private void NowPlaySearchBtmSheet_TapReleased(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
        _longPressTimer.Stop(); // Stop the timer if released early

        if (!_isLongPressed)
        {
            // Short tap action
            SearchSongPopUp.Show();
        }
        else
        {
            SongsColView.ScrollTo(SongsColView.FindItemHandle(HomePageVM.PickedSong), DevExpress.Maui.Core.DXScrollToPosition.Start);
            
        }
    }

    private void NowPlaySearchBtmSheet_TapPressed(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
        // Initialize the timer
        _longPressTimer = new System.Timers.Timer(500); // 1.5 seconds
        _longPressTimer.Elapsed += OnLongPressElapsed;
        _longPressTimer.AutoReset = false; // Only fire once per press
        _isLongPressed = false;
        _longPressTimer.Start(); // Start the timer on button press

    }

    private void UILayoutToggled_SelectionChanged(object sender, EventArgs e)
    {
        var s = sender as ChoiceChipGroup;
        switch (s.SelectedIndex)
        {
            case 0:
                SongsColView.ItemTemplate = (DataTemplate)Resources["HomePageColViewGridOfOne"];
                break;

            case 1:
                SongsColView.ItemSpanSpacing = 2;
                SongsColView.ItemTemplate = (DataTemplate)Resources["HomePageColViewGridOfTwo"];
                break;
            case 2:
                SongsColView.ItemTemplate = (DataTemplate)Resources["HomePageColViewGridOfThree"];
                break;
            case 3:
                SongsColView.ItemTemplate = (DataTemplate)Resources["HomePageColViewGridOfFour"];
                break;
            default:
                break;
        }
        Debug.WriteLine(s.GetType());
    }
}


