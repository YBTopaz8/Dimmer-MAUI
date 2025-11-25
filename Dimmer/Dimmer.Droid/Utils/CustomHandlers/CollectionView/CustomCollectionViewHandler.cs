using Microsoft.Maui.Controls.Handlers.Items;

namespace Dimmer.Utils.CustomHandlers.CollectionView;
public class CustomCollectionViewHandler : CollectionViewHandler
{
    protected override void ConnectHandler(RecyclerView platformView)
    {
        base.ConnectHandler(platformView);

        // Our custom logic now runs for every CollectionView!

        // Disable item animator to prepare for custom ones or for performance
        platformView.SetItemAnimator(null);

        // Remove all item decorations in reverse order
        for (int i = platformView.ItemDecorationCount - 1; i >= 0; i--)
        {
            platformView.RemoveItemDecorationAt(i);
        }

        // Remove background for transparency
        platformView.Background = null;
        platformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
    }
}