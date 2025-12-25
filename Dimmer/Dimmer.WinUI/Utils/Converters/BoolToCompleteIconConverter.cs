using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.WinUI.Utils.Converters;

// BoolToCompleteIconConverter.cs
public class BoolToCompleteIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (bool)value ? "\uE73E" : "\uE711"; // Checkmark vs X
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

// BoolToCompleteColorConverter.cs
public class BoolToCompleteColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (bool)value ? new SolidColorBrush(Colors.LightGreen) : new SolidColorBrush(Colors.OrangeRed);
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public partial class PlayEventToColorBrush : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {

        PlayType playType = (PlayType)value;
        switch (playType)
        {
            case PlayType.Play:
                return Colors.Gray;
               
            case PlayType.Pause:
                return Colors.IndianRed;
                
            case PlayType.Resume:
                return Colors.Lavender;
                
            case PlayType.Completed:
                return Colors.Green;
                
            case PlayType.Seeked:
                return Colors.MediumSlateBlue;
                
            case PlayType.Skipped:
                return Colors.Beige;
                
            case PlayType.Restarted:
                return Colors.LimeGreen;
                
            case PlayType.SeekRestarted:
                return Colors.CadetBlue;
                
            case PlayType.CustomRepeat:
                
                break;
            case PlayType.Previous:
                return Colors.LightGray;
            
            case PlayType.ShareSong:
                return Colors.Azure;
                
            case PlayType.ReceiveShare:
                break;
            case PlayType.Favorited:
                return Colors.DeepPink;

            default:
                break;

        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}