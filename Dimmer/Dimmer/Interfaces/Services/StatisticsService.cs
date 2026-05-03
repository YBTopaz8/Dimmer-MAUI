using Dimmer.Utilities.StatsUtils.Albums;
using Dimmer.Utilities.StatsUtils.Artists;


namespace Dimmer.Interfaces.Services;

// A filter enum for the UI. This is much cleaner than passing dates around.
public enum DateRangeFilter { Last7Days, Last30Days, Last90Days, Last365Days, AllTime }


// A container for all stats related to a single song.
public partial class SongStatsBundle :ObservableObject
{
    public SongStatTwo.SongSingleStatsSummary Summary { get; set; }
    public List<LabelValue> PlayTypeDistribution { get; set; }
    public List<LabelValue> PlayDistributionByHour { get; set; }
    public DimmerStats ListeningStreak { get; set; }
    public DimmerStats FirstImpression { get; set; }
    public List<DimmerStats> DropOffPoints { get; set; }
    public string Title { get; internal set; }
    public string Subtitle { get; internal set; }
    public DateRangeFilter FilterUsed { get; internal set; }
    public List<DimmerStats> PlayHistoryOverTime { get; set; }
    public DimmerStats PowerHour { get; set; }
    public List<DimmerStats> DailyListeningRhythm { get; set; }

    // 1. Engagement Funnel (FunnelChart)
    public List<LabelValue> EngagementFunnel { get; set; } = new();
    // 2. Where Do They Skip? (ScatterSeries)
    public List<DimmerStats> SkipHotspots { get; set; } = new();
    // 3. Rewind & Replay Hotspots (ColumnSeries)
    public List<LabelValue> RewindHotspots10sBins { get; set; } = new();
    // 4. Time-of-Day Heatmap (HeatLandSeries/Matrix)
    public List<DimmerStats> ListeningHeatmap { get; set; } = new();
    // 5. Lyric Sentiment Timeline (LineSeries)
    public List<DimmerStats> KeywordSentimentTimeline { get; set; } = new();
    // 6. Device Ecosystem (PieSeries/Donut)
    public List<LabelValue> DeviceEcosystem { get; set; } = new();
    // 8. Action Radar (PolarLineSeries)
    public List<LabelValue> ActionRadar { get; set; } = new();
    // 10. Loop Segment Tracker (Gantt/RangeBarSeries)
    public List<DimmerStats> LoopedSegments { get; set; } = new();
    // 12. Audio Quality Profile (GaugeSeries)
    public DimmerStats AudioQualityGauge { get; set; } = new();



}
// --- Single Artist stats ---
public class ArtistStatsBundle : StatsBundleBase
{
    public ArtistStats.ArtistSingleStatsSummary Summary { get; set; }
    public ArtistStats.ArtistPlottableData PlottableData { get; set; }
    // 23. Era/Decade Distribution (Histogram/ColumnSeries)
    public List<LabelValue> DecadeDistribution { get; set; } = new();
    // 24. Most Obsessed-Over Songs Formula (HorizontalBarSeries)
    public List<LabelValue> ObsessionRankedSongs { get; set; } = new();
    // 25. Genre Blending (PieSeries / PolarSeries)
    public List<LabelValue> GenreBlending { get; set; } = new();
    // 27. Listening Footprint by Device (Treemap / Nested Pie)
    public List<LabelValue> DeviceFootprint { get; set; } = new();
    // 28. Discovery Velocity (LineSeries - Cumulative)
    public List<DimmerStats> DiscoveryVelocity { get; set; } = new();
    // 30. Collaborator Network (Bubble/Scatter - approximations)
    public List<LabelValue> CollaboratorNetwork { get; set; } = new();
    // 31. Vibe/BPM Spread (ScatterSeries)
    public List<DimmerStats> BpmVersusEngagement { get; set; } = new();
    // 32. Composer Spotlight (StackedColumnSeries)
    public List<LabelValue> ComposerSpotlight { get; set; } = new();
}

