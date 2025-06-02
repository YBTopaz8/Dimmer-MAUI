using Android.OS;
using Dimmer.CustomShellRenderers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Font = Microsoft.Maui.Graphics.Font;

namespace Dimmer.CustomShellRenderers;
// Delegate definitions for event arguments
public class ColorChangedEventArgs : EventArgs
{
    public Color NewColor { get; }
    public Color? SecondaryColor { get; } // For things like active/inactive
    public ShellElement TargetElement { get; }

    public ColorChangedEventArgs(ShellElement target, Color newColor, Color? secondaryColor = null)
    {
        TargetElement = target;
        NewColor = newColor;
        SecondaryColor = secondaryColor;
    }
}

public class FontChangedEventArgs : EventArgs
{
    public Font NewFont { get; }
    public ShellElement TargetElement { get; }
    public string? FontRole { get; } // e.g., "ActiveTab", "InactiveTab", "MoreItem"

    public FontChangedEventArgs(ShellElement target, Font newFont, string? role = null)
    {
        TargetElement = target;
        NewFont = newFont;
        FontRole = role;
    }
}

public class ElevationChangedEventArgs : EventArgs
{
    public float NewElevationDp { get; }
    public ShellElement TargetElement { get; }
    public ElevationChangedEventArgs(ShellElement target, float newElevationDp)
    {
        TargetElement = target;
        NewElevationDp = newElevationDp;
    }
}

public class IconChangedEventArgs : EventArgs
{
    public string NewIconResourceName { get; } // Name of the drawable in Android Resources
    public Color? TintColor { get; }
    public ShellElement TargetElement { get; }
    public IconRole Role { get; }

    public IconChangedEventArgs(ShellElement target, IconRole role, string newIconResourceName, Color? tintColor = null)
    {
        TargetElement = target;
        Role = role;
        NewIconResourceName = newIconResourceName;
        TintColor = tintColor;
    }
}

public class AnimationSettingsChangedEventArgs : EventArgs
{
    public long? DurationMs { get; set; }
    // Add other animation params like InterpolatorType string/enum if needed
    public ShellAnimationTarget TargetAnimation { get; }

    public AnimationSettingsChangedEventArgs(ShellAnimationTarget target, long? duration = null)
    {
        TargetAnimation = target;
        DurationMs = duration;
    }
}

public class TabBehaviorChangedEventArgs : EventArgs
{
    public int? NumberOfVisibleTabs { get; set; }
    // Other tab behavior flags
}


// Enums to identify what to update
public enum ShellElement
{
    BottomNavBar,
    BottomNavItem, // For active/inactive distinction
    MoreSheetBackground,
    MoreSheetItem,
    Toolbar,
    Flyout,
    FlyoutScrim,
    FlyoutMenuPanel,
    PageContentArea,
    ActivityWindow // For status bar, etc.
}

public enum IconRole
{
    ToolbarNavigation,
    // Add more if needed, e.g., specific tab icons by index/route
}

public enum ShellAnimationTarget
{
    ActivityTransition,
    TabSwitchAnimation
}

public static class ShellStylingBridge
{
    // --- Color Change Events ---
    public static event EventHandler<ColorChangedEventArgs>? BackgroundColorChanged;
    public static event EventHandler<ColorChangedEventArgs>? TextColorChanged; // For items like tabs, toolbar title
    public static event EventHandler<ColorChangedEventArgs>? IconTintColorChanged;

    public static void UpdateBackgroundColor(ShellElement target, Color newColor, Color? secondaryColor = null)
    {
        BackgroundColorChanged?.Invoke(null, new ColorChangedEventArgs(target, newColor, secondaryColor));
    }

    public static void UpdateTextColor(ShellElement target, Color newColor, Color? secondaryColor = null)
    {
        TextColorChanged?.Invoke(null, new ColorChangedEventArgs(target, newColor, secondaryColor));
    }

    public static void UpdateIconTintColor(ShellElement target, Color newColor, Color? secondaryColor = null)
    {
        IconTintColorChanged?.Invoke(null, new ColorChangedEventArgs(target, newColor, secondaryColor));
    }


    // --- Font Change Event ---
    public static event EventHandler<FontChangedEventArgs>? FontChanged;
    public static void UpdateFont(ShellElement target, Font newFont, string? role = null)
    {
        FontChanged?.Invoke(null, new FontChangedEventArgs(target, newFont, role));
    }


    // --- Elevation Change Event ---
    public static event EventHandler<ElevationChangedEventArgs>? ElevationChanged;
    public static void UpdateElevation(ShellElement target, float newElevationDp)
    {
        ElevationChanged?.Invoke(null, new ElevationChangedEventArgs(target, newElevationDp));
    }


    // --- Icon Change Event ---
    public static event EventHandler<IconChangedEventArgs>? IconChanged;
    public static void UpdateIcon(ShellElement target, IconRole role, string newIconResourceName, Color? tintColor = null)
    {
        IconChanged?.Invoke(null, new IconChangedEventArgs(target, role, newIconResourceName, tintColor));
    }


    // --- Animation Settings Change Event ---
    public static event EventHandler<AnimationSettingsChangedEventArgs>? AnimationSettingsChanged;
    public static void UpdateAnimationSettings(ShellAnimationTarget target, long? durationMs = null)
    {
        AnimationSettingsChanged?.Invoke(null, new AnimationSettingsChangedEventArgs(target, durationMs));
    }

    // --- Tab Behavior Change Event ---
    public static event EventHandler<TabBehaviorChangedEventArgs>? TabBehaviorChanged;
    public static void UpdateTabBehavior(int? numberOfVisibleTabs = null)
    {
        TabBehaviorChanged?.Invoke(null, new TabBehaviorChangedEventArgs { NumberOfVisibleTabs = numberOfVisibleTabs });
    }


    // --- Generic Refresh Request ---
    // Useful if a PublicStats value changes that isn't tied to a specific event above,
    // or to force a re-evaluation of styles in renderers.
    public static event EventHandler<ShellElement>? ShellElementRefreshRequested;
    public static void RequestShellElementRefresh(ShellElement targetElement)
    {
        ShellElementRefreshRequested?.Invoke(null, targetElement);
    }
}
