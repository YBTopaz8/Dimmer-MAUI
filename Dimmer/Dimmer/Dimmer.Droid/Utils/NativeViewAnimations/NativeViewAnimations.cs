using Android.Animation;
using Android.OS;
using Android.Views.Animations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using View = Android.Views.View;

namespace Dimmer.Utils.NativeViewAnimations;
public static class NativeViewAnimations
{
    public static void FadeIn(Microsoft.Maui.Controls.View mauiView, long duration = -1, ITimeInterpolator interpolator = null, Action onEndAction = null)
    {
        var nativeView = PlatformViewHelper.GetNativeView(mauiView);
        if (nativeView == null)
            return;

        duration = duration == -1 ? PublicStats.DefaultAnimationDurationMs : duration;
        interpolator = interpolator ?? PublicStats.DefaultInterpolator;

        nativeView.Alpha = 0f;
        nativeView.Visibility = ViewStates.Visible;
        nativeView.Animate()
            .Alpha(1f)
            .SetDuration(duration)
            .SetInterpolator(interpolator)
            .WithEndAction(new Java.Lang.Runnable(onEndAction ?? (() => { })))
            .Start();
    }

    public static void FadeOut(Microsoft.Maui.Controls.View mauiView, long duration = -1, ITimeInterpolator interpolator = null, bool hideWhenDone = true, Action onEndAction = null)
    {
        var nativeView = PlatformViewHelper.GetNativeView(mauiView);
        if (nativeView == null || nativeView.Visibility == ViewStates.Gone)
        {
            onEndAction?.Invoke();
            return;
        }

        duration = duration == -1 ? PublicStats.DefaultAnimationDurationMs : duration;
        interpolator = interpolator ?? PublicStats.DefaultInterpolator;

        nativeView.Animate()
            .Alpha(0f)
            .SetDuration(duration)
            .SetInterpolator(interpolator)
            .WithEndAction(new Java.Lang.Runnable(() =>
            {
                if (hideWhenDone)
                    nativeView.Visibility = ViewStates.Gone;
                onEndAction?.Invoke();
            }))
            .Start();
    }

    public static void SlideInFromBottom(Microsoft.Maui.Controls.View mauiView, long duration = -1, ITimeInterpolator interpolator = null, float slideDistanceDp = 50f, Action onEndAction = null)
    {
        var nativeView = PlatformViewHelper.GetNativeView(mauiView);
        if (nativeView == null)
            return;

        duration = duration == -1 ? PublicStats.DefaultAnimationDurationMs : duration;
        interpolator = interpolator ?? PublicStats.DefaultInterpolator;
        float slideDistancePx = nativeView.Context.ToPixels(slideDistanceDp);

        nativeView.TranslationY = slideDistancePx;
        nativeView.Alpha = 0f;
        nativeView.Visibility = ViewStates.Visible;
        nativeView.Animate()
            .TranslationY(0f)
            .Alpha(1f)
            .SetDuration(duration)
            .SetInterpolator(interpolator)
            .WithEndAction(new Java.Lang.Runnable(onEndAction ?? (() => { })))
            .Start();
    }

    public static void SlideOutToBottom(Microsoft.Maui.Controls.View mauiView, long duration = -1, ITimeInterpolator interpolator = null, float slideDistanceDp = 50f, bool hideWhenDone = true, Action onEndAction = null)
    {
        var nativeView = PlatformViewHelper.GetNativeView(mauiView);
        if (nativeView == null || nativeView.Visibility == ViewStates.Gone)
        {
            onEndAction?.Invoke();
            return;
        }

        duration = duration == -1 ? PublicStats.DefaultAnimationDurationMs : duration;
        interpolator = interpolator ?? PublicStats.DefaultInterpolator;
        float slideDistancePx = nativeView.Context.ToPixels(slideDistanceDp);

        nativeView.Animate()
            .TranslationY(slideDistancePx)
            .Alpha(0f)
            .SetDuration(duration)
            .SetInterpolator(interpolator)
            .WithEndAction(new Java.Lang.Runnable(() =>
            {
                if (hideWhenDone)
                    nativeView.Visibility = ViewStates.Gone;
                nativeView.TranslationY = 0f; // Reset for next time
                onEndAction?.Invoke();
            }))
            .Start();
    }

