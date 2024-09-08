namespace Dimmer_MAUI.Views.Mobile;

public partial class ArtistsPageM : ContentPage
{
	public ArtistsPageM(HomePageVM homePageVM)
    {
        InitializeComponent();
        HomePageVM = homePageVM;
        this.BindingContext = homePageVM;
        HomePageVM.GetAllArtistsCommand.Execute(null);
    }
    public HomePageVM HomePageVM { get; }
}