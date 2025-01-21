namespace Dimmer_MAUI.Views.Mobile.FirstSteps;

public partial class FirstStepPage : ContentPage
{
    public HomePageVM MyViewModel { get; }
    public FirstStepPage(HomePageVM homePageVM)
    {
            InitializeComponent();
            this.MyViewModel = homePageVM;
            BindingContext = homePageVM;
            Shell.SetNavBarIsVisible(this, true);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
#if ANDROID
       await MyViewModel.GrantPermissionsAndroid();
#endif
    }

    protected override bool OnBackButtonPressed()
    {
        return false;
        
    }
}