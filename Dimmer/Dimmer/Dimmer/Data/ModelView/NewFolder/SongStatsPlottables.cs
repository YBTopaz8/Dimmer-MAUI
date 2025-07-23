
using Dimmer.Utilities.Extensions;

public static class ChartSpecificStats
{
    private const int PlayType_Play = 0;
    private const int PlayType_Completed = 3;
    private const int PlayType_Skipped = 5;

    //==========================================================================
    #region 1. For Circular Charts (Pie, Doughnut, Radial Bar)
    //==========================================================================

    /// <summary>
    /// [Single Song] Breakdown of interaction types for one song.
    /// Insight: "Do I let this song finish, or do I usually skip or seek it?"
    /// Chart Suggestions: 1. DoughnutSeries, 2. PieSeries, 3. RadialBarSeries
    /// </summary>
    public static List<DimmerStats> GetPlayTypeDistribution(IReadOnlyCollection<DimmerPlayEvent> songEvents)
    {
        var playTypeMap = new Dictionary<int, string>
        {
            {0, "Started"}, {1, "Paused"}, {2, "Resumed"}, {3, "Completed"},
            {4, "Seeked"}, {5, "Skipped"}, {6, "Restarted"}
        };
        return songEvents.GroupBy(e => e.PlayType)
            .Select(g => new DimmerStats
            {
                Name = playTypeMap.TryGetValue(g.Key, out var name) ? name : $"Type {g.Key}",
                Count = g.Count()
            }).OrderByDescending(s => s.Count).ToList();
    }

    /// <summary>
    /// [Global] Shows which day of the week is most active for listening.
    /// Insight: "Am I more of a weekday warrior or a weekend listener?"
    /// Chart Suggestions: 1. RadialBarSeries, 2. DoughnutSeries, 3. PieSeries
    /// </summary>
    public static List<DimmerStats> GetOverallListeningByDayOfWeek(IReadOnlyCollection<DimmerPlayEvent> allEvents)
    {
        return allEvents
            .Where(e => e.PlayType is PlayType_Play or PlayType_Completed && e.EventDate.HasValue)
            .GroupBy(e => e.EventDate!.Value.DayOfWeek)
            .Select(g => new DimmerStats { Name = g.Key.ToString(), Count = g.Count() })
            .OrderBy(s => (int)Enum.Parse<DayOfWeek>(s.Name))
            .ToList();
    }
    #endregion

    //==========================================================================
    #region 2. For Polar Charts (Line, Area, Radar)
    //==========================================================================

    /// <summary>
    /// [Single Song] Creates a "Listening Clock" showing when a song is played.
    /// Insight: "Is this a morning workout song or a late-night focus track?"
    /// Chart Suggestions: 1. PolarRadarSeries (DrawType=Area), 2. PolarAreaSeries, 3. PolarLineSeries
    /// </summary>
    public static List<DimmerStats> GetPlayDistributionByHour(IReadOnlyCollection<DimmerPlayEvent> songEvents)
    {
        // Your existing method is perfect for this.
        return songEvents.Where(e => e.PlayType is PlayType_Play or PlayType_Completed).GroupBy(e => e.EventDate?.Hour)
            .Where(g => g.Key.HasValue).Select(g => new DimmerStats { Name = $"{g.Key:00}:00", Count = g.Count() })
            .OrderBy(s => s.Name).ToList();
    }

    /// <summary>
    /// [Global] Compares the listening clock of your top 3 artists.
    /// Insight: "Do I listen to Artist A in the morning and Artist B at night?"
    /// Chart Suggestions: 1. PolarLineSeries (one per artist), 2. PolarRadarSeries (DrawType=Line), 3. PolarScatterSeries
    /// </summary>
    public static List<DimmerStats> GetTopArtistListeningClocks(IReadOnlyCollection<DimmerPlayEvent> allEvents, IReadOnlyCollection<SongModel> songs, int artistCount = 3)
    {
        var songLookup = songs.ToDictionary(s => s.Id);
        var topArtists = allEvents.Where(e => e.SongId.HasValue && songLookup.ContainsKey(e.SongId.Value))
            .GroupBy(e => songLookup[e.SongId!.Value].ArtistName).OrderByDescending(g => g.Count())
            .Take(artistCount).Select(g => g.Key).ToHashSet();

        return allEvents
            .Where(e => e.SongId.HasValue && songLookup.ContainsKey(e.SongId.Value) && topArtists.Contains(songLookup[e.SongId.Value].ArtistName) && e.EventDate.HasValue)
            .GroupBy(e => new { Artist = songLookup[e.SongId.Value].ArtistName, Hour = e.EventDate!.Value.Hour })
            .Select(g => new DimmerStats { Category = g.Key.Artist, Name = $"{g.Key.Hour:00}:00", Count = g.Count() })
            .ToList();
    }
    #endregion

