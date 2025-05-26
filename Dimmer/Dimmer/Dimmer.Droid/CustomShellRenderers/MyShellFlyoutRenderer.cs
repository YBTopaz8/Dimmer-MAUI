using AndroidX.DrawerLayout.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Android.Content;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Android.Material.BottomNavigation;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Graphics;
using AColor = Android.Graphics.Color;
using AView = Android.Views.View;
using IMenu = Android.Views.IMenu;
using LP = Android.Views.ViewGroup.LayoutParams;
using Orientation = Android.Widget.Orientation;
using View = Android.Views.View;
namespace Dimmer.CustomShellRenderers;
public partial class MyShellFlyoutRenderer : ShellFlyoutRenderer
{
    private bool _isInitialized = false;

    public MyShellFlyoutRenderer(IShellContext shellContext, Context context) : base(shellContext, context)
    {
        // Constructor is a good place for initial setup that doesn't depend on
        // other views being fully attached yet.
        // However, changing background color here might be too early if the base constructor
        // sets it later or if dimensions are not yet known for gradient backgrounds.
    }

    // AttachFlyout is called by the ShellRenderer to add the main content view.
    // This is a good point to apply customizations as the basic structure is being set up.
    protected override void AttachFlyout(IShellContext context, AView content)
    {
        base.AttachFlyout(context, content); // Let the base class attach the content

        // Now 'this' (which is the DrawerLayout) is configured by the base.
        // Apply customizations here.
        ApplyCustomStyling();
        _isInitialized = true;
    }


    // Android views can be measured and laid out multiple times.
    // OnLayout is a reasonable place to apply styling if it depends on final dimensions,
    // but be cautious as it can be called frequently.
    // protected override void OnLayout(bool changed, int l, int t, int r, int b)
    // {
    //    base.OnLayout(changed, l, t, r, b);
    //    if (_isInitialized && changed) // Apply only if layout changed and initialized
    //    {
    //        ApplyCustomStyling();
    //    }
    // }

    private void ApplyCustomStyling()
    {
        // 'this' refers to the MyShellFlyoutRenderer instance, which IS the DrawerLayout.
        if (PublicStats.ShellPageBackgroundColor != null)
        {
            this.SetBackgroundColor(PublicStats.ShellPageBackgroundColor.ToPlatform());
        }

        // Example: Customize Scrim Color (the dimming effect when flyout is open)
        if (PublicStats.FlyoutScrimColor != null) // Add this to PublicStats
        {
            this.SetScrimColor(PublicStats.FlyoutScrimColor.ToPlatform());
        }


        // If you need to access the flyout's content view (the actual menu):
        // The ShellFlyoutRenderer has a protected field `_flyoutContent` of type IShellFlyoutContentRenderer
        // IShellFlyoutContentRenderer flyoutContentViewRenderer = GetFlyoutContentRenderer();
        // if (flyoutContentViewRenderer?.AndroidView != null)
        // {
        //    flyoutContentViewRenderer.AndroidView.SetBackgroundColor(PublicStats.FlyoutMenuBackgroundColor.ToPlatform());
        // }


        // Accessing the main content area (the FrameLayout hosting pages)
        // This is tricky because `_content` (AView content in AttachFlyout) is the direct child
        // that hosts the fragments.
        // If you need to style the container that holds the pages (_frameLayout in ShellRenderer which becomes `_content` here):
        // The `_content` parameter in AttachFlyout is the FrameLayout.
        var pageHostContainer = GetContentView();
        if (pageHostContainer != null && PublicStats.ContentAreaBackgroundColor != null)
        {
            pageHostContainer.SetBackgroundColor(PublicStats.ContentAreaBackgroundColor.ToPlatform());
        }
    }


    // Helper methods to access protected fields from the base class via reflection
    // (Use with caution, as internal structure might change in future MAUI versions)
    private IShellFlyoutContentRenderer GetFlyoutContentRenderer()
    {
        var fieldInfo = typeof(ShellFlyoutRenderer).GetField("_flyoutContent",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return fieldInfo?.GetValue(this) as IShellFlyoutContentRenderer;
    }

    private AView GetContentView()
    {
        // The 'content' view passed to AttachFlyout is stored in a protected field '_content'.
        var fieldInfo = typeof(ShellFlyoutRenderer).GetField("_content",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return fieldInfo?.GetValue(this) as AView;
    }

    // Remember to also update your MyShellRenderer to use this:
    // In MyShellRenderer.cs
    // protected override IShellFlyoutRenderer MyShellRenderer()
    // {
    //     return new MyShellFlyoutRenderer(this, AndroidContext);
    // }
}