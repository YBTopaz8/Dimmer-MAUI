using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Charts;
/// <summary>
/// Provides static methods for calculating and aggregating song playback statistics.
/// </summary>
public static class SongStatisticsService
{

    #region --- Single Song Statistics ---

    /// <summary>
    /// 1. Gets the total number of times a song has been started.
    /// </summary>
    /// <param name="realm">The Realm instance.</param>
    /// <param name="songId">The ID of the song.</param>
    /// <returns>The total play count.</returns>
    public static int GetPlayCount(Realm realm, ObjectId songId)
    {
        var song = realm.Find<SongModel>(songId);
        return song?.PlayHistory.Count(e => e.PlayType == (int)PlayEventType.Play) ?? 0;
    }

    /// <summary>
    /// 2. Gets the total number of times a song has been skipped.
    /// </summary>
    /// <param name="realm">The Realm instance.</param>
    /// <param name="songId">The ID of the song.</param>
    /// <returns>The total skip count.</returns>
    public static int GetSkipCount(Realm realm, ObjectId songId)
    {
        var song = realm.Find<SongModel>(songId);
        return song?.PlayHistory.Count(e => e.PlayType == (int)PlayEventType.Skipped) ?? 0;
    }

    /// <summary>
    /// 3. Gets the total number of times a song has been played to completion.
    /// </summary>
    /// <param name="realm">The Realm instance.</param>
    /// <param name="songId">The ID of the song.</param>
    /// <returns>The total completion count.</returns>
    public static int GetCompletionCount(Realm realm, ObjectId songId)
    {
        var song = realm.Find<SongModel>(songId);
        // Uses either the dedicated flag or the event type for robustness.
        return song?.PlayHistory.Count(e => e.WasPlayCompleted || e.PlayType == (int)PlayEventType.Completed) ?? 0;
    }

    /// <summary>
    /// 4. Gets the total number of times a song has been paused.
    /// </summary>
    /// <param name="realm">The Realm instance.</param>
    /// <param name="songId">The ID of the song.</param>
    /// <returns>The total pause count.</returns>
    public static int GetPauseCount(Realm realm, ObjectId songId)
    {
        var song = realm.Find<SongModel>(songId);
        return song?.PlayHistory.Count(e => e.PlayType == (int)PlayEventType.Pause) ?? 0;
    }

    /// <summary>
    /// 5. Calculates the completion rate (completions / plays) for a song.
    /// </summary>
    /// <param name="realm">The Realm instance.</param>
    /// <param name="songId">The ID of the song.</param>
    /// <returns>A percentage (0-100) representing the completion rate. Returns 0 if never played.</returns>
    public static double GetCompletionRate(Realm realm, ObjectId songId)
    {
        var song = realm.Find<SongModel>(songId);
        if (song == null)
            return 0;

        var playCount = song.PlayHistory.Count(e => e.PlayType == (int)PlayEventType.Play);
        if (playCount == 0)
            return 0;

        var completionCount = GetCompletionCount(realm, songId);
        return (double)completionCount / playCount * 100.0;
    }

    /// <summary>
    /// 6. Gets the most recent time the song was played.
    /// </summary>
    /// <param name="realm">The Realm instance.</param>
    /// <param name="songId">The ID of the song.</param>
    /// <returns>The DateTimeOffset of the last play event, or null if never played.</returns>
    public static DateTimeOffset? GetLastPlayedDate(Realm realm, ObjectId songId)
    {
        var song = realm.Find<SongModel>(songId);
        return song?.PlayHistory
            .OrderByDescending(e => e.DatePlayed)
            .FirstOrDefault()?
            .DatePlayed;
    }

    /// <summary>
    /// 7. Gets data for a histogram of play counts per day for a specific song.
    /// Ideal for a bar chart showing play frequency over time.
    /// </summary>
    /// <param name="realm">The Realm instance.</param>
    /// <param name="songId">The ID of the song.</param>
    /// <returns>A list of data points where X is the Date and Y is the play count for that day.</returns>
    public static List<DataPoint<DateTime, int>> GetPlaysOverTimeHistogram(Realm realm, ObjectId songId)
    {
        var song = realm.Find<SongModel>(songId);
        if (song == null)
            return new List<DataPoint<DateTime, int>>();

        return song.PlayHistory.ToList()
            .Where(e => e.PlayType == (int)PlayEventType.Play)
            .GroupBy(e => e.DatePlayed.Date)
            .Select(g => new DataPoint<DateTime, int>(g.Key, g.Count()))
            .OrderBy(dp => dp.X)
            .ToList();
    }

