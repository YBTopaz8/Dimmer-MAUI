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

    private bool _isUserInteraction = true;

    private async void SongCarouselFlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isUserInteraction || MyViewModel == null)
            return;

        var flipView = sender as FlipView;
        if (flipView == null || flipView.SelectedIndex < 0)
            return;

        // Current song should be at index 1 (middle of the 3-item carousel)
        // If user swipes to index 0, go to previous song
        // If user swipes to index 2, go to next song
        if (flipView.SelectedIndex == 0)
        {
            _isUserInteraction = false;
            await MyViewModel.PreviousTrackAsync();
            // Reset to center after navigation
            await Task.Delay(100);
            flipView.SelectedIndex = 1;
            _isUserInteraction = true;
        }
        else if (flipView.SelectedIndex == 2)
        {
            _isUserInteraction = false;
            await MyViewModel.NextTrackAsync();
            // Reset to center after navigation
            await Task.Delay(100);
            flipView.SelectedIndex = 1;
            _isUserInteraction = true;
        }
    }
}
