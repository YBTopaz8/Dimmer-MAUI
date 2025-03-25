using System.Collections.Concurrent;
using DevExpress.Maui.CollectionView;
using DevExpress.Maui.Core;
using DevExpress.Maui.Core.Internal;

namespace Dimmer_MAUI.ViewModels;

public partial class HomePageVM 
{
    public class FilterWithId
    {
        public Guid Id { get; set; }
        public Func<DimmData, bool> Filter { get; set; }
    }

    #region Update Methods for AllLinks and AllPDaCStateLink


    #region Dynamic Filtering and Querying

    public readonly Subject<(object ActionData, FilterAction Action)> _filterActionSubject = new();

    private IObservable<List<PlayDataLink>> _filteredPlaysObservable;
    private IDisposable _filterSubscription;
    private List<PlayDataLink> _allDimmData = [];
    private List<FilterWithId> _currentFilters = []; // Keep track of filters with IDs

    public enum FilterAction
    {
        Add,
        RemoveById // New action to remove by ID
    }
    private Dictionary<Guid, Func<PlayDataLink, bool>> _filterRegistry = []; // Store filters with their IDs

    public Guid AddFilter(Func<PlayDataLink, bool> filter)
    {
        Guid filterId = Guid.NewGuid();
        _filterRegistry[filterId] = filter;
        _filterActionSubject.OnNext((filterId, FilterAction.Add)); // Send the ID to the stream
        return filterId;
    }

    public void RemoveFilter(Guid filterId)
    {
        _filterActionSubject.OnNext((filterId, FilterAction.RemoveById));
    }
    
    #endregion

    #region Data Collections and Indexes
    private Dictionary<string, string>? _songIdToTitleMap = [];
    private Dictionary<string, string>? _songIdToArtistMap = [];
    private Dictionary<string, string>? _songIdToAlbumMap = [];
    private Dictionary<string, string>? _songIdToGenreMap = [];
    private Dictionary<string, int?>? _songIdToReleaseYearMap = [];
    private Dictionary<string, double> _songIdToDurationMap = [];
    private Dictionary<string, List<PlayDataLink>> _playsBySongId
        = new Dictionary<string, List<PlayDataLink>>(StringComparer.OrdinalIgnoreCase);
    private Dictionary<DateTime, List<PlayDataLink>> _playsByDate 
    = [];
    private Dictionary<int, List<PlayDataLink>> _playsByPlayType
    = [];

    #endregion

    #region Mapping Initialization
    private void InitializeArtistMapping()
    {
        if (AllArtists == null)
        {
            _songIdToArtistMap = [];
            return;
        }

        Dictionary<string, string> tempDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (AlbumArtistGenreSongLinkView? link in AllLinks.Where(link => !string.IsNullOrEmpty(link.SongId) && !string.IsNullOrEmpty(link.ArtistId)))
        {
            string itemName = "Unknown";
            foreach (ArtistModelView item in AllArtists)
            {
                if (item.LocalDeviceId is not null)
                {
                    if (item.LocalDeviceId.Equals(link.AlbumId, StringComparison.OrdinalIgnoreCase))
                    {
                        itemName = item.Name;
                        break;
                    }
                }
            }
            if (!tempDictionary.ContainsKey(link.SongId.ToLower()))
            {
                tempDictionary[link.SongId] = itemName.ToLower();
            }
        }

        _songIdToArtistMap = tempDictionary;
    }

    private void InitializeAlbumMapping()
    {
        
        if (AllLinks == null || AllAlbums == null)
        {
            _songIdToAlbumMap = [];
            return;
        }

        Dictionary<string, string> tempDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (AlbumArtistGenreSongLinkView? link in AllLinks.Where(link => !string.IsNullOrEmpty(link.SongId) && !string.IsNullOrEmpty(link.AlbumId)))
        {
            string albumName = "Unknown";
            foreach (AlbumModelView album in AllAlbums)
            {
                if (album.LocalDeviceId is not null)
                {
                    if (album.LocalDeviceId.Equals(link.AlbumId, StringComparison.OrdinalIgnoreCase))
                    {
                        albumName = album.Name;
                        break;
                    }
                }
            }

            if (!tempDictionary.TryGetValue(link.SongId.ToLower(), out string? value))
            {
                tempDictionary[link.SongId] = albumName.ToLower();
            }
            else
            {
                
            }
        }
        _songIdToAlbumMap = tempDictionary;
    }
    #endregion
    #region Data Loading
    public void LoadData()
    {
        if (SongsMgtService.AllSongs == null || SongsMgtService.AllSongs.Count < 1)
            return;

        AllLinks = SongsMgtService.AllLinks; // Assuming AllLinks is already a List or similar

        // Use LINQ's Where and ToDictionary to filter out null LocalDeviceIds.
        // This prevents NullReferenceExceptions.
        _songIdToTitleMap = SongsMgtService.AllSongs
            .Where(s => s.LocalDeviceId != null)  // Filter out songs with null IDs
            .ToDictionary(
                s => s.LocalDeviceId!.ToLower(),  // Use the null-forgiving operator (!) after filtering
                s => s.Title,
                StringComparer.OrdinalIgnoreCase
            );

        _songIdToReleaseYearMap = SongsMgtService.AllSongs
            .Where(s => s.LocalDeviceId != null && s.ReleaseYear > 0) // Filter null IDs and invalid years
            .ToDictionary(
                s => s.LocalDeviceId!.ToLower(),
                s => s.ReleaseYear,
                StringComparer.OrdinalIgnoreCase
            );

        _songIdToDurationMap = SongsMgtService.AllSongs
            .Where(s => s.LocalDeviceId != null && s.DurationInSeconds > 0) // Filter null IDs and invalid durations
            .ToDictionary(
                s => s.LocalDeviceId!.ToLower(),
                s => s.DurationInSeconds,
                StringComparer.OrdinalIgnoreCase
            );

        _songIdToGenreMap = SongsMgtService.AllSongs
            .Where(s => s.LocalDeviceId != null && s.GenreName != null)  // Filter null IDs and null genres
            .ToDictionary(
                s => s.LocalDeviceId!.ToLower(),
                s => s.GenreName!, // Null-forgiving operator here, as we've filtered out nulls
                StringComparer.OrdinalIgnoreCase
            ); // No need for the ! at the end; ToDictionary returns a non-nullable Dictionary

        InitializeAlbumMapping();
        InitializeArtistMapping();
    }
    #endregion


    [ObservableProperty]
    public partial ObservableCollection<DimmData> PlaysSeeked { get; set; } = new();
    [ObservableProperty]
    public partial ObservableCollection<DimmData> PlaysStarted { get; set; } = new();
    [ObservableProperty]
    public partial ObservableCollection<DimmData> PlaysCompleted { get; set; } = new();


    #region Statistics Calculation Methods
    private double GetSeekToEndRatioForSong(string songId)
    {
        List<PlayDataLink> playsCompleted = _allDimmData.Where(p => p.SongId.Equals(songId, StringComparison.OrdinalIgnoreCase) && p.PlayType == 3).ToList();
        List<PlayDataLink> playsSeeked = _allDimmData.Where(p => p.SongId.Equals(songId, StringComparison.OrdinalIgnoreCase) && p.PlayType == 4).ToList();

        int totalPlays = playsCompleted.Count;
        if (totalPlays == 0)
            return 0.0;

        int seekPlays = playsSeeked.Count;
        return (double)seekPlays / totalPlays * 100;
    }

    public void CallStats()
    {
        if (AllPlayDataLinks is null || AllPlayDataLinks.Count<1)
        {
            return;
        }
        PlayDataLink? fDimm = AllPlayDataLinks.Where(p=>p.PlayType==3).OrderBy(p=>p.EventDate).FirstOrDefault();
        if (fDimm != null)
        {
            SongModelView? fSong = DisplayedSongs.FirstOrDefault(x => x.LocalDeviceId == fDimm.SongId);
            FirstDimmSong= fSong;
            PlayDataLink? e = SongsMgtService.AllPlayDataLinks.Where(x=>x.SongId == MySelectedSong.LocalDeviceId).OrderBy(x=>x.EventDate).FirstOrDefault(x=>x.PlayType == 3);
            DateOfFirstDimm = e.EventDate.ToLongDateString();
            DaysSinceFirstDimm = (DateTime.Now.Date - e.EventDate).Days;
        }
        TotalNumberStartedDimms = AllPlayDataLinks.Where(p => p.PlayType == 0).Count();
        TotalNumberCompletedDimms = AllPlayDataLinks.Where(p => p.PlayType == 3).Count();
        TotalNumberPausedDimms = AllPlayDataLinks.Where(p => p.PlayType == 1).Count();
        TotalNumberResumedDimms = AllPlayDataLinks.Where(p => p.PlayType == 2).Count();
        decimal s = decimal.Divide(TotalNumberCompletedDimms , AllPlayDataLinks.Count);
        PercentageCompletion = s * 100;
        //GetBiggestClimbers(AllPlayDataLinks, month: DateTime.Now.Month, year: DateTime.Now.Year);
        GetTopPlayedSongs(AllPlayDataLinks);
        GetTopPlayedAlbums(AllPlayDataLinks);
        GetTopPlayedGenres(AllPlayDataLinks);
        GetTopPlayedArtists(AllPlayDataLinks);
        GetStreaks(AllPlayDataLinks);
        CalculateGiniIndex(AllPlayDataLinks);
        CalculateParetoRatio(AllPlayDataLinks);
        CalculateEddingtonNumber(AllPlayDataLinks);

    }

