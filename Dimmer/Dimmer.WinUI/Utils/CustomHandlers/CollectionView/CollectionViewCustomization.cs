using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;

using Microsoft.Maui.Controls.Handlers.Items;
using Windows.Foundation;

using Border = Microsoft.UI.Xaml.Controls.Border;
using ListView = Microsoft.UI.Xaml.Controls.ListView;
using ListViewSelectionMode = Microsoft.UI.Xaml.Controls.ListViewSelectionMode;
using Thickness = Microsoft.UI.Xaml.Thickness;

namespace Dimmer.WinUI.Utils.CustomHandlers.CollectionView;
public static class CollectionViewCustomization
{
    // This part for removing styling remains the same, just the method signature changes.
    private static readonly ConditionalWeakTable<ListView, TypedEventHandler<ListViewBase, ContainerContentChangingEventArgs>> _handlers = new();

    // ----------------------------------------------------
    //  METHOD SIGNATURE UPDATED to use the correct handler type
    // ----------------------------------------------------
    public static void RemoveDefaultStyling(CollectionViewHandler handler)
    {
        var listView = handler.PlatformView as ListView;
        if (listView == null)
            return;

        // --- Apply the styling ---
        listView.SelectionMode = ListViewSelectionMode.None;
        listView.Background = null;
        listView.BorderBrush = null;
        listView.BorderThickness = new Thickness(0);

        // --- Fix the event handler leak ---
        TypedEventHandler<ListViewBase, ContainerContentChangingEventArgs> containerContentChangingHandler = (s, args) =>
        {
            if (args.ItemContainer is ListViewItem item)
            {
                item.Background = null;
                item.BorderThickness = new Thickness(0);
                item.FocusVisualPrimaryThickness = new Thickness(0);
                item.FocusVisualSecondaryThickness = new Thickness(0);
            }
        };

        // Using Loaded/Unloaded is more robust for subscribing/unsubscribing
        listView.Loaded += (s, e) =>
        {
            var lv = s as ListView;
            if (lv != null && !_handlers.TryGetValue(lv, out _))
            {
                lv.ContainerContentChanging += containerContentChangingHandler;
                _handlers.Add(lv, containerContentChangingHandler);
            }
        };

        listView.Unloaded += (s, e) =>
        {
            var lv = s as ListView;
            if (lv != null && _handlers.TryGetValue(lv, out var storedHandler))
            {
                lv.ContainerContentChanging -= storedHandler;
                _handlers.Remove(lv);
            }
        };
    }

    // ----------------------------------------------------
    //  METHOD SIGNATURE UPDATED to use the correct handler type
    // ----------------------------------------------------
    public static void AddSelectionAnimation(CollectionViewHandler handler)
    {
        var listView = handler.PlatformView as ListView;
        if (listView == null || listView.SelectionMode == ListViewSelectionMode.None)
            return;

        listView.SelectionChanged += (s, e) =>
        {
            var newSelectedItem = e.AddedItems.FirstOrDefault();
            var oldSelectedItem = e.RemovedItems.FirstOrDefault();

            if (oldSelectedItem != null)
            {
                var oldContainer = listView.ContainerFromItem(oldSelectedItem) as ListViewItem;
                AnimateHighlight(oldContainer, false);
            }

            if (newSelectedItem != null)
            {
                var newContainer = listView.ContainerFromItem(newSelectedItem) as ListViewItem;
                AnimateHighlight(newContainer, true);
            }
        };
    }

    private static void AnimateHighlight(FrameworkElement container, bool isVisible)
    {
        if (container == null)
            return;
        var highlightBorder = FindVisualChild<Border>(container, "HighlightBorder");
        if (highlightBorder == null)
            return;

        var compositor = ElementCompositionPreview.GetElementVisual(highlightBorder).Compositor;
        var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
        opacityAnimation.InsertKeyFrame(1.0f, isVisible ? 1.0f : 0.0f);
        opacityAnimation.Duration = TimeSpan.FromMilliseconds(250);
        opacityAnimation.Target = "Opacity";

        ElementCompositionPreview.GetElementVisual(highlightBorder).StartAnimation("Opacity", opacityAnimation);
    }

    public static T FindVisualChild<T>(DependencyObject obj, string name) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(obj, i);
            if (child != null && child is T && (child as FrameworkElement)?.Name == name)
                return (T)child;

            T childOfChild = FindVisualChild<T>(child, name);
            if (childOfChild != null)
                return childOfChild;
        }
        return null;
    }
}
//public static class CollectionViewCustomization
//{
//    // Use ConditionalWeakTable to associate the handler with the ListView instance
//    // This is a robust way to manage event handlers without causing memory leaks.
//    private static readonly ConditionalWeakTable<ListView, TypedEventHandler<ListViewBase, ContainerContentChangingEventArgs>> _handlers = new();

