﻿using System.Diagnostics;

namespace Dimmer_MAUI.Utilities.TypeConverters;
public class BoolToInverseConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var vm = IPlatformApplication.Current.Services.GetService<HomePageVM>();
        switch (parameter)
        {
            
            case "playbtn":                
                if (!vm.IsPlaying)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            case "pausebtn":
                if (!vm.IsPlaying)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            default:
                return !(bool)value!;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {

        Debug.WriteLine(message: "None");
        return null;
    }
}

public class BoolToYesNoConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var val = (bool)value;
        if (val)
        {
            return "Yes";
        }
        else
        {
            return "No";
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        Debug.WriteLine(message: "None");
        return null;
    }
}
