//using Dimmer.DimmerLive.Models;
namespace Dimmer.WinUI.Views;

public partial class HomePage : ContentPage
{
    public BaseViewModelWin MyViewModel { get; internal set; }
    public HomePage(BaseViewModelWin vm)
    {
        InitializeComponent();
        BindingContext = vm;
        MyViewModel=vm;

    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await MyViewModel.InitializeApp();
    }

    private void SettingsChip_Clicked(object sender, EventArgs e)
    {
        MyViewModel.OpenSettingsWindow();
    }

    private void MainSongsColView_Loaded(object sender, EventArgs e)
    {

    }

    private void ArtistsEffectsView_LongPressed(object sender, EventArgs e)
    {

    }

    private void TapGestRec_Tapped(object sender, TappedEventArgs e)
    {
        var send = (Grid)sender;
        var song = send.BindingContext as SongModelView;
        MyViewModel.PlaySongFromListAsync(song, SongsColView.ItemsSource as IEnumerable<SongModelView>);
    }

    private void SkipPrev_Clicked(object sender, EventArgs e)
    {

    }

    private void PlayPauseBtn_Clicked(object sender, EventArgs e)
    {

    }

    private void SkipNext_Clicked(object sender, EventArgs e)
    {

    }

    private void OpenSongStats_Clicked(object sender, EventArgs e)
    {

    }

    private void OpenArtistWindow_Clicked(object sender, EventArgs e)
    {

    }

    private void OpenAlbumWindow_Clicked(object sender, EventArgs e)
    {

    }

    private void CurrPlayingSongGesRec_Tapped(object sender, TappedEventArgs e)
    {

    }
    private CancellationTokenSource? _debounceTimer;
    private bool isOnFocusMode;
    private async void SearchSongSB_TextChanged(object sender, TextChangedEventArgs e)
    {

        SearchBar searchBar = (SearchBar)sender;
        string txt = searchBar.Text;


        _debounceTimer?.CancelAsync();
        _debounceTimer?.Dispose();
        _debounceTimer = new CancellationTokenSource();
        CancellationToken token = _debounceTimer.Token;
        int delayMilliseconds = 600;


        try
        {
            await Task.Delay(delayMilliseconds, token);

            if (token.IsCancellationRequested)
                return;
            await SearchSongsAsync(txt, token);

        }
        catch (OperationCanceledException ex)
        {
            Debug.WriteLine("Search operation cancelled." +ex.Message);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Search Error: {ex}");
        }
    }

    private async Task SearchSongsAsync(string? searchText, CancellationToken token)
    {
        if ((MyViewModel.NowPlayingDisplayQueue is null || MyViewModel.NowPlayingDisplayQueue.Count < 1))
        {
            return;
        }

        List<SongModelView> songsToDisplay = new();

        if (string.IsNullOrEmpty(searchText))
        {

            songsToDisplay = MyViewModel.NowPlayingDisplayQueue.ToList();
        }
        else
        {

            songsToDisplay= await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();


                var e = MyViewModel.NowPlayingDisplayQueue!.
                            Where(item => (!string.IsNullOrEmpty(item.Title) && item.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                                  (!string.IsNullOrEmpty(item.ArtistName) && item.ArtistName.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                                  (!string.IsNullOrEmpty(item.AlbumName) && item.AlbumName.Contains(searchText, StringComparison.OrdinalIgnoreCase)))
                   .ToList();

                return e;
            }, token);


        }


        Dispatcher.Dispatch(() =>
        {
            if (token.IsCancellationRequested)
                return;


            SongsColView.ItemsSource = songsToDisplay.ToObservableCollection();



        });
    }

    private bool _isThrottling = false;
    private readonly int throttleDelay = 300; // Time in milliseconds

    private async void Slider_DragCompleted(object sender, EventArgs e)
    {
        var send = (Slider)sender;
        if (_isThrottling)
            return;

        _isThrottling = true;

        await MyViewModel.SeekTrackPosition(send.Value);


        await Task.Delay(throttleDelay);
        _isThrottling = false;
    }

    private void SidePaneChip_Clicked(object sender, EventArgs e)
    {
        var send = (SfChip)sender;
        var param = send.CommandParameter.ToString();
        switch (param)
        {
            case "Scroll":
                var itsSource = SongsColView.ItemsSource;
                if (itsSource is ObservableCollection<SongModelView> src)
                {
                    var ind = src.IndexOf(MyViewModel.CurrentPlayingSongView);
                    MainThread.BeginInvokeOnMainThread(() =>
                     SongsColView.ScrollTo(ind, null, ScrollToPosition.Start, true));
                
                }

                break;
            default:
                break;
        }

    }
}