using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Visibility = Microsoft.UI.Xaml.Visibility;

namespace Dimmer.WinUI.Utils;

public class StoreLikeCardAnimation
{
    private FrameworkElement _card;
    private FrameworkElement _expandContent;
    private Visual _cardVisual;
    private Visual _contentVisual;
    private Compositor _compositor;

    public void Initialize(FrameworkElement card, FrameworkElement expandContent)
    {
        _card = card;
        _expandContent = expandContent;
        _cardVisual = ElementCompositionPreview.GetElementVisual(card);
        _contentVisual = ElementCompositionPreview.GetElementVisual(expandContent);
        _compositor = _cardVisual.Compositor;

        // Initially hide expandable content
        _expandContent.Opacity = 0;
        _expandContent.Visibility = Visibility.Collapsed;

        SetupHoverAnimation();
    }

    private void SetupHoverAnimation()
    {
        // Create hover effect with shadow
        var shadow = _compositor.CreateDropShadow();
        shadow.Color = Colors.Black;
        shadow.Opacity = 0.2f;
        shadow.BlurRadius = 20f;
        shadow.Offset = new Vector3(0, 5, 0);
        
        //_cardVisual.Shadow = shadow;

        // Setup implicit animations for smooth transitions
        SetupImplicitAnimations();
    }

    private void SetupImplicitAnimations()
    {
        var animations = _compositor.CreateImplicitAnimationCollection();

        // Animate scale on hover
        var scaleAnimation = _compositor.CreateVector3KeyFrameAnimation();
        scaleAnimation.Target = "Scale";
        scaleAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
        scaleAnimation.Duration = TimeSpan.FromMilliseconds(250);

        animations["Scale"] = scaleAnimation;
        _cardVisual.ImplicitAnimations = animations;
    }

    public void ExpandCard()
    {
        // Show expandable content with fade in
        _expandContent.Visibility = Visibility.Visible;

        var fadeIn = _compositor.CreateScalarKeyFrameAnimation();
        fadeIn.InsertKeyFrame(0f, 0f);
        fadeIn.InsertKeyFrame(1f, 1f, _compositor.CreateCubicBezierEasingFunction(
            new Vector2(0.4f, 0f), new Vector2(0.2f, 1f)));
        fadeIn.Duration = TimeSpan.FromMilliseconds(200);

        _contentVisual.StartAnimation("Opacity", fadeIn);

        // Animate card expansion
        var scaleAnimation = _compositor.CreateVector3KeyFrameAnimation();
        scaleAnimation.InsertKeyFrame(0f, Vector3.One);
        scaleAnimation.InsertKeyFrame(1f, new Vector3(1.1f, 1.1f, 1f),
            _compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.4f, 0f), new Vector2(0.2f, 1f)));
        scaleAnimation.Duration = TimeSpan.FromMilliseconds(300);

        _cardVisual.StartAnimation("Scale", scaleAnimation);
    }

    public void CollapseCard()
    {
        // Fade out expandable content
        var fadeOut = _compositor.CreateScalarKeyFrameAnimation();
        fadeOut.InsertKeyFrame(0f, 1f);
        fadeOut.InsertKeyFrame(1f, 0f, _compositor.CreateCubicBezierEasingFunction(
            new Vector2(0.4f, 0f), new Vector2(0.2f, 1f)));
        fadeOut.Duration = TimeSpan.FromMilliseconds(150);

       
        //fadeOut.Completed += (s, e) =>
        //{
        //    _expandContent.Visibility = Visibility.Collapsed;
        //};

        _contentVisual.StartAnimation("Opacity", fadeOut);

        // Return to original scale
        var scaleAnimation = _compositor.CreateVector3KeyFrameAnimation();
        scaleAnimation.InsertKeyFrame(0f, _cardVisual.Scale);
        scaleAnimation.InsertKeyFrame(1f, Vector3.One,
            _compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.4f, 0f), new Vector2(0.2f, 1f)));
        scaleAnimation.Duration = TimeSpan.FromMilliseconds(250);

        _cardVisual.StartAnimation("Scale", scaleAnimation);
    }
}