    //==========================================================================
    #region 3. For Cartesian: Column / Bar Charts
    //==========================================================================

    /// <summary>
    /// [Global] Ranks artists by total unique songs played.
    /// Insight: "Which artists' discographies have I explored the most?"
    /// Chart Suggestions: 1. BarSeries, 2. FunnelSeries, 3. PyramidSeries
    /// </summary>
    public static List<DimmerStats> GetTopArtistsBySongVariety(IReadOnlyCollection<DimmerPlayEvent> allEvents, IReadOnlyCollection<SongModel> songs, int count)
    {
        // Your existing method is perfect.
        var songLookup = songs.ToDictionary(s => s.Id);
        return allEvents.Where(e => e.SongId.HasValue && songLookup.ContainsKey(e.SongId.Value))
            .Select(e => songLookup[e.SongId.Value]).GroupBy(s => s.ArtistName)
            .Select(g => new DimmerStats { Name = g.Key, Count = g.Select(s => s.Id).Distinct().Count() })
            .OrderByDescending(s => s.Count).Take(count).ToList();
    }

    /// <summary>
    /// [Global] Ranks genres by total listening time.
    /// Insight: "Which genre do I spend the most time listening to?"
    /// Chart Suggestions: 1. ColumnSeries, 2. BarSeries, 3. WaterfallSeries
    /// </summary>
    public static List<DimmerStats> GetTopGenresByListeningTime(IReadOnlyCollection<DimmerPlayEvent> allEvents, IReadOnlyCollection<SongModel> songs, int count)
    {
        var songLookup = songs.ToDictionary(s => s.Id);
        return allEvents
            .Where(e => e.SongId.HasValue && e.DateFinished > e.DatePlayed && songLookup.ContainsKey(e.SongId.Value) && !string.IsNullOrEmpty(songLookup[e.SongId.Value].Genre?.Name))
            .GroupBy(e => songLookup[e.SongId.Value].Genre!.Name)
            .Select(g => new DimmerStats { Name = g.Key, Value = g.Sum(ev => (ev.DateFinished - ev.DatePlayed).TotalSeconds) })
            .OrderByDescending(s => s.Value).Take(count).ToList();
    }
    #endregion

    //==========================================================================
    #region 4. For Cartesian: Line / Spline / Step / FastLine Charts
    //==========================================================================

    /// <summary>
    /// [Single Song] Tracks the play count of a single song over time by month.
    /// Insight: "Has my interest in this song faded or grown over time?"
    /// Chart Suggestions: 1. LineSeries, 2. SplineSeries, 3. StepLineSeries
    /// </summary>
    public static List<DimmerStats> GetSongPlayHistoryOverTime(IReadOnlyCollection<DimmerPlayEvent> songEvents)
    {
        // Your existing method is great.
        return songEvents.Where(e => e.PlayType == PlayType_Completed && e.EventDate.HasValue)
            .GroupBy(e => new { e.EventDate!.Value.Year, e.EventDate!.Value.Month })
            .Select(g => new DimmerStats { Date = new DateTimeOffset(g.Key.Year, g.Key.Month, 1, 0, 0, 0, TimeSpan.Zero), Count = g.Count() })
            .OrderBy(s => s.Date).ToList();
    }

    /// <summary>
    /// [Global] Shows your total listening volume (play count) for every day in a period.
    /// Insight: "How does my listening activity fluctuate day-to-day?"
    /// Chart Suggestions: 1. FastLineSeries, 2. AreaSeries, 3. ColumnSeries
    /// </summary>
    public static List<DimmerStats> GetDailyListeningVolume(IReadOnlyCollection<DimmerPlayEvent> allEvents, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        return allEvents
            .Where(e => e.EventDate >= startDate && e.EventDate < endDate && e.PlayType is PlayType_Play or PlayType_Completed)
            .GroupBy(e => e.EventDate!.Value.Date)
            .Select(g => new DimmerStats { Date = new DateTimeOffset(g.Key), Count = g.Count() })
            .OrderBy(s => s.Date).ToList();
    }
    #endregion

