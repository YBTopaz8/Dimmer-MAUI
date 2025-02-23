using DevExpress.Maui.Core;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;

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
   

    private void SongsColView_Tap(object sender, DevExpress.Maui.CollectionView.CollectionViewGestureEventArgs e)
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
            //MyViewModel.FilteredSongs = filteredSongs;

        }
        MyViewModel.PlaySong(e.Item as SongModelView);
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

    private void ScrollToSong_Clicked(object sender, EventArgs e)
    {
        var itemHandle = SongsColView.FindItemHandle(MyViewModel.TemporarilyPickedSong);
        SongsColView.ScrollTo(itemHandle,DevExpress.Maui.Core.DXScrollToPosition.MakeVisible);
    }

    private void SearchBy_TextChanged(object sender, EventArgs e)
    {
        var send= (TextEdit)sender;
        var Param = (string)send.TextChangedCommandParameter;
        switch (Param)
        {
            case "Title":
                ByTitle();
                break;
            case "Artist":
                ByArtist();
                break;
            default:
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

    private void ChoiceChipGroup_SelectionChanged(object sender, EventArgs e)
    {
        
    }

    private void ScrollToSong_Tap(object sender, HandledEventArgs e)
    {
        var itemHandle = SongsColView.FindItemHandle(MyViewModel.TemporarilyPickedSong);
        SongsColView.ScrollTo(itemHandle, DevExpress.Maui.Core.DXScrollToPosition.MakeVisible);

    }

    DXLayoutBase? CurrentView { get; set; }
    

    private async void SearchSong_Tap(object sender, HandledEventArgs e)
    {
        
        if (!SearchModeUI.IsVisible)
        {
            await Task.WhenAll(
            SearchModeUI.AnimateFadeInFront(),
            CurrentView!.AnimateFadeOutBack());
            isOnFocusMode = true;
            CurrentView = SearchModeUI;

            SearchBy.Focus();
            
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

    private void BtmBar_Tap(object sender, DXTapEventArgs e)
    {
        if (_isPanning)
            return;
        

    }

    private double _startX;
    private double _startY;
    private bool _isPanning;  
    private void PanGesture_PanUpdated(object sender, PanUpdatedEventArgs e)
    {
        var send = (View)sender;

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
                    if (deltaX > 0) // Right
                    {
                        try
                        {
                            HapticFeedback.Perform(HapticFeedbackType.LongPress);
                            Debug.WriteLine("Swiped Right");
                            MyViewModel.PlayNextSongCommand.Execute(null);
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
                            HapticFeedback.Perform(HapticFeedbackType.LongPress);
                        }
                        catch { }
                    }
                    else  //Up
                    {
                        try
                        {
                            if (MyViewModel.IsPlaying)
                            {
                                send.BackgroundColor = Color.FromArgb("#252526");
                                MyViewModel.PauseSong();
                            }
                            else
                            {
                                send.BackgroundColor = Colors.DarkSlateBlue;
                                MyViewModel.ResumeSong();
                            }
                        }
                        catch { }
                    }

                }

                BtmBar.TranslateTo(0, 0, 250, Easing.BounceOut);
                break;


            case GestureStatus.Canceled:  
                _isPanning = false;
                BtmBar.TranslateTo(0, 0, 250, Easing.BounceOut); // Return to original position
                break;

        }
    }

    private void ViewNowPlayPage_Tap(object sender, HandledEventArgs e)
    {
        HomeTabView.SelectedItemIndex = 1;
    }
}