    /// <summary>
    /// 8. Gets a list of positions (in seconds) where a song was skipped.
    /// Useful for a scatter plot or density plot to see where listeners lose interest.
    /// </summary>
    /// <param name="realm">The Realm instance.</param>
    /// <param name="songId">The ID of the song.</param>
    /// <returns>A list of song positions in seconds.</returns>
    public static List<double> GetSkipPositions(Realm realm, ObjectId songId)
    {
        var song = realm.Find<SongModel>(songId);
        if (song == null)
            return new List<double>();

        return song.PlayHistory
            .Where(e => e.PlayType == (int)PlayEventType.Skipped)
            .Select(e => e.PositionInSeconds)
            .ToList();
    }

    /// <summary>
    /// 9. Calculates the average listen duration before a skip or end of session.
    /// </summary>
    /// <param name="realm">The Realm instance.</param>
    /// <param name="songId">The ID of the song.</param>
    /// <returns>Average listen duration in seconds. Returns 0 if no relevant events.</returns>
    public static double GetAverageListenDuration(Realm realm, ObjectId songId)
    {
        var song = realm.Find<SongModel>(songId);
        var relevantEvents = song?.PlayHistory
            .Where(e => e.PlayType == (int)PlayEventType.Skipped || e.PlayType == (int)PlayEventType.Completed)
            .ToList();

        if (relevantEvents == null || relevantEvents.Count==0)
            return 0;

        return relevantEvents.Average(e => e.PositionInSeconds);
    }


    #endregion

    #region --- Aggregate (Multi-Song/Global) Statistics ---

    /// <summary>
    /// 10. Gets the total play count for all songs by a specific artist.
    /// </summary>
    /// <param name="realm">The Realm instance.</param>
    /// <param name="artistId">The ID of the artist.</param>
    /// <returns>The aggregated play count for the artist.</returns>
    public static int GetTotalPlaysForArtist(Realm realm, ObjectId artistId)
    {
        var artist = realm.Find<ArtistModel>(artistId);
        // This sums the play counts for each song linked to the artist.
        return artist?.Songs.Sum(s => s.PlayHistory.Count(e => e.PlayType == (int)PlayEventType.Play)) ?? 0;
    }

    /// <summary>
    /// 11. Gets the total play count for all songs in a specific album.
    /// </summary>
    /// <param name="realm">The Realm instance.</param>
    /// <param name="albumId">The ID of the album.</param>
    /// <returns>The aggregated play count for the album.</returns>
    public static int GetTotalPlaysForAlbum(Realm realm, ObjectId albumId)
    {
        var album = realm.Find<AlbumModel>(albumId);
        return album?.SongsInAlbum?.Sum(s => s.PlayHistory.Count(e => e.PlayType == (int)PlayEventType.Play)) ?? 0;
    }

    /// <summary>
    /// 12. Gets the top N most frequently played songs across the entire library.
    /// </summary>
    /// <param name="realm">The Realm instance.</param>
    /// <param name="count">The number of songs to return.</param>
    /// <returns>A list of SongAggregate records, ordered by play count descending.</returns>
    public static List<SongAggregate> GetTopNMostPlayedSongs(Realm realm, int count)
    {
        // This is more efficient than iterating every song.
        // We group the events first, then find the songs.
        var topSongIds = realm.All<DimmerPlayEvent>().AsEnumerable()
            .Where(e => e.PlayType == (int)PlayEventType.Play && e.SongId != null)
            .GroupBy(e => e.SongId)
            .Select(g => new { SongId = g.Key, PlayCount = g.Count() })
            .OrderByDescending(x => x.PlayCount)
            .Take(count)
            .ToList(); // Execute the query to get IDs and counts

        var results = new List<SongAggregate>();
        foreach (var item in topSongIds)
        {
            var song = realm.Find<SongModel>(item.SongId!.Value);
            if (song != null)
            {
                results.Add(new SongAggregate(song, item.PlayCount));
            }
        }
        return results;
    }

