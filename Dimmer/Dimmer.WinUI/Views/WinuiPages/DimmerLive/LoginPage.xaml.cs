using NavigationEventArgs = Microsoft.UI.Xaml.Navigation.NavigationEventArgs;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages.DimmerLive;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class LoginPage : Page
{
    public LoginViewModelWin ViewModel { get; set; }
    public LoginPage()
    {
        InitializeComponent();
    }
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel = IPlatformApplication.Current!.Services.GetService<LoginViewModelWin>()!;
        this.DataContext = ViewModel; // Set DataContext for binding within DataTemplates
        this.Name = "RootPage"; // Helper for ElementName binding

    }

    private async void LogIn_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoginAsync();
        if (ViewModel.CurrentUserOnline is not null && ViewModel.CurrentUserOnline.IsAuthenticated)
        {
           await ViewModel.NavigateToProfilePageAsync();
        }
    }
}
