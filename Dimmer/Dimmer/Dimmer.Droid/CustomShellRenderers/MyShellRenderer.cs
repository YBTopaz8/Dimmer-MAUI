using Android.Content;
using AndroidX.DrawerLayout.Widget;
using Microsoft.Maui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.CustomShellRenderers;

public partial class MyShellRenderer : ShellRenderer
{
    private readonly IMauiContext mauiContext;
    private DrawerLayout _shellDrawerLayout;
    protected override IShellItemRenderer CreateShellItemRenderer(ShellItem shellItem)
    {
        return new MyShellItemRenderer(this);
    }
    // This constructor might be needed if MAUI calls it
    //public MyShellRenderer(Context context, IMauiContext mauiContext) : base(context)
    //{
    //    this.mauiContext=mauiContext;
    public MyShellRenderer(Context context) : base(context)
    {
        //this.mauiContext=context;
        //this.myContext=context;
    }
    protected override IShellFlyoutRenderer CreateShellFlyoutRenderer()
    {
        // 'this' is the IShellContext
        // 'AndroidContext' is a protected property from the base ShellRenderer
        if (this.AndroidContext == null)
        {
            // This might happen if the parameterless constructor was called and SetMauiContext hasn't run.
            // It's safer if the constructor ensures AndroidContext is set.
            // For now, let's assume AndroidContext will be available.
            // If not, you'd need to get it from this.MauiContext (IMauiContext from IElementHandler)
            //var mauiCtx = (this as IElementHandler).MauiContext;
            //if (mauiCtx != null)
            //    return new MyShellFlyoutRenderer(this, mauiCtx.Context);

            throw new InvalidOperationException("AndroidContext is null in MyShellRenderer.Cannot create MyShellFlyoutRenderer.");
        }
        return new MyShellFlyoutRenderer(this, this.AndroidContext);
    }

    protected override void OnElementSet(Shell shell)
    {
        base.OnElementSet(shell); // Let the base class do its setup, including creating _flyoutView

        if (FlyoutView?.AndroidView is DrawerLayout drawerLayout)
        {
            _shellDrawerLayout = drawerLayout;

            // Example 1: Change the background of the entire DrawerLayout
            // This color will be visible if your page content area doesn't fill everything
            // or if the flyout is open and has transparent parts.
            if (PublicStats.ShellPageBackgroundColor != null) // Assuming you add this to PublicStats
            {
                _shellDrawerLayout.SetBackgroundColor(PublicStats.ShellPageBackgroundColor.ToPlatform());
            }


            // Example 2: Programmatically set Window Insets handling (if needed beyond default)
            //_shellDrawerLayout.SetFitsSystemWindows(true); // Base class already does this for _frameLayout

            // Example 3: Add a scrim color to the drawer (when it opens)
            drawerLayout.SetScrimColor(PublicStats.FlyoutScrimColor.ToPlatform());

            // You can also find the FrameLayout that hosts the content:
            // The base ShellRenderer creates a _frameLayout.
            // Accessing private/internal fields directly is not possible,
            // but the _flyoutView.AndroidView (DrawerLayout) will contain it.
            // The FrameLayout for content is typically added to the DrawerLayout.
            // You might need to iterate children if there's no direct public accessor.
            // For instance, the first child of DrawerLayout that isn't the flyout menu itself.
            // View mainContentContainer = null;
            // for(int i=0; i < drawerLayout.ChildCount; i++)
            // {
            //     var child = drawerLayout.GetChildAt(i);
            //     // Heuristic: find the FrameLayout that is NOT the flyout panel.
            //     // The flyout panel is usually added with specific LayoutParams (e.g., GravityCompat.Start)
            //     // This is fragile and depends on MAUI's internal ShellFlyoutRenderer structure.
            //     var lp = child.LayoutParameters as DrawerLayout.LayoutParams;
            //     if (lp != null && lp.Gravity == (int)GravityFlags.NoGravity) // Content panel often has NoGravity
            //     {
            //         mainContentContainer = child; // This should be the _frameLayout
            //         break;
            //     }
            // }
            //
            // if (mainContentContainer is Android.Widget.FrameLayout frameLayout && PublicStats.ContentAreaBackgroundColor != null)
            // {
            //    frameLayout.SetBackgroundColor(PublicStats.ContentAreaBackgroundColor.ToPlatform());
            // }
        }

        // To modify the Toolbar, you'd typically do it via IShellToolbarTracker
        // or by finding it within the view hierarchy, which is less stable.
        // The base ShellRenderer sets up a Toolbar through its IShellFlyoutRenderer.
    }

    // Accessor for the FlyoutView's AndroidView (which is _flyoutView.AndroidView in base)
    // The base class has `_flyoutView` as private, but its `AndroidView` property is public on IShellFlyoutRenderer
    protected IShellFlyoutRenderer FlyoutView => GetFieldValue<IShellFlyoutRenderer>("_flyoutView");


    // Helper to access private fields via reflection (use with caution, can break with updates)
    private T GetFieldValue<T>(string fieldName) where T : class
    {
        var fieldInfo = typeof(ShellRenderer).GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        return fieldInfo?.GetValue(this) as T;
    }


    // If you need to modify the toolbar, you can override CreateTrackerForToolbar
    // The tracker is responsible for updating the toolbar based on Shell properties
    protected override IShellToolbarTracker CreateTrackerForToolbar(AndroidX.AppCompat.Widget.Toolbar toolbar)
    {
        // You get the actual Toolbar instance here.
        // You can style it directly before passing it to the base tracker or your custom tracker.
        // Example:
        toolbar.SetBackgroundColor(PublicStats.ToolbarBackgroundColor.ToPlatform());
        toolbar.SetTitleTextColor(PublicStats.ToolbarTitleColor.ToPlatform());

        // If you want to use a custom navigation icon (hamburger or back arrow)
        //MauiContext.Context
        //var context = Android.App.Application.Context;
        var navIcon = toolbar.Context.GetDrawable(Resource.Drawable.hamburgermenu);
        navIcon.SetTint(PublicStats.ToolbarNavigationIconColor.ToPlatform().ToArgb());
        toolbar.NavigationIcon = navIcon;


        toolbar.SetSubtitleTextColor(PublicStats.ToolbarSubtitleColor.ToPlatform());

        return base.CreateTrackerForToolbar(toolbar);
        // Or return new MyCustomShellToolbarTracker(this, toolbar, ((IShellContext)this).CurrentDrawerLayout);
    }
}
//OPTIONAL: Override CreateShellView for global Shell view modifications
//protected override CreateShellView()
//{
//    var shellView = base.CreateShellView();
//    shellView.SetBackgroundColor(PublicStats.ShellPageBackgroundColor.ToPlatform());
//    return shellView;
//}
