using Dimmer.DimmerSearch.TQL;

namespace DimmerTQLUnitTest;

[TestClass]
public class AstParserTests
{
    [TestMethod]
    public void Parse_ImplicitAnd_CreatesCorrectLogicalNode()
    {
        var tokens = Lexer.Tokenize("artist:Tool year:>2000");
        var parser = new AstParser(tokens);

        var rootNode = parser.Parse();

        Assert.IsInstanceOfType<LogicalNode>(rootNode);
        var logicalNode = (LogicalNode)rootNode;
        Assert.AreEqual(LogicalOperator.And, logicalNode.Operator);
        Assert.IsInstanceOfType<ClauseNode>(logicalNode.Left);
        Assert.IsInstanceOfType<ClauseNode>(logicalNode.Right);
    }

    [TestMethod]
    public void Parse_OperatorPrecedence_AndIsHigherThanOr()
    {
        // Should be parsed as: artist:Queen OR (genre:Grunge AND year:>1990)
        var tokens = Lexer.Tokenize("artist:Queen or genre:Grunge and year:>1990");
        var parser = new AstParser(tokens);

        var rootNode = parser.Parse();

        // Root should be OR
        Assert.IsInstanceOfType<LogicalNode>(rootNode);
        var orNode = (LogicalNode)rootNode;
        Assert.AreEqual(LogicalOperator.Or, orNode.Operator);

        // Left side of OR should be the simple 'artist' clause
        Assert.IsInstanceOfType<ClauseNode>(orNode.Left);
        Assert.AreEqual("artist", ((ClauseNode)orNode.Left).Field);

        // Right side of OR should be an AND node
        Assert.IsInstanceOfType<LogicalNode>(orNode.Right);
        var andNode = (LogicalNode)orNode.Right;
        Assert.AreEqual(LogicalOperator.And, andNode.Operator);
    }

    [TestMethod]
    public void Parse_Parentheses_OverridePrecedence()
    {
        // Should be parsed as: (artist:Queen OR genre:Grunge) AND year:>1990
        var tokens = Lexer.Tokenize("(artist:Queen or genre:Grunge) and year:>1990");
        var parser = new AstParser(tokens);

        var rootNode = parser.Parse();

        // Root should be AND
        Assert.IsInstanceOfType<LogicalNode>(rootNode);
        var andNode = (LogicalNode)rootNode;
        Assert.AreEqual(LogicalOperator.And, andNode.Operator);

        // Left side of AND should be an OR node
        Assert.IsInstanceOfType<LogicalNode>(andNode.Left);
        var orNode = (LogicalNode)andNode.Left;
        Assert.AreEqual(LogicalOperator.Or, orNode.Operator);

        // Right side of AND should be the simple 'year' clause
        Assert.IsInstanceOfType<ClauseNode>(andNode.Right);
    }

    [TestMethod]
    public void Parse_FuzzyDateClause_CreatesFuzzyDateNode()
    {
        var tokens = Lexer.Tokenize("played:ago(\"30d\")");
        var parser = new AstParser(tokens);

        var rootNode = parser.Parse();

        Assert.IsInstanceOfType<FuzzyDateNode>(rootNode);
        var fuzzyNode = (FuzzyDateNode)rootNode;
        Assert.AreEqual("played", fuzzyNode.DateField);
        Assert.AreEqual(FuzzyDateNode.Qualifier.Ago, fuzzyNode.Type);
        Assert.AreEqual(TimeSpan.FromDays(30), fuzzyNode.OlderThan);
    }
}