// --- Single Album stats ---
public class AlbumStatsBundle : StatsBundleBase
{
    public AlbumStats.AlbumSingleStatsSummary Summary { get; set; }
    public AlbumStats.AlbumPlottableData PlottableData { get; set; }
    public int AlbumEddingtonNumber { get; internal set; }
    public List<DimmerStats> TrackEventBreakdown { get; internal set; }
    public List<LabelValue> LyricalDensityPerTrack { get; internal set; }
    public List<LabelValue> AlbumDropOffCurve { get; internal set; }
    public List<LabelValue> InstrumentalVsVocalPlays { get; internal set; }
}

// --- Comparison stats ---
public class ComparisonStatsBundle : StatsBundleBase
{
    public ArtistStats.ArtistComparisonResult? ArtistComparison { get; set; }
   
}

public class StatisticsService
{
    
        private readonly IRepository<SongModel> _songRepo;
        private readonly IRepository<ArtistModel> _artistRepo;
        private readonly IRepository<AlbumModel> _albumRepo;  
        private readonly IDimmerPlayEventRepository _eventRepo;
        private readonly ILogger<StatisticsService> _logger;

    public StatisticsService(
           IRepository<SongModel> songRepo,
           IRepository<ArtistModel> artistRepo,
           IRepository<AlbumModel> albumRepo,  
           IDimmerPlayEventRepository eventRepo,
           ILogger<StatisticsService> logger)
        {
            _songRepo = songRepo;
            _artistRepo = artistRepo;
            _albumRepo = albumRepo;  
            _eventRepo = eventRepo;
            _logger = logger;

       }

        /// <summary>
        /// The main method to get all library-wide statistics based on a filter.
        /// This is the only method the ViewModel needs to call for global stats.
        /// </summary>
    public LibraryStatsBundle GetLibraryStatistics(DateRangeFilter filter
        )
    {
        _logger.LogInformation("Calculating library statistics for filter: {Filter}", filter);

       
        var endDate = DateTimeOffset.UtcNow;
        DateTimeOffset? startDate = filter switch
        {
            DateRangeFilter.Last7Days => endDate.AddDays(-7),
            DateRangeFilter.Last30Days => endDate.AddDays(-30),
            DateRangeFilter.Last90Days => endDate.AddDays(-90),
            DateRangeFilter.Last365Days => endDate.AddYears(-1),
            DateRangeFilter.AllTime => null,
            _ => null
        };

       
       
        var allSongs =  _songRepo.GetAllAsQueryable();


        var filteredEvents = _eventRepo.GetEventsInDateRangeAsync(startDate, endDate);
        int topCount = 15;

        var bundle = new LibraryStatsBundle
        {
            Title = "Library Overview",
            Subtitle = $"Stats for: {filter.ToString().Replace("Last", "Last ")}",
            FilterUsed = filter,
            
            CollectionSummary = CollectionStats.GetSummary(allSongs, filteredEvents),

           
            TopSongsByPlays = TopStats.GetTopCompletedSongs(allSongs, filteredEvents, topCount),
            TopSongsByTime = TopStats.GetTopSongsByListeningTime(allSongs, filteredEvents, topCount),
            TopArtistsByPlays = TopStats.GetTopCompletedArtists(allSongs, filteredEvents, topCount),
            TopAlbumsByPlays = TopStats.GetTopCompletedAlbums(allSongs, filteredEvents, topCount),
            TopSongsBySkips = TopStats.GetTopSkippedSongs(allSongs, filteredEvents, topCount),
            TopArtistsBySkips = TopStats.GetTopSkippedArtists(allSongs, filteredEvents, topCount),
            TopArtistsByVariety = TopStats.GetTopArtistsBySongVariety(filteredEvents, allSongs, topCount),
            TopRediscoveredSongs = TopStats.GetTopRediscoveredSongs(filteredEvents, allSongs, topCount),
            TopBurnoutSongs = TopStats.GetTopBurnoutSongs(filteredEvents, allSongs, topCount),

           
            ArtistFootprint = TopStats.GetArtistFootprint(allSongs, filteredEvents),
            GenrePopularityOverTime = ChartSpecificStats.GetGenrePopularityOverTime(filteredEvents, allSongs),
            DailyListeningRoutineOHLC = ChartSpecificStats.GetDailyListeningRoutineOHLC(filteredEvents, allSongs, startDate ?? DateTimeOffset.MinValue, endDate),
            OverallListeningByDayOfWeek = ChartSpecificStats.GetOverallListeningByDayOfWeek(filteredEvents, allSongs)
        };

        return bundle;
    }

