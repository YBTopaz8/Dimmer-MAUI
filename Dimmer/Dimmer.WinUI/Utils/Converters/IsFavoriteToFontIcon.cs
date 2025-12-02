using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.WinUI.Utils.Converters;

public partial class IsFavoriteToFontIcon : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var boolVal = (bool)value;
        if(boolVal)
        {
            FontIcon icon = new FontIcon();
            icon.Glyph = "\uEB52";
            return icon;
        }
        FontIcon iconUnFav = new FontIcon();
        iconUnFav.Glyph = "\uEB51";
        return iconUnFav;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