    //==========================================================================
    #region 5. For Cartesian: Stacked Charts (Column/Area)
    //==========================================================================

    /// <summary>
    /// [Global] Shows the breakdown of plays per device for your top artists.
    /// Insight: "Do I listen to Artist A on my desktop and Artist B on my phone?"
    /// Chart Suggestions: 1. StackedColumnSeries, 2. StackedBarSeries, 3. StackedArea100Series
    /// </summary>
    public static List<DimmerStats> GetDeviceUsageByTopArtists(IReadOnlyCollection<DimmerPlayEvent> allEvents, IReadOnlyCollection<SongModel> songs, int topArtistCount)
    {
        // Your existing method is perfect.
        var songLookup = songs.ToDictionary(s => s.Id);
        var topArtists = allEvents.GroupBy(e => e.SongId).Where(g => g.Key.HasValue && songLookup.ContainsKey(g.Key.Value))
            .GroupBy(g => songLookup[g.Key.Value].ArtistName).OrderByDescending(g => g.Count()).Take(topArtistCount).Select(g => g.Key).ToHashSet();

        return allEvents.Where(e => e.SongId.HasValue && songLookup.ContainsKey(e.SongId.Value) && !string.IsNullOrEmpty(e.DeviceName) && topArtists.Contains(songLookup[e.SongId.Value].ArtistName))
            .GroupBy(e => new { Artist = songLookup[e.SongId.Value].ArtistName, Device = e.DeviceName! })
            .Select(g => new DimmerStats { Name = g.Key.Artist, Category = g.Key.Device, Count = g.Count() }).ToList();
    }

    /// <summary>
    /// [Global] Shows how your taste in top genres has evolved over time.
    /// Insight: "Was I listening to more Rock last year and more Electronic this year?"
    /// Chart Suggestions: 1. StackedAreaSeries, 2. StackedLineSeries, 3. StackedColumn100Series
    /// </summary>
    public static List<DimmerStats> GetGenrePopularityOverTime(IReadOnlyCollection<DimmerPlayEvent> allEvents, IReadOnlyCollection<SongModel> songs, int genreCount)
    {
        var songLookup = songs.ToDictionary(s => s.Id);
        var topGenres = allEvents.Where(e => e.SongId.HasValue && songLookup.ContainsKey(e.SongId.Value) && songLookup[e.SongId.Value].Genre?.Name != null)
            .GroupBy(e => songLookup[e.SongId.Value].Genre!.Name).OrderByDescending(g => g.Count()).Take(genreCount).Select(g => g.Key).ToHashSet();

        return allEvents
            .Where(e => e.SongId.HasValue && songLookup.ContainsKey(e.SongId.Value) && songLookup[e.SongId.Value].Genre?.Name != null && topGenres.Contains(songLookup[e.SongId.Value].Genre!.Name) && e.EventDate.HasValue)
            .GroupBy(e => new { Genre = songLookup[e.SongId.Value].Genre!.Name, Month = new DateTime(e.EventDate.Value.Year, e.EventDate.Value.Month, 1) })
            .Select(g => new DimmerStats { Date = new DateTimeOffset(g.Key.Month), Category = g.Key.Genre, Count = g.Count() })
            .OrderBy(s => s.Date).ToList();
    }
    #endregion

    //==========================================================================
    #region 6. For Cartesian: Range Charts (Column/Area)
    //==========================================================================

    /// <summary>
    /// [Global] For each day of the week, shows the earliest and latest hour you listen.
    /// Insight: "What is my daily listening window? Do I listen later on weekends?"
    /// Chart Suggestions: 1. RangeColumnSeries, 2. RangeAreaSeries, 3. SplineRangeAreaSeries
    /// </summary>
    public static List<DimmerStats> GetDailyListeningTimeRange(IReadOnlyCollection<DimmerPlayEvent> allEvents)
    {
        return allEvents
            .Where(e => e.PlayType is PlayType_Play or PlayType_Completed && e.EventDate.HasValue)
            .GroupBy(e => e.EventDate!.Value.DayOfWeek)
            .Select(g => new DimmerStats
            {
                Name = g.Key.ToString(),
                Low = g.Min(e => e.EventDate!.Value.Hour),
                High = g.Max(e => e.EventDate!.Value.Hour)
            })
            .OrderBy(s => (int)Enum.Parse<DayOfWeek>(s.Name))
            .ToList();
    }

