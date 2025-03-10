namespace Dimmer_MAUI.Views.Desktop;

public partial class SettingsPageD : ContentPage
{
	public SettingsPageD(HomePageVM ViewModel)
    {
        InitializeComponent();
        BindingContext = ViewModel;
        this.MyViewModel = ViewModel;
    }
    public bool ToLogin { get; }
    public HomePageVM MyViewModel { get; }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _ = MyViewModel.GetLoggedInDevicesForUser();
        MyViewModel.CurrentPageMainLayout = MainDock;
        MyViewModel.IsSearchBarVisible = false;
    }
    private async void ReportIssueBtn_Clicked(object sender, EventArgs e)
    {
        var reportingLink = $"https://github.com/YBTopaz8/Dimmer-MAUI/issues/new";

        await Browser.Default.OpenAsync(reportingLink, BrowserLaunchMode.SystemPreferred);
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

    private void FullSyncBtn_Clicked(object sender, EventArgs e)
    {
        _= MyViewModel.FullSync();
    }
    private async void SyncPDaCS_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.SongsMgtService.SyncPlayDataAndCompletionData();
    }


    private void SongShellChip_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.Chips.SelectionChangedEventArgs e)
    {
        var selectedTab = SettingsTab.SelectedItem;
        var send = (SfChipGroup)sender;
        var selected = send.SelectedItem as SfChip;
        if (selected is null)
        {
            return;
        }
        _ = int.TryParse(selected.CommandParameter.ToString(), out int selectedStatView);
        GeneralStaticUtilities.RunFireAndForget(SwitchUI(selectedStatView), ex =>
        {
            Debug.WriteLine($"Task error: {ex.Message}");
        });
        return;
    }

    private async Task SwitchUI(int selectedStatView)
    {
        switch (selectedStatView)
        {
            case 0:

                //GeneralStatsView front, rest back
                break;
            case 1:
                break;
            case 2:
                break;
            case 3:
                break;
            case 4:
                break;
            case 5:

                break;
            case 6:

                break;
            default:

                break;
        }

        var viewss = new Dictionary<int, View>
        {
            {0, AlreadyInView},
            {1, FoldersView},
            {2, LoginParseUI},
            {3, SignUpParseUI},
            {4, LogInLastFMUI},
            {5,SocialView }
        };
        if (!viewss.ContainsKey(selectedStatView))
            return;

        await Task.WhenAll
            (viewss.Select(kvp =>
            kvp.Key == selectedStatView
            ? kvp.Value.AnimateFadeInFront()
            : kvp.Value.AnimateFadeOutBack()));
    }

    private void SignLoginUp_Clicked(object sender, EventArgs e)
    {
        var send = (SfChip)sender;
        _ = int.TryParse(send.CommandParameter.ToString(), out int selectedStatView);
        GeneralStaticUtilities.RunFireAndForget(SwitchUI(selectedStatView), ex =>
        {
            Debug.WriteLine($"Task error: {ex.Message}");
        });
    }
    

    private void SfChip_Clicked(object sender, EventArgs e)
    {
        var send = (SfChip)sender;
        MyViewModel.FolderPaths.Remove(send.CommandParameter as string);
    }

    private async void SettingsAction(object sender, EventArgs e)
    {
        var send = (SfChip)sender;
        _ = int.TryParse(send.CommandParameter.ToString(), out int selectedStatView);

        switch (selectedStatView)
        {
            case 0: //Log out
                if(MyViewModel.LogUserOut())
                {
                    GeneralStaticUtilities.RunFireAndForget(SwitchUI(2), ex =>
                    {
                        Debug.WriteLine($"Task error: {ex.Message}");
                    });
                }
                break;
            case 1: //Log in
                if (await MyViewModel.LogInParseOnline(false))
                {
                    GeneralStaticUtilities.RunFireAndForget(SwitchUI(0), ex =>
                    {
                        Debug.WriteLine($"Task error: {ex.Message}");
                    });
                }
                break;
            case 2: //Sign up
                if(await MyViewModel.SignUpUserAsync())
                {
                    GeneralStaticUtilities.RunFireAndForget(SwitchUI(2), ex =>
                    {
                        Debug.WriteLine($"Task error: {ex.Message}");
                    });
                }
                break;
            case 3: //LastFM
                //if(await MyViewModel.LogInToLastFMWebsite(false))
                //{
                //    GeneralStaticUtilities.RunFireAndForget(SwitchUI(0), ex =>
                //    {
                //        Debug.WriteLine($"Task error: {ex.Message}");
                //    });
                //}
                break;
            case 4: //Forgotten password
                if (await MyViewModel.ForgottenPassword())
                {
                    GeneralStaticUtilities.RunFireAndForget(SwitchUI(2), ex =>
                    {
                        Debug.WriteLine($"Task error: {ex.Message}");
                    });

                }
                break;
            case 5: 
                break;
            default:
                break;
        }
    }

    private void UserSelect_Tapped(object sender, TappedEventArgs e)
    {

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
