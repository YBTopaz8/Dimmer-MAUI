//using Dimmer.DimmerLive.Models;
using Dimmer.DimmerSearch;
using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.Utilities.FileProcessorUtils;

using DynamicData;
using DynamicData.Binding;

using MoreLinq;

using ReactiveUI;

using System.DirectoryServices;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;

using SortOrder = Dimmer.Utilities.SortOrder;

namespace Dimmer.WinUI.Views;

public partial class HomePage : ContentPage
{
    public BaseViewModelWin MyViewModel { get; internal set; }
    private void SearchSongSB_TextChanged(object sender, TextChangedEventArgs e)
    {
        MyViewModel.SearchSongSB_TextChanged(e.NewTextValue);
        // Optional: Update a summary label
        //SummaryLabel.Text = query.Humanize();
    }


    public HomePage(BaseViewModelWin vm)
    {
        InitializeComponent();
        BindingContext = vm;
        MyViewModel = vm;


        // --- Keep these lines. They correctly wire up the UI. ---

        MyViewModel.TranslatedSearch= TranslatedSearch;
        MyViewModel.SongsCountLabel = SongsCountLabel;

    }























    //// The master list of ALL songs, powered by Dynamic Data. This is our single source of truth.
    //private readonly SourceList<SongModelView> _masterSongList = new();

    //// The final, read-only collection that our UI's CollectionView binds to.
    //// Dynamic Data will keep this collection updated automatically on the UI thread.
    //private readonly ReadOnlyObservableCollection<SongModelView> _searchResults;

    //// The single instance of our powerful semantic query parser.
    //private readonly SemanticParser _parser = new();

    //// These "subjects" are the reactive triggers for our pipeline.
    //// We push new values into them, and the pipeline re-evaluates.
    //private readonly BehaviorSubject<Func<SongModelView, bool>> _filterPredicate;
    //private readonly BehaviorSubject<IComparer<SongModelView>> _sortComparer;



    //private void LoadAllSongsIntoMasterList()
    //{
    //    // Replace this with your actual data loading logic (e.g., from Realm DB).
    //    // Example:
    //    // var songsFromDb = MyDatabaseService.GetAllSongs();
    //    // _masterSongList.AddRange(songsFromDb);

    //    // Using placeholder data from your previous example:
    //    if (MyViewModel.NowPlayingDisplayQueue != null)
    //    {
    //        _masterSongList.AddRange(MyViewModel.NowPlayingDisplayQueue);
    //    }
    //}

    //// --- 7. The Search Bar Event Handler (Clean and Simple) ---
    //// This method is called every time the user types in the search box.
    //private void SearchSongSB_TextChanged(object sender, TextChangedEventArgs e)
    //{
    //    string searchText = e.NewTextValue;

    //    // 7a. Parse the user's text into a structured query object.
    //    var query = _parser.Parse(searchText);

    //    // 7b. Build the master filter function from the parsed query.
    //    var predicate = BuildMasterPredicate(query);

    //    // 7c. PUSH the new filter into the reactive pipeline. Dynamic Data does the rest.
    //    _filterPredicate.OnNext(predicate);

    //    // 7d. Build the master sort function.
    //    var comparer = BuildMasterComparer(query);

    //    // 7e. PUSH the new sort order into the pipeline.
    //    _sortComparer.OnNext(comparer);

    //    // Optional: Update a summary label on the UI instantly.
    //    // HumanizedQueryLabel.Text = query.Humanize();
    //}

    //#region --- Predicate and Comparer Builder Methods ---
    //private IComparer<SongModelView> BuildMasterComparer(SemanticQuery query)
    //{
    //    // Simply create a new instance of our custom, robust comparer.
    //    return new SongModelViewComparer(query.SortDirectives);
    //}

    //private Func<SongModelView, bool> BuildMasterPredicate(SemanticQuery query)
    //{
    //    // The top-level predicate is now built directly from the top-level query object.
    //    // It will recursively call AsPredicate() on all its children.
    //    return query.AsPredicate();
    //}

    //// A new helper method to create a single comparer for one field.
    //private IComparer<SongModelView>? CreateComparerForField(string fieldName, Dimmer.DimmerSearch.SortDirection direction)
    //{
    //    if (direction == Dimmer.DimmerSearch.SortDirection.Asc)
    //    {
    //        return SortExpressionComparer<SongModelView>.Asc(
    //            song => SemanticQueryHelpers.GetComparableProp(song, fieldName));
    //    }
    //    else
    //    {
    //        return SortExpressionComparer<SongModelView>.Desc(
    //            song => SemanticQueryHelpers.GetComparableProp(song, fieldName));
    //    }
    //}

