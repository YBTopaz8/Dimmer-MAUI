using System.Numerics;

using Dimmer.Data.Models;
using Dimmer.WinUI.Utils.Models;
using Dimmer.WinUI.Utils.WinMgt;
using Dimmer.WinUI.Views.WinuiWindows;
using Dimmer.WinUI.Views.WinuiWindows;

using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Animation;

using Vanara.Extensions.Reflection;

using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Composition;

using ArtistModel = Dimmer.WinUI.Utils.Models.ArtistModelWin;
using Button = Microsoft.UI.Xaml.Controls.Button;
using Frame = Microsoft.UI.Xaml.Controls.Frame;
using Page = Microsoft.UI.Xaml.Controls.Page;
using SpringVector3NaturalMotionAnimation = Microsoft.UI.Composition.SpringVector3NaturalMotionAnimation;
using Window = Microsoft.UI.Xaml.Window;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.ArtistsSpace;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SpecificArtistPage : Page
{
    public SpecificArtistPage()
    {
        InitializeComponent();

        BaseViewModel viewModel = IPlatformApplication.Current!.Services.GetService<BaseViewModel>()!;
        ViewModel=viewModel;
        SpecificArtist.DataContext=ViewModel;
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
        _storedArtist=new();
        _storedArtist.Name="Test";
        //GoBackButton.Loaded += GoBackButton_Loaded;
    }

    private void GoBackButton_Loaded(object sender, RoutedEventArgs e)
    {
        //When we land in page, put focus on the back button
        GoBackButton.Focus(FocusState.Programmatic);
    }
    Frame thisFrame { get; set; } = null!;
    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {

        base.OnNavigatedTo(e);

        // Store the item to be used in binding to UI
        var param = e.Parameter as Dictionary<string, object>;


        if (param != null)
        {
            _storedArtist = param["artist"] as ArtistModelView;
            thisFrame ??= param["frame"] as Frame;
        }

        ConnectedAnimation imageAnimation = ConnectedAnimationService.GetForCurrentView().GetAnimation("ForwardConnectedAnimation");
        if (imageAnimation != null)
        {
            // Connected animation + coordinated animation
            imageAnimation.TryStart(detailedImage, new UIElement[] { coordinatedPanel });

        }
    }

    // Create connected animation back to collection page.
    protected override void OnNavigatingFrom(Microsoft.UI.Xaml.Navigation.NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);

        ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("BackConnectedAnimation", detailedImage);
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {

        //thisFrame.GoBack();
    }

    List<ArtistModel>? Artists { get; set; }
    List<SongModelView>? ArtistsSongs { get; set; }
    public BaseViewModel ViewModel { get; }
    public ArtistModelView _storedArtist { get; set; }

    private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {

    }
}
