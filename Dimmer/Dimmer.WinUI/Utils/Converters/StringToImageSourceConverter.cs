using Microsoft.UI.Xaml.Media.Imaging;

namespace Dimmer.WinUI.Utils.Converters;


public class StringToImageSourceConverter : IValueConverter
{
    /// <summary>
    /// Default decode width in pixels. 
    /// Set to 0 to load full resolution (e.g., for full-screen player views).
    /// Defaults to 150 for minimal RAM usage in lists/grids.
    /// </summary>
    public int DefaultDecodePixelWidth { get; set; } = 150;

    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not string rawPath || string.IsNullOrWhiteSpace(rawPath))
        {
            return null;
        }

        string path = rawPath.Trim();

        try
        {
            Uri uri;

            // 1. Web URLs
            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                uri = new Uri(path);
            }
            // 2. Packaged App Assets
            else if (path.StartsWith("ms-appx://", StringComparison.OrdinalIgnoreCase) ||
                     path.StartsWith("ms-appdata://", StringComparison.OrdinalIgnoreCase))
            {
                uri = new Uri(path);
            }
            // 3. Absolute local Windows file path (e.g. C:\Music\cover.jpg)
            else if (Path.IsPathRooted(path))
            {
                uri = new Uri(path);
            }
            // 4. Fallback: Relative asset file
            else
            {
                string cleanPath = path.TrimStart('/', '\\');
                if (!cleanPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    cleanPath = $"Assets/{cleanPath}";
                }
                uri = new Uri($"ms-appx:///{cleanPath}");
            }

            var bitmap = new BitmapImage(uri);

            // Determine target decode size:
            // 1st priority: Parameter passed in XAML (e.g., ConverterParameter=300)
            // 2nd priority: DefaultDecodePixelWidth property on the converter instance
            int targetWidth = DefaultDecodePixelWidth;

            if (parameter is string paramStr && int.TryParse(paramStr, out int customWidth))
            {
                targetWidth = customWidth;
            }

            // Only constrain decode size if > 0 (0 means full unconstrained original size)
            if (targetWidth > 0)
            {
                bitmap.DecodePixelType = DecodePixelType.Logical;
                bitmap.DecodePixelWidth = targetWidth;

            }

            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}