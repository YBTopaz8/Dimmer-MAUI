namespace Dimmer.WinUI.Utils;
public class AppUtil : IAppUtil
{
    public Shell GetShell()
    {
        return new AppShell();
    }
}

public static class ApplicationProps
{
    public static DisplayArea? DisplayArea { get; set; }

    public static void LaunchSecondWindow()
    {
        var vm = IPlatformApplication.Current!.Services.GetService<HomeViewModel>();
        var window = new TestPage(vm);
        window.Activate();
    }
}