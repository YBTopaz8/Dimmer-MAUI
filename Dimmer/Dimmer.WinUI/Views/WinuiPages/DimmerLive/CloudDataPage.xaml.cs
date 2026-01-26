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
using NavigationEventArgs = Microsoft.UI.Xaml.Navigation.NavigationEventArgs;
using System.Threading.Tasks;
using Color = System.Drawing.Color;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages.DimmerLive;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class CloudDataPage : Page
{
    public SessionManagementViewModel MyViewModel { get; set; }

    public CloudDataPage()
    {
        this.InitializeComponent();

        // Resolve the ViewModel from your DI Container / App.Services
    }
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MyViewModel = IPlatformApplication.Current!.Services.GetService<SessionManagementViewModel>()!;
        MyViewModel.OnPageNavigatedTo();
        this.DataContext = MyViewModel; // Set DataContext for binding within DataTemplates
        this.Name = "RootPage"; // Helper for ElementName binding
        try
        {

            await MyViewModel.LoginViewModel.InitializeAsync();
        await MyViewModel.LoadBackupsAsync();
        await MyViewModel.RegisterCurrentDeviceAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    private async void RestorebackBtn_Click(object sender, RoutedEventArgs e)
    {
        var send = (Button)sender;
        var objId = send.CommandParameter as string;
        await MyViewModel.RestoreBackupAsync(objId);
    }

    private async void MyPage_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var props = e.GetCurrentPoint(this).Properties;
        if (props.IsXButton1Pressed)
        {
            e.Handled = true;

            if (Frame.CanGoBack)
            {

                EnsureCompositionBorderSetup();
                await AnimatePageShrinkAsync();


                Frame.GoBack();
            }

        }

    }
    private void EnsureCompositionBorderSetup()
    {
        // If we've already set it up, don't do it again
        if (_shapeVisual != null) return;

        // 1. Get the Compositor
        var visual = ElementCompositionPreview.GetElementVisual(PageContentGrid);
        var compositor = visual.Compositor;

        // 2. Create a ShapeVisual to hold our vector graphics
        // We place this ON TOP of the XAML content
        _shapeVisual = compositor.CreateShapeVisual();
        ElementCompositionPreview.SetElementChildVisual(PageContentGrid, _shapeVisual);

        // 3. Keep the ShapeVisual the same size as the Grid using Expression Animation
        var bindSizeAnimation = compositor.CreateExpressionAnimation("HostVisual.Size");
        bindSizeAnimation.SetReferenceParameter("HostVisual", visual);
        _shapeVisual.StartAnimation("Size", bindSizeAnimation);

        // 4. Create the Rectangle Geometry (The box)
        var rectGeometry = compositor.CreateRoundedRectangleGeometry();
        // Bind the geometry size to the visual size so it fills the screen
        var bindGeometrySize = compositor.CreateExpressionAnimation("Visual.Size");
        bindGeometrySize.SetReferenceParameter("Visual", _shapeVisual);
        rectGeometry.StartAnimation("Size", bindGeometrySize);

        // 5. Create the SpriteShape (The actual drawable object)
        _borderShape = compositor.CreateSpriteShape(rectGeometry);

        // Setup the Stroke (Border)
        // DarkSlateBlue Hex is #483D8B
        _borderShape.StrokeBrush = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(255, 72, 61, 139));

        // Start with 0 thickness (invisible)
        _borderShape.StrokeThickness = 0f;

        // Important: We don't want a fill, just the border
        _borderShape.FillBrush = null;

        // Add shape to visual
        _shapeVisual.Shapes.Add(_borderShape);
    }

    private async System.Threading.Tasks.Task PlayFeedbackAnimationAsync()
    {
        var visual = ElementCompositionPreview.GetElementVisual(PageContentGrid);
        var compositor = visual.Compositor;

        // --- SETUP CENTER POINT FOR SCALING ---
        // We want to shrink towards the center
        visual.CenterPoint = new Vector3((float)PageContentGrid.ActualWidth / 2, (float)PageContentGrid.ActualHeight / 2, 0);

        // --- CREATE ANIMATIONS ---

        // 1. Scale Animation (Shrink effect)
        var scaleAnim = compositor.CreateVector3KeyFrameAnimation();
        scaleAnim.InsertKeyFrame(1.0f, new Vector3(0.95f, 0.95f, 1.0f)); // Shrink to 95%
        scaleAnim.Duration = TimeSpan.FromMilliseconds(200);
        scaleAnim.Target = "Scale";

        // 2. Stroke Thickness Animation (Border appearing)
        // Animating from 0 to 2.4
        var borderAnim = compositor.CreateScalarKeyFrameAnimation();
        borderAnim.InsertKeyFrame(1.0f, 2.4f);
        borderAnim.Duration = TimeSpan.FromMilliseconds(200);
        borderAnim.Target = "StrokeThickness";

        // --- EXECUTE AS BATCH ---
        // A ScopedBatch lets us await the completion of multiple animations
        var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);

        visual.StartAnimation("Scale", scaleAnim);
        _borderShape.StartAnimation("StrokeThickness", borderAnim);

        batch.End();

        // Use a TaskCompletionSource to await the batch event
        var tcs = new System.Threading.Tasks.TaskCompletionSource<object>();
        batch.Completed += (s, a) => tcs.TrySetResult(null);

        await tcs.Task;
    }
    private CompositionSpriteShape _borderShape;
    private ShapeVisual _shapeVisual;
    private async Task AnimatePageShrinkAsync()
    {

        Visual visual = ElementCompositionPreview.GetElementVisual(PageContentGrid);
        Compositor compositor = visual.Compositor;

        visual.CenterPoint = new Vector3(
            (float)PageContentGrid.ActualWidth / 2,
            (float)PageContentGrid.ActualHeight / 2,
            0);

        var bindSizeAnimation = compositor.CreateExpressionAnimation("Vector3(Visual.Size.X / 2, Visual.Size.Y / 2, 0)");
        bindSizeAnimation.SetReferenceParameter("Visual", visual);
        visual.StartAnimation("CenterPoint", bindSizeAnimation);

        var springAnim = compositor.CreateSpringVector3Animation();
        springAnim.Target = "Scale";


        springAnim.FinalValue = new Vector3(0.98f, 0.98f, 1.0f);


        springAnim.DampingRatio = 0.7f;
        springAnim.Period = TimeSpan.FromSeconds(0.1); 

        visual.StartAnimation("Scale", springAnim);


        await Task.Delay(150);
    }
}