namespace Dimmer.WinUI.Utils;
public class AppUtil : IAppUtil
{
    public Shell GetShell()
    {
        return new AppShell();
    }
    public Window LoadWindow()
    {
        var win = IPlatformApplication.Current!.Services.GetService<DimmerWin>();
        var vm = IPlatformApplication.Current!.Services.GetService<BaseViewModel>()!;
        if (win is null)
        {
            dimmerWin = new DimmerWin(vm);            
        }
        else
        {
            dimmerWin = win;
        }
        return dimmerWin;
    }
    public DimmerWin? dimmerWin { get; set; }
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