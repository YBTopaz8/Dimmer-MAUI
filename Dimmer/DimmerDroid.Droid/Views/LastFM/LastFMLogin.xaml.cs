namespace Dimmer.Views.LastFM;

public partial class LastFMLogin : ContentPage
{
	public LastFMLogin(LastFMViewModel viewModel)
	{
		InitializeComponent();
		MyLastFMViewModel = viewModel;
        BindingContext = viewModel;


        MyLastFMViewModel.WhenPropertyChange(
         nameof(MyLastFMViewModel.IsLastfmAuthenticated),
         isBG => (MyLastFMViewModel.IsLastfmAuthenticated))
         .ObserveOn(RxSchedulers.UI)
         .Subscribe(
             async isBg =>
             {
                 await Shell.Current.GoToAsync(nameof(LastFMHomePage));

             });

    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = MyLastFMViewModel.LoadLastFMSession();
    }
    public LastFMViewModel MyLastFMViewModel { get; set; }

    private async void DXButton_Clicked(object sender, EventArgs e)
    {
        MyLastFMViewModel.LastFMName = "YBTopaz8";
        await MyLastFMViewModel.LoginToLastfmAsync() ;
    }
}