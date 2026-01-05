using Dimmer.Data.ModelView;
using Dimmer.DimmerSearch.TQL;
using Dimmer.DimmerSearch.TQLActions;

namespace Dimmer.Tests;

/// <summary>
/// Tests for accent-insensitive (ICU-style collation) string matching in TQL queries.
/// These tests verify that queries like "doree" match values like "dorée" (keyboard-agnostic matching).
/// </summary>
public class AccentInsensitiveCollationTests
{
    private readonly AstEvaluator _evaluator;

    public AccentInsensitiveCollationTests()
    {
        _evaluator = new AstEvaluator();
    }

    private SongModelView CreateTestSong(string artist = "Julien Dorée", string title = "Les Champs-Élysées", string album = "Bichon")
    {
        return new SongModelView
        {
            ArtistName = artist,
            Title = title,
            AlbumName = album
        };
    }

    [Fact]
    public void Artist_ContainsOperator_WithAccentedValue_MatchesUnaccentedQuery()
    {
        // Arrange
        var song = CreateTestSong(artist: "Julien Dorée");
        var clause = new ClauseNode("artist", ":", "doree");

        // Act
        var predicate = _evaluator.CreatePredicate(clause);
        var result = predicate(song);

        // Assert
        Assert.True(result, "Query 'artist:doree' should match 'Julien Dorée'");
    }

    [Fact]
    public void Artist_ContainsOperator_WithAccentedValue_MatchesPartialUnaccentedQuery()
    {
        // Arrange
        var song = CreateTestSong(artist: "Julien Dorée");
        var clause = new ClauseNode("artist", ":", "dor");

        // Act
        var predicate = _evaluator.CreatePredicate(clause);
        var result = predicate(song);

        // Assert
        Assert.True(result, "Query 'artist:dor' should match 'Julien Dorée'");
    }

    [Fact]
    public void Artist_ContainsOperator_WithAccentedValue_MatchesAccentedQuery()
    {
        // Arrange
        var song = CreateTestSong(artist: "Julien Dorée");
        var clause = new ClauseNode("artist", ":", "dorée");

        // Act
        var predicate = _evaluator.CreatePredicate(clause);
        var result = predicate(song);

        // Assert
        Assert.True(result, "Query 'artist:dorée' should match 'Julien Dorée'");
    }

    [Fact]
    public void Artist_EqualsOperator_WithAccentedValue_MatchesUnaccentedQuery()
    {
        // Arrange
        var song = CreateTestSong(artist: "Dorée");
        var clause = new ClauseNode("artist", "=", "doree");

        // Act
        var predicate = _evaluator.CreatePredicate(clause);
        var result = predicate(song);

        // Assert
        Assert.True(result, "Query 'artist=doree' should match 'Dorée' (exact match, accent-insensitive)");
    }

    [Fact]
    public void Artist_StartsWithOperator_WithAccentedValue_MatchesUnaccentedQuery()
    {
        // Arrange
        var song = CreateTestSong(artist: "Julien Dorée");
        var clause = new ClauseNode("artist", "^", "julien dor");

        // Act
        var predicate = _evaluator.CreatePredicate(clause);
        var result = predicate(song);

        // Assert
        Assert.True(result, "Query 'artist^julien dor' should match 'Julien Dorée' (starts with, accent-insensitive)");
    }

    [Fact]
    public void Artist_EndsWithOperator_WithAccentedValue_MatchesUnaccentedQuery()
    {
        // Arrange
        var song = CreateTestSong(artist: "Julien Dorée");
        var clause = new ClauseNode("artist", "$", "doree");

        // Act
        var predicate = _evaluator.CreatePredicate(clause);
        var result = predicate(song);

        // Assert
        Assert.True(result, "Query 'artist$doree' should match 'Julien Dorée' (ends with, accent-insensitive)");
    }

    [Fact]
    public void Title_WithMultipleAccents_MatchesUnaccentedQuery()
    {
        // Arrange
        var song = CreateTestSong(title: "Les Champs-Élysées");
        var clause = new ClauseNode("t", ":", "elysees");

        // Act
        var predicate = _evaluator.CreatePredicate(clause);
        var result = predicate(song);

        // Assert
        Assert.True(result, "Query 't:elysees' should match 'Les Champs-Élysées'");
    }

    [Fact]
    public void Album_WithAccent_MatchesUnaccentedQuery()
    {
        // Arrange
        var song = CreateTestSong(album: "Bichôn");
        var clause = new ClauseNode("album", ":", "bichon");

        // Act
        var predicate = _evaluator.CreatePredicate(clause);
        var result = predicate(song);

        // Assert
        Assert.True(result, "Query 'album:bichon' should match 'Bichôn'");
    }