    public class SongStatistics
    {
        public string DateOfFirstDimm { get; set; } = "None Yet";
        public string DateOfLastDimm { get; set; } = "None Yet";
        public int DaysSinceFirstDimm { get; set; } = 0;
        public int TotalNumberOfDimms { get; set; } = 0;
        public double AverageDimmsPerDay { get; set; } = 0.0;
        public int SpecificSongEddingtonNumber { get; set; }
        public int SpecificPlayEvents { get; set; }
        public int SpecificTotalPlayCount { get; set; }
        public int SpecificTotalPauseCount { get; set; }
        public int SpecificTotalPreviousCount { get; set; }
        public double SpecificSongCompletionRate { get; set; } = 0.0;
        public double SpecificSongSkipRate { get; set; } = 0.0;
        public int SpecificSongLongestListeningStreak { get; set; }
        public List<PlayDataLink> SpecificSongPlaysStarted { get; set; } = [];
        public List<PlayDataLink> SpecificSongPlaysPaused { get; set; } = [];
        public List<PlayDataLink> SpecificSongPlaysResumed { get; set; } = [];
        public List<PlayDataLink> SpecificSongPlaysCompleted { get; set; } = [];
        public List<PlayDataLink> SpecificSongPlaysSeeked { get; set; } = [];
        public List<PlayDataLink> SpecificSongPlaysSkipped { get; set; } = [];
        public ObservableCollection<DimmData> SpecificSongPlaysDailyCompletedCount { get; set; } = [];
        public double GiniIndex { get; set; }
        public double ParetoRatio { get; set; }

    }




    [ObservableProperty]
    public partial int TotalNumberStartedDimms { get; set; }
    [ObservableProperty]
    public partial decimal PercentageCompletion { get; set; }
    [ObservableProperty]
    public partial int TotalNumberPausedDimms { get; set; }
    [ObservableProperty]
    public partial int TotalNumberResumedDimms { get; set; }
    [ObservableProperty]
    public partial int TotalNumberCompletedDimms { get; set; }

    public SongStatistics CalculateGeneralSongStatistics(string songId)
    {
        if (string.IsNullOrEmpty(songId))
        {
            return new SongStatistics(); // Or throw an exception, depending on your needs
        }

        SongModelView? singleSong = SongsMgtService.AllSongs.SingleOrDefault(x => x.LocalDeviceId == songId); // changed this line to singleordefault.
        if (singleSong is null)
        {
            return new SongStatistics();
        }

        List<PlayDataLink> playData = [.. SongsMgtService.AllPlayDataLinks.Where(x => x.SongId == songId)];
        List<PlayDataLink> completedPlays = [.. playData.Where(x => x.PlayType == 3 && x.EventDate != DateTime.MinValue)];

        SongStatistics stats = new SongStatistics();

        // Calculate First/Last Dimm Dates and Days Since
        CalculateDimmDates(completedPlays, stats);

        // Calculate Counts
        int startedCount = playData.Count(x => x.PlayType == 0);
        int completedCount = completedPlays.Count;
        int skippedCount = playData.Count(x => x.PlayType == 5);

        stats.SpecificSongPlaysStarted = [.. playData.Where(x => x.PlayType == 0)];
        stats.SpecificSongPlaysPaused = [.. playData.Where(x => x.PlayType == 1)];
        stats.SpecificSongPlaysResumed= [.. playData.Where(x => x.PlayType == 2)];
        stats.SpecificSongPlaysCompleted= [.. playData.Where(x => x.PlayType == 3)];
        stats.SpecificSongPlaysSeeked= [.. playData.Where(x => x.PlayType == 4)];
        stats.SpecificSongPlaysSkipped= [.. playData.Where(x => x.PlayType == 5)];

        // Calculate Daily Completion Counts
        stats.SpecificSongPlaysDailyCompletedCount = playData
           .Where(x => x.PlayType == 3)
           .GroupBy(p => p.EventDate.Date) // Group by full date, not day of year.
           .Select(group => new DimmData()
           {
               Date = group.Key,
               DimmCount = group.Count(),
           }).ToObservableCollection();



        // Calculate Other Stats
        stats.TotalNumberOfDimms = completedCount;
        int uniqueDays = playData.Select(p => p.EventDate.Date).Distinct().Count();
        stats.AverageDimmsPerDay = uniqueDays > 0 ? (double)stats.TotalNumberOfDimms / uniqueDays : 0.0;
        stats.SpecificSongEddingtonNumber = CalculateEddingtonNumberInner(playData); // Pass playData, not re-querying
        stats.SpecificSongCompletionRate = (startedCount > 0) ? (double)completedCount / startedCount * 100 : 0;
        stats.SpecificSongSkipRate = (startedCount > 0) ? (double)skippedCount / startedCount * 100 : 0;
        stats.SpecificSongLongestListeningStreak = CalculateLongestListeningStreak(playData); // Pass playData

        stats.GiniIndex = CalculateGiniIndex(playData);
        stats.ParetoRatio = CalculateParetoRatio(playData);

        return stats;
    }


    private void CalculateDimmDates(List<PlayDataLink> completedPlays, SongStatistics stats)
    {
        if (completedPlays.Count != 0)
        {
            PlayDataLink? firstCompleted = completedPlays.OrderBy(x => x.EventDate).FirstOrDefault(); // Use First, not FirstOrDefault, as the condition above guarantees one.
            PlayDataLink? lastCompleted = completedPlays.OrderBy(x => x.EventDate).LastOrDefault(); // needs to be LastOrDefault, and we need to check it for not being null.

            if (firstCompleted is not null)
            {
                stats.DateOfFirstDimm = firstCompleted.EventDate.ToLongDateString();
                stats.DaysSinceFirstDimm = (DateTime.Now.Date - firstCompleted.EventDate.Date).Days;
            }

            if (lastCompleted is not null)
                stats.DateOfLastDimm = lastCompleted.EventDate.ToLongDateString();
        }
    }

    public static int GetTotalPlayEvents(string songId, List<PlayDataLink> playDataLinks)
    {
        return playDataLinks.Count(p => p.SongId == songId);
    }

    public static int GetTotalPlayCount(string songId, List<PlayDataLink> playDataLinks)
    {
        return playDataLinks.Count(p => p.SongId == songId && p.PlayType == 0);
    }

    public static int GetTotalPauseCount(string songId, List<PlayDataLink> playDataLinks)
    {
        return playDataLinks.Count(p => p.SongId == songId && p.PlayType == 1);
    }
    public static int GetTotalPreviousCount(string songId, List<PlayDataLink> playDataLinks)
    {
        return playDataLinks.Count(p => p.SongId == songId && p.PlayType == 9);
    }

    public static int GetTotalResumeCount(string songId, List<PlayDataLink> playDataLinks)
    {
        return playDataLinks.Count(p => p.SongId == songId && p.PlayType == 2);
    }

    public static int GetTotalSeekCount(string songId, List<PlayDataLink> playDataLinks)
    {
        return playDataLinks.Count(p => p.SongId == songId && p.PlayType == 4);
    }

    public static int GetTotalRestartCount(string songId, List<PlayDataLink> playDataLinks)
    {
        return playDataLinks.Count(p => p.SongId == songId && (p.PlayType == 6 || p.PlayType == 7 || p.PlayType == 8));
    }

    public static List<PlayDataLink> GetPlayEventsByPlayType(string songId, int playType, List<PlayDataLink> playDataLinks)
    {
        return [.. playDataLinks.Where(p => p.SongId == songId && p.PlayType == playType)];
    }

