using Point = Microsoft.Maui.Graphics.Point;
using Rect = Microsoft.Maui.Graphics.Rect;

namespace Dimmer.Utils.Extensions;

public static class ViewExts
{
    public static Rect GetAbsoluteBounds(VisualElement view, IWindow window)
    {
        var location = GetAbsoluteLocation(view,window);
        return new Rect(location.X, location.Y, view.Width, view.Height);
    }

    public static Point GetAbsoluteLocation(VisualElement view, IWindow window)
    {
        var transform = GetVisualElementWindowOffset(view,window);
        return new Point(transform.X, transform.Y);
    }

    private static Point GetVisualElementWindowOffset(VisualElement view, IWindow window)
    {
        var offset = new Point(view.X, view.Y);
        var parent = view.Parent as VisualElement;

        while (parent != null && parent != window.Content)
        {
            offset.X += parent.X;
            offset.Y += parent.Y;
            parent = parent.Parent as VisualElement;
        }

        return offset;
    }
}
