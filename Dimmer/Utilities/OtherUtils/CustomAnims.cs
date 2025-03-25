using Microsoft.Maui.Controls.Shapes;

namespace Dimmer_MAUI.Utilities.OtherUtils;

public static class CustomAnimsExtensions
{
    // --- Existing Animations (kept for completeness) ---
    public static async Task AnimateHighlightPointerPressed(this View element)
    {
        await element.ScaleTo(0.95, 80, Easing.CubicIn);
    }
    public static async Task AnimateHighlightPointerReleased(this View element)
    {
        await element.ScaleTo(1.0, 80, Easing.CubicOut);
    }

    public static async Task DimmOut(this View element, double duration = 350, double endOpacity=0.70)
    {
        await element.FadeTo(endOpacity, (uint)duration, Easing.CubicIn);
    }
    public static async Task DimmIn(this View element, double duration = 350)
    {
        await element.FadeTo(1.0, (uint)duration, Easing.CubicOut);
        

    }
    public static async Task DimmOutCompletely(this View element, double duration = 350)
    {
        await element.FadeTo(0.01, (uint)duration, Easing.CubicIn);
    }
    public static async Task DimmOutCompletelyAndHide(this View element, double duration = 350)
    {
        await element.FadeTo(0.01, (uint)duration, Easing.CubicIn);
        element.IsVisible=false;
    }
    public static async Task DimmInCompletely(this View element, double duration = 350)
    {
        await element.FadeTo(1.0, (uint)duration, Easing.CubicOut);

    }
    public static async Task DimmInCompletelyAndShow(this View element, double duration = 350)
    {
        await element.FadeTo(1.0, (uint)duration, Easing.CubicOut);
        element.IsVisible=true;

    }

    public static async Task AnimateRippleBounce(this View element, int bounceCount = 3, double bounceHeight = 20, uint duration = 200)
    {
        for (int i = 0; i < bounceCount; i++)
        {
            // Move the view down
            await element.TranslateTo(0, bounceHeight, duration / 2, Easing.CubicIn);

            // Move the view back up
            await element.TranslateTo(0, 0, duration / 2, Easing.CubicOut);

            // Gradually reduce bounce height for the next bounce
            bounceHeight *= 0.5; // Diminishes like a ripple
        }
    }
    public static async Task AnimateHeight(this View view, double targetHeight, uint duration = 250, Easing? easing = null)
    {
        Animation animation = new Animation(v => view.HeightRequest = v, view.HeightRequest, targetHeight);
        animation.Commit(view, "HeightAnimation", 16, duration, easing, null, null);

        await Task.Delay((int)duration);
    }
    public static async Task AnimateFocusModePointerEnter(this View element, double duration = 250, double endScale = 1)
    {
        // Animate scale-up to 1.2 and opacity to 1 with a smooth transition
        await Task.WhenAll(
            element.ScaleTo(endScale, (uint)duration, Easing.CubicInOut),
            element.FadeTo(1.0, (uint)duration, Easing.CubicInOut)
        );
    }


    public static async Task AnimateFocusModePointerExited(this View element, double duration = 250, double endOpacity = 0.7, double endScale = 0.7)
    {
        // Animate scale-down to 0.8 and opacity to 0.7 with a smooth transition
        await Task.WhenAll(
            element.ScaleTo(endScale, (uint)duration, Easing.CubicInOut),
            element.FadeTo(endOpacity, (uint)duration, Easing.CubicInOut)
        );
    }

    // Extension method to fade out and slide back a view
    public static async Task AnimateFadeOutBack(this View element, uint duration = 250)
    {
        await Task.WhenAll(
            element.FadeTo(0, duration, Easing.CubicInOut), // Fade out
            element.TranslateTo(0, 50, duration, Easing.CubicInOut) // Slide back by 50 units on Y-axis
        );
        element.IsVisible = false; // Hide the view after animation
    }

    // Extension method to fade in and slide forward a view
    public static async Task AnimateFadeInFront(this View element, uint duration = 250)
    {
        element.IsVisible = true; // Show the view before animation
        element.Opacity = 0; // Ensure the view is initially transparent
        element.TranslationY = 50; // Start with the view slightly back
        await Task.WhenAll(
            element.FadeTo(1, duration, Easing.CubicInOut), // Fade in
            element.TranslateTo(0, 0, duration, Easing.CubicInOut) // Slide forward to original position
        );
    }

    public static async Task AnimateSlideDown(this View element, double heightToSlide)
    {
        await element.TranslateTo(0, heightToSlide, 250, Easing.CubicInOut);
        element.HeightRequest = element.Height - heightToSlide;
    }

    public static async Task AnimateSlideUp(this View element, double heightToSlide)
    {
        await element.TranslateTo(0, 0, 250, Easing.CubicInOut);
        element.HeightRequest = element.Height + heightToSlide;
    }

    public static async Task AnimateFontSizeTo(this Label view, double toFontSize, uint duration)
    {
        await view.AnimateAsync("fontSizeAnimation", (progress) =>
        {
            view.FontSize = 19 + (toFontSize - 19) * progress;
        }, length: duration);
    }

    public static async Task AnimateTextColorTo(this Label view, Color toColor, uint duration)
    {
        Color fromColor = view.TextColor;
        await view.AnimateAsync("textColorAnimation", (progress) =>
        {
            view.TextColor = Color.FromRgba(
                (int)(fromColor.Red + (toColor.Red - fromColor.Red) * progress),
                (int)(fromColor.Green + (toColor.Green - fromColor.Green) * progress),
                (int)(fromColor.Blue + (toColor.Blue - fromColor.Blue) * progress),
                fromColor.Alpha + (toColor.Alpha - fromColor.Alpha) * progress);
        }, length: duration);
    }

    // IMPORTANT: This is the implementation of AnimateAsync
    public static Task AnimateAsync(this IAnimatable element, string name, Action<double> callback, uint length, Easing easing = null)
    {
        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
        element.Animate(name, callback, 16, length, easing, (v, c) => tcs.SetResult(c), null);
        return tcs.Task;
    }


    // --- New Animations for the Floating Island ---

    // Initial "pulse" animation for the island (subtle attention-grabber)
    public static async Task AnimateIslandPulse(this View island, double scale = 1.1, uint duration = 500)
    {
        // Scale up and down, creating a pulse effect
        await island.ScaleTo(scale, duration / 2, Easing.SinInOut);
        await island.ScaleTo(1.0, duration / 2, Easing.SinInOut);
    }

    // Expand the island and reveal controls
    public static async Task AnimateIslandExpand(this View island, IList<View> controls, uint duration = 300)
    {
        // 1. Scale the island slightly
        await island.ScaleTo(1.2, duration, Easing.CubicOut);

        // 2. Fade in and position the controls (assuming they're initially hidden)
        foreach (View control in controls)
        {
            control.Opacity = 0; // Start invisible
            control.IsVisible = true;

            // Example positioning (adjust based on your layout):
            //  - Place controls around the island, potentially using a GridLayout
            //  - You might need to set TranslationX/Y initially to make them appear
            //    to "emerge" from the island.

            await control.FadeTo(1.0, duration, Easing.CubicOut);
        }
    }

    // Collapse the island and hide controls
    public static async Task AnimateIslandCollapse(this View island, IList<View> controls, uint duration = 200)
    {
        // 1. Fade out and hide the controls
        foreach (View control in controls)
        {
            await control.FadeTo(0.0, duration, Easing.CubicIn);
            control.IsVisible = false;
        }

        // 2. Scale the island back to its original size
        await island.ScaleTo(1.0, duration, Easing.CubicIn);
    }