    [Fact]
    public void Query_WithVariousAccents_MatchesUnaccentedQuery()
    {
        // Test with various diacritics: à, é, è, ê, ë, ï, ô, ù, û, ü, ç, ñ
        var song = CreateTestSong(artist: "Café");
        var clause = new ClauseNode("artist", ":", "cafe");
        var predicate = _evaluator.CreatePredicate(clause);
        Assert.True(predicate(song), "Query 'artist:cafe' should match 'Café'");

        song = CreateTestSong(artist: "Peña");
        clause = new ClauseNode("artist", ":", "pena");
        predicate = _evaluator.CreatePredicate(clause);
        Assert.True(predicate(song), "Query 'artist:pena' should match 'Peña'");

        song = CreateTestSong(artist: "Naïve");
        clause = new ClauseNode("artist", ":", "naive");
        predicate = _evaluator.CreatePredicate(clause);
        Assert.True(predicate(song), "Query 'artist:naive' should match 'Naïve'");

        song = CreateTestSong(artist: "Über");
        clause = new ClauseNode("artist", ":", "uber");
        predicate = _evaluator.CreatePredicate(clause);
        Assert.True(predicate(song), "Query 'artist:uber' should match 'Über'");
    }

    [Fact]
    public void ReverseCase_UnaccentedValue_MatchesAccentedQuery()
    {
        // Arrange - Song has unaccented value, query has accents
        var song = CreateTestSong(artist: "Julien Doree");
        var clause = new ClauseNode("artist", ":", "dorée");

        // Act
        var predicate = _evaluator.CreatePredicate(clause);
        var result = predicate(song);

        // Assert
        Assert.True(result, "Query 'artist:dorée' should match 'Julien Doree' (reverse case)");
    }

    [Fact]
    public void CaseInsensitivity_StillWorks()
    {
        // Arrange
        var song = CreateTestSong(artist: "Julien Dorée");
        var clause = new ClauseNode("artist", ":", "DOREE");

        // Act
        var predicate = _evaluator.CreatePredicate(clause);
        var result = predicate(song);

        // Assert
        Assert.True(result, "Query 'artist:DOREE' should match 'Julien Dorée' (case-insensitive)");
    }

    [Fact]
    public void NoMatch_WhenQueryDoesNotMatchValue()
    {
        // Arrange
        var song = CreateTestSong(artist: "Julien Dorée");
        var clause = new ClauseNode("artist", ":", "smith");

        // Act
        var predicate = _evaluator.CreatePredicate(clause);
        var result = predicate(song);

        // Assert
        Assert.False(result, "Query 'artist:smith' should NOT match 'Julien Dorée'");
    }

    [Fact]
    public void CollationHelper_Contains_WorksCorrectly()
    {
        Assert.True(CollationHelper.Contains("Julien Dorée", "doree"));
        Assert.True(CollationHelper.Contains("Julien Dorée", "dor"));
        Assert.True(CollationHelper.Contains("Julien Dorée", "dorée"));
        Assert.True(CollationHelper.Contains("café", "cafe"));
        Assert.True(CollationHelper.Contains("naïve", "naive"));
        Assert.False(CollationHelper.Contains("Julien Dorée", "smith"));
    }

    [Fact]
    public void CollationHelper_StartsWith_WorksCorrectly()
    {
        Assert.True(CollationHelper.StartsWith("Julien Dorée", "julien"));
        Assert.True(CollationHelper.StartsWith("Julien Dorée", "Julien Dor"));
        Assert.True(CollationHelper.StartsWith("café", "cafe"));
        Assert.False(CollationHelper.StartsWith("Julien Dorée", "Dorée"));
        Assert.False(CollationHelper.StartsWith("Julien Dorée", "smith"));
    }

    [Fact]
    public void CollationHelper_EndsWith_WorksCorrectly()
    {
        Assert.True(CollationHelper.EndsWith("Julien Dorée", "doree"));
        Assert.True(CollationHelper.EndsWith("Julien Dorée", "dorée"));
        Assert.True(CollationHelper.EndsWith("café", "cafe"));
        Assert.False(CollationHelper.EndsWith("Julien Dorée", "julien"));
        Assert.False(CollationHelper.EndsWith("Julien Dorée", "smith"));
    }

    [Fact]
    public void CollationHelper_Equals_WorksCorrectly()
    {
        Assert.True(CollationHelper.Equals("Dorée", "doree"));
        Assert.True(CollationHelper.Equals("café", "cafe"));
        Assert.True(CollationHelper.Equals("naïve", "naive"));
        Assert.True(CollationHelper.Equals("Café", "CAFE"));
        Assert.False(CollationHelper.Equals("Dorée", "Julien Dorée"));
        Assert.False(CollationHelper.Equals("café", "cafe shop"));
    }
}
