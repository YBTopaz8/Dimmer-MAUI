using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

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
