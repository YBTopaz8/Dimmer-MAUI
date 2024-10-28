namespace Dimmer_MAUI.Utilities.OtherUtils;
public static class CustomAnimsExtensions
{
    public static async Task Bounce(this View element)
    {

        
    }

    public static async Task AnimateHighlightPointerPressed(this View element)
    {
        await element.ScaleTo(0.95, 80, Easing.CubicIn);
    }
    public static async Task AnimateHighlightPointerReleased(this View element)
    {
        await element.ScaleTo(1.0, 80, Easing.CubicOut);        
    }

    public static async Task DimmOut(this View element)//, EventArgs e)
    {
        await element.FadeTo(0.85, 80, Easing.CubicIn);
    }
    public static async Task DimmIn(this View element)//, EventArgs e)
    {
        await element.FadeTo(1.0, 80, Easing.CubicOut);
        
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

    public static async Task AnimateFocusModePointerEnter(this View element, double endOpacity = 1, double endScale = 1)
    {
        // Animate scale-up to 1.2 and opacity to 1 with a smooth transition
        await Task.WhenAll(
            element.ScaleTo(endScale, 250, Easing.CubicInOut),
            element.FadeTo(1.0, 250, Easing.CubicInOut)
        );
    }

    
    public static async Task AnimateFocusModePointerExited(this View element, double endOpacity = 0.7,double endScale = 0.7)
    {
        // Animate scale-down to 0.8 and opacity to 0.7 with a smooth transition
        await Task.WhenAll(
            element.ScaleTo(endScale, 250, Easing.CubicInOut),
            element.FadeTo(endOpacity, 250, Easing.CubicInOut)
        );
    }

    // Extension method to fade out and slide back a view
    public static async Task AnimateFadeOutBack(this View element)
    {
        await Task.WhenAll(
            element.FadeTo(0, 250, Easing.CubicInOut), // Fade out
            element.TranslateTo(0, 50, 250, Easing.CubicInOut) // Slide back by 50 units on Y-axis
        );
        element.IsVisible = false; // Hide the view after animation
    }

    // Extension method to fade in and slide forward a view
    public static async Task AnimateFadeInFront(this View element)
    {
        element.IsVisible = true; // Show the view before animation
        element.Opacity = 0; // Ensure the view is initially transparent
        element.TranslationY = 50; // Start with the view slightly back
        await Task.WhenAll(
            element.FadeTo(1, 250, Easing.CubicInOut), // Fade in
            element.TranslateTo(0, 0, 250, Easing.CubicInOut) // Slide forward to original position
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

    // Helper method to animate HeightRequest smoothly
    private static async Task AnimateHeightRequest(this View element, double targetHeight, uint duration)
    {
        var initialHeight = element.Height;
        var heightAnimation = new Animation(v => element.HeightRequest = v, initialHeight, targetHeight);

        heightAnimation.Commit(element, "HeightRequestAnimation", length: duration, easing: Easing.CubicInOut);
        await Task.Delay((int)duration); // Wait for the animation to complete
    }



}