    /// <summary>
    /// [Global] For top songs, compares full duration vs. average actual listening time.
    /// Insight: "Which songs do I consistently fail to finish?"
    /// Chart Suggestions: 1. RangeBarSeries, 2. RangeColumnSeries, 3. ScatterSeries (plot duration vs avg listen)
    /// </summary>
    public static List<DimmerStats> GetSongDurationVsListenTime(IReadOnlyCollection<DimmerPlayEvent> allEvents, IReadOnlyCollection<SongModel> songs, int count)
    {
        var songLookup = songs.ToDictionary(s => s.Id);
        var topSongs = allEvents.Where(e => e.PlayType == PlayType_Completed && e.SongId.HasValue)
            .GroupBy(e => e.SongId.Value).OrderByDescending(g => g.Count()).Take(count).Select(g => g.Key).ToHashSet();

        return allEvents
            .Where(e => e.SongId.HasValue && topSongs.Contains(e.SongId.Value) && e.PositionInSeconds > 0)
            .GroupBy(e => e.SongId.Value)
            .Select(g => new DimmerStats
            {
                Song = songLookup.ContainsKey(g.Key) ? songLookup[g.Key].ToModelView() : null,
                HighDouble = songLookup.ContainsKey(g.Key) ? songLookup[g.Key].DurationInSeconds : 0,
                LowDouble = g.Average(e => e.PositionInSeconds)
            })
            .Where(s => s.Song != null && s.High > 0)
            .OrderByDescending(s => s.High - s.Low) // Order by largest discrepancy
            .ToList();
    }
    #endregion

    //==========================================================================
    #region 7. For Cartesian: Scatter / Bubble Charts
    //==========================================================================

    /// <summary>
    /// [Single Song] Identifies the exact moments in a song where the user skips away.
    /// Insight: "Do I always skip this song's intro? Where do I get bored?"
    /// Chart Suggestions: 1. ScatterSeries, 2. HistogramSeries (on Value), 3. BoxAndWhiskerSeries (on Value)
    /// </summary>
    public static List<DimmerStats> GetSongDropOffPoints(IReadOnlyCollection<DimmerPlayEvent> songEvents)
    {
        // Your method is perfect.
        return songEvents.Where(e => e.PlayType == PlayType_Skipped && e.PositionInSeconds > 0)
            .Select(e => new DimmerStats { Name =e.SongName, Date = e.EventDate ?? DateTimeOffset.MinValue, Value = e.PositionInSeconds })
            .OrderBy(s => s.Date).ToList();
    }

    /// <summary>
    /// [Global] Plots songs by their play count vs. their duration, with popularity as bubble size.
    /// Insight: "Do I prefer short, repetitive songs or long, epic ones? Are my highest-rated songs long or short?"
    /// Chart Suggestions: 1. BubbleSeries, 2. ScatterSeries, 3. FastLineSeries (if sorted by one axis)
    /// </summary>
    public static List<DimmerStats> GetSongProfileBubbleChart(IReadOnlyCollection<DimmerPlayEvent> allEvents, IReadOnlyCollection<SongModel> songs)
    {
        var songLookup = songs.ToDictionary(s => s.Id);
        return allEvents
            .Where(e => e.SongId.HasValue && songLookup.ContainsKey(e.SongId.Value))
            .GroupBy(e => e.SongId.Value)
            .Select(g => new
            {
                SongId = g.Key,
                PlayCount = g.Count(e => e.PlayType == PlayType_Completed)
            })
            .Where(s => s.PlayCount > 0)
            .Select(s => new DimmerStats
            {
                Song = songLookup[s.SongId].ToModelView(),
                Count = s.PlayCount, // X-Axis: Play Count
                Value = songLookup[s.SongId].DurationInSeconds, // Y-Axis: Duration
                Size = songLookup[s.SongId].Rating == 0 ? 1 : songLookup[s.SongId].Rating // Bubble Size: Rating
            })
            .ToList();
    }
    #endregion

    //==========================================================================
    #region 8. For Cartesian: Waterfall Charts
    //==========================================================================

