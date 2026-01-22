using CommunityToolkit.Diagnostics;
namespace Dimmer.Utils;

/// <summary>
/// Generates advanced, user-facing statistics and reports based on listening history,
/// similar to services like Last.fm.
/// </summary>
//



public class ListeningReportGenerator
{
    private readonly Dictionary<DateTimeOffset, int> _cachedDayPlayCounts = new();
    private DateTimeOffset? _cachedStart;
    private DateTimeOffset? _cachedEnd;

    private readonly ILogger _logger;
    private readonly IRealmFactory realmFactory;

    // Initialize collections to prevent null reference issues
    List<SongModel>  _songsInPeriod = new List<SongModel>();
    IEnumerable<DimmerPlayEventView> _scrobblesInPeriod = new List<DimmerPlayEventView>();
    Dictionary<ObjectId, int> _songPlayCounts = new Dictionary<ObjectId, int>();
        bool _isReportGenerated = false;
    List<SongModel> _allSongs { get; }
    IEnumerable<DimmerPlayEventView> _allScrobbles { get; }
  
    public ListeningReportGenerator(
        List<SongModel>? allSongs,
        IEnumerable<DimmerPlayEventView> allScrobbles, ILogger logger, IRealmFactory realmFactory)
    {
        _allSongs = allSongs ?? new List<SongModel>();
        _allScrobbles = allScrobbles ;
        _logger = logger;
        this.realmFactory = realmFactory;

        // Initialize collections to prevent null reference issues
        _songsInPeriod = new List<SongModel>();
        _scrobblesInPeriod = new List<DimmerPlayEventView>();
        _songPlayCounts = new Dictionary<ObjectId, int>();
        _isReportGenerated = false;

    }
    DateTimeOffset _startDate;
    DateTimeOffset _endDate; 
    
    private readonly object _lock = new();
    /// <summary>
    /// Filters the master data for the specified period and calculates all necessary aggregates.
    /// This MUST be called before any Get...() method.
    /// </summary>
    public async Task<bool> GenerateReportAsync(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        lock (_lock)
        {
            if (_isReportGenerated && _startDate == startDate && _endDate == endDate)
                return true;
        }
        if (_cachedStart == startDate && _cachedEnd == endDate && _isReportGenerated)
            return true; // skip

        _cachedStart = startDate;
        _cachedEnd = endDate;
        
        return await Task.Run(() =>
        {
            try
            {
             
                _logger.LogInformation("Preparing report data from {StartDate} to {EndDate}...", startDate, endDate);
                _startDate = startDate;
                _endDate = endDate;
                _isReportGenerated = false; // Reset state

                // Phase 1: Filter data for the period
                _scrobblesInPeriod = _allScrobbles
                    .Where(p => p.EventDate >= startDate && p.EventDate < endDate)
                    .Where(IsPlayEvent)
                    .ToList();

                if (!_scrobblesInPeriod.Any())
                {
                    _logger.LogInformation("No scrobbles found in the period. Report will be empty.");
                    // We still mark as generated, but collections will be empty.
                    _isReportGenerated = true;
                    return true;
                }

                var songIdsInPeriod = _scrobblesInPeriod
                    .Where(p => p.SongId.HasValue)
                    .Select(p => p.SongId.Value)
                    .Distinct()
                    .ToHashSet(); // HashSet is fast for lookups

                _songsInPeriod = _allSongs
                    .Where(s => songIdsInPeriod.Contains(s.Id))
                    .ToList();

                // Phase 2: Pre-calculate common aggregates
                _songPlayCounts = _scrobblesInPeriod
                    .Where(p => p.SongId.HasValue)
                    .GroupBy(p => p.SongId.Value)
                    .ToDictionary(g => g.Key, g => g.Count());

                _isReportGenerated = true;
                _logger.LogInformation("Report data preparation complete.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to prepare report data.");
                return false;
            }

        });

    }
    private void CheckIfGenerated()
    {
        Guard.IsTrue(_isReportGenerated, nameof(_isReportGenerated),
        "GenerateReportAsync must be called successfully before getting statistics.");
    }

