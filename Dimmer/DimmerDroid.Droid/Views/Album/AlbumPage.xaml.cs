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
                //await StatsVM.LoadAlbumStatsAsync(MyViewModel.SelectedAlbum);

            });
    }

    private void SongsCV_Loaded(object sender, EventArgs e)
    {

    }

    private void SongsCV_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {

        if (e.PropertyName == nameof(SongsCV.VisibleItemCount))
        {

            var newCount = (SongsCV.ItemsSource as ReadOnlyObservableCollection<SongModelView>)?.Count;
            string? fullStr = newCount.ToString();
            //SearchText.Suffix = fullStr;
        }
    }

    private void ImageOnCollectionViewTapped(object sender, TappedEventArgs e)
    {

    }

    private async void TapToPlaySongGestRecog_Tapped(object sender, TappedEventArgs e)
    {
        var send = (View)sender;
        var song = (SongModelView)send.BindingContext;
        //var songsInCV = SongsCV.ItemsSource;


        List<SongModelView> songsInCV = new();
        for (int i = 0; i < SongsCV.VisibleItemCount; i++)
        {
            var itemHandle = SongsCV.GetItemHandleByVisibleIndex(i);

            if (SongsCV.GetItem(itemHandle) is not SongModelView songByItemHandle) continue;
            songsInCV.Add(songByItemHandle);
        }



        await MyViewModel.PlaySongAsync(song, CurrentPage.HomePage, songsInCV);
    }

    private void OtherArtistsName_Clicked(object sender, EventArgs e)
    {
        var dxBtn = (DXButton)sender;
        var song = dxBtn.CommandParameter as SongModelView;
        if (song is null) return;
        MyViewModel.SelectedSong = song;
        var songHandle = SongsCV.FindItemHandle(song);

        SongsCV.ScrollTo(songHandle, DXScrollToPosition.Start);
        ArtistsMgtBtmSheet.Show(BottomSheetState.HalfExpanded);
    }
    private void PreviewArtistSongsBtn_CheckedChanged(object sender, ValueChangedEventArgs<bool> e)
    {
        var send = (DXToggleButton)sender;
        var artist = send.CommandParameter as ArtistModelView;

        if (artist is null) return;
        if (e.NewValue)
        {
            MyViewModel.SwapMainSongsToArtistSongs(artist);
        }
        else
        {
            MyViewModel.SwapBackToMainSongs();
        }
    }
    private void AddToQueueButton_Clicked(object sender, EventArgs e)
    {

    }
    private async void ViewArtist_Clicked(object sender, EventArgs e)
    {

        var send = (DXButton)sender;
        var artist = send.CommandParameter as ArtistModelView;

        if (artist is null) return;


        MyViewModel.SetSelectedArtist(artist);
        await ArtistsMgtBtmSheet.CloseAsync();
        await Shell.Current.GoToAsync(nameof(ArtistPage), true);
    }

}