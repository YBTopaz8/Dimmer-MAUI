using Syncfusion.Maui.Toolkit.Chips;
using Syncfusion.Maui.Toolkit.EffectsView;
using System.Diagnostics;
using SelectionChangedEventArgs = Microsoft.Maui.Controls.SelectionChangedEventArgs;

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
        HomePageVM.AssignCV(SongsColView);

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
            Debug.WriteLine("Error when scrolling "+ex.Message);
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
        var song = send.BindingContext! as SongsModelView;
        isPointerEntered = true;

        HomePageVM.SetContextMenuSong(song);
    }

    private void PointerGestureRecognizer_PointerExited(object sender, PointerEventArgs e)
    {
        var send = (View)sender;
        isPointerEntered = false;

    }
    private bool isPressed = false;  // Track whether the button is pressed
    private bool isAnimating = false;  // Track if an animation is running


    List<string> supportedFilePaths;
    bool isAboutToDropFiles = false;
    private async void DropGestureRecognizer_DragOver(object sender, DragEventArgs e)
    {        
        if(!isAboutToDropFiles)
        {
            isAboutToDropFiles=true;
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
        var song = send.BindingContext! as SongsModelView;
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

    bool inRatingMode;


    private async void SearchSongSB_Focused(object sender, FocusEventArgs e)
    {
        
        var send = (View)sender;
        await SearchFiltersHSL.AnimateFadeInFront();      
    }


    private void SearchBarPointer_PointerExited(object sender, PointerEventArgs e)
    {

    }
    List<string> filterFilters;
    private async void SfChipGroup_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.Chips.SelectionChangedEventArgs e)
    {
        var send = (SfChipGroup)sender;


        var itemm = send.SelectedItem as System.Collections.IList;
        var stringList = (itemm?.OfType<string>()).ToList() ?? new List<string>();
        filterFilters?.Clear();
        filterFilters = stringList;
        if (stringList.Contains("Rating"))
        {
            await ratingFilter.AnimateFadeInFront();
        }
        else
        {
            await ratingFilter.AnimateFadeOutBack();
        }
    }

    private async void Search_Clicked(object sender, EventArgs e)
    {
        HomePageVM.SearchSong(filterFilters);
        await MainBody.AnimateFadeInFront();
    }

    private async void CloseFiltersImgBtn_Clicked(object sender, EventArgs e)
    {
        //SearchFiltersHSL.IsVisible = false;
        //MainBody.IsVisible = true;

        await SearchFiltersHSL.AnimateFadeOutBack();

        await MainBody.AnimateFadeInFront();
    }

    private async void GoToSongOverviewClicked(object sender, EventArgs e)
    {
        await HomePageVM.NavToNowPlayingPage();
    }

    private async void SearchSongSB_Completed(object sender, EventArgs e)
    {
        HomePageVM.SearchSong(filterFilters);
        await MainBody.AnimateFadeInFront();

    }

    SelectionMode currentSelectionMode;
    public void ToggleMultiSelect_Clicked(object sender, EventArgs e)
    {
        switch (SongsColView.SelectionMode)
        {
            case SelectionMode.None:
                SongsColView.SelectionMode = SelectionMode.Multiple;
                NormalMiniUtilBar.IsVisible = false;
                MultiSelectUtilBar.IsVisible = true;
                HomePageVM.EnableContextMenuItems = false;
                HomePageVM.IsMultiSelectOn = true;
                selectedSongs = new();
                selectedSongsViews = new();
                SongsColView.BackgroundColor = Microsoft.Maui.Graphics.Colors.Black;
                break;
            case SelectionMode.Single:
                break;
            case SelectionMode.Multiple:
                SongsColView.BackgroundColor = Microsoft.Maui.Graphics.Colors.Transparent;
                foreach(var view in selectedSongsViews)
                {
                    view.BackgroundColor = Microsoft.Maui.Graphics.Colors.Transparent;
                }
                SongsColView.SelectionMode = SelectionMode.None;
                HomePageVM.IsMultiSelectOn = false;

                NormalMiniUtilBar.IsVisible = true;
                MultiSelectUtilBar.IsVisible = false;
                HomePageVM.EnableContextMenuItems = true;
                break;
            default:
                break;
        }
        currentSelectionMode = SongsColView.SelectionMode;
    }

    List<SongsModelView> selectedSongs;
    List<View> selectedSongsViews;
    private void SfEffectsView_TouchDown(object sender, EventArgs e)
    {
        if (HomePageVM.IsMultiSelectOn)
        {
            var send = (View)sender;
            var song = send.BindingContext as SongsModelView;

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

    }

    private void SearchSongSB_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (e.NewTextValue.Length < 1)
        {
            HomePageVM.IsOnSearchMode = false;
        }
        else
        {
            HomePageVM.IsOnSearchMode = true;   
        }
    }

    private void TurnSearchModeOn_Clicked(object sender, EventArgs e)
    {
        SearchSongSB.Focus();
    }

    private void PlaySong_Tapped(object sender, TappedEventArgs e)
    {
        var send = (View)sender;
        var song = (SongsModelView)send.BindingContext;

        HomePageVM.PlaySongCommand.Execute(song);
    }
}
