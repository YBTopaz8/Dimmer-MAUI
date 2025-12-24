using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.WinUI.Utils.Converters;

public sealed partial class EventDateToTimeAgo : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        try
        {
            if (value is null) return "unknown time ago";
            var eventDate = (DateTime)value;
            var timeSpan = DateTime.Now - eventDate;
            if (timeSpan.TotalSeconds < 60)
                return $"{(int)timeSpan.TotalSeconds} seconds ago";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minutes ago";
            if(timeSpan.TotalHours < 1)
                return "less than an hour ago";
            if(timeSpan.TotalHours == 1)
                return "an hour ago";

            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hours ago";
            if(timeSpan.TotalDays == 1)
                return "yesterday";
            if (timeSpan.TotalDays < 30)
                return $"{(int)timeSpan.TotalDays} days ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} months ago";
            return $"{(int)(timeSpan.TotalDays / 365)} years ago";

        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return "unknown time ago";
        }

    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public sealed partial class EventDateOffsetToTimeAgo : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var eventDate = (DateTimeOffset)value;
        var timeSpan = DateTimeOffset.Now - eventDate;
        if (timeSpan.TotalSeconds < 60)
            return $"{(int)timeSpan.TotalSeconds} seconds ago";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} minutes ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} hours ago";
        if (timeSpan.TotalDays < 30)
            return $"{(int)timeSpan.TotalDays} days ago";   
        if (timeSpan.TotalDays < 365)
            return $"{(int)(timeSpan.TotalDays / 30)} months ago";
        return $"{(int)(timeSpan.TotalDays / 365)} years ago";

    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
