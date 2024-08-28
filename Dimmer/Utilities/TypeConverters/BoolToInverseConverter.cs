﻿namespace Dimmer.Utilities.TypeConverters;
public class BoolToInverseConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        switch (parameter)
        {
            case "playbtn":
                if ((bool)value!)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            case "pausebtn":
                if ((bool)value!)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            default:
                return !(bool)value!;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
