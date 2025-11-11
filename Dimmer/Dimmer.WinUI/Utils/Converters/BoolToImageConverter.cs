using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.WinUI.Utils.Converters;

public partial class BoolToImageConverter : IValueConverter
{
    public object TrueValue { get; set; }
    public object FalseValue { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, string culture)
    => value is bool b && b ? TrueValue : FalseValue;


    public object? ConvertBack(object? value, Type targetType, object? parameter, string culture)
    {
        throw new NotImplementedException();
    }
}