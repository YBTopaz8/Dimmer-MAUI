namespace Dimmer.Views.CustomViews;

public partial class SongLyricsDownloadPopup : DXPopup
{
	public SongLyricsDownloadPopup(BaseViewModelAnd myViewModel, SongModelView concernedSong)
	{
		InitializeComponent();
        MyViewModel = myViewModel;
        MyViewModel.SelectedSong = concernedSong;
        ConcernedSong = concernedSong;


    }
    SongModelView ConcernedSong;
    BaseViewModelAnd MyViewModel;
    private async void LyricsTabVSL_Loaded(object sender, EventArgs e)
    {
        await Task.Delay(2000);
        MyViewModel.ReadySearchViewAndProduceSearchText();
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


        
    }

    private async void SearchLyricsBtn_Clicked(object sender, EventArgs e)
    {
        MyViewModel.SelectedSong = MyViewModel.CurrentPlayingSongView;
        await MyViewModel.SearchLyricsAndLoadLyricsIfFoundAsync(true);
    }

    private void TapToViewSingleSongLyricsPopup_Tapped(object sender, TappedEventArgs e)
    {
        string? lyrics = e.Parameter as string;
        if (lyrics is null) return;
    }

    private async void ApplyLyrics_Clicked(object sender, EventArgs e)
    {
        var onlineResult = ((DXButton)sender).CommandParameter as LrcLibLyrics;
        if (onlineResult == null)
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
        if (saved)
        {
            MyViewModel.SelectedSong!.SyncLyrics = onlineResult.SyncedLyrics;
            MyViewModel.SelectedSong!.UnSyncLyrics = onlineResult.PlainLyrics;

            var songInCollection = MyViewModel.SearchResults.First(x => x.Id == MyViewModel.SelectedSong.Id);
            MyViewModel.SearchResultsHolder.Edit(updater =>
            {
                OnPropertyChanged(nameof(MyViewModel.SelectedSong));
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

            TimeSpan duration = TimeSpan.FromSeconds(2);


            CommunityToolkit.Maui.Alerts.Toast msgToast = new CommunityToolkit.Maui.Alerts.Toast() { Text = text, Duration = CommunityToolkit.Maui.Core.ToastDuration.Short };
            msgToast?.Show(cancellationTokenSource.Token);

            this.Close();


        }
    }

    private void DXPopup_Loaded(object sender, EventArgs e)
    {

        
    }

    private void DXPopup_Opened(object sender, EventArgs e)
    {
        MyViewModel.CleanLyricsSearchProps();
        MyViewModel.AutoFillSearchFields();

    }
}