    // --- BASIC STATS ---
    public DimmerStats? GetTotalScrobbles()
    {
        CheckIfGenerated();
        if (!_scrobblesInPeriod.Any())
            return null;

        var periodDuration = _endDate - _startDate;
        var prevStartDate = _startDate - periodDuration;
        _cachedDayPlayCounts.Clear();
        foreach (var dayGroup in _scrobblesInPeriod.GroupBy(p => p.EventDate.Value.Date))
            _cachedDayPlayCounts[dayGroup.Key] = dayGroup.Count();
        // Calculate previous period scrobbles from the master list
        var prevPeriodScrobbles = _allScrobbles.AsQueryable().Where(p=> IsPlayEvent(p)).Count(p => p.EventDate >= prevStartDate && p.EventDate < _startDate);
        ;

        double percentageChange = prevPeriodScrobbles > 0 ? ((double)(_scrobblesInPeriod.Count() - prevPeriodScrobbles) / prevPeriodScrobbles) * 100 : (_scrobblesInPeriod.Count() > 0 ? 100.0 : 0.0);
        var dailyBreakdown = _cachedDayPlayCounts
         .OrderBy(x => x.Key)
         .Select(x => new ChartDataPoint
         {
             Label = x.Key.ToString("ddd"),
             Value = x.Value,
             SortKey = x.Key
         })
         .ToList();
        return new DimmerStats
        {
            StatTitle = "Total Scrobbles",
            Count = _scrobblesInPeriod.Count(),
            ComparisonValue = Math.Round(percentageChange, 1),
            ComparisonLabel = "% vs. last period",
            PlotData = dailyBreakdown
        };
    }

    public List<DimmerStats?>? GetTopTracks()
    {
        CheckIfGenerated();
        if (_songsInPeriod.Count == 0)
            return null;
        
        var songsDict = _songsInPeriod.ToDictionary(s => s.Id);
        return _songPlayCounts
            .OrderByDescending(kvp => kvp.Value)
            .Take(15)
            .Select((kvp,index) => songsDict.TryGetValue(kvp.Key, out var song)
                ? new DimmerStats { Rank=index+1,  Song = song.ToSongModelView(), Count = kvp.Value }
                : null)

            .Where(s => s != null)
            .ToList();
    }

    public List<DimmerStats>? GetTopArtists()
    {
        CheckIfGenerated();
        if (_songsInPeriod.Count == 0)
            return null;

        var res = _songsInPeriod
            .Where(s => s.Artist != null)
            .GroupBy(s => s.Artist.Id)
            .Select(g => new { Artist = g.First().Artist, PlayCount = g.Sum(song => _songPlayCounts.GetValueOrDefault(song.Id, 0)) })
            .OrderByDescending(x => x.PlayCount)
            .Take(15)
            .Select((x, index) => new DimmerStats {ArtistName = x.Artist.Name, Count = x.PlayCount, SongArtist = x.Artist.ToArtistModelView()
            ,Rank=index+1})
            .ToList();
        return res;
    }

    public List<DimmerStats>? GetTopAlbums()
    {
        CheckIfGenerated();
        if (_songsInPeriod.Count == 0)
            return null;

        return _songsInPeriod
            .Where(s => s.Album != null)
            .GroupBy(s => s.Album) // Assuming Album object has equality implemented correctly
            .Select(g => new { Album = g.Key, PlayCount = g.Sum(song => _songPlayCounts.GetValueOrDefault(song.Id, 0)) })
            .OrderByDescending(x => x.PlayCount)
            .Take(15)
            .Select((x, index) => new DimmerStats { AlbumName = x.Album.Name, Count = x.PlayCount, SongAlbum = x.Album.ToAlbumModelView(), ArtistName = x.Album.Artist?.Name, Rank=index+1 })
            .ToList();
    }

    public DimmerStats? GetListeningClock()
    {
        CheckIfGenerated();
        if (!_scrobblesInPeriod.Any())
            return null;

        var hourlyCounts = _scrobblesInPeriod.GroupBy(p => p.EventDate.Value.Hour).ToDictionary(g => g.Key, g => g.Count());
        var plotData = Enumerable.Range(0, 24).Select(h =>
        {
            return new ChartDataPoint
            {
                Label = // value like "00:00", "01:00", ..., "23:00"
                    h.ToString("00") + ":00",
                
                Value = hourlyCounts.GetValueOrDefault(h, 0),
                SortKeyInt = h
            };
        }).ToList();
        var busiestHour = hourlyCounts.OrderByDescending(kvp => kvp.Value).FirstOrDefault();

        return new DimmerStats
        {
            StatTitle = "Listening Clock",
            PlotData = plotData,
            Name = $"{busiestHour.Key:00}:00",
            Count = busiestHour.Value,
            StatTitle2 = "Busiest Hour"
        };
    }

    public DimmerStats? GetMusicByDecade()
    {
        CheckIfGenerated();
        if (_songsInPeriod.Count == 0)
            return null;

        var decadeCounts = _songsInPeriod
            .Where(s => s.ReleaseYear.HasValue && s.ReleaseYear.Value >= 1900)
            .GroupBy(s => (s.ReleaseYear.Value / 10) * 10)
            .Select(g => new { Decade = g.Key, PlayCount = g.Sum(song => _songPlayCounts.GetValueOrDefault(song.Id, 0)) })
            .OrderBy(x => x.Decade)
            .Select(x => new ChartDataPoint { Label = $"{x.Decade}s", Value = x.PlayCount, SortKeyInt = x.Decade })
            .ToList();

        return new DimmerStats { StatTitle = "Music By Decade", PlotData = decadeCounts };
    }

