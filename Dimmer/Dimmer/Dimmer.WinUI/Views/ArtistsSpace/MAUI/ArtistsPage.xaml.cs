using System.Threading.Tasks;

namespace Dimmer.WinUI.Views.ArtistsSpace.MAUI;

public partial class ArtistsPage : ContentPage
{
    public ArtistsPage(BaseViewModelWin viewModel)
    {
        InitializeComponent();

        //= IPlatformApplication.Current!.Services.GetService<BaseViewModel>()!;
        MyViewModel=viewModel;
        BindingContext=MyViewModel;
    }

    private void ArtistsPage_Loaded(object? sender, EventArgs e)
    {
        Debug.WriteLine("Loaded");
    }

    public BaseViewModelWin MyViewModel { get; }
    protected override void OnAppearing()
    {
        base.OnAppearing();
    }
    public void LoadArtists()
    {
        var s = DeviceStaticUtils.SelectedArtistOne;
        MyViewModel.ViewArtistDetails(s);

    }
    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        LoadArtists();
    }
    private async void NavHome_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
    private void PlayAll_Clicked(object sender, EventArgs e)
    {

    }
    private async void TapGestRec_Tapped(object sender, TappedEventArgs e)
    {
        var send = (Grid)sender;
        var song = send.BindingContext as SongModelView;

        await MyViewModel.PlaySongFromListAsync(song, MyViewModel.CurrentQuery);
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
            //await SearchSongsAsync(txt, token);

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
    ObservableCollection<SongModelView?>? songsToDisplay = new();

    private void ArtistAlbums_Loaded(object sender, EventArgs e)
    {

    }

    private void ArtistSongsColView_Loaded(object sender, EventArgs e)
    {

    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        MyViewModel.SelectedAlbumArtists?.Clear();
        MyViewModel.SelectedArtistAlbums?.Clear();
    }
}