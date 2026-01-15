using System.Threading.Tasks;
using AndroidX.Lifecycle;
using Bumptech.Glide;
using Dimmer.Utilities;
using Point = Microsoft.Maui.Graphics.Point;
using Rect = Microsoft.Maui.Graphics.Rect;

namespace Dimmer.Utils.Extensions;

public static class ViewExts
{
    public static void SetButtonIconColor(this MaterialButton btn, Color color)
    {
        btn.IconTint = AppUtil.ToColorStateList(color);
    }
    public static void SetImageWithGlide(this ImageView ImgView, string imgPath)
    {

        Glide.With(ImgView.Context).Load(imgPath).Into(ImgView);
    }
    public static async Task SetImageWithStringPathViaGlideAndFilterEffect(this ImageView ImgView, string imgPath, FilterType desiredFilter)
    {
        var glassyImg = await ImageFilterUtils.ApplyFilter(imgPath, desiredFilter);
        Glide.With(ImgView.Context)
            .Load(glassyImg)
            .Into(ImgView);
    
    }

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