    // --- UNIQUE COUNT CARDS ---

    public DimmerStats? GetUniqueTracks()
    {
        CheckIfGenerated();
        if (_songsInPeriod.Count == 0)
            return null;

        int newTracks = _songsInPeriod.Count(s => s.FirstPlayed >= _startDate);
        return new DimmerStats { StatTitle = "Unique Tracks", Count = _songsInPeriod.Count, ComparisonValue = newTracks, ComparisonLabel = "new tracks" };
    }

    public DimmerStats? GetUniqueArtists()
    {
        CheckIfGenerated();
        if (_songsInPeriod.Count == 0)
            return null;

        var artistsInPeriod = _songsInPeriod.Where(s => s.Artist != null).Select(s => s.Artist.Id).Distinct().ToHashSet();
        var allPastArtistIds = _allSongs.Where(s => s.Artist != null && s.FirstPlayed < _startDate).Select(s => s.Artist.Id).ToHashSet();

        int newArtists = artistsInPeriod.Count(id => !allPastArtistIds.Contains(id));
        return new DimmerStats { StatTitle = "Unique Artists", Count = artistsInPeriod.Count, ComparisonValue = newArtists, ComparisonLabel = "new artists" };
    }

    public DimmerStats? GetUniqueAlbums()
    {
        CheckIfGenerated();
        if (_songsInPeriod.Count == 0)
            return null;

        var albumsInPeriod = _songsInPeriod.Where(s => s.Album != null).Select(s => s.AlbumName).Distinct().ToList(); // Assuming Album name is unique enough
        var allPastAlbumNames = _allSongs.Where(s => s.Album != null && s.FirstPlayed < _startDate).Select(s => s.AlbumName).ToHashSet();

        int newAlbums = albumsInPeriod.Count(name => !allPastAlbumNames.Contains(name));
        return new DimmerStats { StatTitle = "Unique Albums", Count = albumsInPeriod.Count, ComparisonValue = newAlbums, ComparisonLabel = "new albums" };
    }

    // --- LISTENING FINGERPRINT ---

    public DimmerStats? GetConsistence()
    {
        CheckIfGenerated();
        if (!_scrobblesInPeriod.Any())
            return null;

        var daysListened = _scrobblesInPeriod.Select(p => p.EventDate.Value.Date).Distinct().Count();
        var totalDaysInPeriod = Math.Max(1, (_endDate.Date - _startDate.Date).Days);
        double consistency = (double)daysListened / totalDaysInPeriod * 100;
        return new DimmerStats { StatTitle = "Consistency", Value = Math.Round(consistency, 1) };
    }

    public DimmerStats? GetDiscoveryRate()
    {
        CheckIfGenerated();
        if (_songsInPeriod.Count == 0)
            return null;

        var artistsInPeriod = _songsInPeriod.Where(s => s.Artist != null).Select(s => s.Artist.Id).Distinct().ToHashSet();
        if (artistsInPeriod.Count == 0)
            return new DimmerStats { StatTitle = "Discovery Rate", Value = 0 };

        var allPastArtistIds = _allSongs.Where(s => s.Artist != null && s.FirstPlayed < _startDate).Select(s => s.Artist.Id).ToHashSet();
        int newArtistsCount = artistsInPeriod.Count(id => !allPastArtistIds.Contains(id));
        double discoveryRate = (double)newArtistsCount / artistsInPeriod.Count * 100;

        return new DimmerStats { StatTitle = "Discovery Rate", Value = Math.Round(discoveryRate, 1) };
    }

    public DimmerStats? GetVariance()
    {
        CheckIfGenerated();
        if (_songsInPeriod.Count == 0)
            return null;

        var uniqueTagsCount = _songsInPeriod.SelectMany(s => s.Tags).Select(t => t.Name).Distinct().Count();
        return new DimmerStats { StatTitle = "Variance", Value = uniqueTagsCount };
    }

    public DimmerStats? GetConcentration()
    {
        CheckIfGenerated();
        if (!_scrobblesInPeriod.Any())
            return null;

        var artistPlayCounts = _songsInPeriod
            .Where(s => s.Artist != null)
            .GroupBy(s => s.Artist.Id)
            .ToDictionary(g => g.Key, g => g.Sum(song => _songPlayCounts.GetValueOrDefault(song.Id, 0)));

        if (artistPlayCounts.Count == 0)
            return new DimmerStats { StatTitle = "Concentration", Value = 0 };

        double topArtistPlays = artistPlayCounts.Values.Max();
        double concentration = (topArtistPlays / _scrobblesInPeriod.Count()) * 100;
        return new DimmerStats { StatTitle = "Concentration", Value = Math.Round(concentration, 1) };
    }