    public static int GetPlayCountInLastNDays(string songId, int nDays, List<PlayDataLink> playDataLinks)
    {
        DateTime startDate = DateTime.Now.Date.AddDays(-nDays);
        return playDataLinks.Count(p => p.SongId == songId && p.PlayType == 0 && p.EventDate.Date >= startDate);
    }

    public static int GetPlayCountInLastNMonths(string songId, int nMonths, List<PlayDataLink> playDataLinks)
    {
        DateTime startDate = DateTime.Now.Date.AddMonths(-nMonths);
        return playDataLinks.Count(p => p.SongId == songId && p.PlayType == 0 && p.EventDate.Date >= startDate);
    }
    public static int GetPlayCountInLastNYears(string songId, int nYears, List<PlayDataLink> playDataLinks)
    {
        DateTime startDate = DateTime.Now.Date.AddYears(-nYears);
        return playDataLinks.Count(p => p.SongId == songId && p.PlayType == 0 && p.EventDate.Date >= startDate);
    }


    public static double GetAveragePlaysPerWeek(string songId, List<PlayDataLink> playDataLinks)
    {
        List<PlayDataLink> plays = playDataLinks.Where(p => p.SongId == songId && p.PlayType == 0).ToList();
        if (!plays.Any())
            return 0;

        DateTime firstPlayDate = plays.Min(p => p.EventDate).Date;
        double totalWeeks = (DateTime.Now.Date - firstPlayDate).TotalDays / 7.0;
        return totalWeeks > 0 ? plays.Count / totalWeeks : 0;
    }

    public static double GetAveragePlaysPerMonth(string songId, List<PlayDataLink> playDataLinks)
    {
        List<PlayDataLink> plays = playDataLinks.Where(p => p.SongId == songId && p.PlayType == 0).ToList();
        if (!plays.Any())
            return 0;

        DateTime firstPlayDate = plays.Min(p => p.EventDate).Date;
        // More accurate month calculation
        int totalMonths = ((DateTime.Now.Year - firstPlayDate.Year) * 12) + DateTime.Now.Month - firstPlayDate.Month;

        // Add partial month.
        totalMonths += (DateTime.Now.Day > firstPlayDate.Day ? 1 : 0);

        return totalMonths > 0 ? (double)plays.Count / totalMonths : 0;
    }


    // --- Time-Based Statistics ---
    public static double GetTotalListeningTime(string songId, List<PlayDataLink> playDataLinks)
    {
        List<PlayDataLink> songPlays = playDataLinks.Where(p => p.SongId == songId).OrderBy(p => p.EventDate).ToList();
        double totalSeconds = 0;

        for (int i = 0; i < songPlays.Count - 1; i++)
        {
            if (songPlays[i].PlayType == 0) // Started
            {
                // Find the next relevant event (pause, skip, complete, or another start)
                for (int j = i + 1; j < songPlays.Count; j++)
                {
                    if (songPlays[j].SongId == songId && (songPlays[j].PlayType == 1 || songPlays[j].PlayType == 3 || songPlays[j].PlayType == 5 || songPlays[j].PlayType == 0))
                    {
                        totalSeconds += (songPlays[j].EventDate - songPlays[i].EventDate).TotalSeconds;
                        i = j - 1; // Adjust i to avoid double-counting.
                        break;
                    }
                }
            }
        }

        return totalSeconds;
    }


    public static double GetAverageListeningTimePerPlay(string songId, List<PlayDataLink> playDataLinks)
    {
        double totalListeningTime = GetTotalListeningTime(songId, playDataLinks);
        int totalPlays = GetTotalPlayCount(songId, playDataLinks);
        return totalPlays > 0 ? totalListeningTime / totalPlays : 0;
    }

    public static double GetShortestListeningTime(string songId, List<PlayDataLink> playDataLinks)
    {
        List<PlayDataLink> songPlays = playDataLinks.Where(p => p.SongId == songId).OrderBy(p => p.EventDate).ToList();
        double shortest = double.MaxValue;


        for (int i = 0; i < songPlays.Count - 1; i++)
        {
            if (songPlays[i].PlayType == 0) // Started
            {

                for (int j = i + 1; j < songPlays.Count; j++)
                {
                    if (songPlays[j].SongId == songId && (songPlays[j].PlayType == 1 || songPlays[j].PlayType == 3 || songPlays[j].PlayType == 5 || songPlays[j].PlayType == 0))
                    {
                        double currentDuration = (songPlays[j].EventDate - songPlays[i].EventDate).TotalSeconds;
                        shortest = Math.Min(shortest, currentDuration);
                        i = j-1;
                        break;
                    }
                }
            }
        }

        if (shortest == double.MaxValue)
            return 0;
        return shortest;
    }
    public static double GetLongestListeningTime(string songId, List<PlayDataLink> playDataLinks)
    {
        List<PlayDataLink> songPlays = playDataLinks.Where(p => p.SongId == songId).OrderBy(p => p.EventDate).ToList();
        double longest = 0;


        for (int i = 0; i < songPlays.Count - 1; i++)
        {
            if (songPlays[i].PlayType == 0) // Started
            {

                for (int j = i + 1; j < songPlays.Count; j++)
                {
                    if (songPlays[j].SongId == songId && (songPlays[j].PlayType == 1 || songPlays[j].PlayType == 3 || songPlays[j].PlayType == 5 || songPlays[j].PlayType == 0))
                    {
                        double currentDuration = (songPlays[j].EventDate - songPlays[i].EventDate).TotalSeconds;
                        longest = Math.Max(longest, currentDuration);
                        i = j-1;
                        break;
                    }
                }
            }
        }

        return longest;
    }


    public static string GetMostFrequentPlayTimeOfDay(string songId, List<PlayDataLink> playDataLinks)
    {
        IEnumerable<PlayDataLink> plays = playDataLinks.Where(p => p.SongId == songId && p.PlayType == 0);
        if (!plays.Any())
            return "None";

        Dictionary<string, int> timeOfDayCounts = new Dictionary<string, int>
        {
            { "Morning", 0 },   // 6:00 - 12:00
            { "Afternoon", 0 }, // 12:00 - 18:00
            { "Evening", 0 },   // 18:00 - 0:00
            { "Night", 0 }      // 0:00 - 6:00
        };

        foreach (PlayDataLink? play in plays)
        {
            int hour = play.EventDate.Hour;
            if (hour >= 6 && hour < 12)
                timeOfDayCounts["Morning"]++;
            else if (hour >= 12 && hour < 18)
                timeOfDayCounts["Afternoon"]++;
            else if (hour >= 18 || hour < 0)
                timeOfDayCounts["Evening"]++;
            else
                timeOfDayCounts["Night"]++;
        }

        return timeOfDayCounts.OrderByDescending(kv => kv.Value).FirstOrDefault().Key;
    }

    public static Dictionary<DayOfWeek, int> GetPlayFrequencyDistribution(string songId, List<PlayDataLink> playDataLinks)
    {
        IEnumerable<PlayDataLink> plays = playDataLinks.Where(p => p.SongId == songId && p.PlayType == 0);

        Dictionary<DayOfWeek, int> distribution = Enumerable.Range(0, 7)
            .ToDictionary(i => (DayOfWeek)i, i => 0); // Initialize all days to 0

        foreach (PlayDataLink? play in plays)
        {
            distribution[play.EventDate.DayOfWeek]++;
        }

        return distribution;
    }
    public static Dictionary<string, int> GetTimeOfDayPlayDistribution(string songId, List<PlayDataLink> playDataLinks)
    {
        IEnumerable<PlayDataLink> plays = playDataLinks.Where(p => p.SongId == songId && p.PlayType == 0);

        Dictionary<string, int> distribution = new Dictionary<string, int>()
        {
             { "Morning", 0 },   // 6:00 - 12:00
            { "Afternoon", 0 }, // 12:00 - 18:00
            { "Evening", 0 },   // 18:00 - 0:00
            { "Night", 0 }      // 0:00 - 6:00
        };

        foreach (PlayDataLink? play in plays)
        {
            if (play.EventDate.Hour >= 6 && play.EventDate.Hour < 12)
            {
                distribution["Morning"]++;
            }
            else if (play.EventDate.Hour >= 12 && play.EventDate.Hour < 18)
            {
                distribution["Afternoon"]++;
            }
            else if (play.EventDate.Hour >= 18 || play.EventDate.Hour < 0)
            {
                distribution["Evening"]++;
            }
            else
            {
                distribution["Night"]++;
            }
        }

        return distribution;
    }

    // --- Completion & Skipping ---

