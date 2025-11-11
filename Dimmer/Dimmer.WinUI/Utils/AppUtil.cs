using Dimmer.Interfaces;
using Dimmer.WinUI.Views;

using Windows.Graphics;

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
    public Microsoft.Maui.Controls.Window LoadWindow()
    {

       
        if (this.dimmerWin is null)
        {
            this.dimmerWin = new();
        }
        else
        {
            dimmerWin = BaseViewModelWin.MainMAUIWindow;
        }
        dimmerWin.LoadWindowAndPassVM(BaseViewModelWin, this);
        dimmerWin ??= dimmerWin;
        

        if (dimmerWin == null)
        {
            throw new Exception("DimmerWin is null");
        }

        dimmerWin.Activate();
        PlatUtils.ResizeNativeWindow(dimmerWin, new Windows.Graphics.SizeInt32() { Height = 1200, Width=1080});


        return new Microsoft.Maui.Controls.Window();
    }


    public DimmerWin? dimmerWin { get; set; }
    public BaseViewModelWin BaseViewModelWin { get; }

    public enum SongTransitionAnimation
    {
        Fade,
        Slide,
        Scale,
        Spring
    }
}

