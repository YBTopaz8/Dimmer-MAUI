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


    //public static async Task AnimateHighlightPointerEntered(this View element, EventArgs e)
    //{
    //    element.Margin = new Thickness(40, 0, 0, 0);
    //    await element.ScaleTo(1.05, 150, Easing.CubicIn);        
    //}

    //public static async Task AnimateHighlightPointerExited(this View element, EventArgs e)
    //{
    //    element.Margin = new Thickness(0, 0, 0, 0);
    //    await element.ScaleTo(1.0, 150, Easing.CubicOut);     
    //}


}
