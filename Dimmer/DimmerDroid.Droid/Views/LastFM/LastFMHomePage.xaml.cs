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
        if(!MyLastFMViewModel.IsLastfmAuthenticated)
        {
            await Shell.Current.GoToAsync(nameof(LastFMLogin));

            
            //ici, la ou tu veux ajouter la photo,
            //tu t'arrange a faire photoAppartement.url = filePath; 
            
        }
        Debug.WriteLine("Appeared " + DateTime.Now);
    }

    private void ColOfRecentTracks_Loaded(object sender, EventArgs e)
    {
        
    }

    private async void myPage_Loaded(object sender, EventArgs e)
    {
        await MyLastFMViewModel.LoadUserLastFMDataAsync(MyLastFMViewModel.CurrentUserLocal?.LastFMAccountInfo);
    }
}