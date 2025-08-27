using Dimmer.Data.ModelView.NewFolder;
using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.Utilities.StatsUtils;
using Dimmer.Utilities.StatsUtils.Albums;
using Dimmer.Utilities.StatsUtils.Artists;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Interfaces.Services;

// A filter enum for the UI. This is much cleaner than passing dates around.
public enum DateRangeFilter { Last7Days, Last30Days, Last90Days, Last365Days, AllTime }


// A container for all stats related to a single song.
public class SongStatsBundle
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
}
// --- Single Artist stats ---
public class ArtistStatsBundle : StatsBundleBase
{
    public ArtistStats.ArtistSingleStatsSummary Summary { get; set; }
    public ArtistStats.ArtistPlottableData PlottableData { get; set; }
}

// --- Single Album stats ---
public class AlbumStatsBundle : StatsBundleBase
{
    public AlbumStats.AlbumSingleStatsSummary Summary { get; set; }
    public AlbumStats.AlbumPlottableData PlottableData { get; set; }
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
        public async Task<LibraryStatsBundle> GetLibraryStatisticsAsync(DateRangeFilter filter)
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

       
       
        var allSongs = await _songRepo.GetAllAsync();

        
        var filteredEvents = await _eventRepo.GetEventsInDateRangeAsync(startDate, endDate);
        int topCount = 10;

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
    public SongStatsBundle? GetSongStatisticsAsync(ObjectId songId, DateRangeFilter filter)
    {
        var songDb =  _songRepo.GetById(songId);
        if (songDb == null)
            return null;

       
        var allSongEvents =  _eventRepo.GetAll().Where(x=>x.SongId==songId).ToList().AsReadOnly();
        if (allSongEvents.Count == 0)
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
                        (e.EventDate < endDate))
            .ToList();

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
            FirstImpression = TopStats.GetSongsFirstImpression(allSongEvents),
            DropOffPoints = ChartSpecificStats.GetSongDropOffPoints(filteredEvents)
        };
        return bundle;
    }

    public async Task<ArtistStatsBundle?> GetArtistStatisticsAsync(ObjectId artistId, DateRangeFilter filter)
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

        var allSongs = await _songRepo.GetAllAsync();
        var filteredEvents = await _eventRepo.GetEventsInDateRangeAsync(startDate, endDate);

        var bundle = new ArtistStatsBundle
        {
            Title = artist.Name,
            Subtitle = "Artist Overview",
            FilterUsed = filter,
            Summary = ArtistStats.GetSingleArtistStats(artist, allSongs, filteredEvents),
            PlottableData = ArtistStats.GetSingleArtistPlottableData(artist, allSongs, filteredEvents)
        };
        return bundle;
    }

    public async Task<AlbumStatsBundle?> GetAlbumStatisticsAsync(ObjectId albumId, DateRangeFilter filter)
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

        var allSongs = await _songRepo.GetAllAsync();
        var filteredEvents = await _eventRepo.GetEventsInDateRangeAsync(startDate, endDate);

        var bundle = new AlbumStatsBundle
        {
            Title = album.Name,
            Subtitle = $"Album by {album.Artist?.Name}",
            FilterUsed = filter,
            Summary = AlbumStats.GetSingleAlbumStats(album, allSongs, filteredEvents),
            PlottableData = AlbumStats.GetSingleAlbumPlottableData(album, allSongs, filteredEvents)
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
