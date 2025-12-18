// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.CustomViews.WinuiViews;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class VolumeTab : UserControl
{
    public VolumeTab()
    {
        InitializeComponent();
        MyViewModel = (DataContext) as BaseViewModelWin;
    }

    public BaseViewModelWin? MyViewModel { get; }
}
