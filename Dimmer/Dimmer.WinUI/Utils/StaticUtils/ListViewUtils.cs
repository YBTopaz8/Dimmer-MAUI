using ListView = Microsoft.UI.Xaml.Controls.ListView;
using ListViewSelectionMode = Microsoft.UI.Xaml.Controls.ListViewSelectionMode;
using Point = Windows.Foundation.Point;

namespace Dimmer.WinUI.Utils.StaticUtils;

public static class ListViewHelper
{
    // Returns a list of items currently visible in the ListView viewport.
    public static List<object> GetVisibleItems(ListView listView)
    {
        var visibleItems = new List<object>();
        var scrollViewer = GetScrollViewer(listView);
        if (scrollViewer == null)
            return visibleItems;

        double viewportTop = scrollViewer.VerticalOffset;
        double viewportBottom = viewportTop + scrollViewer.ViewportHeight;

        foreach (var item in listView.Items)
        {
            // Get the item container (may be null if item isn't realized due to virtualization)
            var container = listView.ContainerFromItem(item) as FrameworkElement;
            if (container == null)
                continue;

            // Compute the container's position relative to the ScrollViewer's content
            GeneralTransform transform = container.TransformToVisual(scrollViewer.Content as UIElement);
            Point position = transform.TransformPoint(new Point(0, 0));
            double itemTop = position.Y;
            double itemBottom = itemTop + container.ActualHeight;

            // Check if any portion of the container is in the visible viewport.
            if (itemBottom >= viewportTop && itemTop <= viewportBottom)
                visibleItems.Add(item);
        }
        return visibleItems;
    }

    // Returns the count of visible items.
    public static int GetVisibleItemCount(ListView listView)
    {
        return GetVisibleItems(listView).Count;
    }

    // Returns the total count of items in the ItemsSource.
    public static int GetItemSourceCount(ListView listView)
    {
        return listView.Items.Count;
    }

    // Returns the index of a given item, or -1 if not found.
    public static int GetItemIndex(ListView listView, object item)
    {
        return listView.Items.IndexOf(item);
    }

    // Helper method to retrieve the internal ScrollViewer by traversing the visual tree.
    private static ScrollViewer? GetScrollViewer(DependencyObject element)
    {
        if (element == null)
            return null;

        if (element is ScrollViewer sv)
            return sv;

        int count = VisualTreeHelper.GetChildrenCount(element);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(element, i);
            var result = GetScrollViewer(child);
            if (result != null)
                return result;
        }
        return null;
    }
    // 1. Returns the index of the first item that is fully visible.
    public static int GetFirstFullyVisibleItemIndex(ListView listView)
    {
        ScrollViewer? sv = GetScrollViewer(listView);
        if (sv == null)
            return -1;

        double viewportTop = sv.VerticalOffset;
        double viewportBottom = viewportTop + sv.ViewportHeight;

        for (int i = 0; i < listView.Items.Count; i++)
        {
            var container = listView.ContainerFromIndex(i) as FrameworkElement;
            if (container == null)
                continue;

            GeneralTransform transform = container.TransformToVisual(sv.Content as UIElement);
            Point pos = transform.TransformPoint(new Point(0, 0));
            double itemTop = pos.Y;
            double itemBottom = itemTop + container.ActualHeight;

            if (itemTop >= viewportTop && itemBottom <= viewportBottom)
                return i;
        }
        return -1;
    }

    // 2. Returns the index of the last item that is fully visible.
    public static int GetLastFullyVisibleItemIndex(ListView listView)
    {
        ScrollViewer? sv = GetScrollViewer(listView);
        if (sv == null)
            return -1;

        double viewportTop = sv.VerticalOffset;
        double viewportBottom = viewportTop + sv.ViewportHeight;

        for (int i = listView.Items.Count - 1; i >= 0; i--)
        {
            var container = listView.ContainerFromIndex(i) as FrameworkElement;
            if (container == null)
                continue;

            GeneralTransform transform = container.TransformToVisual(sv.Content as UIElement);
            Point pos = transform.TransformPoint(new Point(0, 0));
            double itemTop = pos.Y;
            double itemBottom = itemTop + container.ActualHeight;

            if (itemTop >= viewportTop && itemBottom <= viewportBottom)
                return i;
        }
        return -1;
    }

    // 3. Calculates the vertical scroll percentage (0 to 100%).
    public static double GetScrollPercentage(ListView listView)
    {
        ScrollViewer? sv = GetScrollViewer(listView);
        if (sv == null)
            return 0;

        double max = sv.ExtentHeight - sv.ViewportHeight;
        if (max <= 0)
            return 100;

        return (sv.VerticalOffset / max) * 100;
    }

    // 4. Determines if a specific item is fully visible in the viewport.
    public static bool IsItemFullyVisible(ListView listView, object item)
    {
        ScrollViewer? sv = GetScrollViewer(listView);
        if (sv == null)
            return false;

        var container = listView.ContainerFromItem(item) as FrameworkElement;
        if (container == null)
            return false;

        GeneralTransform transform = container.TransformToVisual(sv.Content as UIElement);
        Point pos = transform.TransformPoint(new Point(0, 0));
        double viewportTop = sv.VerticalOffset;
        double viewportBottom = viewportTop + sv.ViewportHeight;

        return pos.Y >= viewportTop && (pos.Y + container.ActualHeight) <= viewportBottom;
    }

    // 5. Gets the vertical offset (position) of an item relative to the ListView's content.
    public static double GetItemRelativePosition(ListView listView, object item)
    {
        ScrollViewer? sv = GetScrollViewer(listView);
        if (sv == null)
            return -1;

        var container = listView.ContainerFromItem(item) as FrameworkElement;
        if (container == null)
            return -1;

        GeneralTransform transform = container.TransformToVisual(sv.Content as UIElement);
        Point pos = transform.TransformPoint(new Point(0, 0));
        return pos.Y;
    }

    // 6. Returns the total content height of the ListView (all items combined).
    public static double GetTotalContentHeight(ListView listView)
    {
        ScrollViewer? sv = GetScrollViewer(listView);
        return sv?.ExtentHeight ?? 0;
    }
}
public class BorderlessCollectionView : CollectionView { }

