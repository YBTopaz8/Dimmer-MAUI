using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Realms;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Application = Microsoft.UI.Xaml.Application;
using Border = Microsoft.UI.Xaml.Controls.Border;
using KeyboardAccelerator = Microsoft.UI.Xaml.Input.KeyboardAccelerator;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.CustomViews.WinuiViews;

public sealed partial class ArcFabControl : UserControl
{
    private bool _isOpen = false;
    private Compositor _compositor;
    private readonly List<UIElement> _currentSubButtons = new();

    // Configurable properties
    private const double ItemSize = 48;
    private const double TriggerSize = 56;
    private const double Radius = 110; // Distance from center
    private const double MarginXY = 24; // Distance from screen edge

    public FabDirection ExpandDirection { get; set; } = FabDirection.Up; // Default expands Up/Left
    public event EventHandler<string> ItemClicked;
    private const double Distance = 110;
    public ArcFabControl()
    {
        this.InitializeComponent();
        var accelerator = new KeyboardAccelerator
        {
            Modifiers = VirtualKeyModifiers.Control,
            Key = VirtualKey.Q
        };
        accelerator.Invoked += (s, e) => { MainFab_Click(s, null); e.Handled = true; };
        MainFab.KeyboardAccelerators.Add(accelerator);
    }

    private void ArcFabControl_Loaded(object sender, RoutedEventArgs e)
    {
        _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
        MainFabContainer.Translation = new Vector3(0, 0, 32);
    }

