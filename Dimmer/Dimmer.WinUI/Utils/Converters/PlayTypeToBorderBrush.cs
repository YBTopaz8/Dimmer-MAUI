using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Color = Windows.UI.Color;

namespace Dimmer.WinUI.Utils.Converters;

/// <summary>
/// Indicates the type of play action performed.
/// Possible VALID values for <see cref="PlayType" />:
/// <list type="bullet"><item>
/// <term>0</term>
/// <description>Play</description>
/// </item>
/// <item>
/// <term>1</term>
/// <description>Pause</description>
/// </item>
/// <item>
/// <term>2</term>
/// <description>Resume</description>
/// </item>
/// <item><term>3</term><description>Completed</description></item><item><term>4</term><description>Seeked</description></item><item><term>5</term><description>Skipped</description></item><item><term>6</term><description>Restarted</description></item><item><term>7</term><description>SeekRestarted</description></item><item><term>8</term><description>CustomRepeat</description></item><item><term>9</term><description>Previous</description></item></list>
/// </summary>
/// <value>
/// The type of the play.
/// </value>
public sealed partial class PlayTypeToBorderBrush : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var playType = (int)value;
        if (playType == 3)
        {

            var successBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80)); // Green color for Completed
            return successBrush;
        }
        else if (playType == 0)
        {
            var playBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 33, 150, 243)); // Blue color for Play
            return playBrush;
        }
        else if (playType == 1)
        {
            var pauseBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 193, 7)); // Yellow color for Pause
            return pauseBrush;
        }
        else if (playType == 5)
        {
            var skipBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 244, 67, 54)); // Red color for Skipped
            return skipBrush;
        }
        else if (playType == 4)
        {
            var seekBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 156, 39, 176)); // Purple color for Seeked
            return seekBrush;
        }
        else if (playType == 2)
        {
            var resumeBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 150, 136)); // Teal color for Resume
            return resumeBrush;
        }
        else if(playType == 9)
        {
            var previousBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 121, 85, 72)); // Brown color for Previous
            return previousBrush;
        }
        else if (playType == 6)
        {
            var restartedBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 63, 81, 181)); // Indigo color for Restarted
            return restartedBrush;
        }
        else if (playType == 7)
        {
            var seekRestartedBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 188, 212)); // Cyan color for SeekRestarted
            return seekRestartedBrush;
        }
        else if (playType == 8)
        {
            var customRepeatBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 205, 220, 57)); // Lime color for CustomRepeat
            return customRepeatBrush;
        }
        else
        {
            var defaultBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 158, 158, 158)); // Grey color for others
            return defaultBrush;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