    ///// <summary>
    ///// Creates a compiled Func delegate for sorting using Reflection.
    ///// Caches the compiled functions for performance.
    ///// </summary>
    //private static readonly Dictionary<string, Func<SongModelView, IComparable>> _sortFuncCache = new();

    //#endregion

    //public HomePage(BaseViewModelWin vm)
    //{
    //    InitializeComponent();
    //    BindingContext = vm;
    //    MyViewModel=vm;


    //    // Initialize the reactive subjects with sensible defaults.
    //    _filterPredicate = new BehaviorSubject<Func<SongModelView, bool>>(song => true);
    //    _sortComparer = new BehaviorSubject<IComparer<SongModelView>>(new SongModelViewComparer(null));

    //    // --- 4. THE DYNAMIC DATA PIPELINE ---
    //    // This is the heart of the reactive system. It's defined here and never touched again.
    //    _masterSongList.Connect()
    //        // Throttle waits for a pause in user input before processing. This prevents UI lag.
    //        .Throttle(TimeSpan.FromMilliseconds(400))
    //        .Filter(_filterPredicate) // Filters the list using our dynamically generated predicate.
    //        .Sort(_sortComparer)      // Sorts the list using our dynamically generated comparer.
    //        .ObserveOn(Scheduler.Default) // Perform the filtering/sorting on a background thread.
    //        .Bind(out _searchResults) // Binds the final results to our read-only collection.
    //        .Subscribe(
    //            _ => {
    //                // This block runs on a background thread after the collection has changed.
    //                // We can dispatch final UI updates here if needed.
    //                MainThread.BeginInvokeOnMainThread(() => {
    //                    // Example: Update a label with the result count.
    //                    // CountLabel.Text = $"{_searchResults.Count} songs found";
    //                });
    //            },
    //            ex => {
    //                // Handle any catastrophic errors in the pipeline.
    //                Debug.WriteLine($"Error in DynamicData pipeline: {ex}");
    //            }
    //        );

    //    // --- 5. Connect the UI to the final data source ---
    //    SongsColView.ItemsSource = _searchResults;

    //    // --- 6. Load Your Data ---
    //    // On a real app, you might do this in an OnAppearing override.
    //    LoadAllSongsIntoMasterList();
    //}


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
        //var song = e.Parameter as SongModelView;
        //if (song is not null)
        //{
        //    DeviceStaticUtils.SelectedSongOne = song;
        //    await Shell.Current.GoToAsync(nameof(SingleSongPage), true);
        //    return;
        //}