    /// <summary>
    /// 13. Gets the top N most frequently skipped songs.
    /// </summary>
    /// <param name="realm">The Realm instance.</param>
    /// <param name="count">The number of songs to return.</param>
    /// <returns>A list of SongAggregate records, ordered by skip count descending.</returns>
    public static List<SongAggregate> GetTopNMostSkippedSongs(Realm realm, int count)
    {
        var topSongIds = realm.All<DimmerPlayEvent>().AsEnumerable()
            .Where(e => e.PlayType == (int)PlayEventType.Skipped && e.SongId != null)
            .GroupBy(e => e.SongId)
            .Select(g => new { SongId = g.Key, SkipCount = g.Count() })
            .OrderByDescending(x => x.SkipCount)
            .Take(count)
            .ToList();

        var results = new List<SongAggregate>();
        foreach (var item in topSongIds)
        {
            var song = realm.Find<SongModel>(item.SongId!.Value);
            if (song != null)
            {
                results.Add(new SongAggregate(song, item.SkipCount));
            }
        }
        return results;
    }

    /// <summary>
    /// 14. Gets play counts grouped by the hour of the day.
    /// Perfect for a 24-hour bar chart showing peak listening times.
    /// </summary>
    /// <param name="realm">The Realm instance.</param>
    /// <returns>A list of data points where X is the hour (0-23) and Y is the total plays in that hour.</returns>
    public static List<DataPoint<int, int>> GetPlayCountsByHourOfDay(Realm realm)
    {
        return realm.All<DimmerPlayEvent>().AsEnumerable()
            .Where(e => e.PlayType == (int)PlayEventType.Play)
            .ToList() // Bring to memory for grouping by property
            .GroupBy(e => e.DatePlayed.Hour)
            .Select(g => new DataPoint<int, int>(g.Key, g.Count()))
            .OrderBy(dp => dp.X)
            .ToList();
    }

    /// <summary>
    /// 15. Gets play counts grouped by the day of the week.
    /// Ideal for a bar chart showing which days are most popular for music.
    /// </summary>
    /// <param name="realm">The Realm instance.</param>
    /// <returns>A list of data points where X is the DayOfWeek and Y is the total plays on that day.</returns>
    public static List<DataPoint<DayOfWeek, int>> GetPlayCountsByDayOfWeek(Realm realm)
    {
        return realm.All<DimmerPlayEvent>().AsEnumerable()
            .Where(e => e.PlayType == (int)PlayEventType.Play)
            .ToList() // Bring to memory for grouping by property
            .GroupBy(e => e.DatePlayed.DayOfWeek)
            .Select(g => new DataPoint<DayOfWeek, int>(g.Key, g.Count()))
            .OrderBy(dp => dp.X)
            .ToList();
    }

    /// <summary>
    /// 16. Gets the distribution of plays across different device form factors (e.g., Desktop, Mobile).
    /// </summary>
    /// <param name="realm">The Realm instance.</param>
    /// <returns>A list of data points where X is the form factor string and Y is the play count.</returns>
    public static List<DataPoint<string, int>> GetPlaysByDeviceFormFactor(Realm realm)
    {
        return realm.All<DimmerPlayEvent>()
            .Where(e => e.PlayType == (int)PlayEventType.Play && e.DeviceFormFactor != null)
            .ToList()
            .GroupBy(e => e.DeviceFormFactor!)
            .Select(g => new DataPoint<string, int>(g.Key, g.Count()))
            .OrderByDescending(dp => dp.Y)
            .ToList();
    }

    #endregion


    #region --- Advanced & Insightful Analytics ---

    /// <summary>
    /// 17. Calculates a "Popularity Score" for a song.
    /// This is a weighted score: high play/completion counts increase it, while skips decrease it.
    /// A more nuanced metric than simple play count.
    /// </summary>
    /// <param name="realm">The Realm instance.</param>
    /// <param name="songId">The ID of the song.</param>
    /// <returns>A calculated popularity score.</returns>
    public static double GetPopularityScore(Realm realm, ObjectId songId)
    {
        var song = realm.Find<SongModel>(songId);
        if (song == null || !song.PlayHistory.Any())
            return 0;

        // Weights can be tweaked to your preference
        const double playWeight = 1.0;
        const double completionWeight = 1.5;
        const double skipPenalty = -2.0;
        const double favoriteBonus = 25.0;

        var history = song.PlayHistory.ToList(); // ToList is acceptable here as we're analyzing one song's history

        int playCount = history.Count(e => e.PlayType == (int)PlayEventType.Play);
        int completionCount = history.Count(e => e.WasPlayCompleted || e.PlayType == (int)PlayEventType.Completed);
        int skipCount = history.Count(e => e.PlayType == (int)PlayEventType.Skipped);

        double score = (playCount * playWeight) + (completionCount * completionWeight) + (skipCount * skipPenalty);
        if (song.IsFavorite)
        {
            score += favoriteBonus;
        }

        // Normalize by song duration to not unfairly penalize very short songs that can be played more often.
        return song.DurationInSeconds > 0 ? (score / Math.Sqrt(song.DurationInSeconds)) : Math.Max(0, score);
    }

