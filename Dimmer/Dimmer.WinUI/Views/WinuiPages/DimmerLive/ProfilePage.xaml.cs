using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Dimmer.WinUI.ViewModel.DimmerLiveWin;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;

using NavigationEventArgs = Microsoft.UI.Xaml.Navigation.NavigationEventArgs;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages.DimmerLive;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ProfilePage : Page
{
    public LoginViewModelWin ViewModel { get; set; }

    public ProfilePage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        // Ensure you retrieve the singleton instance that has the logged-in user
        ViewModel = IPlatformApplication.Current!.Services.GetService<LoginViewModelWin>()!;
        if(ViewModel.CurrentUserOnline is null)
        {
            ViewModel.BaseViewModel.NavigateToAnyPageOfGivenType(typeof(LoginPage));
            return;
        }


        this.DataContext = ViewModel;
    }

    // Commands handled in Code-Behind for specific UI actions (like Share)
    // In a strict MVVM, these would be in the VM, but DataTransferManager is UI-specific.
    private void ShareProfile(object sender, RoutedEventArgs e)
    {
        // 1. Get the Main Window Handle
        var hwnd = PlatUtils.DimmerHandle;

        // 2. Get DataTransferManager for this window
        var dataTransferManager = DataTransferManagerInterop.GetForWindow(hwnd);

        // 3. Reset and Attach Event
        dataTransferManager.DataRequested -= DataTransferManager_DataRequested;
        dataTransferManager.DataRequested += DataTransferManager_DataRequested;

        // 4. Show UI
        DataTransferManagerInterop.ShowShareUIForWindow(hwnd);
    }
    private void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
    {
        var request = args.Request;
        var user = ViewModel.CurrentUser;
        if (user != null)
        {
            request.Data.Properties.Title = $"Dimmer Profile: {user.Username}";
            request.Data.SetText($"Add me on Dimmer! Username: {user.Username}");
        }
    }

    private async void ShowEditProfileDialog(object sender, RoutedEventArgs e)
    {
        // Create a separate layout for the dialog
        var stack = new StackPanel { Spacing = 12, Width = 300 };

        var txtBio = new TextBox { Header = "Bio", Text = ViewModel.CurrentUser.UserBio, AcceptsReturn = true, Height = 80 };
        var txtTheme = new TextBox { Header = "Theme Preference", Text = ViewModel.CurrentUser.UserTheme };
        var txtCountry = new TextBox { Header = "Country", Text = ViewModel.CurrentUser.UserCountry };

        stack.Children.Add(txtBio);
        stack.Children.Add(txtTheme);
        stack.Children.Add(txtCountry);

        var dialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Title = "Edit Profile",
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = stack
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            // Update Model
            ViewModel.CurrentUser.UserBio = txtBio.Text;
            ViewModel.CurrentUser.UserTheme = txtTheme.Text;
            ViewModel.CurrentUser.UserCountry = txtCountry.Text;

            // Trigger Save Command
            await ViewModel.SaveProfileChangesCommand.ExecuteAsync(null);
        }
    }

    // --- HANDLER: Change Password ---
    private async void ChangePasswordBtn_Click(object sender, RoutedEventArgs e)
    {
        string newPass = PbNewPass.Password;
        string confirmPass = PbConfirmPass.Password;

        if (string.IsNullOrEmpty(newPass))
        {
            ShowError("Password cannot be empty.");
            return;
        }

        if (newPass != confirmPass)
        {
            ShowError("Passwords do not match.");
            return;
        }

        // Call ViewModel
        await ViewModel.ChangePasswordCommand.ExecuteAsync(newPass);

        // Clear boxes
        PbNewPass.Password = "";
        PbConfirmPass.Password = "";
    }

    private async void ShowError(string msg)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Title = "Error",
            Content = msg,
            CloseButtonText = "OK"
        };
        await dialog.ShowAsync();
    }

    private void PPGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        editPPBtn.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        ProfilePic.OpacityTransition = new ScalarTransition() { Duration = TimeSpan.FromMilliseconds(200) };
        ProfilePic.Opacity = 0.1;
    }

    private void PPGrid_PointerExited(object sender, PointerRoutedEventArgs e)
    {

        editPPBtn.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

        ProfilePic.OpacityTransition = new ScalarTransition() { Duration = TimeSpan.FromMilliseconds(200) };
        ProfilePic.Opacity = 1;
    }

   



    private async void PPGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var props = e.GetCurrentPoint((UIElement)sender).Properties;
        if(props.IsLeftButtonPressed)
            await ViewModel.UploadProfilePictureToCloudAsync();
    }
}

// --- HELPERS ---

// A simple reusable control for the Device Grid (Optional, reduces XAML clutter)
public partial class InfoLabel : UserControl
{
    public string Title { get; set; }
    public string Value { get; set; }

    public InfoLabel()
    {
        var sp = new StackPanel();
        var tTitle = new TextBlock { Opacity = 0.6, FontSize = 12 };
        var tValue = new TextBlock { FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };

        // Simple manual bindings for this lightweight helper
        this.Loaded += (s, e) => {
            tTitle.Text = Title;
            tValue.Text = Value;
        };

        sp.Children.Add(tTitle);
        sp.Children.Add(tValue);
        this.Content = sp;
    }
}

// Date Converter
public class DateTimeOffsetToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateTimeOffset dto)
        {
            return dto.ToString("MMMM dd, yyyy");
        }
        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}