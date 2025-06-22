using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.UI.Xaml.Controls;

namespace Dimmer.WinUI.Utils.CustomHandlers.CollectionView;
// This handler will be applied to ALL CollectionViews in your app.
public class CustomCollectionViewHandler : CollectionViewHandler
{

    protected override void ConnectHandler(ListViewBase platformView)
    {
        base.ConnectHandler(platformView);


        platformView.SelectionMode = Microsoft.UI.Xaml.Controls.ListViewSelectionMode.Single;

        platformView.Background = null;
        platformView.BorderBrush = null;
        platformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
        
        platformView.ContainerContentChanging += (s, args) =>
        {
            if (args.ItemContainer is Microsoft.UI.Xaml.Controls.ListViewItem item)
            {
                item.Background = null;
                item.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
                item.FocusVisualPrimaryThickness = new Microsoft.UI.Xaml.Thickness(0);
                item.FocusVisualSecondaryThickness = new Microsoft.UI.Xaml.Thickness(0);
            }
        };

    }
    protected override void DisconnectHandler(ListViewBase platformView)
    {
        base.DisconnectHandler(platformView);
    }
}