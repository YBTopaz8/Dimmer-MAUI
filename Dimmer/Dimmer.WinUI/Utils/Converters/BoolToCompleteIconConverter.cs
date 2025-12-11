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