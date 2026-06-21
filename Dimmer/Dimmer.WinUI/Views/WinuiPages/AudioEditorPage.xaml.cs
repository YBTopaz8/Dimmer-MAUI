using NavigationEventArgs = Microsoft.UI.Xaml.Navigation.NavigationEventArgs;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AudioEditorPage : Page
{
    public EditorViewModel ViewModel { get; private set; }

    public AudioEditorPage()
    {
        this.InitializeComponent();

    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Ensure ViewModel is resolved (if using DI)
        if (ViewModel == null)
        {
           
            ViewModel = IPlatformApplication.Current!.Services.GetService<EditorViewModel>()!;
        }

        // Handle the passed data
        if (e.Parameter is SongModelView song)
        {
            ViewModel.LoadSong(song);
        }
    }

    private void MainGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var props = e.GetCurrentPoint(this).Properties;
        if (props != null)
        {
            if(props.PointerUpdateKind == Microsoft.UI.Input.PointerUpdateKind.XButton1Pressed)
            {
                if (Frame.CanGoBack)
                    Frame.GoBack();
            }
        }
    }
}