    /// <summary>
    /// Gets all relevant statistics for a single selected song.
    /// </summary>
    public SongStatsBundle? GetSongStatistics(ObjectId songId, DateRangeFilter filter)
    {
        var songDb =  _songRepo.GetById(songId);
        if (songDb == null)
            return null;

       
        var allSongEvents =  _eventRepo.GetAllAsQueryable().AsEnumerable().Where(x=>x.SongId==songId);
        if (!allSongEvents.Any())
            return null;

        var endDate = DateTimeOffset.UtcNow;
        DateTimeOffset? startDate = filter switch
        {
            DateRangeFilter.Last7Days => endDate.AddDays(-7),
            DateRangeFilter.Last30Days => endDate.AddDays(-30),
            DateRangeFilter.Last90Days => endDate.AddDays(-90),
            DateRangeFilter.Last365Days => endDate.AddYears(-1),
            DateRangeFilter.AllTime => null,
            _ => null
        };


        var filteredEvents = allSongEvents
            .Where(e => (!startDate.HasValue || e.EventDate >= startDate.Value) &&
                        (e.EventDate < endDate)).ToList()
            ;

        if (filteredEvents.Count == 0)
            return null;

        var bundle = new SongStatsBundle
        {
            Title = songDb.Title,
            Subtitle = songDb.ArtistName,
            FilterUsed = filter,
            Summary = SongStatTwo.GetSingleSongSummary(songDb, filteredEvents),
            PlayTypeDistribution = SongStatTwo.GetPlayTypeDistribution(songDb, filteredEvents),
            PlayDistributionByHour = SongStatTwo.GetPlayCountPerHourOfDay(songDb, filteredEvents),
            ListeningStreak = TopStats.GetListeningStreak(filteredEvents),
            FirstImpression = TopStats.GetSongsFirstImpression(allSongEvents.ToList()),
            PowerHour = TopStats.GetPowerHour(allSongEvents.ToList()),
            DailyListeningRhythm = TopStats.GetDailyListeningRhythm(allSongEvents.ToList(),songDb),
            DropOffPoints = ChartSpecificStats.GetSongDropOffPoints(filteredEvents),
            PlayHistoryOverTime = ChartSpecificStats.GetSongPlayHistoryOverTime(filteredEvents),


            EngagementFunnel = SongExtensiveStats.GetEngagementFunnel(songDb),
             ActionRadar = SongExtensiveStats.GetActionRadar(songDb),

        };
        return bundle;
    }

    public ArtistStatsBundle? GetArtistStatisticsAsync(ObjectId artistId, DateRangeFilter filter)
    {
        var artist =  _artistRepo.GetById(artistId);
        if (artist == null)
            return null;

       
        var endDate = DateTimeOffset.UtcNow;
        DateTimeOffset? startDate = filter switch
        {
            DateRangeFilter.Last7Days => endDate.AddDays(-7),
            DateRangeFilter.Last30Days => endDate.AddDays(-30),
            DateRangeFilter.Last90Days => endDate.AddDays(-90),
            DateRangeFilter.Last365Days => endDate.AddYears(-1),
            DateRangeFilter.AllTime => null,
            _ => null
        };

        var allSongs =  _songRepo.GetAll();
        var artistSongs = allSongs.Where(s => s.Artist?.Id == artistId).ToList();

        var filteredEvents =  _eventRepo.GetEventsInDateRangeAsync(startDate, endDate);

        var bundle = new ArtistStatsBundle
        {
            Title = artist.Name,
            Subtitle = "Artist Overview",
            FilterUsed = filter,
            Summary = ArtistStats.GetSingleArtistStats(artist, allSongs, filteredEvents),
            PlottableData = ArtistStats.GetSingleArtistPlottableData(artist, allSongs, filteredEvents),

            DecadeDistribution = ArtistExtensiveStats.GetDecadeDistribution(artistSongs),
            ObsessionRankedSongs = ArtistExtensiveStats.GetObsessionRankings(artistSongs),
            CollaboratorNetwork = ArtistExtensiveStats.GetCollaborators(artistSongs),
            BpmVersusEngagement = ArtistExtensiveStats.GetBpmVsEngagement(artistSongs),

            // Quick LINQ for genre blending
            GenreBlending = artistSongs
            .Where(s => !string.IsNullOrEmpty(s.GenreName))
            .GroupBy(s => s.GenreName)
            .Select(g => new LabelValue { Label = g.Key, Value = g.Sum(x => x.PlayCount) })
            .ToList()


        };
        return bundle;
    }

