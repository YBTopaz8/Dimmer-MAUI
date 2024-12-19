using System.Collections.Concurrent;

namespace Dimmer_MAUI.ViewModels;

public partial class HomePageVM
{
    #region Gap Statistics

    public List<DimmData> GetTopGapLargestTimeBetweenDimms(
        int top = 20,
        List<string>? filterSongIdList = null,
        List<DateTime>? filterDates = null,
        bool isAscend = false)
    {
        var filteredPlays = GetFilteredPlays(new List<int> { 0 }, filterSongIdList, filterDates)
                            .OrderBy(p => p.SongId)
                            .ThenBy(p => p.DateFinished)
                            .ToList();

        if (filteredPlays.Count == 0)
            return new List<DimmData>();

        var songToDates = new ConcurrentDictionary<string, List<DateTimeOffset>>();

        Parallel.ForEach(filteredPlays, play =>
        {
            if (string.IsNullOrEmpty(play.SongId))
                return;

            songToDates.AddOrUpdate(play.SongId,
                new List<DateTimeOffset> { play.DateFinished},
                (key, list) =>
                {
                    lock (list)
                    {
                        list.Add(play.DateFinished);
                        return list;
                    }
                });
        });

        var songGaps = new ConcurrentBag<(string SongTitle, TimeSpan Gap)>();

        Parallel.ForEach(songToDates, kvp =>
        {
            var dates = kvp.Value.OrderBy(d => d).ToList();
            if (dates.Count < 2)
                return;

            TimeSpan maxGap = TimeSpan.Zero;
            for (int i = 1; i < dates.Count; i++)
            {
                var gap = dates[i] - dates[i - 1];
                if (gap > maxGap)
                    maxGap = gap;
            }

            string songTitle = _songIdToTitleMap.TryGetValue(kvp.Key, out string title) ? title : "Unknown";
            songGaps.Add((songTitle, maxGap));
        });

        var sortedGaps = isAscend
            ? songGaps.OrderBy(s => s.Gap)
            : songGaps.OrderByDescending(s => s.Gap);

        return sortedGaps.Take(top).Select(p => new DimmData() { SongTitle= p.SongTitle, OngoingGap=p.Gap}).ToList();
    }

    #endregion

    #region Streaks

    public List<DimmData> GetTrackStreaks(
        int top = 20,
        List<string>? filterSongIdList = null,
        List<DateTime>? filterDates = null,
        bool isAscend = false)
    {
        var filteredPlays = GetFilteredPlays(new List<int> { 3 }, filterSongIdList, filterDates)
                            .OrderBy(p => p.DateFinished)
                            .ToList();

        if (filteredPlays.Count == 0)
            return new List<DimmData>();

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

        return sortedStreaks.Take(top)
            .Select(kvp => (new DimmData() { 
                SongTitle= _songIdToTitleMap.TryGetValue(kvp.Key, out string title) ? title : "Unknown",
                ArtistName = _songIdToArtistMap.TryGetValue(kvp.Key, out string artist) ? artist : "Unknown",
                AlbumName = _songIdToAlbumMap.TryGetValue(kvp.Key, out string album) ? album : "Unknown",
                DurationInSecond = _songIdToDurationMap.TryGetValue(kvp.Key, out double duration) ? duration : 0,
                StreakLength = kvp.Value}))
            .ToList();
    } 

    public ObservableCollection<DimmData> GetBiggestFallers(
        int month,
        int year,
        List<string>? filterSongIdList = null)
    {
        DateTime currentMonth = new DateTime(year, month, 1);
        DateTime previousMonth = currentMonth.AddMonths(-1);

        var currentMonthPlays = GetFilteredPlays(new List<int> { 3 }, filterSongIdList, null)
            .Where(p => p.PlayType == 3 && p.DateFinished.Year == year && p.DateFinished.Month == month)
            .GroupBy(p => p.SongId)
            .Select(g => new { SongId = g.Key, PlayCount = g.Count() })
            .ToList();

        var previousMonthPlays = GetFilteredPlays(new List<int> { 3 }, filterSongIdList, null)
            .Where(p => p.PlayType == 3 && p.DateFinished.Year == previousMonth.Year && p.DateFinished.Month == previousMonth.Month)
            .GroupBy(p => p.SongId)
            .Select(g => new { SongId = g.Key, PlayCount = g.Count() })
            .ToList();

        var currentDict = currentMonthPlays.ToDictionary(g => g.SongId, g => g.PlayCount);
        var previousDict = previousMonthPlays.ToDictionary(g => g.SongId, g => g.PlayCount);

        var rankLossList = previousDict
            .Where(kvp => kvp.Value >= 10)
            .Select(kvp =>
            {
                string songId = kvp.Key;
                int previousCount = kvp.Value;
                int currentCount = currentDict.TryGetValue(songId, out int value) ? value : 0;
                int loss = previousCount - currentCount;
                return new { SongId = songId, RankLoss = loss };
            })
            .Where(g => g.RankLoss > 0)
            .OrderByDescending(g => g.RankLoss)
            .Take(10)
            .ToList();

        BiggestFallers = rankLossList
            .Select(g => new DimmData()
            {
                SongTitle = _songIdToTitleMap.TryGetValue(g.SongId, out string title) ? title : "Unknown",
                RankLost = g.RankLoss
            }).ToObservableCollection();
        
        return BiggestFallers;
    } 

