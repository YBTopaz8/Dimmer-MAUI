using System.Diagnostics;

namespace Dimmer_MAUI.Views.Desktop;

public partial class SingleSongShellPageD : ContentPage
{
	public SingleSongShellPageD(HomePageVM homePageVM)
    {
        InitializeComponent();
        MyViewModel = homePageVM;
        BindingContext = homePageVM;
        MediaPlayBackCW.BindingContext = homePageVM;

    }
    public HomePageVM MyViewModel { get; }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (MyViewModel.TemporarilyPickedSong is null)
        {
            return;
        }
        MyViewModel.CurrentPage = PageEnum.NowPlayingPage;
        
        MyViewModel.AssignSyncLyricsCV(LyricsColView);
        switch (MyViewModel.TemporarilyPickedSong.IsFavorite)
        {
            
            case true:
                RatingChipCtrl.SelectedItem = LoveRate;
                break;
            case false:
                RatingChipCtrl.SelectedItem = HateRate;
                break;
            default:
                break;
        }


        if (LyricsColView.SelectedItem is not null)
        {
            LyricsColView.ScrollTo(LyricsColView.SelectedItem, null, ScrollToPosition.Center, true);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StatsTabs.SelectedItem = SyncLyricsChip;
        MyViewModel.LyricsSearchAlbumName = string.Empty;
        MyViewModel.LyricsSearchArtistName= string.Empty;
        MyViewModel.LyricsSearchSongTitle= string.Empty;
    }