    // Add SlideInFromLeft, Right, Top and corresponding SlideOut methods similarly

    public static void Bounce(Microsoft.Maui.Controls.View mauiView, float scaleFactor = 1.1f, long durationPerBounceMs = 150, ITimeInterpolator interpolator = null, Action onEndAction = null)
    {
        var nativeView = PlatformViewHelper.GetNativeView(mauiView);
        if (nativeView == null)
            return;

        interpolator = interpolator ?? new OvershootInterpolator(2f); // Good for bounce

        nativeView.Animate()
            .ScaleX(scaleFactor)
            .ScaleY(scaleFactor)
            .SetDuration(durationPerBounceMs)
            .SetInterpolator(interpolator)
            .WithEndAction(new Java.Lang.Runnable(() =>
            {
                nativeView.Animate()
                    .ScaleX(1f)
                    .ScaleY(1f)
                    .SetDuration(durationPerBounceMs)
                    .SetInterpolator(interpolator) // Can use a different one for return
                    .WithEndAction(new Java.Lang.Runnable(onEndAction ?? (() => { })))
                    .Start();
            }))
            .Start();
    }

    public static void Grow(Microsoft.Maui.Controls.View mauiView, float toScale = 1.5f, long duration = -1, ITimeInterpolator interpolator = null, Action onEndAction = null)
    {
        var nativeView = PlatformViewHelper.GetNativeView(mauiView);
        if (nativeView == null)
            return;
        duration = duration == -1 ? PublicStats.DefaultAnimationDurationMs : duration;
        interpolator = interpolator ?? PublicStats.DefaultInterpolator;

        nativeView.Visibility = ViewStates.Visible;
        nativeView.ScaleX = 0f;
        nativeView.ScaleY = 0f;
        nativeView.Animate()
            .ScaleX(toScale)
            .ScaleY(toScale)
            .SetDuration(duration)
            .SetInterpolator(interpolator)
            .WithEndAction(new Java.Lang.Runnable(onEndAction ?? (() => { })))
            .Start();
    }


    public static void Shrink(Microsoft.Maui.Controls.View mauiView, float fromScale = 1.0f, long duration = -1, ITimeInterpolator interpolator = null, bool hideWhenDone = true, Action onEndAction = null)
    {
        var nativeView = PlatformViewHelper.GetNativeView(mauiView);
        if (nativeView == null || nativeView.Visibility == ViewStates.Gone)
        {
            onEndAction?.Invoke();
            return;
        }
        duration = duration == -1 ? PublicStats.DefaultAnimationDurationMs : duration;
        interpolator = interpolator ?? PublicStats.DefaultInterpolator;

        nativeView.ScaleX = fromScale;
        nativeView.ScaleY = fromScale;
        nativeView.Animate()
            .ScaleX(0f)
            .ScaleY(0f)
            .SetDuration(duration)
            .SetInterpolator(interpolator)
            .WithEndAction(new Java.Lang.Runnable(() =>
            {
                if (hideWhenDone)
                    nativeView.Visibility = ViewStates.Gone;
                nativeView.ScaleX = fromScale; // Reset for next time if not hidden
                nativeView.ScaleY = fromScale;
                onEndAction?.Invoke();
            }))
            .Start();
    }

    public static void Rotate(Microsoft.Maui.Controls.View mauiView, float degrees, long duration = -1, ITimeInterpolator interpolator = null, Action onEndAction = null)
    {
        var nativeView = PlatformViewHelper.GetNativeView(mauiView);
        if (nativeView == null)
            return;
        duration = duration == -1 ? PublicStats.DefaultAnimationDurationMs : duration;
        interpolator = interpolator ?? PublicStats.DefaultInterpolator;

        nativeView.Animate()
            .RotationBy(degrees) // Or Rotation(targetDegrees)
            .SetDuration(duration)
            .SetInterpolator(interpolator)
            .WithEndAction(new Java.Lang.Runnable(onEndAction ?? (() => { })))
            .Start();
    }

}
