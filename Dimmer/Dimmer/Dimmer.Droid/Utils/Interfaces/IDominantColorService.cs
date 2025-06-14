using Android.Graphics;

using AndroidX.Palette.Graphics;

using Color = Microsoft.Maui.Graphics.Color;

namespace Dimmer.Utils.Interfaces;
internal interface IDominantColorService
{
    Task<Color> GetDominantColorAsync(Stream imageStream);

}
public class DominantColorService : IDominantColorService
{
    public async Task<Color> GetDominantColorAsync(Stream imageStream)
    {
        // decode Android bitmap
        var bitmap = await BitmapFactory.DecodeStreamAsync(imageStream);
        if (bitmap == null)
            return Colors.Transparent;

        // generate palette (off UI thread)
        var palette = await Task.Run(() => Palette.From(bitmap).Generate());
        var swatch = palette.DominantSwatch;
        if (swatch == null)
            return Colors.Transparent;

        // convert Android RGB to MAUI Color
        var rgb = swatch.Rgb;
        var a = (byte)((rgb >> 24) & 0xFF);
        var r = (byte)((rgb >> 16) & 0xFF);
        var g = (byte)((rgb >> 8)  & 0xFF);
        var b = (byte)(rgb & 0xFF);
        return Color.FromRgba(r, g, b, a);
    }
}