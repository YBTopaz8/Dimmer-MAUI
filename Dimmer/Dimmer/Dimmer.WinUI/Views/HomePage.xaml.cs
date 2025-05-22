//using Dimmer.DimmerLive.Models;
using Dimmer.Data.Models;
using Dimmer.WinUI.Utils.Models;
using System.Diagnostics;
using System.Threading.Tasks;
using Application = Microsoft.Maui.Controls.Application;

namespace Dimmer.WinUI.Views;

public partial class HomePage : ContentPage
{
    public HomeViewModel MyViewModel { get; internal set; }
    public HomePage(HomeViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
        MyViewModel=vm;      
        
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        //await MyViewModel.LoginFromSecureData();

    }

    private void PasteLyrPlainLyr_Clicked(object sender, EventArgs e)
    {
        //if (MyViewModel.AllSyncLyrics.Count < 1)
        //{
        //    return;

        //}
        //LyricsEditor.Text = MyViewModel.AllSyncLyrics[0].PlainLyrics;
    }



    private void SearchOnline_Clicked(object sender, EventArgs e)
    {
        ImageButton send = (ImageButton)sender;
        //MyViewModel.CntxtMenuSearchCommand.Execute(send.CommandParameter);

    }
    Border LyrBorder { get; set; }
    private void LyrBorder_Loaded(object sender, EventArgs e)
    {
        Border LoadedLyric = (Border)sender;
        LoadedLyric = LyrBorder;
    }

    private void Stamp_Clicked(object sender, EventArgs e)
    {
        ImageButton send = (ImageButton)sender;
        //MyViewModel.CaptureTimestampCommand.Execute((LyricPhraseModel)send.CommandParameter);

    }

    private void DeleteLine_Clicked(object sender, EventArgs e)
    {
        ImageButton send = (ImageButton)sender;

        //MyViewModel.DeleteLyricLineCommand.Execute((LyricPhraseModel)send.CommandParameter);

    }

    private void SaveCapturedLyrics_Clicked(object sender, EventArgs e)
    {
        //MyViewModel.SaveLyricsToLrcAfterSyncingCommand.Execute(null);
    }

    private async void PasteLyricsFromClipBoardBtn_Clicked(object sender, EventArgs e)
    {
        await Task.WhenAll(ManualSyncLyricsView.AnimateFadeInFront(), LyricsEditor.AnimateFadeInFront(), OnlineLyricsResView.AnimateFadeOutBack());

        if (Clipboard.Default.HasText)
        {
            LyricsEditor.Text = await Clipboard.Default.GetTextAsync();
        }


    }

    bool IsSyncing = false;