    public DimmerStats? GetReplayRate()
    {
        CheckIfGenerated();
        if (!_scrobblesInPeriod.Any())
            return null;

        double replayRate = _scrobblesInPeriod.Count() > _songPlayCounts.Count ? (double)(_scrobblesInPeriod.Count() - _songPlayCounts.Count) / _scrobblesInPeriod.Count() * 100 : 0;
        return new DimmerStats { StatTitle = "Replay Rate", Value = Math.Round(replayRate, 1) };
    }

    // --- QUICK FACTS ---

    public DimmerStats? GetTotalListeningTime()
    {
        CheckIfGenerated();
        if (!_scrobblesInPeriod.Any())
            return null;

        var songsDict = _songsInPeriod.ToDictionary(s => s.Id);
        double totalSeconds = _scrobblesInPeriod
            .Where(p => p.PlayType == (int)PlayType.Completed && p.SongId.HasValue && songsDict.ContainsKey(p.SongId.Value))
            .Sum(p => songsDict[p.SongId.Value].DurationInSeconds);

        return new DimmerStats
        {
            StatTitle = "Total Listening Time",
            TimeSpanValue = TimeSpan.FromSeconds(totalSeconds),
            Value = Math.Round(totalSeconds / 3600, 1) // in hours
        };
    }

    public DimmerStats? GetAverageScrobblesDay()
    {
        CheckIfGenerated();
        if (!_scrobblesInPeriod.Any())
            return null;

        var daysInPeriod = Math.Max(1, (_endDate - _startDate).TotalDays);
        double avgScrobbles = _scrobblesInPeriod.Count() / daysInPeriod;
        return new DimmerStats { StatTitle = "Average Scrobbles/Day", Value = Math.Round(avgScrobbles, 1) };
    }

    public DimmerStats? GetMostActiveDay()
    {
        CheckIfGenerated();
        if (!_scrobblesInPeriod.Any())
            return null;

        var mostActiveDay = _scrobblesInPeriod
            .GroupBy(p => p.EventDate.Value.Date)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        return mostActiveDay != null
            ? new DimmerStats { StatTitle = "Most Active Day", DateValue = mostActiveDay.Key, Count = mostActiveDay.Count() }
            : null;
    }

    // --- ADVANCED PLOTS ---

    public DimmerStats? GetListeningConcentration()
    {
        CheckIfGenerated();
        if (_songPlayCounts.Count == 0)
            return null;

        var sortedPlays = _songPlayCounts.Values.OrderByDescending(v => v).ToList();
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
        return new DimmerStats { StatTitle = "Listening Concentration (Pareto)", PlotData = paretoPlot };
    }

    public DimmerStats? GetMusicEddingtonNumber()
    {
        CheckIfGenerated();
        if (_songPlayCounts.Count == 0)
            return null;

        var eddingtonData = _songPlayCounts.Values.ToList();

        var sortedCounts = eddingtonData.OrderByDescending(c => c).ToList();
        int eddingtonNumber = 0;
        for (int i = 0; i < sortedCounts.Count; i++)
        {
            if (sortedCounts[i] >= (i + 1))
            {
                eddingtonNumber = i + 1;
            }
            else
            {
                break;
            }
        }

        return new DimmerStats { StatTitle = "Music Eddington Number", Value = eddingtonNumber };
    }
    public List<DimmerStats>? GetTopGenres()
    {
        CheckIfGenerated();
        if (_songsInPeriod.Count == 0) return null;

        return _songsInPeriod
            .Where(s => s.Genre != null)
            .GroupBy(s => s.Genre.Name)
            .Select(g => new DimmerStats { StatTitle = g.Key, Count = g.Sum(song => _songPlayCounts.GetValueOrDefault(song.Id, 0)) })
            .OrderByDescending(x => x.Count)
            .Take(15)
            .ToList();
    }

    public DimmerStats? GetAverageSessionLength()
    {
        CheckIfGenerated();
        if (!_scrobblesInPeriod.Any()) return null;
        var sessions = _scrobblesInPeriod
            .GroupBy(e => e.DatePlayed.Date)
            .Select(g => g.Max(e => e.PositionInSeconds))
            .DefaultIfEmpty(0)
            .Average();
        return new DimmerStats { StatTitle = "Avg Session Length", Value = Math.Round(sessions / 60, 1) };
    }
    public List<DimmerStats>? GetTopDevices() =>
    _scrobblesInPeriod
        .Where(e => !string.IsNullOrEmpty(e.DeviceName))
        .GroupBy(e => e.DeviceName!)
        .Select(g => new DimmerStats { StatTitle = g.Key, Count = g.Count() })
        .OrderByDescending(x => x.Count)
        .Take(5)
        .ToList();
    private static bool IsPlayEvent(DimmerPlayEventView e)
    => e.PlayType == (int)PlayType.Play || e.PlayType == (int)PlayType.Completed;

}