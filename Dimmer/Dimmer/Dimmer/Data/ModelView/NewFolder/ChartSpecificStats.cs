using Dimmer.Utilities.Extensions;
namespace Dimmer.Data.ModelView.NewFolder;
/// <summary>
/// Provides methods specifically designed to generate data structures
/// for advanced chart types like financial charts, stacked areas, and ranges.
/// This class complements TopStats by focusing on visualization structure
/// rather than just ranking.
/// </summary>


public static class ChartSpecificStats
{
    private const int PlayType_Play = 0;
private const int PlayType_Completed = 3;
private const int PlayType_Skipped = 5;

//==========================================================================
#region --- UPGRADED: Core Library Stats with Full Context ---
//==========================================================================

public static List<DimmerStats> GetOverallListeningByDayOfWeek(IReadOnlyCollection<DimmerPlayEvent> events, IReadOnlyCollection<SongModel> songs)
{
    var songLookup = songs.ToDictionary(s => s.Id);
    return events.Where(e => e.EventDate.HasValue && e.SongId.HasValue && songLookup.ContainsKey(e.SongId.Value))
        .GroupBy(e => e.EventDate!.Value.DayOfWeek)
        .Select(g => new DimmerStats
        {
            StatTitle = "Listening by Day",
            XValue = g.Key.ToString(),
            YValue = g.Count(),
            ContributingSongs = g.Select(ev => songLookup[ev.SongId!.Value].ToModelView()).DistinctBy(s => s.Id).ToList()
        }).OrderBy(s => (int)Enum.Parse<DayOfWeek>((string)s.XValue!)).ToList();
}

public static List<DimmerStats> GetGenrePopularityOverTime(IReadOnlyCollection<DimmerPlayEvent> events, IReadOnlyCollection<SongModel> songs)
{
    var songLookup = songs.ToDictionary(s => s.Id);
    return events
        .Where(e => e.SongId.HasValue && songLookup.ContainsKey(e.SongId.Value) && e.PlayType == PlayType_Completed && !string.IsNullOrEmpty(songLookup[e.SongId.Value].Genre?.Name))
        .GroupBy(e => new { Month = new DateTime(e.EventDate!.Value.Year, e.EventDate.Value.Month, 1), Genre = songLookup[e.SongId!.Value].Genre!.Name })
        .Select(g => new DimmerStats
        {
            StatTitle = "Genre Popularity Over Time",
            XValue = g.Key.Month,
            Category = g.Key.Genre,
            YValue = g.Count(),
            ContributingSongs = g.Select(ev => songLookup[ev.SongId!.Value].ToModelView()).DistinctBy(s => s.Id).ToList()
        }).OrderBy(s => (DateTime)s.XValue!).ToList();
}

public static List<DimmerStats> GetDailyListeningTimeRange(IReadOnlyCollection<DimmerPlayEvent> events, IReadOnlyCollection<SongModel> songs, DateTimeOffset startDate, DateTimeOffset endDate)
{
    var songLookup = songs.ToDictionary(s => s.Id);
    return events.Where(e => e.EventDate >= startDate && e.EventDate < endDate && e.SongId.HasValue && songLookup.ContainsKey(e.SongId.Value))
        .GroupBy(e => e.EventDate!.Value.Date)
        .Where(g => g.Any())
        .Select(g => new DimmerStats
        {
            StatTitle = "Daily Listening Window",
            XValue = g.Key,
            Low = g.Min(ev => ev.EventDate!.Value.Hour),
            High = g.Max(ev => ev.EventDate!.Value.Hour),
            ContributingSongs = g.Select(ev => songLookup[ev.SongId!.Value].ToModelView()).DistinctBy(s => s.Id).ToList()
        }).OrderBy(s => (DateTime)s.XValue!).ToList();
}

public static List<DimmerStats> GetSongProfileBubbleChartData(IReadOnlyCollection<DimmerPlayEvent> events, IReadOnlyCollection<SongModel> songs)
{
    var songLookup = songs.ToDictionary(s => s.Id);
    return events
        .Where(e => e.SongId.HasValue && songLookup.ContainsKey(e.SongId.Value))
        .GroupBy(e => e.SongId!.Value)
        .Select(g => {
            var song = songLookup[g.Key];
            var totalStarts = g.Count(ev => ev.PlayType is PlayType_Play or PlayType_Completed);
            var completions = g.Count(ev => ev.PlayType == PlayType_Completed);
            var listenThroughRate = totalStarts > 0 ? (double)completions / totalStarts * 100 : 0;
            return new DimmerStats
            {
                StatTitle = "Song Profile",
                Song = song.ToModelView(),
                XValue = (double)totalStarts,
                YValue = listenThroughRate,
                SizeDouble = song.DurationInSeconds,
                ContributingSongs = new List<SongModelView> { song.ToModelView() }
            };
        }).Where(s => (double)s.XValue! > 0).ToList();
}

public static List<DimmerStats> GetDailyListeningRoutineOHLC(IReadOnlyCollection<DimmerPlayEvent> events, IReadOnlyCollection<SongModel> songs, DateTimeOffset startDate, DateTimeOffset endDate)
{
    var songLookup = songs.ToDictionary(s => s.Id);
    return events
        .Where(e => e.EventDate >= startDate && e.EventDate < endDate && e.SongId.HasValue && songLookup.ContainsKey(e.SongId.Value))
        .GroupBy(e => e.EventDate!.Value.Date)
        .Select(g => {
            var playsByHour = g.GroupBy(ev => ev.EventDate!.Value.Hour).ToDictionary(h => h.Key, h => h.Count());
            if (playsByHour.Count==0)
                return null;
            var maxPlayHour = playsByHour.OrderByDescending(kvp => kvp.Value).First().Key;
            var minPlayHour = playsByHour.OrderBy(kvp => kvp.Value).First().Key;
            return new DimmerStats
            {
                StatTitle = "Daily Listening Routine",
                XValue = g.Key,
                Name = g.Key.DayOfWeek.ToString(),
                Open = g.Min(ev => ev.EventDate!.Value.Hour),
                High = maxPlayHour,
                Low = minPlayHour,
                Close = g.Max(ev => ev.EventDate!.Value.Hour),
                ContributingSongs = g.Select(ev => songLookup[ev.SongId!.Value].ToModelView()).DistinctBy(s => s.Id).ToList()
            };
        }).Where(s => s != null).OrderBy(s => (DateTime)s!.XValue!).ToList()!;
}
#endregion

//==========================================================================
#region --- NEW & INSPIRED: Last.fm Style Analytics ---
//==========================================================================

/// <summary>
/// (Inspired by Last.fm Listening Fingerprint) - Generates a list of key user behavior metrics.
/// </summary>
public static List<DimmerStats> GetListeningFingerprint(IReadOnlyCollection<DimmerPlayEvent> events, IReadOnlyCollection<SongModel> songs, DateTimeOffset startDate, DateTimeOffset endDate)
{
    var relevantEvents = events.Where(e => e.EventDate >= startDate && e.EventDate < endDate).ToList();
    var relevantSongs = relevantEvents.Where(e => e.SongId.HasValue).Select(e => e.SongId!.Value).Distinct().Select(id => songs.FirstOrDefault(s => s.Id == id)).Where(s => s != null).ToList();
    if (relevantEvents.Count==0)
        return new List<DimmerStats>();

    var fingerprint = new List<DimmerStats>();
    var totalDays = (endDate - startDate).TotalDays;

    // 1. Consistency
    var daysWithPlays = relevantEvents.Select(e => e.EventDate!.Value.Date).Distinct().Count();
    fingerprint.Add(new DimmerStats { StatTitle = "Consistency", StatExplanation = "How regularly you listen. Higher is more consistent.", YValue = Math.Round((double)daysWithPlays / totalDays * 100) });

    // 2. Discovery Rate
    var firstEverPlays = events.GroupBy(e => e.SongId).Select(g => new { SongId = g.Key, FirstPlay = g.Min(ev => ev.EventDate) }).ToDictionary(x => x.SongId, x => x.FirstPlay);
    var newSongsInPeriod = relevantEvents.Where(e => e.SongId.HasValue && firstEverPlays.ContainsKey(e.SongId) && firstEverPlays[e.SongId] >= startDate).Select(e => e.SongId).Distinct().Count();
    var totalSongsInPeriod = relevantEvents.Select(e => e.SongId).Distinct().Count();
    fingerprint.Add(new DimmerStats { StatTitle = "Discovery", StatExplanation = "How much new music you listened to.", YValue = totalSongsInPeriod > 0 ? Math.Round((double)newSongsInPeriod / totalSongsInPeriod * 100) : 0 });

    // 3. Concentration
    var topArtistPlays = relevantEvents.Where(e => e.SongId.HasValue).GroupBy(e => songs.FirstOrDefault(s => s.Id == e.SongId)?.ArtistName).OrderByDescending(g => g.Count()).FirstOrDefault()?.Count() ?? 0;
    fingerprint.Add(new DimmerStats { StatTitle = "Concentration", StatExplanation = "How much you focus on your favorite artists.", YValue = relevantEvents.Count > 0 ? Math.Round((double)topArtistPlays / relevantEvents.Count * 100) : 0 });

    // 4. Replay Rate
    var playsPerSong = relevantEvents.Where(e => e.PlayType == PlayType_Completed).GroupBy(e => e.SongId).Select(g => g.Count());
    var replayPlays = playsPerSong.Where(c => c > 1).Sum(c => c - 1);
    var totalCompletedPlays = playsPerSong.Sum();
    fingerprint.Add(new DimmerStats { StatTitle = "Replay Rate", StatExplanation = "How often you return to your favorite songs.", YValue = totalCompletedPlays > 0 ? Math.Round((double)replayPlays / totalCompletedPlays * 100) : 0 });

    return fingerprint;
}

/// <summary>
/// (Inspired by Last.fm Music by Decade) - Groups all listening history by decade.
/// </summary>
public static List<DimmerStats> GetMusicByDecade(IReadOnlyCollection<DimmerPlayEvent> events, IReadOnlyCollection<SongModel> songs)
{
    var songLookup = songs.ToDictionary(s => s.Id);
    return events
        .Where(e => e.SongId.HasValue && songLookup.ContainsKey(e.SongId.Value) && songLookup[e.SongId.Value].ReleaseYear.HasValue)
        .Select(e => songLookup[e.SongId.Value]) // Get the song model
        .GroupBy(s => (s.ReleaseYear!.Value / 10) * 10) // Group by decade (e.g., 1998 -> 1990)
        .Select(g => {
            var topAlbumInDecade = g.GroupBy(s => s.AlbumName).OrderByDescending(ag => ag.Count()).FirstOrDefault()?.Key;
            return new DimmerStats
            {
                StatTitle = "Music by Decade",
                Name = $"{g.Key}s",
                YValue = g.Count(),
                Category = topAlbumInDecade, // Store the top album name here
                ContributingSongs = g.Select(s => s.ToModelView()).DistinctBy(s => s.Id).ToList()
            };
        }).OrderBy(s => s.Name).ToList();
}

//==========================================================================
#region --- UPGRADED: Single Song Stats with Full Context ---
//==========================================================================

public static List<DimmerStats> GetSongPlayHistoryOverTime(IReadOnlyCollection<DimmerPlayEvent> songEvents)
{
    return songEvents
        .Where(e => e.PlayType == PlayType_Completed && e.EventDate.HasValue)
        .GroupBy(e => new { e.EventDate!.Value.Year, e.EventDate!.Value.Month })
        .Select(g => new DimmerStats
        {
            StatTitle = "Song Play History",
            XValue = new DateTime(g.Key.Year, g.Key.Month, 1),
            YValue = g.Count()
        }).OrderBy(s => (DateTime)s.XValue!).ToList();
}

public static List<DimmerStats> GetSongWeeklyOHLC(IReadOnlyCollection<DimmerPlayEvent> songEvents)
{
    return songEvents
        .Where(e => e.PlayType == PlayType_Completed && e.EventDate.HasValue)
        .GroupBy(e => new { Year = System.Globalization.ISOWeek.GetYear(e.EventDate!.Value.DateTime), Week = System.Globalization.ISOWeek.GetWeekOfYear(e.EventDate!.Value.DateTime) })
        .Select(g => {
            var playsByDay = g.GroupBy(ev => ev.EventDate!.Value.DayOfWeek).ToDictionary(d => d.Key, d => d.Count());
            return new DimmerStats
            {
                StatTitle = "Weekly Song Trend",
                XValue = System.Globalization.ISOWeek.ToDateTime(g.Key.Year, g.Key.Week, DayOfWeek.Monday),
                Open = playsByDay.TryGetValue(g.Min(ev => ev.EventDate!.Value.DayOfWeek), out var o) ? o : 0,
                Close = playsByDay.TryGetValue(g.Max(ev => ev.EventDate!.Value.DayOfWeek), out var cl) ? cl : 0,
                High = playsByDay.Values.Count!=0 ? playsByDay.Values.Max() : 0,
                Low = playsByDay.Values.Count!=0 ? playsByDay.Values.Min() : 0,
            };
        }).OrderBy(s => (DateTime)s.XValue!).ToList();
}

/// <summary>
/// (Chart: ScatterSeries) Identifies the exact moments in a song where the user skips away.
/// Insight: "Do I always skip this song's intro? Where do I get bored?"
/// </summary>
public static List<DimmerStats> GetSongDropOffPoints(IReadOnlyCollection<DimmerPlayEvent> songEvents)
    {
        return songEvents
            .Where(e => e.PlayType == PlayType_Skipped && e.PositionInSeconds > 0)
            .Select(e =>
            {
                var dateTime = e.EventDate is null? DateTime.MinValue:e.EventDate.Value.DateTime;
                return new DimmerStats
                {
                    // For a scatter plot of drop-off points over time
                    XValue =  dateTime, // X-Axis: Date of Skip
                    YValue = e.PositionInSeconds                    // Y-Axis: Position in song
                };
            })
            .OrderBy(s => (DateTimeOffset)s.XValue)
            .ToList();
    }

    #endregion
    #endregion
}