    private void PasteLyrClipboard_Clicked(object sender, EventArgs e)
    {
        PasteLyricsFromClipBoardBtn_Clicked(sender, e);
    }
    private async void CancelAction_Clicked(object sender, EventArgs e)
    {
        await PlainLyricSection.DimmIn();
        PlainLyricSection.IsEnabled = true;

        //MyViewModel.PrepareLyricsSync(LyricsEditor.Text);

        await SyncLyrView.DimmOut();
        SyncLyrView.IsVisible=false;
    }
    private void SyncLyrLine_PointerEntered(object sender, PointerEventArgs e)
    {
        Border send = (Border)sender;
        send.Stroke = Microsoft.Maui.Graphics.Colors.DarkSlateBlue;
        send.StrokeThickness = 2;

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

    private async void CloseButton_Clicked(object sender, EventArgs e)
    {
        await LyricPrewiewUI.AnimateFadeOutBack();

    }

    private async void OkButton_Clicked(object sender, EventArgs e)
    {
        if (lyrType == "Synced Lyrics")
        {
            //await MyViewModel.SaveLyricToFile(SelectedContent, false);

        }
        else
        if (lyrType == "Plain Lyrics")
        {
            //await MyViewModel.SaveLyricToFile(SelectedContent, true);
        }
        await LyricPrewiewUI.AnimateFadeOutBack();
    }
    string lyrType = string.Empty;
    Content SelectedContent { get; set; }
    private void SyncLyrLine_PointerExited(object sender, PointerEventArgs e)
    {
        Border send = (Border)sender;
        send.Stroke = Microsoft.Maui.Graphics.Colors.Transparent;
        send.StrokeThickness = 0;
    }
    private async void ViewLyricsBtn_Clicked(object sender, EventArgs e)
    {
        LyricsEditor.Text = string.Empty;
        Button send = (Button)sender;
        lyrType = send.Text;
        SelectedContent = (Content)send.BindingContext;
        LyricsView.Text = SelectedContent.PlainLyrics is null ? SelectedContent.SyncedLyrics : SelectedContent.PlainLyrics;
        if (lyrType.Equals("Synced Lyrics"))
        {
            SynceOrPlainTitle.Text ="Synced Lyrics";
        }
        else
        {
            SynceOrPlainTitle.Text ="Plain Lyrics";
        }
        await LyricPrewiewUI.AnimateFadeInFront();
    }
    private async void StartSyncing_Clicked(object sender, EventArgs e)
    {
        await PlainLyricSection.DimmOut();
        PlainLyricSection.IsEnabled = false;
        //MyViewModel.PrepareLyricsSync(LyricsEditor.Text);
        //IsSyncing = true;

        await SyncLyrView.DimmIn();
        SyncLyrView.IsVisible=true;
    }

    private async void SearchLyricsOnLyrLib_Clicked(object sender, EventArgs e)
    {

        throw new NotImplementedException();
        //await MyViewModel.FetchLyrics(true);

    }
    private void PlaySong_Tapped(object sender, TappedEventArgs e)
    {

        View send = (View)sender;
        
        SongModelView? song = (SongModelView)send.BindingContext;
        song?.IsCurrentPlayingHighlight = false;

       MyViewModel.PlaySongOnDoubleTap(song!);
    }
    private void UserHoverOnSongInColView(object sender, PointerEventArgs e)
    {
        View send = (View)sender;

        MyViewModel.PointerEntered((SongModelView)send.BindingContext, send);
    }
    
    private static void UserHoverOutSongInColView(object sender, PointerEventArgs e)
    {
        View send = (View)sender;

       HomeViewModel.PointerExited(send);
    }

    private bool _isThrottling;
    private readonly int throttleDelay = 300; 

    private async void Slider_DragCompleted(object sender, EventArgs e)
    {
        if (_isThrottling)
            return;

        _isThrottling = true;
        Slider send = (Slider)sender;
        var s = send;
        MyViewModel.SeekTo( s.Value, true);
        MyViewModel.SeekTo( s.Value, true);


        await Task.Delay(throttleDelay);
        _isThrottling = false;
    }




    private void ToggleRepeat_Tapped(object sender, Microsoft.Maui.Controls.TappedEventArgs e)
    {
        MyViewModel.ToggleRepeatMode();
    }
  

    private void SfEffectsView_Loaded(object sender, EventArgs e)
    {

        var send = (SfEffectsView)sender;
        var mainLayout = (Microsoft.UI.Xaml.UIElement)send.Handler!.PlatformView!;
        mainLayout.PointerWheelChanged += MainLayout_PointerWheelChanged;

    }


    private void MainLayout_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(null);
        int mouseWheelDelta = pointerPoint.Properties.MouseWheelDelta;

        if (mouseWheelDelta != 0)
        {
            if (mouseWheelDelta > 0)
            {
                if (MyViewModel.VolumeLevel >=1)
                {
                    return;
                }
                MyViewModel.IncreaseVolume();
                
            }
            else
            {
                MyViewModel.DecreaseVolume();
                
            }
        }

        e.Handled = true;
    }



    private void SfEffectsView_Unloaded(object sender, EventArgs e)
    {

        var send = (SfEffectsView)sender;
        Microsoft.UI.Xaml.UIElement? mainLayout = (Microsoft.UI.Xaml.UIElement)send.Handler!.PlatformView!;
        mainLayout.PointerWheelChanged -= MainLayout_PointerWheelChanged;


    }

    private void PlayPrevious_Clicked(object sender, EventArgs e)
    {
        MyViewModel.PlayPrevious();
    }

    private void PlayNext_Clicked(object sender, EventArgs e)
    {
        MyViewModel.PlayNext(true);
    }

    private async void PlayPauseSong_Tapped(object sender, TappedEventArgs e)
    {
        await MyViewModel.PlayPauseAsync();
    }

    private void ShuffleBtn_Clicked(object sender, EventArgs e)
    {
        MyViewModel.ToggleRepeatMode();
    }

