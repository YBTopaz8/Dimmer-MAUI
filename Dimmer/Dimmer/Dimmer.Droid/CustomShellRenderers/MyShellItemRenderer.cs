﻿using Android.OS;

using AndroidX.Fragment.App;

using Dimmer.Utils.PageAnimations;

using Google.Android.Material.BottomNavigation;

using AColor = Android.Graphics.Color;
using LP = Android.Views.ViewGroup.LayoutParams;
using Orientation = Android.Widget.Orientation;
using View = Android.Views.View;

namespace Dimmer.CustomShellRenderers;
public partial class MyShellItemRenderer : ShellItemRenderer
{
    private BottomSheetDialog? _moreBottomSheetDialogInstance; // Keep a reference
    private BottomNavigationView _theBottomViewInstance;
    private FrameLayout _theNavigationAreaInstance;
    public MyShellItemRenderer(IShellContext context)
        : base(context)
    {
        ShellStylingBridge.BackgroundColorChanged += OnBridgeBackgroundColorChanged;
        ShellStylingBridge.TextColorChanged += OnBridgeTextColorChanged;
        ShellStylingBridge.ElevationChanged += OnBridgeElevationChanged;
        ShellStylingBridge.TabBehaviorChanged += OnBridgeTabBehaviorChanged;
        ShellStylingBridge.ShellElementRefreshRequested += OnBridgeRefreshRequested;
        ShellStylingBridge.AnimationSettingsChanged += OnBridgeAnimationSettingsChanged;
    }

