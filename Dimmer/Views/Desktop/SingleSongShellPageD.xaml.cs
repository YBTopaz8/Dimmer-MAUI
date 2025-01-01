using Syncfusion.Maui.Toolkit.Chips;

namespace Dimmer_MAUI.Views.Desktop;

public partial class SingleSongShellPageD : ContentPage
{
	public SingleSongShellPageD(HomePageVM homePageVM)
    {
        InitializeComponent();
        ViewModel = homePageVM;
        BindingContext = homePageVM;
        MediaPlayBackCW.BindingContext = homePageVM;

    }
    public HomePageVM ViewModel { get; }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (ViewModel.TemporarilyPickedSong is null)
        {
            return;
        }
        ViewModel.CurrentPage = PageEnum.NowPlayingPage;
        
        ViewModel.AssignSyncLyricsCV(LyricsColView);
        switch (ViewModel.TemporarilyPickedSong.IsFavorite)
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
        ViewModel.LyricsSearchAlbumName = string.Empty;
        ViewModel.LyricsSearchArtistName= string.Empty;
        ViewModel.LyricsSearchSongTitle= string.Empty;
    }

    private void TabView_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.TabView.TabSelectionChangedEventArgs e)
    {
        switch (e.NewIndex)
        {
            case 0:
                break;
            case 1:
                emptyV.IsVisible = false;
                if (ViewModel.AllSyncLyrics is not null)
                {
                    ViewModel.AllSyncLyrics = new();
                }
                break;
            case 2:
                break;
            default:
                
                break;
        }
        if (e.NewIndex == 2)
        {
            ViewModel.ShowSingleSongStatsCommand.Execute(ViewModel.SelectedSongToOpenBtmSheet);
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

        ViewModel.SeekSongPosition();


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
        ViewModel.ToggleSleekModeCommand.Execute(true);
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
        if (ViewModel.IsPlaying)
        {
            ViewModel.PauseSongCommand.Execute(null);
            RunFocusModeAnimation(sender as AvatarView, Color.FromArgb("#8B0000")); // DarkRed for pause
        }
        else
        {
            ViewModel.ResumeSongCommand.Execute(null);
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
                if (LyricsColView.SelectedItem is not null )
                {
                    LyricsColView.ScrollTo(LyricsColView.SelectedItem, null, ScrollToPosition.Center, true);
                }
            }            
         
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    private void SeekSongPosFromLyric_Tapped(object sender, TappedEventArgs e)
    {
        if (ViewModel.IsPlaying)
        {
            var bor = (Border)sender;
            var lyr = (LyricPhraseModel)bor.BindingContext;
            ViewModel.SeekSongPosition(lyr);
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

            await ViewModel.ShowSingleLyricsPreviewPopup(thisContent!, false);
        }
        else
        if (title == "Plain Lyrics")
        {

            await ViewModel.ShowSingleLyricsPreviewPopup(thisContent!, true);
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

        await ViewModel.FetchLyrics(true);

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
                //HomePageVM.GetNotListenedStreaks();
                //HomePageVM.GetTopStreakTracks();

                //HomePageVM.GetGoldenOldies();


                //HomePageVM.GetBiggestFallers(DateTime.Now.Month, DateTime.Now.Year);
                //HomePageVM.GetStatisticalOutlierSongs();
                //HomePageVM.GetDailyListeningVolume();
                //HomePageVM.GetUniqueTracksInMonth(DateTime.Now.Month, DateTime.Now.Year);
                //HomePageVM.GetNewTracksInMonth(DateTime.Now.Month, DateTime.Now.Year);
                //HomePageVM.GetOngoingGapBetweenTracks();
                break;
            case 2:
                break;
            case 3:
                break;
            case 4:
                ViewModel.CalculateGeneralSongStatistics(ViewModel.SelectedSongToOpenBtmSheet.LocalDeviceId);
                
                
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

        ViewModel.RateSongCommand.Execute(ee.CommandParameter.ToString()!);
    }

    List<string> SelectedSongIds = new List<string>();
    List<DateTime> FilterDates = new ();
    private void SongShellTabView_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.TabView.TabSelectionChangedEventArgs e)
    {
        //DailyCalender.MaximumDate = DateTime.Now;
        string SelectedSong1Id = string.Empty;
        if (ViewModel is null || ViewModel.TemporarilyPickedSong is null || ViewModel.SelectedSongToOpenBtmSheet is null || ViewModel.SelectedSongToOpenBtmSheet.LocalDeviceId is null)
        {            
            return;
        }
        SelectedSong1Id = ViewModel.SelectedSongToOpenBtmSheet.LocalDeviceId;
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
        PageBGImg.Source = ViewModel.TemporarilyPickedSong.CoverImagePath;
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
    Label CurrentLyrLabel { get; set; }
    private void Label_Loaded(object sender, EventArgs e)
    {
        CurrentLyrLabel = (Label)sender;
    }

    private void ImageButton_Clicked(object sender, EventArgs e)
    {

    }

}