    /// <summary>
    /// 18. Finds "Divisive" songs - those with both high play counts and high skip counts.
    /// These are "love it or hate it" tracks.
    /// </summary>
    /// <param name="realm">The Realm instance.</param>
    /// <param name="topN">The number of songs to return.</param>
    /// <returns>A list of songs with their play and skip counts.</returns>
    public static List<DataPoint<SongModel, (int Plays, int Skips)>> GetMostDivisiveSongs(Realm realm, int topN)
    {
        // This is a more complex query. We group all relevant events by song.
        var eventStats = realm.All<DimmerPlayEvent>()
            .Where(e => e.SongId != null && (e.PlayType == (int)PlayEventType.Play || e.PlayType == (int)PlayEventType.Skipped))
            .ToList() // Required for complex GroupBy in-memory
            .GroupBy(e => e.SongId)
            .Select(g => new
            {
                SongId = g.Key,
                Plays = g.Count(ev => ev.PlayType == (int)PlayEventType.Play),
                Skips = g.Count(ev => ev.PlayType == (int)PlayEventType.Skipped)
            })
            // A "divisive score" can be the product of plays and skips. Filter out low-count songs.
            .Where(s => s.Plays > 5 && s.Skips > 5)
            .OrderByDescending(s => s.Plays * s.Skips)
            .Take(topN)
            .ToList();

        var results = new List<DataPoint<SongModel, (int Plays, int Skips)>>();
        foreach (var stat in eventStats)
        {
            var song = realm.Find<SongModel>(stat.SongId!.Value);
            if (song != null)
            {
                results.Add(new DataPoint<SongModel, (int Plays, int Skips)>(song, (stat.Plays, stat.Skips)));
            }
        }
        return results;
    }

    /// <summary>
    /// 19. Gets data for a listening heatmap (Hour of Day vs. Day of Week).
    /// Perfect for a Cartesian chart with colored squares to show peak listening times.
    /// </summary>
    /// <param name="realm">The Realm instance.</param>
    /// <returns>A list of heatmap points (DayOfWeek, Hour, PlayCount).</returns>
    public static List<HeatmapPoint> GetListeningActivityHeatmap(Realm realm)
    {
        return realm.All<DimmerPlayEvent>()
            .Where(e => e.PlayType == (int)PlayEventType.Play)
            .ToList() // Grouping on multiple calculated properties requires bringing to memory
            .GroupBy(e => new { Day = (int)e.DatePlayed.DayOfWeek, Hour = e.DatePlayed.Hour })
            .Select(g => new HeatmapPoint(g.Key.Day, g.Key.Hour, g.Count()))
            .ToList();
    }

    /// <summary>
    /// 20. Calculates the skip rate for each genre.
    /// Answers the question: "Which genres do I skip the most?"
    /// </summary>
    /// <param name="realm">The Realm instance.</param>
    /// <returns>A list of data points where X is the Genre Name and Y is the skip rate percentage.</returns>
    public static List<DataPoint<string, double>> GetGenreSkipRates(Realm realm)
    {
        var genres = realm.All<GenreModel>().ToList();
        var results = new List<DataPoint<string, double>>();

        foreach (var genre in genres)
        {
            var songsInGenre = genre.Songs.ToList();
            if (songsInGenre.Count==0)
                continue;

            int totalPlays = songsInGenre.Sum(s => s.PlayHistory.Count(e => e.PlayType == (int)PlayEventType.Play));
            if (totalPlays == 0)
                continue;

            int totalSkips = songsInGenre.Sum(s => s.PlayHistory.Count(e => e.PlayType == (int)PlayEventType.Skipped));

            double skipRate = (double)totalSkips / totalPlays * 100.0;
            results.Add(new DataPoint<string, double>(genre.Name, skipRate));
        }

        return results.OrderByDescending(dp => dp.Y).ToList();
    }

