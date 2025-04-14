using Windows.Graphics;
using WinRT.Interop;
using Window = Microsoft.UI.Xaml.Window;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AlbumWindow : Window
{
    public BaseAlbumViewModel MyViewModel { get; }
    public AlbumWindow(BaseAlbumViewModel viewModel)
    {
        this.InitializeComponent();
        MyViewModel=viewModel;
        // Configure size, position, and topmost settings.
        
        ConfigureWindow();
    }

    private void ConfigureWindow()
    {
        // Obtain the window handle (HWND) for this WinUI window.
        var hWnd = WindowNative.GetWindowHandle(this);

        // Get the windowId and the underlying AppWindow.
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        // *** Set a fixed size ***
        // For example, a width of 800 pixels and height of 600 pixels.
        var fixedSize = new SizeInt32 { Width = 800, Height = 800 };
        appWindow.Resize(fixedSize);

        // *** Center the window on the primary display ***
        // Get the work area of the primary display.
        DisplayArea displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        int centerX = displayArea.WorkArea.X + (displayArea.WorkArea.Width - fixedSize.Width) / 2;
        int centerY = displayArea.WorkArea.Y + (displayArea.WorkArea.Height - fixedSize.Height) / 2;
        appWindow.Move(new PointInt32 { X = centerX, Y = centerY });

        // *** Set the window always on top ***
        appWindow.MoveInZOrderAtTop();
    }

    private void ContentGridView_ItemClick(object sender, Microsoft.UI.Xaml.Controls.ItemClickEventArgs e)
    {

    }

    private void ContentGridView_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {

    }
}
