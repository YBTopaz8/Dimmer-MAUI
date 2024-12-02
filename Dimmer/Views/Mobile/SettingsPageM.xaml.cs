namespace Dimmer_MAUI.Views.Mobile;

public partial class SettingsPageM : ContentPage
{
	public SettingsPageM(HomePageVM vm)
    {
        InitializeComponent();
        this.ViewModel = vm;
        BindingContext = vm;
    }
    public HomePageVM ViewModel { get; }

    private async void ReportIssueBtn_Clicked(object sender, EventArgs e)
    {
        var reportingLink = $"https://github.com/YBTopaz8/Dimmer-MAUI/issues/new";

        await Browser.Default.OpenAsync(reportingLink, BrowserLaunchMode.SystemPreferred);
    }

    private void LoginSignUpToggle_Click(object sender, EventArgs e)
    {
        LoginUI.IsVisible = !LoginUI.IsVisible;
        SignUpUI.IsVisible = !SignUpUI.IsVisible;
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        SongsManagementService.ConnectOnline();
        //LoginBtn_Clicked(null, null); //review this.
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
            await ParseClient.Instance.SignUpAsync(user);
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
        //LoginBtn.IsEnabled = false;
        if (string.IsNullOrWhiteSpace(LoginUname.Text) || string.IsNullOrWhiteSpace(LoginPass.Text))
        {
            await Shell.Current.DisplayAlert("Error", "Username and Password are required.", "OK");
            return;
        }

        try
        {
            var uname= LoginUname.Text.Trim();
            var pass = LoginPass.Text.Trim();
            var oUser = await ParseClient.Instance.LogInAsync(uname,pass);
            ViewModel.SongsMgtService.CurrentOfflineUser.UserPassword = LoginPass.Text;
            ViewModel.CurrentUserOnline = oUser;
            ViewModel.CurrentUser.IsAuthenticated = true;
            await Shell.Current.DisplayAlert("Success !", $"Welcome Back ! {oUser.Username}", "OK");
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
       _=  ViewModel.FullSync();
    }

    private void DXButton_Clicked(object sender, EventArgs e)
    {

    }

    private async void ScanAllBtn_Clicked(object sender, EventArgs e)
    {
        await ViewModel.LoadSongsFromFolders();
    }

    private async void PickFolder_Clicked(object sender, EventArgs e)
    {
         await ViewModel.SelectSongFromFolder();
    }

    private async void SyncPDaCS_Clicked(object sender, EventArgs e)
    {
        await ViewModel.SongsMgtService.SyncPlayDataAndCompletionData();
    }
}