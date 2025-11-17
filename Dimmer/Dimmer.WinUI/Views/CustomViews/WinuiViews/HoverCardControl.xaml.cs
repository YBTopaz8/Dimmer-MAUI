using System.Numerics;

using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;

using Windows.System.Threading;

using Border = Microsoft.UI.Xaml.Controls.Border;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.CustomViews.WinuiViews;

public sealed partial class HoverCardControl : UserControl
{
    private readonly Compositor _compositor;
    private readonly Visual _rootVisual;
    public HoverCardControl()
    {
        InitializeComponent();
        _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
        _rootVisual = ElementCompositionPreview.GetElementVisual(this);

    }

    private ThreadPoolTimer? _hoverTimer;
    private void StartHoverDelay()
    {
        CancelHover();
        _hoverTimer = ThreadPoolTimer.CreateTimer(_ =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                Debug.WriteLine("Hover timer tick fired!");
                var send = CardBorder;
                Debug.WriteLine("Card hover ended");
                Debug.WriteLine($"Border scale now is {send.Scale}");
                Debug.WriteLine($"Border width now is {send.Width}");
                Debug.WriteLine($"Border height now is {send.Height}");
                AnimateExpand();
            });
        }, TimeSpan.FromMilliseconds(250));
    }



    private void CancelHover()
    {
        _hoverTimer?.Cancel();
        _hoverTimer = null;
        DispatcherQueue.TryEnqueue(() => AnimateCollapse());
        Debug.WriteLine("Hover exited!");
    }
    private async void AnimateExpand()
    {
        try
        {
            if (_isAnimating) return;
            _isAnimating = true;

            var scale = _compositor.CreateVector3KeyFrameAnimation();
            scale.InsertKeyFrame(1f, new Vector3(1.05f));
            scale.Duration = TimeSpan.FromMilliseconds(250);
            _rootVisual.CenterPoint = new Vector3((float)ActualWidth / 2, (float)ActualHeight / 2, 0);
            _rootVisual.StartAnimation("Scale", scale);

            var visual = ElementCompositionPreview.GetElementVisual(ExtraPanel);
            visual.StopAnimation("Opacity");
            visual.StopAnimation("Offset");

            var fade = _compositor.CreateScalarKeyFrameAnimation();
            fade.InsertKeyFrame(1f, 1f);
            fade.Duration = TimeSpan.FromMilliseconds(200);

            var slide = _compositor.CreateVector3KeyFrameAnimation();
            slide.InsertKeyFrame(0f, new Vector3(0, 20, 0));
            slide.InsertKeyFrame(1f, Vector3.Zero);
            slide.Duration = TimeSpan.FromMilliseconds(200);

            visual.StartAnimation("Opacity", fade);
            visual.StartAnimation("Offset", slide);

            await Task.Delay(250); // Let animation finish
            _isAnimating = false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AnimateExpand Exception: {ex.Message}");
        }
    }

    private async void AnimateCollapse()
    {
        try
        {

            if (_isAnimating) return;
            _isAnimating = true;

            var scale = _compositor.CreateVector3KeyFrameAnimation();
            scale.InsertKeyFrame(1f, new Vector3(1f));
            scale.Duration = TimeSpan.FromMilliseconds(200);
            _rootVisual.StartAnimation("Scale", scale);

            var visual = ElementCompositionPreview.GetElementVisual(ExtraPanel);

            var fade = _compositor.CreateScalarKeyFrameAnimation();
            fade.InsertKeyFrame(1f, 0f);
            fade.Duration = TimeSpan.FromMilliseconds(200);

            var slide = _compositor.CreateVector3KeyFrameAnimation();
            slide.InsertKeyFrame(0f, Vector3.Zero);
            slide.InsertKeyFrame(1f, new Vector3(0, 20, 0));
            slide.Duration = TimeSpan.FromMilliseconds(200);

            visual.StartAnimation("Opacity", fade);
            visual.StartAnimation("Offset", slide);

            await Task.Delay(250); // Wait until animations are done
            _isAnimating = false;
        }
        catch (Exception ex)
        {

            Debug.WriteLine($"AnimateCollapse Exception: {ex.Message}");
        }
    }
    private static void SafeAnimate(UIElement element, Action<Visual, Compositor> animate)
    {
        element.DispatcherQueue.TryEnqueue(() =>
        {
            if (element is null) return;
            var visual = ElementCompositionPreview.GetElementVisual(element);
            animate(visual, visual.Compositor);
        });
    }

    private void Image_PointerEntered(object sender, PointerRoutedEventArgs e)
    {

        Debug.WriteLine("image text block entered");
    }

    private void TextBlock_PointerEntered(object sender, PointerRoutedEventArgs e)
    {

        Debug.WriteLine("title text block entered");
    }

    private void TextBlock_PointerEntered_1(object sender, PointerRoutedEventArgs e)
    {

        Debug.WriteLine("artist text block entered ");
    }

    private void TextBlock_PointerExited(object sender, PointerRoutedEventArgs e)
    {

        Debug.WriteLine("title text block exited");
    }

    private void TextBlock_PointerExited_1(object sender, PointerRoutedEventArgs e)
    {

        Debug.WriteLine("artist name text block exited");
    }

    private void Image_PointerExited(object sender, PointerRoutedEventArgs e)
    {

        Debug.WriteLine("image text block exited");
    }
    private bool _isHovered;
    private bool _isAnimating;

    private void CardBorder_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        Border send = (Border)sender;
        Debug.WriteLine("Card hover ended");
        Debug.WriteLine($"Border scale now is {send.Scale}");
        Debug.WriteLine($"Border width now is {send.Width}");
        Debug.WriteLine($"Border height now is {send.Height}");
        Debug.WriteLine("Card hover started");
        if (_isHovered || _isAnimating) return;
        _isHovered = true;
        StartHoverDelay();

    }

    private void CardBorder_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        Border send = (Border)sender;
        Debug.WriteLine("Card hover ended");
        Debug.WriteLine($"Border scale now is {send.Scale}");
        Debug.WriteLine($"Border width now is {send.Width}");
        Debug.WriteLine($"Border height now is {send.Height}");
        if (!_isHovered) return;
        _isHovered = false;
        CancelHover();
    }
}
