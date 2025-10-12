using LinqKit;

namespace Dimmer.Utils;

/// <summary>
/// Generates advanced, user-facing statistics and reports based on listening history,
/// similar to services like Last.fm.
/// </summary>
//



public class ListeningReportGenerator
{

    private readonly IRealmFactory _realmFact;
    private Realm realm;
    private readonly ILogger _logger;
    private readonly IMapper mapper;

    public ListeningReportGenerator(IRealmFactory realm, ILogger logger, IMapper mapper)
    {
        _realmFact = realm;
        _logger = logger;
        this.mapper=mapper;
    }


    public async Task<List<DimmerStats>> GenerateReportAsync(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        try
        {
            _logger.LogInformation("Generating listening report from {StartDate} to {EndDate}...", startDate, endDate);

            var reportStats = new List<DimmerStats>();

            realm = _realmFact.GetRealmInstance();
            // Phase 1: Data Preparation
            var scrobblesInPeriod = realm.All<DimmerPlayEvent>()
                                          .Where(p => p.EventDate >= startDate && p.EventDate < endDate &&
                                                      (p.PlayType == (int)PlayType.Play || p.PlayType == (int)PlayType.Completed))
                                          .ToList();

            if (scrobblesInPeriod.Count==0)
            {
                _logger.LogWarning("No scrobbles found for the specified period. Report will be empty.");
                reportStats.Add(new DimmerStats { StatTitle = "No Activity", StatExplanation = $"There was no listening activity between {startDate:d} and {endDate:d}." });
                return reportStats;
            }
            var songIdsInPeriod = scrobblesInPeriod.Where(p => p.SongId.HasValue).Select(p => p.SongId!.Value).Distinct().ToArray();
            var predicate = PredicateBuilder.New<SongModel>(false);


            foreach (var id in songIdsInPeriod)
            {
                // Important: Capture the loop variable to avoid closure issues in the lambda
                var currentId = id;
                predicate = predicate.Or(s => s.Id == currentId);
            }

            // If songIdsInPeriod is empty, the predicate will remain false, returning an empty list.
            // If it's not empty, it will be a chain of s.Id == id1 || s.Id == id2 || ...
            var songsInPeriod = realm.All<SongModel>().Where(predicate).ToList();


            var songPlayCounts = scrobblesInPeriod
                .Where(p => p.SongId.HasValue)
                .GroupBy(p => p.SongId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            // Phase 2: Generate All Statistics
            reportStats.AddRange(GenerateScrobbleStats(scrobblesInPeriod, startDate, endDate));
            reportStats.AddRange(GenerateTopMusicCounts(songsInPeriod, scrobblesInPeriod, startDate));
            reportStats.AddRange(GenerateTopCharts(songsInPeriod, songPlayCounts)); // <-- CORRECTED
            reportStats.AddRange(await GenerateListeningFingerprintAsync(songsInPeriod, scrobblesInPeriod, songPlayCounts, startDate));
            reportStats.Add(GenerateTopTagsChart(songsInPeriod, songPlayCounts, startDate, endDate)); // <-- CORRECTED
            reportStats.AddRange(GenerateMusicByDecade(songsInPeriod, songPlayCounts));
            reportStats.AddRange(GenerateListeningClock(scrobblesInPeriod));
            reportStats.AddRange(GenerateQuickFacts(songsInPeriod, scrobblesInPeriod, startDate, endDate));
            reportStats.AddRange(GenerateAdvancedStatPlots(songsInPeriod, songPlayCounts));

            _logger.LogInformation("Successfully generated listening report with {Count} sections.", reportStats.Count);
            return reportStats;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return Enumerable.Empty<DimmerStats>().ToList();
        }
    }

    #region Corrected and Completed Methods

    // =======================================================================================
    // THIS METHOD IS NOW CORRECTED AND COMPLETE
    // =======================================================================================
    private IEnumerable<DimmerStats> GenerateTopCharts(List<SongModel> songsInPeriod, Dictionary<ObjectId, int> songPlayCounts)
    {
        // Top Tracks
        var topTracks = songPlayCounts
            .OrderByDescending(kvp => kvp.Value)
            .Take(10)
            .Select(kvp =>
            {
                var song = songsInPeriod.FirstOrDefault(s => s.Id == kvp.Key);
                return song != null ? new DimmerStats { Song = song.ToModelView(), Count = kvp.Value } : null;
            })
            .Where(s => s != null)
            .ToList();

        yield return new DimmerStats
        {
            StatTitle = "Top Tracks",
            ChildStats = topTracks
        };

        // Top Artists
        var topArtists = songsInPeriod
            .Where(s => s.Artist != null)
            .GroupBy(s => s.Artist)
            .Select(g => new { Artist = g.Key, PlayCount = g.Sum(song => songPlayCounts.GetValueOrDefault(song.Id, 0)) })
            .OrderByDescending(x => x.PlayCount)
            .Take(10)
            .Select(x => new DimmerStats { ArtistName = x.Artist.Name, Count = x.PlayCount, SongArtist = x.Artist.ToModelView(mapper) })
            .ToList();

        yield return new DimmerStats
        {
            StatTitle = "Top Artists",
            ChildStats = topArtists
        };

        // Top Albums
        var topAlbums = songsInPeriod
            .Where(s => s.Album != null)
            .GroupBy(s => s.Album)
            .Select(g => new { Album = g.Key, PlayCount = g.Sum(song => songPlayCounts.GetValueOrDefault(song.Id, 0)) })
            .OrderByDescending(x => x.PlayCount)
            .Take(10)
            .Select(x => new DimmerStats { AlbumName = x.Album.Name, Count = x.PlayCount, SongAlbum = x.Album.ToModelView(mapper), ArtistName = x.Album.Artist?.Name })
            .ToList();

        yield return new DimmerStats
        {
            StatTitle = "Top Albums",
            ChildStats = topAlbums
        };
    }

    // =======================================================================================
    // THIS METHOD IS NOW CORRECTED AND COMPLETE
    // =======================================================================================
    private DimmerStats GenerateTopTagsChart(List<SongModel> songsInPeriod, Dictionary<ObjectId, int> songPlayCounts, DateTimeOffset reportStartDate, DateTimeOffset reportEndDate)
    {

        realm = _realmFact.GetRealmInstance();
        var topTags = songsInPeriod
            .SelectMany(s => s.Tags.Select(t => new { SongId = s.Id, TagName = t.Name }))
            .GroupBy(x => x.TagName)
            .Select(g => new { TagName = g.Key, PlayCount = g.Sum(x => songPlayCounts.GetValueOrDefault(x.SongId, 0)) })
            .OrderByDescending(x => x.PlayCount)
            .Take(5)
            .Select(x => x.TagName)
            .ToList();

        if (topTags.Count==0)
        {
            return new DimmerStats { StatTitle = "Top Tags Evolution", StatExplanation = "No tagged music was played in this period." };
        }

        var historicalTagData = new List<DimmerStats>();
        var periodDuration = reportEndDate - reportStartDate;

        foreach (var tagName in topTags)
        {
            var plotData = new List<ChartDataPoint>();
            for (int i = 4; i >= 0; i--) // Go back 5 periods (including the current one)
            {
                var loopEndDate = reportEndDate.Add(-i * periodDuration);
                var loopStartDate = reportStartDate.Add(-i * periodDuration);

                var playsInLoopPeriod = realm.All<DimmerPlayEvent>()
                    .Where(p => p.EventDate >= loopStartDate && p.EventDate < loopEndDate && (p.PlayType == (int)PlayType.Play || p.PlayType == (int)PlayType.Completed))
                    .ToList();

                var songIds = playsInLoopPeriod.Where(p => p.SongId.HasValue).Select(p => p.SongId.Value).Distinct();

                var songIdPredicate = PredicateBuilder.New<SongModel>(false);
                foreach (var id in songIds)
                {
                    var currentId = id;
                    songIdPredicate = songIdPredicate.Or(s => s.Id == currentId);
                }

                // Combine the dynamically built 'IN' clause with the existing tag check
                var songsWithTag = realm.All<SongModel>()
                                        .Where(songIdPredicate.And(s => s.Tags.Any(t => t.Name == tagName)))
                                        .Select(s => s.Id)
                                        .ToList();


             
                var count = playsInLoopPeriod.Count(p => p.SongId.HasValue && songsWithTag.Contains(p.SongId.Value));

                plotData.Add(new ChartDataPoint { Label = loopStartDate.ToString("d MMM"), Value = count, SortKey = loopStartDate });
            }

            historicalTagData.Add(new DimmerStats
            {
                Name = tagName, // The name of the tag for this series
                PlotData = plotData
            });
        }

        // Return a single parent object containing all the historical line data in its children
        return new DimmerStats
        {
            StatTitle = "Top Tags Evolution",
            StatExplanation = "How your listening to your top 5 genres has changed over the last 5 periods.",
            ChildStats = historicalTagData // Each child contains a line for the multi-line chart
        };
    }

    #endregion

    #region Other (Unchanged) Methods

    private IEnumerable<DimmerStats> GenerateScrobbleStats(List<DimmerPlayEvent> scrobblesInPeriod, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        realm = _realmFact.GetRealmInstance();
        var totalScrobbles = scrobblesInPeriod.Count;
        var periodDuration = endDate - startDate;
        var prevStartDate = startDate - periodDuration;
        var prevPeriodScrobbles = realm.All<DimmerPlayEvent>().Count(p => p.EventDate >= prevStartDate && p.EventDate < startDate && (p.PlayType == (int)PlayType.Play || p.PlayType == (int)PlayType.Completed));
        double percentageChange = prevPeriodScrobbles > 0 ? ((double)(totalScrobbles - prevPeriodScrobbles) / prevPeriodScrobbles) * 100 : (totalScrobbles > 0 ? 100.0 : 0.0);
        var dailyBreakdown = scrobblesInPeriod.GroupBy(p => p.EventDate.Date).Select(g => new ChartDataPoint { Label = g.Key.ToString("ddd"), Value = g.Count(), SortKey = g.Key }).OrderBy(dp => dp.SortKey).ToList();
        yield return new DimmerStats
        {
            StatTitle = "Total Scrobbles",
            StatExplanation = "The total number of tracks you listened to.",
            Count = totalScrobbles,
            Value = totalScrobbles,
            ComparisonValue = percentageChange,
            ComparisonLabel = "% vs. last period",
            PlotData = dailyBreakdown
        };
    }

    private IEnumerable<DimmerStats> GenerateTopMusicCounts(List<SongModel> songsInPeriod, List<DimmerPlayEvent> scrobblesInPeriod, DateTimeOffset startDate)
    {
        realm = _realmFact.GetRealmInstance();
        int uniqueTracks = songsInPeriod.Count;
        var uniqueArtists = songsInPeriod.Select(s => s.ArtistName).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();
        var uniqueAlbums = songsInPeriod.Select(s => s.AlbumName).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();
        int newTracks = songsInPeriod.Count(s => s.FirstPlayed >= startDate);
        var newArtists = uniqueArtists.Count(artistName => !realm.All<SongModel>().Any(s => s.ArtistName == artistName && s.FirstPlayed < startDate));
        var newAlbums = uniqueAlbums.Count(albumName => !realm.All<SongModel>().Any(s => s.AlbumName == albumName && s.FirstPlayed < startDate));
        yield return new DimmerStats
        {
            StatTitle = "Unique Tracks",
            Count = uniqueTracks,
            ComparisonValue = newTracks,
            ComparisonLabel = "new tracks discovered"
        };
        yield return new DimmerStats
        {
            StatTitle = "Unique Artists",
            Count = uniqueArtists.Count,
            ComparisonValue = newArtists,
            ComparisonLabel = "new artists discovered"
        };
        yield return new DimmerStats
        {
            StatTitle = "Unique Albums",
            Count = uniqueAlbums.Count,
            ComparisonValue = newAlbums,
            ComparisonLabel = "new albums discovered"
        };
    }

    private async Task<List<DimmerStats>> GenerateListeningFingerprintAsync(List<SongModel> songsInPeriod, List<DimmerPlayEvent> scrobblesInPeriod, Dictionary<ObjectId, int> songPlayCounts, DateTimeOffset startDate)
    {
        realm = _realmFact.GetRealmInstance();
        var stats = new List<DimmerStats>();
        var totalScrobbles = scrobblesInPeriod.Count;
        if (totalScrobbles == 0)
        {
            return stats;
        }

        var daysListened = scrobblesInPeriod.Select(p => p.EventDate.Date).Distinct().Count();
        var totalDaysInPeriod = Math.Max(1, (DateTimeOffset.UtcNow.Date - startDate.Date).Days);
        double consistency = (double)daysListened / totalDaysInPeriod * 100;
        stats.Add(new DimmerStats { StatTitle = "Consistency", StatExplanation = "How regularly you listen. (Days with scrobbles / days in period)", Value = Math.Round(consistency, 2) });
        var uniqueArtists = songsInPeriod.Select(s => s.ArtistName).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();
        if (uniqueArtists.Count!=0)
        {
            var newArtistsCount = uniqueArtists.Count(artistName => !realm.All<SongModel>().Any(s => s.ArtistName == artistName && s.FirstPlayed < startDate));
            double discoveryRate = (double)newArtistsCount / uniqueArtists.Count * 100;
            stats.Add(new DimmerStats { StatTitle = "Discovery Rate", StatExplanation = "Percentage of artists you listened to for the first time.", Value = Math.Round(discoveryRate, 2) });
        }
        var uniqueTagsCount = songsInPeriod.SelectMany(s => s.Tags).Select(t => t.Name).Distinct().Count();
        stats.Add(new DimmerStats { StatTitle = "Variance", StatExplanation = "The number of different genres (tags) you explored.", Value = uniqueTagsCount });
        var artistPlayCounts = songsInPeriod.Where(s => !string.IsNullOrEmpty(s.ArtistName)).GroupBy(s => s.ArtistName).ToDictionary(g => g.Key, g => g.Sum(song => songPlayCounts.GetValueOrDefault(song.Id, 0)));
        if (artistPlayCounts.Count!=0)
        {
            double topArtistPlays = artistPlayCounts.Values.Max();
            double concentration = (topArtistPlays / totalScrobbles) * 100;
            stats.Add(new DimmerStats { StatTitle = "Concentration", StatExplanation = "How much you focused on your top artist.", Value = Math.Round(concentration, 2) });
        }
        var uniqueSongsPlayed = songPlayCounts.Keys.Count;
        double replayRate = totalScrobbles > uniqueSongsPlayed ? (double)(totalScrobbles - uniqueSongsPlayed) / totalScrobbles * 100 : 0;
        stats.Add(new DimmerStats { StatTitle = "Replay Rate", StatExplanation = "How often you returned to songs you've already heard in this period.", Value = Math.Round(replayRate, 2) });
        return stats;
    }

    private IEnumerable<DimmerStats> GenerateMusicByDecade(List<SongModel> songsInPeriod, Dictionary<ObjectId, int> songPlayCounts)
    {
        var decadeCounts = songsInPeriod

        .Where(s => s.ReleaseYear.HasValue && s.ReleaseYear.Value >= 1)
        .GroupBy(s => (s.ReleaseYear.Value / 10) * 10)
            .Select(g => new { Decade = g.Key, PlayCount = g.Sum(song => songPlayCounts.GetValueOrDefault(song.Id, 0)) }).OrderBy(x => x.Decade).Select(x =>
        {
            return new ChartDataPoint { Label = $"{x.Decade}s", Value = x.PlayCount, SortKey = new DateTime(x.Decade, 1, 1) };
        }).ToList();
        yield return new DimmerStats { StatTitle = "Music By Decade", StatExplanation = "A breakdown of your listening by the release decade of the music.", PlotData = decadeCounts };
    }

    private IEnumerable<DimmerStats> GenerateListeningClock(List<DimmerPlayEvent> scrobblesInPeriod)
    {
        var hourlyCounts = scrobblesInPeriod.GroupBy(p => p.EventDate.Hour).ToDictionary(g => g.Key, g => g.Count());
        var plotData = Enumerable.Range(0, 24).Select(h => new ChartDataPoint { Label = $"{h:00}:00", Value = hourlyCounts.GetValueOrDefault(h, 0), SortKey = new DateTimeOffset(2000, 1, 1, h, 0, 0, TimeSpan.Zero) }).ToList();
        var busiestHour = hourlyCounts.OrderByDescending(kvp => kvp.Value).FirstOrDefault();

        yield return new DimmerStats { StatTitle = "Listening Clock", 
            StatExplanation = "Shows when you listen to music throughout the day.", 
            PlotData = plotData, Name = $"{busiestHour.Key:00}:00", 
            Count = busiestHour.Value, StatTitle2 = "Busiest Hour" };
        
    
    }

    private IEnumerable<DimmerStats> GenerateQuickFacts(List<SongModel> songsInPeriod, List<DimmerPlayEvent> scrobblesInPeriod, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var completedPlays = scrobblesInPeriod.Where(p => p.PlayType == (int)PlayType.Completed && p.SongId.HasValue);
        double totalSeconds = completedPlays.Join(songsInPeriod, play => play.SongId, song => song.Id, (play, song) => song.DurationInSeconds).Sum();
        yield return new DimmerStats { StatTitle = "Total Listening Time", TimeSpanValue = TimeSpan.FromSeconds(totalSeconds) };
        var daysInPeriod = Math.Max(1, (endDate - startDate).TotalDays);
        double avgScrobbles = scrobblesInPeriod.Count / daysInPeriod;
        yield return new DimmerStats { StatTitle = "Average Scrobbles/Day", Value = Math.Round(avgScrobbles, 1) };
        var mostActiveDay = scrobblesInPeriod.GroupBy(p => p.EventDate.Date).OrderByDescending(g => g.Count()).FirstOrDefault();
        if (mostActiveDay != null)
        { yield return new DimmerStats { StatTitle = "Most Active Day", DateValue = mostActiveDay.Key, Count = mostActiveDay.Count() }; }
    }

    private IEnumerable<DimmerStats> GenerateAdvancedStatPlots(List<SongModel> songsInPeriod, Dictionary<ObjectId, int> songPlayCounts)
    {
        var sortedPlays = songPlayCounts.Values.OrderByDescending(v => v).ToList();
        if (sortedPlays.Count!=0)
        {
            var totalPlays = (double)sortedPlays.Sum();
            var totalSongs = sortedPlays.Count;
            var paretoPlot = new List<ChartDataPoint>();
            double cumulativePlays = 0;
            for (int i = 0; i < totalSongs; i++)
            {
                cumulativePlays += sortedPlays[i];
                paretoPlot.Add(new ChartDataPoint
                {
                    Label = ((double)(i + 1) / totalSongs * 100).ToString("F1"),
                    Value = (cumulativePlays / totalPlays * 100)
                });
            }
            yield return new DimmerStats { StatTitle = "Listening Concentration (Pareto)", StatExplanation = "This curve shows what percentage of your total listening time comes from your top songs. The classic '80/20 rule' suggests 80% of listens come from 20% of songs.", PlotData = paretoPlot };
        }
        var eddingtonData = songPlayCounts.Values.ToList();
        if (eddingtonData.Count!=0)
        {
            var eddingtonNumber = CalculateEddingtonNumber(eddingtonData);
            var playCountDistribution = eddingtonData.GroupBy(count => count).Select(g => new ChartDataPoint { Label = $"{g.Key} plays", Value = g.Count() }).OrderBy(dp => double.Parse(dp.Label.Split(' ')[0])).ToList();
            yield return new DimmerStats
            {
                StatTitle = "Music Eddington Number",
                StatExplanation = $"Your number is {eddingtonNumber}. You have listened to {eddingtonNumber} different songs at least {eddingtonNumber} times each. The chart shows the distribution of play counts.",
                Value = eddingtonNumber,
                PlotData = playCountDistribution
            };
        }
    }

    private int CalculateEddingtonNumber(List<int> playCounts) { if (playCounts == null || playCounts.Count==0)
        {
            return 0;
        }

        var sortedCounts = playCounts.OrderByDescending(c => c).ToList(); 
        int eddington = 0; for (int i = 0; i < sortedCounts.Count; i++) { if (sortedCounts[i] >= (i + 1)) { eddington = i + 1; } else { break; } } return eddington; }
    private enum PlayType { Play = 0, Completed = 3 }

    #endregion
}

