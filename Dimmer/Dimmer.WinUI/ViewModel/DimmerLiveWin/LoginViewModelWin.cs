using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dimmer.WinUI.Views.WinuiPages.DimmerLive;

namespace Dimmer.WinUI.ViewModel.DimmerLiveWin;

public partial class LoginViewModelWin : LoginViewModel
{
    public LoginViewModelWin(IAuthenticationService authService, IFilePicker _filePicker, IRealmFactory realmFactory,BaseViewModelWin baseViewModel) : base(authService, _filePicker, realmFactory)
    {
        BaseViewModel = baseViewModel;
    }

    public BaseViewModelWin BaseViewModel { get; }
    [ObservableProperty]
    public partial string LoginCurrentStatus { get; set; }

     internal void NavigateToProfilePage()
    {
        if(CurrentUserOnline is null)return;
        LoginCurrentStatus = IsAuthenticated ? "Logged In 😊" : "Logged Out";
        if (!CurrentUserOnline.IsAuthenticated)
        { 
            BaseViewModel.NavigateToAnyPageOfGivenType(typeof(LoginPage)); 
        }
        else 
        { 
            BaseViewModel.NavigateToAnyPageOfGivenType(typeof (ProfilePage));
        }

    }
    [RelayCommand]
     internal void NavigateToCloudPage()
    {
        if (CurrentUserOnline is null) return;
        if (CurrentUserOnline.IsAuthenticated)
        { 
            BaseViewModel.NavigateToAnyPageOfGivenType(typeof(CloudDataPage)); 
        }
        else 
        { 
        }

    }

    [RelayCommand]
    public async Task InitAsync()
    {
        await InitializeAsync();
        CurrentUser = BaseViewModel.CurrentUserLocal;
    }

    [ObservableProperty]
    public partial bool IsEditingProfile { get; set; }

    [RelayCommand]
    public async Task SaveProfileChangesAsync()
    {
        if (CurrentUserOnline == null||CurrentUserOnline == null) return;
        IsBusy = true;
        try
        {
            CurrentUserOnline.Username = CurrentUser.Username;
           
            await CurrentUserOnline.SaveAsync();

            await Shell.Current.DisplayAlert("Success", "Profile updated.", "OK");
            IsEditingProfile = false;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save profile: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    // --- CHANGE PASSWORD ---
    [RelayCommand]
    public async Task ChangePasswordAsync(string newPassword)
    {
        if (CurrentUser == null) return;
        IsBusy = true;
        try
        {
            // Parse allows the currently logged-in user to simply set the password
            CurrentUserOnline.Password = newPassword;
            await CurrentUserOnline.SaveAsync();
            await Shell.Current.DisplayAlert("Success", "Password changed successfully.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Could not change password: {ex.Message}", "OK");
        }
        finally { IsBusy = false; }
    }

    // --- PREMIUM SUBSCRIPTION ---
    [RelayCommand]
    public async Task SubscribeToPremiumAsync()
    {
        if (CurrentUser == null) return;
        IsBusy = true;
        try
        {
            // 1. In a real app, you would trigger a Payment Gateway SDK here (Stripe/PayPal)
            // 2. Upon success, you call a Cloud Code function to flip the boolean safely

            var parameters = new Dictionary<string, object>();
            // await ParseClient.Instance.CallCloudCodeFunctionAsync<string>("processSubscription", parameters);

            // For this UI demo, we simulate success:
            await Task.Delay(2000);

            CurrentUser.IsPremium = true;
            // await CurrentUser.SaveAsync(); // Usually handled by server

            await Shell.Current.DisplayAlert("Premium", "Welcome to Dimmer Premium!", "Let's Go");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Subscription failed: {ex.Message}";
        }
        finally { IsBusy = false; }
    }
}
