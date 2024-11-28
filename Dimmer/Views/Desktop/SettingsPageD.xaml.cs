using Syncfusion.Maui.Toolkit.Carousel;
using System.Diagnostics;

namespace Dimmer_MAUI.Views.Desktop;

public partial class SettingsPageD : ContentPage
{
	public SettingsPageD(HomePageVM ViewModel)
    {
        InitializeComponent();
        BindingContext = ViewModel;
        this.ViewModel = ViewModel;
    }
    public bool ToLogin { get; }
    public HomePageVM ViewModel { get; }

    bool IsUserInLastFM;
    protected override void OnAppearing()
    {
        base.OnAppearing();
        SongsManagementService.ConnectOnline();

        if (ViewModel.CurrentUser is not null && !string.IsNullOrEmpty(ViewModel.CurrentUser.UserIDOnline))
        {
            LoginPass.Text = ViewModel.CurrentUser.UserPassword;
            LoginBtn_Clicked(null, null); //review this.
        }
        
    }
    private async void ReportIssueBtn_Clicked(object sender, EventArgs e)
    {
        var reportingLink = $"https://github.com/YBTopaz8/Dimmer-MAUI/issues/new";

        await Browser.Default.OpenAsync(reportingLink, BrowserLaunchMode.SystemPreferred);
    }

    private void ShowHidePreferredFoldersExpander_Tapped(object sender, TappedEventArgs e)
    {
        ShowHidePreferredFoldersExpander.IsExpanded = !ShowHidePreferredFoldersExpander.IsExpanded;
    }


    private void LoginSignUpToggle_Click(object sender, EventArgs e)
    {
        LoginUI.IsVisible = !LoginUI.IsVisible;
        SignUpUI.IsVisible = !SignUpUI.IsVisible;
    }

    private async void SignUpBtn_Clicked(object sender, EventArgs e)
    {
        SignUpBtn.IsEnabled = false;
        if (string.IsNullOrWhiteSpace(SignUpUname.Text) ||
            string.IsNullOrWhiteSpace(SignUpPass.Text) ||
            string.IsNullOrWhiteSpace(SignUpEmail.Text))
        {
            await Shell.Current.DisplayAlert("Error", "All fields are required.", "OK");
            return;
        }

        ParseUser user = new ParseUser()
        {
            Username = SignUpUname.Text.Trim(),
            Password = SignUpPass.Text.Trim(),
            Email = SignUpEmail.Text.Trim()
        };

        try
        {
            await user.SignUpAsync();
            await Shell.Current.DisplayAlert("Success", "Account created successfully!", "OK");
            
            // Navigate to a different page or reset fields
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Sign-up failed: {ex.Message}", "OK");
            
        }
    }

    private async void LoginBtn_Clicked(object sender, EventArgs e)
    {
        if (ViewModel.CurrentUserOnline is not null)
        {
            if (ViewModel.CurrentUserOnline.IsAuthenticated)
            {
                return;
            }
        }
        if (string.IsNullOrWhiteSpace(LoginPass.Text))
        {
        }
        //LoginBtn.IsEnabled = false;
        if (string.IsNullOrWhiteSpace(LoginUname.Text) || string.IsNullOrWhiteSpace(LoginPass.Text))
        {   
            await Shell.Current.DisplayAlert("Error", "Username and Password are required.", "OK");
            return;
        }

        try
        {
            var oUser = await ParseClient.Instance.LogInAsync(LoginUname.Text.Trim(), LoginPass.Text.Trim()).ConfigureAwait(false);
            ViewModel.SongsMgtService.CurrentOfflineUser.UserPassword = LoginPass.Text;
            ViewModel.CurrentUserOnline = oUser;
            ViewModel.CurrentUser.IsAuthenticated = true;
            //await Shell.Current.DisplayAlert("Success !", $"Welcome Back ! {oUser.Username}", "OK"); //if you uncomment this, app will crash :)
            // Navigate to a different page or perform post-login actions
            //ViewModel.SongsMgtService.GetUserAccount(oUser);
        }
        catch (Exception ex)
        {
            ViewModel.CurrentUser.IsAuthenticated = false;
            await Shell.Current.DisplayAlert("Error", $"Login failed: {ex.Message}", "OK");

        }
    }

    private void FullSyncBtn_Clicked(object sender, EventArgs e)
    {
        _= ViewModel.FullSync();
    }
    private async void SyncPDaCS_Clicked(object sender, EventArgs e)
    {
        await ViewModel.SongsMgtService.SyncPlayDataAndCompletionData();
    }
}

public enum UserState
{
    LoggedInSuccessfully,
    SignUpSuccessfully,
    SignUpFailed,
    LoginFailed,
    UserAlreadyExists,
    UserDoesNotExist,
    PasswordIncorrect,
    UserNotVerified,
    UserNotLoggedIn,
    UserLoggedOut,
    ActionCancelled

}
