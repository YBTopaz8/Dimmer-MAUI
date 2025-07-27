using CommunityToolkit.Mvvm.Input;

using ReactiveUI;
namespace Dimmer.ViewModel;


public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthenticationService _authService;

    [ObservableProperty]
    public partial string Username{ get; set; }

    [ObservableProperty]
    public partial string Password { get; set; }

    [ObservableProperty]
    public partial bool RememberMe { get; set; }
    [ObservableProperty]
    public partial bool IsAuthenticated { get; set; }

    [ObservableProperty]
    public partial bool IsLoginEnabled { get; set; } = true;
    [ObservableProperty]
    public partial bool IsRegisterEnabled { get; set; } = true;
    [ObservableProperty]
    public partial string Email { get; set; }


    [ObservableProperty]
    public partial string ErrorMessage{ get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    [NotifyCanExecuteChangedFor(nameof(ForgotPasswordCommand))]
    public partial bool IsBusy{ get; set; }
    public ParseUser CurrentUser { get; private set; }

    public LoginViewModel(IAuthenticationService authService)
    {
        _authService = authService;

    }

    private bool CanLogin() => !IsBusy && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

    [RelayCommand]
    private async Task LoginAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

        var result = await _authService.LoginAsync(Username, Password);

        if (result.IsSuccess)
        {
            // Navigate to the main part of the app
            // e.g., await Shell.Current.GoToAsync("//MainPage");
        }
        else
        {
            ErrorMessage = result.ErrorMessage;
        }

        IsBusy = false;
    }
    private bool CanRegister() => !IsBusy && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);

    [RelayCommand]
    private async Task RegisterAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

        // You might want to add more validation here (e.g., password complexity, valid email format)

        var result = await _authService.RegisterAsync(Username, Email, Password);

        if (result.IsSuccess)
        {
            // Successful registration, navigate to the main part of the app.
            // await _navigationService.NavigateToMainPageAsync();
            await Shell.Current.DisplayAlert("Welcome!", "Your account has been created.", "OK");
        }
        else
        {
            ErrorMessage = result.ErrorMessage;
        }

        IsBusy = false;
    }

    // --- Forgot Password Logic ---

    private bool CanForgotPassword() => !IsBusy && !string.IsNullOrWhiteSpace(Email);

    [RelayCommand]
    private async Task ForgotPasswordAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

         var result = await _authService.RequestPasswordResetAsync(Email);


        IsBusy = false;
    }

    // --- Logout Logic (This would typically live in a different ViewModel, like a Profile or Settings page) ---

    [RelayCommand]
    private async Task LogoutAsync()
    {
        IsBusy = true;
        await _authService.LogoutAsync();
        // await _navigationService.NavigateToLoginPageAsync();
        IsBusy = false;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        var res = await _authService.InitializeAsync();

        if (res.IsSuccess)
        {
            CurrentUser = ParseClient.Instance.CurrentUser;
        }
    }
}