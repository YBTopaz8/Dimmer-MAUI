global using ATL;
global using CommunityToolkit.Maui.Core;
global using DevExpress.Maui.Core;
global using Dimmer.Data.Models.LyricsModels;
global using Font = Microsoft.Maui.Font;
using Dimmer.ViewModel.StatsVMs;


namespace Dimmer.Views.SingleSong;

public partial class DetailsOverview : ContentPage
{


    public DetailsOverview(BaseViewModelAnd baseViewModel, SongStatsViewModel statsVM, LastFMViewModel lastFMVM)
	{
		InitializeComponent();
		MyViewModel = baseViewModel;
        StatsViewModel= statsVM;
        LastFMViewModel = lastFMVM;


	}
    public BaseViewModelAnd MyViewModel { get; }
    public SongStatsViewModel StatsViewModel { get; }
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

            //_ = StatsViewModel.LoadSongStatsAsync(MyViewModel.SelectedSong);
            _ = LastFMViewModel.LoadSelectedSongLastFMData();
        });
        StatsViewModel.LoadSong(MyViewModel.SelectedSong.Id);
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

    private void StatsSectionPreview_Loaded(object sender, EventArgs e)
    {
        StatsSectionPreview.BindingContext = StatsViewModel;
    }

    private void ListInsights_Loaded(object sender, EventArgs e)
    {
        StatsViewModel?.WhenPropertyChanged(nameof(StatsViewModel.ListInsights), v => StatsViewModel?.ListInsights)
            .Subscribe(insight =>
            {
                //ListInsights.ItemsSource = insight;
            });
    }

   

    private void ListPerfectPairings_Loaded(object sender, EventArgs e)
    {
        StatsViewModel?.WhenPropertyChanged(nameof(StatsViewModel.ListPerfectPairings), v => StatsViewModel?.ListPerfectPairings)
            .Subscribe(insight =>
            {
                ListPerfectPairings.ItemsSource = insight;
            });
    }

    private void ListMonthlyTrend_Loaded(object sender, EventArgs e)
    {
        StatsViewModel?.WhenPropertyChanged(nameof(StatsViewModel.ListMonthlyTrend), v => StatsViewModel?.ListMonthlyTrend)
            .Subscribe(insight =>
            {
                ListMonthlyTrend.ItemsSource = insight;
            });
    }

    private void ListWeeklyTrend_Loaded(object sender, EventArgs e)
    {
        StatsViewModel?.WhenPropertyChanged(nameof(StatsViewModel.ListWeeklyTrend), v => StatsViewModel?.ListWeeklyTrend)
            .Subscribe(insight =>
            {
                ListWeeklyTrend.ItemsSource = insight;
            });
    }

    private async void AddLyrics_Clicked(object sender, EventArgs e)
    {
        var localLyrics = await MyViewModel.LyricsMetadataService.GetLocalLyricsAsync(MyViewModel.SelectedSong!);
        if(string.IsNullOrEmpty(localLyrics))
        {
            SongLyricsDownloadPopup popup = new SongLyricsDownloadPopup(MyViewModel, MyViewModel.SelectedSong!);

            await popup.ShowAsync();
        }
    }
}
