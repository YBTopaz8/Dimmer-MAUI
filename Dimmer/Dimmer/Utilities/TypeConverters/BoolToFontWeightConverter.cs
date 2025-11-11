using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.TypeConverters;

public class BoolToFontWeightConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isHeader && isHeader)
            return FontAttributes.Bold;
        return FontAttributes.None;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}


public class SectionColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string type)
            return Application.Current?.RequestedTheme == AppTheme.Dark ? Colors.White : Colors.Black;

        type = type.ToLowerInvariant();

        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

        return type switch
        {
            "chorus" or "refrain" => isDark ? Color.FromArgb("#9AD3FF") : Color.FromArgb("#004B87"),
            "verse" or "couplet" => isDark ? Color.FromArgb("#A0FFA0") : Color.FromArgb("#006600"),
            "bridge" or "pont" => isDark ? Color.FromArgb("#FFC896") : Color.FromArgb("#9B4200"),
            "instrumental" => isDark ? Color.FromArgb("#AAAAAA") : Color.FromArgb("#666666"),
            "intro" => isDark ? Color.FromArgb("#E1BFFF") : Color.FromArgb("#5C0080"),
            "outro" => isDark ? Color.FromArgb("#FFB7B7") : Color.FromArgb("#800000"),
            _ => isDark ? Colors.White : Colors.Black
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}