    private void TabView_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.TabView.TabSelectionChangedEventArgs e)
    {
        switch (e.NewIndex)
        {
            case 0:
                break;
            case 1:
                emptyV.IsVisible = false;
                if (MyViewModel.AllSyncLyrics is not null)
                {
                    MyViewModel.AllSyncLyrics = new();
                }
                break;
            case 2:
                break;
            default:
                
                break;
        }
        if (e.NewIndex == 2)
        {
            MyViewModel.ShowSingleSongStatsCommand.Execute(MyViewModel.MySelectedSong);
        }
    }

    private void SongsPlayed_SelectionChanged(object sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    {

    }

    private bool _isThrottling = false;
    private readonly int throttleDelay = 300; // Time in milliseconds

    private async void Slider_DragCompleted(object sender, EventArgs e)
    {
        if (_isThrottling)
            return;

        _isThrottling = true;

        MyViewModel.SeekSongPosition();


        await Task.Delay(throttleDelay);
        _isThrottling = false;
    }

    bool isOnFocusMode = false;
    /*
    private async void FocusModePointerRec_PointerEntered(object sender, PointerEventArgs e)
    {
        if (isOnFocusMode)
        {
            if (ViewModel.IsSleek)
            {
                return;
            }
            await FocusModeUI.AnimateFocusModePointerEnter(500);
            leftImgBtn.IsVisible = true;
            rightImgBtn.IsVisible = true;
        }
    }

    private async void FocusModePointerRec_PointerExited(object sender, PointerEventArgs e)
    {
        if (isOnFocusMode)
        {
            await FocusModeUI.AnimateFocusModePointerExited(500);
            leftImgBtn.IsVisible = false;
            rightImgBtn.IsVisible = false;
        }
    }
    */
    private void ToggleSleekModeClicked(object sender, EventArgs e)
    {

        //await FocusModeUI.AnimateFocusModePointerExited();
        leftImgBtn.IsVisible = false;
        rightImgBtn.IsVisible = false;
        MyViewModel.ToggleSleekModeCommand.Execute(true);
    }
    private async void ToggleFocusModeClicked(object sender, EventArgs e)
    {
        if (FocusModeUI.IsVisible)
        {
            await Task.WhenAll(
            FocusModeUI.AnimateFadeOutBack(),
            NormalNowPlayingUI.AnimateFadeInFront()

            );

            isOnFocusMode = false;
        }
        else
        {
            await Task.WhenAll(
            FocusModeUI.AnimateFadeInFront(),
            NormalNowPlayingUI.AnimateFadeOutBack());
            isOnFocusMode = true;
        }
    }



    private async void PointerGestureRecognizer_PointerPressed(object sender, PointerEventArgs e)
    {
        var send = (View)sender;
        await send.AnimateHighlightPointerPressed();
    }

    private async void PointerGestureRecognizer_PointerReleased(object sender, PointerEventArgs e)
    {
        var send = (View)sender;
        await send.AnimateHighlightPointerReleased();
    }

    private void FocusModePlayResume_Tapped(object sender, TappedEventArgs e)
    {
        if (MyViewModel.IsPlaying)
        {
            MyViewModel.PauseSongCommand.Execute(null);
            RunFocusModeAnimation(sender as AvatarView, Color.FromArgb("#8B0000")); // DarkRed for pause
        }
        else
        {
            MyViewModel.ResumeSongCommand.Execute(null);
            RunFocusModeAnimation(sender as AvatarView, Color.FromArgb("#483D8B")); // DarkSlateBlue for resume
        }
    }

    private void RunFocusModeAnimation(AvatarView avatarView, Color strokeColor)
    {
        if (avatarView == null)
            return;

        // Set the stroke color based on pause/resume state
        avatarView.Stroke = strokeColor;

        // Define a single animation to embiggen the stroke
        var expandAnimation = new Animation(v => avatarView.StrokeThickness = v, // Only animating StrokeThickness now
            0,                                   // Start with 0 thickness
            5,                                  // Expand to 10 thickness
            Easing.CubicInOut                    // Smooth easing
        );

        // Shrink the stroke back to zero after embiggen
        var shrinkAnimation = new Animation(
            v => avatarView.StrokeThickness = v,
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
        animationSequence.Commit(avatarView, "FocusModeAnimation", length: 500, easing: Easing.Linear);
    }



    private void LyricsColView_SelectionChanged(object sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    {
        try
        {
            
            if (LyricsColView.ItemsSource is not null)
            {
                if (LyricsColView.SelectedItem is not null)
                {
                    LyricsColView.ScrollTo(LyricsColView.SelectedItem, null, ScrollToPosition.Center, true);
                }
            }

            // --- Reset FontSize for Previously Selected Items ---
            if (e.PreviousSelection != null && e.PreviousSelection.Count > 0)
            {
                foreach (LyricPhraseModel oldItem in e.PreviousSelection.Cast<LyricPhraseModel>())
                {
                    oldItem.NowPlayingLyricsFontSize = 29; // Set FontSize to 19 for unselected
                    //Debug.WriteLine($"Item unselected, set FontSize to 19: {oldItem?.Text}");
                }
            }

            // --- Set FontSize for Currently Selected Items ---
            if (e.CurrentSelection != null && e.CurrentSelection.Count > 0)
            {
                foreach (LyricPhraseModel newItem in e.CurrentSelection.Cast<LyricPhraseModel>())
                {
                    newItem.NowPlayingLyricsFontSize = 61; // Set FontSize to 21 for selected
                    //Debug.WriteLine($"Item selected, set FontSize to 21: {newItem?.Text}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }


    // Helper to find the visual element for an item
    private View GetViewForItem(object item)
    {
        foreach (var cell in LyricsColView.GetVisualTreeDescendants().OfType<ViewCell>())
        {
            if (cell.BindingContext == item)
                return cell.View;
        }
        return null;
    }

    // Animation helper
    private async Task AnimateItem(View view, double targetScale)
    {
        if (view == null)
            return;
        await view.ScaleTo(targetScale, 250, Easing.SpringOut);
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

    bool CanScroll = true;
    private void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {
        CanScroll = false;
    }

    private void PointerGestureRecognizer_PointerExited(object sender, PointerEventArgs e)
    {
        CanScroll = true;
    }


    private async void ViewLyricsBtn_Clicked(object sender, EventArgs e)
    {
        var send = (Button)sender;
        var title = send.Text;
        var thisContent = send.BindingContext as Dimmer_MAUI.Utilities.Models.Content;
        if (title == "Synced Lyrics")
        {

            await MyViewModel.ShowSingleLyricsPreviewPopup(thisContent!, false);
        }
        else
        if (title == "Plain Lyrics")
        {

            await MyViewModel.ShowSingleLyricsPreviewPopup(thisContent!, true);
        }
    }

    private void ShowHideChart_CheckChanged(object sender, EventArgs e)
    {
        
    }

    private async void SearchLyricsOnLyrLib_Clicked(object sender, EventArgs e)
    {
        emptyV.IsVisible = true;
        await Task.WhenAll(Lookgif.AnimateFadeInFront(), fetchFailed.AnimateFadeOutBack(),
NoLyricsFoundMsg.AnimateFadeOutBack());

        Lookgif.IsVisible = true;

        await MyViewModel.FetchLyrics(true);

        await Task.WhenAll(Lookgif.AnimateFadeOutBack(), fetchFailed.AnimateFadeInFront(),
NoLyricsFoundMsg.AnimateFadeInFront());
        fetchFailed.IsAnimationPlaying = false;
        
        await Task.Delay(3000);
        fetchFailed.IsAnimationPlaying = false;
        fetchFailed.IsVisible = false;
        emptyV.IsVisible = false;
    }

    private async void SongShellChip_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.Chips.SelectionChangedEventArgs e)
    {
        var selectedTab = StatsTabs.SelectedItem;
        var send = (SfChipGroup)sender;
        var selected = send.SelectedItem as SfChip;
        if (selected is null)
        {
            return;
        }
        _ = int.TryParse(selected.CommandParameter.ToString(), out int selectedStatView);

        switch (selectedStatView)
        {
            case 0:

                //GeneralStatsView front, rest back
                break;
            case 1:
                //SongsStatsView front, rest back
                //MyViewModel.GetNotListenedStreaks();
                //MyViewModel.GetTopStreakTracks();

                //MyViewModel.GetGoldenOldies();


                //MyViewModel.GetBiggestFallers(DateTime.Now.Month, DateTime.Now.Year);
                //MyViewModel.GetStatisticalOutlierSongs();
                //MyViewModel.GetDailyListeningVolume();
                //MyViewModel.GetUniqueTracksInMonth(DateTime.Now.Month, DateTime.Now.Year);
                //MyViewModel.GetNewTracksInMonth(DateTime.Now.Month, DateTime.Now.Year);
                //MyViewModel.GetOngoingGapBetweenTracks();
                break;
            case 2:
                break;
            case 3:
                break;
            case 4:
                MyViewModel.CalculateGeneralSongStatistics(MyViewModel.MySelectedSong.LocalDeviceId);
                
                
                break;
            case 5:

                break;
            case 6:

                break;
            default:

                break;
        }

        var viewss = new Dictionary<int, View>
        {
            {0, SyncedLyricGrid},
            {1, PlainLyricsGrid},
            {2, SearchLyricsGrid},
            {3, SongDetails},
            {4, SongStatsGrid},
            
        };
        if (!viewss.ContainsKey(selectedStatView))
            return;

        await Task.WhenAll
            (viewss.Select(kvp =>
            kvp.Key == selectedStatView
            ? kvp.Value.AnimateFadeInFront()
            : kvp.Value.AnimateFadeOutBack()));
        return;
    }
    private void RatingChipCtrl_ChipClicked(object sender, EventArgs e)
    {
        var ee = (SfChip)sender;

        MyViewModel.RateSongCommand.Execute(ee.CommandParameter.ToString()!);
    }

    List<string> SelectedSongIds = new List<string>();
    List<DateTime> FilterDates = new ();
    private void SongShellTabView_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.TabView.TabSelectionChangedEventArgs e)
    {
        //DailyCalender.MaximumDate = DateTime.Now;
        string SelectedSong1Id = string.Empty;
        if (MyViewModel is null || MyViewModel.TemporarilyPickedSong is null || MyViewModel.MySelectedSong is null || MyViewModel.MySelectedSong.LocalDeviceId is null)
        {            
            return;
        }
        SelectedSong1Id = MyViewModel.MySelectedSong.LocalDeviceId;
        SelectedSongIds = new List<string> { SelectedSong1Id };
        if (e.NewIndex == 6)
        {
            //ViewModel.GetPlayCompletionStatus(SelectedSongIds);
            //PeriodTabView.SelectedIndex = 0;
            //ViewModel.LoadDailyData(SelectedSongIds);
        }
    }

    private void DailyCalender_Tapped(object sender, Syncfusion.Maui.Toolkit.Calendar.CalendarTappedEventArgs e)
    {
        if (!FilterDates.Contains(e.Date))
        {
            FilterDates.Clear();
            FilterDates.Add(e.Date);
            //ViewModel.LoadDailyData(SelectedSongIds,FilterDates);
        }
        else
        {
            return;
            
         
        }
    }

    private async void FocusModePointerRec_PointerEntered(object sender, PointerEventArgs e)
    {
        var send = (View)sender;
        await send.DimmIn(500);

    }
    private async void FocusModePointerRec_PointerExited(object sender, PointerEventArgs e)
    {
        var send = (View)sender;
        await send.DimmOut(300);

    }

    private async void PreviewImage_Clicked(object sender, EventArgs e)
    {
        var send = (Button)sender;
        var item = send.BindingContext as Dimmer_MAUI.Utilities.Models.Content;
        NormalNowPlayingUI.IsVisible = false;
        PageBGImg.Source = item.LinkToCoverImage;
        await Task.Delay(2000);
        NormalNowPlayingUI.IsVisible = true;
        PageBGImg.Source = MyViewModel.TemporarilyPickedSong.CoverImagePath;
    }

    private void SaveImageInfo_Clicked(object sender, EventArgs e)
    {
        
        
    }

    private void SfChip_Clicked(object sender, EventArgs e)
    {

    }

    private async void FocusModePointerRec_PEntered(object sender, PointerEventArgs e)
    {
        var send = (View)sender;
        await send.DimmIn(500);

    }
    private async void FocusModePointerRec_PExited(object sender, PointerEventArgs e)
    {
        var send = (View)sender;
        await send.DimmOut(300);

    }

    private void StatView_Loaded(object sender, EventArgs e)
    {
        var send = (View)sender;
        _ = send.DimmOut(300);

    }

    public void ChangeFontSize()
    {
        
        
    }

    private void ImageButton_Clicked(object sender, EventArgs e)
    {

    }

    private async void NPSongImage_Tapped(object sender, TappedEventArgs e)
    {
        if (FocusModeUI.IsVisible)
        {
            await Task.WhenAll(
            FocusModeUI.AnimateFadeOutBack(),
            NormalNowPlayingUI.AnimateFadeInFront()

            );

            isOnFocusMode = false;
        }
        else
        {
            await Task.WhenAll(
            FocusModeUI.AnimateFadeInFront(),
            NormalNowPlayingUI.AnimateFadeOutBack());
            isOnFocusMode = true;
        }
    }

    Label CurrentLyrLabel { get; set; }
    private void Label_Loaded(object sender, EventArgs e)
    {
        CurrentLyrLabel = (Label)sender;
    }
    private void LyrBorder_SizeChanged(object sender, EventArgs e)
    {

    }

    private void LyrBorder_ParentChanged(object sender, EventArgs e)
    {

    }


    List<string> supportedFilePaths;
    bool isAboutToDropFiles = false;
    private async void DropGestureRecognizer_DragOver(object sender, DragEventArgs e)
    {
        try
        {

            if (!isAboutToDropFiles)
            {
                isAboutToDropFiles = true;
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
                                continue;
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
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
        //return Task.CompletedTask;
    }

    private void DropGestureRecognizer_DragLeave(object sender, DragEventArgs e)
    {
        try
        {
            isAboutToDropFiles = false;
            var send = sender as View;
            if (send is null)
            {
                return;
            }
            send.Opacity = 1;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    private void DropGestureRecognizer_Drop(object sender, DropEventArgs e)
    {
        supportedFilePaths ??= new();
        isAboutToDropFiles = false;
        
        
        if (supportedFilePaths.Count > 0)
        {            
            MyViewModel.LoadLocalSongFromOutSideApp(supportedFilePaths);
        }
    }

}
