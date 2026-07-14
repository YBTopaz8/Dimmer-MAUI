

using Dimmer.ViewModel.StatsVMs;

namespace Dimmer.Views.Artist;

public partial class ArtistPage : ContentPage
{
	public ArtistPage(BaseViewModelAnd baseViewModel , ArtistStatsViewModel statsVM,
    LastFMViewModel lastFMVM)

    {
        InitializeComponent();
        MyViewModel = baseViewModel;
        MylastFMViewModel = lastFMVM;
        StatsVM = statsVM;

    }
    public BaseViewModelAnd MyViewModel { get; }
    
    LastFMViewModel MylastFMViewModel { get; }
    public ArtistStatsViewModel StatsVM { get; }

    private async void LoadLastFMInfo_Clicked(object sender, EventArgs e)
    {
        await MylastFMViewModel.LoadArtistLastFMDataAsync(MyViewModel.SelectedArtist);
    }

    protected async override void OnAppearing()
    {
        BindingContext = MyViewModel.SelectedArtist;
        base.OnAppearing();
        
          
           //await StatsVM.LoadArtistStatsAsync(MyViewModel.SelectedArtist);

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

    private async void MyPage_Loaded(object sender, EventArgs e)
    {

        await Task.Delay(4000);
        await MylastFMViewModel.LoadArtistLastFMDataAsync(MyViewModel.SelectedArtist);
        
    }
}