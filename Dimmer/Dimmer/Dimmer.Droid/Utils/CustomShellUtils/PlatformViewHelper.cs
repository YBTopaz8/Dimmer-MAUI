using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AView = Android.Views.View;

namespace Dimmer.Utils.CustomShellUtils;

public static class PlatformViewHelper
{
    public static AView GetNativeView(VisualElement mauiView) // Changed to VisualElement for broader compatibility
    {
        if (mauiView == null)
        {
            Debug.WriteLine("GetNativeView: MAUI View is null.");
            return null;
        }

        // 1. Check if Handler and PlatformView already exist
        if (mauiView.Handler?.PlatformView is AView existingPlatformView)
        {
            return existingPlatformView;
        }

        // 2. If not, try to ensure a handler is created and get the platform view
        // Getting the MauiContext:
        // A View's MauiContext is typically inherited from its parent or the window.
        // If the view is not yet part of the visual tree, its Handler.MauiContext might be null.
        IMauiContext mauiContext = mauiView.Handler?.MauiContext ??
                                   (mauiView.Parent as Element)?.Handler?.MauiContext ??
                                   Application.Current?.Windows.FirstOrDefault()?.Handler?.MauiContext;

        if (mauiContext == null)
        {
            Debug.WriteLine($"GetNativeView: Could not determine IMauiContext for MAUI View: {mauiView.GetType().Name}. Ensure the view is part of the visual tree or has a Handler.");
            // As a last resort, if you are sure there's a global context (e.g., for views added very late or programmatically without a parent yet)
            // This is less safe as it assumes a single window scenario or that the view will eventually belong to it.
            // mauiContext = MauiApplication.Current?.Application?.Handler?.MauiContext; // Might not always work
            if (MauiApplication.Current?.Services != null)
            {
                mauiContext = MauiApplication.Current.Services.GetService<IMauiContext>();
            }

            if (mauiContext == null)
            {
                Debug.WriteLine("GetNativeView: Fallback to global IMauiContext also failed.");
                return null;
            }
        }

        // Use ToPlatform to ensure the handler and platform view are created
        // This implicitly uses the provided IMauiContext
        try
        {
            var platformView = mauiView.ToPlatform(mauiContext);
            return platformView as AView;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GetNativeView: Exception during ToPlatform for {mauiView.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    // Overload for Microsoft.Maui.Controls.View if specifically needed,
    // but VisualElement is usually what you'll have.
    public static AView GetNativeView(Microsoft.Maui.Controls.View mauiView)
    {
        return GetNativeView(mauiView as VisualElement);
    }
}
