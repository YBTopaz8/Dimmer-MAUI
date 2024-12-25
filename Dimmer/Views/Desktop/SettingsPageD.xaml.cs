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
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        //SongsManagementService.ConnectOnline();
        await ViewModel.LogInToLastFMWebsite();

        if (string.IsNullOrEmpty(ViewModel.CurrentUser.UserIDOnline))
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
            await ParseClient.Instance.SignUpWithAsync(user);
            await Shell.Current.DisplayAlert("Success", "Account created successfully!", "OK");

            _ = SecureStorage.Default.SetAsync("ParseUsername", SignUpUname.Text);
            _ = SecureStorage.Default.SetAsync("ParsePassWord", SignUpPass.Text);
            _ = SecureStorage.Default.SetAsync("ParseEmail", SignUpEmail.Text);
            // Navigate to a different page or reset fields
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Sign-up failed: {ex.Message}", "OK");
            
        }
    }

    private async void LoginBtn_Clicked(object sender, EventArgs e)
    {
        await ViewModel.LogInToParseServer(LoginUname.Text, LoginPass.Text);
    }

    private void FullSyncBtn_Clicked(object sender, EventArgs e)
    {
        _= ViewModel.FullSync();
    }
    private async void SyncPDaCS_Clicked(object sender, EventArgs e)
    {
        await ViewModel.SongsMgtService.SyncPlayDataAndCompletionData();
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        ViewModel.SetupLiveQueries();
    }

    private async void Button_Clicked_1(object sender, EventArgs e)
    {
        await ViewModel.LogInToLastFMWebsite();
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
