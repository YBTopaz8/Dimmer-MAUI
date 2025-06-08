using System.Diagnostics;

using AndroidX.Lifecycle;

using DevExpress.Maui.Mvvm;

using Dimmer.Utilities;

namespace Dimmer.Views.ArtistsSpace;

public partial class ArtistsPage : ContentPage
{
    public ArtistsPage(BaseViewModelAnd viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        MyViewModel=viewModel;
    }
    BaseViewModelAnd MyViewModel { get; set; }
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
    private async void PlayAll_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.BaseVM.PlaySongFromListAsync(MyViewModel.BaseVM.SelectedArtistSongs.FirstOrDefault(), MyViewModel.BaseVM.SelectedArtistSongs);
    }
    private async void TapGestRec_Tapped(object sender, TappedEventArgs e)
    {
        var send = (Grid)sender;
        var song = send.BindingContext as SongModelView;
        var ee = ArtistSongsColView.ItemsSource as IEnumerable<SongModelView>;
        songsToDisplay =ee.ToObservableCollection();
        await MyViewModel.BaseVM.PlaySongFromListAsync(song, ee);
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

    SongModelView selectedSongPopUp = new SongModelView();
    private void MoreIcon_Clicked(object sender, EventArgs e)
    {
        var send = (DXButton)sender;
        var paramss = send.CommandParameter as SongModelView;
        if (paramss is null)
        {
            return;
        }
        selectedSongPopUp = paramss;
        MyViewModel.BaseVM.SetCurrentlyPickedSongForContext(paramss);
        SongsMenuPopup.Show();

    }

    private void ClosePopup(object sender, EventArgs e)
    {
        SongsMenuPopup.Close();
    }

    ObservableCollection<SongModelView> songsToDisplay = new();
    private async Task SearchSongsAsync(string? searchText, CancellationToken token)
    {
        var songs = ArtistSongsColView.ItemsSource as IEnumerable<SongModelView>;

        Debug.WriteLine(songs is null);
        if(songs is null)
        {
            return;
        }
        songsToDisplay ??= songs.ToObservableCollection();
        if ((songsToDisplay is null || songsToDisplay.Count < 1))
        {
            return;
        }

        if (string.IsNullOrEmpty(searchText))
        {

            songsToDisplay = [.. MyViewModel.BaseVM.SelectedArtistSongs];
        }
        else
        {

            songsToDisplay= await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();


                var e = MyViewModel.BaseVM.SelectedArtistSongs!.
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

            MyViewModel.BaseVM.CurrentTotalSongsOnDisplay= songsToDisplay.Count;
            ArtistSongsColView.ItemsSource = songsToDisplay;



        });
    }
}