// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.CustomViews.WinuiViews;

public sealed partial class MorphContainerView : UserControl
{
    public MorphContainerView()
    {
        InitializeComponent();
    }
    private void MorphContainer_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        // Start expanding
        ExpandStoryboard.Begin();

        // Optional: Bring to front (Z-Index) so it expands OVER other items
        Canvas.SetZIndex((UIElement)sender, 10);
    }

    private void MorphContainer_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        // Collapse back
        CollapseStoryboard.Begin();

        // Reset Z-Index
        Canvas.SetZIndex((UIElement)sender, 0);
    }
}
