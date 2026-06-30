using static Realms.ThreadSafeReference;

namespace Dimmer.Views.LastFM;

public partial class LastFMHomePage : ContentPage
{
    public LastFMHomePage(LastFMViewModel viewModel)
    {
        InitializeComponent();
        MyLastFMViewModel = viewModel;
        BindingContext = viewModel;

       
    }
    public LastFMViewModel MyLastFMViewModel { get; set; }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
            _ = MyLastFMViewModel.LoadLastFMSession();
        Debug.WriteLine("Appeared " + DateTime.Now);
    }

    private void ColOfRecentTracks_Loaded(object sender, EventArgs e)
    {

    }

    private async void myPage_Loaded(object sender, EventArgs e)
    {
    }


    private async void DXButton_Clicked(object sender, EventArgs e)
    {
        MyLastFMViewModel.LastFMName = "YBTopaz8";
        await MyLastFMViewModel.LoginToLastfmAsync();
    }

    private void DXScrollView_Loaded(object sender, EventArgs e)
    {
        MyLastFMViewModel.WhenPropertyChanged(
       nameof(MyLastFMViewModel.IsLastfmAuthenticated),
       isBG => (MyLastFMViewModel.IsLastfmAuthenticated))
       .ObserveOn(RxSchedulers.UI)
       .Subscribe(
           async isBg =>
           {
               if (!isBg)
               {
                   LoginBtmSheet.Show();
               }
               else
               {

                   LoginBtmSheet.Close();

                   await MyLastFMViewModel.LoadUserLastFMDataAsync(MyLastFMViewModel.CurrentUserLocal?.LastFMAccountInfo);
               }

           });
    }

    private async void LoginToLastfm_Clicked(object sender, EventArgs e)
    {
        MyLastFMViewModel.LastFMName = "YBTopaz8";
        await MyLastFMViewModel.LoginToLastfmAsync();
    }
}