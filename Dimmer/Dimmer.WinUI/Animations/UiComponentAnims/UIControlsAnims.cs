using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

using CommunityToolkit.WinUI;

using Button = Microsoft.UI.Xaml.Controls.Button;
using Border = Microsoft.UI.Xaml.Controls.Border;
using Colors = Microsoft.UI.Colors;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;

namespace Dimmer.WinUI.Animations.UiComponentAnims;

public static class UIControlsAnims
{
    public static void AnimateBtnPointerEntered(this Button button, Compositor compositor)
    {
        var btn = (UIElement)button;

        var visual = btn.GetVisual();


        var anim = compositor.CreateScalarKeyFrameAnimation();
        anim.InsertKeyFrame(1f, 1.2f);
        anim.Duration = TimeSpan.FromMilliseconds(250);
        visual.CenterPoint = new Vector3((float)btn.RenderSize.Width / 2, (float)btn.RenderSize.Height / 2, 0);
        visual.StartAnimation("Scale.X", anim);
        visual.StartAnimation("Scale.Y", anim);
        button.Foreground = new SolidColorBrush(Colors.DarkSlateBlue);
        
    }
    public static void AnimateBtnPointerExited(this Button button, Compositor compositor)
    {
        var btn = (UIElement)button;
        var visual = btn.GetVisual();
        var anim = compositor.CreateScalarKeyFrameAnimation();
        anim.InsertKeyFrame(1f, 1f);
        anim.Duration = TimeSpan.FromMilliseconds(150);
        visual.StartAnimation("Scale.X", anim);
        visual.StartAnimation("Scale.Y", anim);
        button.Foreground = new SolidColorBrush(Colors.White);

    }
    public static void AnimateBorderPointerExited(this Border border, Compositor compositor)
    {
        var btn = (UIElement)border;
        var visual = btn.GetVisual();
        var anim = compositor.CreateScalarKeyFrameAnimation();
        anim.InsertKeyFrame(1f, 1f);
        anim.Duration = TimeSpan.FromMilliseconds(150);
        visual.StartAnimation("Scale.X", anim);
        visual.StartAnimation("Scale.Y", anim);

    }
}