//    public static void RemoveDefaultStyling(Microsoft.Maui.Controls.Handlers.Items.ReorderableItemsViewHandler<ReorderableItemsView> handler)
//    {
//        var listView = handler.PlatformView as ListView;
//        if (listView == null)
//            return;

//        // --- Apply the styling ---
//        listView.SelectionMode = ListViewSelectionMode.None;
//        listView.Background = null;
//        listView.BorderBrush = null;
//        listView.BorderThickness = new Thickness(0);

//        // --- Fix the event handler leak ---
//        // 1. Create the handler delegate ONCE.
//        TypedEventHandler<ListViewBase, ContainerContentChangingEventArgs> containerContentChangingHandler = (s, args) =>
//        {
//            if (args.ItemContainer is ListViewItem item)
//            {
//                item.Background = null;
//                item.BorderThickness = new Thickness(0);
//                item.FocusVisualPrimaryThickness = new Thickness(0);
//                item.FocusVisualSecondaryThickness = new Thickness(0);
//            }
//        };

//        // 2. Subscribe and store the handler for later removal.
//        listView.Loaded += (s, e) =>
//        {
//            var lv = s as ListView;
//            if (lv == null)
//                return;

//            // Only add if it's not already there
//            if (!_handlers.TryGetValue(lv, out _))
//            {
//                lv.ContainerContentChanging += containerContentChangingHandler;
//                _handlers.Add(lv, containerContentChangingHandler);
//            }
//        };

//        // 3. Unsubscribe correctly.
//        listView.Unloaded += (s, e) =>
//        {
//            var lv = s as ListView;
//            if (lv == null)
//                return;

//            if (_handlers.TryGetValue(lv, out var storedHandler))
//            {
//                lv.ContainerContentChanging -= storedHandler;
//                _handlers.Remove(lv); // Clean up the table
//            }
//        };
//    }

//    public static void AddSelectionAnimation(Microsoft.Maui.Controls.Handlers.Items.ReorderableItemsViewHandler<ReorderableItemsView> handler)
//    {
//        var listView = handler.PlatformView as ListView;
//        if (listView == null)
//            return;

//        // We only want animations if selection is enabled
//        if (listView.SelectionMode == ListViewSelectionMode.None)
//            return;

//        listView.SelectionChanged += (s, e) =>
//        {
//            // Get the old and new selected data items
//            var newSelectedItem = e.AddedItems.FirstOrDefault();
//            var oldSelectedItem = e.RemovedItems.FirstOrDefault();

//            // Animate the old item out (fade out)
//            if (oldSelectedItem != null)
//            {
//                // ContainerFromItem can return null if the item is virtualized (scrolled off-screen)
//                var oldContainer = listView.ContainerFromItem(oldSelectedItem) as ListViewItem;
//                AnimateHighlight(oldContainer, false); // Animate to invisible
//            }

//            // Animate the new item in (fade in)
//            if (newSelectedItem != null)
//            {
//                var newContainer = listView.ContainerFromItem(newSelectedItem) as ListViewItem;
//                AnimateHighlight(newContainer, true); // Animate to visible
//            }
//        };
//    }

//    private static void AnimateHighlight(FrameworkElement container, bool isVisible)
//    {
//        // Ensure the container and its content exist
//        if (container == null)
//            return;

//        // Find our custom highlight border within the item's template
//        var highlightBorder = FindVisualChild<Border>(container, "HighlightBorder");
//        if (highlightBorder == null)
//            return;

//        // Get the compositor for creating animations
//        var compositor = ElementCompositionPreview.GetElementVisual(highlightBorder).Compositor;

//        // Create a scalar (float) animation for the Opacity property
//        var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
//        opacityAnimation.InsertKeyFrame(1.0f, isVisible ? 1.0f : 0.0f); // Target value
//        opacityAnimation.Duration = TimeSpan.FromMilliseconds(250);
//        opacityAnimation.Target = "Opacity";

//        // Start the animation on the highlight border's visual
//        ElementCompositionPreview.GetElementVisual(highlightBorder).StartAnimation("Opacity", opacityAnimation);
//    }

//    // Helper method to find a named element in the visual tree
//    public static T FindVisualChild<T>(DependencyObject obj, string name) where T : DependencyObject
//    {
//        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
//        {
//            DependencyObject child = VisualTreeHelper.GetChild(obj, i);
//            if (child != null && child is T && (child as FrameworkElement)?.Name == name)
//            {
//                return (T)child;
//            }
//            else
//            {
//                T childOfChild = FindVisualChild<T>(child, name);
//                if (childOfChild != null)
//                    return childOfChild;
//            }
//        }
//        return null;
//    }
//}