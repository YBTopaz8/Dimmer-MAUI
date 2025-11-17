using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Button = Microsoft.UI.Xaml.Controls.Button;

namespace Dimmer.WinUI.Animations.UiComponentAnims;

public static class ButtonAnims
{
    public static void AnimateBtnPointerEntered(this Button button, Compositor compositor)
    {
        var btn = (UIElement)button;

        var visual = ElementCompositionPreview.GetElementVisual(btn);


        var anim = compositor.CreateScalarKeyFrameAnimation();
        anim.InsertKeyFrame(1f, 1.2f);
        anim.Duration = TimeSpan.FromMilliseconds(250);
        visual.CenterPoint = new Vector3((float)btn.RenderSize.Width / 2, (float)btn.RenderSize.Height / 2, 0);
        visual.StartAnimation("Scale.X", anim);
        visual.StartAnimation("Scale.Y", anim);

    }
    public static void AnimateBtnPointerExited(this Button button, Compositor compositor)
    {
        var btn = (UIElement)button;
        var visual = ElementCompositionPreview.GetElementVisual(btn);
        var anim = compositor.CreateScalarKeyFrameAnimation();
        anim.InsertKeyFrame(1f, 1f);
        anim.Duration = TimeSpan.FromMilliseconds(150);
        visual.StartAnimation("Scale.X", anim);
        visual.StartAnimation("Scale.Y", anim);

    }
}
