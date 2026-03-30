

namespace Dimmer.Views.Artist;

public partial class ArtistPage : ContentPage
{
	public ArtistPage(BaseViewModelAnd baseViewModel , StatisticsViewModel statsVM)

    {
        InitializeComponent();
        MyViewModel = baseViewModel;
        StatsVM = statsVM;

        BindingContext = MyViewModel.SelectedArtist;
    }
    public BaseViewModelAnd MyViewModel { get; }
    public StatisticsViewModel StatsVM { get; }

    private async void LoadLastFMInfo_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.LoadArtistLastFMDataAsync(MyViewModel.SelectedArtist);
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        await MyViewModel.LoadArtistLastFMDataAsync(MyViewModel.SelectedArtist);
       await StatsVM.LoadArtistStatsAsync(MyViewModel.SelectedArtist);
    }

    private void ExportEvt_Tap(object sender, HandledEventArgs e)
    {
        
    }

    private async void CurrentPlayingCoverTapGesture_Tapped(object sender, TappedEventArgs e)
    {
        var song = ((View)sender).BindingContext as SongModelView;
        MyViewModel.SelectedSong = song;
        await Shell.Current.GoToAsync(nameof(DetailsOverview), true);
    }

    private async void MiddleGridSection_Tapped(object sender, TappedEventArgs e)
    {
        var send = (View)sender;
        var song = (SongModelView)send.BindingContext;
        await MyViewModel.PlaySongAsync(song, CurrentPage.HomePage, MyViewModel.SearchResults);
    }

    private void TitleChip_Tap(object sender, HandledEventArgs e)
    {

    }

    private void ArtistChip_Tap(object sender, HandledEventArgs e)
    {

    }

    private void ArtistChip_LongPress(object sender, HandledEventArgs e)
    {

    }

    private void ChartsScrollView_Loaded(object sender, EventArgs e)
    {
        ChartsScrollView.BindingContext = StatsVM;
    }
}