    // Animation for when a new track starts (a "bounce" or "pop")
    public static async Task AnimateNewTrackBounce(this View island, double scale = 1.3, uint duration = 200)
    {
        // Quick scale up and down
        await island.ScaleTo(scale, duration / 2, Easing.CubicOut);
        await island.ScaleTo(1.0, duration / 2, Easing.CubicIn);
    }
    // Rotate animation
    public static async Task AnimateRotate(this View view, double rotation, uint duration = 250, Easing easing = null)
    {
        await view.RotateTo(rotation, duration, easing ?? Easing.Linear);
    }

    // Rotate and scale animation
    public static async Task AnimateRotateAndScale(this View view, double rotation, double scale, uint duration = 250, Easing easing = null)
    {
        await Task.WhenAll(
            view.RotateTo(rotation, duration, easing ?? Easing.Linear),
            view.ScaleTo(scale, duration, easing ?? Easing.Linear)
        );
    }
    // Color change animation (for the island's background, for example)
    public static async Task AnimateIslandColorChange(this View island, Color fromColor, Color toColor, uint duration = 400)
    {
        // Assumes you have a way to set the island's background color
        // (e.g., a property like IslandBackgroundColor)

        await island.AnimateAsync("islandColorChange", (progress) =>
        {
            island.BackgroundColor = Color.FromRgba(
                fromColor.Red + (toColor.Red - fromColor.Red) * progress,
                fromColor.Green + (toColor.Green - fromColor.Green) * progress,
                fromColor.Blue + (toColor.Blue - fromColor.Blue) * progress,
                fromColor.Alpha + (toColor.Alpha - fromColor.Alpha) * progress
            );
        }, length: duration);
    }
    //Shaking for easter eggs
    public static async Task AnimateShake(this View view, int shakes = 4, double distance = 10, uint duration = 50)
    {
        for (int i = 0; i < shakes; i++)
        {
            await view.TranslateTo(distance, 0, duration, Easing.Linear);
            await view.TranslateTo(-distance, 0, duration, Easing.Linear);
        }
        await view.TranslateTo(0, 0, duration, Easing.Linear); // Return to original position
    }

    public static async Task Pulse(this VisualElement element, double scale = 1.1, uint duration = 100)
    {
        await element.ScaleTo(scale, duration / 2, Easing.CubicOut);
        await element.ScaleTo(1, duration / 2, Easing.CubicIn);
    }

    public static async Task FadeIn(this VisualElement element, uint duration = 250, double startOpacity = 0)
    {
        element.Opacity = startOpacity;
        await element.FadeTo(1, duration, Easing.Linear);
    }

    public static async Task FadeOut(this VisualElement element, uint duration = 200, double endOpacity = 0)
    {
        await element.FadeTo(endOpacity, duration, Easing.Linear);
    }
    public static async Task SlideInFromRight(this VisualElement element, uint duration = 300)
    {
        element.Opacity = 0;
        element.TranslationX = Shell.Current.Width;
        element.IsVisible = true; // Ensure it's visible before animating

        await Task.WhenAll(
            element.FadeIn(duration), // Use the FadeIn extension method
            element.TranslateTo(0, 0, duration, Easing.CubicOut)
        );
    }

    public static async Task SlideOutToRight(this VisualElement element, uint duration = 250)
    {
        await Task.WhenAll(
            element.FadeOut(duration), // Use the FadeOut extension method
            element.TranslateTo(Shell.Current.Width, 0, duration, Easing.CubicIn)
        );
        element.IsVisible = false;
    }

    public static async Task SlideInFromLeft(this VisualElement element, uint duration = 300)
    {
        element.Opacity = 0;
        element.TranslationX = -Shell.Current.Width;
        element.IsVisible = true; // Ensure it's visible before animating
        await Task.WhenAll(
            element.FadeIn(duration),
            element.TranslateTo(0, 0, duration, Easing.CubicOut)
        );
    }

    public static async Task SlideOutToLeft(this VisualElement element, uint duration = 250)
    {
        await Task.WhenAll(
            element.FadeOut(duration),
            element.TranslateTo(-Shell.Current.Width, 0, duration, Easing.CubicIn)
        );
        element.IsVisible = false;
    }

    public static async Task SlideInFromBottom(this VisualElement element, uint duration = 300)
    {
        element.Opacity = 0;
        element.TranslationY = Application.Current.MainPage.Height;
        element.IsVisible = true;
        await Task.WhenAll(
            element.FadeIn(duration),
            element.TranslateTo(0, 0, duration, Easing.CubicOut)
        );
    }

    public static async Task SlideOutToBottom(this VisualElement element, uint duration = 250)
    {
        await Task.WhenAll(
            element.FadeOut(duration),
            element.TranslateTo(0, Application.Current.MainPage.Height, duration, Easing.CubicIn)
        );
        element.IsVisible = false;
    }

    public static async Task Expand(this VisualElement element, double targetWidth, double targetHeight, uint duration = 300)
    {
        // Initial Pulse
        await element.Pulse(1.05, 80);
        double initialWidth = element.Width;
        double initialHeight = element.Height;

        // Asymmetrical Expansion (Width first)
        Animation widthAnimation = new Animation(v => element.WidthRequest = v, initialWidth, targetWidth, Easing.CubicOut);
        widthAnimation.Commit(element, "WidthExpansion", length: ((uint)(duration * 0.8))); // 80% of duration
        await Task.Delay(25);
        Animation heightAnimation = new Animation(v => element.HeightRequest = v, initialHeight, targetHeight, Easing.CubicOut);
        heightAnimation.Commit(element, "HeightExpansion", length: duration);
        await Task.Delay(50);

    }

    public static async Task Shrink(this VisualElement element, double targetWidth, double targetHeight, uint duration = 250)
    {

        double initialWidth = element.Width;
        double initialHeight = element.Height;

        // Asymmetrical Contraction (Height first)
        Animation heightAnimation = new Animation(v => element.HeightRequest = v, initialHeight, targetHeight, Easing.CubicIn);
        heightAnimation.Commit(element, "HeightContraction", length: duration);
        await Task.Delay(50);

        Animation widthAnimation = new Animation(v => element.WidthRequest = v, initialWidth, targetWidth, Easing.CubicIn);
        widthAnimation.Commit(element, "WidthContraction", length: (uint)(duration * 0.8)); // 80% of duration
        await Task.Delay(25);

        // Final Pulse
        await element.Pulse(1.03, 100);
    }

    public static async Task Rotate(this VisualElement element, double degrees, uint duration = 250)
    {
        await element.Pulse(); // Pulse before rotation
        await element.RotateTo(degrees, duration, Easing.CubicInOut); // Use CubicInOut for smooth rotation
    }
    public static async Task Shake(this VisualElement element, uint duration = 300, double distance = 10)
    {
        // Quick, small translations left and right.
        uint shakeDuration = duration / 6;
        await element.TranslateTo(distance, 0, shakeDuration, Easing.Linear);
        await element.TranslateTo(-distance, 0, shakeDuration, Easing.Linear);
        await element.TranslateTo(distance, 0, shakeDuration, Easing.Linear);
        await element.TranslateTo(-distance, 0, shakeDuration, Easing.Linear);
        await element.TranslateTo(0, 0, shakeDuration, Easing.Linear); // Return to center.
    }
    public static async Task BounceIn(this VisualElement element, uint duration = 400)
    {
        element.Scale = 0; // Start small
        element.Opacity = 0;
        element.IsVisible = true;
        await Task.WhenAll(
            element.ScaleTo(1, duration, Easing.SpringOut), // Use SpringOut for bounce
            element.FadeIn(duration - 100) // Fade in a bit quicker
        );
    }

