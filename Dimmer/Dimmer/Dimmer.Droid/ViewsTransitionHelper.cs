using AndroidX.Transitions;

using Google.Android.Material.Transition;

namespace Dimmer;
public static class AndroidTransitionHelper
{
    /// <summary>
    /// Performs a Material Design Container Transform between two MAUI views.
    /// This creates the "morphing" effect between a list item and a detail view.
    /// </summary>
    /// <param name="mauiLayout">The root MAUI Layout containing both views.</param>
    /// <param name="startView">The starting MAUI view (e.g., the smaller list item or its container).</param>
    /// <param name="endView">The ending MAUI view (e.g., the larger detail page container).</param>
    /// <param name="duration">Animation duration in milliseconds.</param>
    public static void BeginMaterialContainerTransform(
        Layout mauiLayout,
        Microsoft.Maui.Controls.View startView,
        Microsoft.Maui.Controls.View endView,
        long duration = 450)
    {
        if (mauiLayout?.Handler?.PlatformView is not ViewGroup nativeParent ||
            startView?.Handler?.PlatformView is not Android.Views.View nativeStartView ||
            endView?.Handler?.PlatformView is not Android.Views.View nativeEndView)
        {
            // If views aren't ready, we can't perform the transition.
            // Just swap visibility instantly as a fallback.
            startView.IsVisible = false;
            endView.IsVisible = true;
            return;
        }

        // This is it. The magic class from the Material Components library.
        var transform = new MaterialContainerTransform
        {
            // This is crucial. It tells the transform which views are the start and end.
            StartView = nativeStartView,
            EndView = nativeEndView,

            // You can target specific elements within the start/end views if needed,
            // but for full-view morphing, this is enough.


            // Set the scrim to transparent to see the content fade underneath.
            // The scrim is the faded background that appears during the transition.
            ScrimColor = Android.Graphics.Color.Transparent
        };

        // To make it even more like the article, you can configure colors.
        // This ensures the expanding container has a consistent background color.
        transform.SetAllContainerColors(global::Android.Resource.Attribute.ColorAccent);

        // Tell the TransitionManager to use our shiny new Material Transform.
        TransitionManager.BeginDelayedTransition(nativeParent, transform);

        // With the transition primed, now we perform the layout change.
        // The StartView will be hidden, and the EndView will be shown.
        // The TransitionManager will intercept this and play our animation.
        startView.IsVisible = false;
        endView.IsVisible = true;
    }
}