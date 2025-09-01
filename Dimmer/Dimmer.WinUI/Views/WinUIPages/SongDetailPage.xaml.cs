using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

using NavigationEventArgs = Microsoft.UI.Xaml.Navigation.NavigationEventArgs;
using Page = Microsoft.UI.Xaml.Controls.Page;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinUIPages;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SongDetailPage : Page
{
    public SongModelView DetailedSong { get; set; }
    public SongDetailPage()
    {
        InitializeComponent();

    }
    BaseViewModel MyViewModel { get; set; }
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // 1. Get the Song object passed from the previous page
        DetailedSong = e.Parameter as SongModelView;

        // 2. Get the animation service and retrieve the animation we prepared
        ConnectedAnimation animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("ForwardConnectedAnimation");

        if (animation != null)
        {
            // 3. Start the animation, connecting it to our target Image.
            //    The second parameter is a list of elements to animate in coordination.
            animation.TryStart(detailedImage, new UIElement[] { coordinatedPanel });
        }
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);

        // This is for the return journey!
        // Prepare the animation back to the main list. We use a different key.
        ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("BackConnectedAnimation", detailedImage);
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