    public static async Task BounceOut(this VisualElement element, uint duration = 350)
    {
        //quick scale up, scale down with spring out, fade out.

        await element.ScaleTo(1.2, duration / 4, Easing.CubicOut); //quick scale up
        await element.ScaleTo(0, duration * 3 / 4, Easing.SpringOut); //scale down to 0
        await element.FadeOut(duration * 3 / 4);
        element.IsVisible = false;
    }

    public static async Task Flash(this VisualElement element, uint duration = 200)
    {
        // Quickly fade to white and back.
        await element.FadeTo(0, duration / 2, Easing.Linear);
        await element.FadeTo(1, duration / 2, Easing.Linear);
    }

    // In a static class for extension methods
    public static async Task RippleExpand(this VisualElement trigger, BoxView overlay, Color rippleColor, uint duration = 400)
    {
        // 1. Position the overlay at the trigger's location.
        overlay.TranslationX = trigger.X + trigger.Width / 2 - overlay.Width / 2;
        overlay.TranslationY = trigger.Y + trigger.Height / 2 - overlay.Height / 2;
        overlay.BackgroundColor = rippleColor.WithAlpha(0.7f); // Semi-transparent
        overlay.CornerRadius = (float)(Math.Max(trigger.Width, trigger.Height) * 2); // Start as a circle
        overlay.IsVisible = true;
        overlay.Scale = 0.1; // Start small

        // 2. Animate the overlay's scale and opacity.
        await Task.WhenAll(
            overlay.ScaleTo(2, duration, Easing.CubicOut), // Expand significantly
            overlay.FadeTo(0, duration, Easing.CubicOut)  // Fade out
        );
        overlay.IsVisible = false;

        // 3. Optional: Pulse the trigger element.
        await trigger.Pulse();
    }

    // Example Usage (XAML):
    // <Grid>
    //     <Button x:Name="MyButton" Text="Tap Me" />
    //     <BoxView x:Name="RippleOverlay" WidthRequest="50" HeightRequest="50" IsVisible="False" />
    // </Grid>

    // Example Usage (C#):
    // MyButton.Clicked += async (s, e) => await MyButton.RippleExpand(RippleOverlay, Colors.LightBlue);


    public static async Task FlipCard(this VisualElement frontView, VisualElement backView, bool toBack, uint duration = 400)
    {
        // Ensure both views have the same dimensions.  This is crucial for a smooth flip.
        backView.WidthRequest = frontView.Width;
        backView.HeightRequest = frontView.Height;
        backView.RotationY = toBack ? -180 : 0; //backview initial position
        frontView.RotationY = toBack ? 0 : 180;

        // Hide the view that's being flipped *to* initially.
        if (toBack)
        {
            frontView.IsVisible = true;
            backView.IsVisible = false;

        }
        else
        {
            frontView.IsVisible = false;
            backView.IsVisible = true;
        }

        // Asymmetrical timing:  Rotation starts slightly before the visibility switch.
        await Task.WhenAll(
            frontView.RotateYTo(toBack ? 90 : -90, duration / 2, Easing.CubicIn),
            backView.RotateYTo(toBack ? -90 : 90, duration / 2, Easing.CubicIn)
        );
        frontView.IsVisible = !toBack;
        backView.IsVisible = toBack;

        await Task.WhenAll(
            frontView.RotateYTo(toBack ? 180 : -180, duration / 2, Easing.CubicOut),
            backView.RotateYTo(toBack ? 0 : 180, duration / 2, Easing.CubicOut)
        );
    }

    //XAML example
    // <Grid>
    //    <Frame x:Name="CardFront" BackgroundColor="LightBlue">
    //content
    //    </Frame>
    //      <Frame x:Name="CardBack" BackgroundColor="Yellow">
    //content
    //    </Frame>
    //  <Grid.GestureRecognizers>
    //<TapGestureRecognizer Tapped="OnCardTapped" />
    //</Grid.GestureRecognizers>
    //</Grid>

    // C# example
    // private bool _isFront = true;
    // private async void OnCardTapped(object sender, TappedEventArgs e)
    // {
    //    await CardFront.FlipCard(CardBack, _isFront);
    //     _isFront = !_isFront;
    // }

    public static async Task StaggeredFadeIn(this Layout layout, uint duration = 300, int delay = 50)
    {
        // Iterate through the *children* of the Layout.
        foreach (VisualElement child in layout.Children.OfType<VisualElement>())
        {
            child.Opacity = 0;
            child.IsVisible = true; // Make sure they're visible
            await child.FadeIn(duration);
            await Task.Delay(delay); // Delay before the next child.
        }
    }

    //example:
    // <StackLayout x:Name="MyStackLayout">
    //     <Label Text="Item 1" />
    //     <Label Text="Item 2" />
    //     <Label Text="Item 3" />
    // </StackLayout>

    // MyStackLayout.StaggeredFadeIn();

    public static async Task SequentialSlideIn(this Layout layout, bool fromRight = true, uint duration = 300, int delay = 75)
    {
        double startX = fromRight ? Application.Current.MainPage.Width : -Application.Current.MainPage.Width;

        foreach (VisualElement child in layout.Children.OfType<VisualElement>())
        {
            child.Opacity = 0;
            child.TranslationX = startX; //start outside
            child.IsVisible = true; //make sure it's visible

            await Task.WhenAll(
                child.TranslateTo(0, 0, duration, Easing.CubicOut),
                child.FadeIn(duration - 100)
            );

            await Task.Delay(delay); // Delay before the next child.
        }
    }
    //same usage as the previous one.

    public static async Task ExpandAndReveal(this VisualElement container, VisualElement content, double targetWidth, double targetHeight, uint duration = 350)
    {
        // Start with content hidden
        content.Opacity = 0;
        content.IsVisible = false; // Ensure it's initially hidden

        double initialWidth = container.Width;
        double initialHeight = container.Height;

        // Expand the container (use asymmetrical timing)
        Animation widthAnimation = new Animation(v => container.WidthRequest = v, initialWidth, targetWidth, Easing.CubicOut);
        widthAnimation.Commit(container, "WidthExpansion", length: (uint)(duration * 0.8));
        await Task.Delay(50);
        Animation heightAnimation = new Animation(v => container.HeightRequest = v, initialHeight, targetHeight, Easing.CubicOut);
        heightAnimation.Commit(container, "HeightExpansion", length: duration);
        await Task.Delay(100);

        // Fade in the content (delayed)
        content.IsVisible = true; //make visble first
        await content.FadeIn(200);
    }
    //XAML example
    // <Frame x:Name="ExpandableContainer" BackgroundColor="LightGray" WidthRequest="50" HeightRequest="50" CornerRadius="25">
    //     <Frame.GestureRecognizers>
    //         <TapGestureRecognizer Tapped="OnContainerTapped" />
    //     </Frame.GestureRecognizers>
    // </Frame>
    //<Label x:Name="HiddenContent" Text="Revealed Content!" TextColor="Black" />
    // C# example
    // private async void OnContainerTapped(object sender, TappedEventArgs e)
    // {
    //     await ExpandableContainer.ExpandAndReveal(HiddenContent, 200, 150);
    // }

