namespace Dimmer.Utils;

internal static class UIStaticUtils
{
    public static LinearLayout.LayoutParams CreateGridParams(int height, float weight = 0)
    {
        // If weight > 0, width is 0 (optimization). If weight == 0, width is WrapContent.
        int width = weight > 0 ? 0 : ViewGroup.LayoutParams.WrapContent;

        return new LinearLayout.LayoutParams(width, height, weight);
    }
}
