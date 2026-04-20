using AndroidX.Lifecycle;

namespace Dimmer.Views.Album;

public partial class AlbumPage : ContentPage
{
	public AlbumPage(BaseViewModelAnd baseViewModel, StatisticsViewModel statsVM,
    LastFMViewModel lastFMVM)

    {
        InitializeComponent();
        MyViewModel = baseViewModel;
        StatsVM = statsVM;
        MylastFMViewModel = lastFMVM;
      


    }
    LastFMViewModel MylastFMViewModel { get; }
    public BaseViewModelAnd MyViewModel { get; }
    public StatisticsViewModel StatsVM { get; }

    private async void LoadLastFMInfo_Clicked(object sender, EventArgs e)
    {
        await MylastFMViewModel.LoadAlbumLastFMDataAsync(MyViewModel.SelectedAlbum);
    }

    protected async override void OnAppearing()
    {
        BindingContext = MyViewModel.SelectedAlbum;
        base.OnAppearing();
        _ = Task.Run(
            async () =>
            {
                await MylastFMViewModel.LoadAlbumLastFMDataAsync(MyViewModel.SelectedAlbum);
                await StatsVM.LoadAlbumStatsAsync(MyViewModel.SelectedAlbum);

            });
    }
}