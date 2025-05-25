using Android.Animation;
using Android.Graphics;
using Android.Views.Animations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Android.Resource;
using AColor = Android.Graphics.Color;
using Color = Android.Graphics.Color;
using LP = Android.Views.ViewGroup.LayoutParams;
using Orientation = Android.Widget.Orientation;

namespace Dimmer.Utils;
internal static class PublicStats
{
    public static int NumberOfTabsToShow { get; set; } = 4;
    public static Color TabsBackgroundColor { get; set; } = Android.Graphics.Color.DarkSlateBlue;
    public static int IconMarginLeftDp { get; set; } = 20;
    public static int IconMarginRightDp { get; set; } = 20;
    public static int IconMarginTopDp { get; set; } = 6;
    public static int IconMarginBottomDp { get; set; } = 6;

    public static Color IconTintColor { get; set; }
    public static float IconTintAlpha { get; set; }

    public static int TabRowPaddingTopBottomDp { get; set; }
    public static Color TabTitleTextColor { get; set; } = Color.White;
    public static string TabTitleFontFamily { get; set; } = "sans-serif-medium";
    public static float TabTitleFontSizeSp { get; set; } = 14f;

    public static float TabTitleLayoutWeight { get; set; } = 1f;


    public static Color RippleColor { get; set; } = Color.Black;
    public static float RippleAlpha { get; set; } = 0.2f;

    // --- Bottom Tab Bar (BottomNavigationView) ---
    public static Color BottomNavBackgroundColor { get; set; } = AColor.ParseColor("#483d8b"); // Light grey
    public static AColor BottomNavItemCheckedColor { get; set; } = AColor.ParseColor("#007AFF"); // Blue
    public static AColor BottomNavItemUncheckedColor { get; set; } = AColor.ParseColor("#8A8A8E"); // Grey
    public static float BottomNavElevationDp { get; set; } = 18f; // Elevation for shadow
    public static Microsoft.Maui.Font BottomNavItemFont { get; set; } = Microsoft.Maui.Font.SystemFontOfSize(12);
    public static Microsoft.Maui.Font BottomNavItemFontActive { get; set; } = Microsoft.Maui.Font.SystemFontOfSize(13, FontWeight.Bold);


    // --- "More" Bottom Sheet ---
    public static AColor MoreSheetBackgroundColor { get; set; } = AColor.White;
    public static AColor MoreSheetItemRippleColor { get; set; } = AColor.ParseColor("#40000000"); // Translucent black for ripple
    public static AColor MoreSheetItemBackground { get; set; } = AColor.Transparent; // Background for each item before ripple
    public static AColor MoreSheetItemTextColor { get; set; } = AColor.ParseColor("#222222");
    public static Microsoft.Maui.Font MoreSheetItemFont { get; set; } = Microsoft.Maui.Font.OfSize("sans-serif-medium", 16);
    public static int MoreSheetItemIconMarginLeftDp { get; set; } = 16;
    public static int MoreSheetItemIconMarginRightDp { get; set; } = 32;
    public static int MoreSheetItemIconMarginTopDp { get; set; } = 8;
    public static int MoreSheetItemIconMarginBottomDp { get; set; } = 8;
    public static AColor MoreSheetItemIconTintColor { get; set; } = AColor.DarkGray;


    // --- Animations ---
    public static long DefaultAnimationDurationMs { get; set; } = 600;
    public static Android.Views.Animations.IInterpolator DefaultInterpolator { get; set; } = new Android.Views.Animations.DecelerateInterpolator();
    public static bool AutoHideTabBarOnScroll { get; set; } = false;

    public static int ActivityEnterAnimationResId { get; set; } = 0; // 0 means use system default or none
    public static int ActivityExitAnimationResId { get; set; } = 0;
    public static string TabContentDescriptionFormat { get; set; } = "{0} tab";
    public static bool LargeTextMode { get; set; } = false;
    public static bool HapticOnTap { get; set; } = true;
    public static bool HapticOnLongPress { get; set; } = true;
    public static int BottomSheetAnimationDurationMs { get; set; } = 300;
    public static int TabBarAnimationDurationMs { get; set; } = 250;

    public static Action<int, string>? OnTabClicked { get; set; }
    public static Action<int, string>? OnSheetItemClicked { get; set; }

    public static void InvokeTabClicked(int tabIndex, string tabName)
    {
        OnTabClicked?.Invoke(tabIndex, tabName);
    }

    public static void InvokeSheetItemClicked(int itemIndex, string itemName)
    {
        OnSheetItemClicked?.Invoke(itemIndex, itemName);
    }
    public static Dictionary<int, int> TabBadgeCounts { get; set; } = new();
    public static Color TabBadgeColor { get; set; } = Color.Red;
    public static bool ShowBarShadow { get; set; } = true;
    public static Color BarColorLight { get; set; } = Color.White;
    public static Color BarColorDark { get; set; } = Color.Black;
    public static bool FollowSystemTheme { get; set; } = true;

    //public static BottomSheetAnimationStyle AnimationStyle { get; set; } = BottomSheetAnimationStyle.SlideUp;
    public static Color SheetDragHandleColor { get; set; } = Color.Gray;
    public static bool ShowSheetDragHandle { get; set; } = true;
    public static bool CloseSheetOnOutsideTap { get; set; } = true;
    public static Microsoft.Maui.Graphics.Color ShellPageBackgroundColor { get; set; } = Microsoft.Maui.Graphics.Colors.Transparent;
    public static Microsoft.Maui.Graphics.Color ContentAreaBackgroundColor { get; set; } = Microsoft.Maui.Graphics.Colors.Transparent;
    public static Microsoft.Maui.Graphics.Color ToolbarBackgroundColor { get; set; } = Microsoft.Maui.Graphics.Colors.Chocolate;
    public static Microsoft.Maui.Graphics.Color ToolbarTitleColor { get; set; } = Microsoft.Maui.Graphics.Colors.White;
    public static Microsoft.Maui.Graphics.Color ToolbarSubtitleColor { get; set; } = Microsoft.Maui.Graphics.Colors.White;
    public static Microsoft.Maui.Graphics.Color FlyoutScrimColor { get; set; } = Microsoft.Maui.Graphics.Colors.White;
    public static Microsoft.Maui.Graphics.Color ToolbarNavigationIconColor { get; set; } = Microsoft.Maui.Graphics.Colors.Red;

    public static ITimeInterpolator ActivityTransitionInterpolator { get; set; } = new DecelerateInterpolator();
    public static float ActivityTransitionDuration { get; set; } = 900;
    public static long ActivityTransitionDurationMs { get; set; } = 950;
    public static long TabSwitchAnimationDurationMs { get; set; } = 950;


    public static ActivityTransitionType EnterTransition { get; set; } = ActivityTransitionType.SlideFromBottom;
    public static ActivityTransitionType ExitTransition { get; set; } = ActivityTransitionType.Explode;
    public static ActivityTransitionType ReenterTransition { get; set; } = ActivityTransitionType.Fade;
    public static ActivityTransitionType ReturnTransition { get; set; } = ActivityTransitionType.SlideFromStart;
}
public enum ActivityTransitionType
{
    None,
    Fade,
    SlideFromEnd,
    SlideFromStart,
    SlideFromBottom,
    Explode
    // Add ChangeBounds for shared elements
    // ChangeBounds
}