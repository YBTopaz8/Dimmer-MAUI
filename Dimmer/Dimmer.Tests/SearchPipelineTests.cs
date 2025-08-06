//// In Dimmer.Tests/SearchPipelineTests.cs

//using Dimmer.Data.ModelView;
//using Dimmer.DimmerSearch;
//using Dimmer.DimmerSearch.Exceptions;

//using DynamicData;

//using Microsoft.Reactive.Testing;

//using System.Collections.ObjectModel;
//using System.Reactive.Concurrency;
//using System.Reactive.Disposables;
//using System.Reactive.Linq;
//using System.Reactive.Subjects;

//using Xunit;

//namespace Dimmer.Tests.SearchPipelineTests;
//// This record is a necessary helper to pass parsed components through the pipeline.
//// It's good practice to define it within the test file if it's only used for testing.
//public record QueryComponents(
//    Func<SongModelView, bool> Predicate,
//    IComparer<SongModelView> Comparer,
//    LimiterClause? Limiter
//);


//public record TestSong(string Title, string Artist, string Album, int Year, int Bpm);

//public class SearchPipelineTests : IDisposable
//{
//    // =====================================================================
//    // These are the "probes" we will use to control and observe the pipeline.
//    // =====================================================================

//    // INPUTS
//    private readonly SourceList<SongModelView> _songSource;
//    private readonly BehaviorSubject<string> _searchQuerySubject;

//    // OUTPUTS - These are what our tests will assert against.
//    private ReadOnlyObservableCollection<SongModelView> _searchResults;
//    private string _searchErrorMessage;

//    // UTILITIES
//    private readonly CompositeDisposable _disposables = new();
//    private readonly TestScheduler _scheduler;

//    public SearchPipelineTests()
//    {
//        // ARRANGE - Create the predictable test data source.
//        var testSongs = new[]
//        {
//            new TestSong("One", "Metallica", "And Justice for All", 1988, 108),
//            new TestSong("Enter Sandman", "Metallica", "The Black Album", 1991, 123),
//            new TestSong("Bohemian Rhapsody", "Queen", "A Night at the Opera", 1975, 72),
//            new TestSong("Another One Bites the Dust", "Queen", "The Game", 1980, 110),
//            new TestSong("Smells Like Teen Spirit", "Nirvana", "Nevermind", 1991, 117),
//            new TestSong("Come As You Are", "Nirvana", "Nevermind", 1991, 120),
//            new TestSong("Stairway to Heaven", "Led Zeppelin", "Led Zeppelin IV", 1971, 82),
//            new TestSong("Losing My Religion", "R.E.M.", "Out of Time", 1991, 126),
//            new TestSong("Radio Free Europe", "R.E.M.", "Murmur", 1983, 150),
//            new TestSong("Yesterday", "The Beatles", "Help!", 1965, 97)
//        };

//        _songSource = new SourceList<SongModelView>();
//        _songSource.AddRange(testSongs.Select(s => new SongModelView { Title = s.Title, ArtistName = s.Artist, AlbumName = s.Album, ReleaseYear = s.Year, BPM = s.Bpm }));

//        _searchQuerySubject = new BehaviorSubject<string>("");
//        _scheduler = new TestScheduler();
//    }

//    /// <summary>
//    /// This method replicates the exact reactive pipeline from your BaseViewModel.
//    /// It's called by each test to set up the environment.
//    /// </summary>
//    private void RunPipeline()
//    {
//        // 1. CONTROL SUBJECTS: These are the inputs to the data pipeline.
//        var filterPredicate = new BehaviorSubject<Func<SongModelView, bool>>(song => true);
//        var sortComparer = new BehaviorSubject<IComparer<SongModelView>>(new SongModelViewComparer(null));
//        var limiterClause = new BehaviorSubject<LimiterClause?>(null);

//        // 2. CONTROL PIPELINE: Parses the query and updates the control subjects.
//        _searchQuerySubject
//            .Throttle(TimeSpan.FromMilliseconds(300), _scheduler) // Use the test scheduler
//            .Select(query =>
//            {
//                if (string.IsNullOrWhiteSpace(query))
//                    return (Components: new QueryComponents(p => true, new SongModelViewComparer(null), null), ErrorMessage: (string?)null);
//                try
//                {
//                    var orchestrator = new MetaParser(query);
//                    var components = new QueryComponents(
//                        orchestrator.CreateMasterPredicate(),
//                        orchestrator.CreateSortComparer(),
//                        orchestrator.CreateLimiterClause());
//                    return (Components: components, ErrorMessage: (string?)null);
//                }
//                catch (ParsingException ex)
//                {
//                    return (Components: (QueryComponents?)null, ErrorMessage: ex.Message);
//                }
//                catch (Exception)
//                {
//                    return (Components: (QueryComponents?)null, ErrorMessage: "An unexpected error occurred.");
//                }
//            })
//            .ObserveOn(_scheduler)
//            .Subscribe(result =>
//            {
//                _searchErrorMessage = result.ErrorMessage ?? "";
//                if (result.Components is not null)
//                {
//                    filterPredicate.OnNext(result.Components.Predicate ?? (p => true));
//                    sortComparer.OnNext(result.Components.Comparer ?? new SongModelViewComparer(null));
//                    limiterClause.OnNext(result.Components.Limiter);
//                }
//            })
//            .DisposeWith(_disposables);

//        // 3. COMBINED CONTROLS: Package the control subjects for the data pipeline.
//        var controlPipeline = Observable.CombineLatest(
//            filterPredicate, sortComparer, limiterClause,
//            (p, s, l) => new { Predicate = p, Comparer = s, Limiter = l }
//        );