    [ObservableProperty]
    public partial ObservableCollection<DimmData> BiggestFallers { get; set; }

    public Dictionary<DayOfWeek, Dictionary<int, int>> GetPlayEventHeatmap(
        List<string>? filterSongIdList = null)
    {
        var filteredPlays = GetFilteredPlays(new List<int> { 3 }, filterSongIdList, null)
            .Where(p => p.PlayType == 3)
            .ToList();

        var heatmap = new Dictionary<DayOfWeek, Dictionary<int, int>>();

        foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
        {
            heatmap[day] = new Dictionary<int, int>();
            for (int hour = 0; hour < 24; hour++)
            {
                heatmap[day][hour] = 0;
            }
        }

        foreach (var play in filteredPlays)
        {            
            DayOfWeek day = play.DateFinished.DayOfWeek;
            int hour = play.DateFinished.Hour;
            heatmap[day][hour]++;
        }

        return heatmap;
    }
    #region Get Tops

    public List<DimmData> GetTopPlayedArtists(
            int top = 25,
            List<string>? filterSongIdList = null,
            List<DateTime>? filterDates = null,
            bool isAscend = false)
    {
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(new List<int> { 3 }, filterSongIdList, filterDates);
        ConcurrentDictionary<string, int> artistPlayCounts = new ConcurrentDictionary<string, int>();

        Parallel.ForEach(filteredPlays, play =>
        {
            if (_songIdToArtistMap.TryGetValue(play.SongId, out string artistName))
            {
                artistPlayCounts.AddOrUpdate(artistName, 1, (key, count) => count + 1);
            }
            else
            {
                artistPlayCounts.AddOrUpdate("Unknown Artist", 1, (key, count) => count + 1);
            }
        });

        IOrderedEnumerable<KeyValuePair<string, int>> sortedArtists = isAscend
            ? artistPlayCounts.OrderBy(kvp => kvp.Value)
            : artistPlayCounts.OrderByDescending(kvp => kvp.Value);

        TopPlayedArtists = sortedArtists.Take(top)
            .Select(kvp => new DimmData() { ArtistName = kvp.Key, DimmCount = kvp.Value })
            .ToObservableCollection();
        return sortedArtists.Take(top).Select(kvp => new DimmData() { ArtistName = kvp.Key, DimmCount = kvp.Value }).ToList();
    }

    public void GetTopPlayedAlbums(
           int top = 50,
           List<string>? filterSongIdList = null,
           List<DateTime>? filterDates = null,
           bool isAscend = false)
    {
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(new List<int> { 3 }, filterSongIdList, filterDates);
        ConcurrentDictionary<string, int> albumPlayCounts = new ConcurrentDictionary<string, int>();

        Parallel.ForEach(filteredPlays, play =>
        {
            if (_songIdToAlbumMap.TryGetValue(play.SongId, out string albumName))
            {
                albumPlayCounts.AddOrUpdate(albumName, 1, (key, count) => count + 1);
            }
            else
            {
                albumPlayCounts.AddOrUpdate("Unknown", 1, (key, count) => count + 1);
            }
        });

        IOrderedEnumerable<KeyValuePair<string, int>> sortedAlbums = isAscend
            ? albumPlayCounts.OrderBy(kvp => kvp.Value)
            : albumPlayCounts.OrderByDescending(kvp => kvp.Value);

        TopPlayedAlbums = sortedAlbums.Take(top)
            .Select(kvp => new DimmData() { AlbumName = kvp.Key, DimmCount = kvp.Value })
            .ToObservableCollection();
        
    }

    public void GetTopPlayedGenres(
            int top = 50,
            List<string>? filterSongIdList = null,
            List<DateTime>? filterDates = null,
            bool isAscend = false)
    {
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(new List<int> { 3 }, filterSongIdList, filterDates);
        ConcurrentDictionary<string, int> genrePlayCounts = new ConcurrentDictionary<string, int>();

        Parallel.ForEach(filteredPlays, play =>
        {
            if (_songIdToGenreMap.TryGetValue(play.SongId, out string genreName))
            {
                genrePlayCounts.AddOrUpdate(genreName, 1, (key, count) => count + 1);
            }
            else
            {
                genrePlayCounts.AddOrUpdate("Unknown", 1, (key, count) => count + 1);
            }
        });

        IOrderedEnumerable<KeyValuePair<string, int>> sortedGenres = isAscend
            ? genrePlayCounts.OrderBy(kvp => kvp.Value)
            : genrePlayCounts.OrderByDescending(kvp => kvp.Value);

        TopPlayedGenres= sortedGenres.Take(top).Select(kvp => new DimmData() { GenreName = kvp.Key, DimmCount = kvp.Value }).ToObservableCollection();
    }

