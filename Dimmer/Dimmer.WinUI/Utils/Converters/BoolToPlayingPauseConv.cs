namespace Dimmer.WinUI.Utils.Converters;


public partial class BoolToPlayingPauseConv : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string culture)
    {
        FontIcon pauseIcon = new FontIcon();
        pauseIcon.Glyph = "\uE769";
        pauseIcon.Height = 24; pauseIcon.Width = 24;
        FontIcon PlayIcon = new FontIcon();
        PlayIcon.Height=24; PlayIcon.Width=24;
        PlayIcon.Glyph = "\uE768";
        if (value is bool boolVal)
        {
            return boolVal ? pauseIcon : PlayIcon;
        }
        return PlayIcon;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, string culture)
    {
        return "Paused";
    }
}