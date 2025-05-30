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
        var vm = IPlatformApplication.Current!.Services.GetService<BaseViewModelWin>()!;
        if (win is null)
        {
            dimmerWin = new DimmerWin(vm);            
        }
        else
        {
            dimmerWin = win;
        }
        dimmerWin.MinimumHeight = 750;
        dimmerWin.MinimumWidth = 900;
        dimmerWin.Height = 850;
        dimmerWin.Width = 1100;


        
        return dimmerWin;
    }
    public DimmerWin? dimmerWin { get; set; }
}

