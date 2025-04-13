

using System.Threading.Tasks;

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
        MyViewModel.CurrentlySelectedPage = Utilities.Enums.CurrentPage.HomePage;
        
        MyViewModel.SetCollectionView(SongsColView);
    }
    private async void PlaySong_Tapped(object sender, TappedEventArgs e)
    {

        View send = (View)sender;
        SongModelView? song = (SongModelView)send.BindingContext;
        if (song is not null)
        {
            song.IsCurrentPlayingHighlight = false;
        }

       await MyViewModel.PlaySongOnDoubleTap(song!);
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
        await MyViewModel.SeekSongPosition(currPosPer: s.Value);


        await Task.Delay(throttleDelay);
        _isThrottling = false;
    }




    private void ToggleRepeat_Tapped(object sender, Microsoft.Maui.Controls.TappedEventArgs e)
    {
        MyViewModel.ToggleRepeatMode();
    }
    private async void ShowCntxtMenuBtn_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SingleSongPage));
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
                MyViewModel.IncredeVolume();
                
            }
            else
            {
                MyViewModel.DecredeVolume();
                
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

    private async void PlayPrevious_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.PlayPrevious();
    }

    private async void PlayNext_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.PlayNext();
    }

    private async void PlayPauseSong_Tapped(object sender, TappedEventArgs e)
    {
        await MyViewModel.PlayPauseSong();
    }

    private void ShuffleBtn_Clicked(object sender, EventArgs e)
    {
        MyViewModel.ToggleRepeatMode();
    }

    private async void MediaChipBtn_ChipClicked(object sender, EventArgs e)
    {
        SfChip ee = (Syncfusion.Maui.Toolkit.Chips.SfChip)sender;
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
                await MyViewModel.PlayPrevious();
                break;
            case 2:
            case 3:
                await MyViewModel.PlayPauseSong();
                
                break;
            case 4:
                await MyViewModel.PlayNext();
                break;
            case 5:
                MyViewModel.IsShuffle = !MyViewModel.IsShuffle;
                break;

            case 6:
                MyViewModel.IncredeVolume();
                break;

            default:
                break;
        }
    }
    private async void TempSongChipGroup_ChipClicked(object sender, EventArgs e)
    {
        SfChip ee = (Syncfusion.Maui.Toolkit.Chips.SfChip)sender;
        string? param = ee.CommandParameter.ToString();
        if (param is null)
        {
            return;
        }
        var CurrentIndex = int.Parse(param);
        switch (CurrentIndex)
        {
            case 0:
                //show the now playing Queue
                break;
            case 1:
                //show the artists songs
                break;
            case 2:
                //show the albums songs
                break;
            case 3:

                
                break;
            case 4:
                await MyViewModel.PlayNext();
                break;
            case 5:
                MyViewModel.IsShuffle = !MyViewModel.IsShuffle;
                break;

            case 6:
                MyViewModel.IncredeVolume();
                break;

            default:
                break;
        }
    }


    private CancellationTokenSource? _debounceTimer;
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
        
        if (MyViewModel.MasterSongs is null || MyViewModel.MasterSongs.Count<1)
        {
            return; 
        }

        List<SongModelView> songsToDisplay;
        bool wasSearch = false; 

        if (string.IsNullOrEmpty(searchText))
        {
           
            songsToDisplay = MyViewModel.MasterSongs.ToList(); 
            wasSearch = false;
        }
        else
        {
            wasSearch = true;

            songsToDisplay= await Task.Run(() => 
            {
                token.ThrowIfCancellationRequested(); 

                
                var e= MyViewModel.MasterSongs.
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

            MyViewModel.CurrentQueue = wasSearch ? 1 : 0;
            

            MyViewModel.DisplayedSongs?.Clear();
            MyViewModel.SongsCV!.ItemsSource = songsToDisplay.ToObservableCollection();
            //foreach (SongModelView song in songsToDisplay)
            //{
            //    MyViewModel.DisplayedSongs.Add(song);
            //}
            
            
            if (wasSearch)
            {
                MyViewModel.FilteredSongs = songsToDisplay; 
            }
            else
            {
                MyViewModel.FilteredSongs = null; 
            }
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

    private async void CurrentPositionSlider_DragCompleted(object sender, EventArgs e)
    {
       await MyViewModel.SeekSongPosition(currPosPer: CurrentPositionSlider.Value);
    }


    private void VolumeSlider_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        MyViewModel.SetVolume(VolumeSlider.Value);
    }

    private async void BtmBarPointerGest_PointerEntered(object sender, PointerEventArgs e)
    {
        await BmtBarView.AnimateFocusModePointerEnter();

    }

    private async void BtmBarPointerGest_PointerExited(object sender, PointerEventArgs e)
    {
        await BmtBarView.AnimateFocusModePointerExited(endOpacity: 0.4, endScale: 1);

    }

    private static async void PrimarySectionGest_Tapped(object sender, TappedEventArgs e)
    {
       await Shell.Current.GoToAsync(nameof(SingleSongPage));
    }
    private async void SeekSongPosFromLyric_Tapped(object sender, TappedEventArgs e)
    {
        if (MyViewModel.IsPlaying)
        {
            View bor = (View)sender;
            LyricPhraseModelView lyr = (LyricPhraseModelView)bor.BindingContext;
            await MyViewModel.SeekSongPosition(lyr);
        }
    }

    private void LyricsColView_Loaded(object sender, EventArgs e)
    {
        try
        {
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

            //MyViewModel.ScrollAfterAppearing();
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
    private async void LyricsColView_SelectionChanged(object sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    {
        LyricPhraseModelView? CurrLyric = LyricsColView.SelectedItem as LyricPhraseModelView;
        if (CurrLyric is null)
            return;
        if (MyViewModel.IsPlaying)
        {
            if (string.IsNullOrEmpty(CurrLyric.Text))
            {
                await Task.WhenAll(BmtBarView.DimmOutCompletely(),
                    MainGrid.DimmOutCompletely(), PageBGImg.DimmInCompletely());

            }
            else
            {
                await Task.WhenAll(BmtBarView.DimmInCompletely(),
                    MainGrid.DimmInCompletely(), PageBGImg.DimmInCompletely(),
                    PageBGImg.DimmOut(endOpacity: 0.15));

            }
        }
    }

}