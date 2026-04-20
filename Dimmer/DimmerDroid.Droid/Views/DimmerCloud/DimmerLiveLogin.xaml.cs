namespace Dimmer.Views.DimmerCloud;

public partial class DimmerLiveLogin : ContentPage
{
	public DimmerLiveLogin(LoginViewModel loginViewModel)
	{
        LoginViewModel = loginViewModel;
        InitializeComponent();
        BindingContext = loginViewModel;

	}
    LoginViewModel LoginViewModel { get; set; }

}