//        // 4. DATA PIPELINE: Reads from the master source, applies controls, and populates a dedicated results holder.
//        var searchResultsHolder = new SourceList<SongModelView>();

//        _songSource.Connect()
//            .ToCollection()
//            .ObserveOn(_scheduler)
//            .CombineLatest(controlPipeline, (songs, controls) => new { songs, controls })
//            .Select(data =>
//            {
//                var filtered = data.songs.Where(data.controls.Predicate);
//                IOrderedEnumerable<SongModelView> sorted;
//                if (data.controls.Limiter?.Type == LimiterType.Random)
//                {
//                    var random = new Random(0); // Use a fixed seed for predictable tests
//                    sorted = filtered.OrderBy(x => random.Next());
//                }
//                else if (data.controls.Limiter?.Type == LimiterType.Last)
//                {
//                    var invertedComparer = (data.controls.Comparer as SongModelViewComparer)?.Inverted() ?? data.controls.Comparer;
//                    sorted = filtered.OrderBy(x => x, invertedComparer);
//                }
//                else
//                {
//                    sorted = filtered.OrderBy(x => x, data.controls.Comparer);
//                }
//                return sorted.Take(data.controls.Limiter?.Count ?? int.MaxValue).ToList();
//            })
//            .ObserveOn(_scheduler)
//            .Subscribe(newList => searchResultsHolder.Edit(updater =>
//            {
//                updater.Clear();
//                updater.AddRange(newList);
//            }))
//            .DisposeWith(_disposables);

//        // 5. BINDING: The final step binds the holder to our public-facing collection.
//        searchResultsHolder.Connect()
//            .ObserveOn(_scheduler)
//            .Bind(out _searchResults)
//            .Subscribe()
//            .DisposeWith(_disposables);
//    }

//    [Fact]
//    public void Simple_Text_Search_Filters_Correctly()
//    {
//        // ARRANGE
//        RunPipeline();

//        // ACT
//        _searchQuerySubject.OnNext("One");
//        _scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

//        // ASSERT
//        Assert.Equal(2, _searchResults.Count); // "One" and "Another One Bites the Dust"
//        Assert.Contains(_searchResults, s => s.Title == "One");
//        Assert.Contains(_searchResults, s => s.Title == "Another One Bites the Dust");
//    }

//    [Fact]
//    public void Invalid_Query_Sets_Error_Message_And_Does_Not_Change_Results()
//    {
//        // ARRANGE
//        RunPipeline();

//        // ACT 1: Set a valid initial state
//        _searchQuerySubject.OnNext("Nirvana");
//        _scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

//        // ASSERT 1: Check initial state
//        Assert.Equal(2, _searchResults.Count);
//        Assert.Empty(_searchErrorMessage);

//        // ACT 2: Enter an invalid query
//        _searchQuerySubject.OnNext("year:>");
//        _scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

//        // ASSERT 2: Check error state
//        Assert.NotEmpty(_searchErrorMessage);
//        Assert.Contains("Unexpected token", _searchErrorMessage);
//        Assert.Equal(2, _searchResults.Count); // Results MUST remain unchanged from the last valid state
//    }

//    [Fact]
//    public void Complex_Query_With_Sorting_And_Limiting_Works()
//    {
//        // ARRANGE
//        RunPipeline();

//        // ACT: Find all songs from 1991, sort them by BPM ascending, and take the first 2.
//        _searchQuerySubject.OnNext("year:1991 asc bpm first:2");
//        _scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

//        // ASSERT
//        Assert.Equal(2, _searchResults.Count);
//        Assert.Equal("Smells Like Teen Spirit", _searchResults[0].Title); // BPM 117
//        Assert.Equal("Come As You Are", _searchResults[1].Title);       // BPM 120
//    }

//    [Fact]
//    public void Sort_With_Last_Keyword_Correctly_Inverts_Order()
//    {
//        // ARRANGE
//        RunPipeline();

//        // ACT: Find all songs from 1991, sort by BPM ascending, but take the LAST 2.
//        _searchQuerySubject.OnNext("year:1991 asc bpm last:2");
//        _scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

//        // ASSERT: The order should be inverted from the 'first:2' test.
//        Assert.Equal(2, _searchResults.Count);
//        Assert.Equal("Losing My Religion", _searchResults[0].Title); // BPM 126 (highest)
//        Assert.Equal("Enter Sandman", _searchResults[1].Title);      // BPM 123 (second highest)
//    }

//    [Fact]
//    public void Exclusion_Query_Correctly_Removes_Items()
//    {
//        // ARRANGE
//        RunPipeline();

//        // ACT: Find all Metallica songs, but exclude the one from 1988.
//        _searchQuerySubject.OnNext("Metallica exclude year:1988");
//        _scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

//        // ASSERT
//        Assert.Single(_searchResults);
//        Assert.Equal("Enter Sandman", _searchResults[0].Title);
//    }

//    [Fact]
//    public void Clearing_Query_Returns_All_Items()
//    {
//        // ARRANGE
//        RunPipeline();

//        // ACT 1: Perform a search
//        _searchQuerySubject.OnNext("Queen");
//        _scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);
//        Assert.Equal(2, _searchResults.Count); // Ensure the filter was applied

//        // ACT 2: Clear the search query
//        _searchQuerySubject.OnNext("");
//        _scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

//        // ASSERT
//        Assert.Empty(_searchErrorMessage);
//        Assert.Equal(10, _searchResults.Count); // All songs should be returned
//    }

//    public void Dispose()
//    {
//        _disposables.Dispose();
//        _songSource.Dispose();
//    }
//}