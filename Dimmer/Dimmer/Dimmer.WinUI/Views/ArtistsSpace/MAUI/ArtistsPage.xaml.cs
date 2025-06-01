using System.Threading.Tasks;

using Dimmer.Interfaces.Services.Interfaces;

using Syncfusion.Maui.Toolkit.Carousel;

using static Vanara.PInvoke.User32;

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

    public BaseViewModelWin MyViewModel { get; }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadArtists();
    }
    public void LoadArtists()
    {
        var s = DeviceStaticUtils.SelectedArtistOne;
        MyViewModel.ViewArtistDetails(s);

    }
    private async void NavHome_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
    private async void TapGestRec_Tapped(object sender, TappedEventArgs e)
    {
        var send = (Grid)sender;
        var song = send.BindingContext as SongModelView;
        var ee = ArtistSongsColView.ItemsSource as IEnumerable<SongModelView>;
        songsToDisplay =ee.ToObservableCollection();
        await MyViewModel.PlaySongFromListAsync(song, ee);
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
    ObservableCollection<SongModelView> songsToDisplay = new();
    private async Task SearchSongsAsync(string? searchText, CancellationToken token)
    {
        if ((songsToDisplay is null || songsToDisplay.Count < 1))
        {
            return;
        }

        if (string.IsNullOrEmpty(searchText))
        {

            songsToDisplay = [.. MyViewModel.SelectedArtistSongs];
        }
        else
        {

            songsToDisplay= await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();


                var e = MyViewModel.SelectedArtistSongs!.
                            Where(item => (!string.IsNullOrEmpty(item.Title) && item.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                                  (!string.IsNullOrEmpty(item.ArtistName) && item.ArtistName.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                                  (!string.IsNullOrEmpty(item.AlbumName) && item.AlbumName.Contains(searchText, StringComparison.OrdinalIgnoreCase)))
                   .ToObservableCollection();

                return e;
            }, token);


        }


        Dispatcher.Dispatch(() =>
        {
            if (token.IsCancellationRequested)
                return;

            MyViewModel.CurrentTotalSongsOnDisplay= songsToDisplay.Count;
            ArtistSongsColView.ItemsSource = songsToDisplay;



        });
    }

}