#if WINDOWS
using Microsoft.UI.Xaml;

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
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (MyViewModel.MySelectedSong is null)
        {
            return;
        }
        MyViewModel.CurrentPage = PageEnum.NowPlayingPage;
        MyViewModel.CurrentPageMainLayout = MainDock;
        await MyViewModel.AssignSyncLyricsCV(LyricsColView);
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
  
    private async void LyricsColView_SelectionChanged(object sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    {
        var CurrLyric = LyricsColView.SelectedItem as LyricPhraseModel;
        if (CurrLyric is null)
            return;
        if (MyViewModel.IsPlaying)
        {
            if (string.IsNullOrEmpty(CurrLyric.Text))
            {
                await Task.WhenAll(SearchLyricsGrid.DimmOutCompletely(),
                    PlainLyricsGrid.DimmOutCompletely(), SyncedLyricGrid.DimmOutCompletely(),
                    LeftPane.DimmOutCompletely(),MediaPlayBackCW.DimmOutCompletely(), 
                    StatsTabs.DimmOutCompletely(),PageBGImg.DimmInCompletely(), 
                    MainDock.DimmOut());

                return;
            }
            else
            {
                await Task.WhenAll(SearchLyricsGrid.DimmInCompletely(),
                    PlainLyricsGrid.DimmInCompletely(), SyncedLyricGrid.DimmInCompletely(),
                    LeftPane.DimmInCompletely(),
                    MediaPlayBackCW.DimmInCompletely(), StatsTabs.DimmInCompletely(),
                    PageBGImg.DimmOut(endOpacity:0.15),
                    MainDock.DimmInCompletely());
                
            }
        }
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
        LyricsEditor.Text = string.Empty;
        var send = (Button)sender;
        var title = send.Text;
        var thisContent = (Content)send.BindingContext;
        if (title == "Synced Lyrics")
        {
            await MyViewModel.ShowSingleLyricsPreviewPopup(thisContent!, false);
        }
        else
        if (title == "Plain Lyrics")
        {
            LyricsEditor.Text = thisContent!.PlainLyrics;
            PasteLyricsFromClipBoardBtn_Clicked(send, e);            
        }
    }

    private void ShowHideChart_CheckChanged(object sender, EventArgs e)
    {
        
    }

    private async void SearchLyricsOnLyrLib_Clicked(object sender, EventArgs e)
    {
        
        await Task.WhenAll(ManualSyncLyricsView.AnimateFadeOutBack(), LyricsEditor.AnimateFadeOutBack(), OnlineLyricsResView.AnimateFadeInFront());

        await MyViewModel.FetchLyrics(true);

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
                
                await Task.WhenAll(ManualSyncLyricsView.AnimateFadeOutBack(), LyricsEditor.AnimateFadeOutBack(), OnlineLyricsResView.AnimateFadeInFront());
                
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
    private async void DropGestureRecognizer_DragOver(object sender, Microsoft.Maui.Controls.DragEventArgs e)
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

                            if (fileExtension == ".png" || fileExtension == ".jpg" ||
                                fileExtension == ".jpeg" || fileExtension == ".webp" )
                            {
                                if (MyViewModel.MySelectedSong is not null)
                                {
                                    MyViewModel.MySelectedSong.CoverImagePath = file.Path;
                                    MyViewModel.SongsMgtService.UpdateSongDetails(MyViewModel.MySelectedSong);
                                    if (MyViewModel.MySelectedSong == MyViewModel.TemporarilyPickedSong )
                                    {
                                        MyViewModel.TemporarilyPickedSong.CoverImagePath = file.Path;
                                    }
                                }
                            } 
                            else
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

    private void DropGestureRecognizer_DragLeave(object sender, Microsoft.Maui.Controls.DragEventArgs e)
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

            if (nativeView is Microsoft.UI.Xaml.Controls.ListView listView)
            {

                listView.SelectionMode = Microsoft.UI.Xaml.Controls.ListViewSelectionMode.None;

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

            MyViewModel.ScrollAfterAppearing();
            Debug.WriteLine($"PlatformView Type: {nativeView?.GetType()}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to remove highlight: {ex.Message}");
        }
#endif
    }

    
    private void LyricsColView_Unloaded(object sender, EventArgs e)
    {

#if WINDOWS
        try
        {
            var nativeView = LyricsColView.Handler?.PlatformView;

            if (nativeView is Microsoft.UI.Xaml.Controls.ListView listView)
            {
                listView.ContainerContentChanging -= (s, args) =>
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

            
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to remove highlight: {ex.Message}");
        }
#endif
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

    private void Stamp_Clicked(object sender, EventArgs e)
    {
        var send = (ImageButton)sender;
        MyViewModel.CaptureTimestampCommand.Execute((LyricPhraseModel)send.CommandParameter);
        
    }

    private void DeleteLine_Clicked(object sender, EventArgs e)
    {
        var send = (ImageButton)sender;
        
        MyViewModel.DeleteLyricLineCommand.Execute((LyricPhraseModel)send.CommandParameter);

    }

    private void SaveCapturedLyrics_Clicked(object sender, EventArgs e)
    {
        MyViewModel.SaveLyricsToLrcAfterSyncingCommand.Execute(null);
    }

    private async void PasteLyricsFromClipBoardBtn_Clicked(object sender, EventArgs e)
    {
        await Task.WhenAll(ManualSyncLyricsView.AnimateFadeInFront(), LyricsEditor.AnimateFadeInFront(), OnlineLyricsResView.AnimateFadeOutBack());

        //if (Clipboard.Default.HasText)
        //{
        //    LyricsEditor.Text = await Clipboard.Default.GetTextAsync();
        //}

        
    }

    bool IsSyncing = false;

    private async void StartSyncing_Clicked(object sender, EventArgs e)
    {
        await PlainLyricSection.DimmOut();
        PlainLyricSection.IsEnabled = false;
        MyViewModel.PrepareLyricsSync(LyricsEditor.Text);
        IsSyncing = true;

        await SyncLyrView.DimmIn();
        SyncLyrView.IsVisible=true;
    }

    private async void PointerGestureRecognizer_PointerEntered1(object sender, PointerEventArgs e)
    {
        await Task.WhenAll(SearchLyricsGrid.DimmInCompletely(),
            PlainLyricsGrid.DimmInCompletely(), SyncedLyricGrid.DimmInCompletely(),
            LeftPane.DimmInCompletely(),
            MediaPlayBackCW.DimmInCompletely(), StatsTabs.DimmInCompletely(),
            PageBGImg.DimmOutCompletely(),
            MainDock.DimmInCompletely());

    }

#if WINDOWS
    private DispatcherTimer _pointerExitTimer;
#endif
    private void PointerGestureRecognizer_PointerExited1(object sender, PointerEventArgs e)
    {
        if(MyViewModel.MySelectedSong is null)
        {
            return;
        }
        if(MyViewModel.MySelectedSong.SyncLyrics is not null && MyViewModel.MySelectedSong.SyncLyrics.Count > 1)
        {
            return;
        }
#if WINDOWS
        // Cancel any existing timer
        _pointerExitTimer?.Stop();

        _pointerExitTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _pointerExitTimer.Tick += async (s, args) =>
        {
            _pointerExitTimer.Stop(); // Stop the timer after it fires once

            if (MyViewModel.MySelectedSong is null)
            {
                return;
            }
            if (MyViewModel.MySelectedSong.SyncLyrics is null || MyViewModel.MySelectedSong.SyncLyrics.Count < 1)
            {
                return;
            }

            await Task.WhenAll(
                SearchLyricsGrid.DimmOutCompletely(),
                PlainLyricsGrid.DimmOutCompletely(),
                SyncedLyricGrid.DimmOutCompletely(),
                LeftPane.DimmOutCompletely(),
                MediaPlayBackCW.DimmOutCompletely(),
                StatsTabs.DimmOutCompletely(),
                PageBGImg.DimmInCompletely(),
                MainDock.DimmOut()
            );
        };
        _pointerExitTimer.Start();
#endif
    }

    private async void CancelAction_Clicked(object sender, EventArgs e)
    {
        await PlainLyricSection.DimmIn();
        PlainLyricSection.IsEnabled = true;
        
        //MyViewModel.PrepareLyricsSync(LyricsEditor.Text);
        IsSyncing = false;
        
        await SyncLyrView.DimmOut();
        SyncLyrView.IsVisible=false;
    }
}
