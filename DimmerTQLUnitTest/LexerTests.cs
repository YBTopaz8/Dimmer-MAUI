using Dimmer.DimmerSearch.TQL;

namespace DimmerTQLUnitTest;

[TestClass]
public class LexerTests
{
    [TestMethod]
    public void Tokenize_SimpleClause_ReturnsCorrectTokens()
    {
        var tokens = Lexer.Tokenize("artist:tool");

        Assert.HasCount(4, tokens); // identifier, colon, identifier, eof
        Assert.AreEqual(TokenType.Identifier, tokens[0].Type);
        Assert.AreEqual("artist", tokens[0].Text);
        Assert.AreEqual(TokenType.Colon, tokens[1].Type);
        Assert.AreEqual(TokenType.Identifier, tokens[2].Type);
        Assert.AreEqual("tool", tokens[2].Text);
        Assert.AreEqual(TokenType.EndOfFile, tokens[3].Type);
    }

    [TestMethod]
    public void Tokenize_QuotedString_HandlesSpaces()
    {
        var tokens = Lexer.Tokenize("album:\"10,000 Days\"");

        Assert.HasCount(4, tokens);
        Assert.AreEqual(TokenType.StringLiteral, tokens[2].Type);
        Assert.AreEqual("10,000 Days", tokens[2].Text);
    }

    [TestMethod]
    public void Tokenize_OperatorsAndKeywords_AreCorrectlyIdentified()
    {
        var tokens = Lexer.Tokenize("year:>2000 and (fav:true or not haslyrics:false)");

        var expectedTypes = new[]
        {
            TokenType.Identifier, TokenType.Colon, TokenType.GreaterThan, TokenType.Number,
            TokenType.And, TokenType.LeftParen, TokenType.Identifier, TokenType.Colon,
            TokenType.Identifier, TokenType.Or, TokenType.Not, TokenType.Identifier,
            TokenType.Colon, TokenType.Identifier, TokenType.RightParen, TokenType.EndOfFile
        };

        Assert.HasCount(expectedTypes.Length, tokens);
        for (int i = 0; i < expectedTypes.Length; i++)
        {
            Assert.AreEqual(expectedTypes[i], tokens[i].Type);
        }
    }
}