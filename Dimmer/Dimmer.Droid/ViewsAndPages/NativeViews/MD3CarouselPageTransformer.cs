using Android.Views;
using AndroidX.ViewPager2.Widget;

namespace Dimmer.ViewsAndPages.NativeViews;

/// <summary>
/// Page transformer that provides Material Design 3 style push animation for carousel
/// </summary>
public class MD3CarouselPageTransformer : Java.Lang.Object, ViewPager2.IPageTransformer
{
    private const float MIN_SCALE = 0.85f;
    private const float MIN_ALPHA = 0.5f;

    public void TransformPage(View page, float position)
    {
        if (page == null) return;

        int pageWidth = page.Width;
        int pageHeight = page.Height;

        if (position < -1)
        {
            // Page is way off-screen to the left
            page.Alpha = 0f;
        }
        else if (position <= 1)
        {
            // Page is being swiped
            // MD3 push-style: pages slide in from the side with scale and fade
            
            // Scale the page down (between MIN_SCALE and 1)
            float scaleFactor = Math.Max(MIN_SCALE, 1 - Math.Abs(position) * 0.15f);
            page.ScaleX = scaleFactor;
            page.ScaleY = scaleFactor;

            // Fade the page based on its position
            float alphaFactor = Math.Max(MIN_ALPHA, 1 - Math.Abs(position));
            page.Alpha = alphaFactor;

            // Apply translation for smooth push effect
            // This creates the "push" feeling where pages move together
            if (position < 0)
            {
                // Moving to the left
                page.TranslationX = pageWidth * -position * 0.25f;
            }
            else
            {
                // Moving to the right
                page.TranslationX = pageWidth * -position * 0.25f;
            }

            // Add elevation effect for depth
            page.TranslationZ = -Math.Abs(position) * 10;
        }
        else
        {
            // Page is way off-screen to the right
            page.Alpha = 0f;
        }
    }
}
