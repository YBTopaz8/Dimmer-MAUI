using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IntelliJ.Lang.Annotations;

namespace Dimmer.ViewModels;

public partial class LoginViewModelAnd  : LoginViewModel
{
    public LoginViewModelAnd(IAuthenticationService authService, IFilePicker _filePicker, IRealmFactory realmFactory, BaseViewModelAnd baseViewModel) : base(authService, _filePicker, realmFactory)
{
    BaseViewModel = baseViewModel;
}



    private readonly BehaviorSubject<SongModelView?> _currentSong = new(null);


    public IObservable<SongModelView?> CurrentSongChanged => _currentSong.AsObservable();

    public void SetCurrentSong(SongModelView? song)
    {
        _currentSong.OnNext(song);
    }

    


    public BaseViewModelAnd BaseViewModel { get; }
[ObservableProperty]
public partial string LoginCurrentStatus { get; set; }

internal void NavigateToProfilePage(Fragment callerFrag, Fragment destinationFrag, string tag)
{
    if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
    {

        LoginCurrentStatus = IsAuthenticated ? "Logged In 😊" : "Logged Out";

        if (!IsAuthenticated)
        {
            BaseViewModel.NavigateToAnyPageOfGivenType(callerFrag,destinationFrag,tag);
        }
        else
        {
            BaseViewModel.NavigateToAnyPageOfGivenType(callerFrag, destinationFrag, tag);
        }
    }
}

public void NavigateToCloudPage(Fragment callerFrag, Fragment destinationFrag, string tag)
{
    if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
    {

        if (CurrentUserOnline is null) return;

        if (CurrentUserOnline.IsAuthenticated)
        {
            BaseViewModel.NavigateToAnyPageOfGivenType(callerFrag, destinationFrag, tag);
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
}

