

using DevExpress.Maui.Core;

namespace Dimmer_MAUI.Views.Mobile;

public partial class SettingsPageM : ContentPage
{
	public SettingsPageM(HomePageVM vm)
    {
        InitializeComponent();
        this.MyViewModel = vm;
        BindingContext = vm;
    }
    public HomePageVM MyViewModel { get; }

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
                
         //await MyViewModel.SetChatRoom(ChatRoomOptions.PersonalRoom);

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
            await ParseClient.Instance.SignUpWithAsync(user);
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
        if (MyViewModel.CurrentUserOnline is not null)
        {
            if (await MyViewModel.CurrentUserOnline.IsAuthenticatedAsync())
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
            var oUser = await ParseClient.Instance.LogInWithAsync(uname,pass);
            MyViewModel.SongsMgtService.UpdateUserLoginDetails(oUser);
            MyViewModel.SongsMgtService.CurrentOfflineUser.UserPassword = LoginPass.Text;
            MyViewModel.CurrentUserOnline = oUser;
            MyViewModel.CurrentUser.IsAuthenticated = true;
            await Shell.Current.DisplayAlert("Success !", $"Welcome Back ! {oUser.Username}", "OK");

            LoginUname.Text = string.Empty;
            LoginPass.Text = string.Empty;
            // Navigate to a different page or perform post-login actions
            //MyViewModel.SongsMgtService.GetUserAccount(oUser);
        }
        catch (Exception ex)
        {
            MyViewModel.CurrentUser.IsAuthenticated = false;
            await Shell.Current.DisplayAlert("Error", $"Login failed: {ex.Message}", "OK");

        }
    }

    private void FullSyncBtn_Clicked(object sender, EventArgs e)
    {
       _=  MyViewModel.FullSync();
    }

    private void DXButton_Clicked(object sender, EventArgs e)
    {

    }

    private async void ScanAllBtn_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.LoadSongsFromFolders();
    }

    private async void PickFolder_Clicked(object sender, EventArgs e)
    {
         await MyViewModel.SelectSongFromFolder();
    }

    private async void SyncPDaCS_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.SongsMgtService.SyncPlayDataAndCompletionData();
    }


    private void DXButton_Clicked_1(object sender, EventArgs e)
    {
        MyViewModel.SongsMgtService.GetUserAccountOnline();
    }
}