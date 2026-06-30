using AndroidX.Lifecycle;
using DevExpress.Maui.Core.Internal;
using Dimmer.DimmerLive.Models;
using Google.Android.Material.Dialog;

namespace Dimmer.Views.DimmerCloud;

public partial class DimmerHomeCenter : ContentPage
{
	public DimmerHomeCenter(LoginViewModel loginViewModel, SessionManagementViewModel sessVM)

    {
        LoginViewModel = loginViewModel;
        InitializeComponent();
        BindingContext = loginViewModel;

        MyViewModel = sessVM;

    }
    LoginViewModel LoginViewModel { get; set; }

    private void MainGrid_Loaded(object sender, EventArgs e)
    {

        LoginViewModel.WhenPropertyChanged(
      nameof(LoginViewModel.IsAuthenticated),
      isBG => (LoginViewModel.IsAuthenticated))
      .ObserveOn(RxSchedulers.UI)
      .Subscribe(
          async isBg =>
          {
              if (!isBg)
              {
                  LoginBottomSheet.Show();
                  
              }
              else
              {

                  LoginBottomSheet.Close();
                  BindingContext = MyViewModel;
                  await MyViewModel.RegisterCurrentDeviceAsync();
              }

          });
    }
    SessionManagementViewModel MyViewModel;
    private async void CancelLoginChip_Tap(object sender, HandledEventArgs e)
    {
        await Shell.Current.GoToAsync("//HomePage");

    }

    private void NameViewDevice_Clicked(object sender, EventArgs e)
    {
        DXButton btn = (DXButton)sender;
        var dev = btn.BindingContext as Dimmer.DimmerLive.Models.UserDeviceSession
        ;

        if (dev is null) return;

        MyViewModel.SelectedDevice = dev;
        


    }

    private void PerformOnlineBackup_Clicked(object sender, EventArgs e)
    {

    }

    private async void EditDevice_Clicked(object sender, EventArgs e)
    {
        DXButton send = (DXButton)sender;
        UserDeviceSession? dev = send.CommandParameter as UserDeviceSession;
        if (dev is null) return;

        var result = await Shell.Current.DisplayPromptAsync("Edit Device Name", "Enter new device name", "OK", "Cancel", dev.DeviceName, 20, Keyboard.Text, dev.DeviceName);
            
        if (!string.IsNullOrWhiteSpace(result))
        {
            dev.DeviceName = result;
            await MyViewModel.UpdateDeviceNameAsync(dev);
        }

    }
}