using Window = Microsoft.UI.Xaml.Window;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SettingsWindow : Window
{
    public SettingsWindow(BaseViewModel viewModel)
    {
        this.InitializeComponent();
        MyViewModel=viewModel;
        
    }

    public BaseViewModel MyViewModel { get; }
}
