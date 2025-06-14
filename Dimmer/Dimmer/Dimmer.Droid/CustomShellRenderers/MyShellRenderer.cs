using AndroidX.Core.View;
using AndroidX.DrawerLayout.Widget;
using AndroidX.Fragment.App;

namespace Dimmer.CustomShellRenderers;

public partial class MyShellRenderer : ShellRenderer
{
    private readonly IMauiContext mauiContext;
    private DrawerLayout _shellDrawerLayout;
    private Android.Views.View _contentView;
    IShellItemRenderer _currentView;
    bool _disposed;
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
    public MyShellRenderer()
    {

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

    public static Fragment currentPageFragment;

    protected override void SwitchFragment(FragmentManager manager, global::Android.Views.View targetView, ShellItem newItem, bool animate = true)
    {

        var animation = HelperConverter.GetRoot();

        var previousView = _currentView;
        _currentView = CreateShellItemRenderer(newItem);
        _currentView.ShellItem = newItem;
        Fragment? fragment = _currentView.Fragment;

        FragmentTransaction transaction = manager.BeginTransaction();


        if (animate)
            transaction.SetCustomAnimations(Resource.Animation.m3_bottom_sheet_slide_in, Resource.Animation.m3_bottom_sheet_slide_out);

        if (animation.AbovePage == Utils.CustomShellUtils.Enums.PageType.NextPage && animate)
        {
            transaction.Add(targetView.Id, fragment);
            Task.Run(async () =>
            {
                await Task.Delay(animation.Duration);
                FragmentTransaction transactionTemp = manager.BeginTransaction();
                transactionTemp.Replace(fragment.Id, fragment);
                transactionTemp.CommitAllowingStateLoss();
            });
        }
        else
        {
            transaction.Replace(targetView.Id, fragment);
        }

        if (previousView == null)
        {
            transaction.SetReorderingAllowed(true);
        }

        transaction.CommitAllowingStateLoss();


        void OnDestroyed(object? sender, EventArgs args)
        {

            previousView.Destroyed -= OnDestroyed;
            previousView.Dispose();
            previousView = null;
        }

        if (previousView != null)
            previousView.Destroyed += OnDestroyed;
    }




    protected override void OnElementSet(Shell shell)
    {
        base.OnElementSet(shell); // Let the base class do its , including creating _flyoutView


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

            ViewCompat.SetElevation(_shellDrawerLayout, 16f);
            // Example 2: Programmatically set Window Insets handling (if needed beyond default)
            //_shellDrawerLayout.SetFitsSystemWindows(true); // Base class already does this for _frameLayout

            // Example 3: Add a scrim color to the drawer (when it opens)
            drawerLayout.SetScrimColor(PublicStats.FlyoutScrimColor.ToPlatform());
            for (int i = 0; i < _shellDrawerLayout.ChildCount; i++)
            {
                var child = _shellDrawerLayout.GetChildAt(i);
                var lp = child.LayoutParameters as DrawerLayout.LayoutParams;
                // The content view usually has Gravity NO_GRAVITY or is not the one with START/END
                if (lp != null && lp.Gravity == (int)GravityFlags.NoGravity)
                {
                    _contentView = child; // This is likely the FrameLayout holding page content
                    break;
                }
                // Fallback if the above isn't reliable enough (depends on MAUI internal structure)
                // Check if it's NOT the flyout view provided by the DrawerListener later
            }
            if (_contentView == null && _shellDrawerLayout.ChildCount > 0)
            {
                // Often the content view is the 0-indexed child IF the flyout is added later
                // or if MAUI's ShellRenderer adds content first.
                // This is less reliable than checking gravity.
                _contentView = _shellDrawerLayout.GetChildAt(0);
            }
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
