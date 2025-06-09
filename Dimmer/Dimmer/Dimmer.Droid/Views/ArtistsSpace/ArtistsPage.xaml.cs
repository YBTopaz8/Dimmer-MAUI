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
    private async void SongsColView_Tap(object sender, CollectionViewGestureEventArgs e)
    {
        var song = e.Item as SongModelView;
        await MyViewModel.BaseVM.PlaySongFromListAsync(song, SongsColView.ItemsSource as IEnumerable<SongModelView>);
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
        var ee = SongsColView.ItemsSource as IEnumerable<SongModelView>;
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
        var songs = SongsColView.ItemsSource as IEnumerable<SongModelView>;

        Debug.WriteLine(songs is null);
        if (songs is null)
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
            SongsColView.ItemsSource = songsToDisplay;



        });
    }
    SortOrder internalOrder = SortOrder.Ascending;
    private bool SortIndeed()
    {
        ObservableCollection<SongModelView> songs = SongsColView.ItemsSource as ObservableCollection<SongModelView>
        ;
        if (songs == null || !songs.Any())
            return false;
        internalOrder =  internalOrder== SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;

        MyViewModel.BaseVM.CurrentSortOrder = internalOrder;

        switch (MyViewModel.BaseVM.CurrentSortProperty)
        {
            case "Title":
                SongsColView.ItemsSource =   CollectionSortHelper.SortByTitle(songs, MyViewModel.BaseVM.CurrentSortOrder);
                songsToDisplay=SongsColView.ItemsSource as ObservableCollection<SongModelView> ?? new ObservableCollection<SongModelView>();
                break;
            case "Artist": // Assuming CommandParameter is "Artist" for ArtistName
                SongsColView.ItemsSource =    CollectionSortHelper.SortByArtistName(songs, MyViewModel.BaseVM.CurrentSortOrder);
                songsToDisplay=SongsColView.ItemsSource as ObservableCollection<SongModelView> ?? new ObservableCollection<SongModelView>();
                break;
            case "Album": // Assuming CommandParameter is "Album" for AlbumName
                SongsColView.ItemsSource =  CollectionSortHelper.SortByAlbumName(songs, MyViewModel.BaseVM.CurrentSortOrder);
                songsToDisplay=SongsColView.ItemsSource as ObservableCollection<SongModelView> ?? new ObservableCollection<SongModelView>();
                break;
            case "Genre":
                SongsColView.ItemsSource =   CollectionSortHelper.SortByGenre(songs, MyViewModel.BaseVM.CurrentSortOrder);
                songsToDisplay=SongsColView.ItemsSource as ObservableCollection<SongModelView> ?? new ObservableCollection<SongModelView>();
                break;
            case "Duration":
                SongsColView.ItemsSource =   CollectionSortHelper.SortByDuration(songs, MyViewModel.BaseVM.CurrentSortOrder);
                songsToDisplay=SongsColView.ItemsSource as ObservableCollection<SongModelView> ?? new ObservableCollection<SongModelView>();
                break;
            case "Year": // Assuming CommandParameter for ReleaseYear
                SongsColView.ItemsSource =   CollectionSortHelper.SortByReleaseYear(songs, MyViewModel.BaseVM.CurrentSortOrder);
                songsToDisplay=SongsColView.ItemsSource as ObservableCollection<SongModelView> ?? new ObservableCollection<SongModelView>();
                break;
            case "DateAdded": // Assuming CommandParameter for DateCreated
                SongsColView.ItemsSource = CollectionSortHelper.SortByDateAdded(songs, MyViewModel.BaseVM.CurrentSortOrder);
                songsToDisplay=SongsColView.ItemsSource as ObservableCollection<SongModelView> ?? new ObservableCollection<SongModelView>();
                break;
            default:
                System.Diagnostics.Debug.WriteLine($"Unsupported sort property: {MyViewModel.BaseVM.CurrentSortProperty}");
                // Reset sort state if property is unknown, or do nothing
                MyViewModel.BaseVM.CurrentSortProperty = string.Empty;
                MyViewModel.BaseVM.CurrentTotalSongsOnDisplay= songsToDisplay.Count;
                break;

        }
        MyViewModel.BaseVM.CurrentSortOrderInt = (int)MyViewModel.BaseVM.CurrentSortOrder;

        return true;
    }


    private void SortChoose_Clicked(object sender, EventArgs e)
    {

        var chip = sender as DXButton; // Or whatever your SfChip type is
        if (chip == null || chip.CommandParameter == null)
            return;

        string sortProperty = chip.CommandParameter.ToString();
        if (string.IsNullOrEmpty(sortProperty))
            return;


        // Update current sort state
        MyViewModel.BaseVM.CurrentSortProperty = sortProperty;


        SortOrder newOrder;

        // Toggle order if sorting by the same property again
        newOrder = (MyViewModel.BaseVM.CurrentSortOrder == SortOrder.Ascending) ? SortOrder.Descending : SortOrder.Ascending;


        MyViewModel.BaseVM.CurrentSortOrder = newOrder;
        MyViewModel.BaseVM.CurrentSortOrderInt = (int)newOrder;
        // Optional: Update UI to show sort indicators (e.g., change chip appearance)
        bool flowControl = SortIndeed();
        if (!flowControl)
        {
            return;
        }

        // Optional: Scroll to top after sorting
        // if (SongsColView.CurrentItems.Count > 0)
        // {
        //     SongsColView.ScrollTo(songs.FirstOrDefault(), ScrollToPosition.Start, true);
        // }
    }

    string SearchParam = string.Empty;

    private void SearchBy_TextChanged(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(SearchBy.Text))
        {
            ByAll();
            return;
        }
        switch (SearchParam)
        {
            case "Title":
                ByTitle();
                break;
            case "Artist":
                ByArtist();
                break;
            case "":
                ByAll();
                break;
            default:
                ByAll();
                break;
        }

    }

    private void ByTitle()
    {
        if (!string.IsNullOrEmpty(SearchBy.Text))
        {
            if (SearchBy.Text.Length >= 1)
            {

                SongsColView.FilterString = $"Contains([Title], '{SearchBy.Text}')";
            }
            else
            {
                SongsColView.FilterString = string.Empty;
            }
        }
    }
    private void ByAll()
    {
        if (!string.IsNullOrEmpty(SearchBy.Text))
        {
            if (SearchBy.Text.Length >= 1)
            {
                SongsColView.FilterString =
                    $"Contains([Title], '{SearchBy.Text}') OR " +
                    $"Contains([ArtistName], '{SearchBy.Text}') OR " +
                    $"Contains([AlbumName], '{SearchBy.Text}')";
            }
            else
            {
                SongsColView.FilterString = string.Empty;
            }
        }
        else
        {
            SongsColView.FilterString = string.Empty;
        }
    }
    private void ByArtist()
    {
        if (!string.IsNullOrEmpty(SearchBy.Text))
        {
            if (SearchBy.Text.Length >= 1)
            {
                SongsColView.FilterString = $"Contains([ArtistName], '{SearchBy.Text}')";

            }
            else
            {
                SongsColView.FilterString = string.Empty;
            }
        }
    }
}