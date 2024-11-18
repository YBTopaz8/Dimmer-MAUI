namespace Dimmer_MAUI.Views.Desktop;

public partial class MainPageD : ContentPage
{
	public MainPageD(HomePageVM homePageVM)
    {
        InitializeComponent();
        HomePageVM = homePageVM;
        this.BindingContext = homePageVM;

        //MediaPlayBackCW.BindingContext = homePageVM;

    }
    public HomePageVM HomePageVM { get; }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        HomePageVM.CurrentPage = PageEnum.MainPage;
        HomePageVM.AssignCV(SongsColView);
        
        //scrollView.ScrollToIndex(HomePageVM.DisplayedSongs.IndexOf(HomePageVM.TemporarilyPickedSong),true);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }

    private void ScrollToSong_Clicked(object sender, EventArgs e)
    {
        //var s = SongsColView.SelectedItem;
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
            Debug.WriteLine("Error when scrolling " + ex.Message);
        }
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

    bool inRatingMode;
    private void ratingViewD_PointerEntered(object sender, PointerEventArgs e)
    {
        inRatingMode = true;
    }

    private void ratingViewD_PointerExited(object sender, PointerEventArgs e)
    {
        inRatingMode = false;
    }


    private void CancelMultiSelect_Clicked(object sender, EventArgs e)
    {
        ToggleMultiSelect_Clicked(sender, e);
    }

    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        base.OnNavigatingFrom(args);
        //if (!isPointerEntered)
        //{
        //    HomePageVM.SelectedSongToOpenBtmSheet = HomePageVM.TemporarilyPickedSong;
        //}
    }

    private async void NavToArtistClicked(object sender, EventArgs e)
    {
        await HomePageVM.NavigateToArtistsPage(1);
    }

    bool isPointerEntered;
    private void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {
        var send = (View)sender;
        var song = send.BindingContext! as SongModelView;
        send.BackgroundColor = Microsoft.Maui.Graphics.Colors.DarkSlateBlue;
        isPointerEntered = true;

        //HomePageVM.SetContextMenuSong(song!);
    }

    private void PointerGestureRecognizer_PointerExited(object sender, PointerEventArgs e)
    {
        var send = (View)sender;
        send.BackgroundColor = Microsoft.Maui.Graphics.Colors.Transparent;
        isPointerEntered = false;

    }
    private bool isPressed = false;  // Track whether the button is pressed
    private bool isAnimating = false;  // Track if an animation is running


    List<string> supportedFilePaths;
    bool isAboutToDropFiles = false;
    private async void DropGestureRecognizer_DragOver(object sender, DragEventArgs e)
    {
        if (!isAboutToDropFiles)
        {
            isAboutToDropFiles = true;
            SongsColView.Opacity = 0.7;

#if WINDOWS
            var WindowsEventArgs = e.PlatformArgs.DragEventArgs;
            var dragUI = WindowsEventArgs.DragUIOverride;
            

            var items = await WindowsEventArgs.DataView.GetStorageItemsAsync();
            e.AcceptedOperation = DataPackageOperation.None;
            supportedFilePaths = new List<string>();

            if (items.Count > 0)
            {
                foreach (var item in items)
                {
                    if (item is Windows.Storage.StorageFile file)
                    {
                        /// Check file extension
                        string fileExtension = file.FileType.ToLower();
                        if (fileExtension != ".mp3" && fileExtension != ".flac" &&
                            fileExtension != ".wav" && fileExtension != ".m4a")
                        {
                            e.AcceptedOperation = DataPackageOperation.None;
                            dragUI.IsGlyphVisible = true;
                            dragUI.Caption = $"{fileExtension.ToUpper()} Files Not Supported";
                            return;
                            //break;  // If any invalid file is found, break the loop
                        }
                        else
                        {
                            dragUI.IsGlyphVisible = false;
                            dragUI.Caption = "Drop to Play!";
                            Debug.WriteLine($"File is {item.Path}");
                            supportedFilePaths.Add(item.Path.ToLower());
                        }
                    }
                }

            }
#endif
        }
    }

    private void DropGestureRecognizer_DragLeave(object sender, DragEventArgs e)
    {
        isAboutToDropFiles = false;

        SongsColView.Opacity = 1;
    }

    private async void DropGestureRecognizer_Drop(object sender, DropEventArgs e)
    {
        isAboutToDropFiles = false;
        SongsColView.Opacity = 1;
        var colView = (View)SongsColView;
        if (supportedFilePaths.Count > 0)
        {
            await colView.AnimateRippleBounce();
            HomePageVM.LoadLocalSongFromOutSideApp(supportedFilePaths);
        }
    }


    private void FavImagStatView_HoveredAndExited(object sender, EventArgs e)
    {
        var send = (View)sender;
        var song = send.BindingContext! as SongModelView;
        if (song is null)
            return;
        if (song.IsFavorite)
        {
            song.IsFavorite = false;
        }
        else
        {
            song.IsFavorite = true;
        }
    }


    private async void GoToSongOverviewClicked(object sender, EventArgs e)
    {
        await HomePageVM.NavToSingleSongShell();
    }


    SelectionMode currentSelectionMode;
    public void ToggleMultiSelect_Clicked(object sender, EventArgs e)
    {
        switch (SongsColView.SelectionMode)
        {
            case SelectionMode.None:
                SongsColView.SelectionMode = SelectionMode.Multiple;
                //NormalMiniUtilBar.IsVisible = false;
                //MultiSelectUtilBar.IsVisible = true;
                HomePageVM.EnableContextMenuItems = false;
                HomePageVM.IsMultiSelectOn = true;
                selectedSongs = new();
                selectedSongsViews = new();
                SongsColView.BackgroundColor = Color.Parse("#1D1932");                
                break;
            case SelectionMode.Single:
                break;
            case SelectionMode.Multiple:
                SongsColView.BackgroundColor = Microsoft.Maui.Graphics.Colors.Transparent;
                foreach (var view in selectedSongsViews)
                {
                    view.BackgroundColor = Microsoft.Maui.Graphics.Colors.Transparent;
                }
                SongsColView.SelectionMode = SelectionMode.None;
                HomePageVM.IsMultiSelectOn = false;
                SongsColView.SelectedItems = null;
                //NormalMiniUtilBar.IsVisible = true;
                //MultiSelectUtilBar.IsVisible = false;
                HomePageVM.EnableContextMenuItems = true;
                break;
            default:
                break;
        }
        currentSelectionMode = SongsColView.SelectionMode;
    }

    List<SongModelView> selectedSongs;
    List<View> selectedSongsViews;
    private void SfEffectsView_TouchDown(object sender, EventArgs e)
    {
        View send = (View)sender;
        SongModelView song = (send.BindingContext as SongModelView)!;

        if (HomePageVM.IsMultiSelectOn)
        {
            
            // Add or remove song based on its presence in the selectedSongs list
            if (selectedSongs.Contains(song))
            {
                selectedSongs.Remove(song);
                selectedSongsViews.Remove(send);
                send.BackgroundColor = Microsoft.Maui.Graphics.Colors.Transparent;
            }
            else
            {
                selectedSongs.Add(song);
                selectedSongsViews.Add(send);
                send.BackgroundColor = Microsoft.Maui.Graphics.Colors.DarkSlateBlue;
            }
            HomePageVM.MultiSelectText = $"{selectedSongs.Count} Song{(selectedSongs.Count > 1 ? "s" : "")}/{HomePageVM.DisplayedSongs.Count} Selected";
            return;
        }
        else
        {
            HomePageVM.SetContextMenuSong((SongModelView)((View)sender).BindingContext);
        }
    }

    private async void PlaySong_Tapped(object sender, TappedEventArgs e)
    {
        var send = (View)sender;
        var song = (SongModelView)send.BindingContext;

        await HomePageVM.PlaySong(song);
    }

}