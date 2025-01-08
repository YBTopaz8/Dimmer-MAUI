using System.Collections.Concurrent;
using System.Diagnostics;
using DevExpress.Maui.Core.Internal;

namespace Dimmer_MAUI.ViewModels;

public partial class HomePageVM : ObservableObject
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
    private List<PlayDataLink> _allDimmData = new List<PlayDataLink>();
    private List<FilterWithId> _currentFilters = new List<FilterWithId>(); // Keep track of filters with IDs

    public enum FilterAction
    {
        Add,
        RemoveById // New action to remove by ID
    }
    private Dictionary<Guid, Func<PlayDataLink, bool>> _filterRegistry = new(); // Store filters with their IDs

    public Guid AddFilter(Func<PlayDataLink, bool> filter)
    {
        var filterId = Guid.NewGuid();
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
    private Dictionary<string, string>? _songIdToTitleMap = new();
    private Dictionary<string, string>? _songIdToArtistMap = new();
    private Dictionary<string, string>? _songIdToAlbumMap = new();
    private Dictionary<string, string>? _songIdToGenreMap = new();
    private Dictionary<string, int?>? _songIdToReleaseYearMap = new();
    private Dictionary<string, double> _songIdToDurationMap = new();
    private Dictionary<string, List<PlayDataLink>> _playsBySongId
        = new Dictionary<string, List<PlayDataLink>>(StringComparer.OrdinalIgnoreCase);
    private Dictionary<DateTime, List<PlayDataLink>> _playsByDate 
    = new Dictionary<DateTime, List<PlayDataLink>>();
    private Dictionary<int, List<PlayDataLink>> _playsByPlayType
    = new Dictionary<int, List<PlayDataLink>>();

    #endregion

    #region Mapping Initialization
    private void InitializeArtistMapping()
    {
        if (AllArtists == null)
        {
            _songIdToArtistMap = new Dictionary<string, string>();
            return;
        }

        Dictionary<string, string> tempDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (AlbumArtistGenreSongLinkView? link in AllLinks.Where(link => !string.IsNullOrEmpty(link.SongId) && !string.IsNullOrEmpty(link.ArtistId)))
        {
            string artistName = AllArtists
                .FirstOrDefault(artist => artist.LocalDeviceId.Equals(link.ArtistId, StringComparison.OrdinalIgnoreCase))?.Name ?? "Unknown";

            if (!tempDictionary.ContainsKey(link.SongId.ToLower()))
            {
                tempDictionary[link.SongId] = artistName.ToLower();
            }
        }

        _songIdToArtistMap = tempDictionary;
    }

    private void InitializeAlbumMapping()
    {
        
        if (AllLinks == null || AllAlbums == null)
        {
            _songIdToAlbumMap = new Dictionary<string, string>();
            return;
        }

        Dictionary<string, string> tempDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (AlbumArtistGenreSongLinkView? link in AllLinks.Where(link => !string.IsNullOrEmpty(link.SongId) && !string.IsNullOrEmpty(link.AlbumId)))
        {
            string albumName = AllAlbums
                .FirstOrDefault(album => album.LocalDeviceId.Equals(link.AlbumId, StringComparison.OrdinalIgnoreCase))?.Name ?? "Unknown";

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
        AllPlayDataLinks = SongsMgtService.AllPlayDataLinks.ToList();
        AllLinks = SongsMgtService.AllLinks;
        if (SongsMgtService.AllSongs == null || SongsMgtService.AllSongs.Count < 1)
            return;

        _songIdToTitleMap = SongsMgtService.AllSongs
            .ToDictionary(s => s.LocalDeviceId!.ToLower(), s => s.Title, StringComparer.OrdinalIgnoreCase)!;
        
        _songIdToReleaseYearMap = SongsMgtService.AllSongs
            .Where(s => s.ReleaseYear > 0)
            .ToDictionary(s => s.LocalDeviceId!.ToLower(), s => s.ReleaseYear, StringComparer.OrdinalIgnoreCase);

        _songIdToDurationMap = SongsMgtService.AllSongs
            .Where(s => s.DurationInSeconds > 0)
            .ToDictionary(s => s.LocalDeviceId!.ToLower(), s => s.DurationInSeconds, StringComparer.OrdinalIgnoreCase);

        _songIdToGenreMap = SongsMgtService.AllSongs
            .Where(s => s.GenreName != null)
            .ToDictionary(s => s.LocalDeviceId!.ToLower(), s => s.GenreName, StringComparer.OrdinalIgnoreCase)!;
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
        var playsCompleted = _allDimmData.Where(p => p.SongId.Equals(songId, StringComparison.OrdinalIgnoreCase) && p.PlayType == 3).ToList();
        var playsSeeked = _allDimmData.Where(p => p.SongId.Equals(songId, StringComparison.OrdinalIgnoreCase) && p.PlayType == 4).ToList();

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
        var fDimm = AllPlayDataLinks.Where(p=>p.PlayType==3).OrderBy(p=>p.DateFinished).FirstOrDefault();
        if (fDimm != null)
        {
            var fSong = DisplayedSongs.FirstOrDefault(x => x.LocalDeviceId == fDimm.SongId);
            FirstDimmSong= fSong;
            var e= fSong.PlayData.OrderBy(x=>x.DateFinished).FirstOrDefault(x=>x.PlayType == 3);
            DateOfFirstDimm = e.DateFinished.ToLongDateString();
            DaysSinceFirstDimm = (DateTime.Now.Date - e.DateFinished).Days;
        }
        TotalNumberStartedDimms = AllPlayDataLinks.Where(p => p.PlayType == 0).Count();
        TotalNumberCompletedDimms = AllPlayDataLinks.Where(p => p.PlayType == 3).Count();
        TotalNumberPausedDimms = AllPlayDataLinks.Where(p => p.PlayType == 1).Count();
        TotalNumberResumedDimms = AllPlayDataLinks.Where(p => p.PlayType == 2).Count();
        var  s= decimal.Divide(TotalNumberCompletedDimms , AllPlayDataLinks.Count);
        PercentageCompletion = s * 100;
        GetBiggestClimbers(AllPlayDataLinks, month: DateTime.Now.Month, year: DateTime.Now.Year);
        GetTopPlayedSongs(AllPlayDataLinks);
        GetTopPlayedAlbums(AllPlayDataLinks);
        GetTopPlayedGenres(AllPlayDataLinks);
        GetTopPlayedArtists(AllPlayDataLinks);
        GetStreaks(AllPlayDataLinks);
        CalculateGiniIndex(AllPlayDataLinks);
        CalculateParetoRatio(AllPlayDataLinks);
        CalculateEddingtonNumber(AllPlayDataLinks);

    }

    /// <summary>
    /// Indicates the type of play action performed.    
    /// Possible VALID values for <see cref="PlayType"/>:
    /// <list type="bullet">
    /// <item><term>0</term><description>Play</description></item>
    /// <item><term>1</term><description>Pause</description></item>
    /// <item><term>2</term><description>Resume</description></item>
    /// <item><term>3</term><description>Completed</description></item>
    /// <item><term>4</term><description>Seeked</description></item>
    /// <item><term>5</term><description>Skipped</description></item>
    /// <item><term>6</term><description>Restarted</description></item>
    /// <item><term>7</term><description>SeekRestarted</description></item>
    
    /// </list>
    /// </summary>
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
    public void CalculateGeneralSongStatistics(string songId)
    {
        if (DisplayedSongs is null)
        {
            return;
        }
        
        SongModelView singleSong= SongsMgtService.AllSongs.FirstOrDefault(x => x.LocalDeviceId == songId)!;
        if (singleSong is null)
        {
            return;
        }
        var lastSongLinkk = singleSong.PlayData.Where(x => x.DateFinished != DateTime.MinValue).ToList();
        
        if (lastSongLinkk.Count > 0)
        {
            PlayDataLink? lastSongLink = lastSongLinkk.LastOrDefault();
            DateOfLastDimm = lastSongLink.DateFinished.ToLongDateString();
        }
        DateOfFirstDimm = "None Yet";
        DaysSinceFirstDimm = 0;
        //DateOfFirstDimm = "None Yet";
        if (singleSong.PlayData.LastOrDefault() != null)
        {
            var w= singleSong.PlayData.LastOrDefault(x=>x.PlayType==3);
            if (w is not null)
            {
                DateOfFirstDimm = w.DateFinished.ToLongDateString();
                DaysSinceFirstDimm = (DateTime.Now.Date - w.DateFinished).Days;
            }
        }
        
        SpecificSongPlaysStarted = singleSong.PlayData.Where(x => x.PlayType == 0).ToList();
        SpecificSongPlaysPaused = singleSong.PlayData.Where(x => x.PlayType == 1).ToList();
        SpecificSongPlaysResumed= singleSong.PlayData.Where(x => x.PlayType == 2).ToList();
        SpecificSongPlaysCompleted= singleSong.PlayData.Where(x => x.PlayType == 3).ToList();
        SpecificSongPlaysSeeked= singleSong.PlayData.Where(x => x.PlayType == 4).ToList();
        SpecificSongPlaysSkipped= singleSong.PlayData.Where(x => x.PlayType == 5).ToList();

        SpecificSongPlaysDailyCompletedCount = singleSong.PlayData.Where(x => x.PlayType == 3)
        .GroupBy(p => p.DateFinished.DayOfYear)
        .Select(group => new DimmData()
        {
            Date = new DateTime(DateTime.Now.Year, 1, 1).AddDays(group.Key - 1),
            DimmCount = group.Count(),
            //DimmDatas = group.ToList()
        }).ToObservableCollection();

        TotalNumberOfDimms = SpecificSongPlaysCompleted.Count();
        int uniqueDays = singleSong.PlayData.Select(p => p.DateFinished.DayOfYear).Distinct().Count();
        AverageDimmsPerDay = uniqueDays > 0 ? (double)TotalNumberOfDimms / uniqueDays : 0.0;
        SpecificSongEddingtonNumber = CalculateEddingtonNumberInner(singleSong.PlayData);

        SpecificSongCompletionRate = CalculateTotalCompletionRate(singleSong.PlayData);
        SpecificSongSkipRate = CalculateSkipRate(singleSong.PlayData);
        SpecificSongLongestListeningStreak = CalculateLongestListeningStreak(singleSong.PlayData);

        CalculateGiniIndex(singleSong.PlayData);
        CalculateParetoRatio(singleSong.PlayData);


    }
    [ObservableProperty]
    public partial ObservableCollection<DimmData>? SpecificSongDistributionByDayOfWeek { get; set; } = new();
    
    [ObservableProperty]
    public partial double SpecificSongGiniIndex { get; set; } = new();
    private int CalculateEddingtonNumberInner(List<PlayDataLink> playData)
    {
        var dailyPlaysBySong = playData
            .GroupBy(p => new { p.DateFinished.Date, p.SongId })
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

        CalculateEddingtonNumberTotal();
        
        return eddington;
    }

    [ObservableProperty]
    public partial ObservableCollection<DimmData> BiggestClimbers { get; set; }
    public void GetBiggestClimbers(List<PlayDataLink> playData, int top = 20, bool isAscend = false, int month = 0, int year = 0)
    {
        try
        {

            if (playData == null || !playData.Any())
            {
                BiggestClimbers.Clear();
                return;
            }

            int currentMonth = month;
            int currentYear = year;
            int previousMonth = currentMonth == 1 ? 12 : currentMonth - 1;
            int previousYear = currentMonth == 1 ? currentYear - 1 : currentYear;

            var currentMonthPlays = playData
                .Where(p => p.DateFinished.Year == currentYear && p.DateFinished.Month == currentMonth && p.SongId != null)
                .GroupBy(p => p.SongId, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() >= 3)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var previousMonthPlays = playData
                .Where(p => p.DateFinished.Year == previousYear && p.DateFinished.Month == previousMonth && p.SongId != null)
                .GroupBy(p => p.SongId, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() >= 5)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var allSongIdsToConsider = currentMonthPlays.Keys.Union(previousMonthPlays.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            var climbers = new List<(string SongId, string SongTitle, string ArtistName, int PreviousMonthDimms, int CurrentMonthDimms, int RankChange)>();

            foreach (var songId in allSongIdsToConsider)
            {
                currentMonthPlays.TryGetValue(songId, out int currentDimms);
                previousMonthPlays.TryGetValue(songId, out int prevDimms);
                int rankChange = currentDimms - prevDimms;

                string songTitle = _songIdToTitleMap.TryGetValue(songId, out string title) ? title : "Unknown Title";
                string artistName = _songIdToArtistMap.TryGetValue(songId, out string artist) ? artist : "Unknown Artist";
                climbers.Add((SongId: songId, songTitle, artistName, prevDimms, currentDimms, rankChange));
            }

            var sortedClimbers = isAscend
                ? climbers.OrderBy(c => c.RankChange)
                : climbers.OrderByDescending(c => c.RankChange);

            IEnumerable<DimmData> dimmDataQuery = sortedClimbers.Select(kvp => new DimmData
            {
                SongId = kvp.SongId,
                SongTitle = kvp.SongTitle,
                PreviousMonthDimms = kvp.PreviousMonthDimms,
                CurrentMonthDimms = kvp.CurrentMonthDimms,
                RankChange = kvp.RankChange,
                ArtistName = kvp.ArtistName
            });

            if (top == 9999999)
            {
                BiggestClimbers = new ObservableCollection<DimmData>(dimmDataQuery.Take(20).ToObservableCollection());
            }
            else
            {
                BiggestClimbers = new ObservableCollection<DimmData>(dimmDataQuery.Take(top).ToObservableCollection());
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Biggest climbers Ex{ex.Message}");
            
        }
    }


    public double CalculateShannonEntropy(List<PlayDataLink> playData)
    {
        if (playData == null || !playData.Any())
        {
            return 0;
        }
        var songPlayCounts = playData
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

        foreach (var count in songPlayCounts)
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

        var songPlayCounts = playData
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
        var cumSum = songPlayCounts.Zip(Enumerable.Range(1, (int)n), (x, y) => (x, y)).Select(val => val.x).ToArray();
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

        var songPlayCounts = playData
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

        var songPlayCounts = playData
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
        var filteredPlays = Playdata
                            .OrderBy(p => p.DateFinished)
                            .ToList();

        if (filteredPlays.Count == 0)
            return;

        var streaks = new ConcurrentDictionary<string, int>();
        string? currentSong = null;
        int currentStreak = 0;

        foreach (var play in filteredPlays)
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

        var sortedStreaks = isAscend
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
            .GroupBy(p => new { p.DateFinished.Date, p.SongId })
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
            .GroupBy(p => new { p.DateFinished.Date})
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
            .GroupBy(p => p.DateFinished.DayOfWeek)
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
            .GroupBy(p => p.DateFinished.Year)
            .Select(g => new DimmData
            {
                Year = g.Key.ToString(),
                DimmCount = g.Sum(p => (p.DateFinished - p.DateStarted).TotalSeconds)
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
        var playDurations = playEvents
            .Where(p => p.DateStarted != DateTime.MinValue && p.DateFinished != DateTime.MinValue)
            .Select(p => (p.DateFinished - p.DateStarted).TotalSeconds)
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
            return new List<string>();

        double averagePlayCount = songPlayCounts.Average(x => x.Count);
        double stdDev = Math.Sqrt(songPlayCounts.Sum(x => Math.Pow(x.Count - averagePlayCount, 2)) / (songPlayCounts.Count - 1));

        return songPlayCounts
            .Where(x => Math.Abs(x.Count - averagePlayCount) > threshold * stdDev)
            .Select(x => x.SongId  )
            .ToList();
    }
    

        public double CalculateTotalCompletionRate(List<PlayDataLink> playEvents)
        {
            var totalStarts = SpecificSongPlaysStarted.Count;
        var totalCompletes = SpecificSongPlaysCompleted.Count;
            return totalStarts == 0 ? 0 : (double)totalCompletes / totalStarts * 100;
        }

        public double CalculateSkipRate(List<PlayDataLink> playEvents)
        {
            var totalStarts = SpecificSongPlaysStarted.Count;
            var totalSkips = SpecificSongPlaysSkipped.Count;
            return totalStarts == 0 ? 0 : (double)totalSkips / totalStarts * 100;
        }

        public int CalculateLongestListeningStreak(List<PlayDataLink> playEvents, string? songID = null)
        {
            var dailyPlays = playEvents
                 .Where(p => songID == null || p.SongId == songID)
                 .GroupBy(p => p.DateStarted)
                 .OrderBy(g => g.Key) // Ensure chronological ordering
                 .ToList();

            if (dailyPlays.Count() <= 1)
                return dailyPlays.Count();

            int maxStreak = 0;
            int currentStreak = 0;
            DateTime? previousDate = null;


            foreach (var dailyPlay in dailyPlays)
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
        var songStreaks = new Dictionary<string, TimeSpan>();
        var groupedBySong = playEvents
             .GroupBy(p => p.SongId)
             .ToList();

        foreach (var songGroup in groupedBySong)
        {
            var orderedPlays = songGroup.OrderBy(p => p.DateFinished).ToList();
            if (orderedPlays.Count <= 1)
            {
                songStreaks[songGroup.Key] = TimeSpan.Zero;
                continue;
            }
            TimeSpan maxStreak = TimeSpan.Zero;
            TimeSpan currentStreak = TimeSpan.Zero;
            for (int i = 1; i < orderedPlays.Count; i++)
            {
                var timeDiff = orderedPlays[i].DateFinished - orderedPlays[i - 1].DateFinished;
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
        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var allPlaysBeforeMonth = playEvents
            .Where(p => p.DateFinished < monthStart)
            .GroupBy(p => p.SongId)
            .Select(g => g.Key)
            .ToHashSet();

        var newPlaysInMonth = playEvents
             .Where(p => p.DateFinished >= monthStart && p.DateFinished < monthEnd)
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
            .Where(p => p.DateStarted != DateTime.MinValue && p.DateFinished != DateTime.MinValue)
            .GroupBy(p => p.DateFinished.Date)
            .Select(g => new DimmData
            {
                Date = g.Key,
                DoubleKey = g.Sum(p => (p.DateFinished - p.DateStarted).TotalSeconds)
            })
            .OrderBy(d => d.Date)
            .ToObservableCollection();
    }
    public double GetOngoingGapBetweenTracks(List<PlayDataLink> playEvents)
    {
        var orderedPlays = playEvents
              
              .OrderBy(p => p.DateFinished)
              .ToList();

        if (orderedPlays.Count <= 1)
            return 0;

        var timeGaps = new List<double>();
        for (int i = 1; i < orderedPlays.Count; i++)
        {
            timeGaps.Add((orderedPlays[i].DateFinished - orderedPlays[i - 1].DateFinished).TotalSeconds);
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
}
