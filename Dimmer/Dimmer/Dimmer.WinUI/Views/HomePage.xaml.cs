//using Dimmer.DimmerLive.Models;
using Dimmer.DimmerSearch;
using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.Utilities.FileProcessorUtils;

using DynamicData;
using DynamicData.Binding;
using Compositor = Microsoft.UI.Composition.Compositor;
using Visual = Microsoft.UI.Composition.Visual;
using Microsoft.Maui.Controls.Internals;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;

using MoreLinq;

using ReactiveUI;

using System.DirectoryServices;
using System.Numerics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;

using Windows.UI.Composition;

using SortOrder = Dimmer.Utilities.SortOrder;
using WinUIControls = Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using CompositionBatchTypes = Microsoft.UI.Composition.CompositionBatchTypes;
using CompositionEasingFunction = Microsoft.UI.Composition.CompositionEasingFunction;

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
    private Compositor _compositor;
    private Visual _scrollViewerContentVisual; // The visual we will animate for scrolling
    private WinUIControls.ListView? _nativeListView;
    private void AllLyricsColView_Loaded(object sender, EventArgs e)
    {
        if (AllLyricsColView.Handler?.PlatformView is WinUIControls.ListView nativeListView)
        {

            return;
            _nativeListView = nativeListView;

            var elVis = ElementCompositionPreview.GetElementVisual(nativeListView);
            //elVis.
            //_compositor = ElementCompositionPreview.GetElementVisual(nativeListView).Compositor;


            //nativeListView.LayoutUpdated += OnLayoutUpdated;

            _nativeListView.SelectionChanged += _nativeListView_SelectionChanged;


            AllLyricsColView.Unloaded += (s, e) =>
            {
                _nativeListView.SelectionChanged -= _nativeListView_SelectionChanged;
                //_nativeListView.ContainerContentChanging -= _nativeListView_ContainerContentChanging;
                _nativeListView = null;
            };
        }
    }

    private void OnLayoutUpdated(object? sender, object e)
    {
        // We only need to do this once.
        if (_scrollViewerContentVisual != null)
        {
            return;
        }

        if (_nativeListView != null)
        {
            _scrollViewer= FindVisualChild<WinUIControls.ScrollViewer>(_nativeListView);
            if (_scrollViewer != null)
            {

                // Initialize the visual we need to animate.
                _scrollViewerContentVisual = ElementCompositionPreview.GetElementVisual(_scrollViewer.Content as UIElement);

                // CRITICAL: Immediately unsubscribe from the event to avoid performance issues.
                _nativeListView.LayoutUpdated -= OnLayoutUpdated;
                if (_pendingAnimationRequest.HasValue)
                {
                    var request = _pendingAnimationRequest.Value;
                    _pendingAnimationRequest = null;
                    OrchestrateCoordinatedTransition(request.deselected, request.selected);
                }
            }
        }
    }

    private void PrintVisualTree(DependencyObject obj, int indent = 0)
    {
        string prefix = new string(' ', indent * 4);
        System.Diagnostics.Debug.WriteLine($"{prefix}{obj.GetType().Name}");

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            PrintVisualTree(VisualTreeHelper.GetChild(obj, i), indent + 1);
        }
    }
    private object? _itemToAnimateOnLoad = null;
    private void _nativeListView_ContainerContentChanging(WinUIControls.ListViewBase sender, WinUIControls.ContainerContentChangingEventArgs args)
    {
        // Check if the item being loaded is the one we're waiting to animate.
        if (args.InRecycleQueue == false && args.Item == _itemToAnimateOnLoad)
        {
            // We found it! Animate the container.
            AnimateItemSelected((WinUIControls.ListViewItem)args.ItemContainer);

            // IMPORTANT: Reset the field so we don't accidentally re-animate it.
            _itemToAnimateOnLoad = null;
        }
    }

    private void _nativeListView_SelectionChanged(object sender, WinUIControls.SelectionChangedEventArgs e)
    {
        if (_isAnimatingSelection)
            return;

        if (e.RemovedItems.FirstOrDefault() is { } deselectedItem &&
            e.AddedItems.FirstOrDefault() is { } selectedItem)
        {
            if (_scrollViewerContentVisual == null)
            {
                _pendingAnimationRequest = (deselectedItem, selectedItem);
                return;
            }

            OrchestrateCoordinatedTransition(deselectedItem, selectedItem);
        }
    }

    private (object deselected, object selected)? _pendingAnimationRequest;
    private bool _isAnimatingSelection = false;
    private void OrchestrateCoordinatedTransition(object deselectedItem, object selectedItem)
    {
        _isAnimatingSelection = true;

        // --- PHASE 1: "Eyelid Shuts" (Fade Out) ---
        var fadeOutAnimation = _compositor.CreateScalarKeyFrameAnimation();
        fadeOutAnimation.InsertKeyFrame(1.0f, 0.0f);
        fadeOutAnimation.Duration = TimeSpan.FromMilliseconds(200);

        var scopeBatch = _compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        scopeBatch.Completed += (s, a) =>
        {
            // --- PHASE 2: Work while invisible ---
            // Instantly shrink the old item back to normal.
            if (_nativeListView.ContainerFromItem(deselectedItem) is WinUIControls.ListViewItem deselectedContainer)
            {
                var visual = ElementCompositionPreview.GetElementVisual(deselectedContainer);
                visual.Scale = Vector3.One;
            }

            // Perform the invisible scroll.
            _nativeListView.ScrollIntoView(selectedItem, WinUIControls.ScrollIntoViewAlignment.Leading);

            // Instantly grow the new item. It will be revealed this way.
            if (_nativeListView.ContainerFromItem(selectedItem) is WinUIControls.ListViewItem selectedContainer)
            {
                AnimateItemGrow(selectedContainer);
            }

            // --- PHASE 3: "Eyelid Opens" (Fade In) ---
            var fadeInAnimation = _compositor.CreateScalarKeyFrameAnimation();
            fadeInAnimation.InsertKeyFrame(1.0f, 1.0f);
            fadeInAnimation.Duration = TimeSpan.FromMilliseconds(350);
            fadeInAnimation.DelayTime = TimeSpan.FromMilliseconds(50);
            _listViewVisual.StartAnimation("Opacity", fadeInAnimation);

            _isAnimatingSelection = false;
        };

        _listViewVisual.StartAnimation("Opacity", fadeOutAnimation);
        scopeBatch.End();
    }
    private Visual _listViewVisual;
    private void AnimateItemGrow(WinUIControls.ListViewItem item)
    {
        var visual = ElementCompositionPreview.GetElementVisual(item);

        // The robust ExpressionAnimation to guarantee it scales from the center.
        var centerPointExpression = _compositor.CreateExpressionAnimation("Vector3(this.Target.Size.X * 0.5, this.Target.Size.Y * 0.5, 0)");
        visual.StartAnimation("CenterPoint", centerPointExpression);

        // Instantly set the scale. We don't animate it here, we just set the final state.
        visual.Scale = new Vector3(1.1f, 1.1f, 1.0f);
    }
    private WinUIControls.ScrollViewer? _scrollViewer;
    private void CleanupPreviousAnimationState()
    {
        if (_scrollViewerContentVisual == null || _scrollViewer == null)
            return;

        // Stop any rogue animations that might be running.
        _scrollViewerContentVisual.StopAnimation("Offset");

        // The ScrollViewer knows the *correct* scroll position. Our visual's offset is just for animation.
        // We calculate what the visual's offset *should* be based on the ScrollViewer's real position.
        var correctOffset = -(float)_scrollViewer.VerticalOffset;

        // If our visual's animated offset is different from the correct offset (with a small tolerance),
        // it means the last animation was interrupted. We snap it back into place instantly.
        if (Math.Abs(_scrollViewerContentVisual.Offset.Y - correctOffset) > 0.1f)
        {
            _scrollViewerContentVisual.Offset = new Vector3(0, correctOffset, 0);
        }
    }


    /*    // --- STEP 1: Get the starting state ---
        var deselectedContainer = _nativeListView.ContainerFromItem(deselectedItem) as WinUIControls.ListViewItem;
        if (deselectedContainer == null)
        { _isAnimatingSelection = false; return; } // Can't animate if old item is gone

        var startOffset = _scrollViewerContentVisual.Offset.Y;

        // --- STEP 2: The trick to find the end state ---
        _nativeListView.ScrollIntoView(selectedItem, WinUIControls.ScrollIntoViewAlignment.Leading);
        _nativeListView.UpdateLayout(); // Force the layout to update and create the new container
        var selectedContainer = _nativeListView.ContainerFromItem(selectedItem) as WinUIControls.ListViewItem;
        if (selectedContainer == null)
        { _isAnimatingSelection = false; return; } // New item couldn't be found

        // Calculate the distance we need to scroll.
        var endOffset = startOffset - (selectedContainer.TransformToVisual(_nativeListView).TransformPoint(new Windows.Foundation.Point(0, 0)).Y);

        // Instantly jump back to the start position before the user sees anything
        _scrollViewerContentVisual.Offset = new Vector3(0, startOffset, 0);

        // --- STEP 3: Create and run the synchronized animations ---
        var scopeBatch = _compositor.CreateScopedBatch(Microsoft.UI.Composition.CompositionBatchTypes.Animation);
        var duration = TimeSpan.FromMilliseconds(500);
        var ease = _compositor.CreateCubicBezierEasingFunction(new Vector2(0.4f, 0.0f), new Vector2(0.2f, 1.0f));

        // Animation 1: Shrink/Fade out old item
        AnimateItem(ElementCompositionPreview.GetElementVisual(deselectedContainer), 1.0f, 0.8f, 1.0f, 0.0f, duration, ease);

        // Animation 2: Grow/Fade in new item
        AnimateItem(ElementCompositionPreview.GetElementVisual(selectedContainer), 0.8f, 1.1f, 0.0f, 1.0f, duration, ease);

        // Animation 3: Animate the scroll
        var scrollAnimation = _compositor.CreateVector3KeyFrameAnimation();
        scrollAnimation.Duration = duration;
        scrollAnimation.InsertKeyFrame(1.0f, new Vector3(0, endOffset, 0), ease);
        _scrollViewerContentVisual.StartAnimation("Offset", scrollAnimation);

        scopeBatch.Completed += (s, a) =>
        {
            // Cleanup: Reset the scroll offset and re-enable selection
            _scrollViewerContentVisual.StopAnimation("Offset");
            _scrollViewerContentVisual.Offset = new Vector3(0, endOffset, 0);
            _isAnimatingSelection = false;
        };
        scopeBatch.End();
    }
    */
    private void AnimateItem(Visual visual, float startScale, float endScale, float startOpacity, float endOpacity, TimeSpan duration, CompositionEasingFunction ease, TimeSpan delay)
    {
        var centerPointExpression = _compositor.CreateExpressionAnimation(
            "Vector3(this.Target.Size.X * 0.5, this.Target.Size.Y * 0.5, 0)");

        // We "start" this animation, but with no duration, it's not an animation over time.
        // It's a rule that is now permanently active for this visual.
        visual.StartAnimation("CenterPoint", centerPointExpression);
        // ----------------------

        visual.Opacity = startOpacity;

        var scaleAnim = _compositor.CreateScalarKeyFrameAnimation();
        scaleAnim.Duration = duration;
        scaleAnim.DelayTime = delay;
        scaleAnim.InsertKeyFrame(0.0f, startScale);
        scaleAnim.InsertKeyFrame(1.0f, endScale, ease);

        var opacityAnim = _compositor.CreateScalarKeyFrameAnimation();
        opacityAnim.Duration = duration;
        opacityAnim.DelayTime = delay;
        // The end opacity is now correctly respected from your parameter.
        opacityAnim.InsertKeyFrame(1.0f, endOpacity, ease);

        visual.StartAnimation("Scale.X", scaleAnim);
        visual.StartAnimation("Scale.Y", scaleAnim);
        visual.StartAnimation("Opacity", opacityAnim);
    }

    private T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            DependencyObject? child = VisualTreeHelper.GetChild(obj, i);
            if (child != null && child is T)
            {
                return (T)child;
            }
            else
            {
                T? childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
        }
        return null;
    }


    // STEP 3: The Animations
    private void AnimateItemSelected(WinUIControls.ListViewItem item)
    {
        var visual = ElementCompositionPreview.GetElementVisual(item);
        Microsoft.UI.Composition.Compositor? compositor = visual.Compositor;

        // CRITICAL: Make the animation grow from the center, not the top-left.
        visual.CenterPoint = new Vector3((float)item.ActualWidth / 2, (float)item.ActualHeight / 2, 0);

        var duration = TimeSpan.FromMilliseconds(600);

        // A nice springy easing function
        var ease = compositor.CreateCubicBezierEasingFunction(new Vector2(0.2f, 1.0f), new Vector2(0.4f, 1.0f));

        var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
        scaleAnimation.InsertKeyFrame(0.0f, new Vector3(1.0f, 1.0f, 1.0f), ease); // Start at normal size
        scaleAnimation.InsertKeyFrame(1.0f, new Vector3(1.1f, 1.1f, 1.0f), ease); // End 10% larger
        scaleAnimation.Duration = duration;

        visual.StartAnimation("Scale", scaleAnimation);
    }

    private void AnimateItemDeselected(WinUIControls.ListViewItem item)
    {
        var visual = ElementCompositionPreview.GetElementVisual(item);
        var compositor = visual.Compositor;

        visual.CenterPoint = new Vector3((float)item.ActualWidth / 2, (float)item.ActualHeight / 2, 0);

        var duration = TimeSpan.FromMilliseconds(500);
        var ease = compositor.CreateCubicBezierEasingFunction(new Vector2(0.8f, 0.0f), new Vector2(0.8f, 0.0f));

        var scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
        // We only need to animate it back to its final state.
        scaleAnimation.InsertKeyFrame(1.0f, new Vector3(1.0f, 1.0f, 1.0f), ease); // End at normal size
        scaleAnimation.Duration = duration;
        //scaleAnimation.EasingFunction = ease;

        visual.StartAnimation("Scale", scaleAnimation);
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
        SearchSongSB.Focus();
        await Task.WhenAll(
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

    private void AllLyricsColView_SelectionChanged(object sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    {

    }

    private void AllLyricsColView_SelectionChanged_1(object sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    {
        var newItem = e.CurrentSelection;
        if (newItem.Count > 0)
        {

            AllLyricsColView.ScrollTo(item: newItem[0], ScrollToPosition.Start, animate: true);
        }
    }

    private async void AllEvents_Clicked(object sender, EventArgs e)
    {

        Debug.WriteLine(AllEventsColView.ItemsSource);
        if (!AllEventsBorder.IsVisible)
        {
            await Task.WhenAll(AllEventsBorder.AnimateFadeInFront(400), StatsView.AnimateFadeOutBack(400), SongsColView.AnimateFadeOutBack(400));

        }
        else
        {


        }


    }

    private void MiddleClickGest_PointerReleased(object sender, PointerEventArgs e)
    {
        Microsoft.UI.Input.PointerDeviceType ee = e.PlatformArgs.PointerRoutedEventArgs.Pointer.PointerDeviceType;
        Windows.System.VirtualKeyModifiers ewe = e.PlatformArgs.PointerRoutedEventArgs.KeyModifiers;
        var ewae = e.PlatformArgs.PointerRoutedEventArgs.OriginalSource;
        var qewe = e.PlatformArgs.PointerRoutedEventArgs.Pointer;
        if (ewe==Windows.System.VirtualKeyModifiers.Control && ee==Microsoft.UI.Input.PointerDeviceType.Mouse)
        {
            MyViewModel.SearchSongSB_TextChanged($"genre:)
        }
    }

    private void ArtistSfEffectsView_TouchUp(object sender, EventArgs e)
    {

    }
}
