using Dimmer.Interfaces.Services.Interfaces;

namespace Dimmer.WinUI.Utils;
public class AppUtil : IAppUtil
{

    public AppUtil(BaseViewModelWin baseViewModelWin)
    {

        BaseViewModelWin=baseViewModelWin;
        this.dimmerWin ??=baseViewModelWin.MainMAUIWindow;

    }

    public Shell GetShell()
    {
        return new AppShell(BaseViewModelWin);
    }
    public Window LoadWindow()
    {
        this.dimmerWin ??=BaseViewModelWin.MainMAUIWindow;
        dimmerWin ??= new DimmerWin(BaseViewModelWin, this);
        

        if (dimmerWin == null)
        {
            throw new Exception("DimmerWin is null");
        }
        dimmerWin.MinimumWidth = 450;
        dimmerWin.MaximumWidth = 500;
        dimmerWin.Width = 450;



        return dimmerWin;
    }
    public DimmerWin? dimmerWin { get; set; }
    public BaseViewModelWin BaseViewModelWin { get; }
}

