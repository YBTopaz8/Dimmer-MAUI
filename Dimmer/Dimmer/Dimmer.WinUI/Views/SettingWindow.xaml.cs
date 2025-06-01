using Dimmer.WinUI.Views.WinuiWindows;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

using SpringVector3NaturalMotionAnimation = Microsoft.UI.Composition.SpringVector3NaturalMotionAnimation;
using Window = Microsoft.UI.Xaml.Window;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SettingWindow : Window
{
    public SettingWindow(BaseViewModel viewModel)
    {
        InitializeComponent();
        ViewModel=viewModel;

    }

    public BaseViewModel ViewModel { get; }

    SpringVector3NaturalMotionAnimation _springAnimation;
    private int previousSelectedIndex;



    private void SelectorBar2_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        SelectorBarItem selectedItem = sender.SelectedItem;
        int currentSelectedIndex = sender.Items.IndexOf(selectedItem);
        System.Type pageType;

        switch (currentSelectedIndex)
        {
            case 0:
                pageType = typeof(FolderScanPage);
                break;
            case 1:
                pageType = typeof(OnlinePage);
                break;

            default:
                pageType = typeof(FolderScanPage);
                break;
        }

        var slideNavigationTransitionEffect = currentSelectedIndex - previousSelectedIndex > 0 ? SlideNavigationTransitionEffect.FromRight : SlideNavigationTransitionEffect.FromLeft;

        ContentFrame.Navigate(pageType, null, new SlideNavigationTransitionInfo() { Effect = slideNavigationTransitionEffect });

        previousSelectedIndex = currentSelectedIndex;
    }
}
