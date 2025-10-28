using Dimmer.Interfaces.Services.Interfaces;

namespace Dimmer.WinUI.Utils;
public class AppUtil : IAppUtil
{

    public AppUtil(BaseViewModelWin baseViewModelWin)
    {

        BaseVM=baseViewModelWin;
        //this.dimmerWin ??=baseViewModelWin.MainMAUIWindow;

    }

    public Shell GetShell()
    {
        return new AppShell(BaseVM);
    }
    public Window LoadWindow()
    {
        var mauiWindow = new Microsoft.Maui.Controls.Window();
        //this.dimmerWin ??=BaseVM.MainMAUIWindow;
        dimmerWin ??= new DimmerWin(BaseVM, this);

        if (dimmerWin == null)
        {
            throw new Exception("DimmerWin is null");
        }
        dimmerWin.Activate();
        //dimmerWin.MinimumHeight = 750;
        //dimmerWin.MinimumWidth = 900;
        //dimmerWin.Height = 950;
        //dimmerWin.Width = 1100;



        return mauiWindow;
    }
    public DimmerWin dimmerWin { get; set; }
    public BaseViewModelWin BaseVM { get; }
}

