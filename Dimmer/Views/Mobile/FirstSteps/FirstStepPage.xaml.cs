namespace Dimmer_MAUI.Views.Mobile.FirstSteps;

public partial class FirstStepPage : ContentPage
{
    public HomePageVM HomePageVM { get; }
    public FirstStepPage(HomePageVM homePageVM)
    {
            InitializeComponent();
            this.HomePageVM = homePageVM;
            BindingContext = homePageVM;
            Shell.SetNavBarIsVisible(this, true);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
#if ANDROID
       await HomePageVM.GrantPermissionsAndroid();
#endif
    }

    protected override bool OnBackButtonPressed()
    {
        return false;
        
    }
}