    /// <summary>
    /// 21. Finds "Forgotten Favorites" - songs marked as favorite but not played recently.
    /// </summary>
    /// <param name="realm">The Realm instance.</param>
    /// <param name="monthsAgo">How many months to consider "recent".</param>
    /// <returns>A list of favorite songs not played recently.</returns>
    public static List<SongModel> GetForgottenFavorites(Realm realm, int monthsAgo)
    {
        var cutoffDate = DateTimeOffset.UtcNow.AddMonths(-monthsAgo);
        return realm.All<SongModel>()
            .Where(s => s.IsFavorite && s.PlayHistory.AsEnumerable().All(e => e.DatePlayed < cutoffDate))
            .ToList();
    }

    /// <summary>
    /// 22. Gets the average "listen percentage" before a skip for a given song.
    /// Answers "How far into this song do people get before they give up?"
    /// </summary>
    /// <param name="realm">The Realm instance.</param>
    /// <param name="songId">The ID of the song.</param>
    /// <returns>The average percentage (0-100) into the song where skips occur.</returns>
    public static double GetAverageSkipPercentage(Realm realm, ObjectId songId)
    {
        var song = realm.Find<SongModel>(songId);
        if (song == null || song.DurationInSeconds <= 0)
            return 0;

        var skipEvents = song.PlayHistory.Where(e => e.PlayType == (int)PlayEventType.Skipped).ToList();
        if (skipEvents.Count==0)
            return 0; // Or 100, depending on how you want to interpret "no skips"

        return skipEvents.Average(e => e.PositionInSeconds / song.DurationInSeconds * 100.0);
    }

    /// <summary>
    /// 23. Get Top N Artists by total play time, not just play count.
    /// This favors artists with longer songs that are listened to fully.
    /// </summary>
    /// <param name="realm">The Realm instance.</param>
    /// <param name="topN">The number of artists to return.</param>
    /// <returns>Data points of Artist and their total listened-to minutes.</returns>
    public static List<DataPoint<ArtistModel, double>> GetTopArtistsByPlayTime(Realm realm, int topN)
    {
        var artistPlayTimes = realm.All<DimmerPlayEvent>()
            .Where(e => e.SongId != null)
            .ToList()
            .Select(e =>
            {
                // Find the song and its main artist
                var song = realm.Find<SongModel>(e.SongId.Value);
                return new { Event = e, Artist = song?.Artist };
            })
            .Where(x => x.Artist != null)
            .GroupBy(x => x.Artist)
            .Select(g => new
            {
                Artist = g.Key,
                // Sum the position of "end" events (completed or skipped)
                TotalSeconds = g.Where(x => x.Event.PlayType == (int)PlayEventType.Completed || x.Event.PlayType == (int)PlayEventType.Skipped)
                                .Sum(x => x.Event.PositionInSeconds)
            })
            .OrderByDescending(x => x.TotalSeconds)
            .Take(topN)
            .ToList();

        // Convert to the final return type
        return artistPlayTimes
            .Select(a => new DataPoint<ArtistModel, double>(a.Artist!, a.TotalSeconds / 60.0)) // a.Artist! is safe due to .Where()
            .ToList();
    }

    #endregion
}

/// A generic, lightweight data structure for holding plot points.
/// </summary>
/// <param name="X">The value for the X-axis.</param>
/// <param name="Y">The value for the Y-axis.</param>
public record struct DataPoint<T, U>(T X, U Y);

/// <summary>
/// A record to hold song-specific aggregate data.
/// </summary>
/// <param name="Song">The song object.</param>
/// <param name="Count">The calculated count (e.g., play count, skip count).</param>
public record SongAggregate(SongModel Song, int Count);

public record SongScore(SongModel Song, double Score);

public record HeatmapPoint(int X, int Y, int Value);
/// <summary>
/// Represents the type of play action performed.
/// This enum makes the statistic queries much more readable.
/// </summary>
public enum PlayEventType
{
    Play = 0,
    Pause = 1,
    Resume = 2,
    Completed = 3,
    Seeked = 4,
    Skipped = 5,
    Restarted = 6,
    SeekRestarted = 7,
    CustomRepeat = 8,
    Previous = 9
}