namespace Dimmer_MAUI.Utilities.OtherUtils.CustomControl;

public partial class MarqueeLabel : Label
{
    private bool _isAnimating;
    private double _containerWidth;

    public MarqueeLabel()
    {
        LineBreakMode = LineBreakMode.NoWrap;
        SizeChanged += OnSizeChanged;
        ;
    }

    private async void OnSizeChanged(object? sender, System.EventArgs e)
    {
        if (Width <= 0)
            return;

        _containerWidth = Width;
        // Start animation if text width is larger than container
        if (!_isAnimating && GetTextWidth() > _containerWidth)
            await StartAnimation();
    }

    private double GetTextWidth()
    {
        // Measures the text width based on available height
        Size size = Measure(double.PositiveInfinity, Height);
        return size.Width;
    }

    public async Task StartAnimation()
    {
        _isAnimating = true;
        double textWidth = GetTextWidth();

        // Calculate extra distance to scroll completely off screen
        double scrollDistance = textWidth + _containerWidth;

        // Reset starting position (text starts at container's right edge)
        TranslationX = _containerWidth;

        while (true)
        {
            
            // Animate translation: move from right to left completely
            await this.TranslateTo(-textWidth, 0, 5000, Easing.Linear);
            // Optional pause at the end
            await Task.Delay(1000);
            // Reset instantly to starting position
            TranslationX = _containerWidth;
        }
    }
}