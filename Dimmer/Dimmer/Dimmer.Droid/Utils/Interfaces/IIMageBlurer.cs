using Android.Graphics;
using Android.OS;
using Android.Renderscripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Element = Android.Renderscripts.Element;

namespace Dimmer.Utils.Interfaces;
public interface IImageBlurrer
{
    Task<Bitmap> BlurBitmapAsync(Bitmap source, float radius, Context context);
}

public class ImageBlurrer : IImageBlurrer
{
    // Simple Stack Blur implementation (example, not as good as Gaussian)
    // For production, prefer RenderScript (if supporting < API 31) or RenderEffect
    public Task<Bitmap> BlurBitmapAsync(Bitmap source, float radius, Context context)
    {
        // --- RenderEffect example (API 31+) ---
        if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
        {
            return Task.Run(() =>
            {
                var renderEffect = Android.Graphics.RenderEffect.CreateBlurEffect(radius, radius, Shader.TileMode.Decal);
                // This effect needs to be applied to a View or drawn onto a canvas with the effect.
                // To get a blurred BITMAP, you'd draw the source bitmap onto another bitmap
                // via a canvas that has this render effect.
                // This is a simplification; direct bitmap-to-bitmap with RenderEffect is more involved.
                // For simplicity, let's show RenderScript which directly modifies bitmap.
                // For a View: myImageView.SetRenderEffect(renderEffect);
                // Fallback to RenderScript for this example of bitmap processing.
                return BlurWithRenderScript(source, radius, context);
            });
        }

        // --- RenderScript example (fallback for < API 31 or direct bitmap processing) ---
        return Task.Run(() => BlurWithRenderScript(source, radius, context));
    }

    private Bitmap BlurWithRenderScript(Bitmap source, float radius, Context context)
    {
        if (source == null || source.IsRecycled)
            return null;

        Bitmap outputBitmap = Bitmap.CreateBitmap(source.Width, source.Height, source.GetConfig());
        try
        {
            RenderScript rs = RenderScript.Create(context);
            ScriptIntrinsicBlur blurScript = ScriptIntrinsicBlur.Create(rs, Element.U8_4(rs)); // Or Element.A_8 for alpha

            Allocation allocIn = Allocation.CreateFromBitmap(rs, source);
            Allocation allocOut = Allocation.CreateFromBitmap(rs, outputBitmap);

            // Ensure radius is within RenderScript's valid range (0 < radius <= 25)
            float validatedRadius = Math.Max(0.1f, Math.Min(25.0f, radius));
            blurScript.SetRadius(validatedRadius);

            blurScript.SetInput(allocIn);
            blurScript.ForEach(allocOut); // Apply blur
            allocOut.CopyTo(outputBitmap); // Copy result to output bitmap

            rs.Destroy(); // Crucial to release RenderScript resources
            blurScript.Destroy();
            allocIn.Destroy();
            allocOut.Destroy();
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RenderScript Blur error: {ex.Message}");
            // outputBitmap might be partially processed or null, handle gracefully
            if (outputBitmap != null && !outputBitmap.IsRecycled)
                outputBitmap.Recycle();
            return source; // Or null, or a default
        }
        return outputBitmap;
    }
}