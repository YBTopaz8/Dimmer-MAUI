using Parse;

namespace Dimmer.WinUI.ViewModel.DimmerLiveWin;

public partial class LoginViewModelWin : LoginViewModel
{
    public LoginViewModelWin(IAuthenticationService authService, IFilePicker _filePicker, IRealmFactory realmFactory,BaseViewModelWin baseViewModel, SessionManagementViewModel sessVM) : base(authService, _filePicker, realmFactory)
    {
        BaseViewModel = baseViewModel;
        SessionMgtVM = sessVM;
    }

    public BaseViewModelWin BaseViewModel { get; }
    public SessionManagementViewModel SessionMgtVM { get; }
    [ObservableProperty]
    public partial string LoginCurrentStatus { get; set; }

     internal async Task NavigateToProfilePageAsync()
    {
        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            if(ParseClient.Instance is null)
            {
                ServiceRegistration.RegisterParseAndItsClasses();   
            }
            await InitializeAsync();
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
            await Shell.Current.DisplayAlertAsync(
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
                Dictionary<string, object> param = new Dictionary<string, object>();
                param.Add("loginVM", this);
                BaseViewModel.NavigateToAnyPageOfGivenType(typeof(CloudDataPage),param);
            }
            else
            {
            }
        }
    }
    
    [RelayCommand]
     internal void NavigateToSocialPage()
    {
        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {

            if (CurrentUserOnline is null) return;
            if (CurrentUserOnline.IsAuthenticated)
            {
               

                BaseViewModel.NavigateToAnyPageOfGivenType(typeof(SocialPage));
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

            await Shell.Current.DisplayAlertAsync("Premium", "Welcome to Dimmer Premium!", "Let's Go");
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
