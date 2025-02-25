#if WINDOWS

#endif


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
        if (MyViewModel.MySelectedSong is null)
        {
            return;
        }
        MyViewModel.CurrentPage = PageEnum.NowPlayingPage;
        MyViewModel.CurrentPageMainLayout = MainDock;
        MyViewModel.AssignSyncLyricsCV(LyricsColView);
        MyViewModel.DoRefreshDependingOnPage();
        switch (MyViewModel.MySelectedSong.IsFavorite)
        {
            
            case true:
                RatingChipCtrl.SelectedItem = LoveRate;
                break;
            case false:
                switch (MyViewModel.MySelectedSong.Rating)
                {
                    case 0:
                        RatingChipCtrl.SelectedItem = NeutralRate;
                        break;
                    case 1:
                        //RatingChipCtrl.SelectedItem = LikeRate;
                        break;
                    case 2:
                        RatingChipCtrl.SelectedItem = LoveRate;
                        break;
                    case 3:
                        //RatingChipCtrl.SelectedItem = DislikeRate;
                        break;
                    case 4:
                        //RatingChipCtrl.SelectedItem = HateRate;
                        break;
                    case 5:
                        RatingChipCtrl.SelectedItem = HateRate;
                        break;
                            

                    default:
                        break;
                }
                RatingChipCtrl.SelectedItem = NeutralRate;
                break;
            default:
                break;
        }

    }



    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StatsTabs.SelectedItem = SyncLyricsChip;
        MyViewModel.LyricsSearchAlbumName = string.Empty;
        MyViewModel.LyricsSearchArtistName = string.Empty;
        MyViewModel.LyricsSearchSongTitle = string.Empty;
        MyViewModel.UnAssignSyncLyricsCV();
    }


    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
       
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
        var send = (Border)sender;
        if (MyViewModel.IsPlaying)
        {
            MyViewModel.PauseSongCommand.Execute(null);
            send.RunFocusModeAnimation( Color.FromArgb("#8B0000")); // DarkRed for pause
        }
        else
        {
            MyViewModel.ResumeSongCommand.Execute(null);
            send.RunFocusModeAnimation(Color.FromArgb("#483D8B")); // DarkSlateBlue for resume
        }
    }

    int unFocusedLyricSize = 29;
    int focusedLyricSize = 60;
   
    private void SeekSongPosFromLyric_Tapped(object sender, TappedEventArgs e)
    {
        if (MyViewModel.IsPlaying)
        {
            var bor = (View)sender;
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
        if (send.SelectedItem is not SfChip selected)
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

    Border CurrentLyrLabel { get; set; }
    private void Label_Loaded(object sender, EventArgs e)
    {
        CurrentLyrLabel = (Border)sender;
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

    private void ImageButton_Clicked_1(object sender, EventArgs e)
    {

    }

    private async void ToggleFullScreen_Clicked(object sender, EventArgs e)
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

    private void ToggleShellPane_Clicked(object sender, EventArgs e)
    {
        MyViewModel.ToggleFlyout();

    }

    private void LyricsColView_Loaded(object sender, EventArgs e)
    {
#if WINDOWS
        try
        {
            var nativeView = LyricsColView.Handler?.PlatformView;


            Debug.WriteLine(Shell.Current.CurrentPage);
            if (nativeView is Microsoft.UI.Xaml.Controls.Primitives.Selector selector)
            {
                selector.Background = null;
            }

            if (nativeView is Microsoft.UI.Xaml.Controls.ItemsControl itemsControl)
            {
                itemsControl.Background = null;
            }

            if (nativeView is Microsoft.UI.Xaml.Controls.Control control)
            {
                control.Background = null;
            }

            if (nativeView is Microsoft.UI.Xaml.UIElement uiElement)
            {
                uiElement.Visibility = Microsoft.UI.Xaml.Visibility.Visible; // Make sure it's still visible
            }
            if (nativeView is Microsoft.UI.Xaml.Controls.ListView listView)
            {

                listView.Background = null;
                listView.BorderBrush = null;
                listView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);

                listView.ContainerContentChanging += (s, args) =>
                {
                    if (args.ItemContainer is Microsoft.UI.Xaml.Controls.ListViewItem item)
                    {
                        item.Background = null;
                        item.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
                        item.FocusVisualPrimaryThickness = new Microsoft.UI.Xaml.Thickness(0);
                        item.FocusVisualSecondaryThickness = new Microsoft.UI.Xaml.Thickness(0);
                    }
                };                
            }

            MyViewModel.ScrollAfterAppearing();

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to remove highlight: {ex.Message}");
        }
#endif
    }

    private void LyricsColView_Unloaded(object sender, EventArgs e)
    {

    }

    private void SearchOnline_Clicked(object sender, EventArgs e)
    {
        var send = (ImageButton)sender;
        MyViewModel.CntxtMenuSearchCommand.Execute(send.CommandParameter);

    }
    Border LyrBorder { get; set; }
    private void LyrBorder_Loaded(object sender, EventArgs e)
    {
        var LoadedLyric = (Border)sender;
        LoadedLyric = LyrBorder;
    }
}
