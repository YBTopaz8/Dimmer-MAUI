using Microsoft.Maui.Controls.Handlers.Items;

namespace Dimmer.WinUI.Utils.CustomHandlers.CollectionView;
// This handler will be applied to ALL CollectionViews in your app.
public class CustomCollectionViewHandler : CollectionViewHandler
{

    protected override void ConnectHandler(ListViewBase platformView)
    {
        base.ConnectHandler(platformView);


        //var ee = new Microsoft.UI.Xaml.Media.Animation.TransitionCollection();
        //ee.Add(new Microsoft.UI.Xaml.Media.Animation.ReorderThemeTransition()
        //);
        //ee.Add(new Microsoft.UI.Xaml.Media.Animation.ContentThemeTransition());
        //ee.Add(new Microsoft.UI.Xaml.Media.Animation.AddDeleteThemeTransition() );
        //ee.Add(new Microsoft.UI.Xaml.Media.Animation.RepositionThemeTransition() { IsStaggeringEnabled=true});
        //ee.Add(new Microsoft.UI.Xaml.Media.Animation.EntranceThemeTransition() { IsStaggeringEnabled=true});
        //ee.Add(new Microsoft.UI.Xaml.Media.Animation.PopupThemeTransition() );

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