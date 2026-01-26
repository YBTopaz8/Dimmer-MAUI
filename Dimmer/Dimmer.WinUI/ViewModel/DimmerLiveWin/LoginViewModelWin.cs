using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dimmer.WinUI.Views.WinuiPages.DimmerLive;
using Parse;

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

     internal async Task NavigateToProfilePage()
    {
        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            if(ParseClient.Instance is null)
            {
                ServiceRegistration.RegisterParseAndItsClasses();   
            }
            LoginCurrentStatus = IsAuthenticated ? "Logged In 😊" : "Logged Out";
            if (!IsAuthenticated)
            {
                BaseViewModel.NavigateToAnyPageOfGivenType(typeof(LoginPage));
            }
            else
            {
                BaseViewModel.NavigateToAnyPageOfGivenType(typeof(ProfilePage));
            }
        }
        else
        {
            await Shell.Current.DisplayAlert(
                 "No Internet",
                "Please connection your device to the internet and retry",
                "OK");
                
                
        }
        
    }
    
    [RelayCommand]
     internal void NavigateToCloudPage()
    {
        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
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
    }

    [RelayCommand]
    public async Task InitAsync()
    {
        await InitializeAsync();
        CurrentUser = BaseViewModel.CurrentUserLocal;
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

    internal async Task UploadProfilePictureToCloudAsync()
    {
       byte[]? resultByteArray = await BaseViewModel.PickProfilePictureFromFolderAndUploadToCloudAsync();
        if(resultByteArray == null) return;
        await BaseViewModel.SessionMgtVM.UpdateProfilePicture(resultByteArray);
    }
}