    public static async Task ShrinkAndHide(this VisualElement container, VisualElement content, double targetWidth, double targetHeight, uint duration = 300)
    {
        // Fade out the content
        await content.FadeOut(150);
        content.IsVisible = false;
        double initialWidth = container.Width;
        double initialHeight = container.Height;

        // Shrink the container (use asymmetrical timing, height first)

        Animation heightAnimation = new Animation(v => container.HeightRequest = v, initialHeight, targetHeight, Easing.CubicIn);
        heightAnimation.Commit(container, "HeightContraction", length: duration);

        await Task.Delay(50);

        Animation widthAnimation = new Animation(v => container.WidthRequest = v, initialWidth, targetWidth, Easing.CubicIn);
        widthAnimation.Commit(container, "WidthContraction", length: (uint)(duration * 0.8));
    }
    //Usage, similar to the previous one

    public static async Task ProgressFill(this ProgressBar progressBar, double targetProgress, uint duration = 500)
    {
        //Pulse the progressBar
        await progressBar.Pulse();

        //fill it
        await progressBar.ProgressTo(targetProgress, duration, Easing.CubicInOut);
    }

    // <ProgressBar x:Name="MyProgressBar" Progress="0" />
    //await MyProgressBar.ProgressFill(0.8); // Fill to 80%

    public static async Task ShowNotificationBanner(this Label notificationLabel, string message, uint duration = 400)
    {
        notificationLabel.Text = message;
        notificationLabel.TranslationY = -notificationLabel.Height; // Start above the container
        notificationLabel.Opacity = 0;
        notificationLabel.IsVisible = true; //make visible

        // Slide down and fade in.
        await Task.WhenAll(
            notificationLabel.TranslateTo(0, 0, duration, Easing.CubicOut),
            notificationLabel.FadeIn(duration - 100)
        );

        // Wait for a few seconds (optional).
        await Task.Delay(3000);

        // Slide up and fade out.
        await Task.WhenAll(
            notificationLabel.TranslateTo(0, -notificationLabel.Height, duration, Easing.CubicIn),
            notificationLabel.FadeOut(duration - 100)
        );
    }
    //XAML
    // <Grid x:Name="MainContainer">
    //    <!-- Main app content -->
    //     <Label x:Name="NotificationLabel" BackgroundColor="LightGreen" TextColor="Black" HorizontalTextAlignment="Center" />
    // </Grid>
    //C#
    // await MainContainer.ShowNotificationBanner(NotificationLabel, "New message received!");

    public static async Task AnimateCounter(this Label counterLabel, int startValue, int endValue, uint duration = 500)
    {
        // Use a custom animation to update the label's text.
        Animation animation = new Animation(v => counterLabel.Text = ((int)v).ToString(), startValue, endValue);
        animation.Commit(counterLabel, "CounterAnimation", length: duration, easing: Easing.CubicInOut);

        // Pulse the label for emphasis.
        await counterLabel.Pulse();
    }

    // <Label x:Name="CounterLabel" Text="0" />
    // <Button Text="Increment" Clicked="OnIncrementClicked" />
    //
    // private async void OnIncrementClicked(object sender, EventArgs e)
    // {
    //     await CounterLabel.AnimateCounter(int.Parse(CounterLabel.Text), int.Parse(CounterLabel.Text) + 10);
    // }

    public static async Task CarouselTransition(this Image currentImage, Image nextImage, bool slideRight, uint duration = 400)
    {
        // Set initial positions.
        nextImage.TranslationX = slideRight ? Application.Current.MainPage.Width : -Application.Current.MainPage.Width;
        nextImage.Opacity = 0;
        nextImage.IsVisible = true; //make next image visible

        // Slide both images simultaneously.
        await Task.WhenAll(
            currentImage.TranslateTo(slideRight ? -Application.Current.MainPage.Width : Application.Current.MainPage.Width, 0, duration, Easing.CubicInOut),
            currentImage.FadeOut(duration),
            nextImage.TranslateTo(0, 0, duration, Easing.CubicInOut),
            nextImage.FadeIn(duration)
        );
        currentImage.IsVisible = false; //hide the prev image at last.

    }
    //XAML
    // <Grid>
    //     <Image x:Name="Image1" Source="image1.png" Aspect="AspectFill" />
    //     <Image x:Name="Image2" Source="image2.png" Aspect="AspectFill" IsVisible="False" />
    //  <Grid.GestureRecognizers>
    // <SwipeGestureRecognizer Direction="Left" Swiped="OnSwipedLeft" />
    //<SwipeGestureRecognizer Direction="Right" Swiped="OnSwipedRight" />
    // </Grid.GestureRecognizers>
    // </Grid>
    //C#
    // private async void OnSwipedLeft(object sender, SwipedEventArgs e)
    // {
    //     // Assuming you have a mechanism to switch images (e.g., a list of image sources).
    //     await Image1.CarouselTransition(Image2, true);
    // }

    public static async Task ValidateFormField(this Entry entry, Label errorLabel, bool isValid, string errorMessage = "")
    {
        if (!isValid)
        {
            errorLabel.Text = errorMessage;
            await Task.WhenAll(
                errorLabel.FadeIn(),
                entry.Shake() // Use the Shake animation from the previous set
            );
        }
        else
        {
            await errorLabel.FadeOut();
        }
    }
    //XAML
    // <Entry x:Name="MyEntry" Placeholder="Enter text" />
    // <Label x:Name="ErrorLabel" TextColor="Red" IsVisible="False" />
    // <Button Text="Validate" Clicked="OnValidateClicked" />
    //C#
    // private async void OnValidateClicked(object sender, EventArgs e)
    // {
    //    bool isValid = !string.IsNullOrWhiteSpace(MyEntry.Text);
    //    await MyEntry.ValidateFormField(ErrorLabel, isValid, "Please enter text.");
    // }

    public static async Task TabSwitchTransition(this VisualElement currentContent, VisualElement nextContent, bool slideRight, uint duration = 300)
    {
        // Set initial positions.
        nextContent.TranslationX = slideRight ? Application.Current.MainPage.Width : -Application.Current.MainPage.Width;
        nextContent.Opacity = 0;
        nextContent.IsVisible = true;

        // Slide both views simultaneously.
        await Task.WhenAll(
            currentContent.TranslateTo(slideRight ? -Application.Current.MainPage.Width : Application.Current.MainPage.Width, 0, duration, Easing.CubicInOut),
            currentContent.FadeOut(duration),
            nextContent.TranslateTo(0, 0, duration, Easing.CubicInOut),
            nextContent.FadeIn(duration)
        );

        currentContent.IsVisible = false; // Keep only next content visible
    }
    //XAML
    // <Grid>
    //     <StackLayout x:Name="Content1">
    //content
    //     </StackLayout>
    //     <StackLayout x:Name="Content2" IsVisible="False">
    //      //Content
    //     </StackLayout>
    //    <HorizontalStackLayout>
    //     <Button Text="Tab 1" Clicked="OnTab1Clicked" />
    //     <Button Text="Tab 2" Clicked="OnTab2Clicked" />
    //     </HorizontalStackLayout>
    // </Grid>
    //C#
    // private async void OnTab1Clicked(object sender, EventArgs e)
    // {
    //     await Content2.TabSwitchTransition(Content1, false); // Slide left
    // }

