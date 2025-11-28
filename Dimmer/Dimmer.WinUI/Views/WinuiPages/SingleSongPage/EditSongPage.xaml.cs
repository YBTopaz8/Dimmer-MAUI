
using System.Text.RegularExpressions;

using Microsoft.UI.Xaml.Documents;

using Button = Microsoft.UI.Xaml.Controls.Button;
using ListView = Microsoft.UI.Xaml.Controls.ListView;
using ListViewSelectionMode = Microsoft.UI.Xaml.Controls.ListViewSelectionMode;
using MenuFlyout = Microsoft.UI.Xaml.Controls.MenuFlyout;
using MenuFlyoutItem = Microsoft.UI.Xaml.Controls.MenuFlyoutItem;
using NavigationEventArgs = Microsoft.UI.Xaml.Navigation.NavigationEventArgs;
using Thickness = Microsoft.UI.Xaml.Thickness;
using ToolTip = Microsoft.UI.Xaml.Controls.ToolTip;
using Visual = Microsoft.UI.Composition.Visual;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages.SingleSongPage;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class EditSongPage : Page
{
    readonly Microsoft.UI.Xaml.Controls.Page? NativeWinUIPage;
    private SongTransitionAnimation _userPrefAnim = SongTransitionAnimation.Spring;

    private readonly Compositor _compositor;
    public SongModelView? DetailedSong { get; set; }
    public EditSongPage()
    {
        InitializeComponent(); 
        _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
    }

    BaseViewModelWin MyViewModel { get; set; }
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        //DetailedSong = DetailedSong is null ? MyViewModel.SelectedSong : DetailedSong;

        if (e.Parameter is SongDetailNavArgs args)
        {
            var vm = args.ExtraParam is null ? args.ViewModel as BaseViewModelWin : args.ExtraParam as BaseViewModelWin;

            if (vm != null)
            {
                detailedImage.Opacity = 0;
                MyViewModel = vm;
                DetailedSong = args.Song;

                MyViewModel.CurrentWinUIPage = this;
                Visual? visual = ElementCompositionPreview.GetElementVisual(detailedImage);
                PlatUtils.ApplyEntranceEffect(visual, detailedImage, _userPrefAnim, _compositor);

                var animation = ConnectedAnimationService.GetForCurrentView()
               .GetAnimation("ForwardConnectedAnimation");

                detailedImage.Loaded += (_, _) =>
                {
                    detailedImage.Opacity = 1;
                    animation?.TryStart(detailedImage);
                };
                MyViewModel.SelectedSong = DetailedSong;
                await MyViewModel.LoadSelectedSongLastFMData();
                //LoadUiComponents();

            }
        }
    }
    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        // Standard navigation back
        if (Frame.CanGoBack)
        {

            Frame.GoBack();
        }
    }
}
