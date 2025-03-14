using CommunityToolkit.Maui.Extensions;
using DevExpress.Maui.CollectionView;
using DevExpress.Maui.Core;
using System.Threading.Tasks;
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


        await MyViewModel.ConnectToLiveQueriesAsync();
        await MyViewModel.SetChatRoom(ChatRoomOptions.PersonalRoom);
    }
    private void ToggleRepeat_Clicked(object sender, EventArgs e)
    {
        MyViewModel.ToggleRepeatModeCommand.Execute(true);
    }
    private void ProgressSlider_TapReleased(object sender, DXTapEventArgs e)
    {
        MyViewModel.SeekSongPosition(currPosPer: ProgressSlider.Value);
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
            BtmBar.MyBackgroundColorTo(Color.FromArgb("#483D8B"), length: 500));
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
        SongsMenuPopup.WidthRequest = this.Width;
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
        await ToggleSearchPanel();
    }

    private async Task ToggleSearchPanel()
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
                            var t1= send.MyBackgroundColorTo(Colors.MediumPurple, length: 300); 
                            var t2=  Task.Delay(500);
                            var t3 = send.MyBackgroundColorTo(Colors.DarkSlateBlue, length: 300); 
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
                            if (HomeTabView.SelectedItemIndex != 0)
                            {
                                HomeTabView.SelectedItemIndex=0;
                            }
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
                                await MyViewModel.AssignSyncLyricsCV(LyricsColView);
                            }
                            else
                            {
                                MyViewModel.LoadArtistSongs();
                                ContextBtmSheet.State = BottomSheetState.HalfExpanded;
                                ContextBtmSheet.HalfExpandedRatio = 0.8;
                                await NowPlayingQueueView.DimmOutCompletely();
                                NowPlayingQueueView.IsVisible=false;
                                await ArtistSongsView.DimmInCompletely();
                                ArtistSongsView.IsVisible=true;
                                //HomeTabView.SelectedItemIndex = prevViewIndex;
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
        await element.MyBackgroundColorTo(color, length: 300);
        await Task.Delay(300); // Reduce freeze by using a lower delay
        await element.MyBackgroundColorTo(Colors.DarkSlateBlue, length: 300);
    }
    private void ViewNowPlayPage_Tap(object sender, HandledEventArgs e)
    {
        MyViewModel.UpdateContextMenuData(MyViewModel.MySelectedSong);
        ContextBtmSheet.State = BottomSheetState.HalfExpanded;
    }

    //mini bar tap play/pause

    private async void BtmBarTapGest_Tapped(object sender, TappedEventArgs e)
    {
        var send = (DXBorder)sender;
        
        if (MyViewModel.IsPlaying)
        {
            
            MyViewModel.PauseSong();
            RunFocusModeAnimation(send, Color.FromArgb("#8B0000")); // DarkRed for pause
            
            await send.MyBackgroundColorTo(Color.FromArgb("#252526"), length: 300);
        }
        else
        {
            await send.MyBackgroundColorTo(Color.FromArgb("#483D8B"), length: 300);
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

    private void LyricsColView_SelectionChanged(object sender, CollectionViewSelectionChangedEventArgs e)
    {
        var CurrLyric = LyricsColView.SelectedItem as LyricPhraseModel;
        
        if (!this.IsLoaded)
        {
            return;
        }
        if (CurrLyric is null)
            return;

        try
        {
            
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

    private async void SearchBy_ClearIconClicked(object sender, HandledEventArgs e)
    {
        SongsColView.RefreshData();
        await ToggleSearchPanel();
    }

    private void SearchFiltersChips_ChipTap(object sender, ChipEventArgs e)
    {
        var send = (ChoiceChipGroup)sender;
        var chip = (Chip)send.SelectedItem;
        SearchParam = chip.TapCommandParameter.ToString()!;
    }

    private void PlayFromNPList_Tapped(object sender, TappedEventArgs e)
    {

    }

    private void LyricsColView_Loaded(object sender, EventArgs e)
    {

    }

    private void LyricsColView_Unloaded(object sender, EventArgs e)
    {

    }

    private async void ShowArtistSongsAndAlbums_Tap(object sender, HandledEventArgs e)
    {
        MyViewModel.LoadArtistSongs();
        ContextBtmSheet.State = BottomSheetState.HalfExpanded;
        ContextBtmSheet.HalfExpandedRatio = 0.8;
        await NowPlayingQueueView.DimmOutCompletely();
        NowPlayingQueueView.IsVisible=false;    
        await ArtistSongsView.DimmInCompletely();
        ArtistSongsView.IsVisible=true;
    }

    private void ShowSongInAlbum_Tap(object sender, HandledEventArgs e)
    {
        
    }

    

    private void ShowArtistAlbums_Tapped(object sender, EventArgs e)
    {
        var send = (DXButton)sender;
        var curSel = send.BindingContext as AlbumModelView;
        send.BackgroundColor = Microsoft.Maui.Graphics.Colors.DarkSlateBlue;
        send.PressedBackgroundColor = Microsoft.Maui.Graphics.Colors.DarkSlateBlue;
        MyViewModel.GetAllSongsFromAlbumID(curSel!.LocalDeviceId);
    }

    private void ResetSongs_TapPressed(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
        MyViewModel.LoadArtistAlbumsAndSongs(MyViewModel.SelectedArtistOnArtistPage);
    }

    private void SingleSongBtn_Clicked(object sender, EventArgs e)
    {
        MyViewModel.CurrentQueue = 1;
        var s = (View)sender;
        var song = s.BindingContext as SongModelView;
        MyViewModel.PlaySong(song);

    }

    private void Chip_Tap(object sender, HandledEventArgs e)
    {
        var send = (Chip)sender;
        var param = send.TapCommandParameter.ToString();
        MyViewModel.ToggleRepeatModeCommand.Execute(true);
        switch (param)
        {
            case "repeat":

                
                break;
            case "shuffle":
                MyViewModel.CurrentQueue = 1;
                break;
            case "Lyrics":
                MyViewModel.CurrentQueue = 2;
                break;
            default:
                break;
        }

    }

    private void LyricsColView_Tap(object sender, CollectionViewGestureEventArgs e)
    {
        Debug.WriteLine(e.Item.GetType());
        if (MyViewModel.IsPlaying)
        {
            var lyr = (LyricPhraseModel)e.Item;
            MyViewModel.SeekSongPosition(lyr);
        }
    }

    private void HomeTabView_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        
    }

    private async void ContextIcon_Tap(object sender, HandledEventArgs e)
    {
        MyViewModel.LoadArtistSongs();
        ContextBtmSheet.State = BottomSheetState.HalfExpanded;
        ContextBtmSheet.HalfExpandedRatio = 0.8;
        await NowPlayingQueueView.DimmOutCompletely();
        NowPlayingQueueView.IsVisible=false;
        await ArtistSongsView.DimmInCompletely();
        ArtistSongsView.IsVisible=true;
    }
    private void SearchOnline_Clicked(object sender, EventArgs e)
    {
        var send = (ImageButton)sender;
        MyViewModel.CntxtMenuSearchCommand.Execute(send.CommandParameter);

    }
    Border LyrBorder { get; set; }
  

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
    private void Chip_Tap_1(object sender, HandledEventArgs e)
    {
        MyViewModel.ToggleShuffleStateCommand.Execute(true);
    }

    private async void StartSyncing_Clicked(object sender, EventArgs e)
    {
        await PlainLyricSection.DimmOut();
        PlainLyricSection.IsEnabled = false;
        MyViewModel.PrepareLyricsSync(LyricsEditor.Text);
        IsSyncing = true;

        await SyncLyrView.DimmIn();
        SyncLyrView.IsVisible=true;
    }

    bool IsSyncing = false;
    private async void CancelAction_Clicked(object sender, EventArgs e)
    {
        await PlainLyricSection.DimmIn();
        PlainLyricSection.IsEnabled = true;

        //MyViewModel.PrepareLyricsSync(LyricsEditor.Text);
        IsSyncing = false;

        await SyncLyrView.DimmOut();
        SyncLyrView.IsVisible=false;
    }
    private async void SearchLyricsOnLyrLib_Clicked(object sender, EventArgs e)
    {

        await Task.WhenAll(ManualSyncLyricsView.AnimateFadeOutBack(), LyricsEditor.AnimateFadeOutBack(), OnlineLyricsResView.AnimateFadeInFront());

        await MyViewModel.FetchLyrics(true);

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
    private async void PasteLyricsFromClipBoardBtn_Clicked(object sender, EventArgs e)
    {
        await Task.WhenAll(ManualSyncLyricsView.AnimateFadeInFront(), LyricsEditor.AnimateFadeInFront(), OnlineLyricsResView.AnimateFadeOutBack());

        if (Clipboard.Default.HasText)
        {
            LyricsEditor.Text = await Clipboard.Default.GetTextAsync();
        }


    }

    private void CurrQueueColView_Tap(object sender, CollectionViewGestureEventArgs e)
    {
        MyViewModel.CurrentQueue = 1;
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

    }

    private void DXCollectionView_Tap(object sender, CollectionViewGestureEventArgs e)
    {
        var send = (View)sender;

        var curSel = send.BindingContext as AlbumModelView;
        MyViewModel.AllArtistsAlbumSongs=MyViewModel.GetAllSongsFromAlbumID(curSel!.LocalDeviceId);
    }
}
