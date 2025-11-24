// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.CustomViews.WinuiViews;

public sealed partial class DeviceListTab : UserControl
{
    public DeviceListTab()
    {
        InitializeComponent();
        MyViewModel = (DataContext) as BaseViewModelWin;
    }

    public BaseViewModelWin? MyViewModel { get; }
}
