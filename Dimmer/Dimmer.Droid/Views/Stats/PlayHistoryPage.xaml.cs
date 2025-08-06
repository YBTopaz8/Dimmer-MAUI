namespace Dimmer.Views.Stats;

public partial class PlayHistoryPage : ContentPage
{
    public PlayHistoryPage(BaseViewModelAnd vm)
    {
        InitializeComponent();
        MyViewModel=vm;

        //MyViewModel!.LoadPageViewModel();
        BindingContext = vm;
        //NavChips.ItemsSource = new List<string> { "Home", "Artists", "Albums", "Genres", "Settings"};
        //NavChipss.ItemsSource = new List<string> { "Home", "Artists", "Albums", "Genres", "Settings" };
    }

    public BaseViewModelAnd MyViewModel { get; internal set; }
    protected override void OnAppearing()
    {
        base.OnAppearing();
    }
}