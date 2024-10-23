namespace Dimmer_MAUI.Utilities.OtherUtils;
public static class CustomAnimsExtensions
{
    public static async Task Bounce(this View element)
    {

        
    }

    public static async Task AnimateHighlightPointerPressed(this View element, EventArgs e)
    {
        await element.ScaleTo(0.95, 80, Easing.CubicIn);
    }
    public static async Task AnimateHighlightPointerReleased(this View element, EventArgs e)
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
