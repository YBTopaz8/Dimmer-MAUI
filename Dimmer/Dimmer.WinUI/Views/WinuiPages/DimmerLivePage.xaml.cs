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

using Grid = Microsoft.UI.Xaml.Controls.Grid;
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
                var isAuth =  MyViewModel.LoginViewModel.CurrentUserOnline?.IsAuthenticated;
                if (isAuth is null) return;
                if ((bool)isAuth)
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

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {

        MyViewModel.LoginViewModel.Username = DimmerUserName.Text;
        MyViewModel.LoginViewModel.Password = DimmerPassword.Text;
        await MyViewModel.LoginViewModel.LoginAsync();
        MyViewModel.LoginViewModel.Username = string.Empty;
        MyViewModel.LoginViewModel.Password = string.Empty;

    }

    private void ForgottenPassword_Click(object sender, RoutedEventArgs e)
    {

    }

    private async void SwitchToSignUpBtn_Click(object sender, RoutedEventArgs e)
    {
        await Task.WhenAll(
            PlatUtils.ApplyExitEffectAsync(LoginStackPanel, _compositor, ExitTransitionEffect.FlyUp), ApplyEntranceEffectAsync(SignUpPanel, SongTransitionAnimation.Slide)
            );
    }

    private async void SwitchToLoginBtn_Click(object sender, RoutedEventArgs e)
    {
        await Task.WhenAll(
            PlatUtils.ApplyExitEffectAsync(SignUpPanel, _compositor, ExitTransitionEffect.FlyUp), ApplyEntranceEffectAsync(LoginStackPanel, SongTransitionAnimation.Slide)
            );
        LoginStackPanel.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center;
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
            await PlatUtils.ApplyExitEffectAsync(NotSamePasswordTxtBlock, _compositor, ExitTransitionEffect.SlideLeft);

            return;
        }
        RxSchedulers.UI.ScheduleToUI(() =>
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

    private async void BackUpDeviceBtn_Click(object sender, RoutedEventArgs e)
    {
       await MyViewModel.BackUpDataToCloud();
    }
}