    public static double GetAverageCompletionPercentage(string songId, List<PlayDataLink> playDataLinks, double songDurationSeconds)
    {
        if (songDurationSeconds <= 0)
            return 0; // Avoid division by zero

        List<PlayDataLink> relevantEvents = playDataLinks.Where(p => p.SongId == songId && (p.PlayType == 0 || p.PlayType == 3 || p.PlayType == 5)).ToList();
        double totalCompletionPercentage = 0;
        int count = 0;

        for (int i = 0; i < relevantEvents.Count; i++)
        {
            if (relevantEvents[i].PlayType == 0) //start
            {
                for (int j = i + 1; j < relevantEvents.Count; j++)
                {
                    if (relevantEvents[j].PlayType == 3 || relevantEvents[j].PlayType == 5)
                    {
                        totalCompletionPercentage += (relevantEvents[j].PositionInSeconds / songDurationSeconds) * 100;
                        count++;
                        i = j; // Move i to the end event.
                        break;
                    }
                    else if (relevantEvents[j].PlayType == 0) // another play starts before this ends.
                    {
                        i = j -1; // i stays as is.
                        break; // if we find another play, break the loop
                    }
                }
            }
        }

        return count > 0 ? totalCompletionPercentage / count : 0;
    }


    public static double GetMedianCompletionPercentage(string songId, List<PlayDataLink> playDataLinks, double songDurationSeconds)
    {
        if (songDurationSeconds <= 0)
            return 0; // Avoid division by zero

        List<double> completionPercentages = new List<double>();

        List<PlayDataLink> relevantEvents = playDataLinks.Where(p => p.SongId == songId && (p.PlayType == 0 || p.PlayType == 3 || p.PlayType == 5)).ToList();


        for (int i = 0; i < relevantEvents.Count; i++)
        {
            if (relevantEvents[i].PlayType == 0) //start
            {
                for (int j = i + 1; j < relevantEvents.Count; j++)
                {
                    if (relevantEvents[j].PlayType == 3 || relevantEvents[j].PlayType == 5)
                    {
                        completionPercentages.Add((relevantEvents[j].PositionInSeconds / songDurationSeconds) * 100);

                        i = j; // Move i to the end event.
                        break;
                    }
                    else if (relevantEvents[j].PlayType == 0) // another play starts before this ends.
                    {
                        i = j -1;
                        break; // if we find another play, break the loop
                    }
                }
            }
        }


        if (!completionPercentages.Any())
            return 0;

        completionPercentages.Sort();
        int mid = completionPercentages.Count / 2;
        return completionPercentages.Count % 2 != 0
            ? completionPercentages[mid]
            : (completionPercentages[mid - 1] + completionPercentages[mid]) / 2.0;
    }



