using AndroidX.Lifecycle;
using ATL;
using DevExpress.Maui.Core;
using Dimmer.Data.Models.LyricsModels;
using DynamicData;
using Java.Interop;

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
 

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        BindingContext = MyViewModel.SelectedSong;
        StatisticsStackLayout.BindingContext = StatsViewModel;

        if(MyViewModel.IsSearchingLyrics)
        {
            SongTabView.SelectedItemIndex = 1;
        }

        _ = StatsViewModel.LoadSongStatsAsync(MyViewModel.SelectedSong);
        _ = LastFMViewModel.LoadSelectedSongLastFMData();
    }


          private void SongTitleLabel_SizeChanged(object sender, EventArgs e)
    {
        double startX = TitleLabel.Width;
        double endX = -TitleLabel.Width;

        //now marquee the text
        var animation = new Animation(v => TitleLabel.TranslationX = v, startX, endX);
        animation.Commit(this, "MarqueeAnimation", 16, 10000, Easing.Linear, (v, c) => TitleLabel.TranslationX = startX, () => true);
        
    }

    private void LyricsTabVSL_Loaded(object sender, EventArgs e)
    {
        LyricsTabVSL.BindingContext = MyViewModel;
    }

    private void SongTabView_PropertyChanging(object sender, Microsoft.Maui.Controls.PropertyChangingEventArgs e)
    {
        var propName = e.PropertyName;
        if(propName == nameof(SongTabView.SelectedItemIndex))
        {
            if(SongTabView.SelectedItemIndex==0)
            {
                MyViewModel.ReadySearchViewAndProduceSearchText();
            }
        }
    }

   


    private void StatisticsStackLayout_Loaded(object sender, EventArgs e)
    {
        DXStackLayout dXScroll =(DXStackLayout)sender;
        dXScroll.BindingContext = StatsViewModel;
    }

    private void Label_Loaded(object sender, EventArgs e)
    {

    }

    private async void SearchLyricsBtn_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.SearchLyricsAndLoadLyricsIfFoundAsync();
    }

    private async void ApplyLyrics_Clicked(object sender, EventArgs e)
    {
        var onlineResult = ((DXButton)sender).CommandParameter as LrcLibLyrics;
        if(onlineResult == null)
            return;
        var newSyncLyricsInfo = new LyricsInfo();
        var newUnSyncLyrics = new LyricsInfo();
        string? fetchedLrcData = onlineResult.SyncedLyrics;
        string? plainLyrics = onlineResult.PlainLyrics;
        if (!string.IsNullOrWhiteSpace(fetchedLrcData))
        {
            newSyncLyricsInfo.Parse(fetchedLrcData);
            newUnSyncLyrics.Parse(plainLyrics);
        }
        else
        {
            newUnSyncLyrics.UnsynchronizedLyrics = plainLyrics;
        }

        fetchedLrcData = onlineResult.SyncedLyrics;
        plainLyrics = onlineResult.PlainLyrics;

        // Save the new lyrics back to the file metadata
        bool saved = await MyViewModel.LyricsMetadataService.SaveLyricsToDB(onlineResult.Instrumental, plainLyrics, MyViewModel.SelectedSong.ToSongModel()!, fetchedLrcData, newSyncLyricsInfo);
        if(saved)
        {
            MyViewModel.SelectedSong!.SyncLyrics = onlineResult.SyncedLyrics;
            MyViewModel.SelectedSong!.UnSyncLyrics = onlineResult.PlainLyrics;

            var songInCollection = MyViewModel.SearchResults.First(x=>x.Id == MyViewModel.SelectedSong.Id);
            MyViewModel.SearchResultsHolder.Edit(updater =>
            {
                updater.Replace(songInCollection, MyViewModel.SelectedSong);
            });
        }
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
        await ArtistToSongPopup.ShowAsync(this);
    }
}