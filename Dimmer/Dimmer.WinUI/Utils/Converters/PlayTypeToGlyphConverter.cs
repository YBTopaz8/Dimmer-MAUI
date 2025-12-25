using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dimmer.Charts;

namespace Dimmer.WinUI.Utils.Converters;


public partial class PlayTypeToGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        // Default to a simple dot if unknown
        string glyph = "\uE915";
        int intVal = 0;
        if (value is int)
        {
            glyph = GetGlyph((PlayEventType)intVal);
        }
        else if (value is PlayEventType typeVal)
        {
            glyph = GetGlyph(typeVal);
        }
        else if(value is string strVal && Enum.TryParse<PlayEventType>(strVal, out var parsedType))
        {
            glyph = GetGlyph(parsedType);
        }

        return glyph;
    }

    private static string GetGlyph(PlayEventType type)
    {
        return type switch
        {
            PlayEventType.Play => "\uE768",          // Play Icon
            PlayEventType.Pause => "\uE769",         // Pause Icon
            PlayEventType.Resume => "\uE768",        // Play Icon (Resume)
            PlayEventType.Completed => "\uE73E",     // Checkmark (Success)
            PlayEventType.Seeked => "\uE762",        // Progress Ring / Seek
            PlayEventType.Skipped => "\uE711",       // 'X' (Close) - indicating it wasn't finished
            PlayEventType.Restarted => "\uE72C",     // Rotate/Refresh
            PlayEventType.SeekRestarted => "\uE72C", // Rotate/Refresh
            PlayEventType.CustomRepeat => "\uE8EE",  // Repeat
            PlayEventType.Previous => "\uE892",      // Previous
            _ => "\uF133"                            // Unknown question mark
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}