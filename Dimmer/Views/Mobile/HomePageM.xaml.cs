using CommunityToolkit.Maui.Extensions;
using DevExpress.Maui.CollectionView;
using DevExpress.Maui.Core;
using View = Microsoft.Maui.Controls.View;

namespace Dimmer_MAUI.Views.Mobile;

public partial class HomePageM : ContentPage
{
    public HomePageVM MyViewModel { get; }
    public HomePageM(HomePageVM homePageVM)
    {
        InitializeComponent();
        this.MyViewModel = homePageVM;
        BindingContext = homePageVM;
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (MyViewModel.isFirstTimeOpeningApp)
        {
            await Shell.Current.GoToAsync(nameof(FirstStepPage));
            return;
        }

        if (MyViewModel.TemporarilyPickedSong is null)
        {
            return;
        }
        MyViewModel.CurrentPage = PageEnum.MainPage;


    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        MyViewModel.NowPlayBtmSheetState = DevExpress.Maui.Controls.BottomSheetState.Hidden;
    }
    bool isOnFocusMode = false;
   

    //colview tap play
    private async void SongsColView_Tap(object sender, DevExpress.Maui.CollectionView.CollectionViewGestureEventArgs e)
    {
        MyViewModel.CurrentQueue = 0;
        if (MyViewModel.IsOnSearchMode)
        {
            MyViewModel.CurrentQueue = 1;
            var filterSongs = Enumerable.Range(0, SongsColView.VisibleItemCount)
                     .Select(i => SongsColView.GetItemHandleByVisibleIndex(i))
                     .Where(handle => handle != -1)
                     .Select(handle => SongsColView.GetItem(handle) as SongModelView)
                     .Where(item => item != null)
                     .ToList()!;
            
        }
        MyViewModel.PlaySong(e.Item as SongModelView);

        await Task.WhenAll(BtmBar.AnimateNewTrackBounce(duration: 500),
            BtmBar.BackgroundColorTo(Color.FromArgb("#483D8B"), length: 500));
    }

    protected override bool OnBackButtonPressed()
    {
        if (!NormalNowPlayingUI.IsVisible)
        {
            Task.WhenAll(
            CurrentView.AnimateFadeOutBack(),
            NormalNowPlayingUI.AnimateFadeInFront()

            );
            CurrentView = NormalNowPlayingUI;
            isOnFocusMode = false;

            SearchBy.Unfocus();

        }
        switch (HomeTabView.SelectedItemIndex)
        {
            case 0:                
                break;
            case 1:
                HomeTabView.SelectedItemIndex = 0;
                break;
            default:
                break;
        }
        return true;
    }

