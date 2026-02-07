using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media.Animation;
using System.Numerics;
using System.Runtime.CompilerServices; // Required for ConditionalWeakTable
using Visibility = Microsoft.UI.Xaml.Visibility;
using VisualStateManager = Microsoft.UI.Xaml.VisualStateManager;

namespace Dimmer.WinUI.Utils;

public static class AnimationHelper
{
    #region Connected Animation Enhancements
    public const string Key_Forward = "ForwardAnimation";
    public const string Key_ListToDetail = "ListToDetailAnimation";
    public const string Key_ArtistToSong = "ArtistToSongPage";
    public const string Key_NowPlayingPage = "ToNowPlayingPage";
    public const string Key_DetailToList = "DetailToListAnimation";
    public const string Key_ToViewSingleSongPopUp = "ToSingleSongPopupAnimation";
    public const string Key_ToViewQueue = "ToQueueAnimation";
    public enum ConnectedAnimationStyle
    {
        GravitySwing,
        GravityBounce,
        DirectTransport,
        CrossFadeOnly,
        ScaleUp,
        ScaleDown
    }

    public static void Prepare(string key, FrameworkElement? source,
                               ConnectedAnimationStyle style = ConnectedAnimationStyle.GravitySwing,
                               double duration = 500)
    {
        if (source == null) return;

        var service = ConnectedAnimationService.GetForCurrentView();
        var animation = service.PrepareToAnimate(key, source);

        // Store style and duration
        animation.SetAnimationStyle(style);
        animation.SetDuration(duration);
    }
    /// <summary>
    /// Finds a named child (e.g., "ArtistNameTxt") inside a parent element and prepares the animation.
    /// </summary>
    public static void PrepareFromChild(DependencyObject root, string childName, string key,
                                        ConnectedAnimationStyle style = ConnectedAnimationStyle.GravitySwing)
    {
        if (root == null) return;

        var child = FindChildByName(root, childName);
        if (child != null)
        {
            Prepare(key, child, style);
        }
    }

    /// <summary>
    /// Finds a container in a List/TableView, finds a named element inside it, and prepares the animation.
    /// </summary>
    public static void PrepareFromList(ItemsControl listControl, object dataItem, string childName, string key,
                                       ConnectedAnimationStyle style = ConnectedAnimationStyle.GravitySwing)
    {
        if (listControl == null || dataItem == null) return;

        // 1. Try to get the container (works for ListView/GridView)
        DependencyObject? container = listControl.ContainerFromItem(dataItem);

        // 2. If ContainerFromItem fails (common in Virtualized lists or custom TableViews),
        // try to find it via Visual Tree traversal if it's currently visible.
        if (container == null)
        {
            // Fallback: This is expensive but necessary if ContainerFromItem returns null
            container = FindContainerForData(listControl, dataItem);
        }

        if (container != null)
        {
            // 3. Find the specific visual element (e.g. CoverImage) inside the row
            var child = FindChildByName(container, childName);
            if (child != null)
            {
                Prepare(key, child, style);
            }
        }
    }


