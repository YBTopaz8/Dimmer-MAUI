using Dimmer.Data.ModelView;
using Dimmer.DimmerSearch.TQL;
using Dimmer.DimmerSearch.TQL.RealmSection;

namespace DimmerTQLUnitTest;

[TestClass]
public class HybridExecutionTests
{
    private List<SongModelView> _allSongs = null!;

    [TestInitialize]
    public void Setup()
    {
        _allSongs = TestData.AllSongs;
    }

    private void AssertQuery(string query, string expectedRql, Func<SongModelView, bool> expectedFilter)
    {
        // 1. Parse the query to get the AST
        var plan = MetaParser.Parse(query);

        // 2. Test the RQL Generator
        Assert.AreEqual(expectedRql, plan.RqlFilter, "Generated RQL does not match expected RQL.");

        // 3. Test the AST Evaluator (in-memory predicate)
        var actualResults = _allSongs.Where(plan.InMemoryPredicate).ToList();
        var expectedResults = _allSongs.Where(expectedFilter).ToList();

        CollectionAssert.AreEquivalent(expectedResults, actualResults, "In-memory evaluation returned a different result set.");
    }

    [TestMethod]
    public void Execute_SimpleTextQuery_IsCorrect()
    {
        AssertQuery(
            query: "artist:Tool",
            expectedRql: "OtherArtistsName CONTAINS[c] 'Tool'",
            expectedFilter: s => s.OtherArtistsName == "Tool"
        );
    }

    [TestMethod]
    public void Execute_NumericQuery_IsCorrect()
    {
        AssertQuery(
            query: "year:>2000",
            expectedRql: "ReleaseYear > 2000",
            expectedFilter: s => s.ReleaseYear > 2000
        );
    }

    [TestMethod]
    public void Execute_BooleanQuery_IsCorrect()
    {
        AssertQuery(
            query: "fav:true",
            expectedRql: "IsFavorite == true",
            expectedFilter: s => s.IsFavorite
        );
    }

    [TestMethod]
    public void Execute_FuzzyDateNeverPlayed_IsCorrect()
    {
        AssertQuery(
            query: "played:never",
            expectedRql: "LastPlayed == null",
            expectedFilter: s => s.LastPlayed == null
        );
    }

    [TestMethod]
    public void Execute_FuzzyDateAgo_IsCorrect()
    {
        var boundary = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(365));
        AssertQuery(
            query: "played:ago(\"1y\")", // "played in the last year"
            expectedRql: $"LastPlayed > {RqlGenerator.FormatValue(boundary, FieldType.Date)}",
            expectedFilter: s => s.LastPlayed > boundary
        );
    }

    [TestMethod]
    public void Execute_InMemoryOnlyQuery_GeneratesTruePredicateAndFiltersInMemory()
    {
        // DaypartNode is in-memory only.
        var query = "played:morning";
        var plan = MetaParser.Parse(query);

        // RQL Generator should ignore it and match all
        Assert.AreEqual("TRUEPREDICATE", plan.RqlFilter);

        // In-memory evaluator should apply the logic
        var results = _allSongs.Where(plan.InMemoryPredicate).ToList();

        // No songs in our test data were played in the "morning" (6am-12pm)
        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void Execute_LogicalOrQuery_IsCorrect()
    {
        AssertQuery(
            query: "artist:Nirvana or genre:Electronic",
            expectedRql: "(OtherArtistsName CONTAINS[c] 'Nirvana' OR GenreName CONTAINS[c] 'Electronic')",
            expectedFilter: s => s.OtherArtistsName == "Nirvana" || s.GenreName == "Electronic"
        );
    }
}