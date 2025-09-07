using Realms;
using LinqKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utils;

/// <summary>
/// Generates advanced, user-facing statistics and reports based on listening history,
/// similar to services like Last.fm.
/// </summary>
//



public class ListeningReportGenerator
{


    private readonly ILogger _logger;
    private readonly IMapper mapper;

    List<SongModel> SongsInPeriod { get; }
    List<DimmerPlayEvent> ScrobblesInPeriod { get; }
    public ListeningReportGenerator(
    List<SongModel> songsInPeriod,
    List<DimmerPlayEvent> scrobblesInPeriod, ILogger logger, IMapper mapper)
    {
        SongsInPeriod= songsInPeriod;
        ScrobblesInPeriod= scrobblesInPeriod;
        _logger = logger;
        this.mapper=mapper;
    }


    public async Task<List<DimmerStats>> GenerateReportAsync(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        try
        {
            _logger.LogInformation("Generating listening report from {StartDate} to {EndDate}...", startDate, endDate);

            var reportStats = new List<DimmerStats>();


            if (ScrobblesInPeriod.Count==0)
            {
                _logger.LogWarning("No scrobbles found for the specified period. Report will be empty.");
                reportStats.Add(new DimmerStats { StatTitle = "No Activity", StatExplanation = $"There was no listening activity between {startDate:d} and {endDate:d}." });
                return reportStats;
            }
            var songIdsInPeriod = ScrobblesInPeriod.Where(p => p.SongId.HasValue).Select(p => p.SongId!.Value).Distinct().ToArray();
            var predicate = PredicateBuilder.New<SongModel>(false);


            foreach (var id in songIdsInPeriod)
            {
                // Important: Capture the loop variable to avoid closure issues in the lambda
                var currentId = id;
                predicate = predicate.Or(s => s.Id == currentId);
            }

            // If songIdsInPeriod is empty, the predicate will remain false, returning an empty list.
            // If it's not empty, it will be a chain of s.Id == id1 || s.Id == id2 || ...


            var songPlayCounts = ScrobblesInPeriod
                .Where(p => p.SongId.HasValue)
                .GroupBy(p => p.SongId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            // Phase 2: Generate All Statistics
            reportStats.AddRange(GenerateScrobbleStats(ScrobblesInPeriod, startDate, endDate));
            reportStats.AddRange(GenerateTopMusicCounts(SongsInPeriod, ScrobblesInPeriod, startDate));
            reportStats.AddRange(GenerateTopCharts(SongsInPeriod, songPlayCounts)); // <-- CORRECTED
            reportStats.AddRange(await GenerateListeningFingerprintAsync(SongsInPeriod, ScrobblesInPeriod, songPlayCounts, startDate));
            reportStats.AddRange(GenerateMusicByDecade(SongsInPeriod, songPlayCounts));
            reportStats.AddRange(GenerateListeningClock(ScrobblesInPeriod));
            reportStats.AddRange(GenerateQuickFacts(SongsInPeriod, ScrobblesInPeriod, startDate, endDate));

            reportStats.AddRange(GenerateNewAdvancedStats(SongsInPeriod, ScrobblesInPeriod, songPlayCounts, startDate, endDate));
            reportStats.AddRange(GenerateAdvancedStatPlots(SongsInPeriod, songPlayCounts));

            _logger.LogInformation("Successfully generated listening report with {Count} sections.", reportStats.Count);
            return reportStats;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return Enumerable.Empty<DimmerStats>().ToList();
        }
    }

    private IEnumerable<DimmerStats> GenerateNewAdvancedStats(List<SongModel> songsInPeriod, List<DimmerPlayEvent> ScrobblesInPeriod, Dictionary<ObjectId, int> songPlayCounts, DateTimeOffset startDate, DateTimeOffset endDate)
    {   // 1. Weekly Listening Rhythm
        yield return GenerateWeeklyListeningRhythm(ScrobblesInPeriod);

        // 2. Discovery vs. Comfort Listening
        yield return GenerateDiscoveryVsComfort(ScrobblesInPeriod, songsInPeriod, startDate);

        // 3. Artist Binge Factor
        var topBinge = GenerateArtistBingeFactor(ScrobblesInPeriod, songsInPeriod);
        if (topBinge != null)
            yield return topBinge;

        // 4. Musical Era Profile
        yield return GenerateMusicalEraProfile(SongsInPeriod, songPlayCounts);

        // 5. Rediscovery Report
        var comebackTracks = GenerateRediscoveryReport(songPlayCounts, songsInPeriod, startDate, endDate);
        if (comebackTracks != null)
            yield return comebackTracks;

    }
    /// <summary>
    /// Analyzes listening patterns across the days of the week.
    /// </summary>
    private DimmerStats GenerateWeeklyListeningRhythm(List<DimmerPlayEvent> ScrobblesInPeriod)
    {
        var playsByDay = ScrobblesInPeriod
            .GroupBy(p => p.EventDate.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.Count());

        var plotData = Enum.GetValues(typeof(DayOfWeek))
            .Cast<DayOfWeek>()
            .Select(day => new ChartDataPoint
            {
                Label = day.ToString(),
                Value = playsByDay.GetValueOrDefault(day, 0),
                SortKeyInt = (int)day, // For correct ordering
            })
            .OrderBy(d => d.SortKey)
            .ToList();

        return new DimmerStats
        {
            StatTitle = "Weekly Listening Rhythm",
            StatExplanation = "A breakdown of your listening habits for each day of the week.",
            PlotData = plotData
        };
    }

    /// <summary>
    /// Compares when you listen to newly discovered music versus familiar tracks.
    /// </summary>
    private DimmerStats GenerateDiscoveryVsComfort(List<DimmerPlayEvent> ScrobblesInPeriod, List<SongModel> songsInPeriod, DateTimeOffset startDate)
    {
        var songFirstPlayDict = songsInPeriod.ToDictionary(s => s.Id, s => s.FirstPlayed);

        var discoveryPlaysByHour = ScrobblesInPeriod
            .Where(p => p.SongId.HasValue && songFirstPlayDict.ContainsKey(p.SongId.Value) && songFirstPlayDict[p.SongId.Value] >= startDate)
            .GroupBy(p => p.EventDate.Hour)
            .ToDictionary(g => g.Key, g => g.Count());

        var comfortPlaysByHour = ScrobblesInPeriod
            .Where(p => p.SongId.HasValue && songFirstPlayDict.ContainsKey(p.SongId.Value) && songFirstPlayDict[p.SongId.Value] < startDate)
            .GroupBy(p => p.EventDate.Hour)
            .ToDictionary(g => g.Key, g => g.Count());

        var discoveryPlot = new List<ChartDataPoint>();
        var comfortPlot = new List<ChartDataPoint>();

        for (int h = 0; h < 24; h++)
        {
            discoveryPlot.Add(new ChartDataPoint { Label = $"{h:00}", Value = discoveryPlaysByHour.GetValueOrDefault(h, 0), SortKeyInt = h });
            comfortPlot.Add(new ChartDataPoint { Label = $"{h:00}", Value = comfortPlaysByHour.GetValueOrDefault(h, 0), SortKeyInt = h });
        }

        return new DimmerStats
        {
            StatTitle = "Discovery vs. Comfort",
            StatExplanation = "Compares when you listen to new music (discovered this period) versus familiar favorites from your past.",
            ChildStats = new List<DimmerStats>
            {
                new DimmerStats { Name = "Discovery", PlotData = discoveryPlot },
                new DimmerStats { Name = "Comfort", PlotData = comfortPlot }
            }
        };
    }

    /// <summary>
    /// Finds the longest consecutive streak of songs by a single artist.
    /// </summary>
    private DimmerStats GenerateArtistBingeFactor(List<DimmerPlayEvent> orderedScrobbles, List<SongModel> songsInPeriod)
    {
        if (orderedScrobbles.Count < 2)
            return null;

        var songToArtistMap = songsInPeriod
            .Where(s => s.Artist != null)
            .ToDictionary(s => s.Id, s => s.Artist);

        int maxStreak = 0;
        ArtistModel maxStreakArtist = null;
        int currentStreak = 0;
        ObjectId? currentStreakArtistId = null;

        foreach (var scrobble in orderedScrobbles)
        {
            if (!scrobble.SongId.HasValue || !songToArtistMap.TryGetValue(scrobble.SongId.Value, out var artist))
            {
                // Song has no artist or isn't in our map, reset streak
                currentStreak = 0;
                currentStreakArtistId = null;
                continue;
            }

            if (artist.Id == currentStreakArtistId)
            {
                currentStreak++;
            }
            else
            {
                // New artist, reset streak
                currentStreak = 1;
                currentStreakArtistId = artist.Id;
            }

            if (currentStreak > maxStreak)
            {
                maxStreak = currentStreak;
                maxStreakArtist = artist;
            }
        }

        if (maxStreak > 1 && maxStreakArtist != null)
        {
            return new DimmerStats
            {
                StatTitle = "Top Binge",
                StatExplanation = "The longest streak of consecutive tracks you played by a single artist.",
                ArtistName = maxStreakArtist.Name,
                SongArtist = maxStreakArtist.ToModelView(mapper),
                Count = maxStreak,
            };
        }
        return null;
    }

    /// <summary>
    /// Groups music into distinct cultural eras instead of just decades.
    /// </summary>
    private DimmerStats GenerateMusicalEraProfile(List<SongModel> songsInPeriod, Dictionary<ObjectId, int> songPlayCounts)
    {
        string GetEra(int? year)
        {
            if (!year.HasValue || year < 1950)
                return "Pre-1950s";
            if (year < 1960)
                return "1950s";
            if (year < 1970)
                return "1960s";
            if (year < 1980)
                return "1970s";
            if (year < 1990)
                return "1980s";
            if (year < 2000)
                return "1990s";
            if (year < 2010)
                return "2000s";
            if (year < 2020)
                return "2010s";
            return "2020s";
        }

        var eraCounts = songsInPeriod
            .GroupBy(s => GetEra(s.ReleaseYear))
            .Select(g => new
            {
                Era = g.Key,
                PlayCount = g.Sum(song => songPlayCounts.GetValueOrDefault(song.Id, 0))
            })
            .Where(x => x.PlayCount > 0)
            .OrderBy(x => x.Era)
            .Select(x => new ChartDataPoint { Label = x.Era, Value = x.PlayCount })
            .ToList();

        return new DimmerStats
        {
            StatTitle = "Musical Era Profile",
            StatExplanation = "A breakdown of your listening by cultural music eras.",
            PlotData = eraCounts
        };
    }

    /// <summary>
    /// Finds top tracks played this period that were not played at all in the previous period.
    /// </summary>
    private DimmerStats GenerateRediscoveryReport(Dictionary<ObjectId, int> songPlayCounts, List<SongModel> songsInPeriod, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var periodDuration = endDate - startDate;
        var prevStartDate = startDate - periodDuration;

        var previousPeriodSongIdsList = ScrobblesInPeriod
            .Where(p => p.EventDate >= prevStartDate && p.EventDate < startDate && p.SongId.HasValue)
            .ToList();
        var previousPeriodSongIds = previousPeriodSongIdsList
            .Select(p => p.SongId.Value).ToList();
        var previousPeriodSongIdSet = previousPeriodSongIds
            .Distinct()
            .ToHashSet(); // HashSet for efficient lookups

        var comebackTracks = songPlayCounts
            .Where(kvp => !previousPeriodSongIds.Contains(kvp.Key)) // Filter out songs played previously
            .OrderByDescending(kvp => kvp.Value)
            .Take(5)
            .Select(kvp =>
            {
                var song = songsInPeriod.FirstOrDefault(s => s.Id == kvp.Key);
                return song != null ? new DimmerStats { Song = song.ToModelView(), Count = kvp.Value } : null;
            })
            .Where(s => s != null)
            .ToList();

        if (comebackTracks.Any())
        {
            return new DimmerStats
            {
                StatTitle = "Comeback Tracks",
                StatExplanation = "Your most-played tracks this period that you didn't listen to at all in the last one.",
                ChildStats = comebackTracks
            };
        }
        return null;
    }

    /// <summary>
    /// For the top artist, calculates the percentage of their known songs that were played.
    /// </summary>



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

  
    #endregion

    #region Other (Unchanged) Methods

    private IEnumerable<DimmerStats> GenerateScrobbleStats(List<DimmerPlayEvent> ScrobblesInPeriod, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var totalScrobbles = ScrobblesInPeriod.Count;
        var periodDuration = endDate - startDate;
        var prevStartDate = startDate - periodDuration;
        var prevPeriodScrobbles = ScrobblesInPeriod.Count(p => p.EventDate >= prevStartDate && p.EventDate < startDate && (p.PlayType == (int)PlayType.Play || p.PlayType == (int)PlayType.Completed));
        double percentageChange = prevPeriodScrobbles > 0 ? ((double)(totalScrobbles - prevPeriodScrobbles) / prevPeriodScrobbles) * 100 : (totalScrobbles > 0 ? 100.0 : 0.0);
        var dailyBreakdown = ScrobblesInPeriod.GroupBy(p => p.EventDate.Date).Select(g => new ChartDataPoint { Label = g.Key.ToString("ddd"), Value = g.Count(), SortKey = g.Key }).OrderBy(dp => dp.SortKey).ToList();
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

    private IEnumerable<DimmerStats> GenerateTopMusicCounts(List<SongModel> songsInPeriod, List<DimmerPlayEvent> ScrobblesInPeriod, DateTimeOffset startDate)
    {
        
        int uniqueTracks = songsInPeriod.Count;
        var uniqueArtists = songsInPeriod.Select(s => s.ArtistName).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();
        var uniqueAlbums = songsInPeriod.Select(s => s.AlbumName).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();
        int newTracks = songsInPeriod.Count(s => s.FirstPlayed >= startDate);
        var newArtists = uniqueArtists.Count(artistName => !SongsInPeriod.Any(s => s.ArtistName == artistName && s.FirstPlayed < startDate));
        var newAlbums = uniqueAlbums.Count(albumName => !SongsInPeriod.Any(s => s.AlbumName == albumName && s.FirstPlayed < startDate));
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

    private async Task<List<DimmerStats>> GenerateListeningFingerprintAsync(List<SongModel> songsInPeriod, List<DimmerPlayEvent> ScrobblesInPeriod, Dictionary<ObjectId, int> songPlayCounts, DateTimeOffset startDate)
    {
        
        var stats = new List<DimmerStats>();
        var totalScrobbles = ScrobblesInPeriod.Count;
        if (totalScrobbles == 0)
        {
            return stats;
        }

        var daysListened = ScrobblesInPeriod.Select(p => p.EventDate.Date).Distinct().Count();
        var totalDaysInPeriod = Math.Max(1, (DateTimeOffset.UtcNow.Date - startDate.Date).Days);
        double consistency = (double)daysListened / totalDaysInPeriod * 100;
        stats.Add(new DimmerStats
        {
            StatTitle = "Consistency",
            StatExplanation = "How regularly you listen. (Days with scrobbles / days in period)",
            Value = Math.Round(consistency, 2)
        });
        var uniqueArtists = songsInPeriod.Select(s => s.ArtistName).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();
        if (uniqueArtists.Count!=0)
        {
            var newArtistsCount = uniqueArtists.Count(artistName => !SongsInPeriod.Any(s => s.ArtistName == artistName && s.FirstPlayed < startDate));
            double discoveryRate = (double)newArtistsCount / uniqueArtists.Count * 100;
            stats.Add(new DimmerStats
            {
                StatTitle = "Discovery Rate",
                StatExplanation = "Percentage of artists you listened to for the first time.",
                Value = Math.Round(discoveryRate, 2)
            });
        }
        var uniqueTagsCount = songsInPeriod.SelectMany(s => s.Tags).Select(t => t.Name).Distinct().Count();
        stats.Add(new DimmerStats
        {
            StatTitle = "Variance",
            StatExplanation = "The number of different genres (tags) you explored.",
            Value = uniqueTagsCount
        });
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

    private IEnumerable<DimmerStats> GenerateListeningClock(List<DimmerPlayEvent> ScrobblesInPeriod)
    {
        var hourlyCounts = ScrobblesInPeriod.GroupBy(p => p.EventDate.Hour)
            .ToDictionary(g => g.Key, g => g.Count());
        var plotData = Enumerable.Range(0, 24)
            .Select(h => 
            new ChartDataPoint
            {
                Label = $"{h:00}:00",
                Value = hourlyCounts.GetValueOrDefault(h, 0),
                SortKey = new DateTimeOffset(2000, 1, 1, h, 0, 0, TimeSpan.Zero)
            }).ToList();
        var busiestHour = hourlyCounts.OrderByDescending(kvp => kvp.Value).FirstOrDefault();

        yield return new DimmerStats 
        { 
            StatTitle = "Listening Clock", 
            StatExplanation = "Shows when you listen to music throughout the day.", 
            PlotData = plotData, 
            Name = $"{busiestHour.Key:00}:00", 
            Count = busiestHour.Value, 
            StatTitle2 = "Busiest Hour" 
        };
        
    
    }

    private IEnumerable<DimmerStats> GenerateQuickFacts(List<SongModel> songsInPeriod, List<DimmerPlayEvent> ScrobblesInPeriod, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var completedPlays = ScrobblesInPeriod.Where(p => p.PlayType == (int)PlayType.Completed && p.SongId.HasValue);
        double totalSeconds = completedPlays.Join(SongsInPeriod, play => play.SongId, song => song.Id, (play, song) => song.DurationInSeconds).Sum();
        yield return new DimmerStats
        {
            StatTitle = "Total Listening Time",
            TimeSpanValue = TimeSpan.FromSeconds(totalSeconds),
            Value = Math.Round(totalSeconds / 3600, 1), // in hours
            StatExplanation = "Total time spent listening to music."
            ,TotalSeconds = $"{totalSeconds} seconds",
            TotalSecondsNumeric = totalSeconds
        };
        var daysInPeriod = Math.Max(1, (endDate - startDate).TotalDays);
        double avgScrobbles = ScrobblesInPeriod.Count / daysInPeriod;
        yield return new DimmerStats
        {
            StatTitle = "Average Scrobbles/Day",
            Value = Math.Round(avgScrobbles, 1),
            StatExplanation = "Your average number of tracks listened to per day."
            
        };
        var mostActiveDay = ScrobblesInPeriod.GroupBy(p => p.EventDate.Date).OrderByDescending(g => g.Count()).FirstOrDefault();
        if (mostActiveDay != null)
        { yield return new DimmerStats
        {
            StatTitle = "Most Active Day",
            DateValue = mostActiveDay.Key,
            Count = mostActiveDay.Count(),
            StatExplanation = "The day you listened to the most music."
            
        }; }
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
            yield return new DimmerStats
            {
                StatTitle = "Listening Concentration (Pareto)",
                StatExplanation = "This curve shows what percentage of your total listening time comes from your top songs. The classic '80/20 rule' suggests 80% of listens come from 20% of songs.",
                PlotData = paretoPlot
            };
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

    public static int CalculateEddingtonNumber(List<int> playCounts) { if (playCounts == null || playCounts.Count==0)
        {
            return 0;
        }

        var sortedCounts = playCounts.OrderByDescending(c => c).ToList(); 
        int eddington = 0; for (int i = 0; i < sortedCounts.Count; i++) { if (sortedCounts[i] >= (i + 1)) { eddington = i + 1; } else { break; } } return eddington; }
    private enum PlayType { Play = 0, Completed = 3 }

    #endregion
}

