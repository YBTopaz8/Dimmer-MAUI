using Dimmer.DimmerSearch.TQL;

namespace DimmerTQLUnitTest;

[TestClass]
public class MetaParserTests
{
    [TestMethod]
    public void Parse_QueryWithSort_PopulatesSortDescriptions()
    {
        var plan = MetaParser.Parse("genre:rock asc year");

        Assert.IsNotNull(plan.SortDescriptions);
        Assert.HasCount(1, plan.SortDescriptions);
        Assert.AreEqual("ReleaseYear", plan.SortDescriptions[0].PropertyName);
        Assert.AreEqual(SortDirection.Ascending, plan.SortDescriptions[0].Direction);
    }

    [TestMethod]
    public void Parse_QueryWithLimit_PopulatesLimiterClause()
    {
        var plan = MetaParser.Parse("fav:true first 5");

        Assert.IsNotNull(plan.Limiter);
        Assert.AreEqual(LimiterType.First, plan.Limiter.Type);
        Assert.AreEqual(5, plan.Limiter.Count);
    }

    [TestMethod]
    public void Parse_QueryWithCommand_PopulatesCommandNode()
    {
        var plan = MetaParser.Parse("artist:Tool >> savepl My Tool Playlist!");

        Assert.IsNotNull(plan.CommandNode);
        var commandNode = (CommandNode)plan.CommandNode;

        Assert.AreEqual("save", commandNode.Command);
        Assert.IsTrue(commandNode.Arguments.ContainsKey("playlistName"));
        Assert.AreEqual("My Tool Playlist!", commandNode.Arguments["playlistName"]);
    }

    [TestMethod]
    public void Parse_ComplexQuery_SeparatesAllPartsCorrectly()
    {
        var plan = MetaParser.Parse("fav:true or year:>2000 desc added first 10 >> addnext!");

        // Check filter part
        Assert.AreEqual("(IsFavorite == true OR ReleaseYear > 2000)", plan.RqlFilter);

        // Check sort part
        Assert.HasCount(1, plan.SortDescriptions);
        Assert.AreEqual("DateCreated", plan.SortDescriptions[0].PropertyName);
        Assert.AreEqual(SortDirection.Descending, plan.SortDescriptions[0].Direction);

        // Check limit part
        Assert.IsNotNull(plan.Limiter);
        Assert.AreEqual(LimiterType.First, plan.Limiter.Type);
        Assert.AreEqual(10, plan.Limiter.Count);

        // Check command part
        Assert.IsNotNull(plan.CommandNode);
        Assert.AreEqual("addnext", ((CommandNode)plan.CommandNode).Command);
    }
}