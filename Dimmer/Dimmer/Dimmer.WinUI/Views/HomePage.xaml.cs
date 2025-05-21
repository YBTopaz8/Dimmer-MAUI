//using Dimmer.DimmerLive.Models;
using Dimmer.WinUI.Utils.Models;
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

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        //await MyViewModel.LoginFromSecureData();

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
                await SwitchUIs(0);
                SongsColView.ScrollTo(MyViewModel.TemporarilyPickedSong);
                //show the now playing Queue
                break;
            case 1:
                await SwitchUIs(1);
                //show the synced lyrics

                break;
            case 2:
                //show the albums songs
                PlatUtils.OpenAlbumWindow(MyViewModel.SecondSelectedSong);

                break;
            case 3:

                PlatUtils.OpenSettingsWindow();

                return;
                break;
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
                await SwitchUIs(4);
                break;
            case 7:
                MyViewModel.ShareSongOnline();
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

    private async Task SwitchUIs(int CurrentIndex)
    {
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

    private async void MainAppGrid_Loaded(object sender, EventArgs e)
    {
        
    }

    private async void FreshAppStartRecogn_PointerEntered(object sender, PointerEventArgs e)
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
}