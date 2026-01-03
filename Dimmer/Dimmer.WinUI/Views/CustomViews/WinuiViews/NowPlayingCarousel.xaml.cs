using Dimmer.ViewModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Dimmer.WinUI.Views.CustomViews.WinuiViews;

public sealed partial class NowPlayingCarousel : UserControl
{
    public NowPlayingCarousel()
    {
        InitializeComponent();
        MyViewModel = IPlatformApplication.Current?.Services.GetService<BaseViewModelWin>();
    }

    public BaseViewModelWin? MyViewModel { get; internal set; }

    private bool _isNavigating = false;
    private int _lastSelectedIndex = 1;

    private async void SongCarouselFlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isNavigating || MyViewModel == null)
            return;

        var flipView = sender as FlipView;
        if (flipView == null || flipView.SelectedIndex < 0)
            return;

        var selectedIndex = flipView.SelectedIndex;
        
        // Only respond if the index actually changed from center
        if (selectedIndex == _lastSelectedIndex)
            return;

        // Current song should be at index 1 (middle of the 3-item carousel)
        // If user swipes to index 0, go to previous song
        // If user swipes to index 2, go to next song
        if (selectedIndex == 0)
        {
            _isNavigating = true;
            await MyViewModel.PreviousTrackAsync();
            // ViewModel will update CarouselItems, which will trigger resetting to center
            _isNavigating = false;
        }
        else if (selectedIndex == 2)
        {
            _isNavigating = true;
            await MyViewModel.NextTrackAsync();
            // ViewModel will update CarouselItems, which will trigger resetting to center
            _isNavigating = false;
        }

        _lastSelectedIndex = selectedIndex;
    }
}