    private async void MediaChipBtn_ChipClicked(object sender, EventArgs e)
    {
        SfChip ee = (SfChip)sender;
        string? param = ee.CommandParameter.ToString();
        if (param is null)
        {
            return;
        }
        var CurrentIndex = int.Parse(param);
        switch (CurrentIndex)
        {
            case 0:
                MyViewModel.ToggleRepeatMode();
                break;
            case 1:
                MyViewModel.PlayPrevious();
                break;
            case 2:
            case 3:
                await MyViewModel.PlayPauseAsync();
                
                break;
            case 4:
                MyViewModel.PlayNext(true);
                break;
            case 5:
                MyViewModel.IsShuffle = !MyViewModel.IsShuffle;
                break;

            case 6:
                MyViewModel.IncreaseVolume();
                break;

            default:

                break;
        }
    }
    private async void TempSongChipGroup_ChipClicked(object sender, EventArgs e)
    {
 
        SfChip ee = (SfChip)sender;
        string? param = ee.CommandParameter.ToString();
        if (param is null)
        {
            return;
        }
        var CurrentIndex = int.Parse(param);
        
        switch (CurrentIndex)
        {
            case 0:

                if(currentViewIndex!=0)
                {
                    await SwitchUIs(0);
                }
                
                SongsColView.ScrollTo(MyViewModel.TemporarilyPickedSong, null, ScrollToPosition.Start,true);
                //show the now playing Queue
                break;
            case 1:
                await SwitchUIs(1);
                //show the synced lyrics

                break;
            case 2:
                MyViewModel.OpenAlbumPage(MyViewModel.SecondSelectedSong);
                PlatUtils.OpenAlbumWindow(MyViewModel.SecondSelectedSong);
                return;
            case 3:

                PlatUtils.OpenSettingsWindow();

                return;
            case 4:

                break;
            case 5:
                await SwitchUIs(2);
               MyViewModel.WindowsOpened?.Clear();

                foreach (var win in Application.Current!.Windows)
                {
                    if (win == Application.Current!.Windows[0])
                    {
                        continue;
                    }
                    // 1) get a pure Win32 snapshot
                    var thumbnail = win.CaptureWindow();

                    // 2) build your info object
                    var info = new WindowInfo(win, thumbnail);
                    MyViewModel.WindowsOpened!.Add(info);

                    Debug.WriteLine($"Captured: {info.Title} ({info.TypeName}) " +
                                    $"at [{info.X},{info.Y}] {info.Width}×{info.Height}");
                }

                ControlPanelColView.ItemsSource = MyViewModel.WindowsOpened;

                break;
            case 6:

                await MyViewModel.OpenArtistPage(MyViewModel.SecondSelectedSong);
                PlatUtils.OpenArtistWindow(MyViewModel.SecondSelectedSong);
                break;
            case 7:
                SongsColView.ScrollTo(MyViewModel.SelectedSong, null, ScrollToPosition.Start, true);

                //MyViewModel.ShareSongOnline();
                break;
            case 8:

                
                DimmerSongWindow newWin = new DimmerSongWindow(MyViewModel);

                var dq = DispatcherQueue.GetForCurrentThread();
                dq.TryEnqueue(() =>
                {
                    Application.Current!.OpenWindow(newWin);
                });
                break;
            default:
                break;
        }
        await SwitchUIs(CurrentIndex);
    }
    int currentViewIndex = 0;
    private async Task SwitchUIs(int CurrentIndex)
    {
        if (currentViewIndex == CurrentIndex)
            return;
        currentViewIndex=CurrentIndex;
        Dictionary<int, View> viewss = new Dictionary<int, View>
        {
            {0, SongsColView},
            {1, LyricsColView},
            {2, ControlPanel},
            //{3, SettingsPanel},
            {4, UserNoteView},


        };
        if (!viewss.ContainsKey(CurrentIndex))
            return;

        await Task.WhenAll
            (viewss.Select(kvp =>
            kvp.Key == CurrentIndex
            ? kvp.Value.AnimateFadeInFront()
            : kvp.Value.AnimateFadeOutBack()));
        
    }


    private CancellationTokenSource? _debounceTimer;
    private bool isOnFocusMode;

