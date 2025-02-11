﻿namespace Dimmer_MAUI.Utilities.OtherUtils;
public static class CustomAnimsExtensions
{
   
    public static async Task AnimateHighlightPointerPressed(this View element)
    {
        await element.ScaleTo(0.95, 80, Easing.CubicIn);
    }
    public static async Task AnimateHighlightPointerReleased(this View element)
    {
        await element.ScaleTo(1.0, 80, Easing.CubicOut);        
    }

    public static async Task DimmOut(this View element, double duration = 250)
    {
        await element.FadeTo(0.70, (uint)duration, Easing.CubicIn);
    }
    public static async Task DimmIn(this View element, double duration = 250)
    {
        await element.FadeTo(1.0, (uint)duration, Easing.CubicOut);
        
    }

    public static async Task AnimateRippleBounce(this View element, int bounceCount = 3, double bounceHeight = 20, uint duration = 200)
    {
        for (int i = 0; i < bounceCount; i++)
        {
            // Move the view down
            await element.TranslateTo(0, bounceHeight, duration / 2, Easing.CubicIn);

            // Move the view back up
            await element.TranslateTo(0, 0, duration / 2, Easing.CubicOut);

            // Gradually reduce bounce height for the next bounce
            bounceHeight *= 0.5; // Diminishes like a ripple
        }
    }
    public static async Task AnimateHeight(this View view, double targetHeight, uint duration = 250, Easing? easing = null)
    {
        var animation = new Animation(v => view.HeightRequest = v, view.HeightRequest, targetHeight);
        animation.Commit(view, "HeightAnimation", 16, duration, easing, null, null);
        
        await Task.Delay((int)duration);        
    }
    public static async Task AnimateFocusModePointerEnter(this View element,double duration=250, double endScale = 1)
    {
        // Animate scale-up to 1.2 and opacity to 1 with a smooth transition
        await Task.WhenAll(
            element.ScaleTo(endScale, (uint)duration, Easing.CubicInOut),
            element.FadeTo(1.0, (uint)duration, Easing.CubicInOut)
        );
    }

    
    public static async Task AnimateFocusModePointerExited(this View element, double duration = 250, double endOpacity = 0.7,double endScale = 0.7)
    {
        // Animate scale-down to 0.8 and opacity to 0.7 with a smooth transition
        await Task.WhenAll(
            element.ScaleTo(endScale, (uint)duration, Easing.CubicInOut),
            element.FadeTo(endOpacity, (uint)duration, Easing.CubicInOut)
        );
    }

    // Extension method to fade out and slide back a view
    public static async Task AnimateFadeOutBack(this View element, uint duration = 250)
    {
        await Task.WhenAll(
            element.FadeTo(0, duration, Easing.CubicInOut), // Fade out
            element.TranslateTo(0, 50, duration, Easing.CubicInOut) // Slide back by 50 units on Y-axis
        );
        element.IsVisible = false; // Hide the view after animation
    }

    // Extension method to fade in and slide forward a view
    public static async Task AnimateFadeInFront(this View element, uint duration=250)
    {
        element.IsVisible = true; // Show the view before animation
        element.Opacity = 0; // Ensure the view is initially transparent
        element.TranslationY = 50; // Start with the view slightly back
        await Task.WhenAll(
            element.FadeTo(1, duration, Easing.CubicInOut), // Fade in
            element.TranslateTo(0, 0, duration, Easing.CubicInOut) // Slide forward to original position
        );
    }

    public static async Task AnimateSlideDown(this View element, double heightToSlide)
    {
        await element.TranslateTo(0, heightToSlide, 250, Easing.CubicInOut);
        element.HeightRequest = element.Height - heightToSlide;
    }

    public static async Task AnimateSlideUp(this View element, double heightToSlide)
    {
        await element.TranslateTo(0, 0, 250, Easing.CubicInOut);
        element.HeightRequest = element.Height + heightToSlide;
    }

    public static async Task AnimateFontSizeTo(this Label view, double toFontSize, uint duration)
    {
        await view.AnimateAsync("fontSizeAnimation", (progress) =>
        {
            view.FontSize = 19 + (toFontSize - 19) * progress;
        }, length: duration);
    }

    public static async Task AnimateTextColorTo(this Label view, Color toColor, uint duration)
    {
        Color fromColor = view.TextColor;
        await view.AnimateAsync("textColorAnimation", (progress) =>
        {
            view.TextColor = Color.FromRgba(
                (int)(fromColor.Red + (toColor.Red - fromColor.Red) * progress),
                (int)(fromColor.Green + (toColor.Green - fromColor.Green) * progress),
                (int)(fromColor.Blue + (toColor.Blue - fromColor.Blue) * progress),
                fromColor.Alpha + (toColor.Alpha - fromColor.Alpha) * progress);
        }, length: duration);
    }

    // IMPORTANT: This is the implementation of AnimateAsync
    public static Task AnimateAsync(this IAnimatable element, string name, Action<double> callback, uint length, Easing easing = null)
    {
        var tcs = new TaskCompletionSource<bool>();
        element.Animate(name, callback, 16, length, easing, (v, c) => tcs.SetResult(c), null);
        return tcs.Task;
    }
}