    /// <summary>
    /// [Global] Shows how your music library has grown (or shrunk) over time.
    /// Insight: "How many songs did I add each month to reach my current library size?"
    /// Chart Suggestions: 1. WaterfallSeries, 2. ColumnSeries, 3. StackedAreaSeries (if tracking adds vs deletes)
    /// </summary>
    public static List<DimmerStats> GetLibraryGrowthWaterfall(IReadOnlyCollection<SongModel> songs)
    {
        return songs
            .Where(s => s.DateCreated.HasValue)
            .GroupBy(s => new { s.DateCreated!.Value.Year, s.DateCreated!.Value.Month })
            .Select(g => new DimmerStats
            {
                Name = $"{g.Key.Year}-{g.Key.Month:D2}",
                Count = g.Count()
            })
            .OrderBy(s => s.Name).ToList();
    }

    /// <summary>
    /// [Global] Shows how each of your top artists contributes to your total skip count.
    /// Insight: "Which artists are the biggest contributors to my total skips?"
    /// Chart Suggestions: 1. WaterfallSeries, 2. PieSeries, 3. BarSeries
    /// </summary>
    public static List<DimmerStats> GetSkipContributionByArtist(IReadOnlyCollection<DimmerPlayEvent> allEvents, IReadOnlyCollection<SongModel> songs, int artistCount)
    {
        var songLookup = songs.ToDictionary(s => s.Id);
        return allEvents
            .Where(e => e.PlayType == PlayType_Skipped && e.SongId.HasValue && songLookup.ContainsKey(e.SongId.Value))
            .GroupBy(e => songLookup[e.SongId.Value].ArtistName)
            .Select(g => new DimmerStats { Name = g.Key, Count = g.Count() })
            .OrderByDescending(s => s.Count)
            .Take(artistCount)
            .ToList();
    }
    #endregion

    //==========================================================================
    #region 9. For Cartesian: Box and Whisker Charts
    //==========================================================================

    /// <summary>
    /// [Global] Shows the distribution of song release years for your top genres.
    /// Insight: "Is my Rock collection mostly 70s music? Is my Pop collection from the 2010s?"
    /// Chart Suggestions: 1. BoxAndWhiskerSeries, 2. ViolinSeries (if available), 3. StackedBar100Series
    /// </summary>
    public static List<DimmerStats> GetReleaseYearDistributionByGenre(IReadOnlyCollection<SongModel> songs, int genreCount)
    {
        // Box Plot requires a collection of values for each category.
        // This is complex and often requires a different model or manual processing in the ViewModel.
        // This method returns the raw data; you'd group it in the VM before passing to the chart.
        var topGenres = songs.Where(s => s.Genre?.Name != null).GroupBy(s => s.Genre!.Name)
            .OrderByDescending(g => g.Count()).Take(genreCount).Select(g => g.Key).ToHashSet();

        return songs
            .Where(s => s.ReleaseYear.HasValue && s.Genre?.Name != null && topGenres.Contains(s.Genre.Name))
            .Select(s => new DimmerStats { Category = s.Genre!.Name, Value = s.ReleaseYear.Value })
            .ToList();
    }

    /// <summary>
    /// [Global] Shows the distribution of play counts for artists within each genre.
    /// Insight: "Does my 'Indie' genre have many artists with few plays, while 'Pop' has a few superstars?"
    /// Chart Suggestions: 1. BoxAndWhiskerSeries, 2. ViolinSeries (if available), 3. HistogramSeries
    /// </summary>
    public static List<DimmerStats> GetArtistPopularityDistributionByGenre(IReadOnlyCollection<DimmerPlayEvent> allEvents, IReadOnlyCollection<SongModel> songs)
    {
        var songLookup = songs.ToDictionary(s => s.Id);
        var artistPlayCounts = allEvents
            .Where(e => e.SongId.HasValue && songLookup.ContainsKey(e.SongId.Value))
            .GroupBy(e => new { Artist = songLookup[e.SongId.Value].ArtistName, Genre = songLookup[e.SongId.Value].Genre?.Name })
            .Where(g => g.Key.Genre != null)
            .Select(g => new { g.Key.Artist, g.Key.Genre, PlayCount = g.Count() });

        // Again, this returns the raw data to be processed into lists for the Box Plot.
        return artistPlayCounts
            .Select(a => new DimmerStats { Category = a.Genre!, Value = a.PlayCount })
            .ToList();
    }
    #endregion

