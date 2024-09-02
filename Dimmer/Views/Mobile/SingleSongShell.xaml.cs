namespace Dimmer_MAUI.Views.Mobile;

public partial class SingleSongShell : ContentPage
{
	public SingleSongShell(HomePageVM homePageVM)
	{
		InitializeComponent();
        HomePageVM = homePageVM;
        BindingContext = homePageVM;
        
    }

    
    public HomePageVM HomePageVM { get; }

    
}