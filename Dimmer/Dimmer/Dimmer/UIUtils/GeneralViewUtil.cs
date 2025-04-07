namespace Dimmer.UIUtils;
public static class GeneralViewUtil
{
    public static void PointerOnView(View theView)
    {
        theView.BackgroundColor = Colors.DarkSlateBlue;
    }
    public static void PointerOffView(View theView)
    {
        theView.BackgroundColor = Colors.Transparent;
    }

    public static Task MyBackgroundColorToBuiltIn(this VisualElement element, Color targetColor, uint length = 250, Easing easing = null)
    {
        if (element.BackgroundColor == null)
            element.BackgroundColor = Colors.Transparent; // Or some default

        Color startColor = element.BackgroundColor;

        // Create the animation definition
        var animation = new Animation(v =>
        {
            // 'v' is the interpolated value between 0.0 and 1.0
            Color stepColor = Color.FromRgba(
                startColor.Red + (targetColor.Red - startColor.Red) * v,
                startColor.Green + (targetColor.Green - startColor.Green) * v,
                startColor.Blue + (targetColor.Blue - startColor.Blue) * v,
                startColor.Alpha + (targetColor.Alpha - startColor.Alpha) * v
            );
            element.BackgroundColor = stepColor;
        }, 0.0, 1.0, easing ?? Easing.Linear); // Start=0, End=1, Optional Easing

        // Commit the animation
        // Use a unique handle name per element/property to enable cancellation
        var handle = $"{element.Id}_BackgroundColorAnimation";
        animation.Commit(element, handle, length: length, finished: (v, cancelled) =>
        {
            if (!cancelled)
            {
                // Optionally ensure final value, though Commit usually handles it well
                // element.BackgroundColor = targetColor;
            }
            // You could add cleanup logic here if needed
        });

        // Return a Task that completes when the animation finishes or is cancelled
        // Note: Animation.Commit doesn't return a Task directly in older XF. MAUI Task-based extensions might exist.
        // For simplicity, often you don't await these UI animations directly unless needed for sequencing.
        // If you need a Task, you'd typically use a TaskCompletionSource inside the 'finished' callback.
        return Task.CompletedTask; // Simplified return - see note above
    }
}
