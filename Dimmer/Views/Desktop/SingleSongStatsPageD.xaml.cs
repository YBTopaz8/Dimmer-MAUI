namespace Dimmer_MAUI.Views.Desktop;

public partial class SingleSongStatsPageD : ContentPage
{
	public SingleSongStatsPageD(HomePageVM homePageVM)
    {
        InitializeComponent();
        this.BindingContext = homePageVM;
        HomePageVM = homePageVM;
    }
    public HomePageVM HomePageVM { get; }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        HomePageVM.CurrentPage = PageEnum.FullStatsPage;
    }
}