        //switch (e.Parameter)
        //{
        //    case "Alb":
        //        //DeviceStaticUtils.SelectedAlbumOne = song.AlbumId;
        //        //await Shell.Current.GoToAsync(nameof(AlbumPage), true);
        //        return;
        //    default:
        //        break;
        //}
        //if (await MyViewModel.SelectedArtistAndNavtoPage(song))
        //{
        //    await Shell.Current.GoToAsync(nameof(ArtistsPage), true);
        //}
    }

    private bool isOnFocusMode;
    private bool _isThrottling = false;
    private readonly int throttleDelay = 300; // Time in milliseconds

    List<SongModelView> songsToDisplay = new();

    private void ArtistsChip_Clicked(object sender, EventArgs e)
    {

    }
    private string _currentSortProperty = string.Empty;
    private SortOrder _currentSortOrder = SortOrder.Asc;

    private async void Sort_Clicked(object sender, EventArgs e)
    {

        var chip = sender as SfChip; // Or whatever your SfChip type is
        if (chip == null || chip.CommandParameter == null)
            return;

        string sortProperty = chip.CommandParameter.ToString();
        if (string.IsNullOrEmpty(sortProperty))
            return;


        SortOrder newOrder;
        if (_currentSortProperty == sortProperty)
        {
            // Toggle order if sorting by the same property again
            newOrder = (_currentSortOrder == SortOrder.Asc) ? SortOrder.Desc : SortOrder.Asc;
        }
        else
        {
            // Default to ascending when sorting by a new property
            newOrder = SortOrder.Asc;
        }

        // Update current sort state
        _currentSortProperty = sortProperty;
        _currentSortOrder = newOrder;

        MyViewModel.SearchSongSB_TextChanged($"{_currentSortOrder} {_currentSortProperty}");
        // Optional: Update UI to show sort indicators (e.g., change chip appearance)

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

    private async void ScrollToSong_Clicked(object sender, EventArgs e)
    {
        try
        {
            //Debug.WriteLine(SongsColView.ItemsSource.GetType());
            var obsCol = SongsColView.ItemsSource as IEnumerable<SongModelView>;
            var index = obsCol.ToList();
            var iind = index.FindIndex(x => x.Id== MyViewModel.CurrentPlayingSongView.Id);
            if (iind<0)
            {
                return;
            }
            SongsColView.ScrollTo(index: iind, -1, ScrollToPosition.Start, true);

        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    private void PlaylistsChip_Clicked(object sender, EventArgs e)
    {
        PlaylistPopup.Show();
    }

    private void SongsColView_SelectionChanged(object sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    {

    }

    private void AllLyricsColView_Loaded(object sender, EventArgs e)
    {

    }

    private void QuickSearchAlbum_Clicked(object sender, EventArgs e)
    {
        SearchSongSB.Text= ((MenuFlyoutItem)sender).CommandParameter.ToString();
        SearchSongSB.Focus();
    }

    private void QuickSearchArtist_Clicked(object sender, EventArgs e)
    {

    }

    private async void PointerRecog_PointerEntered(object sender, PointerEventArgs e)
    {
        await Task.WhenAll(SongsColView.DimmOut(),
            TranslatedSearch.DimmIn(),
            UtilitySection.AnimateHeight(77, 450, Easing.SpringOut)
            );
        //UtilitySection.AnimateFadeOutBack()

    }

    private async void PointerRecog_PointerExited(object sender, PointerEventArgs e)
    {
        await Task.WhenAll(SongsColView.DimmIn(),
            TranslatedSearch.DimmOut(),
            UtilitySection.AnimateHeight(0, 350, Easing.SpringOut)
            );
        SearchSongSB.Unfocus();
    }
    private async void SearchSongSB_Focused(object sender, FocusEventArgs e)
    {

        await Task.WhenAll(SongsColView.DimmOut(),
             AdvSearch.DimmInCompletelyAndShow(),
            SearchSongSB.AnimateHeight(150, 650, Easing.SpringOut));
        //UtilitySection.AnimateFadeOutBack()
    }

    private async void SearchSongSB_Unfocused(object sender, FocusEventArgs e)
    {
        await Task.Delay(500);
        await Task.WhenAll(SongsColView.DimmIn(),
     TranslatedSearch.DimmOut(), AdvSearch.DimmOutCompletelyAndHide(),
         SearchSongSB.AnimateHeight(50, 500, Easing.SpringIn));
        //UtilitySection.AnimateFadeInFront()
        SearchSongSB.FontSize = 17;

    }


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

    private CancellationTokenSource _lyricsCts;
    private bool _isLyricsProcessing = false;
    private async void RefreshLyrics_Clicked(object sender, EventArgs e)
    {
        if (_isLyricsProcessing)
        {
            // Optionally, offer to cancel the running process
            bool cancel = await DisplayAlert("Processing...", "Lyrics are already being processed. Cancel the current operation?", "Yes, Cancel", "No");
            if (cancel)
            {
                _lyricsCts?.Cancel();
            }
            return;
        }

        _isLyricsProcessing = true;
        MyProgressBar.IsVisible = true; // Show a progress bar
        MyProgressLabel.IsVisible = true; // Show a label

        // Create a new CancellationTokenSource for this operation
        _lyricsCts = new CancellationTokenSource();

        // The IProgress<T> object automatically marshals calls to the UI thread.
        var progressReporter = new Progress<LyricsProcessingProgress>(progress =>
        {
            // This code runs on the UI thread safely!
            MyProgressBar.Progress = (double)progress.ProcessedCount / progress.TotalCount;
            MyProgressLabel.Text = $"Processing: {progress.CurrentFile}";
        });

        try
        {
            MyViewModel.SearchSongSB_TextChanged(string.Empty); // Clear the search bar to refresh the list
            // Get the list of songs you want to process
            var songsToRefresh = MyViewModel.SearchResults; // Or your full master list
            var lryServ = IPlatformApplication.Current.Services.GetService<ILyricsMetadataService>();
            // --- Call our static, background-safe method ---
            await SongDataProcessor.ProcessLyricsAsync(songsToRefresh, lryServ, progressReporter, _lyricsCts.Token);

            await DisplayAlert("Complete", "Lyrics processing finished!", "OK");
        }
        catch (OperationCanceledException)
        {
            await DisplayAlert("Cancelled", "The operation was cancelled.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An unexpected error occurred: {ex.Message}", "OK");
        }
        finally
        {
            // Clean up and hide UI elements
            _isLyricsProcessing = false;
            MyProgressBar.IsVisible = false;
            MyProgressLabel.IsVisible = false;
        }
    }


    private void Label_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {

    }
}
