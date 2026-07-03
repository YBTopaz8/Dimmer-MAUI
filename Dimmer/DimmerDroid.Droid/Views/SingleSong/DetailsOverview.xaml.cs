global using ATL;
global using CommunityToolkit.Maui.Core;
global using DevExpress.Maui.Core;
global using Dimmer.Data.Models.LyricsModels;
global using Font = Microsoft.Maui.Font;


namespace Dimmer.Views.SingleSong;

public partial class DetailsOverview : ContentPage
{


    public DetailsOverview(BaseViewModelAnd baseViewModel, StatisticsViewModel statisticsService, LastFMViewModel lastFMVM)
	{
		InitializeComponent();
		MyViewModel = baseViewModel;
        StatsViewModel= statisticsService;
        LastFMViewModel = lastFMVM;


	}
    public BaseViewModelAnd MyViewModel { get; }
    public StatisticsViewModel StatsViewModel { get; }
    public LastFMViewModel LastFMViewModel { get; }
    public SongModelView ConcernedSong { get; private set; }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        if (MyViewModel.SelectedSong is null) return;
        BindingContext = MyViewModel.SelectedSong;

        StatsSectionPreview.BindingContext = StatsViewModel;

        ConcernedSong = MyViewModel.SelectedSong!;
        _ = Task.Run(() =>
        {

            _ = StatsViewModel.LoadSongStatsAsync(MyViewModel.SelectedSong);
            _ = LastFMViewModel.LoadSelectedSongLastFMData();
        });
    }

    protected override void OnDisappearing()
    {
        MyViewModel.CleanLyricsSearchProps();
        base.OnDisappearing();
    }
      
 

   


    private void StatisticsStackLayout_Loaded(object sender, EventArgs e)
    {
        DXStackLayout dXScroll =(DXStackLayout)sender;
        dXScroll.BindingContext = StatsViewModel;
    }

    private void Label_Loaded(object sender, EventArgs e)
    {

    }

   

   
    private async void AlbumChip_Tap(object sender, HandledEventArgs e)
    {
        var chip = (Chip)sender;
        var album = chip.TapCommandParameter as AlbumModelView;
        MyViewModel.SetSelectedAlbum(album);

        await Shell.Current.GoToAsync(nameof(AlbumPage));
    }

    private async void NavToArtistPage_Clicked(object sender, EventArgs e)
    {
        var btn = (DXButton)sender;
        var artist = btn.CommandParameter as ArtistModelView;
        MyViewModel.SetSelectedArtist(artist);
        ArtistToSongPopup.Close();
        await Shell.Current.GoToAsync(nameof(ArtistPage));
    }

    private async void ArtistNamesChip_Tap(object sender, HandledEventArgs e)
    {
        var chip = ((Chip)sender);
        var chipX = chip.X;
        var chipY = chip.Y;
        if(this.ConcernedSong.ArtistToSong.Count==1)
        {
            MyViewModel.SetSelectedArtist(ConcernedSong.Artist);

            await Shell.Current.GoToAsync(nameof(ArtistPage));
            return;
        }
        await ArtistToSongPopup.ShowAsync(this);
    }

    
   

    private void ActionsRadarChart_SelectionChanged(object sender, DevExpress.Maui.Charts.SelectionChangedEventArgs e)
    {
        
    }

    private void DetailPagesShimmerView_Loaded(object sender, EventArgs e)
    {
        
    }

    private async void EditLyrics_Clicked(object sender, EventArgs e)
    {
        SongLyricsDownloadPopup popup = new SongLyricsDownloadPopup(MyViewModel, MyViewModel.CurrentPlayingSongView);

        await popup.ShowAsync();

    }
}
