namespace Dimmer_MAUI.Views.Desktop;

public partial class SingleSongStatsPageD : ContentPage
{
	public SingleSongStatsPageD(HomePageVM homePageVM)
    {
        InitializeComponent();
        this.BindingContext = homePageVM;
        MyViewModel = homePageVM;
    }
    public HomePageVM MyViewModel { get; }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (MyViewModel.TemporarilyPickedSong is null)
        {
            return;
        }
        MyViewModel.CurrentPage = PageEnum.FullStatsPage;
    }
}