    public AlbumStatsBundle? GetAlbumStatisticsAsync(ObjectId albumId, DateRangeFilter filter)
    {
        var album =  _albumRepo.GetById(albumId);
        if (album == null)
            return null;

       
        var endDate = DateTimeOffset.UtcNow;
        DateTimeOffset? startDate = filter switch
        {
            DateRangeFilter.Last7Days => endDate.AddDays(-7),
            DateRangeFilter.Last30Days => endDate.AddDays(-30),
            DateRangeFilter.Last90Days => endDate.AddDays(-90),
            DateRangeFilter.Last365Days => endDate.AddYears(-1),
            DateRangeFilter.AllTime => null,
            _ => null
        };

        var allSongs =  _songRepo.GetAllAsQueryable();
        var filteredEvents =  _eventRepo.GetEventsInDateRangeAsync(startDate, endDate);
        var albumSongs = allSongs.Where(s => s.Album.Id == albumId).ToList();

        var bundle = new AlbumStatsBundle
        {
            Title = album.Name,
            Subtitle = $"Album by {album.Artist?.Name}",
            FilterUsed = filter,
            Summary = AlbumStats.GetSingleAlbumStats(album, allSongs, filteredEvents),
            PlottableData = AlbumStats.GetSingleAlbumPlottableData(album, allSongs, filteredEvents),

            // --- NEW Advanced Visualizations ---
            AlbumDropOffCurve = AlbumExtensiveStats.GetDropOffCurve(albumSongs),
            LyricalDensityPerTrack = AlbumExtensiveStats.GetLyricalDensity(albumSongs),
            TrackEventBreakdown = AlbumExtensiveStats.GetTrackEventBreakdown(albumSongs),
            AlbumEddingtonNumber = AlbumExtensiveStats.CalculateEddingtonNumber(albumSongs),

            // Quick LINQ inline for simple ones
            InstrumentalVsVocalPlays = new List<LabelValue>
        {
            new() { Label = "Instrumental", Value = albumSongs.Where(s => s.IsInstrumental == true).Sum(s => s.PlayCount) },
            new() { Label = "Vocal", Value = albumSongs.Where(s => s.IsInstrumental == false || s.IsInstrumental == null).Sum(s => s.PlayCount) }
        }
        };
        return bundle;
    }


}
public abstract class StatsBundleBase
{
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public DateRangeFilter FilterUsed { get; set; }
    public CollectionStatsSummary? CollectionSummary { get; set; }
}
public class LibraryStatsBundle : StatsBundleBase
{
    public List<DimmerStats>? TopSongsByPlays { get; set; }
    public List<DimmerStats>? TopSongsByTime { get; set; }
    public List<DimmerStats>? TopArtistsByPlays { get; set; }
    public List<DimmerStats>? TopAlbumsByPlays { get; set; }
    public List<DimmerStats>? TopSongsBySkips { get; set; }
    public List<DimmerStats>? TopArtistsBySkips { get; set; }
    public List<DimmerStats>? TopArtistsByVariety { get; set; }
    public List<DimmerStats>? TopRediscoveredSongs { get; set; }
    public List<DimmerStats>? TopBurnoutSongs { get; set; }
    public List<DimmerStats>? ArtistFootprint { get; set; }

   
    public List<DimmerStats>? GenrePopularityOverTime { get; set; }
    public List<DimmerStats>? DailyListeningRoutineOHLC { get; set; }
    public List<DimmerStats>? OverallListeningByDayOfWeek { get; set; }
    public List<DimmerStats>? TopCompletedSongs { get; internal set; }
    public List<DimmerStats>? MostSkippedSongs { get; internal set; }
}
