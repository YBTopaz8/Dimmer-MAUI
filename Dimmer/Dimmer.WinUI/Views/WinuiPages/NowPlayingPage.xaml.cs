using Dimmer.WinUI.Views.WinuiPages.SingleSongPage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Dimmer.WinUI.Views.WinuiPages;

public sealed partial class NowPlayingPage : Page
{
    public NowPlayingPage()
    {
        InitializeComponent();
        MyViewModel = IPlatformApplication.Current?.Services.GetService<BaseViewModelWin>();
    }

    public BaseViewModelWin? MyViewModel { get; internal set; }

    private void ViewLyricsButton_Click(object sender, RoutedEventArgs e)
    {
        MyViewModel?.OpenLyricsPopUpWindow(1);
    }

    private void ViewSongDetailsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (MyViewModel == null) return;

            MyViewModel.SelectedSong = MyViewModel.CurrentPlayingSongView;
            var dimmerWindow = MyViewModel.winUIWindowMgrService.GetWindow<DimmerWin>();
            dimmerWindow ??= MyViewModel.winUIWindowMgrService.CreateWindow<DimmerWin>();

            if (dimmerWindow != null)
                dimmerWindow.NavigateToPage(typeof(SongDetailPage));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }
}
