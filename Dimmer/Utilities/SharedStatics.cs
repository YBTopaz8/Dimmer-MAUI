using System.Runtime.CompilerServices;

namespace Dimmer_MAUI.Utilities;


public static class ImageColorExtractor
{
    public static (string DominantHex, string DimmedHex) GetDominantColor(string imagePath)
    {
        using var bitmap = SKBitmap.Decode(imagePath);
        var dominantColor = GetMostFrequentColor(bitmap);
        var dimmedColor = DimColor(dominantColor);

        return (ColorToHex(dominantColor), ColorToHex(dimmedColor));
    }

    private static SKColor GetMostFrequentColor(SKBitmap bitmap)
    {
        var colorCounts = new Dictionary<SKColor, int>();

        for (int x = 0; x < bitmap.Width; x++)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                var color = bitmap.GetPixel(x, y);
                if (color.Alpha > 0) // To ignore transparent pixels
                {
                    if (colorCounts.ContainsKey(color))
                        colorCounts[color]++;
                    else
                        colorCounts[color] = 1;
                }
            }
        }
        return colorCounts.OrderByDescending(kv => kv.Value).First().Key;
    }

    private static SKColor DimColor(SKColor color)
    {
        const float factor = 0.7f; // Adjust brightness factor (0 = black, 1 = same color)
        return new SKColor(
            (byte)(color.Red * factor),
            (byte)(color.Green * factor),
            (byte)(color.Blue * factor),
            color.Alpha);
    }

    private static string ColorToHex(SKColor color)
    {
        return $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
    }

    public static class ViewLocator
    {
        private static Dictionary<object, View> itemViewCache = new();

        public static event Action<View> ViewAppeared;

        public static void AnnounceView(SongModelView item, View view)
        {
            int key = RuntimeHelpers.GetHashCode(item); // Use reference hash of exact instance
            itemViewCache[key] = view;
        }

        public static View GetView(object item)
        {
            int key = RuntimeHelpers.GetHashCode(item);
            if (itemViewCache.TryGetValue(key, out var view))
            {
                return view;
            }
            else
            {
                return null;
            }
        }
    }
}