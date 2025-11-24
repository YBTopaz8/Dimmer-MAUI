using Dimmer.Data.ModelView;

namespace DimmerTQLUnitTest;

public static class TestData
{
    public static readonly List<SongModelView> AllSongs;

    static TestData()
    {
        AllSongs = new List<SongModelView>
        {
            new()
            {
                Title = "The Pot",
                OtherArtistsName = "Tool",
                AlbumName = "10,000 Days",
                GenreName = "Progressive Metal",
                ReleaseYear = 2006,
                IsFavorite = true,
                HasLyrics = true,
                DurationInSeconds = 381, // 6:21
                DateCreated = DateTimeOffset.UtcNow.AddYears(-5),
                LastPlayed = DateTimeOffset.UtcNow.AddMonths(-6)
            },
            new()
            {
                Title = "Smells Like Teen Spirit",
                OtherArtistsName = "Nirvana",
                AlbumName = "Nevermind",
                GenreName = "Grunge",
                ReleaseYear = 1991,
                IsFavorite = false,
                HasLyrics = true,
                DurationInSeconds = 301, // 5:01
                DateCreated = DateTimeOffset.UtcNow.AddYears(-10),
                LastPlayed = DateTimeOffset.UtcNow.AddYears(-2)
            },
            new()
            {
                Title = "Bohemian Rhapsody",
                OtherArtistsName = "Queen",
                AlbumName = "A Night at the Opera",
                GenreName = "Rock",
                ReleaseYear = 1975,
                IsFavorite = true,
                HasLyrics = false, // Intentionally false for testing
                DurationInSeconds = 355, // 5:55
                DateCreated = DateTimeOffset.UtcNow.AddYears(-3),
                LastPlayed = null // Never played
            },
            new()
            {
                Title = "Around the World",
                OtherArtistsName = "Daft Punk",
                AlbumName = "Homework",
                GenreName = "Electronic",
                ReleaseYear = 1997,
                IsFavorite = true,
                HasLyrics = true,
                DurationInSeconds = 429, // 7:09
                DateCreated = DateTimeOffset.UtcNow.AddDays(-50),
                LastPlayed = DateTimeOffset.UtcNow.AddDays(-10)
            },
            new()
            {
                Title = "A Title with \"Quotes\"", // For testing escaped strings
                OtherArtistsName = "Test Artist",
                AlbumName = "Test Album",
                GenreName = "Test",
                ReleaseYear = 2023,
                IsFavorite = false,
                HasLyrics = false,
                DurationInSeconds = 120,
                DateCreated = DateTimeOffset.UtcNow.AddDays(-1), // Added yesterday
                LastPlayed = DateTimeOffset.UtcNow.AddHours(-2) // Played today in the evening/afternoon
            }
        };
    }
}
