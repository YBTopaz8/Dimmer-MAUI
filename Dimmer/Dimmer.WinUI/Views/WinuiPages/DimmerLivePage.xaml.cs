using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Controls.Primitives;

using Button = Microsoft.UI.Xaml.Controls.Button;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using System.Threading.Tasks;
using Dimmer.Utilities.Extensions;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class DimmerLivePage : Page
{
    SessionManagementViewModel MyViewModel;
    BaseViewModelWin uiViewModel;
    public DimmerLivePage()
    {
        InitializeComponent(); 
        
        _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;


    }

    private readonly Microsoft.UI.Composition.Compositor _compositor;
    protected async override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MyViewModel ??= IPlatformApplication.Current!.Services.GetService<SessionManagementViewModel>()!;
        uiViewModel = IPlatformApplication.Current!.Services.GetService<BaseViewModelWin>()!;
         // The parameter passed from Frame.Navigate is in e.Parameter.
        // Cast it to your ViewModel type and set your properties.
        if (MyViewModel != null)
        {
            //MyViewModel.CurrentWinUIPage = this;
            // Now that the ViewModel is set, you can set the DataContext.
            this.DataContext = MyViewModel;
            try
            {
                await MyViewModel.LoginViewModel.InitializeAsync();
                var isAuth =  MyViewModel.LoginViewModel.CurrentUser.IsAuthenticated;
                if (isAuth)
                {
                    GridOfAuth.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                    GridFullView.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    await MyViewModel.RegisterCurrentDeviceAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }

    private void BackBtn_Click(object sender, RoutedEventArgs e)
    {
        if(Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private void ForgottenPassword_PointerPressed(object sender, PointerRoutedEventArgs e)
    {

    }
    private void ResetElementVisual(FrameworkElement frameElt)
    {
        var visual = ElementCompositionPreview.GetElementVisual(frameElt);

        // Reset all transform properties to defaults
        visual.Opacity = 1f;
        visual.Offset = Vector3.Zero;
        visual.Scale = Vector3.One;
        visual.RotationAngleInDegrees = 0f;
        visual.RotationAxis = new Vector3(0, 0, 1); // Default Z-axis
    }
    private void Button_Click(object sender, RoutedEventArgs e)
    {

    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {

        MyViewModel.LoginViewModel.Username = DimmerUserName.Text;
        MyViewModel.LoginViewModel.Password = DimmerPassword.Text;
        await MyViewModel.LoginViewModel.LoginCommand.ExecuteAsync(null);
        MyViewModel.LoginViewModel.Username = string.Empty;
        MyViewModel.LoginViewModel.Password = string.Empty;

    }

    private void ForgottenPassword_Click(object sender, RoutedEventArgs e)
    {

    }

    private async void SwitchToSignUpBtn_Click(object sender, RoutedEventArgs e)
    {
        await Task.WhenAll(
            ApplyExitEffectAsync(LoginStackPanel, ExitTransitionEffect.FlyUp), ApplyEntranceEffectAsync(SignUpPanel, SongTransitionAnimation.Slide)
            );
    }

    private async void SwitchToLoginBtn_Click(object sender, RoutedEventArgs e)
    {
        await Task.WhenAll(
            ApplyExitEffectAsync(SignUpPanel, ExitTransitionEffect.FlyUp), ApplyEntranceEffectAsync(LoginStackPanel, SongTransitionAnimation.Slide)
            );
        LoginStackPanel.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center;
    }
    private async void SwitchViews_Click(object sender, RoutedEventArgs e)
    {
        // 1. Animate the old view out
        await ApplyExitEffectAsync(LoginStackPanel, ExitTransitionEffect.SlideLeft);

        // 2. Prepare the new view (ensure it's visible but 0 opacity or offset if doing an entrance)
        SignUpPanel.Visibility = Microsoft.UI.Xaml.Visibility.Visible;

        // 3. Animate the new view in
       await ApplyEntranceEffectAsync(SignUpPanel, SongTransitionAnimation.Slide);
    }
    public async Task ApplyEntranceEffectAsync(FrameworkElement frameElt, SongTransitionAnimation defAnim = SongTransitionAnimation.Spring)
    {
        // 1. Ensure the element is visible to the layout engine
        frameElt.Visibility = Microsoft.UI.Xaml.Visibility.Visible;

        // Optional: Ensure layout is calculated so ActualWidth/Height are correct for CenterPoint
        // frameElt.UpdateLayout(); 

        var visual = ElementCompositionPreview.GetElementVisual(frameElt);
        var duration = TimeSpan.FromMilliseconds(350);

        // 2. Create the Batch to wait for completion
        var batch = _compositor.CreateScopedBatch(CompositionBatchTypes.Animation);

        // Standard easing
        var cubicBezier = _compositor.CreateCubicBezierEasingFunction(new Vector2(0.5f, 0.0f), new Vector2(0.5f, 1.0f));

        switch (defAnim)
        {
            case SongTransitionAnimation.Fade:
                // INITIAL STATE: Invisible
                visual.Opacity = 0f;
                visual.Offset = Vector3.Zero; // Reset any previous offsets
                visual.Scale = Vector3.One;   // Reset any previous scaling

                // ANIMATION: Fade to 1
                var fade = _compositor.CreateScalarKeyFrameAnimation();
                fade.InsertKeyFrame(1f, 1f);
                fade.Duration = duration;
                visual.StartAnimation("Opacity", fade);
                break;

            case SongTransitionAnimation.Scale:
                // INITIAL STATE: Small and Opaque
                visual.Opacity = 1f;
                visual.Offset = Vector3.Zero;
                visual.CenterPoint = new Vector3((float)frameElt.ActualWidth / 2,
                                                 (float)frameElt.ActualHeight / 2, 0);
                visual.Scale = new Vector3(0.8f);

                // ANIMATION: Scale to 1
                var scale = _compositor.CreateVector3KeyFrameAnimation();
                scale.InsertKeyFrame(1f, Vector3.One, cubicBezier);
                scale.Duration = duration;
                visual.StartAnimation("Scale", scale);
                break;

            case SongTransitionAnimation.Slide:
                // INITIAL STATE: Shifted Right
                visual.Opacity = 1f;
                visual.Scale = Vector3.One;
                visual.Offset = new Vector3(80f, 0, 0);

                // ANIMATION: Slide to center as HorizontalAlignment = Center
                var slide = _compositor.CreateVector3KeyFrameAnimation();
                slide.InsertKeyFrame(1f, Vector3.Zero, cubicBezier);
                slide.Duration = duration;
                visual.StartAnimation("Offset", slide);
                break;

            case SongTransitionAnimation.Spring:
            default:
                // INITIAL STATE: Shifted Down
                visual.Opacity = 1f;
                visual.Scale = Vector3.One;
                visual.Offset = new Vector3(0, 40, 0);

                // ANIMATION: Spring to 0
                var spring = _compositor.CreateSpringVector3Animation();
                spring.FinalValue = Vector3.Zero;
                spring.DampingRatio = 0.5f;
                spring.Period = duration;
                visual.StartAnimation("Offset", spring);
                break;
        }

        // 3. End Batch and Wait
        batch.End();
        var tcs = new TaskCompletionSource<bool>();
        batch.Completed += (s, e) => tcs.SetResult(true);
        await tcs.Task;
    }
    private async Task ApplyExitEffectAsync(FrameworkElement frameElt, ExitTransitionEffect exitAnim = ExitTransitionEffect.FadeSlideDown)
    {
        var visual = ElementCompositionPreview.GetElementVisual(frameElt);
        var duration = TimeSpan.FromMilliseconds(400);
        // 1. Create a "Batch" to track when animations finish
        var batch = _compositor.CreateScopedBatch(CompositionBatchTypes.Animation);

        // Ensure the center point is set correctly for Scale/Rotation animations
        visual.CenterPoint = new Vector3((float)frameElt.ActualWidth / 2f,
                                         (float)frameElt.ActualHeight / 2f, 0);

        // Standard linear easing for simple fades, Cubic for movement
        var cubicBezier = _compositor.CreateCubicBezierEasingFunction(new Vector2(0.5f, 0.0f), new Vector2(0.5f, 1.0f));

        switch (exitAnim)
        {
            // 1. THE REQUESTED EFFECT: Fade Out + Slide Down
            case ExitTransitionEffect.FadeSlideDown:
            default:
                // Opacity: 1 -> 0
                var fadeDown = _compositor.CreateScalarKeyFrameAnimation();
                fadeDown.InsertKeyFrame(1f, 0f);
                fadeDown.Duration = duration;

                // Offset: (0,0) -> (0, 50)
                var slideDown = _compositor.CreateVector3KeyFrameAnimation();
                slideDown.InsertKeyFrame(1f, new Vector3(0, 50f, 0), cubicBezier);
                slideDown.Duration = duration;

                visual.StartAnimation("Opacity", fadeDown);
                visual.StartAnimation("Offset", slideDown);
                break;

            // 2. Zoom Out (Scale down to nothing)
            case ExitTransitionEffect.ZoomOut:
                var zoom = _compositor.CreateVector3KeyFrameAnimation();
                zoom.InsertKeyFrame(1f, new Vector3(0f)); // Scale to 0
                zoom.Duration = duration;

                var fadeZoom = _compositor.CreateScalarKeyFrameAnimation();
                fadeZoom.InsertKeyFrame(1f, 0f);
                fadeZoom.Duration = duration;

                visual.StartAnimation("Scale", zoom);
                visual.StartAnimation("Opacity", fadeZoom);
                break;

            // 3. Slide Right
            case ExitTransitionEffect.SlideRight:
                var slideR = _compositor.CreateVector3KeyFrameAnimation();
                // Move right by width + buffer
                slideR.InsertKeyFrame(1f, new Vector3((float)frameElt.ActualWidth + 50f, 0, 0), cubicBezier);
                slideR.Duration = duration;

                visual.StartAnimation("Offset", slideR);
                break;

            // 4. Slide Left
            case ExitTransitionEffect.SlideLeft:
                var slideL = _compositor.CreateVector3KeyFrameAnimation();
                slideL.InsertKeyFrame(1f, new Vector3(-((float)frameElt.ActualWidth + 50f), 0, 0), cubicBezier);
                slideL.Duration = duration;

                visual.StartAnimation("Offset", slideL);
                break;

            // 5. Fly Up (Fade out while moving up)
            case ExitTransitionEffect.FlyUp:
                var flyUp = _compositor.CreateVector3KeyFrameAnimation();
                flyUp.InsertKeyFrame(1f, new Vector3(0, -50f, 0), cubicBezier);
                flyUp.Duration = duration;

                var fadeUp = _compositor.CreateScalarKeyFrameAnimation();
                fadeUp.InsertKeyFrame(1f, 0f);
                fadeUp.Duration = duration;

                visual.StartAnimation("Offset", flyUp);
                visual.StartAnimation("Opacity", fadeUp);
                break;

            // 6. Spin and Shrink (Whirlpool effect)
            case ExitTransitionEffect.SpinAndShrink:
                var spin = _compositor.CreateScalarKeyFrameAnimation();
                spin.InsertKeyFrame(1f, 360f, cubicBezier); // Rotate full circle
                spin.Duration = duration;

                var shrink = _compositor.CreateVector3KeyFrameAnimation();
                shrink.InsertKeyFrame(1f, Vector3.Zero);
                shrink.Duration = duration;

                visual.StartAnimation("RotationAngleInDegrees", spin);
                visual.StartAnimation("Scale", shrink);
                break;

            // 7. Flip Horizontal (Rotates along Y axis until invisible)
            case ExitTransitionEffect.FlipHorizontal:
                var flip = _compositor.CreateScalarKeyFrameAnimation();
                // Rotate 90 degrees (edge-on to viewer, effectively invisible)
                flip.InsertKeyFrame(1f, 90f, cubicBezier);
                flip.Duration = duration;

                // Set the Axis of rotation to Y
                visual.RotationAxis = new Vector3(0, 1, 0);
                visual.StartAnimation("RotationAngleInDegrees", flip);

                // Helpful to fade it slightly at the end to prevent "clipping" artifacts
                var fadeFlip = _compositor.CreateScalarKeyFrameAnimation();
                fadeFlip.InsertKeyFrame(0.5f, 1f);
                fadeFlip.InsertKeyFrame(1f, 0f);
                fadeFlip.Duration = duration;
                visual.StartAnimation("Opacity", fadeFlip);
                break;

            // 8. Fold Vertical (Squash Y scale)
            case ExitTransitionEffect.FoldVertical:
                var fold = _compositor.CreateVector3KeyFrameAnimation();
                // Keep X scale at 1, squash Y to 0
                fold.InsertKeyFrame(1f, new Vector3(1f, 0f, 1f), cubicBezier);
                fold.Duration = duration;

                visual.StartAnimation("Scale", fold);
                break;

            // 9. Explode (Scale up + Fade out)
            case ExitTransitionEffect.Explode:
                var explodeScale = _compositor.CreateVector3KeyFrameAnimation();
                explodeScale.InsertKeyFrame(1f, new Vector3(1.5f), cubicBezier); // Grow 1.5x
                explodeScale.Duration = duration;

                var explodeFade = _compositor.CreateScalarKeyFrameAnimation();
                explodeFade.InsertKeyFrame(0f, 1f);
                explodeFade.InsertKeyFrame(1f, 0f); // Disappear
                explodeFade.Duration = duration;

                visual.StartAnimation("Scale", explodeScale);
                visual.StartAnimation("Opacity", explodeFade);
                break;
        }

        // 2. End the batch definition
        batch.End();

        // 3. Wait for the batch to complete asynchronously
        var tcs = new TaskCompletionSource<bool>();
        batch.Completed += (s, e) => tcs.SetResult(true);
        await tcs.Task;

        // 4. CLEANUP: Now that animation is done, hide the XAML element
        frameElt.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

        // 5. RESET: Reset visuals so if you set Visibility.Visible later, it looks normal
        ResetElementVisual(frameElt);
    }

    

    private async  void DimmerPasswordSignUpConfirm_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
    {
       
    }

    private async void DimmerPasswordSignUpConfirm_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(DimmerPasswordSignUpConfirm.Password))
            return;
        var dimmPassOne = DimmerPasswordSignUp.Password;
        if (dimmPassOne != DimmerPasswordSignUpConfirm.Password)
        {
            SignupBtn.IsEnabled = false;
            NotSamePasswordTxtBlock.Text = "Both PassWords Must Be Identical";
            await ApplyEntranceEffectAsync(NotSamePasswordTxtBlock, SongTransitionAnimation.Fade);
            await Task.Delay(800);
            await ApplyExitEffectAsync(NotSamePasswordTxtBlock, ExitTransitionEffect.SlideLeft);

            return;
        }
        RxSchedulers.UI.Schedule(() =>
        {
            SignupBtn.IsEnabled = true;
        });

    }

    private async void SignupBtn_Click(object sender, RoutedEventArgs e)
    {
        var res = await MyViewModel.LoginViewModel.SignUpNormally(DimmerUserNameSignUp.Text, DimmerPasswordSignUpConfirm.Password, DimmerEmailSignUp.Text);
        if(res.IsSuccess)
        {
            SwitchToLoginBtn_Click(sender, e);
        }
    }
}
