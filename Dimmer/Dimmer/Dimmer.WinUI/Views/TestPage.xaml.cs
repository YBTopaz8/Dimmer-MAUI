using Microsoft.UI.Xaml;
using Window = Microsoft.UI.Xaml.Window;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class TestPage : Window
{
    public TestPage(HomeViewModel viewModel)
    {
        this.InitializeComponent();
        MyViewModel=viewModel;        
    }

    public HomeViewModel MyViewModel { get; }
    
    private void MyTableView_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        FrameworkElement view = (FrameworkElement)e.OriginalSource;       

        SongModelView? song = (SongModelView)view.DataContext;

        song?.IsCurrentPlayingHighlight = false;
    }

    private void Slider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {

    }
}
