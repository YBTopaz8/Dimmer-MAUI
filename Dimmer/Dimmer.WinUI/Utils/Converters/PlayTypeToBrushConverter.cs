using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dimmer.Charts;

namespace Dimmer.WinUI.Utils.Converters;


public class PlayTypeToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        // Default color (Gray/SurfaceStroke)
        SolidColorBrush brush = new SolidColorBrush(Colors.Gray);

        if (value is int intVal)
        {
            brush = GetBrush((PlayEventType)intVal);
        }
        else if (value is PlayEventType typeVal)
        {
            brush = GetBrush(typeVal);
        }

        return brush;
    }

    private SolidColorBrush GetBrush(PlayEventType type)
    {
        return type switch
        {
            // Completed = Green (Success)
            PlayEventType.Completed => new SolidColorBrush(Colors.LimeGreen),

            // Skipped = OrangeRed (Did not finish)
            PlayEventType.Skipped => new SolidColorBrush(Colors.OrangeRed),

            // Active events = System Accent Color
            PlayEventType.Play or PlayEventType.Resume =>
                (SolidColorBrush)Microsoft.UI.Xaml.Application.Current.Resources["SystemControlHighlightAccentBrush"],

            // Neutral events
            PlayEventType.Pause => new SolidColorBrush(Colors.Yellow),
            PlayEventType.Restarted => new SolidColorBrush(Colors.DeepSkyBlue),

            _ => new SolidColorBrush(Colors.Gray)
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}