    private async void SearchSongSB_TextChanged(object sender, TextChangedEventArgs e)
    {

        SearchBar searchBar = (SearchBar)sender;
        string txt = searchBar.Text;

        
        _debounceTimer?.CancelAsync();
        _debounceTimer?.Dispose();
        _debounceTimer = new CancellationTokenSource();
        CancellationToken token = _debounceTimer.Token;
        int delayMilliseconds = 600;
        

        try
        {
            await Task.Delay(delayMilliseconds, token);

            if (token.IsCancellationRequested)
                return; 
            await SearchSongsAsync(txt, token);
             
        }
        catch (OperationCanceledException ex) 
        {
            Debug.WriteLine("Search operation cancelled." +ex.Message);
        }
        catch (Exception ex) 
        {
            Debug.WriteLine($"Search Error: {ex}"); 
        }
    }

    private async Task SearchSongsAsync(string? searchText, CancellationToken token)
    {
        if ((MyViewModel.PlaylistSongs is null || MyViewModel.PlaylistSongs.Count < 1)&&(BaseAppFlow.MasterList is null ||  MyViewModel.PlaylistSongs?.Count<1))
        {
            return;
        }
        
        List<SongModelView> songsToDisplay=new();

        if (string.IsNullOrEmpty(searchText))
        {

            songsToDisplay = MyViewModel.PlaylistSongs.ToList();
            BaseViewModel.IsSearching = false;
        }
        else
        {
            BaseViewModel.IsSearching = true;

            songsToDisplay= await Task.Run(() => 
            {
                token.ThrowIfCancellationRequested(); 

                
                var e = MyViewModel.PlaylistSongs!    .
                            Where(item => (!string.IsNullOrEmpty(item.Title) && item.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                                  (!string.IsNullOrEmpty(item.ArtistName) && item.ArtistName.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                                  (!string.IsNullOrEmpty(item.AlbumName) && item.AlbumName.Contains(searchText, StringComparison.OrdinalIgnoreCase)))
                   .ToList();
                
                return e;
            }, token);

             
        }

        
        Dispatcher.Dispatch(() =>
        {
            if (token.IsCancellationRequested)
                return;

            
                MyViewModel.FilteredSongs = songsToDisplay;
            SongsColView.ItemsSource = MyViewModel.FilteredSongs;

            MyViewModel.NowPlayingQueue = songsToDisplay.ToObservableCollection();

        });
    }

    private static void Button_Clicked(object sender, EventArgs e)
    {
        PlatUtils.ApplicationProps.LaunchSecondWindow();  
    }
    private static void Button2_Clicked(object sender, EventArgs e)
    {
        PlatUtils.ApplicationProps.LaunchSecondWindow();  
    }

    private void CurrentPositionSlider_DragCompleted(object sender, EventArgs e)
    {  
        var send = (Slider)sender;
        if (MyViewModel.IsPlaying)
        {
            MyViewModel.SeekTo(send.Value,true);
        }

    }

    private void VolumeSlider_ValueChanged(object sender, ValueChangedEventArgs e)
    {
    }


    private async void BtmBarPointerGest_PointerEntered(object sender, PointerEventArgs e)
    {
        await BmtBarView.AnimateFocusModePointerEnter();

    }

    private async void BtmBarPointerGest_PointerExited(object sender, Syncfusion.Maui.Toolkit.Internals.PointerEventArgs e)
    {
        await BmtBarView.AnimateFocusModePointerExited(endOpacity: 0.4, endScale: 1);

    }

    private static async void PrimarySectionGest_Tapped(object sender, TappedEventArgs e)
    {
       await Shell.Current.GoToAsync(nameof(SingleSongPage));
    }
    private void SeekSongPosFromLyric_Tapped(object sender, TappedEventArgs e)
    {
        if (MyViewModel.IsPlaying)
        {
            View bor = (View)sender;
            LyricPhraseModelView lyr = (LyricPhraseModelView)bor.BindingContext;
            MyViewModel.SeekSongPosition(lyr);
        }
    }

    private void LyricsColView_Loaded(object sender, EventArgs e)
    {
        try
        {
            MyViewModel.SetSongLyricsView(LyricsColView);
            //MyViewModel.
            var nativeView = LyricsColView.Handler?.PlatformView;

            if (nativeView is Microsoft.UI.Xaml.Controls.ListView listView)
            {
                
                listView.SelectionMode = Microsoft.UI.Xaml.Controls.ListViewSelectionMode.None;

                listView.Background = null;
                listView.BorderBrush = null;
                listView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);

                listView.ContainerContentChanging += ListView_ContainerContentChanging;
                
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

            Debug.WriteLine($"PlatformView Type: {nativeView?.GetType()}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to remove highlight: {ex.Message}");
        }

    }

    private static void ListView_ContainerContentChanging(Microsoft.UI.Xaml.Controls.ListViewBase sender, Microsoft.UI.Xaml.Controls.ContainerContentChangingEventArgs args)
    {
        if (args.ItemContainer is Microsoft.UI.Xaml.Controls.ListViewItem item)
        {
            item.Background = null;
            item.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
            item.FocusVisualPrimaryThickness = new Microsoft.UI.Xaml.Thickness(0);
            item.FocusVisualSecondaryThickness = new Microsoft.UI.Xaml.Thickness(0);
        }
    }

    private void LyricsColView_Unloaded(object sender, EventArgs e)
    {

        try
        {
            var nativeView = LyricsColView.Handler?.PlatformView;

            if (nativeView is Microsoft.UI.Xaml.Controls.ListView listView)
            {
                listView.ContainerContentChanging -= ListView_ContainerContentChanging;
            }


        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to remove highlight: {ex.Message}");
        }
    }
    private void LyricsColView_SelectionChanged(object sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    {
        //if (LyricsColView.SelectedItem is not LyricPhraseModelView CurrLyric || MyViewModel is null)
            


        //if (MyViewModel.IsPlaying)
        //{
        //    if (string.IsNullOrEmpty(CurrLyric.Text))
        //    {
        //        await Task.WhenAll(BmtBarView.DimmOutCompletely(),
        //            MainGrid.DimmOutCompletely(), PageBGImg.DimmInCompletely());

        //    }
        //    else
        //    {
        //        await Task.WhenAll(BmtBarView.DimmInCompletely(),
        //            MainGrid.DimmInCompletely(), PageBGImg.DimmInCompletely(),
        //            PageBGImg.DimmOut(endOpacity: 0.15));

        //    }
        //}
    }

    private void SongsUIContextMenu_Clicked(object sender, EventArgs e)
    {

    }

    private void ScrollToSongIcon_Clicked(object sender, EventArgs e)
    {
        MyViewModel.ScrollToCurrentlyPlayingSong();
    }

    private void VolumeSlider_DragCompleted(object sender, EventArgs e)
    {
        var send = (Slider)sender;
        MyViewModel.SetVolume(send.Value);
    }

    private void SongRow_TouchDown(object sender, EventArgs e)
    {
        var view = (SfEffectsView)sender;
        var song = (SongModelView)view.BindingContext;
        if (song is not null)
        {
            MyViewModel.SetCurrentlyPickedSong(song);
        }
    }

    private void ActualSongView_ChipClicked(object sender, EventArgs e)
    {

        SfChip ee = (SfChip)sender;
        string? param = ee.CommandParameter.ToString();
        if (param is null)
        {
            return;
        }
        var CurrentIndex = int.Parse(param);
        switch (CurrentIndex)
        {
            case 0:

                break;
            case 1:

                break;
            case 2:
                PlatUtils.OpenAlbumWindow(MyViewModel.TemporarilyPickedSong!);
                
                break;

            default:
                break;
        }
    }

    private async void SongImage_Clicked(object sender, EventArgs e)
    {
        await SwitchUIs(0);

        SongsColView.ScrollTo(MyViewModel.TemporarilyPickedSong, null, ScrollToPosition.Start,true);
    }

    private void CloseSubWindowChip_Clicked(object sender, EventArgs e)
    {
        var send = (SfChip)sender;
        var item = (WindowInfo)send.BindingContext;
        Application.Current!.CloseWindow(item.WindowInstance);
        MyViewModel.WindowsOpened.Remove(item);
    }

    private static void FocusChip_Clicked(object sender, EventArgs e)
    {

        var send = (SfChip)sender;
        var item = (WindowInfo)send.BindingContext;
       Task.Run(()=>  Application.Current!.ActivateWindow(item.WindowInstance));
    }

    private void CloseAllWin_Clicked(object sender, EventArgs e)
    {
        foreach (var win in MyViewModel.WindowsOpened)
        {
            Application.Current!.CloseWindow(win.WindowInstance);
        }
        MyViewModel.WindowsOpened.Clear();
    }

  

    private void AddAttachmentBtn_Clicked(object sender, EventArgs e)
    {
        if (ThoughtBtmSheetBottomSheet.IsVisible )
        {
            ThoughtBtmSheetBottomSheet.IsEnabled = false;
            ThoughtBtmSheetBottomSheet.IsVisible = false;
            return;
        }

        ThoughtBtmSheetBottomSheet.IsVisible = true;
        ThoughtBtmSheetBottomSheet.IsEnabled = true;

    }
    private async void SaveNoteBtn_Clicked(object sender, EventArgs e)
    {
        UserNoteModelView note = new()
        {
            UserMessageText=NoteText.Text,

        };
        await MyViewModel.SaveUserNoteToDB(note, MyViewModel.SecondSelectedSong);
    }

    private void DltFolder_Clicked(object sender, EventArgs e)
    {
        var send = (Button)sender;
        var item = (string)send.BindingContext;

        MyViewModel.DeleteFolderPath(item);
    }

    private void DeleteBtn_Clicked(object sender, EventArgs e)
    {

    }

    private void ChangeFolder_Clicked(object sender, EventArgs e)
    {

    }

    private void AddNewMusicFolder_Clicked(object sender, EventArgs e)
    {

    }

    private void MainAppGrid_Loaded(object sender, EventArgs e)
    {
        
    }

    private void FreshAppStartRecogn_PointerEntered(object sender, PointerEventArgs e)
    {
     
    }

    private void GetStartedChip_Clicked(object sender, EventArgs e)
    {
        MyViewModel.InitializeWindows();
        //{
        //    MyViewModel.CurrentlySelectedPage = CurrentPage.HomePage;

        //    MyViewModel.SetCollectionView(SongsColView);
        //    MyViewModel.SetSongLyricsView(LyricsColView);
        //    await MyViewModel.LoginFromSecureData();
        //}

    }

    private void SongsColView_SizeChanged(object sender, EventArgs e)
    {

    }

    private void LogMsgChip_Clicked(object sender, EventArgs e)
    {
        Debug.WriteLine(LogMsgChip.CommandParameter.GetType());
    }
    string currParam=string.Empty;
    private void ShortingTitle_TouchUp(object sender, EventArgs e)
    {
        var send = (SfEffectsView)sender;
        var param = send.TouchUpCommandParameter.ToString();
        ObservableCollection<SongModelView> currSongs = (SongsColView.ItemsSource as ObservableCollection<SongModelView>)!;
        Debug.WriteLine(SongsColView.ItemsSource.GetType());
        bool isSameParam = param == currParam;
        switch (param)
        {
            case "title":
                if (isSameParam)
                {
                    SongsColView.ItemsSource = currSongs.OrderByDescending(x => x.Title).ToList();
                }
                else
                {
                    SongsColView.ItemsSource = currSongs.OrderBy(x => x.Title).ToList();
                }
                    break;
            case "artist":
                if (isSameParam)
                {
                    SongsColView.ItemsSource = currSongs.OrderByDescending(x => x.ArtistName).ToList();
                }
                else
                {
                    SongsColView.ItemsSource = currSongs.OrderBy(x => x.ArtistName).ToList();
                }
                break;
            case "album":
                if (isSameParam)
                {
                    SongsColView.ItemsSource = currSongs.OrderByDescending(x => x.Album?.Name).ToList();
                }
                else
                {
                    SongsColView.ItemsSource = currSongs.OrderBy(x => x.Album?.Name).ToList();
                }
                break;
            case "genre":
                if (isSameParam)
                {
                    SongsColView.ItemsSource = currSongs.OrderByDescending(x => x.Genre?.Name).ToList();
                }
                else
                {
                    
                    SongsColView.ItemsSource = currSongs.OrderBy(x => x.Genre?.Name).ToList();
                }
                break;

            case "duration":
                if (isSameParam)
                {
                    SongsColView.ItemsSource = currSongs.OrderByDescending(x => x.DurationInSeconds).ToList();
                }
                else
                {
                    SongsColView.ItemsSource = currSongs.OrderBy(x => x.DurationInSeconds).ToList();
                }
                break;
            default:

                break;
        }
    }


    private void ShortingTitle_LongPressed(object sender, EventArgs e)
    {
        var send = (SfEffectsView)sender;
        var param = send.TouchUpCommandParameter.ToString();
        ObservableCollection<SongModelView> currSongs = (SongsColView.ItemsSource as ObservableCollection<SongModelView>)!;
        var listOfArtists = MyViewModel.TemporarilyPickedSong.ArtistIds.Select(x => x.Name).ToList(); ;
        //var res = await DisplayActionSheet("Choose", "Choooose", "OK", "ss");
    }
}