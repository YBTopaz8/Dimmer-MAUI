namespace Dimmer_MAUI.Views.Mobile;

public partial class SearchPageM : ContentPage
{
	public SearchPageM(HomePageVM homePageVM)
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
        HomePageVM.ShowGeneralTopTenSongsCommand.Execute(null);
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        var send = (FlexLayout)sender;
        var song = send.BindingContext as SongsModelView;

    }
}