namespace Dimmer_MAUI.Views.Desktop;

public partial class MainPageD : ContentPage
{
	public MainPageD(HomePageVM homePageVM)
    {
        InitializeComponent();
        HomePageVM = homePageVM;
        this.BindingContext = homePageVM;

        //MediaPlayBackCW.BindingContext = homePageVM;

    }
    public HomePageVM HomePageVM { get; }
}