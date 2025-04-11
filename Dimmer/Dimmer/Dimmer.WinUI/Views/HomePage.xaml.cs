

using Dimmer.Data.ModelView;
using Dimmer.Orchestration;
using Dimmer.WinUI.Utils.StaticUtils;
using Microsoft.UI.Xaml.Input;
using Syncfusion.Maui.Toolkit.Chips;
using Syncfusion.Maui.Toolkit.EffectsView;
using System.ComponentModel;
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

        this.Loaded += SongsColView_Loaded;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        MyViewModel.CurrentlySelectedPage = Utilities.Enums.CurrentPage.HomePage;
        PlatUtils.GetWindowHandle();
    }
    private void SongsColView_Loaded(object? sender, EventArgs e)
    {
       
        try
        {
            //SongsColView.ItemsSource = MyViewModel.BaseVM.DisplayedSongs;
            MyViewModel.SetCollectionView(SongsColView);
            
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error when scrolling " + ex.Message);
        }
    }
    private void PlaySong_Tapped(object sender, TappedEventArgs e)
    {

        View send = (View)sender;
        SongModelView? song = (SongModelView)send.BindingContext;
        if (song is not null)
        {
            song.IsCurrentPlayingHighlight = false;
        }

        MyViewModel.PlaySongOnDoubleTap(song!);
    }
    private void UserHoverOnSongInColView(object sender, PointerEventArgs e)
    {
        View send = (View)sender;

        MyViewModel.PointerEntered((SongModelView)send.BindingContext, send);
    }
    
    private void UserHoverOutSongInColView(object sender, PointerEventArgs e)
    {
        View send = (View)sender;

        MyViewModel.PointerExited(send);
    }
    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {

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
        MyViewModel.SeekSongPosition(currPosPer: s.Value);


        await Task.Delay(throttleDelay);
        _isThrottling = false;
    }

    private async void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {

        //await MediaBtmBar.AnimateFocusModePointerEnter();
    }

    private async void PointerGestureRecognizer_PointerExited(object sender, PointerEventArgs e)
    {
        //await MediaBtmBar.AnimateFocusModePointerExited(endOpacity: 0.4, endScale: 1);
    }




    private void ToggleRepeat_Tapped(object sender, Microsoft.Maui.Controls.TappedEventArgs e)
    {
        MyViewModel.ToggleRepeatMode();
    }
    private void ShowCntxtMenuBtn_Clicked(object sender, EventArgs e)
    {
        //await MyViewModel.ShowHideContextMenuFromBtmBar(thiss);
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

    private void PlayPrevious_Clicked(object sender, EventArgs e)
    {
        MyViewModel.PlayPrevious();
    }

    private void PlayNext_Clicked(object sender, EventArgs e)
    {
        MyViewModel.PlayNext();
    }

    private void PlayPauseSong_Tapped(object sender, TappedEventArgs e)
    {
        MyViewModel.PlayPauseSong();
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


    private void SearchSongSB_Focused(object sender, FocusEventArgs e)
    {

    }


    private void SearchSongSB_Unfocused(object sender, FocusEventArgs e)
    {
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

            MyViewModel.IsOnSearchMode = wasSearch;
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

    private void Button_Clicked(object sender, EventArgs e)
    {
        ApplicationProps.LaunchSecondWindow();  
    }
    private void Button2_Clicked(object sender, EventArgs e)
    {
        ApplicationProps.LaunchSecondWindow();  
    }

    private void CurrentPositionSlider_DragCompleted(object sender, EventArgs e)
    {
        MyViewModel.SeekSongPosition(currPosPer: CurrentPositionSlider.Value);
    }


    private void VolumeSlider_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        MyViewModel.SetVolume(VolumeSlider.Value);
    }
}