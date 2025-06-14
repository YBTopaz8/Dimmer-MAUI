//using Dimmer.DimmerLive.Models;
using System.Diagnostics;

using MoreLinq;

using SortOrder = Dimmer.Utilities.SortOrder;

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


    protected override void OnAppearing()
    {
        base.OnAppearing();
    }


    private void MainSongsColView_Loaded(object sender, EventArgs e)
    {

    }

    private async void ArtistsEffectsView_LongPressed(object sender, EventArgs e)
    {
        var send = (MenuFlyoutItem)sender;
        var song = send.BindingContext as SongModelView;

        if (await MyViewModel.SelectedArtistAndNavtoPage(song))
        {
            await Shell.Current.GoToAsync(nameof(ArtistsPage), true);
        }
    }

    private async void TapGestRec_Tapped(object sender, TappedEventArgs e)
    {
        var send = (Grid)sender;
        var song = send.BindingContext as SongModelView;
        await MyViewModel.PlaySongFromListAsync(song, SongsColView.ItemsSource as IEnumerable<SongModelView>);
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

    private async void CurrPlayingSongGesRec_Tapped(object sender, TappedEventArgs e)
    {
        var song = e.Parameter as SongModelView;
        if (song is not null)
        {
            DeviceStaticUtils.SelectedSongOne = song;
            await Shell.Current.GoToAsync(nameof(SingleSongPage), true);
            return;
        }

        switch (e.Parameter)
        {
            case "Alb":
                //DeviceStaticUtils.SelectedAlbumOne = song.AlbumId;
                //await Shell.Current.GoToAsync(nameof(AlbumPage), true);
                return;
            default:
                break;
        }
        if (await MyViewModel.SelectedArtistAndNavtoPage(song))
        {
            await Shell.Current.GoToAsync(nameof(ArtistsPage), true);
        }
    }
    private CancellationTokenSource? _debounceTimer;
    private bool isOnFocusMode;
    private void SearchSongSB_TextChanged(object sender, TextChangedEventArgs e)
    {

        try
        {
            Task.Run(async () =>
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


            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }

    }
    List<SongModelView> songsToDisplay = new();
    private async Task SearchSongsAsync(string? searchText, CancellationToken token)
    {
        if ((MyViewModel.NowPlayingDisplayQueue is null || MyViewModel.NowPlayingDisplayQueue.Count < 1))
        {
            return;
        }

        ObservableCollection<SongModelView> songsToDisplay = new();

        if (string.IsNullOrEmpty(searchText))
        {

            songsToDisplay = [.. MyViewModel.NowPlayingDisplayQueue];
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
                   .ToObservableCollection();

                return e;
            }, token);


        }


        Dispatcher.Dispatch(() =>
        {
            if (token.IsCancellationRequested)
                return;

            MyViewModel.CurrentTotalSongsOnDisplay= songsToDisplay.Count;
            SongsColView.ItemsSource = songsToDisplay;



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

        MyViewModel.SeekTrackPosition(send.Value);


        await Task.Delay(throttleDelay);
        _isThrottling = false;
    }


    private void ArtistsChip_Clicked(object sender, EventArgs e)
    {

    }
    private string _currentSortProperty = string.Empty;
    private SortOrder _currentSortOrder = SortOrder.Ascending;

    private void Sort_Clicked(object sender, EventArgs e)
    {

        var chip = sender as SfChip; // Or whatever your SfChip type is
        if (chip == null || chip.CommandParameter == null)
            return;

        string sortProperty = chip.CommandParameter.ToString();
        if (string.IsNullOrEmpty(sortProperty))
            return;

        var songs = SongsColView.ItemsSource as ObservableCollection<SongModelView>;
        if (songs == null || !songs.Any())
            return;

        SortOrder newOrder;
        if (_currentSortProperty == sortProperty)
        {
            // Toggle order if sorting by the same property again
            newOrder = (_currentSortOrder == SortOrder.Ascending) ? SortOrder.Descending : SortOrder.Ascending;
        }
        else
        {
            // Default to ascending when sorting by a new property
            newOrder = SortOrder.Ascending;
        }

        // Update current sort state
        _currentSortProperty = sortProperty;
        _currentSortOrder = newOrder;

        // Optional: Update UI to show sort indicators (e.g., change chip appearance)

        switch (sortProperty)
        {
            case "Title":
                SongsColView.ItemsSource =   CollectionSortHelper.SortByTitle(songs, newOrder);
                songsToDisplay=SongsColView.ItemsSource as List<SongModelView> ?? new List<SongModelView>();
                break;
            case "Artist": // Assuming CommandParameter is "Artist" for ArtistName
                SongsColView.ItemsSource =    CollectionSortHelper.SortByArtistName(songs, newOrder);
                songsToDisplay=SongsColView.ItemsSource as List<SongModelView> ?? new List<SongModelView>();
                break;
            case "Album": // Assuming CommandParameter is "Album" for AlbumName
                SongsColView.ItemsSource =  CollectionSortHelper.SortByAlbumName(songs, newOrder);
                songsToDisplay=SongsColView.ItemsSource as List<SongModelView> ?? new List<SongModelView>();
                break;
            case "Genre":
                SongsColView.ItemsSource =   CollectionSortHelper.SortByGenre(songs, newOrder);
                songsToDisplay=SongsColView.ItemsSource as List<SongModelView> ?? new List<SongModelView>();
                break;
            case "Duration":
                SongsColView.ItemsSource =   CollectionSortHelper.SortByDuration(songs, newOrder);
                songsToDisplay=SongsColView.ItemsSource as List<SongModelView> ?? new List<SongModelView>();
                break;
            case "Year": // Assuming CommandParameter for ReleaseYear
                SongsColView.ItemsSource =   CollectionSortHelper.SortByReleaseYear(songs, newOrder);
                songsToDisplay=SongsColView.ItemsSource as List<SongModelView> ?? new List<SongModelView>();
                break;
            case "DateAdded": // Assuming CommandParameter for DateCreated
                SongsColView.ItemsSource = CollectionSortHelper.SortByDateAdded(songs, newOrder);
                songsToDisplay=SongsColView.ItemsSource as List<SongModelView> ?? new List<SongModelView>();
                break;
            default:
                System.Diagnostics.Debug.WriteLine($"Unsupported sort property: {sortProperty}");
                // Reset sort state if property is unknown, or do nothing
                _currentSortProperty = string.Empty;
                MyViewModel.CurrentTotalSongsOnDisplay= songsToDisplay.Count;
                return;

        }

        // Optional: Scroll to top after sorting
        // if (SongsColView.CurrentItems.Count > 0)
        // {
        //     SongsColView.ScrollTo(songs.FirstOrDefault(), ScrollToPosition.Start, true);
        // }
    }

    public class SortHeaderClass
    {
        public string SortProperty { get; set; } = string.Empty;
        public bool IsAscending { get; set; }

        public List<SortHeaderClass> DefaultHeaders { get; set; } = new List<SortHeaderClass>
        {
            new SortHeaderClass { SortProperty = "Title", IsAscending = true },
            new SortHeaderClass { SortProperty = "Artist", IsAscending = true },
            new SortHeaderClass { SortProperty = "Album", IsAscending = true },
            new SortHeaderClass { SortProperty = "Genre", IsAscending = true },
            new SortHeaderClass { SortProperty = "Duration", IsAscending = true },
            new SortHeaderClass { SortProperty = "Year", IsAscending = true },
            new SortHeaderClass { SortProperty = "DateAdded", IsAscending = true }
        };

        public SortHeaderClass() { }
    }

    private void Filter_Clicked(object sender, EventArgs e)
    {

    }

    private async void StatsSfChip_Clicked(object sender, EventArgs e)
    {
        if (SongsView.IsVisible)
        {
            MyViewModel.LoadStatsApp();

            await Task.WhenAll(SongsView.AnimateFadeOutBack(400), StatsView.AnimateFadeInFront(300));


        }
        else
        {
            await Task.WhenAll(SongsView.AnimateFadeInFront(300), StatsView.AnimateFadeOutBack(400));


        }
    }

    private static async void ViewSong_Clicked(object sender, EventArgs e)
    {

        var song = (SongModelView)((MenuFlyoutItem)sender).CommandParameter;

        DeviceStaticUtils.SelectedSongOne = song;
        await Shell.Current.GoToAsync(nameof(SingleSongPage), true);
    }

    private void DataPointSelectionBehavior_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.Charts.ChartSelectionChangedEventArgs e)
    {
        Debug.WriteLine(e.NewIndexes);
        Debug.WriteLine(e.NewIndexes.GetType());

    }

    private void AddToPlaylistClicked(object sender, EventArgs e)
    {
        PlaylistPopup.IsOpen = !PlaylistPopup.IsOpen;
        //MyViewModel.ActivePlaylistModel
    }

    private void ScrollToSong_Clicked(object sender, EventArgs e)
    {
        var obsCol = SongsColView.ItemsSource as ObservableCollection<SongModelView>;
        var index = obsCol.ToList();
        var iind = index.FindIndex(x => x.Id== MyViewModel.CurrentPlayingSongView.Id);

        SongsColView.ScrollTo(index: iind, -1, ScrollToPosition.Start, true);
        //SongsColView.ScrollTo(MyViewModel.CurrentPlayingSongView, null, ScrollToPosition.Start, true);
    }

    private void PlaylistsChip_Clicked(object sender, EventArgs e)
    {

    }
}