    public static async Task LiquidFill(this BoxView box, double targetHeight, Color fillColor, uint duration = 800)
    {
        // Start with the BoxView at the bottom and zero height.
        box.BackgroundColor = fillColor;
        box.HeightRequest = 0;  //initially invisible
        box.VerticalOptions = LayoutOptions.End;
        box.IsVisible = true;

        // Animate the HeightRequest to simulate filling.
        Animation animation = new Animation(v => box.HeightRequest = v, 0, targetHeight);

        // Use a custom easing function to simulate liquid motion (slightly "wobbly").
        animation.Commit(box, "LiquidFillAnimation", length: duration, easing: new Easing(t => {
            // A modified sine wave for a liquid-like wobble.
            return Math.Sin(t * Math.PI) * 0.1 * Math.Exp(-t * 5) + t;
        }));
        await Task.Delay((int)duration); //wait for the anim to finish.
    }
    //<BoxView x:Name="LiquidBox" />
    //await LiquidBox.LiquidFill(200, Colors.Blue);

    public static async Task PulsatingGradient(this BoxView box, Color color1, Color color2, uint duration = 1000)
    {
        //we'll use linear gradient, we can set it to radial.
        LinearGradientBrush gradientBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1), // Diagonal gradient
            GradientStops =
        [
            new GradientStop { Color = color1, Offset = 0 },
            new GradientStop { Color = color2, Offset = 1 }
        ]
        };

        box.Background = gradientBrush;
        box.IsVisible = true;

        // Animate the Offset of one of the GradientStops to create the pulsation.
        double startOffset = 0;
        double endOffset = 0.3;

        Animation animation = new Animation(v => {
            gradientBrush.GradientStops[1].Offset = (float)v;
        }, startOffset, endOffset);
        Animation animation2 = new Animation(v => {
            gradientBrush.GradientStops[1].Offset = (float)v;
        }, endOffset, startOffset);

        animation.Commit(box, "PulsatingGradientAnimation", length: duration / 2, repeat: () =>
        {
            //run anim2 when anim finishes
            animation2.Commit(box, "PulsatingGradientAnimation", length: duration / 2, repeat: () => true);
            return false; // Stop repeating anim, so we don't have it repeated forever.
        });
        await Task.Delay(-1);
    }

    //<BoxView x:Name="GradientBox" WidthRequest="200" HeightRequest="200" />
    //await GradientBox.PulsatingGradient(Colors.Red, Colors.Yellow);

    public static async Task CircularReveal(this BoxView box, bool reveal, uint duration = 500)
    {
        // Use clipping with an EllipseGeometry.
        EllipseGeometry ellipseGeometry = new EllipseGeometry();
        box.Clip = ellipseGeometry;

        // Initial state:  Either fully revealed or fully hidden.
        if (reveal)
        {
            // Start with a tiny circle in the center.
            ellipseGeometry.Center = new Point(box.Width / 2, box.Height / 2);
            ellipseGeometry.RadiusX = 0;
            ellipseGeometry.RadiusY = 0;

        }
        else
        {
            // Start with a circle covering the entire BoxView.
            ellipseGeometry.Center = new Point(box.Width / 2, box.Height / 2);
            ellipseGeometry.RadiusX = Math.Max(box.Width, box.Height); //ensure the circle is large enough to cover.
            ellipseGeometry.RadiusY = Math.Max(box.Width, box.Height);
        }

        // Animate the RadiusX and RadiusY properties.
        double targetRadius = reveal ? Math.Max(box.Width, box.Height) : 0; //final value will depend on whether we're revealing or hiding.
        Animation animation = new Animation(v =>
        {
            ellipseGeometry.RadiusX = v;
            ellipseGeometry.RadiusY = v;
        }, reveal ? 0 : targetRadius, targetRadius); //start and end points of anim depends on "reveal".

        animation.Commit(box, "CircularRevealAnimation", length: duration, easing: Easing.CubicInOut);
        await Task.Delay((int)duration); //wait for anim
    }

    //<BoxView x:Name="CircleBox" BackgroundColor="Orange" WidthRequest="200" HeightRequest="200" />
    //await CircleBox.CircularReveal(true); // Reveal

    public static async Task AnimateDashedBorder(this BoxView box, double dashLength = 10, double gapLength = 5, uint duration = 1000)
    {
        // We'll simulate a dashed border by animating the Background color with a repeating linear gradient.
        // This is a clever trick, as MAUI doesn't have native dashed borders on BoxView.

        box.IsVisible = true;
        LinearGradientBrush gradientBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0.5), // Horizontal line
            EndPoint = new Point(1, 0.5),
            GradientStops =
        [
            new GradientStop { Color = box.BackgroundColor, Offset = 0 }, // Dash color
            new GradientStop { Color = box.BackgroundColor, Offset = (float)(dashLength / (dashLength + gapLength)) }, // End of dash
            new GradientStop { Color = Colors.Transparent, Offset = (float)(dashLength / (dashLength + gapLength)) },   // Start of gap
            new GradientStop { Color = Colors.Transparent, Offset = 1 }  // End of gap
        ]
        };

        box.Background = gradientBrush; //initially set the gradient

        // Animate the StartPoint and EndPoint to move the dashes.
        Animation animation = new Animation(v =>
        {
            gradientBrush.StartPoint = new Point(-v, 0.5);
            gradientBrush.EndPoint = new Point(1 - v, 0.5);
        }, 0, 1);

        animation.Commit(box, "DashedBorderAnimation", length: duration, repeat: () => true); // Repeat indefinitely
        await Task.Delay(-1);
    }

    //<BoxView x:Name="DashedBox" BackgroundColor="Blue" WidthRequest="200" HeightRequest="50" />
    //await DashedBox.AnimateDashedBorder();

    public static async Task ConnectedSlide(this VisualElement view1, VisualElement view2, bool slideRight, uint duration = 300)
    {
        double distance = Application.Current.MainPage.Width; //slide distance
        double endX1 = slideRight ? -distance : distance; //endX for View1
        double startX2 = slideRight ? distance : -distance; //start X for view2
        double endX2 = 0; //endX for view 2

        // Make sure View2 is visible, but initially off-screen.
        view2.TranslationX = startX2;
        view2.Opacity = 0;
        view2.IsVisible = true;

        // Slide both views in opposite directions.
        await Task.WhenAll(
            view1.TranslateTo(endX1, 0, duration, Easing.CubicInOut),
            view1.FadeOut(duration),
            view2.TranslateTo(endX2, 0, duration, Easing.CubicInOut),
            view2.FadeIn(duration)
        );
        view1.IsVisible = false;
    }
    //<Grid>
    //  <Label x:Name="Label1" Text="View 1" BackgroundColor="LightBlue" />
    //  <Label x:Name="Label2" Text="View 2" BackgroundColor="LightGreen" IsVisible="False" />
    //   <Grid.GestureRecognizers>
    //    <TapGestureRecognizer Tapped="OnConnectedSlideTapped" />
    //  </Grid.GestureRecognizers>
    //</Grid>

    // private async void OnConnectedSlideTapped(object sender, TappedEventArgs e)
    // {
    //     await Label1.ConnectedSlide(Label2, true); // Slide right
    // }

    public static async Task DepthZoom(this Image background, Image foreground, Point tapPoint, uint duration = 500)
    {
        // 1. Position the foreground image at the tap point.
        foreground.TranslationX = tapPoint.X - foreground.Width / 2;
        foreground.TranslationY = tapPoint.Y - foreground.Height / 2;

        //2. Make sure both views are visible
        background.IsVisible = true;
        foreground.IsVisible = true;

        // 2. Initial state: background is full size, foreground is small and transparent.
        background.Scale = 1;
        foreground.Scale = 0;
        foreground.Opacity = 0;


        // 3. Animate: background scales *down*, foreground scales *up* and fades in.
        await Task.WhenAll(
            background.ScaleTo(0.8, duration, Easing.CubicInOut), // Subtle zoom out
            foreground.ScaleTo(1, duration, Easing.CubicOut),    // Zoom in with overshoot
            foreground.FadeIn(duration)
        );
    }
    // <Grid>
    //     <Image x:Name="BackgroundImage" Source="background.jpg" Aspect="AspectFill" />
    //     <Image x:Name="ForegroundImage" Source="foreground.png" Aspect="AspectFill" WidthRequest="100" HeightRequest="100" IsVisible="False"/>
    //     <Grid.GestureRecognizers>
    //        <TapGestureRecognizer Tapped="OnBackgroundImageTapped" />
    //     </Grid.GestureRecognizers>
    // </Grid>

    // private async void OnBackgroundImageTapped(object sender, TappedEventArgs e)
    // {
    //     Point tapPoint = e.GetPosition(BackgroundImage).Value;
    //     await BackgroundImage.DepthZoom(ForegroundImage, tapPoint);
    // }

    public static async Task SplitReveal(this VisualElement view1, VisualElement view2, bool reveal, uint duration = 400)
    {
        // Make view2 visible.
        view2.IsVisible = true;

        // Initial states
        if (reveal)
        {
            // Start with view1 split and view2 hidden
            view1.TranslationX = 0;
            view2.Opacity = 0;

        }
        else
        {
            // Start with view1 together and view2 visible.
            view1.TranslationX = -view1.Width / 2;
            view2.Opacity = 1;
        }

        // Animate
        if (reveal)
        {
            // Split view1 apart and fade in view2
            await Task.WhenAll(
                view1.TranslateTo(-Shell.Current.Window.Width / 2, 0, duration, Easing.CubicInOut),
                view2.FadeIn(duration)
            );
        }
        else
        {
            // Bring view1 together and fade out view2
            await Task.WhenAll(
                view1.TranslateTo(0, 0, duration, Easing.CubicInOut),
                view2.FadeOut(duration)
            );
        }
    }
    // <Grid>
    //     <HorizontalStackLayout x:Name="SplitView" Spacing = "0">
    //         <BoxView BackgroundColor="Red" WidthRequest="100" HeightRequest="200" />
    //         <BoxView BackgroundColor="Blue" WidthRequest="100" HeightRequest="200" />
    //     </HorizontalStackLayout>
    //     <Label x:Name="ContentLabel" Text="Revealed Content" HorizontalOptions="Center" VerticalOptions="Center" />
    //     <Grid.GestureRecognizers>
    //        <TapGestureRecognizer Tapped="OnSplitViewTapped"/>
    //     </Grid.GestureRecognizers>
    // </Grid>

    // private bool _isRevealed = false;
    // private async void OnSplitViewTapped(object sender, TappedEventArgs e)
    // {
    //     await SplitView.SplitReveal(ContentLabel, _isRevealed);
    //     _isRevealed = !_isRevealed;
    // }

    public static async Task PageTurn(this Image currentPage, Image nextPage, uint duration = 600)
    {
        // Setup initial state
        nextPage.RotationY = -90; // Start rotated as if on the "back" of a page
        nextPage.Opacity = 0;
        nextPage.IsVisible = true;
        // Bring the current page forward and the next page backward
        currentPage.ZIndex = 1;
        nextPage.ZIndex = 0;

        // Create a combined animation
        await Task.WhenAll(
            currentPage.RotateYTo(90, duration, Easing.CubicInOut),
            currentPage.FadeOut(duration),
            nextPage.RotateYTo(0, duration, Easing.CubicInOut),
            nextPage.FadeIn(duration)

        );

        currentPage.IsVisible = false;
        currentPage.RotationY = 0; //reset previous image
        currentPage.ZIndex = 0;
        nextPage.ZIndex = 1;
    }
    //<Grid>
    // <Image x:Name="Page1" Source="page1.png" Aspect="AspectFill" />
    // <Image x:Name="Page2" Source="page2.png" Aspect="AspectFill" IsVisible="False" />
    //  <Grid.GestureRecognizers>
    //    <TapGestureRecognizer Tapped="OnPageTurnTapped" />
    // </Grid.GestureRecognizers>
    //</Grid>

    //private async void OnPageTurnTapped(object sender, TappedEventArgs e)
    //{
    //  await Page1.PageTurn(Page2);
    //}

    public static async Task StaggeredRevealWithMask(this Image image, BoxView mask, Layout contentLayout, uint duration = 600)
    {
        // 1. Setup:  Mask covers the image, content is hidden.
        mask.WidthRequest = image.Width;
        mask.HeightRequest = image.Height; //same size!
        mask.BackgroundColor = Colors.Black; // Or any color that contrasts with the image
        mask.IsVisible = true;
        mask.Opacity = 1; //fully visible

        //make content layout invisible at first.
        foreach (VisualElement child in contentLayout.Children)
        {
            child.Opacity = 0; // Start invisible
            child.IsVisible = false;
        }

        // 2. Animate the mask to reveal the image.
        //  Could be a simple slide, a circular reveal, or any other reveal animation.
        //  Here, we use a simple slide to the right.
        await mask.TranslateTo(image.Width, 0, duration, Easing.CubicInOut);
        mask.IsVisible = false; //hid it

        // 3. Staggered fade-in of the content elements.
        await contentLayout.StaggeredFadeIn(duration: 300, delay: 50); // Reuse existing method
    }
    //<Grid>
    //   <Image x:Name="BackgroundImage" Source="image.jpg" Aspect="AspectFill" />
    //    <BoxView x:Name="ImageMask" BackgroundColor="Black" />
    //   <StackLayout x:Name="ContentLayout" Padding="20">
    //      <Label Text="Title" TextColor="White" FontSize="24" />
    //    <Label Text="Subtitle" TextColor="White" FontSize="16" />
    //     <Button Text="Learn More" BackgroundColor="Blue" TextColor="White" />
    //  </StackLayout>
    //    <Grid.GestureRecognizers>
    //        <TapGestureRecognizer Tapped="OnRevealTapped" />
    //    </Grid.GestureRecognizers>
    //</Grid>

    // private async void OnRevealTapped(object sender, TappedEventArgs e)
    // {
    //     await BackgroundImage.StaggeredRevealWithMask(ImageMask, ContentLayout);
    // }
    public static async Task ExplodeView(this Layout container, uint duration = 500)
    {
        // This assumes the container has multiple direct child elements.
        Random random = new Random();

        foreach (VisualElement child in container.Children.OfType<VisualElement>())
        {
            // Generate random translation distances and rotation angles.
            double translateX = random.Next(-200, 200); // Adjust range as needed
            double translateY = random.Next(-200, 200);
            double rotation = random.Next(-180, 180);

            // Animate each child independently.
            await Task.WhenAll(
               child.TranslateTo(translateX, translateY, duration, Easing.CubicOut),
               child.RotateTo(rotation, duration, Easing.CubicOut),
               child.FadeOut(duration)
           );
            child.IsVisible = false;
        }
    }

    public static void RunFocusModeAnimation(this Border avatarView, Color strokeColor)
    {
        if (avatarView == null)
            return;

        // Set the stroke color based on pause/resume state
        avatarView.Stroke = strokeColor;

        // Define a single animation to embiggen the stroke
        Animation expandAnimation = new Animation(v => avatarView.StrokeThickness = v, // Only animating StrokeThickness now
            0,                                   // Start with 0 thickness
            5,                                  // Expand to 10 thickness
            Easing.CubicInOut                    // Smooth easing
        );

        // Shrink the stroke back to zero after embiggen
        Animation shrinkAnimation = new Animation(
            v => avatarView.StrokeThickness = v,
            5,                                   // Start at 10 thickness
            0,                                    // Reduce to 0 thickness
            Easing.CubicInOut
        );

        // Combine expand and shrink animations into one sequence
        Animation animationSequence = new Animation
        {
            { 0, 0.5, expandAnimation },   // Embiggen in the first half
            { 0.5, 1, shrinkAnimation }    // Shrink back in the second half
        };

        // Run the full animation sequence
        animationSequence.Commit(avatarView, "FocusModeAnimation", length: 500, easing: Easing.Linear);
    }

    // <StackLayout x:Name="ExplodingContainer" Padding="20">
    //     <BoxView BackgroundColor="Red" WidthRequest="50" HeightRequest="50" />
    //     <BoxView BackgroundColor="Green" WidthRequest="50" HeightRequest="50" />
    //     <BoxView BackgroundColor="Blue" WidthRequest="50" HeightRequest="50" />
    //     <StackLayout.GestureRecognizers>
    //      <TapGestureRecognizer Tapped="OnExplodeTapped" />
    //     </StackLayout.GestureRecognizers>
    // </StackLayout>

    // private async void OnExplodeTapped(object sender, TappedEventArgs e)
    // {
    //   await ExplodingContainer.ExplodeView();
    // }


    //XAML
    // <Grid>
    //     <Grid x:Name="Toolbar" BackgroundColor="LightBlue" HeightRequest="50">
    //         <Label Text="Toolbar" HorizontalOptions="Center" VerticalOptions="Center" />
    //     </Grid>
    //      <ScrollView x:Name="MainScrollView" Scrolled="OnScrolled">
    //         <StackLayout x:Name="ContentArea" Padding="0,50,0,0">
    //A LOT of content here
    //          </StackLayout>
    //      </ScrollView>
    // </Grid>

    //C#
    // private async void OnScrolled(object sender, ScrolledEventArgs e)
    // {
    //     await Toolbar.CollapseToolbar(MainScrollView, ContentArea, Toolbar.Height, e);
    // }

    // --- Existing Animations --- (Keep all your previous animations here)

    // --- 10 NEW BASIC ANIMATIONS ---

    // 1. Blink (Rapid Opacity Change)
    public static async Task Blink(this VisualElement element, uint duration = 100, int blinks = 3)
    {
        for (int i = 0; i < blinks; i++)
        {
            await element.FadeTo(0, duration, Easing.Linear);
            await element.FadeTo(1, duration, Easing.Linear);
        }
    }
    // Usage: await MyLabel.Blink();

    // 3. ColorShift (Smooth transition between two colors)
    public static async Task ColorShift(this VisualElement element, Color fromColor, Color toColor, uint duration = 400)
    {
        await element.AnimateAsync("ColorShiftAnimation", (progress) =>
        {
            element.BackgroundColor = Color.FromRgba(
                fromColor.Red + (toColor.Red - fromColor.Red) * progress,
                fromColor.Green + (toColor.Green - fromColor.Green) * progress,
                fromColor.Blue + (toColor.Blue - fromColor.Blue) * progress,
                fromColor.Alpha + (toColor.Alpha - fromColor.Alpha) * progress
            );
        }, length: duration);
    }
    // Usage:  await MyBoxView.ColorShift(Colors.Red, Colors.Blue);

    // 4.  ScaleX (Horizontal Scaling)
    public static async Task ScaleX(this VisualElement element, double targetScaleX, uint duration = 250)
    {
        await element.ScaleXTo(targetScaleX, duration, Easing.CubicInOut);
    }
    // Usage: await MyImage.ScaleX(0.5); // Shrink horizontally to half its size

    // 5. ScaleY (Vertical Scaling)
    public static async Task ScaleY(this VisualElement element, double targetScaleY, uint duration = 250)
    {
        await element.ScaleYTo(targetScaleY, duration, Easing.CubicInOut);
    }
    // Usage: await MyButton.ScaleY(1.5); // Enlarge vertically by 50%

    // 6. TranslateX (Horizontal Movement)
    public static async Task TranslateX(this VisualElement element, double targetX, uint duration = 250)
    {
        await element.TranslateTo(targetX, element.TranslationY, duration, Easing.CubicInOut);
    }
    //Usage: await myView.TranslateX(250);

    // 7. TranslateY (Vertical Movement)
    public static async Task TranslateY(this VisualElement element, double targetY, uint duration = 250)
    {
        await element.TranslateTo(element.TranslationX, targetY, duration, Easing.CubicInOut);
    }
    //Usage: await myView.TranslateY(250);


    // 8.  FadeAndTranslate (Combine Fade and Translation)
    public static async Task FadeAndTranslate(this VisualElement element, double targetX, double targetY, uint duration = 300)
    {
        await Task.WhenAll(
            element.FadeTo(0.5, duration / 2, Easing.Linear), // Fade out halfway
            element.TranslateTo(targetX, targetY, duration, Easing.CubicInOut)
        );
        await element.FadeTo(1, duration / 2, Easing.Linear);    //fade in to complete.
    }
    // Usage: await MyLabel.FadeAndTranslate(50, -30);


    // 9. OpacityPulse (Continuous Opacity Fluctuation)
    public static async Task OpacityPulse(this VisualElement element, double minOpacity = 0.5, uint duration = 500)
    {
        Animation fadeInAnim = new Animation(v => element.Opacity = v, minOpacity, 1, Easing.SinInOut);
        Animation fadeOutAnim = new Animation(v => element.Opacity = v, 1, minOpacity, Easing.SinInOut);

        fadeInAnim.Commit(element, "OpacityPulseIn", length: duration / 2, repeat: () =>
        {
            fadeOutAnim.Commit(element, "OpacityPulseOut", length: duration / 2, repeat: () => true); // Repeat indefinitely
            return false; //end first animation
        });
        await Task.Delay(-1); //run forever.

    }
    // Usage: MyButton.OpacityPulse();

    // 10. BorderColorChange (Smoothly changes border color)
    public static async Task BorderColorChange(this Border border, Color fromColor, Color toColor, uint duration = 400)
    {
        await border.AnimateAsync("BorderColorChange", (progress) =>
        {
            border.Stroke = Color.FromRgba(
                fromColor.Red + (toColor.Red - fromColor.Red) * progress,
                fromColor.Green + (toColor.Green - fromColor.Green) * progress,
                fromColor.Blue + (toColor.Blue - fromColor.Blue) * progress,
                fromColor.Alpha + (toColor.Alpha - fromColor.Alpha) * progress
            );
        }, length: duration);
    }
    // Usage (XAML): <Border x:Name="MyBorder" Stroke="Red" StrokeThickness="2" />
    //        await MyBorder.BorderColorChange(Colors.Red, Colors.Blue);

    // --- 10 NEW CURVED ANIMATIONS ---

    // 11.  Jiggle (Small Random Rotations)
    public static async Task Jiggle(this VisualElement element, uint duration = 200, int jiggles = 3, double angle = 10)
    {
        Random random = new Random();
        for (int i = 0; i < jiggles; i++)
        {
            double rotation = (random.NextDouble() * 2 - 1) * angle; // Random angle between -angle and +angle
            await element.RotateTo(rotation, duration / (uint)jiggles, Easing.CubicInOut);
        }
        await element.RotateTo(0, duration / (uint)jiggles, Easing.CubicInOut); // Return to original rotation
    }
    // Usage: await MyImage.Jiggle();

    // 12. ParabolicJump (Jump along a Parabolic Path)
    public static async Task ParabolicJump(this VisualElement element, double height, double distance, uint duration = 500)
    {
        // Simulate a parabolic path using combined TranslationX and TranslationY with different easings.

        // Upward and forward motion
        Animation jumpUp = new Animation(v =>
        {
            element.TranslationY = -v; // Move up
            element.TranslationX = distance * (v / height); // Move forward proportionally
        }, 0, height, Easing.CubicOut);  // Easing.CubicOut for the upward motion

        // Downward and forward motion
        Animation jumpDown = new Animation(v =>
        {
            element.TranslationY = -height + v; // Move from peak down to original Y
            element.TranslationX = distance * ((height + v) / (2 * height));   // Continue moving forward
        }, 0, height, Easing.CubicIn);   // Easing.CubicIn for the downward motion


        jumpUp.Commit(element, "JumpUp", length: duration / 2, finished: (v, c) =>
        {
            jumpDown.Commit(element, "JumpDown", length: duration / 2); // Start the downward motion when the upward is finished.
        });
        await Task.Delay((int)duration); //wait until jump anim is done.

    }
    // Usage: await MyButton.ParabolicJump(height: 100, distance: 50);


    // 13.  SwirlIn (Scale and Rotate Simultaneously)
    public static async Task SwirlIn(this VisualElement element, uint duration = 400, double rotations = 2)
    {
        element.Rotation = 360 * rotations; // Start rotated
        element.Scale = 0;                 // Start small
        element.Opacity = 0;
        element.IsVisible = true;
        await Task.WhenAll(
            element.RotateTo(0, duration, Easing.CubicOut),
            element.ScaleTo(1, duration, Easing.CubicOut),
            element.FadeIn(duration)
        );
    }
    // Usage: await MyFrame.SwirlIn();

    // 14. SwirlOut (Opposite of SwirlIn)
    public static async Task SwirlOut(this VisualElement element, uint duration = 400, double rotations = 2)
    {
        await Task.WhenAll(
          element.RotateTo(360 * rotations, duration, Easing.CubicIn),
          element.ScaleTo(0, duration, Easing.CubicIn),
          element.FadeOut(duration)
        );
        element.IsVisible = false;
    }
    // Usage: await myView.SwirlOut()

    // 15. SquashAndStretch (Simulate Elasticity)
    public static async Task SquashAndStretch(this VisualElement element, double squashFactor = 0.7, uint duration = 200)
    {
        await Task.WhenAll( // Initial squash
            element.ScaleXTo(1 + (1 - squashFactor), duration / 2, Easing.CubicOut),
            element.ScaleYTo(squashFactor, duration / 2, Easing.CubicOut)
        );
        await Task.WhenAll( // Stretch back
             element.ScaleXTo(squashFactor, duration / 2, Easing.CubicIn),
             element.ScaleYTo(1 + (1 - squashFactor), duration / 2, Easing.CubicIn)
        );
        await Task.WhenAll(
            element.ScaleXTo(1, duration / 4, Easing.CubicOut),
            element.ScaleYTo(1, duration / 4, Easing.CubicOut)
        );
    }
    // Usage: await MyButton.SquashAndStretch();

    // 16.  ElasticSnap (Like Pulling and Releasing)
    public static async Task ElasticSnap(this VisualElement element, double displacementX, double displacementY, uint duration = 300)
    {
        // Move to the displaced position
        await element.TranslateTo(displacementX, displacementY, duration / 2, Easing.CubicOut);
        // Snap back with overshoot
        await element.TranslateTo(0, 0, duration / 2, Easing.SpringOut);
    }
    // Usage: await MyImage.ElasticSnap(-50, 20); // Pull left and up, then snap back

    // 17. Wobble (Continuous Tilting)
    public static async Task Wobble(this VisualElement element, double angle = 15, uint duration = 500)
    {
        Animation wobbleRight = new Animation(v => element.Rotation = v, 0, angle, Easing.SinInOut); //go to +angle
        Animation wobbleLeft = new Animation(v => element.Rotation = v, angle, -angle, Easing.SinInOut); //go to -angle
        Animation wobbleCenter = new Animation(v => element.Rotation = v, -angle, 0, Easing.SinInOut);  //return to center

        wobbleRight.Commit(element, "WobbleRight", length: duration / 4, finished: (_, _) =>
        {
            wobbleLeft.Commit(element, "WobbleLeft", length: duration / 2, finished: (_, _) =>
            {
                wobbleCenter.Commit(element, "WobbleCenter", length: duration / 4, repeat: () => true);
            });

        });

        await Task.Delay(-1);  // Infinite animation
    }
    // Usage:  MyLabel.Wobble();

    // 18.  DelayedReveal (Fade In with a Delay)
    public static async Task DelayedReveal(this VisualElement element, uint delay = 500, uint duration = 300)
    {
        element.Opacity = 0;
        element.IsVisible = true;
        await Task.Delay((int)delay);
        await element.FadeIn(duration);
    }
    // Usage: await MyBoxView.DelayedReveal(delay: 1000); // Wait 1 second, then fade in

    public static async Task MorphShapes(this BoxView boxView, bool toCircle, uint duration = 400)
    {
        double startRadius, endRadius;

        if (toCircle)
        {
            startRadius = 0;  // Square
            endRadius = Math.Max(boxView.Width, boxView.Height) / 2; // Circle

        }
        else
        {
            startRadius = Math.Max(boxView.Width, boxView.Height) / 2; // Circle
            endRadius = 0;   // Square
        }
        //Commit the animation
        await boxView.AnimateAsync("MorphShapes", (progress) =>
        {
            boxView.CornerRadius = new CornerRadius(startRadius + (endRadius - startRadius) * progress);
        }, length: duration, easing: Easing.CubicInOut);

    }
    // Usage (XAML): <BoxView x:Name="MorphingBox" BackgroundColor="Blue" WidthRequest="100" HeightRequest="100" />
    //        await MorphingBox.MorphShapes(true);  // Morph to circle
    //       await MorphingBox.MorphShapes(false); // Morph to square

    public static async Task MyBackgroundColorTo(this VisualElement element, Color targetColor, uint length = 250)
    {
        if (element.BackgroundColor == null)
            return;

        Color startColor = element.BackgroundColor;
        uint startTime = (uint)Environment.TickCount;

        while (Environment.TickCount - startTime < length)
        {
            double progress = (double)(Environment.TickCount - startTime) / length;
            progress = Math.Min(1, progress); // Clamp to 1

            Color currentColor = Color.FromRgba(
                startColor.Red + (targetColor.Red - startColor.Red) * progress,
                startColor.Green + (targetColor.Green - startColor.Green) * progress,
                startColor.Blue + (targetColor.Blue - startColor.Blue) * progress,
                startColor.Alpha + (targetColor.Alpha - startColor.Alpha) * progress
            );

            // Update the UI on the main thread, but do NOT wait for it!
            MainThread.BeginInvokeOnMainThread(() =>
            {
                element.BackgroundColor = currentColor;
            });

            // Yield control to allow the UI thread to process events.
            await Task.Yield(); // VERY IMPORTANT!
        }

        // Ensure the final color is set.
        MainThread.BeginInvokeOnMainThread(() =>
        {
            element.BackgroundColor = targetColor;
        });
    }


}