using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.TypeConverters;
public partial class PercentageInverterConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            double d => 1.0 - d,
            float f => 1.0f - f,
            int i => 100 - i,
            long l => 100 - l,
            _ => throw new ArgumentException("Unsupported type for percentage inversion", nameof(value))
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            double d => 1.0 - d,
            float f => 1.0f - f,
            int i => 100 - i,
            long l => 100 - l,
            _ => throw new ArgumentException("Unsupported type for percentage inversion", nameof(value))
        };

        //how to use this converter in C#?
        // Example usage in C#:
        // var converter = new PercentageInverterConverter();
        // double invertedValue = (double)converter.Convert(0.75, typeof(double), null, CultureInfo.InvariantCulture);
        // Console.WriteLine(invertedValue); // Output: 0.25
        // Note: The ConvertBack method is the same as Convert in this case, as the operation is symmetric.
        // Example usage in XAML:
        // <Window.Resources>
        //     <local:PercentageInverterConverter x:Key="PercentageInverterConverter" />
        // </Window.Resources>
        // <TextBlock Text="{Binding SomePercentageValue, Converter={StaticResource PercentageInverterConverter}}" />

    }
}
