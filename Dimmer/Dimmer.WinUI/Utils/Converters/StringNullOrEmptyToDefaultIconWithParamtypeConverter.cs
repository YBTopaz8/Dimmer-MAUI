using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.WinUI.Utils.Converters;

public partial class StringNullOrEmptyToDefaultIconWithParamtypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var paramStr = parameter as string;
        var str = value as string;
        if (string.IsNullOrEmpty(str))
        { 
            switch (paramStr)
            {
                case "UserIcon":
                        return "\uE77B"; // Default user icon
                case "MusicIcon":
                        return "\uE189"; // Default music icon
                case "PlaylistIcon":
                        return "\uE8F1"; // Default playlist icon
                case "AlbumIcon":
                        return "\uE7C5"; // Default album icon
                case "ArtistIcon":
                        return "\uE8D7"; // Default artist icon
                case "FolderIcon":
                        return "\uE8B7"; // Default folder icon

            }
        }
            return str;
        
            
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
