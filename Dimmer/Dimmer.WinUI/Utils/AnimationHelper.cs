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
    public const string Key_Backward = "BackWardAnimation";
    public const string Key_ListToDetail = "ListToDetailAnimation";
    public const string Key_ToSingleDownloadedLyrics = "ToSingleDownloadedLyrics";
    public const string Key_ArtistToSong = "ArtistToSongPage";
    public const string Key_AlbumToArtist = "AlbumPageToArtistPage";
    public const string Key_SongDetailToArtist = "SongDetailPageToArtistPage";
    public const string Key_ArtistToAlbum = "ArtistPageToAlbumPage";
    public const string Key_NowPlayingPage = "ToNowPlayingPage";
    public const string Key_DetailToListFromAlbum = "ToListFromAllAlbums";
    public const string Key_ToAlbumPage = "ToAlbumPage";
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

    public static void AddHoverImplicitAnimations(UIElement element)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;

        // Create implicit animation collection
        var implicitAnimations = compositor.CreateImplicitAnimationCollection();

        // Scale animation
        var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
        scaleAnimation.Target = "Scale";
        scaleAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
        scaleAnimation.Duration = TimeSpan.FromMilliseconds(300);

        // Offset animation
        var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
        offsetAnimation.Target = "Offset";
        offsetAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
        offsetAnimation.Duration = TimeSpan.FromMilliseconds(300);

        // Apply to properties
        implicitAnimations["Scale"] = scaleAnimation;
        implicitAnimations["Offset"] = offsetAnimation;

        visual.ImplicitAnimations = implicitAnimations;
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

            if (animation == null)
            {

                return ;
            }
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
    public static void OptimizeForPerformance(UIElement element)
    {
        // Enable caching for complex visuals
        ElementCompositionPreview.SetIsTranslationEnabled(element, true);

        // Cache the visual as a bitmap for complex UI
        var visual = ElementCompositionPreview.GetElementVisual(element);

        // Use SpriteVisual with surface for complex content
        var compositor = visual.Compositor;
        var surface = LoadedImageSurface.StartLoadFromUri(new Uri("ms-appx:///Assets/card.png"));
        var sprite = compositor.CreateSpriteVisual();
        sprite.Brush = compositor.CreateSurfaceBrush(surface);


    }


    #region Store Card Hover Effects

    /// <summary>
    /// Attaches a Microsoft Store-like hover effect to a card.
    /// </summary>
    /// <param name="cardRoot">The main container of the card.</param>
    /// <param name="contentToScale">The visual element to scale (usually the same as cardRoot, or a child grid).</param>
    /// <param name="revealPanel">The panel containing the 'Install' button (optional).</param>
    /// <param name="hoverScale">How much to scale up (e.g., 1.05f).</param>
    public static void AttachCardHoverAnimation(FrameworkElement cardRoot, FrameworkElement contentToScale, FrameworkElement? revealPanel, float hoverScale = 1.05f)
    {
        var visual = ElementCompositionPreview.GetElementVisual(contentToScale);
        var compositor = visual.Compositor;

        // 1. Configure Implicit Animations (Auto-interpolate changes)
        // This ensures that whenever we change Scale/Offset, it animates smoothly automatically.
        var animationGroup = compositor.CreateAnimationGroup();

        // Scale Animation (Spring for bounce effect)
        var scaleAnim = compositor.CreateVector3KeyFrameAnimation();
        scaleAnim.Target = "Scale";
        scaleAnim.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
        scaleAnim.Duration = TimeSpan.FromMilliseconds(400); // Slower for smoothness
        // A gentle spring effect
        var easing = compositor.CreateCubicBezierEasingFunction(new Vector2(0.1f, 0.9f), new Vector2(0.2f, 1.0f));

        // Offset/Translation Animation
        var offsetAnim = compositor.CreateVector3KeyFrameAnimation();
        offsetAnim.Target = "Offset";
        offsetAnim.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
        offsetAnim.Duration = TimeSpan.FromMilliseconds(400);

        // Apply implicit animations
        var implicitAnimations = compositor.CreateImplicitAnimationCollection();
        implicitAnimations["Scale"] = scaleAnim;
        implicitAnimations["Offset"] = offsetAnim;
        visual.ImplicitAnimations = implicitAnimations;

        // 2. Handle Reveal Panel (The "Install" part)
        Visual? revealVisual = null;
        if (revealPanel != null)
        {
            revealVisual = ElementCompositionPreview.GetElementVisual(revealPanel);
            revealVisual.Opacity = 0f; // Hidden by default

            // Slide/Fade animation for the reveal
            var opacityAnim = compositor.CreateScalarKeyFrameAnimation();
            opacityAnim.Target = "Opacity";
            opacityAnim.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
            opacityAnim.Duration = TimeSpan.FromMilliseconds(200);

            var revealImplicit = compositor.CreateImplicitAnimationCollection();
            revealImplicit["Opacity"] = opacityAnim;
            revealVisual.ImplicitAnimations = revealImplicit;
        }

        // 3. Attach Pointer Events to trigger the changes
        // Important: Update CenterPoint for scaling so it zooms from the center
        contentToScale.SizeChanged += (s, e) =>
        {
            visual.CenterPoint = new Vector3((float)e.NewSize.Width / 2, (float)e.NewSize.Height / 2, 0);
        };

        cardRoot.PointerEntered += (s, e) =>
        {
            // IMPORTANT: Bring to front so it overlaps neighbors
            Canvas.SetZIndex(cardRoot, 10);

            // Scale Up
            visual.Scale = new Vector3(hoverScale, hoverScale, 1.0f);

            // Optional: Lift it up slightly (3D effect)
            // visual.Offset = new Vector3(0, -4, 0); 
            // Note: If using Shadow, WinUI handles shadow automatically on elevation if using ThemeShadow

            // Show Reveal Panel
            revealVisual?.Opacity = 1.0f;
        };

        cardRoot.PointerExited += (s, e) =>
        {
            // Reset Z-Index (Delayed slightly ensures it doesn't clip while shrinking, 
            // but standard 0 usually works fine immediately if animation is fast enough)
            Canvas.SetZIndex(cardRoot, 0);

            // Reset Scale
            visual.Scale = Vector3.One;
            // visual.Offset = Vector3.Zero;

            // Hide Reveal Panel
            revealVisual?.Opacity = 0f;
        };
    }
    #endregion
}