    public IEnumerable<DimmData> GetTopPlayedSongs(
            int top = 50,
            List<string>? filterSongIdList = null,
            List<DateTime>? filterDates = null,
            bool isAscend = false)
    {
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(new List<int> { 3 }, filterSongIdList, filterDates);
        ConcurrentDictionary<string, int> songPlayCounts = new ConcurrentDictionary<string, int>();

        Parallel.ForEach(filteredPlays, play =>
        {
            if (_songIdToTitleMap.TryGetValue(play.SongId, out string songTitle))
            {
                songPlayCounts.AddOrUpdate(songTitle, 1, (key, count) => count + 1);
            }
            else
            {
                songPlayCounts.AddOrUpdate("Unknown Song", 1, (key, count) => count + 1);
            }
        });

        IOrderedEnumerable<KeyValuePair<string, int>> sortedSongs = isAscend
            ? songPlayCounts.OrderBy(kvp => kvp.Value)
            : songPlayCounts.OrderByDescending(kvp => kvp.Value);
        TopPlayedSongs = sortedSongs
            .Take(top)
            .Select(kvp => new DimmData() { SongTitle = kvp.Key, DimmCount = kvp.Value })
            .ToObservableCollection();
        return TopPlayedSongs;
    }

    public ObservableCollection<DimmData> GetTopStreakTracks(
        int top = 50,
        List<string>? filterSongIdList = null)
    {
        var streaks = GetTrackStreaks(10, filterSongIdList: filterSongIdList, null);

        var songStreakDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var streak in streaks)
        {
            if (songStreakDict.ContainsKey(streak.SongTitle))
            {
                if (streak.StreakLength > songStreakDict[streak.SongTitle])
                {
                    songStreakDict[streak.SongTitle] = streak.StreakLength;
                }
            }
            else
            {
                songStreakDict[streak.SongTitle] = streak.StreakLength;
            }
        }

        var topStreaks = songStreakDict
            .OrderByDescending(kvp => kvp.Value)
            .Take(top)
            .Select(kvp => (new DimmData() { SongTitle = kvp.Key, MaxStreak = kvp.Value }))
            .ToObservableCollection();

