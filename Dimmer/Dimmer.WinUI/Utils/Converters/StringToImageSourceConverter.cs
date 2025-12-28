using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.WinUI.Utils.Converters;


public class StringToImageSourceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string path && !string.IsNullOrEmpty(path))
        {
            // If it's a web URL
            if (path.StartsWith("http"))
            {
                return new BitmapImage(new Uri(path));
            }
            // If it's a local file path (ms-appx:// or absolute path)
            // For simple "user.png" in Assets, assume ms-appx
            if (!path.Contains(":") && !path.StartsWith("/"))
            {
                return new BitmapImage(new Uri($"ms-appx:///Assets/{path}"));
            }

            try
            {
                return new BitmapImage(new Uri(path));
            }
            catch { 
                return null; 
            }
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}