namespace Dimmer.Views.DimmerCloud;

public partial class DimmerHomeCenter : ContentPage
{
	public DimmerHomeCenter(LoginViewModel loginViewModel)

    {
        LoginViewModel = loginViewModel;
        InitializeComponent();
        BindingContext = loginViewModel;



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
                  await LoginPopup.ShowAsync();
                  LoginPopup.CloseOnScrimTap = false;
              }
              else
              {

                  LoginPopup.Close();

              }

          });
    }
}