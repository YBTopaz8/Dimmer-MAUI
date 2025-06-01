﻿//using Dimmer.DimmerLive.Models;
using System.Threading.Tasks;
using System.Windows.Forms;

using Dimmer.WinUI.Views.ArtistsSpace.MAUI;

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


    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await MyViewModel.InitializeApp();
    }


    private void MainSongsColView_Loaded(object sender, EventArgs e)
    {

    }

    private async void ArtistsEffectsView_LongPressed(object sender, EventArgs e)
    {
        var send = (MenuFlyoutItem)sender;
        var song = send.BindingContext as SongModelView;
        var art = song.ArtistIds?.FirstOrDefault();
        DeviceStaticUtils.SelectedArtistOne = art;
        await Shell.Current.GoToAsync(nameof(ArtistsPage), true);
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
                break;
            case "Artist": // Assuming CommandParameter is "Artist" for ArtistName
                SongsColView.ItemsSource =    CollectionSortHelper.SortByArtistName(songs, newOrder);
                break;
            case "Album": // Assuming CommandParameter is "Album" for AlbumName
                SongsColView.ItemsSource =  CollectionSortHelper.SortByAlbumName(songs, newOrder);
                break;
            case "Genre":
                SongsColView.ItemsSource =   CollectionSortHelper.SortByGenre(songs, newOrder);
                break;
            case "Duration":
                SongsColView.ItemsSource =   CollectionSortHelper.SortByDuration(songs, newOrder);
                break;
            case "Year": // Assuming CommandParameter for ReleaseYear
                SongsColView.ItemsSource =   CollectionSortHelper.SortByReleaseYear(songs, newOrder);
                break;
            case "DateAdded": // Assuming CommandParameter for DateCreated
                SongsColView.ItemsSource = CollectionSortHelper.SortByDateAdded(songs, newOrder);
                break;
            default:
                System.Diagnostics.Debug.WriteLine($"Unsupported sort property: {sortProperty}");
                // Reset sort state if property is unknown, or do nothing
                _currentSortProperty = string.Empty;
                return;
        }

        // Optional: Scroll to top after sorting
        // if (SongsColView.Items.Count > 0)
        // {
        //     SongsColView.ScrollTo(songs.FirstOrDefault(), ScrollToPosition.Start, true);
        // }
    }
}