    // --- Event Handlers from ShellStylingBridge ---
    private void OnBridgeBackgroundColorChanged(object? sender, ColorChangedEventArgs e)
    {
        if (e.TargetElement == ShellElement.BottomNavBar && _theBottomViewInstance != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _theBottomViewInstance.SetBackgroundColor(e.NewColor.ToPlatform());
                _theBottomViewInstance.Invalidate();
            });
        }
        // Handle MoreSheetBackground if e.TargetElement matches
    }

    private void OnBridgeTextColorChanged(object? sender, ColorChangedEventArgs e)
    {
        if (e.TargetElement == ShellElement.BottomNavItem && _theBottomViewInstance != null && e.SecondaryColor != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateBottomNavItemColors(e.NewColor.ToPlatform(), e.SecondaryColor.ToPlatform());
            });
        }
        // Handle MoreSheetItem text color
    }

    private void OnBridgeElevationChanged(object? sender, ElevationChangedEventArgs e)
    {
        if (e.TargetElement == ShellElement.BottomNavBar && _theBottomViewInstance != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _theBottomViewInstance.Elevation = Context.ToPixels(e.NewElevationDp);
                _theBottomViewInstance.Invalidate(); // May not be strictly needed for elevation but good practice
            });
        }
    }

    private void OnBridgeTabBehaviorChanged(object? sender, TabBehaviorChangedEventArgs e)
    {
        if (e.NumberOfVisibleTabs.HasValue)
        {
            PublicStats.NumberOfTabsToShow = e.NumberOfVisibleTabs.Value;
            // This is the tricky part:
            // Changing NumberOfTabsToShow live requires rebuilding the BottomNavigationView menu
            // or significantly altering its items.
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_theBottomViewInstance != null && ShellItem != null)
                {
                    // Option A: Force re-setup (might have flicker)
                    // _theBottomViewInstance.Menu.Clear(); // Clear existing items
                    // SetupMenu(_theBottomViewInstance.Menu, PublicStats.NumberOfTabsToShow, ShellItem); // Re-populate

                    // Option B: More complex logic to add/remove "More" item and adjust others
                    // This is highly dependent on how SetupMenu works and is generally complex.
                    // For now, a full refresh or simpler update is more feasible.
                    // A simpler approach might be to just refresh the current view, assuming
                    // a subsequent navigation or app restart will pick up the new count.
                    // For a true live update of this, you'd need deep menu manipulation.
                    System.Diagnostics.Debug.WriteLine($"TODO: Implement live update for NumberOfVisibleTabs. Current value: {PublicStats.NumberOfTabsToShow}. Requires menu rebuild.");
                    // Potentially trigger a Shell navigation to force some re-evaluation or
                    // call a method on the MAUI Shell to refresh its current item view.
                    _theBottomViewInstance.Invalidate(); // Redraw at least
                                                         // Requesting a layout might be needed if item visibility changes
                    _theBottomViewInstance.RequestLayout();
                }
            });
        }
    }

    private static void OnBridgeAnimationSettingsChanged(object? sender, AnimationSettingsChangedEventArgs e)
    {
        Console.WriteLine(sender.GetType());
        Console.WriteLine(e.DurationMs);
        Console.WriteLine(e.TargetAnimation);
        if (e.TargetAnimation == ShellAnimationTarget.TabSwitchAnimation)
        {
            if (e.DurationMs.HasValue)
                PublicStats.TabSwitchAnimationDurationMs = e.DurationMs.Value; // Add to PublicStats
                                                                               // Interpolator changes would also be set in PublicStats here
            System.Diagnostics.Debug.WriteLine($"Tab switch animation duration updated to: {PublicStats.TabSwitchAnimationDurationMs}");
        }
        else
        {
            Console.WriteLine(sender.GetType());
            Console.WriteLine(e.DurationMs);
            Console.WriteLine(e.TargetAnimation);
        }
    }


    private void OnBridgeRefreshRequested(object? sender, ShellElement e)
    {
        if (e == ShellElement.BottomNavBar && _theBottomViewInstance != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ApplyInitialBottomNavStyles(); // Re-apply all styles from PublicStats
                                               // If CreateMoreBottomSheet styles depend on PublicStats, and it's visible, you might need to recreate/restyle it.
            });
        }
    }

    protected override void OnShellSectionChanged()
    {
        if (_theNavigationAreaInstance != null && Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
        {
            // Simple Fade out
            _theNavigationAreaInstance.Animate()!
                .Alpha(0f)
                .SetDuration(PublicStats.TabSwitchAnimationDurationMs) // Use from PublicStats
                .SetInterpolator(PublicStats.AccelerateInterpolator) // Use from PublicStats
                .WithEndAction(new Java.Lang.Runnable(() =>
                {
                    base.OnShellSectionChanged(); // Swaps content
                    _theNavigationAreaInstance.Alpha = 0f;
                    _theNavigationAreaInstance.Animate()!
                        .Alpha(1f)
                        .SetDuration(PublicStats.EndTabSwitchAnimationDurationMs)
                        .SetInterpolator(PublicStats.DecelerateInterpolator)
                        .SetStartDelay(50)
                        .Start();
                }))
                .Start();
        }
        else
        {
            base.OnShellSectionChanged();
        }
    }
    public override void OnDestroy() // Or protected override void Dispose(bool disposing)
    {
        ShellStylingBridge.BackgroundColorChanged -= OnBridgeBackgroundColorChanged;
        ShellStylingBridge.TextColorChanged -= OnBridgeTextColorChanged;
        ShellStylingBridge.ElevationChanged -= OnBridgeElevationChanged;
        ShellStylingBridge.TabBehaviorChanged -= OnBridgeTabBehaviorChanged;
        ShellStylingBridge.ShellElementRefreshRequested -= OnBridgeRefreshRequested;
        ShellStylingBridge.AnimationSettingsChanged -= OnBridgeAnimationSettingsChanged;

        // ... (your existing OnDestroy logic for _moreBottomSheetDialogInstance) ...
        base.OnDestroy();
    }


    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var outerLayoutView = base.OnCreateView(inflater, container, savedInstanceState);

        if (outerLayoutView is ViewGroup outerViewGroup)
        {
            for (int i = 0; i < outerViewGroup.ChildCount; i++)
            {
                var child = outerViewGroup.GetChildAt(i);
                if (child is BottomNavigationView bnv)
                {
                    _theBottomViewInstance = bnv;
                }
                else if (child is FrameLayout fl && _theNavigationAreaInstance == null) // Assuming nav area is a FrameLayout
                {
                    // This is a bit heuristic, if there are multiple FrameLayouts,
                    // you might need a more specific way to identify it (e.g., ID if MAUI sets one)
                    // Often the navigation area is added before the bottom bar or has FillParent/MatchParent
                    _theNavigationAreaInstance = fl;
                }
            }
        }

        if (_theBottomViewInstance != null)
        {
            _theBottomViewInstance.SetBackgroundColor(PublicStats.BottomNavBackgroundColor);
            _theBottomViewInstance.Elevation = Context.ToPixels(PublicStats.BottomNavElevationDp);

            int[][] states = new int[][] {
                new int[] { global::Android.Resource.Attribute.StateChecked },
                new int[] { -global::Android.Resource.Attribute.StateChecked }
            };
            int[] colors = new int[] {
                PublicStats.BottomNavItemCheckedColor.ToArgb(),
                PublicStats.BottomNavItemUncheckedColor.ToArgb()
            };
            var colorStateList = new Android.Content.Res.ColorStateList(states, colors);
            _theBottomViewInstance.ItemIconTintList = colorStateList;
            _theBottomViewInstance.ItemTextColor = colorStateList;
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[MyShellItemRenderer] BottomNavigationView instance NOT found in OnCreateView. Customizations skipped.");
        }
        ApplyInitialBottomNavStyles();
        return outerLayoutView;
    }
    private void ApplyInitialBottomNavStyles()
    {
        if (_theBottomViewInstance == null)
            return;

        _theBottomViewInstance.SetBackgroundColor(PublicStats.BottomNavBackgroundColor);
        _theBottomViewInstance.Elevation = Context.ToPixels(PublicStats.BottomNavElevationDp);
        UpdateBottomNavItemColors(PublicStats.BottomNavItemCheckedColor, PublicStats.BottomNavItemUncheckedColor);
    }
    private void UpdateBottomNavItemColors(AColor activeColor, AColor inactiveColor)
    {
        if (_theBottomViewInstance == null)
            return;
        int[][] states = new int[][] {
                new int[] { global::Android.Resource.Attribute.StateChecked },
                new int[] { -global::Android.Resource.Attribute.StateChecked }
            };
        int[] colors = new int[] { activeColor.ToArgb(), inactiveColor.ToArgb() };
        var colorStateList = new ColorStateList(states, colors);
        _theBottomViewInstance.ItemIconTintList = colorStateList;
        _theBottomViewInstance.ItemTextColor = colorStateList;
        _theBottomViewInstance.Invalidate(); // Request redraw
    }
    void OnMoreItemSelected(int shellSectionIndex, BottomSheetDialog dialog)
    {
        OnMoreItemSelected(ShellItemController.GetItems()[shellSectionIndex], dialog);
    }

    public static bool IsDisposed(Java.Lang.Object obj)
    {
        return obj.Handle == nint.Zero;
    }

    protected override BottomSheetDialog CreateMoreBottomSheet(Action<int, BottomSheetDialog> selectCallback)
    {
        _moreBottomSheetDialogInstance = new BottomSheetDialog(Context!);
        var bottomSheetLayout = new LinearLayout(Context);


        using (var bottomShellLP = new LP(LP.MatchParent, LP.WrapContent))
            bottomSheetLayout.LayoutParameters = bottomShellLP;


        bottomSheetLayout.Orientation = Orientation.Vertical;

        // handle the more tab
        var items = ((IShellItemController)ShellItem).GetItems();

        for (int i = PublicStats.NumberOfTabsToShow; i < items.Count; i++)
        {
            var closure_i = i;
            var shellContent = items[i];

            using (var innerLayout = new LinearLayout(Context))
            {
                innerLayout.SetClipToOutline(true);
                innerLayout.SetBackground(CreateItemBackgroundDrawable());
                innerLayout.SetPadding(0, (int)Context.ToPixels(6), 0, (int)Context.ToPixels(6));
                innerLayout.Orientation = Orientation.Horizontal;
                using (var param = new LP(LP.MatchParent, LP.WrapContent))
                    innerLayout.LayoutParameters = param;

                // technically the unhook isn't needed
                // we dont even unhook the events that dont fire
                void clickCallback(object? s, EventArgs e)
                {
                    selectCallback(closure_i, _moreBottomSheetDialogInstance);
                    if (!IsDisposed(innerLayout))
                        innerLayout.Click -= clickCallback;
                }

                innerLayout.Click += clickCallback;

                var image = new ImageView(Context);
                var lp = new LinearLayout.LayoutParams((int)Context.ToPixels(32), (int)Context.ToPixels(32))
                {

                    LeftMargin = PublicStats.IconMarginLeftDp,
                    RightMargin = PublicStats.IconMarginRightDp,
                    TopMargin = PublicStats.IconMarginTopDp,
                    BottomMargin = PublicStats.IconMarginBottomDp,
                    Gravity = GravityFlags.Center
                };
                image.LayoutParameters = lp;

                IServiceProvider services = ShellContext.Shell.Handler!.MauiContext!.Services;

                shellContent.Icon.LoadImage(ShellContext.Shell.Handler.MauiContext,
                    (result) =>
                    {
                        image.SetImageDrawable(result?.Value);
                        if (result?.Value != null)
                        {
                            PublicStats.IconTintColor = Colors.Black.MultiplyAlpha(0.6f).ToPlatform();
                            result.Value.SetTint(PublicStats.IconTintColor);
                        }
                    });

                innerLayout.AddView(image);

                using (var text = new TextView(Context))
                {
                    text.Typeface = services.GetRequiredService<IFontManager>()
                        .GetTypeface(Microsoft.Maui.Font.OfSize("AleySans", 0.0));

                    // Change textcolor here
                    text.SetTextColor(PublicStats.TabTitleTextColor);
                    text.Text = shellContent.Title;
                    lp = new LinearLayout.LayoutParams(0, LP.WrapContent)
                    {
                        Gravity = GravityFlags.Center,
                        Weight = PublicStats.TabTitleLayoutWeight,
                    };
                    text.LayoutParameters = lp;
                    lp.Dispose();

                    innerLayout.AddView(text);
                }

                bottomSheetLayout.AddView(innerLayout);
            }
        }
        _moreBottomSheetDialogInstance.DismissWithAnimation=true;


        _moreBottomSheetDialogInstance.SetContentView(bottomSheetLayout);

        return _moreBottomSheetDialogInstance;
    }
    protected static string GetPageIdentifier(Page page)
    {
        // Use a consistent identifier for the page type.
        // Routing.GetRoute(page) might work if pages are always navigated via route.
        // Otherwise, page.GetType().Name is usually safe.
        return page.GetType().Name;
    }
    protected override void OnDisplayedPageChanged(Page newPage, Page oldPage)
    {
        base.OnDisplayedPageChanged(newPage, oldPage);
    }

    protected override Task<bool> HandleFragmentUpdate(ShellNavigationSource navSource, ShellSection shellSection, Page page, bool animated)
    {
        return base.HandleFragmentUpdate(navSource, shellSection, page, animated);
    }

    protected override void SetupAnimation(ShellNavigationSource navSource, FragmentTransaction t, Page page)
    {
        // On Pop, animations are handled automatically by the FragmentManager based on the original Push.
        //if (navSource == ShellNavigationSource.Pop || navSource == ShellNavigationSource.PopToRoot)
        //{
        //    base.SetupAnimation(navSource, t, page);
        //    return;
        //}

        var _animationService = IPlatformApplication.Current?.Services.GetService<IAnimationService>();

        switch (navSource)
        {
            case ShellNavigationSource.Push:
                int enterAnim, exitAnim, popEnterAnim, popExitAnim;

                // Handle special cases first
                if (page is HomePage)
                {
                    enterAnim = _animationService.GetHomePagePushEnterAnimation().ResourceId;
                    exitAnim = _animationService.GetHomePagePushExitAnimation().ResourceId;
                    popEnterAnim = _animationService.GetHomePagePopEnterAnimation().ResourceId;
                    popExitAnim = _animationService.GetDefaultPopExitAnimation().ResourceId;
                }
                else
                {
                    // Get the full profile for the page, which handles loading from Preferences or getting defaults.
                    var profile = AnimationManager.GetPageAnimations(page.GetType(), _animationService);
                    enterAnim = profile.PushEnter.ResourceId;
                    exitAnim = profile.PushExit.ResourceId;
                    popEnterAnim = profile.PopEnter.ResourceId;
                    popExitAnim = profile.PopExit.ResourceId;
                }

                t.SetCustomAnimations(enterAnim, exitAnim, popEnterAnim, popExitAnim);
                break;


            case ShellNavigationSource.Pop:
            // Pop animations are handled by the FragmentManager based on the original Push.
            // No need to set custom animations here, unless you want to override the defaults.
            // If you do, you can set them here, but usually not needed.


            case ShellNavigationSource.PopToRoot:
                // For PopToRoot, the default pop animation can look weird.
                // A simple fade is often a much cleaner user experience.
                // This will OVERRIDE the default pop animation for this specific action.
                t.SetCustomAnimations(
                    Resource.Animation.m3_motion_fade_enter, // New page (the root) fades in
                    Resource.Animation.m3_motion_fade_exit   // Current page fades out
                );
                break;

            case ShellNavigationSource.ShellSectionChanged:
                // Use a specific, non-configurable fade for tab changes for a consistent UX.
                t.SetCustomAnimations(
                    Resource.Animation.m3_motion_fade_enter,
                    Resource.Animation.m3_motion_fade_exit
                );
                break;

            default:
                base.SetupAnimation(navSource, t, page);
                break;
        }
    }
    protected override Drawable CreateItemBackgroundDrawable()
    {
        var stateList = ColorStateList.ValueOf(PublicStats.RippleColor);

        // Change background color here
        var colorDrawable = new ColorDrawable(PublicStats.TabsBackgroundColor);
        return new RippleDrawable(stateList, colorDrawable, null);
    }

    protected override bool OnItemSelected(IMenuItem item)
    {
        var id = item.ItemId;
        if (id == MoreTabId)
        {
            //var items = CreateTabList(ShellItem);
            var _bottomSheetDialog = CreateMoreBottomSheet((int a, BottomSheetDialog b) => OnMoreItemSelected(a, b));

            _moreBottomSheetDialogInstance?.Show();
            _moreBottomSheetDialogInstance?.DismissEvent += OnMoreSheetDismissed;

            return true;
        }

        return base.OnItemSelected(item);
    }
}