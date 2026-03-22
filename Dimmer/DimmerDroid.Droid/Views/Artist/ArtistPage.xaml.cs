namespace Dimmer.Views.Artist;

public partial class ArtistPage : ContentPage
{
	public ArtistPage(BaseViewModelAnd baseViewModel)

    {
        InitializeComponent();
        MyViewModel = baseViewModel;

        BindingContext = MyViewModel.SelectedArtist;
    }
    public BaseViewModelAnd MyViewModel { get; }

    private async void LoadLastFMInfo_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.LoadArtistLastFMDataAsync(MyViewModel.SelectedArtist);
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        await MyViewModel.LoadArtistLastFMDataAsync(MyViewModel.SelectedArtist);
    }

    private void ExportEvt_Tap(object sender, HandledEventArgs e)
    {
        
    }
}