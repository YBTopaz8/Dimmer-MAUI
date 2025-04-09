using Microsoft.Maui.Controls.Shapes;

namespace Dimmer.Utilities.CustomAnimations;

public static class CustomAnimsExtensions
{
    
    public static async Task AnimateHighlightPointerPressed(this View element)
    {
        await element.ScaleTo(0.95, 80, Easing.CubicIn);
    }
    public static async Task AnimateHighlightPointerReleased(this View element)
    {
        await element.ScaleTo(1.0, 80, Easing.CubicOut);
    }

    public static async Task DimmOut(this View element, double duration = 350, double endOpacity = 0.70)
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
            
            await element.TranslateTo(0, bounceHeight, duration / 2, Easing.CubicIn);

            
            await element.TranslateTo(0, 0, duration / 2, Easing.CubicOut);

            
            bounceHeight *= 0.5; 
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
        
        await Task.WhenAll(
            element.ScaleTo(endScale, (uint)duration, Easing.CubicInOut),
            element.FadeTo(1.0, (uint)duration, Easing.CubicInOut)
        );
    }


    public static async Task AnimateFocusModePointerExited(this View element, double duration = 250, double endOpacity = 0.7, double endScale = 0.7)
    {
        
        await Task.WhenAll(
            element.ScaleTo(endScale, (uint)duration, Easing.CubicInOut),
            element.FadeTo(endOpacity, (uint)duration, Easing.CubicInOut)
        );
    }

    
    public static async Task AnimateFadeOutBack(this View element, uint duration = 250)
    {
        await Task.WhenAll(
            element.FadeTo(0, duration, Easing.CubicInOut), 
            element.TranslateTo(0, 50, duration, Easing.CubicInOut) 
        );
        element.IsVisible = false; 
    }

    
    public static async Task AnimateFadeInFront(this View element, uint duration = 250)
    {
        element.IsVisible = true; 
        element.Opacity = 0; 
        element.TranslationY = 50; 
        await Task.WhenAll(
            element.FadeTo(1, duration, Easing.CubicInOut), 
            element.TranslateTo(0, 0, duration, Easing.CubicInOut) 
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

    
    public static Task AnimateAsync(this IAnimatable element, string name, Action<double> callback, uint length, Easing easing = null)
    {
        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
        element.Animate(name, callback, 16, length, easing, (v, c) => tcs.SetResult(c), null);
        return tcs.Task;
    }


    

    
    public static async Task AnimateIslandPulse(this View island, double scale = 1.1, uint duration = 500)
    {
        
        await island.ScaleTo(scale, duration / 2, Easing.SinInOut);
        await island.ScaleTo(1.0, duration / 2, Easing.SinInOut);
    }

    
    public static async Task AnimateIslandExpand(this View island, IList<View> controls, uint duration = 300)
    {
        
        await island.ScaleTo(1.2, duration, Easing.CubicOut);

        
        foreach (View control in controls)
        {
            control.Opacity = 0; 
            control.IsVisible = true;

            
            
            
            

            await control.FadeTo(1.0, duration, Easing.CubicOut);
        }
    }

    
    public static async Task AnimateIslandCollapse(this View island, IList<View> controls, uint duration = 200)
    {
        
        foreach (View control in controls)
        {
            await control.FadeTo(0.0, duration, Easing.CubicIn);
            control.IsVisible = false;
        }

        
        await island.ScaleTo(1.0, duration, Easing.CubicIn);
    }

    
    public static async Task AnimateNewTrackBounce(this View island, double scale = 1.3, uint duration = 200)
    {
        
        await island.ScaleTo(scale, duration / 2, Easing.CubicOut);
        await island.ScaleTo(1.0, duration / 2, Easing.CubicIn);
    }
    
    public static async Task AnimateRotate(this View view, double rotation, uint duration = 250, Easing easing = null)
    {
        await view.RotateTo(rotation, duration, easing ?? Easing.Linear);
    }

    
    public static async Task AnimateRotateAndScale(this View view, double rotation, double scale, uint duration = 250, Easing easing = null)
    {
        await Task.WhenAll(
            view.RotateTo(rotation, duration, easing ?? Easing.Linear),
            view.ScaleTo(scale, duration, easing ?? Easing.Linear)
        );
    }
    
    public static async Task AnimateIslandColorChange(this View island, Color fromColor, Color toColor, uint duration = 400)
    {
        
        

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
        await view.TranslateTo(0, 0, duration, Easing.Linear); 
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
        element.IsVisible = true; 

        await Task.WhenAll(
            element.FadeIn(duration), 
            element.TranslateTo(0, 0, duration, Easing.CubicOut)
        );
    }

    public static async Task SlideOutToRight(this VisualElement element, uint duration = 250)
    {
        await Task.WhenAll(
            element.FadeOut(duration), 
            element.TranslateTo(Shell.Current.Width, 0, duration, Easing.CubicIn)
        );
        element.IsVisible = false;
    }

    public static async Task SlideInFromLeft(this VisualElement element, uint duration = 300)
    {
        element.Opacity = 0;
        element.TranslationX = -Shell.Current.Width;
        element.IsVisible = true; 
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


    public static async Task Expand(this VisualElement element, double targetWidth, double targetHeight, uint duration = 300)
    {
        
        await element.Pulse(1.05, 80);
        double initialWidth = element.Width;
        double initialHeight = element.Height;

        
        Animation widthAnimation = new Animation(v => element.WidthRequest = v, initialWidth, targetWidth, Easing.CubicOut);
        widthAnimation.Commit(element, "WidthExpansion", length: ((uint)(duration * 0.8))); 
        await Task.Delay(25);
        Animation heightAnimation = new Animation(v => element.HeightRequest = v, initialHeight, targetHeight, Easing.CubicOut);
        heightAnimation.Commit(element, "HeightExpansion", length: duration);
        await Task.Delay(50);

    }

    public static async Task Shrink(this VisualElement element, double targetWidth, double targetHeight, uint duration = 250)
    {

        double initialWidth = element.Width;
        double initialHeight = element.Height;

        
        Animation heightAnimation = new Animation(v => element.HeightRequest = v, initialHeight, targetHeight, Easing.CubicIn);
        heightAnimation.Commit(element, "HeightContraction", length: duration);
        await Task.Delay(50);

        Animation widthAnimation = new Animation(v => element.WidthRequest = v, initialWidth, targetWidth, Easing.CubicIn);
        widthAnimation.Commit(element, "WidthContraction", length: (uint)(duration * 0.8)); 
        await Task.Delay(25);

        
        await element.Pulse(1.03, 100);
    }

    public static async Task Rotate(this VisualElement element, double degrees, uint duration = 250)
    {
        await element.Pulse(); 
        await element.RotateTo(degrees, duration, Easing.CubicInOut); 
    }
    public static async Task Shake(this VisualElement element, uint duration = 300, double distance = 10)
    {
        
        uint shakeDuration = duration / 6;
        await element.TranslateTo(distance, 0, shakeDuration, Easing.Linear);
        await element.TranslateTo(-distance, 0, shakeDuration, Easing.Linear);
        await element.TranslateTo(distance, 0, shakeDuration, Easing.Linear);
        await element.TranslateTo(-distance, 0, shakeDuration, Easing.Linear);
        await element.TranslateTo(0, 0, shakeDuration, Easing.Linear); 
    }
    public static async Task BounceIn(this VisualElement element, uint duration = 400)
    {
        element.Scale = 0; 
        element.Opacity = 0;
        element.IsVisible = true;
        await Task.WhenAll(
            element.ScaleTo(1, duration, Easing.SpringOut), 
            element.FadeIn(duration - 100) 
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
        
        await element.FadeTo(0, duration / 2, Easing.Linear);
        await element.FadeTo(1, duration / 2, Easing.Linear);
    }

    
    public static async Task RippleExpand(this VisualElement trigger, BoxView overlay, Color rippleColor, uint duration = 400)
    {
        
        overlay.TranslationX = trigger.X + trigger.Width / 2 - overlay.Width / 2;
        overlay.TranslationY = trigger.Y + trigger.Height / 2 - overlay.Height / 2;
        overlay.BackgroundColor = rippleColor.WithAlpha(0.7f); 
        overlay.CornerRadius = (float)(Math.Max(trigger.Width, trigger.Height) * 2); 
        overlay.IsVisible = true;
        overlay.Scale = 0.1; 

        
        await Task.WhenAll(
            overlay.ScaleTo(2, duration, Easing.CubicOut), 
            overlay.FadeTo(0, duration, Easing.CubicOut)  
        );
        overlay.IsVisible = false;

        
        await trigger.Pulse();
    }

    
    
    
    
    

    
    


    public static async Task FlipCard(this VisualElement frontView, VisualElement backView, bool toBack, uint duration = 400)
    {
        
        backView.WidthRequest = frontView.Width;
        backView.HeightRequest = frontView.Height;
        backView.RotationY = toBack ? -180 : 0; //backview initial position
        frontView.RotationY = toBack ? 0 : 180;

        
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
    
    
    //content
    
    
    //content
    
    
    //<TapGestureRecognizer Tapped="OnCardTapped" />
    //</Grid.GestureRecognizers>
    //</Grid>

    
    
    
    
    
    
    

    public static async Task StaggeredFadeIn(this Layout layout, uint duration = 300, int delay = 50)
    {
        
        foreach (VisualElement child in layout.Children.OfType<VisualElement>())
        {
            child.Opacity = 0;
            child.IsVisible = true; 
            await child.FadeIn(duration);
            await Task.Delay(delay); 
        }
    }

    //example:
    
    
    
    
    

    

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

            await Task.Delay(delay); 
        }
    }
    //same usage as the previous one.

    public static async Task ExpandAndReveal(this VisualElement container, VisualElement content, double targetWidth, double targetHeight, uint duration = 350)
    {
        
        content.Opacity = 0;
        content.IsVisible = false; 

        double initialWidth = container.Width;
        double initialHeight = container.Height;

        
        Animation widthAnimation = new Animation(v => container.WidthRequest = v, initialWidth, targetWidth, Easing.CubicOut);
        widthAnimation.Commit(container, "WidthExpansion", length: (uint)(duration * 0.8));
        await Task.Delay(50);
        Animation heightAnimation = new Animation(v => container.HeightRequest = v, initialHeight, targetHeight, Easing.CubicOut);
        heightAnimation.Commit(container, "HeightExpansion", length: duration);
        await Task.Delay(100);

        
        content.IsVisible = true; //make visble first
        await content.FadeIn(200);
    }
    //XAML example
    
    
    
    
    
    //<Label x:Name="HiddenContent" Text="Revealed Content!" TextColor="Black" />
    
    
    
    
    

    public static async Task ShrinkAndHide(this VisualElement container, VisualElement content, double targetWidth, double targetHeight, uint duration = 300)
    {
        
        await content.FadeOut(150);
        content.IsVisible = false;
        double initialWidth = container.Width;
        double initialHeight = container.Height;

        

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

    
    //await MyProgressBar.ProgressFill(0.8); 

    public static async Task ShowNotificationBanner(this Label notificationLabel, string message, uint duration = 400)
    {
        notificationLabel.Text = message;
        notificationLabel.TranslationY = -notificationLabel.Height; 
        notificationLabel.Opacity = 0;
        notificationLabel.IsVisible = true; //make visible

        
        await Task.WhenAll(
            notificationLabel.TranslateTo(0, 0, duration, Easing.CubicOut),
            notificationLabel.FadeIn(duration - 100)
        );

        
        await Task.Delay(3000);

        
        await Task.WhenAll(
            notificationLabel.TranslateTo(0, -notificationLabel.Height, duration, Easing.CubicIn),
            notificationLabel.FadeOut(duration - 100)
        );
    }
    //XAML
    
    
    
    
    //C#
    

    public static async Task AnimateCounter(this Label counterLabel, int startValue, int endValue, uint duration = 500)
    {
        
        Animation animation = new Animation(v => counterLabel.Text = ((int)v).ToString(), startValue, endValue);
        animation.Commit(counterLabel, "CounterAnimation", length: duration, easing: Easing.CubicInOut);

        
        await counterLabel.Pulse();
    }

    
    
    //
    
    
    
    

    public static async Task CarouselTransition(this Image currentImage, Image nextImage, bool slideRight, uint duration = 400)
    {
        
        nextImage.TranslationX = slideRight ? Application.Current.MainPage.Width : -Application.Current.MainPage.Width;
        nextImage.Opacity = 0;
        nextImage.IsVisible = true; //make next image visible

        
        await Task.WhenAll(
            currentImage.TranslateTo(slideRight ? -Application.Current.MainPage.Width : Application.Current.MainPage.Width, 0, duration, Easing.CubicInOut),
            currentImage.FadeOut(duration),
            nextImage.TranslateTo(0, 0, duration, Easing.CubicInOut),
            nextImage.FadeIn(duration)
        );
        currentImage.IsVisible = false; //hide the prev image at last.

    }
    //XAML
    
    
    
    
    
    //<SwipeGestureRecognizer Direction="Right" Swiped="OnSwipedRight" />
    
    
    //C#
    
    
    
    
    

    public static async Task ValidateFormField(this Entry entry, Label errorLabel, bool isValid, string errorMessage = "")
    {
        if (!isValid)
        {
            errorLabel.Text = errorMessage;
            await Task.WhenAll(
                errorLabel.FadeIn(),
                entry.Shake() 
            );
        }
        else
        {
            await errorLabel.FadeOut();
        }
    }
    //XAML
    
    
    
    //C#
    
    
    
    
    

    public static async Task TabSwitchTransition(this VisualElement currentContent, VisualElement nextContent, bool slideRight, uint duration = 300)
    {
        
        nextContent.TranslationX = slideRight ? Application.Current.MainPage.Width : -Application.Current.MainPage.Width;
        nextContent.Opacity = 0;
        nextContent.IsVisible = true;

        
        await Task.WhenAll(
            currentContent.TranslateTo(slideRight ? -Application.Current.MainPage.Width : Application.Current.MainPage.Width, 0, duration, Easing.CubicInOut),
            currentContent.FadeOut(duration),
            nextContent.TranslateTo(0, 0, duration, Easing.CubicInOut),
            nextContent.FadeIn(duration)
        );

        currentContent.IsVisible = false; 
    }
    //XAML
    
    
    //content
    
    
    
    
    
    
    
    
    
    //C#
    
    
    
    

    public static async Task LiquidFill(this BoxView box, double targetHeight, Color fillColor, uint duration = 800)
    {
        
        box.BackgroundColor = fillColor;
        box.HeightRequest = 0;  //initially invisible
        box.VerticalOptions = LayoutOptions.End;
        box.IsVisible = true;

        
        Animation animation = new Animation(v => box.HeightRequest = v, 0, targetHeight);

        
        animation.Commit(box, "LiquidFillAnimation", length: duration, easing: new Easing(t => {
            
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
            EndPoint = new Point(1, 1), 
            GradientStops =
        [
            new GradientStop { Color = color1, Offset = 0 },
            new GradientStop { Color = color2, Offset = 1 }
        ]
        };

        box.Background = gradientBrush;
        box.IsVisible = true;

        
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
            return false; 
        });
        await Task.Delay(-1);
    }

    //<BoxView x:Name="GradientBox" WidthRequest="200" HeightRequest="200" />
    //await GradientBox.PulsatingGradient(Colors.Red, Colors.Yellow);

    public static async Task CircularReveal(this BoxView box, bool reveal, uint duration = 500)
    {
        
        EllipseGeometry ellipseGeometry = new EllipseGeometry();
        box.Clip = ellipseGeometry;

        
        if (reveal)
        {
            
            ellipseGeometry.Center = new Point(box.Width / 2, box.Height / 2);
            ellipseGeometry.RadiusX = 0;
            ellipseGeometry.RadiusY = 0;

        }
        else
        {
            
            ellipseGeometry.Center = new Point(box.Width / 2, box.Height / 2);
            ellipseGeometry.RadiusX = Math.Max(box.Width, box.Height); //ensure the circle is large enough to cover.
            ellipseGeometry.RadiusY = Math.Max(box.Width, box.Height);
        }

        
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
    //await CircleBox.CircularReveal(true); 

    public static async Task AnimateDashedBorder(this BoxView box, double dashLength = 10, double gapLength = 5, uint duration = 1000)
    {
        
        

        box.IsVisible = true;
        LinearGradientBrush gradientBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0.5), 
            EndPoint = new Point(1, 0.5),
            GradientStops =
        [
            new GradientStop { Color = box.BackgroundColor, Offset = 0 }, 
            new GradientStop { Color = box.BackgroundColor, Offset = (float)(dashLength / (dashLength + gapLength)) }, 
            new GradientStop { Color = Colors.Transparent, Offset = (float)(dashLength / (dashLength + gapLength)) },   
            new GradientStop { Color = Colors.Transparent, Offset = 1 }  
        ]
        };

        box.Background = gradientBrush; //initially set the gradient

        
        Animation animation = new Animation(v =>
        {
            gradientBrush.StartPoint = new Point(-v, 0.5);
            gradientBrush.EndPoint = new Point(1 - v, 0.5);
        }, 0, 1);

        animation.Commit(box, "DashedBorderAnimation", length: duration, repeat: () => true); 
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

        
        view2.TranslationX = startX2;
        view2.Opacity = 0;
        view2.IsVisible = true;

        
        await Task.WhenAll(
            view1.TranslateTo(endX1, 0, duration, Easing.CubicInOut),
            view1.FadeOut(duration),
            view2.TranslateTo(endX2, 0, duration, Easing.CubicInOut),
            view2.FadeIn(duration)
        );
        view1.IsVisible = false;
    }
    //<Grid>
    
    
    
    
    
    //</Grid>

    
    
    
    

    public static async Task DepthZoom(this Image background, Image foreground, Point tapPoint, uint duration = 500)
    {
        
        foreground.TranslationX = tapPoint.X - foreground.Width / 2;
        foreground.TranslationY = tapPoint.Y - foreground.Height / 2;

        //2. Make sure both views are visible
        background.IsVisible = true;
        foreground.IsVisible = true;

        
        background.Scale = 1;
        foreground.Scale = 0;
        foreground.Opacity = 0;


        
        await Task.WhenAll(
            background.ScaleTo(0.8, duration, Easing.CubicInOut), 
            foreground.ScaleTo(1, duration, Easing.CubicOut),    
            foreground.FadeIn(duration)
        );
    }
    
    
    
    
    
    
    

    
    
    
    
    

    public static async Task SplitReveal(this VisualElement view1, VisualElement view2, bool reveal, uint duration = 400)
    {
        
        view2.IsVisible = true;

        
        if (reveal)
        {
            
            view1.TranslationX = 0;
            view2.Opacity = 0;

        }
        else
        {
            
            view1.TranslationX = -view1.Width / 2;
            view2.Opacity = 1;
        }

        
        if (reveal)
        {
            
            await Task.WhenAll(
                view1.TranslateTo(-Shell.Current.Window.Width / 2, 0, duration, Easing.CubicInOut),
                view2.FadeIn(duration)
            );
        }
        else
        {
            
            await Task.WhenAll(
                view1.TranslateTo(0, 0, duration, Easing.CubicInOut),
                view2.FadeOut(duration)
            );
        }
    }
    
    
    
    
    
    
    
    
    
    

    
    
    
    
    
    

    public static async Task PageTurn(this Image currentPage, Image nextPage, uint duration = 600)
    {
        
        nextPage.RotationY = -90; 
        nextPage.Opacity = 0;
        nextPage.IsVisible = true;
        
        currentPage.ZIndex = 1;
        nextPage.ZIndex = 0;

        
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
    
    
    
    
    
    //</Grid>

    //private async void OnPageTurnTapped(object sender, TappedEventArgs e)
    //{
    
    //}

    public static async Task StaggeredRevealWithMask(this Image image, BoxView mask, Layout contentLayout, uint duration = 600)
    {
        
        mask.WidthRequest = image.Width;
        mask.HeightRequest = image.Height; //same size!
        mask.BackgroundColor = Colors.Black; 
        mask.IsVisible = true;
        mask.Opacity = 1; //fully visible

        //make content layout invisible at first.
        foreach (VisualElement child in contentLayout.Children)
        {
            child.Opacity = 0; 
            child.IsVisible = false;
        }

        
        
        
        await mask.TranslateTo(image.Width, 0, duration, Easing.CubicInOut);
        mask.IsVisible = false; //hid it

        
        await contentLayout.StaggeredFadeIn(duration: 300, delay: 50); 
    }
    //<Grid>
    
    
    
    
    
    
    
    
    
    
    //</Grid>

    
    
    
    
    public static async Task ExplodeView(this Layout container, uint duration = 500)
    {
        
        Random random = new Random();

        foreach (VisualElement child in container.Children.OfType<VisualElement>())
        {
            
            double translateX = random.Next(-200, 200); 
            double translateY = random.Next(-200, 200);
            double rotation = random.Next(-180, 180);

            
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

        
        avatarView.Stroke = strokeColor;

        
        Animation expandAnimation = new Animation(v => avatarView.StrokeThickness = v, 
            0,                                   
            5,                                  
            Easing.CubicInOut                    
        );

        
        Animation shrinkAnimation = new Animation(
            v => avatarView.StrokeThickness = v,
            5,                                   
            0,                                    
            Easing.CubicInOut
        );

        
        Animation animationSequence = new Animation
        {
            { 0, 0.5, expandAnimation },   
            { 0.5, 1, shrinkAnimation }    
        };

        
        animationSequence.Commit(avatarView, "FocusModeAnimation", length: 500, easing: Easing.Linear);
    }

    
    
    
    
    
    
    
    

    
    
    
    


    //XAML
    
    
    
    
    
    
    //A LOT of content here
    
    
    

    //C#
    
    
    
    

    

    

    
    public static async Task Blink(this VisualElement element, uint duration = 100, int blinks = 3)
    {
        for (int i = 0; i < blinks; i++)
        {
            await element.FadeTo(0, duration, Easing.Linear);
            await element.FadeTo(1, duration, Easing.Linear);
        }
    }
    

    
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
    

    
    public static async Task ScaleX(this VisualElement element, double targetScaleX, uint duration = 250)
    {
        await element.ScaleXTo(targetScaleX, duration, Easing.CubicInOut);
    }
    

    
    public static async Task ScaleY(this VisualElement element, double targetScaleY, uint duration = 250)
    {
        await element.ScaleYTo(targetScaleY, duration, Easing.CubicInOut);
    }
    

    
    public static async Task TranslateX(this VisualElement element, double targetX, uint duration = 250)
    {
        await element.TranslateTo(targetX, element.TranslationY, duration, Easing.CubicInOut);
    }
    //Usage: await myView.TranslateX(250);

    
    public static async Task TranslateY(this VisualElement element, double targetY, uint duration = 250)
    {
        await element.TranslateTo(element.TranslationX, targetY, duration, Easing.CubicInOut);
    }
    //Usage: await myView.TranslateY(250);


    
    public static async Task FadeAndTranslate(this VisualElement element, double targetX, double targetY, uint duration = 300)
    {
        await Task.WhenAll(
            element.FadeTo(0.5, duration / 2, Easing.Linear), 
            element.TranslateTo(targetX, targetY, duration, Easing.CubicInOut)
        );
        await element.FadeTo(1, duration / 2, Easing.Linear);    //fade in to complete.
    }
    


    
    public static async Task OpacityPulse(this VisualElement element, double minOpacity = 0.5, uint duration = 500)
    {
        Animation fadeInAnim = new Animation(v => element.Opacity = v, minOpacity, 1, Easing.SinInOut);
        Animation fadeOutAnim = new Animation(v => element.Opacity = v, 1, minOpacity, Easing.SinInOut);

        fadeInAnim.Commit(element, "OpacityPulseIn", length: duration / 2, repeat: () =>
        {
            fadeOutAnim.Commit(element, "OpacityPulseOut", length: duration / 2, repeat: () => true); 
            return false; //end first animation
        });
        await Task.Delay(-1); //run forever.

    }
    

    
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
    
    

    

    
    public static async Task Jiggle(this VisualElement element, uint duration = 200, int jiggles = 3, double angle = 10)
    {
        Random random = new Random();
        for (int i = 0; i < jiggles; i++)
        {
            double rotation = (random.NextDouble() * 2 - 1) * angle; 
            await element.RotateTo(rotation, duration / (uint)jiggles, Easing.CubicInOut);
        }
        await element.RotateTo(0, duration / (uint)jiggles, Easing.CubicInOut); 
    }
    

    
    public static async Task ParabolicJump(this VisualElement element, double height, double distance, uint duration = 500)
    {
        

        
        Animation jumpUp = new Animation(v =>
        {
            element.TranslationY = -v; 
            element.TranslationX = distance * (v / height); 
        }, 0, height, Easing.CubicOut);  

        
        Animation jumpDown = new Animation(v =>
        {
            element.TranslationY = -height + v; 
            element.TranslationX = distance * ((height + v) / (2 * height));   
        }, 0, height, Easing.CubicIn);   


        jumpUp.Commit(element, "JumpUp", length: duration / 2, finished: (v, c) =>
        {
            jumpDown.Commit(element, "JumpDown", length: duration / 2); 
        });
        await Task.Delay((int)duration); //wait until jump anim is done.

    }
    


    
    public static async Task SwirlIn(this VisualElement element, uint duration = 400, double rotations = 2)
    {
        element.Rotation = 360 * rotations; 
        element.Scale = 0;                 
        element.Opacity = 0;
        element.IsVisible = true;
        await Task.WhenAll(
            element.RotateTo(0, duration, Easing.CubicOut),
            element.ScaleTo(1, duration, Easing.CubicOut),
            element.FadeIn(duration)
        );
    }
    

    
    public static async Task SwirlOut(this VisualElement element, uint duration = 400, double rotations = 2)
    {
        await Task.WhenAll(
          element.RotateTo(360 * rotations, duration, Easing.CubicIn),
          element.ScaleTo(0, duration, Easing.CubicIn),
          element.FadeOut(duration)
        );
        element.IsVisible = false;
    }
    

    
    public static async Task SquashAndStretch(this VisualElement element, double squashFactor = 0.7, uint duration = 200)
    {
        await Task.WhenAll( 
            element.ScaleXTo(1 + (1 - squashFactor), duration / 2, Easing.CubicOut),
            element.ScaleYTo(squashFactor, duration / 2, Easing.CubicOut)
        );
        await Task.WhenAll( 
             element.ScaleXTo(squashFactor, duration / 2, Easing.CubicIn),
             element.ScaleYTo(1 + (1 - squashFactor), duration / 2, Easing.CubicIn)
        );
        await Task.WhenAll(
            element.ScaleXTo(1, duration / 4, Easing.CubicOut),
            element.ScaleYTo(1, duration / 4, Easing.CubicOut)
        );
    }
    

    
    public static async Task ElasticSnap(this VisualElement element, double displacementX, double displacementY, uint duration = 300)
    {
        
        await element.TranslateTo(displacementX, displacementY, duration / 2, Easing.CubicOut);
        
        await element.TranslateTo(0, 0, duration / 2, Easing.SpringOut);
    }
    

    
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

        await Task.Delay(-1);  
    }
    

    
    public static async Task DelayedReveal(this VisualElement element, uint delay = 500, uint duration = 300)
    {
        element.Opacity = 0;
        element.IsVisible = true;
        await Task.Delay((int)delay);
        await element.FadeIn(duration);
    }
    

    public static async Task MorphShapes(this BoxView boxView, bool toCircle, uint duration = 400)
    {
        double startRadius, endRadius;

        if (toCircle)
        {
            startRadius = 0;  
            endRadius = Math.Max(boxView.Width, boxView.Height) / 2; 

        }
        else
        {
            startRadius = Math.Max(boxView.Width, boxView.Height) / 2; 
            endRadius = 0;   
        }
        //Commit the animation
        await boxView.AnimateAsync("MorphShapes", (progress) =>
        {
            boxView.CornerRadius = new CornerRadius(startRadius + (endRadius - startRadius) * progress);
        }, length: duration, easing: Easing.CubicInOut);

    }
    
    
    

    public static async Task MyBackgroundColorTo(this VisualElement element, Color targetColor, uint length = 250)
    {
        if (element.BackgroundColor == null)
            return;

        Color startColor = element.BackgroundColor;
        uint startTime = (uint)Environment.TickCount;

        while (Environment.TickCount - startTime < length)
        {
            double progress = (double)(Environment.TickCount - startTime) / length;
            progress = Math.Min(1, progress); 

            Color currentColor = Color.FromRgba(
                startColor.Red + (targetColor.Red - startColor.Red) * progress,
                startColor.Green + (targetColor.Green - startColor.Green) * progress,
                startColor.Blue + (targetColor.Blue - startColor.Blue) * progress,
                startColor.Alpha + (targetColor.Alpha - startColor.Alpha) * progress
            );

            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                element.BackgroundColor = currentColor;
            });

            
            await Task.Yield(); 
        }

        
        MainThread.BeginInvokeOnMainThread(() =>
        {
            element.BackgroundColor = targetColor;
        });
    }


}