    private void ArcFabControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        //RecalculatePosition();
    }

    /// <summary>
    /// Generates the buttons dynamically based on input list.
    /// Styles them to look like the reference code (Shadows, White Background).
    /// </summary>
    public void SetMenuItems(List<string> items)
    {
        // Cleanup old items
        foreach (var item in _currentSubButtons)
        {
            SubItemsContainer.Children.Remove(item);
        }
        _currentSubButtons.Clear();

        // Create new items
        foreach (var text in items)
        {
            // 1. Create the Button
            var btn = new Button
            {
                Content = new FontIcon { Glyph = "\uE713", FontSize = 20 }, // Placeholder icon or use text
                Width = ItemSize,
                Height = ItemSize,
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(ItemSize / 2),
                Background = new SolidColorBrush(Microsoft.UI.Colors.WhiteSmoke), // Reference style
                HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
                VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch,
                Padding = new Thickness(0),
                Tag = text
            };

            // If you want text instead of icon:
            if (text.Length > 2) btn.Content = text;

            btn.Click += (s, e) =>
            {
                ItemClicked?.Invoke(this, (s as Button)?.Tag.ToString()!);
                CloseMenu();
            };

            // 2. Wrap in Border for ThemeShadow (WinUI specific depth)
            var border = new Border
            {
                Width = ItemSize,
                Height = ItemSize,
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(ItemSize / 2),
                Background = new SolidColorBrush(Microsoft.UI.Colors.White),
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.LightGray),
                BorderThickness = new Thickness(1),
                Opacity = 0,
                Child = btn,
                // Translation Z gives it the shadow effect
                Translation = new Vector3(0, 0, 16)
            };

            // 3. Add Shadow
            var shadow = new ThemeShadow();
            border.Shadow = shadow;

            // 4. Setup Composition
            ElementCompositionPreview.SetIsTranslationEnabled(border, true);

            // 5. Add to Canvas
            SubItemsContainer.Children.Insert(0, border);
            _currentSubButtons.Add(border);
        }

        // Ensure positions are correct
        //RecalculatePosition();
    }

    
    private void MainFab_Click(object sender, RoutedEventArgs e)
    {
        if (_isOpen) CloseMenu();
        else OpenMenu();
    }

    private void DimmerOverlay_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_isOpen) CloseMenu();
    }
    private void OpenMenu()
    {
        if (_compositor == null) return;
        _isOpen = true;

        // 1. Show Dimmer
        DimmerOverlay.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        DimmerOverlay.IsHitTestVisible = true;
        AnimateOpacity(DimmerOverlay, 0.4f);

        // 2. Rotate Main Icon (X)
        if (MainIcon.RenderTransform is RotateTransform rot)
        {
            var anim = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation { To = 45, Duration = TimeSpan.FromMilliseconds(100) };
            var sb = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
            sb.Children.Add(anim);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(anim, rot);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(anim, "Angle");
            sb.Begin();
        }

        // 3. Fire Pulse Animation
        AnimatePulse();

        // 4. Expand Sub-Buttons
        var (startAngle, endAngle) = GetArcAngles(ExpandDirection);
        double range = endAngle - startAngle;
        int count = _currentSubButtons.Count;

        // If only 1 item, put it in the middle of the range. Else spread evenly.
        double step = count > 1 ? range / (count - 1) : 0;
        double currentAngle = startAngle;
        if (count == 1) currentAngle = startAngle + (range / 2);

        for (int i = 0; i < count; i++)
        {
            var btn = _currentSubButtons[i];

            // WinUI coordinates: 0=Right, 90=Down, 180=Left, 270=Up
            double rad = currentAngle * (Math.PI / 180);

            // Calculate final X,Y relative to the center
            float x = (float)(Math.Cos(rad) * Distance);
            float y = (float)(Math.Sin(rad) * Distance);

            // Staggered Delay (The "Wave" effect)
            int delay = i * 30;

            // Enable HitTest
            if (btn is Border b) b.Child.IsHitTestVisible = true;
            else btn.IsHitTestVisible = true;

            AnimateSpring(btn, new Vector3(x, y, 0), 1.0f, delay);

            currentAngle += step;
        }
    }

    private void CloseMenu()
    {
        _isOpen = false;

        // 1. Hide Dimmer
        AnimateOpacity(DimmerOverlay, 0f);
        DimmerOverlay.IsHitTestVisible = false;

        // 2. Rotate Icon Back (+)
        if (MainIcon.RenderTransform is RotateTransform rot)
        {
            var anim = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation { To = 0, Duration = TimeSpan.FromMilliseconds(200) };
            var sb = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
            sb.Children.Add(anim);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(anim, rot);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(anim, "Angle");
            sb.Begin();
        }

        // 3. Retract Buttons
        for (int i = 0; i < _currentSubButtons.Count; i++)
        {
            var btn = _currentSubButtons[i];

            if (btn is Border b) b.Child.IsHitTestVisible = false;
            else btn.IsHitTestVisible = false;

            // Reverse delay for closing
            int delay = (_currentSubButtons.Count - 1 - i) * 20;

            AnimateSpring(btn, Vector3.Zero, 0f, delay);
        }
    }

    private void AnimatePulse()
    {
        var visual = ElementCompositionPreview.GetElementVisual(PulseRing);
        visual.CenterPoint = new Vector3(28f, 28f, 0); // Center of 56x56

        // Scale
        var scaleAnim = _compositor.CreateVector3KeyFrameAnimation();
        scaleAnim.Target = "Scale";
        scaleAnim.InsertKeyFrame(0f, new Vector3(1f));
        scaleAnim.InsertKeyFrame(1f, new Vector3(2.5f));
        scaleAnim.Duration = TimeSpan.FromMilliseconds(100);

        // Opacity
        var fadeAnim = _compositor.CreateScalarKeyFrameAnimation();
        fadeAnim.Target = "Opacity";
        fadeAnim.InsertKeyFrame(0f, 0.6f);
        fadeAnim.InsertKeyFrame(1f, 0f);
        fadeAnim.Duration = TimeSpan.FromMilliseconds(100);

        visual.StartAnimation("Scale", scaleAnim);
        visual.StartAnimation("Opacity", fadeAnim);
    }

    private void AnimateSpring(UIElement target, Vector3 offset, float opacity, int delay)
    {
        var visual = ElementCompositionPreview.GetElementVisual(target);

        // Spring Translation
        var spring = _compositor.CreateSpringVector3Animation();
        spring.Target = "Translation";
        spring.FinalValue = offset;
        spring.DampingRatio = 0.65f; // Nice bounce
        spring.Period = TimeSpan.FromMilliseconds(12);
        if (delay > 0) spring.DelayTime = TimeSpan.FromMilliseconds(delay);

        // Opacity
        var fade = _compositor.CreateScalarKeyFrameAnimation();
        fade.Target = "Opacity";
        fade.InsertKeyFrame(1.0f, opacity);
        fade.Duration = TimeSpan.FromMilliseconds(100);
        if (delay > 0) fade.DelayTime = TimeSpan.FromMilliseconds(delay);

        visual.StartAnimation("Translation", spring);
        visual.StartAnimation("Opacity", fade);
    }

    private void AnimateOpacity(UIElement target, float opacity)
    {
        var visual = ElementCompositionPreview.GetElementVisual(target);
        var fade = _compositor.CreateScalarKeyFrameAnimation();
        fade.Target = "Opacity";
        fade.InsertKeyFrame(1.0f, opacity);
        fade.Duration = TimeSpan.FromMilliseconds(100);
        visual.StartAnimation("Opacity", fade);
    }

    private (double start, double end) GetArcAngles(FabDirection dir)
    {
        // 0 = Right, 90 = Down, 180 = Left, 270 = Up
        return dir switch
        {
            FabDirection.Left => (180, 270), // Fan out from Left to Up
            FabDirection.Up => (270, 360), // Fan out from Up to Right (Wrap around)
            FabDirection.Right => (0, 90),   // Fan out from Right to Down
            FabDirection.Down => (90, 180),  // Fan out from Down to Left
            _ => (180, 270)
        };
    }

    private void MainFab_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {

        ExpandDirection = ExpandDirection == FabDirection.Left ? FabDirection.Up : FabDirection.Left;
    }
}

public enum FabDirection { Left, Right, Up, Down }