    private static FrameworkElement? FindChildByName(DependencyObject parent, string name)
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is FrameworkElement fe && fe.Name == name)
                return fe;

            var result = FindChildByName(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    private static DependencyObject? FindContainerForData(DependencyObject parent, object dataItem)
    {
        // Recursively search for a FrameworkElement whose DataContext matches dataItem
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is FrameworkElement fe && ReferenceEquals(fe.DataContext, dataItem))
            {
                return fe;
            }

            var result = FindContainerForData(child, dataItem);
            if (result != null) return result;
        }
        return null;
    }
    public static void TryStart(UIElement destination,
                                IEnumerable<UIElement>? coordinatedElements = null,
                                params string[] potentialKeys)
    {
        destination.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
        {
            var service = ConnectedAnimationService.GetForCurrentView();
            ConnectedAnimation? animation = null;

            foreach (var key in potentialKeys)
            {
                animation = service.GetAnimation(key);
                if (animation != null) break;
            }

            if (animation == null) return;

            var style = animation.GetAnimationStyle();
            var duration = animation.GetDuration();

            // Configure based on style
            animation.Configuration = CreateConfiguration(style);

            destination.Opacity = 1;
            destination.Visibility = Visibility.Visible;

            if (coordinatedElements != null)
                animation.TryStart(destination, coordinatedElements);
            else
                animation.TryStart(destination);
        });
    }

    private static ConnectedAnimationConfiguration? CreateConfiguration(ConnectedAnimationStyle style)
    {
        switch (style)
        {
            case ConnectedAnimationStyle.GravityBounce:
                return new BasicConnectedAnimationConfiguration();

            case ConnectedAnimationStyle.DirectTransport:
                return new DirectConnectedAnimationConfiguration();

            case ConnectedAnimationStyle.CrossFadeOnly:
                return null; // Basic fade

            case ConnectedAnimationStyle.ScaleUp:
            case ConnectedAnimationStyle.ScaleDown:
                return new BasicConnectedAnimationConfiguration();

            default: // GravitySwing
                return new GravityConnectedAnimationConfiguration
                {
                    IsShadowEnabled = true // This works here
                };
        }
    }

    #endregion

    #region Composition Animation Helper (GPU-Based)

    public static void FadeIn(UIElement element, double duration = 300, CompositionEasingFunction? easing = null)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;

        var fadeAnimation = compositor.CreateScalarKeyFrameAnimation();
        fadeAnimation.InsertKeyFrame(0f, 0f);
        fadeAnimation.InsertKeyFrame(1f, 1f, easing ?? compositor.CreateLinearEasingFunction());
        fadeAnimation.Duration = TimeSpan.FromMilliseconds(duration);

        visual.StartAnimation("Opacity", fadeAnimation);
    }

    public static void SlideIn(UIElement element, SlideDirection direction = SlideDirection.Left,
                               double distance = 100, double duration = 400)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;

        var offset = GetDirectionVector(direction) * (float)distance;
        // Note: Resetting offset manually here prevents cumulative shifts if called multiple times
        visual.Offset = new Vector3(offset.X, offset.Y, 0);

        var slideAnimation = compositor.CreateVector3KeyFrameAnimation();
        slideAnimation.InsertKeyFrame(1f, Vector3.Zero,
            compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1f)));
        slideAnimation.Duration = TimeSpan.FromMilliseconds(duration);

        visual.StartAnimation("Offset", slideAnimation);
    }

    public static void ScaleElement(FrameworkElement element, float startScale = 0.8f,
                                    float endScale = 1f, double duration = 300)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;


        // CRITICAL FIX: Ensure scaling happens from the center of the element
        element.SizeChanged += (s, e) =>
        {
            var v = ElementCompositionPreview.GetElementVisual(element);
            v.CenterPoint = new Vector3((float)e.NewSize.Width / 2, (float)e.NewSize.Height / 2, 0);
        };
        // Attempt to set it immediately if size is known
        if (element.ActualSize.X > 0)
        {
            visual.CenterPoint = new Vector3((float)element.ActualSize.X / 2, (float)element.ActualSize.Y / 2, 0);
        }

        var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
        scaleAnimation.InsertKeyFrame(0f, new Vector3(startScale, startScale, 1));
        scaleAnimation.InsertKeyFrame(1f, new Vector3(endScale, endScale, 1),
            compositor.CreateCubicBezierEasingFunction(new Vector2(0.68f, -0.6f), new Vector2(0.32f, 1.6f)));
        scaleAnimation.Duration = TimeSpan.FromMilliseconds(duration);

        visual.StartAnimation("Scale", scaleAnimation);
    }

    public enum SlideDirection { Left, Right, Top, Bottom }

    private static Vector2 GetDirectionVector(SlideDirection direction)
    {
        return direction switch
        {
            SlideDirection.Left => new Vector2(-1, 0),
            SlideDirection.Right => new Vector2(1, 0),
            SlideDirection.Top => new Vector2(0, -1),
            SlideDirection.Bottom => new Vector2(0, 1),
            _ => new Vector2(-1, 0)
        };
    }

    #endregion

    #region Visual State Manager Helper

    public static bool GoToState(Control control, string stateName, bool useTransitions = true)
    {
        return VisualStateManager.GoToState(control, stateName, useTransitions);
    }

    // Removed SetupVisualState: 
    // VisualState.Name and VisualStateGroup.Name are read-only in C#.
    // You must define VisualStates in XAML for GoToState to work correctly.

    #endregion

    #region Extension Methods for Animation Properties

    // FIX: Use ConditionalWeakTable to prevent memory leaks. 
    // This allows the keys (ConnectedAnimation objects) to be garbage collected.
    private static readonly ConditionalWeakTable<ConnectedAnimation, AnimationData> _animationData = new();

    // Helper class to hold data
    private class AnimationData
    {
        public ConnectedAnimationStyle Style { get; set; } = ConnectedAnimationStyle.GravitySwing;
        public double Duration { get; set; } = 500;
    }

    public static void SetAnimationStyle(this ConnectedAnimation animation, ConnectedAnimationStyle style)
    {
        var data = _animationData.GetOrCreateValue(animation);
        data.Style = style;
    }

    public static ConnectedAnimationStyle GetAnimationStyle(this ConnectedAnimation animation)
    {
        if (_animationData.TryGetValue(animation, out var data))
        {
            return data.Style;
        }
        return ConnectedAnimationStyle.GravitySwing;
    }

    public static void SetDuration(this ConnectedAnimation animation, double duration)
    {
        var data = _animationData.GetOrCreateValue(animation);
        data.Duration = duration;
    }

    public static double GetDuration(this ConnectedAnimation animation)
    {
        if (_animationData.TryGetValue(animation, out var data))
        {
            return data.Duration;
        }
        return 500;
    }

    #endregion
}