        return topStreaks;
    }
    #endregion


    [ObservableProperty]
    public partial ObservableCollection<DimmData> TopStreakTracks { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<DimmData> DimmsWalkThrough { get; set; }

    public ObservableCollection<DimmData> GetMonthlyTopArtists(
        int month,
        int year,
        int top = 20,
        List<string>? filterSongIdList = null)
    {
        var filteredPlays = GetFilteredPlays(new List<int> { 3 }, filterSongIdList, null)
            .Where(p => p.PlayType == 3 && p.DateFinished.Month == month && p.DateFinished.Year == year)
            .ToList();

        var artistPlayCounts = filteredPlays
            .GroupBy(p => _songIdToArtistMap.TryGetValue(p.SongId, out string aName) ? aName : "Unknown")
            .Select(g => new { ArtistName = g.Key, PlayCount = g.Count() })
            .OrderByDescending(g => g.PlayCount)
            .Take(top)
            .ToList();

        TopPlayedArtists = artistPlayCounts
            .Select(g => new DimmData
            {
                ArtistName = g.ArtistName,
                DimmCount = g.PlayCount
            })
            .ToObservableCollection();
        return TopPlayedArtists;
    } 

    [ObservableProperty]
    public partial ObservableCollection<DimmData> MonthlyTopArtistsAndPlayCount { get; set; }

    private void LoadMostEndedPlaysByArtist()
    {
        var result = GetMostEndedPlaysByArtist();

        DailyMostEndedPlaysByArtist = new ObservableCollection<DimmData>(
            result.Daily.Select(d => new DimmData
            {
                Date = d.Date,
                DimmCount = d.PlayCount,
                ArtistName = d.ArtistName
            }));

        WeeklyMostEndedPlaysByArtist = new ObservableCollection<DimmData>(
            result.Weekly.Select(w => new DimmData
            {
                Date = new DateTime(DateTime.Now.Year, 1, 1).AddDays((w.WeekNumber - 1) * 7),
                DimmCount = w.PlayCount,
                ArtistName = w.ArtistName
            }));

        MonthlyMostEndedPlaysByArtist = new ObservableCollection<DimmData>(
            result.Monthly.Select(m => new DimmData
            {
                Date = DateTime.ParseExact(m.Month, "yyyy-M", CultureInfo.InvariantCulture),
                DimmCount = m.PlayCount,
                ArtistName = m.ArtistName
            }));

        YearlyMostEndedPlaysByArtist = new ObservableCollection<DimmData>(
            result.Yearly.Select(y => new DimmData
            {
                Date = new DateTime(y.Year, 1, 1),
                DimmCount = y.PlayCount,
                ArtistName = y.ArtistName
            }));
    } 

    public (
        IEnumerable<(DateTime Date, string ArtistName, int PlayCount)> Daily,
        IEnumerable<(int WeekNumber, string ArtistName, int PlayCount)> Weekly,
        IEnumerable<(string Month, string ArtistName, int PlayCount)> Monthly,
        IEnumerable<(int Year, string ArtistName, int PlayCount)> Yearly
    ) GetMostEndedPlaysByArtist(List<string>? filterSongIdList = null)
    {
        var filteredPlays = GetFilteredPlays(new List<int> { 3 }, filterSongIdList, null)
            .Where(p => p.PlayType == 3)
            .ToList();

        var daily = filteredPlays
            .GroupBy(p => new { p.DateFinished.LocalDateTime.Date, Artist = GetArtistName(p.SongId) })
            .Select(g => (Date: g.Key.Date, ArtistName: g.Key.Artist, PlayCount: g.Count()))
            .OrderByDescending(g => g.PlayCount)
            .ToList();

        var weekly = filteredPlays
            .GroupBy(p => new
            {
                WeekNumber = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                    p.DateFinished.LocalDateTime,
                    CalendarWeekRule.FirstDay,
                    DayOfWeek.Monday),
                Artist = GetArtistName(p.SongId)
            })
            .Select(g => (WeekNumber: g.Key.WeekNumber, ArtistName: g.Key.Artist, PlayCount: g.Count()))
            .OrderByDescending(g => g.PlayCount)
            .ToList();

        var monthly = filteredPlays
            .GroupBy(p => new
            {
                Month = $"{p.DateFinished.Year}-{p.DateFinished.Month}",
                Artist = GetArtistName(p.SongId)
            })
            .Select(g => (Month: g.Key.Month, ArtistName: g.Key.Artist, PlayCount: g.Count()))
            .OrderByDescending(g => g.PlayCount)
            .ToList();

        var yearly = filteredPlays
            .GroupBy(p => new
            {
                Year = p.DateFinished.Year,
                Artist = GetArtistName(p.SongId)
            })
            .Select(g => (Year: g.Key.Year, ArtistName: g.Key.Artist, PlayCount: g.Count()))
            .OrderByDescending(g => g.PlayCount)
            .ToList();

        return (daily, weekly, monthly, yearly);
    } 

    private string GetArtistName(string songId) =>
        _songIdToArtistMap.TryGetValue(songId, out string artistName) ? artistName : "Unknown"; 

    [ObservableProperty]
    public partial ObservableCollection<DimmData> DailyMostEndedPlaysByArtist { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DimmData> WeeklyMostEndedPlaysByArtist { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DimmData> MonthlyMostEndedPlaysByArtist { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DimmData> YearlyMostEndedPlaysByArtist { get; set; }

    public List<(int StreakLength, DateTime StartDate, DateTime EndDate)> GetNotListenedStreaks(
        List<string>? filterSongIdList = null,
        List<DateTime>? filterDates = null)
    {
        DateTime startDate = filterDates?.Min().Date ?? (AllPDaCStateLink?.Min(p => p.DateFinished.LocalDateTime) ?? DateTime.MinValue);
        DateTime endDate = filterDates?.Max().Date ?? DateTime.Now.Date;

        var listenDates = GetFilteredPlays(new List<int> { 3 }, filterSongIdList, filterDates)
            .Select(p => p.DateFinished.LocalDateTime)
            .Distinct()
            .OrderBy(d => d)
            .ToHashSet();

        var streaks = new List<(int StreakLength, DateTime StartDate, DateTime EndDate)>();

        int currentStreak = 0;
        DateTime? streakStart = null;

        for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (!listenDates.Contains(date))
            {
                if (currentStreak == 0)
                {
                    streakStart = date;
                }
                currentStreak++;
            }
            else
            {
                if (currentStreak > 0 && streakStart.HasValue)
                {
                    streaks.Add((currentStreak, streakStart.Value, date.AddDays(-1)));
                    currentStreak = 0;
                    streakStart = null;
                }
            }
        }

        if (currentStreak > 0 && streakStart.HasValue)
        {
            streaks.Add((currentStreak, streakStart.Value, endDate));
        }

        return streaks;
    }

    #endregion

    #region Climbers
    [ObservableProperty]
    public partial int SelectedSongStatRank { get; set; }
    public void GetBiggestClimbers(
        int top = 9999999,
        List<string>? filterSongIdList = null,
        List<DateTime>? filterDates = null,
        bool isAscend = false)
    {
        var filteredPlays = GetFilteredPlays(new List<int> { 3 }, filterSongIdList, filterDates).ToList();

        if (filteredPlays.Count == 0)
            return;

        DateTime currentMonthDate = DateTime.Now;
        int currentMonth = currentMonthDate.Month;
        int currentYear = currentMonthDate.Year;
        int previousMonth = currentMonth == 1 ? 12 : currentMonth - 1;
        int previousYear = currentMonth == 1 ? currentYear - 1 : currentYear;

        var currentMonthPlays = filteredPlays
            .Where(p => p.DateFinished.Year == currentYear && p.DateFinished.Month == currentMonth)
            .GroupBy(p => p.SongId)
            .Where(g => g.Count() >= 3)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var previousMonthPlays = filteredPlays
            .Where(p => p.DateFinished.Year == previousYear && p.DateFinished.Month == previousMonth)
            .GroupBy(p => p.SongId)
            .Where(g => g.Count() >= 3)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var climbers = new ConcurrentBag<(string SongId, string SongTitle, string ArtistName, int PreviousMonthDimms, int CurrentMonthDimms, int RankChange)>();

        Parallel.ForEach(currentMonthPlays, kvp =>
        {
            string songId = kvp.Key;
            int currentDimms = kvp.Value;
            previousMonthPlays.TryGetValue(songId, out int prevDimms);
            int rankChange = currentDimms - prevDimms;
            string songTitle = _songIdToTitleMap.TryGetValue(songId, out string title) ? title : "Unknown Title";
            string artistName = _songIdToArtistMap.TryGetValue(songId, out string artist) ? artist : "Unknown Artist";
            climbers.Add((SongId:songId, songTitle, artistName, prevDimms, currentDimms, rankChange));
        });

        var sortedClimbers = isAscend
            ? climbers.OrderBy(c => c.RankChange)
            : climbers.OrderByDescending(c => c.RankChange);

        if (top == 9999999)
        {
            BiggestClimbers = sortedClimbers
                .Select(kvp =>
                new DimmData()
                {
                    SongId = kvp.SongId,
                    SongTitle = kvp.SongTitle,
                    PreviousMonthDimms = kvp.PreviousMonthDimms,
                    CurrentMonthDimms = kvp.CurrentMonthDimms,
                    RankChange = kvp.RankChange,
                    ArtistName = kvp.ArtistName
                })
                .ToObservableCollection();
        }
        else
        {
            BiggestClimbers = sortedClimbers
                .Select(kvp =>
                new DimmData()
                {
                    SongId = kvp.SongId,
                    SongTitle = kvp.SongTitle,
                    PreviousMonthDimms = kvp.PreviousMonthDimms,
                    CurrentMonthDimms = kvp.CurrentMonthDimms,
                    RankChange = kvp.RankChange, //can be negative
                    ArtistName = kvp.ArtistName
                })
                .ToObservableCollection();
        }
    } 

    [ObservableProperty]
    public partial ObservableCollection<DimmData> BiggestClimbers { get; set; }

    #endregion
    public int GetEddingtonNumber()
    {
        var plays = GetFilteredPlays(new List<int> { 3 }, null, null);

        // Group plays by their date (using only the date part)
        var playsPerDay = plays
            .GroupBy(p => p.DateFinished.LocalDateTime.Date)
            .Select(g =>   g.Count())
            .OrderByDescending(count => count)
            .ToList();

        int eddington = 0;
        // Check for the largest E where at least E days have E or more plays
        for (int i = 0; i < playsPerDay.Count; i++)
        {
            int dayCount = playsPerDay[i];
            int required = i + 1; // E is zero-based here, so E = i+1
            if (dayCount >= required)
                eddington = required;
            else
                break; // Once we fail, we stop since it's sorted descending
        }

        EddingtonNumber = eddington;
        NextEddingtonNumber = eddington+1;
        return EddingtonNumber;
    }

    public int GetDaysNeededForNextEddington()
    {
        int currentEddington = GetEddingtonNumber();
        int nextEddington = currentEddington + 1;

        var plays = GetFilteredPlays(new List<int> { 3 }, null, null);
        var playsPerDay = plays
            .GroupBy(p => p.DateFinished.LocalDateTime.Date)
            .Select(g => g.Count())
            .OrderByDescending(count => count)
            .ToList();

        // Count how many days currently have at least (nextEddington) plays
        int daysWithNextNeeded = playsPerDay.Count(count => count >= nextEddington);

        // If we already have enough days for the next Eddington number
        if (daysWithNextNeeded >= nextEddington)
            return 0;

        // Otherwise, we need more days meeting that criterion
        int daysShort = nextEddington - daysWithNextNeeded;
        NextDaysNeededForNextEddingtonNumber = daysShort;
        return daysShort;
    }


    [ObservableProperty]
    public partial int EddingtonNumber { get; set; }
    
    [ObservableProperty]
    public partial int NextDaysNeededForNextEddingtonNumber { get; set; }
    [ObservableProperty]
    public partial int NextEddingtonNumber { get; set; }

    public DimmData GetMostPopularYear(bool isAscend = false)
    {
        var plays = GetFilteredPlays(new List<int> { 3 }, null, null)
                    .GroupBy(p => p.DateFinished.Year)
                    .Select(g => new { Year = g.Key, Count = g.Count() });

        if (!plays.Any())
            return null;

        var mostPopular = isAscend
            ? plays.OrderBy(g => g.Count).First()
            : plays.OrderByDescending(g => g.Count).First();

        return new DimmData() { Year = mostPopular.Year.ToString(), DimmCount = mostPopular.Count };
    } 

    public DimmData GetMostPopularMonth(bool isAscend = false)
    {
        var plays = GetFilteredPlays(new List<int> { 3 }, null, null)
                    .GroupBy(p => new { p.DateFinished.Year, p.DateFinished.Month })
                    .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Count = g.Count() });

        if (!plays.Any())
            return null;

        var mostPopular = isAscend
            ? plays.OrderBy(g => g.Count).First()
            : plays.OrderByDescending(g => g.Count).First();

        string monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(mostPopular.Month);
        MostPopularMonth = new DimmData
        {
            Month = monthName,
            Year = mostPopular.Year.ToString(),
            DimmCount = mostPopular.Count
        };
        return MostPopularMonth;
    } 

    [ObservableProperty]
    public partial DimmData MostPopularMonth { get; set; }

    #region Daily Statistics
    public ObservableCollection<DimmData> GetMostDimmsPerDay(
       int top = 50,
       List<string>? filterSongIdList = null,
       List<DateTime>? filterDates = null,
       bool isAscend = false)
    {
        // Filter plays
        List<PlayDateAndCompletionStateSongLinkView>? filteredPlays = GetFilteredPlays(new List<int> { 3 }, filterSongIdList, filterDates);

        // Group by date and enrich DimmData directly
        var dailyCounts = filteredPlays
            .GroupBy(p => p.DateFinished.LocalDateTime.Date) // Group by date
            .Select(g => new DimmData
            {
                Date = g.Key, // Group key (date)
                DimmCount = (int)g.Count(), // Number of plays on that date
                SongTitle = _songIdToTitleMap.TryGetValue(g.First().SongId, out var title) ? title : "Unknown Title",
                ArtistName = _songIdToArtistMap.TryGetValue(g.First().SongId, out var artist) ? artist : "Unknown Artist",
                AlbumName = _songIdToAlbumMap.TryGetValue(g.First().SongId, out var album) ? album : "Unknown Album"
            });

        // Sort the results
        var sortedDays = isAscend
            ? dailyCounts.OrderBy(d => d.DimmCount)
            : dailyCounts.OrderByDescending(d => d.DimmCount);

        // Take the top N results
        MostDimmsPerDayCol = sortedDays
            .Select(s=> new DimmData() { DimmCount=s.DimmCount, SongTitle=s.SongTitle, ArtistName=s.ArtistName}).Take(top).ToObservableCollection();

        return MostDimmsPerDayCol;
    }




    [ObservableProperty]
    public partial ObservableCollection<DimmData> MostDimmsPerDayCol { get; set; } = new();

    #endregion

    public ObservableCollection<(string SongTitle, TimeSpan MaxGap)> GetGapBetweenTracks(
        List<string>? filterSongIdList = null,
        List<DateTime>? filterDates = null,
        bool isAscend = false)
    {
        var filteredPlays = GetFilteredPlays(new List<int> { 3 }, filterSongIdList, filterDates);

        var groupedPlays = filteredPlays
            .Where(p => p.PlayType == 3)
            .GroupBy(p => p.SongId)
            .Select(g => new
            {
                SongId = g.Key,
                SortedPlays = g.OrderBy(p => p.DateFinished).ToList()
            });

        var gapResults = new List<(string SongTitle, TimeSpan MaxGap)>();

        foreach (var group in groupedPlays)
        {
            if (group.SortedPlays.Count < 2)
                continue;

            TimeSpan maxGap = TimeSpan.Zero;

            for (int i = 1; i < group.SortedPlays.Count; i++)
            {
                var currentGap = group.SortedPlays[i].DateStarted - group.SortedPlays[i - 1].DateStarted;
                if (currentGap > maxGap)
                {
                    maxGap = currentGap;
                }
            }

            string songTitle = _songIdToTitleMap.TryGetValue(group.SongId, out string title) ? title : "Unknown";

            gapResults.Add((songTitle, maxGap));
        }

        var sortedResults = isAscend
            ? gapResults.OrderBy(r => r.MaxGap)
            : gapResults.OrderByDescending(r => r.MaxGap);

        return sortedResults.ToObservableCollection();
    } 

    [ObservableProperty]
    public partial ObservableCollection<(string SongTitle, TimeSpan MaxGap)> GapBetweenTracks { get; set; }

    public ObservableCollection<DimmData> GetOngoingGapBetweenTracks(
        List<string>? filterSongIdList = null,
        List<DateTime>? filterDates = null)
    {
        var filteredPlays = GetFilteredPlays(new List<int> { 3 }, filterSongIdList, filterDates);

        var lastEndedPlays = filteredPlays
            .Where(p => p.PlayType == 3)
            .GroupBy(p => p.SongId)
            .Select(g => new
            {
                SongId = g.Key,
                LastEnded = g.Max(p => p.DateFinished)
            });

        var currentTime = DateTime.Now;

        var ongoingGaps = lastEndedPlays
            .Select(g => new
            {
                SongTitle = _songIdToTitleMap.TryGetValue(g.SongId, out string title) ? title : "Unknown",
                OngoingGap = currentTime - g.LastEnded
            })
            .OrderByDescending(g => g.OngoingGap)
            .ToList();

        return ongoingGaps
            .Select(g => new DimmData() { SongTitle = g.SongTitle, OngoingGap = g.OngoingGap })
            .ToObservableCollection();
    } 

    [ObservableProperty]
    public partial ObservableCollection<DimmData> OngoingGapBetweenTracks { get; set; }

    public List<string> GetNewTracksInMonth(
        int month,
        int year,
        List<string>? filterSongIdList = null)
    {
        var filteredPlays = GetFilteredPlays(new List<int> { 3 }, filterSongIdList, null);

        var firstPlays = filteredPlays
            .Where(p => p.PlayType == 3 && p.DateFinished.Month == month && p.DateFinished.Year == year)
            .GroupBy(p => p.SongId)
            .Select(g => new
            {
                SongId = g.Key,
                FirstPlay = g.Min(p => p.DateFinished)
            })
            .ToList();

        var newTracks = firstPlays
            .Select(g => _songIdToTitleMap.TryGetValue(g.SongId, out string title) ? title : "Unknown")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return newTracks;
    } 

    [ObservableProperty]
    public partial ObservableCollection<string> NewTracksInMonth { get; set; } = new();

    public int GetUniqueTracksInMonth(
        int month,
        int year,
        List<string>? filterSongIdList = null)
    {
        var filteredPlays = GetFilteredPlays(new List<int> { 3 }, filterSongIdList, null);

        var uniqueTracks = filteredPlays
            .Where(p => p.PlayType == 3 && p.DateFinished.Month == month && p.DateFinished.Year == year)
            .Select(p => p.SongId)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        NewUniqueTracks = uniqueTracks.ToObservableCollection();
        NewUniqueTracksCount = uniqueTracks.Count;
        return NewUniqueTracksCount;
    } 

    [ObservableProperty]
    public partial ObservableCollection<string?> NewUniqueTracks { get; set; } = new();

    [ObservableProperty]
    public partial int NewUniqueTracksCount { get; set; }

    public void GetGoldenOldies(
        List<string>? filterSongIdList = null,
        List<DateTime>? filterDates = null)
    {
        var filteredPlays = GetFilteredPlays(new List<int> { 3 }, filterSongIdList, filterDates);

        var goldenOldies = filteredPlays
            .Where(p => p.PlayType == 3)
            .GroupBy(p => p.SongId)
            .Where(g => g.Count() >= 10)
            .Select(g => new
            {
                SongId = g.Key,
                AverageDate = new DateTime((long)g.Average(p => (double)p.DateFinished.Ticks))
            })
            .OrderByDescending(g => g.AverageDate)
            .ToList();

        var result = goldenOldies
            .Select(d =>
             new DimmData
             {
                 AverageEndedDate = d.AverageDate.ToLongDateString(),
                 SongTitle = _songIdToTitleMap.TryGetValue(d.SongId, out string title) ? title : "Unknown"
             })
            .ToObservableCollection();

        GoldenOldies= result;
    } 

    [ObservableProperty]
    public partial ObservableCollection<DimmData> GoldenOldies { get; set; }

    public List<(string SongTitle, int UniqueWeeks)> GetWeeksPerTrack(
        List<string>? filterSongIdList = null,
        List<DateTime>? filterDates = null,
        bool isAscend = false)
    {
        var filteredPlays = GetFilteredPlays(new List<int> { 3 }, filterSongIdList, filterDates);

        var songWeekCounts = filteredPlays
            .Where(p => p.PlayType == 3)
            .GroupBy(p => p.SongId)
            .Select(g => new
            {
                SongId = g.Key,
                UniqueWeeks = g.Select(p => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                                        p.DateFinished.LocalDateTime,
                                        CalendarWeekRule.FirstDay,
                                        DayOfWeek.Monday)).Distinct().Count()
            })
            .ToList();

        var results = songWeekCounts
            .Select(g => (
                _songIdToTitleMap.TryGetValue(g.SongId, out string title) ? title : "Unknown",
                g.UniqueWeeks))
            .OrderBy(r => isAscend ? r.UniqueWeeks : -r.UniqueWeeks)
            .ToList();

        return results;
    } 

    //public List<DimmData> GetTrackStreaks(
    //    List<string>? filterSongIdList = null,
    //    List<DateTime>? filterDates = null)
    //{
    //    var filteredPlays = GetFilteredPlays(new List<int> { 0, 3 }, filterSongIdList, filterDates)
    //        .OrderBy(p => p.DateFinished)
    //        .ToList();

    //    var streaks = new List<DimmData>();

    //    if (filteredPlays.Count == 0)
    //        return streaks;

    //    string currentSong = _songIdToTitleMap.TryGetValue(filteredPlays[0].SongId, out string title) ? title : "Unknown";
    //    int currentStreak = 1;
    //    DateTime streakStart = filteredPlays[0].DateStarted.LocalDateTime;
    //    DateTime streakEnd = filteredPlays[0].DateStarted.LocalDateTime;

    //    for (int i = 1; i < filteredPlays.Count; i++)
    //    {
    //        string songTitle = _songIdToTitleMap.TryGetValue(filteredPlays[i].SongId, out string t) ? t : "Unknown";

    //        if (songTitle == currentSong)
    //        {
    //            currentStreak++;
    //            streakEnd = filteredPlays[i].DateStarted.LocalDateTime;
    //        }
    //        else
    //        {
    //            streaks.Add( new DimmData() { SongTitle = currentSong, StreakLength = currentStreak, StartDate = streakStart, EndDate = streakEnd });

    //            currentSong = songTitle;
    //            currentStreak = 1;
    //            streakStart = filteredPlays[i].DateStarted.LocalDateTime;
    //            streakEnd = filteredPlays[i].DateStarted.LocalDateTime;
    //        }
    //    }
    //    streaks.Add(new DimmData() { SongTitle = currentSong, StreakLength = currentStreak, StartDate = streakStart, EndDate = streakEnd });
    //    return streaks;
    //} 

    public ObservableCollection<DimmData> GetHourlyPlayEventData(bool includeIntersection = true)
    {
        var filteredPlays = GetFilteredPlays(new List<int> { 3 });

        if (!includeIntersection)
        {
            filteredPlays = filteredPlays
                .GroupBy(p => new { p.SongId, Hour = p.DateFinished.LocalDateTime.Hour })
                .Select(g => g.First())
                .ToList();
        }

        HourlyPlayEventData =filteredPlays
            .GroupBy(p => p.DateFinished.LocalDateTime.Hour)
            .Select(g => new DimmData
            {
                TimeKey = g.Key,
                DimmCount = g.Count()
            })
            .ToObservableCollection();
        return HourlyPlayEventData;
    } 

    public ObservableCollection<DimmData> GetDailyPlayEventData(bool includeIntersection = true)
    {
        var filteredPlays = GetFilteredPlays(new List<int> { 3 });

        if (!includeIntersection)
        {
            filteredPlays = filteredPlays
                .GroupBy(p => new { p.SongId, Date = p.DateFinished.LocalDateTime })
                .Select(g => g.First())
                .ToList();
        }

        DailyPlayEventData= filteredPlays
            .GroupBy(p => p.DateFinished.Day)
            .Select(g => new DimmData
            {
                TimeKey = g.Key,
                DimmCount = g.Count()
            })
            .ToObservableCollection();
        return DailyPlayEventData;
    } 

    public ObservableCollection<DimmData> GetMonthlyPlayEventData(bool includeIntersection = true)
    {
        var filteredPlays = GetFilteredPlays(playTypes: new List<int> { 3 });

        if (!includeIntersection)
        {
            filteredPlays = filteredPlays
                .GroupBy(p => new { p.SongId, Month = p.DateFinished.DateTime.Month })
                .Select(g => g.First())
                .ToList();
        }

        MonthlyPlayEventData= filteredPlays
            .GroupBy(p => p.DateFinished.DateTime.Month)
            .Select(g => new DimmData
            {
                TimeKey = g.Key,
                DimmCount = g.Count()
            })
            .ToObservableCollection();
        return MonthlyPlayEventData;
    }

    [ObservableProperty]
    public partial ObservableCollection<DimmData>? MonthlyPlayEventData { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DimmData>? WeeklyPlayEventData { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DimmData>? DailyPlayEventData { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<DimmData>? HourlyPlayEventData { get; set; }
}

public partial class DimmData : ObservableObject
{
    [ObservableProperty]
    public partial int TimeKey { get; set; }
    [ObservableProperty]
    public partial string? SongId { get; set; }
    [ObservableProperty]
    public partial DateTime Date { get; set; }
    [ObservableProperty]
    public partial int DimmCount { get; set; }
    [ObservableProperty]
    public partial string PlayEventDescription { get; set; }
    [ObservableProperty]
    public partial int PlayEventCode { get; set; }
    
    [ObservableProperty]
    public partial int SeekCount { get; set; }
    [ObservableProperty]
    public partial string? PeakSessionStartDate { get; set; }
    [ObservableProperty]
    public partial int ConsecutivePlays { get; set; }

    [ObservableProperty]
    public partial int? Hour{ get; set; }
    [ObservableProperty]
    public partial string? Month { get; set; }
    [ObservableProperty]
    public partial string? Year { get; set; }
    [ObservableProperty]
    public partial string? ArtistName { get; set; }
    [ObservableProperty]
    public partial string? AlbumName { get; set; }
    [ObservableProperty]
    public partial string? GenreName { get; set; }
    
    [ObservableProperty]
    public partial int WeekNumber { get; set; }
    [ObservableProperty]
    public partial string? SongTitle { get; set; }
    [ObservableProperty]
    public partial int RankChange { get; set; }
    [ObservableProperty]
    public partial int PreviousMonthDimms { get; set; }
    [ObservableProperty]
    public partial int CurrentMonthDimms { get; set; }
    [ObservableProperty]
    public partial int MaxStreak { get; set; }
    [ObservableProperty]
    public partial string? AverageEndedDate { get; set; }
    [ObservableProperty]
    public partial TimeSpan MaxGap { get; set; }
    [ObservableProperty]
    public partial TimeSpan OngoingGap { get; set; }
    [ObservableProperty]
    public partial int UniqueWeeks { get; set; }
    [ObservableProperty]
    public partial int StreakLength { get; set; }
    [ObservableProperty]
    public partial DateTime StartDate { get; set; }
    [ObservableProperty]
    public partial DateTime FirstDimmDate{ get; set; }
    [ObservableProperty]
    public partial double TotalListeningHours { get; set; }
    [ObservableProperty]
    public partial double LifeTimeHours { get; set; }
    [ObservableProperty]
    public partial double DurationInSecond { get; set; }
    [ObservableProperty]
    public partial double SeekToEndRatio { get; set; }
    [ObservableProperty]
    public partial DateTime EndDate { get; set; }
    [ObservableProperty]
    public partial int RankLost { get; set; }
} 
