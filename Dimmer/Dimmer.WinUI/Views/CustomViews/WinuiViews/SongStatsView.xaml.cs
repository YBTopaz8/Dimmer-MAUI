// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.CustomViews.WinuiViews;

public sealed partial class SongStatsView : UserControl
{
    public SongStatsView()
    {
        InitializeComponent();
    }
    BaseViewModelWin ViewModel => DataContext as BaseViewModelWin ?? throw new InvalidOperationException("DataContext is not a valid BaseViewModelWin.");
}