    private void ShowMoreBtn_Clicked(object sender, EventArgs e)
    {
        var s = (View)sender;
        var song = (SongModelView)s.BindingContext;
        MyViewModel.SetContextMenuSong(song);
        SongsMenuPopup.Show();

    }
    private async void GotoArtistBtn_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.NavigateToArtistsPage(1);
        SongsMenuPopup.Close();
    }
    private void ClosePopup(object sender, EventArgs e)
    {
        SongsMenuPopup.Close();
    }

    
    private void SearchBy_TextChanged(object sender, EventArgs e)
    {
        switch (SearchParam)
        {
            case "Title":
                ByTitle();
                break;
            case "Artist":
                ByArtist();
                break;
            case "":
                ByAll();
                break;
            default:
                ByAll();
                break;
        }
        
    }

    private void ByTitle()
    {
        if (!string.IsNullOrEmpty(SearchBy.Text))
        {
            if (SearchBy.Text.Length >= 1)
            {
                MyViewModel.IsOnSearchMode = true;                
                SongsColView.FilterString = $"Contains([Title], '{SearchBy.Text}')";
            }
            else
            {
                MyViewModel.IsOnSearchMode = false;
                SongsColView.FilterString = string.Empty;
            }
        }
    }
    private void ByAll()
    {
        if (!string.IsNullOrEmpty(SearchBy.Text))
        {
            if (SearchBy.Text.Length >= 1)
            {
                MyViewModel.IsOnSearchMode = true;
                SongsColView.FilterString = 
                    $"Contains([Title], '{SearchBy.Text}') OR " +
                    $"Contains([ArtistName], '{SearchBy.Text}') OR " +
                    $"Contains([AlbumName], '{SearchBy.Text}')";
            }
            else
            {
                MyViewModel.IsOnSearchMode = false;
                SongsColView.FilterString = string.Empty;
            }
        }
    }
    private void ByArtist()
    {
        if (!string.IsNullOrEmpty(SearchBy.Text))
        {
            if (SearchBy.Text.Length >= 1)
            {
                MyViewModel.IsOnSearchMode = true;                
                SongsColView.FilterString = $"Contains([ArtistName], '{SearchBy.Text}')";
                
            }
            else
            {
                MyViewModel.IsOnSearchMode = false;
                SongsColView.FilterString = string.Empty;
            }
        }
    }

  
    DXLayoutBase? CurrentView { get; set; }
    
    string SearchParam = string.Empty;
    private async void SearchSong_Tap(object sender, HandledEventArgs e)
    {
        
        if (!SearchModeUI.IsVisible)
        {
            await Task.WhenAll(SearchModeUI.AnimateFadeInFront()
                );
            //CurrentView!.AnimateFadeOutBack());
            isOnFocusMode = true;
            //CurrentView = SearchModeUI;

            SearchBy.Focus();
            SearchParam = "Title";
        }
        else
        {
            await Task.WhenAll(SearchModeUI.AnimateFadeOutBack()
                );
            //CurrentView!.AnimateFadeOutBack());
            isOnFocusMode = false;
            //CurrentView = SearchModeUI;

            SearchBy.Unfocus();
            SearchParam = string.Empty;

        }
    }

    private void NormalNowPlayingUI_Loaded(object sender, EventArgs e)
    {
        CurrentView = NormalNowPlayingUI;
    }
    
    private void NormalNowPlayingUI_Unloaded(object sender, EventArgs e)
    {
        CurrentView = null;
    
    }

    
    private double _startX;
    private double _startY;
    private bool _isPanning;
    private CancellationTokenSource _debounceTokenSource = new CancellationTokenSource();
    private int _lastFullyVisibleHandle = -1; // Track the last *fully visible* item handle.

    private async void PanGesture_PanUpdated(object sender, PanUpdatedEventArgs e)
    {
        var send = (DXBorder)sender;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _isPanning = true;
                _startX = BtmBar.TranslationX;  
                _startY = BtmBar.TranslationY;
                break;

            case GestureStatus.Running:
                if (!_isPanning)
                    return; // Safety check

                BtmBar.TranslationX = _startX + e.TotalX;
                BtmBar.TranslationY = _startY + e.TotalY;
                break;

            case GestureStatus.Completed:
                _isPanning = false; 

                double deltaX = BtmBar.TranslationX - _startX;
                double deltaY = BtmBar.TranslationY - _startY;
                double absDeltaX = Math.Abs(deltaX);
                double absDeltaY = Math.Abs(deltaY);

                // Haptic feedback based on direction
                if (absDeltaX > absDeltaY) // Horizontal swipe
                {
                    if (absDeltaX > absDeltaY) // Horizontal swipe
                    {
                        try
                        {
                            if (deltaX > 0) // Right
                            {
                                HapticFeedback.Perform(HapticFeedbackType.LongPress);
                                Debug.WriteLine("Swiped Right");

                                MyViewModel.PlayNextSongCommand.Execute(null);

                                var colorTask = AnimateColor(send, Colors.SlateBlue);
                                var bounceTask = BtmBar.TranslateTo(0, 0, 250, Easing.BounceOut);

                                await Task.WhenAll(colorTask, bounceTask);
                            }
                             else // Left
                            {
                                Vibration.Vibrate(TimeSpan.FromMilliseconds(50)); // Short vibration
                                MyViewModel.PlayPreviousSongCommand.Execute(null);

                                var colorTask = AnimateColor(send, Colors.MediumPurple);
                                var bounceTask = BtmBar.TranslateTo(0, 0, 250, Easing.BounceOut);

                                await Task.WhenAll(colorTask, bounceTask);
                            }
                        }
                        catch { }
                    }

                    else // Left
                    {
                        try
                        {
                            Vibration.Vibrate(TimeSpan.FromMilliseconds(50)); // Short vibration
                            MyViewModel.PlayPreviousSongCommand.Execute(null);
                            Debug.WriteLine("Swiped left");
                            var t1= send.BackgroundColorTo(Colors.MediumPurple, length: 300); 
                            var t2=  Task.Delay(500);
                            var t3 = send.BackgroundColorTo(Colors.DarkSlateBlue, length: 300); 
                            await Task.WhenAll(t1, t2, t3);
                        }
                        catch { }
                    }
                }
                else  //Vertical swipe
                {
                    if (deltaY > 0) // Down
                    {
                        try
                        {
                            var itemHandle = SongsColView.FindItemHandle(MyViewModel.TemporarilyPickedSong);
                            SongsColView.ScrollTo(itemHandle, DevExpress.Maui.Core.DXScrollToPosition.Start);
                            
                            HapticFeedback.Perform(HapticFeedbackType.LongPress);
                        }
                        catch { }
                    }
                    else  //Up
                    {
                        try
                        {
                            if (HomeTabView.SelectedItemIndex != 1)
                            {
                                HomeTabView.SelectedItemIndex = 1;
                                MyViewModel.CurrentPage = PageEnum.NowPlayingPage;
                            }
                            else
                            {
                                HomeTabView.SelectedItemIndex = prevViewIndex;
                            }
                        }
                        catch { }
                    }

                }

                await BtmBar.TranslateTo(0, 0, 250, Easing.BounceOut);
                break;


            case GestureStatus.Canceled:  
                _isPanning = false;
                await BtmBar.TranslateTo(0, 0, 250, Easing.BounceOut); // Return to original position
                break;

        }
    }
    int prevViewIndex = 0;
    // Extracted color animation method for reusability
    async Task AnimateColor(VisualElement element, Color color)
    {
        await element.BackgroundColorTo(color, length: 300);
        await Task.Delay(300); // Reduce freeze by using a lower delay
        await element.BackgroundColorTo(Colors.DarkSlateBlue, length: 300);
    }
    private void ViewNowPlayPage_Tap(object sender, HandledEventArgs e)
    {
        MyViewModel.UpdateContextMenuData(MyViewModel.MySelectedSong);
        CurrentQueueListBtmSheetView.State = BottomSheetState.HalfExpanded;
    }

    //mini bar tap play/pause

    private async void BtmBarTapGest_Tapped(object sender, TappedEventArgs e)
    {
        var send = (DXBorder)sender;
        
        if (MyViewModel.IsPlaying)
        {
            
            MyViewModel.PauseSong();
            RunFocusModeAnimation(send, Color.FromArgb("#8B0000")); // DarkRed for pause
            
            await send.BackgroundColorTo(Color.FromArgb("#252526"), length: 300);
        }
        else
        {
            await send.BackgroundColorTo(Color.FromArgb("#483D8B"), length: 300);
            //RunFocusModeAnimation(send, Color.FromArgb("#483D8B")); // DarkSlateBlue for resume
            if (MyViewModel.CurrentPositionInSeconds.IsZeroOrNaN())
            {
                MyViewModel.PlaySong(MyViewModel.TemporarilyPickedSong);
            }
            else
            {
                MyViewModel.ResumeSong();
            }
        }

    }

    public void RunFocusModeAnimation(DXBorder bView, Color strokeColor)
    {
        if (bView == null)
            return;

        // Set the stroke color based on pause/resume state
        bView.BorderColor= strokeColor;

        // Define a single animation to embiggen the stroke
        var expandAnimation = new Animation(v => bView.BorderThickness = v, // Only animating BorderThickness now
            0,                                   // Start with 0 thickness
            5,                                  // Expand to 10 thickness
            Easing.CubicInOut                    // Smooth easing
        );

        // Shrink the stroke back to zero after embiggen
        var shrinkAnimation = new Animation(
            v => bView.BorderThickness = v,
            5,                                   // Start at 10 thickness
            0,                                    // Reduce to 0 thickness
            Easing.CubicInOut
        );

        // Combine expand and shrink animations into one sequence
        var animationSequence = new Animation
        {
            { 0, 0.5, expandAnimation },   // Embiggen in the first half
            { 0.5, 1, shrinkAnimation }    // Shrink back in the second half
        };

        // Run the full animation sequence
        animationSequence.Commit(bView, "FocusModeAnimation", length: 300, easing: Easing.Linear);
    }

    private void BtmBarTitle_Loaded(object sender, EventArgs e)
    {
        
    }

    private void BtmBar_Loaded(object sender, EventArgs e)
    {

    }

    private void BtmBar_BindingContextChanged(object sender, EventArgs e)
    {

    }

    private void SongsColView_Scrolled(object sender, DevExpress.Maui.CollectionView.DXCollectionViewScrolledEventArgs e)
    {
        var itemHandle = SongsColView.FindItemHandle(MyViewModel.TemporarilyPickedSong);
        bool isFullyVisible = e.FirstVisibleItemHandle <= itemHandle && itemHandle <= e.LastVisibleItemHandle;
        
    }


    private void SongFrom2ndaryQueueBtn_Tap(object sender, DXTapEventArgs e)
    {
       
    }

    private void CurrQueueColView_Loaded(object sender, EventArgs e)
    {

    }

    private void SingleSongRow_TapReleased(object sender, DXTapEventArgs e)
    {
        var send = (View)sender;
        var song = (SongModelView)send.BindingContext;
        if (song is not null)
        {
            song.IsCurrentPlayingHighlight = false;
        }

        MyViewModel.PlaySong(song);
    }

    private async void LyricsColView_SelectionChanged(object sender, CollectionViewSelectionChangedEventArgs e)
    {
        if (!this.IsLoaded)
        {
            return;
        }
        try
        {
            if (LyricsColView.SelectedItem is not null)
            {
                // Set SelectedItem FIRST to ensure UI updates
                LyricsColView.SelectedItem = MyViewModel.CurrentLyricPhrase;

                // Let UI process selection before animating
                await Task.Delay(10);

                // Animate Font Size First
                if (e.RemovedItems?.Count > 0)
                {
                    foreach (LyricPhraseModel oldItem in e.RemovedItems.Cast<LyricPhraseModel>())
                        oldItem.NowPlayingLyricsFontSize = 29;
                }
                if (e.AddedItems?.Count > 0)
                {
                    foreach (LyricPhraseModel newItem in e.AddedItems.Cast<LyricPhraseModel>())
                        newItem.NowPlayingLyricsFontSize = 60;
                }

                // Wait a bit so font size change is visible before scrolling
                await Task.Delay(10);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    //var itemHandle = LyricsColView.FindItemHandle(MyViewModel.TemporarilyPickedSong);
                    //LyricsColView.ScrollTo(itemHandle,DXScrollToPosition.Start);
                }
                );
                // Scroll AFTER font size animation
            }

            // Animate selection smoothly
            if (e.AddedItems.FirstOrDefault() is LyricPhraseModel selectedLyric)
            {
                var item = LyricsColView.ItemTemplate.CreateContent() as View;
                if (item != null)
                {
                    item.TranslationY = 50;
                    await item.TranslateTo(0, 0, 400, Easing.BounceOut);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    Label CurrentLyrLabel { get; set; }
    private void LyrBorder_Loaded(object sender, EventArgs e)
    {
        CurrentLyrLabel = (Label)sender;
    }

    private void SeekSongPosFromLyric_Tapped(object sender, TappedEventArgs e)
    {
        if (MyViewModel.IsPlaying)
        {
            var bor = (Label)sender;
            var lyr = (LyricPhraseModel)bor.BindingContext;
            MyViewModel.SeekSongPosition(lyr);
        }
    }

    private void SearchBy_ClearIconClicked(object sender, HandledEventArgs e)
    {
        SongsColView.RefreshData();
    }

    private void SearchFiltersChips_ChipTap(object sender, ChipEventArgs e)
    {
        var send = (ChoiceChipGroup)sender;
        var chip = (Chip)send.SelectedItem;
        SearchParam = chip.TapCommandParameter.ToString()!;
    }
}
