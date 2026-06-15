using AndroidX.Lifecycle;
using DevExpress.Maui.Core.Internal;

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

        LoginViewModel.WhenPropertyChange(
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
}