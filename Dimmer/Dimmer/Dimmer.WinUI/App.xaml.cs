using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        Debug.WriteLine("Dimmer WinUI :D");
        this.InitializeComponent();
    }

    protected override MauiApp CreateMauiApp()
    {
        var s = IPlatformApplication.Current;
        
        return MauiProgram.CreateMauiApp();
    }


}
public static class AppPlatform
{
    public static Window CreatePlatformWindow(IActivationState? state)
    {
        return new Window(new AppShell());// your platform-specific shell
    }
}