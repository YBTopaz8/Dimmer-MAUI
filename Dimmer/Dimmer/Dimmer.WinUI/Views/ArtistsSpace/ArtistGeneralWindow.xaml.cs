
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System.Numerics;

using Dimmer.WinUI.Utils.Models;
using Dimmer.WinUI.Utils.WinMgt;

using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

using Vanara.Extensions.Reflection;

using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Composition;

using Button = Microsoft.UI.Xaml.Controls.Button;
using Page = Microsoft.UI.Xaml.Controls.Page;
using SpringVector3NaturalMotionAnimation = Microsoft.UI.Composition.SpringVector3NaturalMotionAnimation;
using Window = Microsoft.UI.Xaml.Window;
using Dimmer.WinUI.Views.ArtistsSpace.MAUI;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.ArtistsSpace;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ArtistGeneralWindow : Window
{
    public ArtistGeneralWindow()
    {
        BaseViewModel viewModel = IPlatformApplication.Current!.Services.GetService<BaseViewModel>()!;
        ViewModel=viewModel;
        InitializeComponent();

        this.ArtistsPage.DataContext=ViewModel;
    }
    public BaseViewModel ViewModel { get; }
    SpringVector3NaturalMotionAnimation _springAnimation;
    private int previousSelectedIndex;


    private void SelectorBar2_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        SelectorBarItem selectedItem = sender.SelectedItem;
        int currentSelectedIndex = sender.Items.IndexOf(selectedItem);
        System.Type pageType = this.GetType();

        switch (currentSelectedIndex)
        {
            case 0:
                pageType = typeof(AllArtistsPage);
                break;
            case 1:
                break;

            default:
                break;
        }

        var slideNavigationTransitionEffect = currentSelectedIndex - previousSelectedIndex > 0 ? SlideNavigationTransitionEffect.FromRight : SlideNavigationTransitionEffect.FromLeft;

        ContentFrame.Navigate(pageType, null, new SlideNavigationTransitionInfo() { Effect = slideNavigationTransitionEffect });

        previousSelectedIndex = currentSelectedIndex;
    }
}
