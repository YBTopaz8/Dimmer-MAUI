namespace Dimmer_MAUI.Views.Mobile;

public partial class SpecificAlbumPage : UraniumContentPage
{
    public SpecificAlbumPage(HomePageVM homePageVM)
    {
        InitializeComponent();
        this.BindingContext = homePageVM;
        HomePageVM = homePageVM;
    }
    public HomePageVM HomePageVM { get; }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        HomePageVM.CurrentPage = PageEnum.SpecificAlbumPage;
    }
}
