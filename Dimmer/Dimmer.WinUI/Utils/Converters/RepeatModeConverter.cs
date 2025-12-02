using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.WinUI.Utils.Converters;

public partial class RepeatModeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string culture)
    {
        FontIcon repeateOneIcon = new FontIcon();
        repeateOneIcon.Glyph = "\uE8ED";
        FontIcon repeatAllIcon = new FontIcon();
        repeatAllIcon.Glyph = "\uE8EE";
        

        FontIcon repeateOffIcon = new FontIcon();
        repeateOffIcon.Glyph = "\uF5E7";

        FontIcon repeatCust = new FontIcon();
        repeatCust.Glyph = "\uEF3B";

        Debug.WriteLine(value?.GetType());
        var valRepeatMode = (RepeatMode?)value;
        if (valRepeatMode != null)
            switch (valRepeatMode)
            {
                case RepeatMode.All:
                    return repeatAllIcon;
                    break;
                case RepeatMode.Off:
                    return repeateOffIcon;
                    break;
                case RepeatMode.One:
                    return repeateOneIcon;
                    break;
                case RepeatMode.Custom:
                    return repeatCust;
                    break;
                default:
                    break;
            }
        return repeatAllIcon;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, string culture)
    {
        return "Paused";
    }
}