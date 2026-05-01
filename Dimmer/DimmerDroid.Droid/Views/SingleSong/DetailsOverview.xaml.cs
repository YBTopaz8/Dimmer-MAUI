global using ATL;
global using CommunityToolkit.Maui.Alerts;
global using CommunityToolkit.Maui.Core;
global using DevExpress.Maui.Core;
global using Dimmer.Data.Models.LyricsModels;
global using Font = Microsoft.Maui.Font;
global using Toast = CommunityToolkit.Maui.Alerts.Toast;

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
        StatisticsStackLayout.BindingContext = StatsViewModel;
        MyViewModel.AutoFillSearchFields();
        ConcernedSong = MyViewModel.SelectedSong;

        _ = StatsViewModel.LoadSongStatsAsync(MyViewModel.SelectedSong);
        _ = LastFMViewModel.LoadSelectedSongLastFMData();
    }

    protected override void OnDisappearing()
    {
        MyViewModel.CleanLyricsSearchProps();
        base.OnDisappearing();
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
                updater.AddOrUpdate(MyViewModel.SelectedSong, MyViewModel.SelectedSong.Id.ToString());

            });

           

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            var snackbarOptions = new SnackbarOptions
            {
                BackgroundColor = Colors.Black,
                TextColor = Colors.White,
                ActionButtonTextColor = Colors.SlateBlue,
                CornerRadius = new CornerRadius(10),
                Font = Font.SystemFontOfSize(14),
                ActionButtonFont = Font.SystemFontOfSize(14),
                CharacterSpacing = 0.5
            };

            string text = "Song Lyrics Updated";
            string actionButtonText = "Dismiss";
            TimeSpan duration = TimeSpan.FromSeconds(2);

            var snackbar = Snackbar.Make(text, null, actionButtonText, duration, snackbarOptions);



            await snackbar.Show(cancellationTokenSource.Token);


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

    private async void QuickActionChip_Tap(object sender, HandledEventArgs e)
    {
Chip tappedChip = (Chip)sender;
        var tapParam = tappedChip.TapCommandParameter as string;
        var longPressParam = tappedChip.LongPressCommandParameter as string;
        if (tapParam is null) return;
        if (longPressParam is null) return;


        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

       

        string text = string.Empty;


        TimeSpan duration = TimeSpan.FromSeconds(2);
        
        


        var artistNames = ConcernedSong.OtherArtistsName;
        var albumName = ConcernedSong.AlbumName;
        var titleName = ConcernedSong.Title;
            switch (longPressParam)
            {
                case "copy":
                if (tapParam.Equals("artist"))
                {
                    MyViewModel.LyricsArtistNameSearch = artistNames;
                    text = "Artist Copied to Search Box";
                    
                }
                if (tapParam.Equals("album"))
                {
                    MyViewModel.LyricsAlbumNameSearch = albumName;
                    text = "Album Copied to Search Box";

                }
                if (tapParam.Equals("title"))
                {
                    MyViewModel.LyricsTrackNameSearch = titleName;
                    text = "Title Copied to Search Box";

                }
                    
                break;

                case "paste":
                    var anyAvailText = await Clipboard.Default.GetTextAsync();
                if (tapParam.Equals("artist"))
                {
                    if (string.IsNullOrEmpty(anyAvailText))
                    {
                        text = "No Text on Clipboard";
                    }
                    else
                    {
                        MyViewModel.LyricsArtistNameSearch = anyAvailText;
                        text = "Copied to clipboard pasted to Artist Name Search Box";
                    }
                }
                else if (tapParam.Equals("album"))
                {
                    if (string.IsNullOrEmpty(anyAvailText))
                    {
                        text = "No Text on Clipboard";
                    }
                    else
                    {
                        MyViewModel.LyricsAlbumNameSearch = anyAvailText;
                        text = "Copied to clipboard pasted to Album Name Search Box";
                    }
                }
                else if (tapParam.Equals("title"))
                {
                    if (string.IsNullOrEmpty(anyAvailText))
                    {
                        text = "No Text on Clipboard";
                    }
                    else
                    {
                        MyViewModel.LyricsTrackNameSearch = anyAvailText;
                        text = "Copied to clipboard pasted to Title Name Search Box";
                    }
                }
                    break;
                default:
                    break;
            }


        CommunityToolkit.Maui.Alerts.Toast msgToast = new CommunityToolkit.Maui.Alerts.Toast() { Text = text, Duration = CommunityToolkit.Maui.Core.ToastDuration.Short };

    }
}