    //==========================================================================
    #region 10. For Cartesian: Histogram Charts
    //==========================================================================

    /// <summary>
    /// [Global] Shows the frequency distribution of song durations in your library.
    /// Insight: "Is my library mostly composed of 3-minute songs, or do I have many long tracks?"
    /// Chart Suggestions: 1. HistogramSeries, 2. BoxAndWhiskerSeries, 3. ColumnSeries (if binned manually)
    /// </summary>
    public static List<DimmerStats> GetSongDurationHistogram(IReadOnlyCollection<SongModel> songs)
    {
        // Histogram takes a simple list of values.
        return songs.Select(s => new DimmerStats { Value = s.DurationInSeconds }).ToList();
    }

    /// <summary>
    /// [Global] Shows the frequency distribution of your listening sessions' length.
    /// Insight: "Do I typically listen for 30 seconds (skips), 3 minutes, or 5+ minutes at a time?"
    /// Chart Suggestions: 1. HistogramSeries, 2. BoxAndWhiskerSeries, 3. StepAreaSeries
    /// </summary>
    public static List<DimmerStats> GetListeningSessionDurationHistogram(IReadOnlyCollection<DimmerPlayEvent> allEvents)
    {
        return allEvents
            .Where(e => e.DateFinished > e.DatePlayed)
            .Select(e => new DimmerStats { Value = (e.DateFinished - e.DatePlayed).TotalSeconds })
            .Where(s => s.Value > 0 && s.Value < 3600) // Filter out noise
            .ToList();
    }
    #endregion

    //==========================================================================
    #region 11. For Cartesian: OHLC / Candle Charts
    //==========================================================================

    /// <summary>
    /// [Single Song] Shows the weekly listening trend for a song.
    /// Insight: "How did my listening for this song fluctuate each week?"
    /// Chart Suggestions: 1. CandleSeries, 2. OHLCSeries, 3. RangeColumnSeries
    /// </summary>
    public static List<DimmerStats> GetSongWeeklyOHLC(IReadOnlyCollection<DimmerPlayEvent> songEvents)
    {
        return songEvents
            .Where(e => e.EventDate.HasValue)
            .GroupBy(e => System.Globalization.ISOWeek.GetWeekOfYear(e.EventDate.Value.DateTime))
            .Select(g =>
            {
                var dailyCounts = g.GroupBy(ev => ev.EventDate!.Value.DayOfWeek)
                                   .ToDictionary(dg => dg.Key, dg => dg.Count());
                return new DimmerStats
                {
                    Date = g.First().EventDate.Value,
                    Open = dailyCounts.TryGetValue(DayOfWeek.Monday, out var o) ? o : 0,
                    Close = dailyCounts.TryGetValue(DayOfWeek.Sunday, out var c) ? c : 0,
                    High = dailyCounts.Any() ? dailyCounts.Values.Max() : 0,
                    Low = dailyCounts.Any() ? dailyCounts.Values.Min() : 0,
                };
            })
            .OrderBy(s => s.Date).ToList();
    }

    /// <summary>
    /// [Global] Shows the daily range of listening hours.
    /// Insight: "What's my daily listening routine? (Start, End, Peak, Low)"
    /// Chart Suggestions: 1. CandleSeries, 2. OHLCSeries, 3. RangeAreaSeries
    /// </summary>
    public static List<DimmerStats> GetDailyListeningRoutineOHLC(IReadOnlyCollection<DimmerPlayEvent> allEvents)
    {
        return allEvents
            .Where(e => e.EventDate.HasValue)
            .GroupBy(e => e.EventDate!.Value.Date)
            .Select(g =>
            {
                var hourlyCounts = g.GroupBy(ev => ev.EventDate!.Value.Hour)
                                    .ToDictionary(hg => hg.Key, hg => hg.Count());
                return new DimmerStats
                {
                    Date = new DateTimeOffset(g.Key),
                    Open = g.Min(e => e.EventDate!.Value.Hour),
                    Close = g.Max(e => e.EventDate!.Value.Hour),
                    High = hourlyCounts.Any() ? hourlyCounts.OrderByDescending(kvp => kvp.Value).First().Key : 0,
                    Low = hourlyCounts.Any() ? hourlyCounts.OrderBy(kvp => kvp.Value).First().Key : 0
                };
            })
            .OrderBy(s => s.Date).ToList();
    }
    #endregion
}