    public static double GetMostFrequentSkipPoint(string songId, List<PlayDataLink> playDataLinks)
    {
        IEnumerable<PlayDataLink> skipEvents = playDataLinks.Where(p => p.SongId == songId && p.PlayType == 5);
        if (!skipEvents.Any())
            return 0;

        // Group skip events by their position (in seconds, rounded to nearest second for grouping)
        var skipPointCounts = skipEvents
            .GroupBy(p => (int)Math.Round(p.PositionInSeconds))
            .Select(group => new { Position = group.Key, Count = group.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        // Return the most frequent skip point, or 0 if there are no skips.
        return skipPointCounts.FirstOrDefault()?.Position ?? 0;

    }

    public static Dictionary<string, double> GetCompletionRateByTimeOfDay(string songId, List<PlayDataLink> playDataLinks, double songDurationSeconds)
    {
        IEnumerable<PlayDataLink> plays = playDataLinks.Where(p => p.SongId == songId && (p.PlayType == 0 || p.PlayType==3 || p.PlayType == 5));

        Dictionary<string, double> distribution = new Dictionary<string, double>()
        {
             { "Morning", 0 },   // 6:00 - 12:00
            { "Afternoon", 0 }, // 12:00 - 18:00
            { "Evening", 0 },   // 18:00 - 0:00
            { "Night", 0 }      // 0:00 - 6:00
        };

        Dictionary<string, int> counts = new Dictionary<string, int>()
        {
             { "Morning", 0 },   // 6:00 - 12:00
            { "Afternoon", 0 }, // 12:00 - 18:00
            { "Evening", 0 },   // 18:00 - 0:00
            { "Night", 0 }      // 0:00 - 6:00
        };


        for (int i = 0; i < plays.Count(); i++)
        {
            if (plays.ElementAt(i).PlayType == 0) //start
            {
                string timeOfDay = "";
                if (plays.ElementAt(i).EventDate.Hour >= 6 && plays.ElementAt(i).EventDate.Hour < 12)
                {
                    timeOfDay = "Morning";
                }
                else if (plays.ElementAt(i).EventDate.Hour >= 12 && plays.ElementAt(i).EventDate.Hour < 18)
                {
                    timeOfDay = "Afternoon";
                }
                else if (plays.ElementAt(i).EventDate.Hour >= 18 || plays.ElementAt(i).EventDate.Hour < 0)
                {
                    timeOfDay = "Evening";
                }
                else
                {
                    timeOfDay = "Night";
                }

                for (int j = i + 1; j < plays.Count(); j++)
                {
                    if (plays.ElementAt(j).PlayType == 3 || plays.ElementAt(j).PlayType == 5)
                    {

                        distribution[timeOfDay] += (plays.ElementAt(j).PositionInSeconds / songDurationSeconds);
                        counts[timeOfDay] +=1;
                        i = j; // Move i to the end event.
                        break;
                    }
                    else if (plays.ElementAt(j).PlayType == 0) // another play starts before this ends.
                    {
                        i = j -1;
                        break; // if we find another play, break the loop
                    }
                }
            }
        }

        foreach (string? key in distribution.Keys.ToList())
        {
            if (counts[key] != 0)
            {
                distribution[key] = distribution[key] / counts[key] * 100;
            }
        }
        return distribution;
    }
    // --- Streaks & Patterns ---

    public static List<List<PlayDataLink>> GetListeningStreaks(string songId, List<PlayDataLink> playDataLinks)
    {
        List<PlayDataLink> plays = playDataLinks.Where(p => p.SongId == songId && p.PlayType == 0)
            .OrderBy(p => p.EventDate).ToList();

        List<List<PlayDataLink>> streaks = new List<List<PlayDataLink>>();
        if (!plays.Any())
            return streaks;

        List<PlayDataLink> currentStreak = new List<PlayDataLink> { plays[0] };

        for (int i = 1; i < plays.Count; i++)
        {
            // Check if the current play is within one day of the previous play
            if ((plays[i].EventDate.Date - plays[i - 1].EventDate.Date).Days <= 1)
            {
                currentStreak.Add(plays[i]);
            }
            else
            {
                // End of current streak, start a new one
                streaks.Add(currentStreak);
                currentStreak = [plays[i]];
            }
        }
        streaks.Add(currentStreak); // Add the last streak
        return streaks;
    }



    public static int GetAverageListeningStreakLength(string songId, List<PlayDataLink> playDataLinks)
    {
        List<List<PlayDataLink>> streaks = GetListeningStreaks(songId, playDataLinks);
        if (!streaks.Any())
            return 0;

        int totalStreakLength = streaks.Sum(streak => streak.Count);
        return totalStreakLength / streaks.Count;
    }

    public static int GetDaysSinceLastPlay(string songId, List<PlayDataLink> playDataLinks)
    {
        PlayDataLink? lastPlay = playDataLinks
            .Where(p => p.SongId == songId && p.PlayType == 0)
            .OrderByDescending(p => p.EventDate)
            .FirstOrDefault(); // Use FirstOrDefault to handle no plays

        return lastPlay != null ? (DateTime.Now.Date - lastPlay.EventDate.Date).Days : -1; //return -1 if it has never been played.
    }

    public static int GetNumberOfUniqueDaysPlayed(string songId, List<PlayDataLink> playDataLinks)
    {
        return playDataLinks
        .Where(x => x.SongId == songId)
        .Select(p => p.EventDate.Date).Distinct().Count();
    }


    

    public static Dictionary<string, double> GetCompletionRateByDevice(string songId, List<PlayDataLink> playDataLinks, double songDurationSeconds)
    {
        IEnumerable<PlayDataLink> plays = playDataLinks.Where(p => p.SongId == songId && (p.PlayType == 0 || p.PlayType == 3 || p.PlayType == 5));
        Dictionary<string, double> completionRates = new Dictionary<string, double>();
        Dictionary<string, int> deviceCounts = new Dictionary<string, int>();


        for (int i = 0; i < plays.Count(); i++)
        {

            if (plays.ElementAt(i).PlayType == 0)
            {
                if (!completionRates.ContainsKey(plays.ElementAt(i).LocalDeviceId))
                {
                    completionRates[plays.ElementAt(i).LocalDeviceId] = 0;
                    deviceCounts[plays.ElementAt(i).LocalDeviceId] = 0;
                }

                for (int j = i + 1; j < plays.Count(); j++)
                {
                    if (plays.ElementAt(j).PlayType == 3 || plays.ElementAt(j).PlayType == 5)
                    {
                        completionRates[plays.ElementAt(i).LocalDeviceId] += plays.ElementAt(j).PositionInSeconds / songDurationSeconds;
                        deviceCounts[plays.ElementAt(i).LocalDeviceId] +=1;
                        i = j; // Move i to the end event.
                        break;
                    }
                    else if (plays.ElementAt(j).PlayType == 0)
                    {
                        i=j-1;
                        break;
                    }

                }
            }
        }


        foreach (string? key in completionRates.Keys.ToList())
        {
            if (deviceCounts[key] != 0)
            {
                completionRates[key] = completionRates[key] / deviceCounts[key] * 100;
            }
        }
        return completionRates;
    }


    public static List<PlayDataLink> GetPlayEventsBeforeDate(string songId, DateTime date, List<PlayDataLink> playDataLinks)
    {
        return [.. playDataLinks.Where(p => p.SongId == songId && p.EventDate < date)];
    }

    public static List<PlayDataLink> GetPlayEventsAfterDate(string songId, DateTime date, List<PlayDataLink> playDataLinks)
    {
        return [.. playDataLinks.Where(p => p.SongId == songId && p.EventDate > date)];
    }

    public static List<PlayDataLink> GetPlayEventsBetweenDates(string songId, DateTime startDate, DateTime endDate, List<PlayDataLink> playDataLinks)
    {
        return [.. playDataLinks.Where(p => p.SongId == songId && p.EventDate >= startDate && p.EventDate <= endDate)];
    }

    public static bool GetIsMostPlayedSong(string songId, List<string> songIds, List<PlayDataLink> playDataLinks)
    {
        int targetSongPlays = playDataLinks.Count(x => x.SongId == songId && x.PlayType == 0);
        foreach (string otherSongId in songIds)
        {
            if (otherSongId == songId)
                continue; //dont check against itself.
            if (targetSongPlays < playDataLinks.Count(x => x.SongId == otherSongId && x.PlayType == 0))
                return false;
        }
        return true;

    }




    [ObservableProperty]
    public partial ObservableCollection<DimmData>? SpecificSongDistributionByDayOfWeek { get; set; } = new();
    
    [ObservableProperty]
    public partial double SpecificSongGiniIndex { get; set; } = new();
    private int CalculateEddingtonNumberInner(List<PlayDataLink> playData)
    {
        var dailyPlaysBySong = playData
            .GroupBy(p => new { p.EventDate.Date, p.SongId })
            .Select(group => new
            {
                group.Key.Date,
                group.Key.SongId,
                PlayCount = group.Count()
            })
            .ToList();

        // Now, we need to count how many song-day combinations have at least X plays
        int eddington = 0;
        for (int e = 1; ; e++) // Start with E = 1 and increment
        {
            int countOfSongDaysWithAtLeastEPlays = dailyPlaysBySong
                .Count(item => item.PlayCount >= e);

            if (countOfSongDaysWithAtLeastEPlays >= e)
            {
                eddington = e;
            }
            else
            {
                break; // If we don't have enough, the previous value of 'eddington' is the answer
            }
        }

        //CalculateEddingtonNumberTotal();
        
        return eddington;
    }

    [ObservableProperty]
    public partial ObservableCollection<DimmData> BiggestClimbers { get; set; }

private Dictionary<string, int> GetMonthData(List<PlayDataLink> playData, int month, int year)
{
    return playData
    .Where(p => p.EventDate.Year == year && p.EventDate.Month == month && p.SongId != null)
    .GroupBy(p => p.SongId, StringComparer.OrdinalIgnoreCase)
    .Where(g => g.Count() >= (month == DateTime.Now.Month ? 3 : 5)) //Simplified.
    .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
}

    private List<(string SongId, string SongTitle, string ArtistName, int PreviousMonthDimms, int CurrentMonthDimms, int RankChange)> CalculateClimbers(Dictionary<string, int> currentMonthData, Dictionary<string, int> previousMonthData)
    {
        List<(string, string, string, int, int, int)> climbers = new List<(string, string, string, int, int, int)>();
        IEnumerable<string> allSongIds = currentMonthData.Keys.Union(previousMonthData.Keys, StringComparer.OrdinalIgnoreCase).Distinct(StringComparer.OrdinalIgnoreCase);


        foreach (string? songId in allSongIds)
        {
            currentMonthData.TryGetValue(songId, out int currentDimms);
            previousMonthData.TryGetValue(songId, out int prevDimms);
            int rankChange = currentDimms - prevDimms;

            _songIdToTitleMap.TryGetValue(songId, out string title);
            _songIdToArtistMap.TryGetValue(songId, out string artist);
            climbers.Add((songId, title ?? "Unknown Title", artist ?? "Unknown Artist", prevDimms, currentDimms, rankChange));
        }
        return climbers;
    }


    public double CalculateShannonEntropy(List<PlayDataLink> playData)
    {
        if (playData == null || !playData.Any())
        {
            return 0;
        }
        List<double> songPlayCounts = playData
            .Where(p => p.PlayType == 3 && p.SongId != null)
             .GroupBy(p => p.SongId)
             .Select(g => (double)g.Count())
            .ToList();


        if (!songPlayCounts.Any())
        {
            return 0;
        }

        double totalPlays = songPlayCounts.Sum();
        double entropy = 0;

        foreach (double count in songPlayCounts)
        {
            double probability = count / totalPlays;
            entropy -= probability * Math.Log2(probability);
        }
        
        return entropy;

    }

    public double CalculateGiniIndex(List<PlayDataLink> playData)
    {
        if (playData == null || !playData.Any())
        {
            return 0; // Return 0 if there is no data
        }

        List<double> songPlayCounts = playData
            .Where(p => p.PlayType == 3 && p.SongId != null)
            .GroupBy(p => p.SongId)
            .Select(g => (double)g.Count())
            .OrderBy(count => count)
            .ToList();

        if (songPlayCounts.Count <= 1)
        {
            return 0; // Gini is undefined with 0 or 1 element
        }

        double n = songPlayCounts.Count;
        double sum = songPlayCounts.Sum();
        double[] cumSum = songPlayCounts.Zip(Enumerable.Range(1, (int)n), (x, y) => (x, y)).Select(val => val.x).ToArray();
        double cumSumValue = 0;
        for (int i = 0; i < n; i++)
        {
            cumSumValue += cumSum[i];
        }

        double giniSum = (cumSum.Select((val, idx) => ((idx + 1) * val)).Sum() * 2);
        GiniPlayIndex= (giniSum / (n * sum) - ((n + 1) / n));
        return GiniPlayIndex;
    }

    public double CalculateParetoRatio(List<PlayDataLink> playData)
    {
        if (playData == null || !playData.Any())
        {
            return 0; // Return 0 if there is no data
        }

        List<double> songPlayCounts = playData
            .Where(p => p.PlayType == 3 && p.SongId != null)
            .GroupBy(p => p.SongId)
            .Select(g => (double)g.Count())
            .OrderByDescending(count => count)
            .ToList();

        if (!songPlayCounts.Any())
        {
            return 0; // Return 0 if no plays
        }

        int top20PercentCount = (int)Math.Ceiling(songPlayCounts.Count * 0.2);
        double top20PercentTotalPlays = songPlayCounts.Take(top20PercentCount).Sum();
        double totalPlays = songPlayCounts.Sum();

        if (totalPlays == 0)
        {
            return 0;
        }
        ParetoPlayRatio= top20PercentTotalPlays / totalPlays;
        return ParetoPlayRatio;
    }

    public double CalculateEddingtonNumber(List<PlayDataLink> playData)
    {
        if (playData == null || !playData.Any())
        {
            return 0;
        }

        List<int> songPlayCounts = playData
            .Where(p => p.PlayType == 3 && p.SongId != null)
            .GroupBy(p => p.SongId)
            .Select(g => g.Count())
            .OrderByDescending(count => count)
            .ToList();

        if (!songPlayCounts.Any())
        {
            return 0;
        }
        int eddingtonNumber = 0;
        int i = 0;
        while (i < songPlayCounts.Count)
        {
            if (songPlayCounts[i] >= (i + 1))
            {
                eddingtonNumber = i + 1;
            }
            else
            {
                break;
            }
            i++;
        }

        EddingtonNumber= eddingtonNumber;
        return EddingtonNumber;
    } 
    [ObservableProperty]
    public partial double EddingtonNumber { get; set; } = new();  
    public ObservableCollection<DimmData> GetTopPlayedArtists(List<PlayDataLink>? PlayData,
           int top = 25,
           List<DateTime>? filterDates = null,
           bool isAscend = false)
    {
        
        ConcurrentDictionary<string, int> artistPlayCounts = new ConcurrentDictionary<string, int>();

        Parallel.ForEach(PlayData, play =>
        {

            if (play.SongId is not null)
            {
                if (_songIdToArtistMap.TryGetValue(play.SongId, out string artistName))
                {
                    artistPlayCounts.AddOrUpdate(artistName, 1, (key, count) => count + 1);
                }
            }
            
        });

        IOrderedEnumerable<KeyValuePair<string, int>> sortedArtists = isAscend
            ? artistPlayCounts.OrderBy(kvp => kvp.Value)
            : artistPlayCounts.OrderByDescending(kvp => kvp.Value);

        TopPlayedArtists = sortedArtists.Take(top)
            .Select(kvp => new DimmData() { ArtistName = kvp.Key, DimmCount = kvp.Value })
            .ToObservableCollection();
        return sortedArtists.Take(top).Select(kvp => new DimmData() { ArtistName = kvp.Key, DimmCount = kvp.Value }).ToObservableCollection();
    }

    public void GetTopPlayedAlbums(List<PlayDataLink>? PlayData,
           int top = 50,
           List<DateTime>? filterDates = null,
           bool isAscend = false)
    {
        List<PlayDataLink> filteredPlays = PlayData;
        ConcurrentDictionary<string, int> albumPlayCounts = new ConcurrentDictionary<string, int>();

        Parallel.ForEach(filteredPlays, play =>
        {
            if (play.SongId is not null)
            {
                if (_songIdToAlbumMap.TryGetValue(play.SongId, out string albumName))
                {
                    albumPlayCounts.AddOrUpdate(albumName, 1, (key, count) => count + 1);
                }

            }
        });

        IOrderedEnumerable<KeyValuePair<string, int>> sortedAlbums = isAscend
            ? albumPlayCounts.OrderBy(kvp => kvp.Value)
            : albumPlayCounts.OrderByDescending(kvp => kvp.Value);

        TopPlayedAlbums = sortedAlbums.Take(top)
            .Select(kvp => new DimmData() { AlbumName = kvp.Key, DimmCount = kvp.Value })
            .ToObservableCollection();

    }
    public void GetTopPlayedGenres(List<PlayDataLink> PlayData,
            int top = 25,
            List<DateTime>? filterDates = null,
            bool isAscend = false)
    {
        List<PlayDataLink> filteredPlays = PlayData;
        ConcurrentDictionary<string, int> genrePlayCounts = new ConcurrentDictionary<string, int>();

        Parallel.ForEach(filteredPlays, play =>
        {
            if (play.SongId is not null && _songIdToGenreMap.TryGetValue(play.SongId, out string genreName))
            {
                genrePlayCounts.AddOrUpdate(genreName, 1, (key, count) => count + 1);
            }
        });

        IOrderedEnumerable<KeyValuePair<string, int>> sortedGenres = isAscend
            ? genrePlayCounts.OrderBy(kvp => kvp.Value)
            : genrePlayCounts.OrderByDescending(kvp => kvp.Value);

        TopPlayedGenres = sortedGenres.
            Take(top).Select(kvp => new DimmData() { GenreName = kvp.Key, DimmCount = kvp.Value }).ToObservableCollection();
    }

    public IEnumerable<DimmData> GetTopPlayedSongs(List<PlayDataLink> PlayData,
            int top = 20,
            List<DateTime>? filterDates = null,
            bool isAscend = false)
    {
        
        ConcurrentDictionary<string, int> songPlayCounts = new ConcurrentDictionary<string, int>();

        Parallel.ForEach(AllPlayDataLinks, play =>
        {
            if (play.SongId is not null)
            {
                songPlayCounts.AddOrUpdate(play.SongId, 1, (songId, count) => count + 1);
            }
        });
        Double totalFromTop20 = 0;
        IOrderedEnumerable<KeyValuePair<string, int>> sortedSongs = isAscend
            ? songPlayCounts.OrderBy(kvp => kvp.Value)
            : songPlayCounts.OrderByDescending(kvp => kvp.Value);
        TopPlayedSongs = sortedSongs
            .Take(top)
            .Select(kvp => new DimmData()
            {
                SongId = kvp.Key,
                SongTitle = _songIdToTitleMap.TryGetValue(kvp.Key, out string songTitle) ? songTitle.ToUpper() : "Unknown Title",
                ArtistName = _songIdToArtistMap.TryGetValue(kvp.Key, out string artistName) ? artistName.ToUpper() : "Unknown Artist",
                AlbumName = _songIdToArtistMap.TryGetValue(kvp.Key, out string AlbumName) ? AlbumName.ToUpper() : "Unknown Album",
                DimmCount = kvp.Value
            })
            .ToObservableCollection();
        TopPlayedSong = TopPlayedSongs.FirstOrDefault();
        totalFromTop20 = TopPlayedSongs.Sum(x => x.DimmCount);
        return TopPlayedSongs;
    }
    [ObservableProperty]
    public partial DimmData? TopPlayedSong { get; set; } = new();
    public void GetStreaks(
        List<PlayDataLink> Playdata,
        int top = 20,
        List<DateTime>? filterDates = null,
        bool isAscend = false)
    {
        List<PlayDataLink> filteredPlays = Playdata
                            .OrderBy(p => p.EventDate)
                            .ToList();

        if (filteredPlays.Count == 0)
            return;

        ConcurrentDictionary<string, int> streaks = new ConcurrentDictionary<string, int>();
        string? currentSong = null;
        int currentStreak = 0;

        foreach (PlayDataLink? play in filteredPlays)
        {
            string songId = play.SongId;
            
            if (songId == currentSong)
            {
                currentStreak++;
            }
            else
            {
                if (currentSong != null)
                {
                    streaks.AddOrUpdate(currentSong, currentStreak, (key, existing) =>
                    {
                        return currentStreak > existing ? currentStreak : existing;
                    });
                }

                currentSong = songId;
                currentStreak = 1;
            }
        }

        if (currentSong != null)
        {
            streaks.AddOrUpdate(currentSong, currentStreak, (key, existing) => currentStreak > existing ? currentStreak : existing);
        }

        IOrderedEnumerable<KeyValuePair<string, int>> sortedStreaks = isAscend
            ? streaks.OrderBy(kvp => kvp.Value)
            : streaks.OrderByDescending(kvp => kvp.Value);

        TopStreaks = sortedStreaks.Take(top)
            .Select(kvp => (new DimmData()
            {
                SongId = kvp.Key,
                SongTitle = _songIdToTitleMap.TryGetValue(kvp.Key, out string title) ? title : kvp.Key,
                DurationInSecond = _songIdToDurationMap.TryGetValue(kvp.Key, out double duration) ? duration : 0,
                ArtistName = _songIdToArtistMap.TryGetValue(kvp.Key, out string artistName) ? artistName : "Unknown Artist",
                StreakLength = kvp.Value
            }))
            .ToObservableCollection();

    }
    
    [ObservableProperty]
    public partial ObservableCollection<DimmData>? TopStreaks { get; set; } = new();
    private int CalculateEddingtonNumberTotal()
    {
        var dailyPlaysBySong = AllPlayDataLinks
            .GroupBy(p => new { p.EventDate.Date, p.SongId })
            .Select(group => new
            {
                group.Key.Date,
                group.Key.SongId,
                PlayCount = group.Count()
            })
            .ToList();

        // Now, we need to count how many song-day combinations have at least X plays
        int eddington = 0;
        for (int e = 1; ; e++) // Start with E = 1 and increment
        {
            int countOfSongDaysWithAtLeastEPlays = dailyPlaysBySong
                .Count(item => item.PlayCount >= e);

            if (countOfSongDaysWithAtLeastEPlays >= e)
            {
                eddington = e;
            }
            else
            {
                break; // If we don't have enough, the previous value of 'eddington' is the answer
            }
        }

        TopEddingtonNum = eddington;
        return eddington;
    }

    public ObservableCollection<DimmData> GetDailyPlayCountByUser(List<PlayDataLink> playEvents)
    {
        return playEvents
            .GroupBy(p => new { p.EventDate.Date})
            .Select(g => new DimmData
            {
                Date = g.Key.Date,                
                DimmCount = g.Count()
            })
            .ToObservableCollection();
    }
    public ObservableCollection<DimmData> GetPlayCountDistributionByDayOfWeek(List<PlayDataLink> playEvents)
    {
        return playEvents
            .GroupBy(p => p.EventDate.DayOfWeek)
            .Select(g => new DimmData
            {
                DayOfWeekk = g.Key,
                DimmCount = g.Count()
            })
            .ToObservableCollection();
    }

    public ObservableCollection<DimmData> GetTotalPlayTimeByUser(List<PlayDataLink> playEvents)
    {
        return playEvents
            .GroupBy(p => p.EventDate.Year)
            .Select(g => new DimmData
            {
                Year = g.Key.ToString(),
                DimmCount = g.Sum(p => (p.EventDate - p.EventDate).TotalSeconds)
            })
            .ToObservableCollection();
    }

    public Dictionary<int, int> CalculatePlayFrequencyDistribution(List<PlayDataLink> playEvents)
    {
        return playEvents
            
            .GroupBy(p => p.SongId)
            .Select(g => g.Count())
            .GroupBy(count => count)
            .ToDictionary(g => g.Key, g => g.Count());
    }
    public double CalculateSharpeRatio(List<PlayDataLink> playEvents, double riskFreeRate = 0.01)
    {
        List<double> playDurations = playEvents
            .Where(p => p.EventDate != DateTime.MinValue && p.EventDate != DateTime.MinValue)
            .Select(p => (p.EventDate - p.EventDate).TotalSeconds)
            .ToList();

        if (playDurations.Count <= 1)
            return 0;

        double averageDuration = playDurations.Average();
        double variance = playDurations.Sum(duration => Math.Pow(duration - averageDuration, 2)) / (playDurations.Count - 1);
        double volatility = Math.Sqrt(variance);

        // Assuming average duration is the 'return'
        double excessReturn = averageDuration - riskFreeRate;
        return volatility == 0 ? 0 : excessReturn / volatility;
    }

    public List<string> DetectUnusualPlayPatterns(List<PlayDataLink> playEvents, double threshold = 2.0)
    {
        var songPlayCounts = playEvents
            
            .GroupBy(p => p.SongId)
            .Select(g => new { SongId = g.Key, Count = (double)g.Count()})
            .ToList();

        if (songPlayCounts.Count <= 1)
            return [];

        double averagePlayCount = songPlayCounts.Average(x => x.Count);
        double stdDev = Math.Sqrt(songPlayCounts.Sum(x => Math.Pow(x.Count - averagePlayCount, 2)) / (songPlayCounts.Count - 1));

        return [.. songPlayCounts
            .Where(x => Math.Abs(x.Count - averagePlayCount) > threshold * stdDev)
            .Select(x => x.SongId  )];
    }
    

        public double CalculateTotalCompletionRate(List<PlayDataLink> playEvents)
        {
        int totalStarts = SpecificSongPlaysStarted.Count;
        int totalCompletes = SpecificSongPlaysCompleted.Count;
            return totalStarts == 0 ? 0 : (double)totalCompletes / totalStarts * 100;
        }

        public double CalculateSkipRate(List<PlayDataLink> playEvents)
        {
        int totalStarts = SpecificSongPlaysStarted.Count;
        int totalSkips = SpecificSongPlaysSkipped.Count;
            return totalStarts == 0 ? 0 : (double)totalSkips / totalStarts * 100;
        }

        public int CalculateLongestListeningStreak(List<PlayDataLink> playEvents, string? songID = null)
        {
        List<IGrouping<DateTime, PlayDataLink>> dailyPlays = playEvents
                 .Where(p => songID == null || p.SongId == songID)
                 .GroupBy(p => p.EventDate)
                 .OrderBy(g => g.Key) // Ensure chronological ordering
                 .ToList();

            if (dailyPlays.Count() <= 1)
                return dailyPlays.Count();

            int maxStreak = 0;
            int currentStreak = 0;
            DateTime? previousDate = null;


            foreach (IGrouping<DateTime, PlayDataLink>? dailyPlay in dailyPlays)
            {
                if (previousDate == null || dailyPlay.Key == previousDate.Value.AddDays(1))
                {
                    currentStreak++;
                }
                else
                {
                    maxStreak = Math.Max(maxStreak, currentStreak);
                    currentStreak = 1;
                }
                previousDate = dailyPlay.Key;
            }
            maxStreak = Math.Max(maxStreak, currentStreak); // Check for last streak

            return maxStreak;
        }
    public ObservableCollection<DimmData> GetTopStreakTracks(List<PlayDataLink> playEvents)
    {
        Dictionary<string, TimeSpan> songStreaks = new Dictionary<string, TimeSpan>();
        List<IGrouping<string?, PlayDataLink>> groupedBySong = playEvents
             .GroupBy(p => p.SongId)
             .ToList();

        foreach (IGrouping<string?, PlayDataLink>? songGroup in groupedBySong)
        {
            List<PlayDataLink> orderedPlays = songGroup.OrderBy(p => p.EventDate).ToList();
            if (orderedPlays.Count <= 1)
            {
                songStreaks[songGroup.Key] = TimeSpan.Zero;
                continue;
            }
            TimeSpan maxStreak = TimeSpan.Zero;
            TimeSpan currentStreak = TimeSpan.Zero;
            for (int i = 1; i < orderedPlays.Count; i++)
            {
                TimeSpan timeDiff = orderedPlays[i].EventDate - orderedPlays[i - 1].EventDate;
                if (timeDiff.TotalHours <= 24)
                {
                    currentStreak += timeDiff;
                }
                else
                {
                    maxStreak = TimeSpan.FromTicks(Math.Max(maxStreak.Ticks, currentStreak.Ticks));
                    currentStreak = TimeSpan.Zero;
                }
            }
            maxStreak = TimeSpan.FromTicks(Math.Max(maxStreak.Ticks, currentStreak.Ticks));
            songStreaks[songGroup.Key] = maxStreak;
        }

        return songStreaks
            .OrderByDescending(s => s.Value)
            .Select(s => new DimmData { SongId = s.Key, SongTitle = _songIdToTitleMap[s.Key], TimeKey = s.Value.ToString() })
            .ToObservableCollection();
    }

    public ObservableCollection<DimmData> GetNewTracksInMonth(List<PlayDataLink> playEvents, int month, int year)
    {
        DateTime monthStart = new DateTime(year, month, 1);
        DateTime monthEnd = monthStart.AddMonths(1);

        HashSet<string?> allPlaysBeforeMonth = playEvents
            .Where(p => p.EventDate < monthStart)
            .GroupBy(p => p.SongId)
            .Select(g => g.Key)
            .ToHashSet();

        List<string?> newPlaysInMonth = playEvents
             .Where(p => p.EventDate >= monthStart && p.EventDate < monthEnd)
             .GroupBy(p => p.SongId)
             .Select(g => g.Key)
             .Where(songId => !allPlaysBeforeMonth.Contains(songId))
             .ToList();

        return newPlaysInMonth
            .Select(songId => new DimmData { SongId = songId, Date = monthStart })
            .ToObservableCollection();
    }
    public ObservableCollection<DimmData> GetDailyListeningVolume(List<PlayDataLink> playEvents)
    {
        return playEvents
            .Where(p => p.EventDate != DateTime.MinValue && p.EventDate != DateTime.MinValue)
            .GroupBy(p => p.EventDate.Date)
            .Select(g => new DimmData
            {
                Date = g.Key,
                DoubleKey = g.Sum(p => (p.EventDate - p.EventDate).TotalSeconds)
            })
            .OrderBy(d => d.Date)
            .ToObservableCollection();
    }
    public double GetOngoingGapBetweenTracks(List<PlayDataLink> playEvents)
    {
        List<PlayDataLink> orderedPlays = playEvents
              
              .OrderBy(p => p.EventDate)
              .ToList();

        if (orderedPlays.Count <= 1)
            return 0;

        List<double> timeGaps = new List<double>();
        for (int i = 1; i < orderedPlays.Count; i++)
        {
            timeGaps.Add((orderedPlays[i].EventDate - orderedPlays[i - 1].EventDate).TotalSeconds);
        }

        return timeGaps.Average();
    }

    [ObservableProperty]
    public partial int SpecificSongEddingtonNumber{ get; set; } 
    [ObservableProperty]
    public partial int SongStandardDeviation { get; set; } 
    [ObservableProperty]
    public partial int TopEddingtonNum { get; set; } 
    [ObservableProperty]
    public partial double SpecificSongSkipRate { get; set; } 
    [ObservableProperty]
    public partial int SpecificSongLongestListeningStreak { get; set; } 
    [ObservableProperty]
    public partial int SpecificSongRecencyScores { get; set; } 
    [ObservableProperty]
    public partial double SpecificSongCompletionRate { get; set; } 
        
    
    
    [ObservableProperty]
    public partial List<PlayDataLink> SpecificSongPlaysSeeked { get; set; } = new();
    [ObservableProperty]
    public partial List<PlayDataLink> SpecificSongPlaysStarted { get; set; } = new();
    [ObservableProperty]
    public partial List<PlayDataLink> SpecificSongPlaysResumed { get; set; } = new();
    [ObservableProperty]
    public partial List<PlayDataLink> SpecificSongPlaysPaused { get; set; } = new();
    [ObservableProperty]
    public partial List<PlayDataLink> SpecificSongPlaysSkipped { get; set; } = new();
    [ObservableProperty]
    public partial List<PlayDataLink> SpecificSongPlaysCompleted { get; set; } = new();
    [ObservableProperty]
    public partial ObservableCollection<DimmData> SpecificSongPlaysDailyCompletedCount { get; set; } = new();
    

    #endregion



    #region General Statistics Properties

    [ObservableProperty]
    public partial int DaysSinceFirstDimm { get; set; }

    [ObservableProperty]
    public partial string? DateOfFirstDimm { get; set; }

    [ObservableProperty]
    public partial SongModelView? FirstDimmSong { get; set; } = new();
    [ObservableProperty]
    public partial string? FirstDimmArtist { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string? FirstDimmAlbum { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? DateOfLastDimm { get; set; }
    

    [ObservableProperty]
    public partial string? LastDimmSong { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int TotalNumberOfArtists { get; set; }

    [ObservableProperty]
    public partial int TotalNumberOfAlbums { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DimmData> TopPlayedSongs { get; set; } = new();

    [ObservableProperty]
    public partial string? TopPlayedArtist { get; set; } = string.Empty;
    [ObservableProperty]
    public partial ObservableCollection<DimmData> TopPlayedArtists { get; set; } = new();

    [ObservableProperty]
    public partial string? TopPlayedAlbum { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? TopPlayedGenre { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<DimmData> TopPlayedAlbums { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<DimmData> TopPlayedGenres { get; set; } = new();

    [ObservableProperty]
    public partial int TotalNumberOfDimms { get; set; }

    [ObservableProperty]
    public partial double AverageDimmsPerDay { get; set; }

    [ObservableProperty]
    public partial double AverageDimmsPerWeek { get; set; }

    #endregion
    #region IDisposable Support
    private bool _disposedValue;
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _filterSubscription?.Dispose();
            }

            _disposedValue = true;
        }
    }
    #endregion
    #endregion

    CollectionView? SyncLyricsCV { get; set; }
    DXCollectionView? SyncLyricsCVV { get; set; }
    public async Task AssignSyncLyricsCV(CollectionView cv)
    {
        SyncLyricsCV = cv;
        SyncLyricsCV.SelectionChanged += SyncLyricsCV_SelectionChanged;
        if (MySelectedSong is null)
        {
            return;
        }
        if (MySelectedSong.SyncLyrics is null || MySelectedSong.SyncLyrics?.Count < 1)
        {
            await FetchLyrics(false);
        }
    }
    public async Task AssignSyncLyricsCV(DXCollectionView cv)
    {
        if (SyncLyricsCVV is not null)
        {
            return;
        }
        SyncLyricsCVV = cv;
        SyncLyricsCVV.SelectionChanged += SyncLyricsCVV_SelectionChanged;
        
        if (MySelectedSong.SyncLyrics is null || MySelectedSong.SyncLyrics?.Count < 1)
        {
            await FetchLyrics(false);
        }
    }
    [ObservableProperty]
    public partial int SelectedItemIndexMobile { get; set; } = 0;

    [ObservableProperty]
    public partial bool IsTopBarVisible { get; set; } = false;

    
    partial void OnSelectedItemIndexMobileChanging(int oldValue, int newValue)
    {

        switch (newValue)
        {
            case 0:
                IsTopBarVisible = true;
                break;
            case 1:
                CurrentPage = PageEnum.NowPlayingPage;
                ShowUtilsForNowPlayingUI();
                break;
            case 2:
                break;
            default:
                IsTopBarVisible = false;
                break;
        }
    }
    private void SyncLyricsCVV_SelectionChanged(object? sender, CollectionViewSelectionChangedEventArgs e)
    {
        if (SyncLyricsCVV is null || SyncLyricsCVV.ItemsSource is null || SyncLyricsCVV.SelectedItem is null
            )
        {
            return;
        }
        try
        {
            if (SyncLyricsCVV.SelectedItem is not null)
            {
                if (CurrentLyricPhrase is null)
                {
                    return;
                }

                int itemHandle = SyncLyricsCVV.FindItemHandle(SyncLyricsCVV.SelectedItem);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SyncLyricsCVV.ScrollTo(itemHandle, DXScrollToPosition.Start);
                });
                // Set SelectedItem FIRST to ensure UI updates
                SyncLyricsCVV.SelectedItem = CurrentLyricPhrase;

                CurrentLyricPhrase.Opacity = 1;
                CurrentLyricPhrase.LyricsFontAttributes = FontAttributes.Bold;

                // Scroll AFTER font size animation
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
        
    }

    public void UnAssignSyncLyricsCV()
    {
        if (SyncLyricsCV is not null)
        {
            SyncLyricsCV.SelectionChanged -= SyncLyricsCV_SelectionChanged;
        }
    }

    [ObservableProperty]
    public partial bool AreUtilsVisible { get; set; }
    DXBorder BtmBar { get; set; }
    public void ShowUtilsForNowPlayingUI()
    {
        //AreUtilsVisible 
    }
    [ObservableProperty]
    public partial int UnFocusedLyricSize { get; set; } = 29;
    [ObservableProperty]
    public partial int FocusedLyricSize { get; set; } = 60;
    private void SyncLyricsCV_SelectionChanged(object? sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    {
        if (SyncLyricsCV is null || SyncLyricsCV.ItemsSource is null || SyncLyricsCV.SelectedItem is null 
            || SyncLyricsCV.SelectedItems is null )            
        {
            return;
        }
        try
        {
            if (SyncLyricsCV.SelectedItem is not null)
            {
                // Set SelectedItem FIRST to ensure UI updates
                SyncLyricsCV.SelectedItem = CurrentLyricPhrase;

                // Animate Font Size First
                if (e.PreviousSelection?.Count > 0)
                {
                    if (e.CurrentSelection?.Count < 1)
                    {
                        foreach (LyricPhraseModel oldItem in e.PreviousSelection.Cast<LyricPhraseModel>())
                        {
                            oldItem.NowPlayingLyricsFontSize = FocusedLyricSize;
                            oldItem.LyricsFontAttributes = FontAttributes.Bold;
                        }

                    }
                    else
                    {
                        foreach (LyricPhraseModel oldItem in e.PreviousSelection.Cast<LyricPhraseModel>())
                        {
                            oldItem.NowPlayingLyricsFontSize = UnFocusedLyricSize;
                            oldItem.LyricsFontAttributes = FontAttributes.None;
                        }
                    }
                }
                if (e.CurrentSelection?.Count > 0)
                {
                    foreach (LyricPhraseModel newItem in e.CurrentSelection.Cast<LyricPhraseModel>())
                    {
                        newItem.NowPlayingLyricsFontSize = FocusedLyricSize;
                        newItem.LyricsFontAttributes = FontAttributes.Bold;
                    }
                }
                

                SyncLyricsCV.ScrollTo(SyncLyricsCV.SelectedItem, null, ScrollToPosition.Center, true);
                // Scroll AFTER font size animation
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    public void ScrollAfterAppearing()
    {
        if (CurrentLyricPhrase is not null)
        {
            CurrentLyricPhrase.NowPlayingLyricsFontSize = FocusedLyricSize;
            CurrentLyricPhrase.LyricsFontAttributes = FontAttributes.Bold;
            if (SyncLyricsCV is null || SyncLyricsCV.ItemsSource is null)
            {
                return;
            }
            SyncLyricsCV.SelectedItem = CurrentLyricPhrase;
            SyncLyricsCV.ScrollTo(SyncLyricsCV.SelectedItem, null, ScrollToPosition.Center, true);
        }
    }
}
