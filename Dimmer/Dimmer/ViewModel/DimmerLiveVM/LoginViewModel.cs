﻿using CommunityToolkit.Mvvm.Input;

using Dimmer.DimmerLive.Interfaces.Implementations;

using ReactiveUI;
namespace Dimmer.ViewModel;


public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthenticationService _authService;
    private readonly IFilePicker filePicker;
    private readonly IRealmFactory realmFactory;

    [ObservableProperty]
    public partial string Username{ get; set; }

    [ObservableProperty]
    public partial string Password { get; set; }

    [ObservableProperty]
    public partial bool RememberMe { get; set; }
    public static bool IsAuthenticated => string.IsNullOrEmpty( ParseClient.Instance.CurrentUser?.SessionToken);

    [ObservableProperty]
    public partial bool IsLoginEnabled { get; set; } = true;
    [ObservableProperty]
    public partial bool IsRegisterEnabled { get; set; } = true;
    [ObservableProperty]
    public partial string? Email { get; set; }


    [ObservableProperty]
    public partial string? ErrorMessage{ get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    [NotifyCanExecuteChangedFor(nameof(ForgotPasswordCommand))]
    public partial bool IsBusy{ get; set; }


    [ObservableProperty]
    public partial UserModelOnline? CurrentUser { get;  set; }
    [ObservableProperty]
    public partial int SelectedIndex { get;  set; }

    public LoginViewModel(IAuthenticationService authService, IFilePicker _filePicker, IRealmFactory realmFactory)
    {
        _authService = authService;
        this.filePicker=_filePicker;
        this.realmFactory=realmFactory;
    }

    public bool CanLogin()
    {
        return !IsBusy && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

      
        var result = await _authService.LoginAsync(Username, Password);

        if (result.IsSuccess)
        {
            await InitializeAsync();
            SelectedIndex= 1;
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
        await ParseClient.Instance.RequestPasswordResetAsync(Email);


        IsBusy = false;
    }



    // --- Logout Logic (This would typically live in a different ViewModel, like a Profile or Settings page) ---

    [RelayCommand]
    private async Task LogoutAsync()
    {
        IsBusy = true;
        await _authService.LogoutAsync();
        CurrentUser=null;
        // await _navigationService.NavigateToLoginPageAsync();
        IsBusy = false;
    }

    [RelayCommand]
    public async Task<bool> InitializeAsync()
    {

        var res = await _authService.InitializeAsync();

        if (res)
        {
            var qr = new ParseQuery<UserModelOnline>(ParseClient.Instance)
                .WhereEqualTo("objectId", ParseClient.Instance.CurrentUser.ObjectId);

            var usr = await qr.FirstOrDefaultAsync();
            if (usr is null)
            {

                UserModelOnline newUsr = new UserModelOnline(ParseClient.Instance.CurrentUser);

                await newUsr.SaveAsync();
                CurrentUser=newUsr;
                return true;
            }
            CurrentUser = usr;
            return true;


        }
        else
        {
            /*
            try
            {
                // This is the correct method.
                ParseUser anonymousUser = ParseClient.Instance.CreateObject<UserModelOnline>();

                // You can pre-populate data for this new anonymous user if you want
                anonymousUser["displayName"] = $"Listener_{DeviceInfo.Platform.ToString() } {DeviceInfo.Manufacturer}";
                anonymousUser["isPremium"] = false;
                anonymousUser["referralCount"] = 0;
                await anonymousUser.SaveAsync();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating anonymous user: {ex.Message}");
            }*/
            return false;
        }
    }
    [RelayCommand]
    public async Task PickImageFromDevice()
    {
        var fileResult = await filePicker.PickAsync(new PickOptions
        {
            PickerTitle = "Please select an image",
            FileTypes = FilePickerFileType.Images
        });
        if (fileResult != null)
        { 
            var originalExtension = Path.GetExtension(fileResult.FileName); 

            var sanitizedFileName = $"{Guid.NewGuid()}{originalExtension}";
            
            await using var stream = await fileResult.OpenReadAsync();


            
            var result = await UpdateProfileImageAsync(stream, sanitizedFileName);
            if (result.IsSuccess)
            {
                await Shell.Current.DisplayAlert("Success", "Profile image updated successfully.", "OK");
            
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", result.ErrorMessage, "OK");

                // Handle failure, e.g., show an error message
            }
        }
    }



    public async Task<AuthResult> UpdateProfileImageAsync(Stream imageStream, string fileName)
    {
        
        if (CurrentUser == null)
        {
            return AuthResult.Failure("User is not logged in.");
        }

        try
        {
            // 1. Create a ParseFile from the stream.
            //    The SDK will handle reading the stream and uploading the bytes.
            var imageFile = new ParseFile(fileName, imageStream);

            // 2. Save the file to the Parse Server. This uploads the data.
            //    A CancellationToken can be passed here to handle cancellation.
            await imageFile.SaveAsync(ParseClient.Instance);


            // 3. Associate the uploaded file with the user.
            //    We'll assume your UserModelOnline has a 'profileImage' property of type ParseFile.
            CurrentUser.ProfileImageFile = imageFile; // You'll need to add this property to your UserModelOnline class.

            // 4. Save the user object to persist the link to the new file.
            await CurrentUser.SaveAsync();


            // 5. Update the local user representation.
            //var userModelView = await _authService.SyncUser(ParseClient.Instance.CurrentUser);

            // 6. Return a success result with the new image URL.
            return AuthResult.Success();
        }
        catch (Exception ex)
        {
            return AuthResult.Failure("Failed to upload profile image. "+ex.Message);
        }
    }
    [ObservableProperty]
    public partial bool IsRegisterMode { get; set; } = false;

    public string ToggleText => IsRegisterMode ? "Already have an account? " : "Don't have an account? ";
    public string ToggleLinkText => IsRegisterMode ? "Login" : "Sign Up";
    [RelayCommand]
    private void ToggleMode()
    {
        IsRegisterMode = !IsRegisterMode;
        ErrorMessage = string.Empty; // Clear errors when toggling
        OnPropertyChanged(nameof(ToggleText));
        OnPropertyChanged(nameof(ToggleLinkText));
    }
}