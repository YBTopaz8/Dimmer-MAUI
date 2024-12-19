using System.Collections.Concurrent;
using System.Diagnostics;
using DevExpress.Maui.Core.Internal;


namespace Dimmer_MAUI.ViewModels;

public partial class HomePageVM
{
    #region General Statistics Properties

    [ObservableProperty]
    public partial int DaysSinceFirstDimm { get; set; }

    [ObservableProperty]
    public partial DateTime DateOfFirstDimm { get; set; }

    [ObservableProperty]
    public partial string FirstDimmSong { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string FirstDimmArtist { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string FirstDimmAlbum { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTime DateOfLastDimm { get; set; }

    [ObservableProperty]
    public partial string LastDimmSong { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int TotalNumberOfArtists { get; set; }

    [ObservableProperty]
    public partial int TotalNumberOfAlbums { get; set; }

    [ObservableProperty]
    public partial string TopPlayedSong { get; set; } = string.Empty;
    [ObservableProperty]
    public partial ObservableCollection<DimmData> TopPlayedSongs { get; set; } = new();

    [ObservableProperty]
    public partial string TopPlayedArtist { get; set; } = string.Empty;
    [ObservableProperty]
    public partial ObservableCollection<DimmData> TopPlayedArtists { get; set; } = new();

    [ObservableProperty]
    public partial string TopPlayedAlbum { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TopPlayedGenre { get; set; } = string.Empty;
    
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

    private List<PlayDateAndCompletionStateSongLinkView> GetFilteredPlays(
        List<int>? playTypes,
        List<string>? filterSongIdList = null,
        List<DateTime>? filterDates = null)
    {
        PlayCacheKey cacheKey = new PlayCacheKey(filterSongIdList, filterDates, playTypes);
        if (_playCache.TryGet(cacheKey, out List<PlayDateAndCompletionStateSongLinkView>? cachedPlays))
            return cachedPlays;

        IEnumerable<PlayDateAndCompletionStateSongLinkView> results = AllPDaCStateLink!;

        bool filtered = false;

        if (filterSongIdList != null && filterSongIdList.Count > 0)
        {
            List<PlayDateAndCompletionStateSongLinkView> songResults = new List<PlayDateAndCompletionStateSongLinkView>();
            HashSet<string> uniqueSongIds = new HashSet<string>(filterSongIdList, StringComparer.OrdinalIgnoreCase);

            foreach (string sid in uniqueSongIds)
            {
                if (_playsBySongId.TryGetValue(sid, out List<PlayDateAndCompletionStateSongLinkView>? sList))
                {
                    songResults.AddRange(sList);
                }
            }

            results = songResults;
            filtered = true;
        }

        if (filterDates != null && filterDates.Count > 0)
        {
            HashSet<DateTime> dateSet = new HashSet<DateTime>(filterDates);
            if (!filtered)
            {
                List<PlayDateAndCompletionStateSongLinkView> dateResults = new List<PlayDateAndCompletionStateSongLinkView>();
                foreach (DateTime d in dateSet)
                {
                    if (_playsByDate.TryGetValue(d, out List<PlayDateAndCompletionStateSongLinkView>? dList))
                    {
                        dateResults.AddRange(dList);
                    }
                }
                results = dateResults;
            }
            else
            {
                List<PlayDateAndCompletionStateSongLinkView> filteredList = new List<PlayDateAndCompletionStateSongLinkView>();
                foreach (PlayDateAndCompletionStateSongLinkView item in results)
                {
                    if (dateSet.Contains(item.DateFinished.LocalDateTime.Date))
                        filteredList.Add(item);
                }
                results = filteredList;
            }
            filtered = true;
        }

        if (playTypes != null && playTypes.Count > 0)
        {
            HashSet<int> playTypeSet = new HashSet<int>(playTypes);
            List<PlayDateAndCompletionStateSongLinkView> finalList = new List<PlayDateAndCompletionStateSongLinkView>();
            foreach (PlayDateAndCompletionStateSongLinkView p in results)
            {
                if (playTypeSet.Contains(p.PlayType))
                    finalList.Add(p);
            }
            results = finalList;
        }

        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = results.ToList();
        _playCache.Add(cacheKey, filteredPlays);
        return filteredPlays;
    } 

    public void CalculateGeneralStatistics(List<PlayDateAndCompletionStateSongLinkView> playss)
    {
        if (playss == null || playss.Count == 0)
        {
            DaysSinceFirstDimm = 0;
            DateOfFirstDimm = DateTime.MinValue;
            FirstDimmSong = "N/A";
            DateOfLastDimm = DateTime.MinValue;
            LastDimmSong = "N/A";
            TotalNumberOfArtists = 0;
            TotalNumberOfAlbums = 0;
            TopPlayedSong = "N/A";
            TopPlayedArtist = "N/A";
            TopPlayedAlbum = "N/A";
            TopPlayedGenre = "N/A";
            TotalNumberOfDimms = 0;
            AverageDimmsPerDay = 0.0;
            AverageDimmsPerWeek = 0.0;
            return;
        }
        DisplayedSongs = SongsMgtService.AllSongs.ToObservableCollection();
        IEnumerable<PlayDateAndCompletionStateSongLinkView> plays = playss.Where(x => x.PlayType == 3);
        List<PlayDateAndCompletionStateSongLinkView> sortedPlays = plays.OrderBy(p => p.DateFinished).ToList();

        PlayDateAndCompletionStateSongLinkView? firstPlay = sortedPlays.FirstOrDefault(p => p.DateFinished > DateTime.MinValue);
        if (firstPlay != null)
        {
            DateOfFirstDimm = firstPlay.DateFinished.LocalDateTime;
            FirstDimmSong = _songIdToTitleMap.TryGetValue(firstPlay.SongId, out string title) ? title : DisplayedSongs.FirstOrDefault(x=>x.LocalDeviceId == firstPlay.SongId).Title;
            FirstDimmArtist = _songIdToArtistMap.TryGetValue(firstPlay.SongId, out string artist) ? artist: DisplayedSongs.FirstOrDefault(x => x.LocalDeviceId == firstPlay.SongId).ArtistName;
            FirstDimmAlbum = _songIdToAlbumMap.TryGetValue(firstPlay.SongId, out string album) ? artist: DisplayedSongs.FirstOrDefault(x => x.LocalDeviceId == firstPlay.SongId).AlbumName;

            DaysSinceFirstDimm = (DateTime.Now.Date - DateOfFirstDimm).Days;
        }
        else
        {
            DateOfFirstDimm = DateTime.MinValue;
            FirstDimmSong = "N/A";
            DaysSinceFirstDimm = 0;
        }

        PlayDateAndCompletionStateSongLinkView? lastPlay = sortedPlays.LastOrDefault();
        if (lastPlay != null)
        {
            DateOfLastDimm = lastPlay.DateFinished.LocalDateTime;
            LastDimmSong = _songIdToTitleMap.TryGetValue(lastPlay.SongId, out string lTitle) ? lTitle : "Unknown";
        }

        TotalNumberOfArtists = _songIdToArtistMap.Values.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        TotalNumberOfAlbums = _songIdToAlbumMap.Values.Distinct(StringComparer.OrdinalIgnoreCase).Count();

        DimmData? topSong = GetTopPlayedSongs(top: 1).FirstOrDefault();
        TopPlayedSong = topSong != default ? topSong.SongTitle : "N/A";

        SongModelView? statSong = DisplayedSongs.Where(x => x.Title.Equals(topSong.SongTitle)).FirstOrDefault();
        var art = GetAllArtistsFromSongID(statSong.LocalDeviceId).FirstOrDefault().Name;
        TopPlayedArtist = art is null ? "Unknown Artist" : art;
        var alb= GetAlbumFromSongID(statSong.LocalDeviceId).FirstOrDefault().Name;
        TopPlayedAlbum = alb is null ? "Unknown Album" : alb;

        TotalNumberOfDimms = plays.Count();
        int uniqueDays = plays.Select(p => p.DateFinished.LocalDateTime.DayOfYear).Distinct().Count();
        AverageDimmsPerDay = uniqueDays > 0 ? (double)TotalNumberOfDimms / uniqueDays : 0.0;

        int uniqueWeeks = plays.Where(p => p.DateFinished.LocalDateTime.Date != DateTime.MinValue).Select(p =>
            CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                p.DateFinished.LocalDateTime,
                CalendarWeekRule.FirstDay,
                DayOfWeek.Monday))
            .Distinct()
            .Count();
        AverageDimmsPerWeek = uniqueWeeks > 0 ? (double)TotalNumberOfDimms / uniqueWeeks : 0.0;
      
    } 
    private void GetAllStats()
    {
        //FibonacciPlay = GetFibonacciPlayCount();
        //GiniPlayIndex = GetGiniPlayIndex();
        //ParetoPlayRatio = GetParetoPlayRatio();
        //OopsIDidItAgainRatio = GetOopsIDidItAgainRatio();
        //PauseResumeRatio = GetPauseResumeRatio();
        ////BlackSwanSkips = GetBlackSwanSkips();
        //DaysNeededForNextEddington = GetDaysNeededForNextEddington();
        //SongLoyaltyIndex = GetSongLoyaltyIndex();
        //GenreConsistencyScore = GetGenreConsistencyScore();
        //PeakListeningSession = GetPeakListeningSession();
        //ArchetypalGenreMix = GetArchetypalGenreMix();
        //BayesianGenreBelief = GetBayesianGenreBelief();
        //BenfordGenreDistribution = GetBenfordGenreDistribution();
        ////CauchyInterarrivalTimes = GetCauchyInterarrivalTimes();
        //ChaosTheoryAttractorScore = GetChaosTheoryAttractorScore();
        //CognitiveDissonanceRatio = GetCognitiveDissonanceRatio();
        //CulturalCapitalIndex = GetCulturalCapitalIndex();
        //CumulativeAdvantageIndex = GetCumulativeAdvantageIndex();
        //DecibelThresholdCrossings = GetDecibelThresholdCrossings();
        //EcologicalFootprintOfGenres = GetEcologicalFootprintOfGenres();
        //EmotionalEnergyGradient = GetEmotionalEnergyGradient();
        //FourierRhythmSignature = GetFourierRhythmSignature();
        ////FractalListeningDimension = GetFractalListeningDimension();
        ////FuzzySetGenreMembership = GetFuzzySetGenreMembership();
        ////GameTheoryShuffleScore = GetGameTheoryShuffleScore();
        //GaussianListeningSpread = GetGaussianListeningSpread();
        //GeographicalSpreadOfArtists = GetGeographicalSpreadOfArtists();
        //GetGoldenOldies();
        //GoldenRatioPlaylistAffinity = GetGoldenRatioPlaylistAffinity();
        //GuitarStringBalance = GetGuitarStringBalance();
        //HarmonicMeanPlayLength = GetHarmonicMeanPlayLength();
        //HeatmapHero = GetHeatmapHero();
        
        //H_indexOfArtists = GetH_indexOfArtists();
        //InfluenceNetworkCentrality = GetInfluenceNetworkCentrality();
        //KolmogorovComplexityOfPlaylist = GetKolmogorovComplexityOfPlaylist();
        //LorenzCurveGenreEquality = GetLorenzCurveGenreEquality();
        //MoodConvergenceScore = GetMoodConvergenceScore();
        //MusicalROI = GetMusicalROI();
        //PoissonSkipFrequency = GetPoissonSkipFrequency();
        //ProcrastinationTuneIndex = GetProcrastinationTuneIndex();
        //PythagoreanGenreHarmony = GetPythagoreanGenreHarmony();
        //QuantumSuperpositionOfTastes = GetQuantumSuperpositionOfTastes();
        //ReverseChronologyPlayStreak = GetReverseChronologyPlayStreak();
        //SeasonalAutocorrelation = GetSeasonalAutocorrelation();
        //SeekSurgeMoments = GetSeekSurgeMoments();
        //SeekToEndRatioForAllSongs = GetSeekToEndRatioForAllSongs();
        //SemanticLyricDiversity = GetSemanticLyricDiversity();
        //ShannonEntropyOfGenres = GetShannonEntropyOfGenres();
        //SimpsonGenreDiversityIndex = GetSimpsonGenreDiversityIndex();
        //SocioAcousticIndex = GetSocioAcousticIndex();
        //StochasticResonanceIndex = GetStochasticResonanceIndex();
        //SynestheticColorSpread = GetSynestheticColorSpread();
        //TemporalCompressionIndex = GetTemporalCompressionIndex();
        //TopGapLargestTimeBetweenDimms = GetTopGapLargestTimeBetweenDimms();
        //TopLatestDiscoveries = GetTopLatestDiscoveries();
        ////TopSongsWithMostSeeks = GetTopSongsWithMostSeeks(); // redundant
        //TopStreakTracks = GetTopStreakTracks().ToObservableCollection();
        //DimmsWalkThrough = GetTrackStreaks(50).ToObservableCollection();
        //TopWeekPerTrack = GetTopWeekPerTrack();
        //VirtuosoDensityIndex = GetVirtuosoDensityIndex();
        //WaveletListeningComplexity = GetWaveletListeningComplexity();
        //WeightedMedianPlayTime = GetWeightedMedianPlayTime();
        //ZipfLyricFocus = GetZipfLyricFocus();
        //Z_ScoreOfListeningTime = GetZ_ScoreOfListeningTime();
        

    }

    [ObservableProperty]
    public partial ObservableCollection<PlayTypeSummaryCount>? PlayTypeAndCounts { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<PlayCompletionStatusString>? PlayTypeSummaryData { get; set; } = new();

    public void LoadDailyData(List<string>? filterSongIdList = null, List<DateTime>? FilterDates = null)
    {
        PlayTypeSummaryData = GetPlayCompletionStatus(filterSongIdList, FilterDates).ToObservableCollection();
        PlayTypeAndCounts = GetPlayEventAndCount(filterSongIdList, FilterDates).ToObservableCollection();

        List<PlayDateAndCompletionStateSongLinkView> dimmPlays = GetFilteredPlays(new List<int> { 3 }, filterSongIdList, FilterDates).ToList();

        if (CurrentPage == PageEnum.FullStatsPage)
        {
            CalculateGeneralStatistics(dimmPlays);
        }
    } 

    private Dictionary<string, string>? _songIdToTitleMap = new();
    private Dictionary<string, string>? _songIdToArtistMap = new();
    private Dictionary<string, string>? _songIdToAlbumMap = new();
    private Dictionary<string, string>? _songIdToGenreMap = new();
    private Dictionary<string, int?>? _songIdToReleaseYearMap = new();
    private Dictionary<string, double> _songIdToDurationMap = new();

    public void LoadData()
    {
        if (DisplayedSongs == null || DisplayedSongs.Count < 1)
            return;

        _songIdToTitleMap = DisplayedSongs
            .ToDictionary(s => s.LocalDeviceId, s => s.Title, StringComparer.OrdinalIgnoreCase);

        InitializeArtistMapping();
        InitializeAlbumMapping();

        _songIdToReleaseYearMap = DisplayedSongs
            .Where(s => s.ReleaseYear > 0)
            .ToDictionary(s => s.LocalDeviceId, s => s.ReleaseYear, StringComparer.OrdinalIgnoreCase);

        _songIdToDurationMap = DisplayedSongs
            .Where(s => s.DurationInSeconds > 0)
            .ToDictionary(s => s.LocalDeviceId, s => s.DurationInSeconds, StringComparer.OrdinalIgnoreCase);

        _songIdToGenreMap = DisplayedSongs
            .Where(s => s.GenreName != null)
            .ToDictionary(s => s.LocalDeviceId, s => s.GenreName, StringComparer.OrdinalIgnoreCase);

        BuildIndexes();
    } 

    private void BuildIndexes()
    {
        _playsBySongId.Clear();
        _playsByDate.Clear();
        _playsByPlayType.Clear();

        foreach (PlayDateAndCompletionStateSongLinkView p in AllPDaCStateLink)
        {
            if (p.DateStarted == DateTimeOffset.MinValue || p.DateFinished == DateTimeOffset.MinValue)
                continue;

            if (!string.IsNullOrEmpty(p.SongId))
            {
                string sid = p.SongId.ToLowerInvariant();
                if (!_playsBySongId.TryGetValue(sid, out List<PlayDateAndCompletionStateSongLinkView>? songList))
                {
                    songList = new List<PlayDateAndCompletionStateSongLinkView>();
                    _playsBySongId[sid] = songList;
                }
                songList.Add(p);
            }

            DateTime finishDate = p.DateFinished.LocalDateTime.Date;
            if (!_playsByDate.TryGetValue(finishDate, out List<PlayDateAndCompletionStateSongLinkView>? dateList))
            {
                dateList = new List<PlayDateAndCompletionStateSongLinkView>();
                _playsByDate[finishDate] = dateList;
            }
            dateList.Add(p);

            if (!_playsByPlayType.TryGetValue(p.PlayType, out List<PlayDateAndCompletionStateSongLinkView>? typeList))
            {
                typeList = new List<PlayDateAndCompletionStateSongLinkView>();
                _playsByPlayType[p.PlayType] = typeList;
            }
            typeList.Add(p);
        }
    } 

    private void InitializeArtistMapping()
    {
        if (AllLinks == null || AllArtists == null)
        {
            _songIdToArtistMap = new Dictionary<string, string>();
            return;
        }

        Dictionary<string, string> tempDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (AlbumArtistGenreSongLinkView? link in AllLinks.Where(link => !string.IsNullOrEmpty(link.SongId) && !string.IsNullOrEmpty(link.ArtistId)))
        {
            string artistName = AllArtists
                .FirstOrDefault(artist => artist.LocalDeviceId.Equals(link.ArtistId, StringComparison.OrdinalIgnoreCase))?.Name ?? "Unknown";

            if (!tempDictionary.ContainsKey(link.SongId))
            {
                tempDictionary[link.SongId] = artistName;
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

            if (!tempDictionary.TryGetValue(link.SongId, out string? value))
            {
                tempDictionary[link.SongId] = albumName;
            }
            else
            {
                Debug.WriteLine($"Duplicate SongId detected: {link.SongId} (Existing Album: {value}, New Album: {albumName})");
            }
        }

        _songIdToAlbumMap = tempDictionary;
    } 

    public int GetUniqueTracksInSingleMonth(
        List<string>? filterSongIdList = null,
        List<DateTime>? filterDates = null)
    {
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(new List<int> { 0 }, filterSongIdList, filterDates);
        return filteredPlays
            .Where(p => !string.IsNullOrEmpty(p.SongId))
            .Select(p => p.SongId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
    } 

    public List<PlayTypeSummaryCount> GetPlayEventAndCount(
        List<string>? filterSongIdList = null,
        List<DateTime>? filterDates = null,
        bool isAscend = false)
    {
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(new List<int> { 2,3,4,5 }, filterSongIdList, filterDates);
        var playEventCounts = filteredPlays
            .GroupBy(p =>
            {
                switch (p.PlayType)
                {
                    case 2:
                        return "Paused";
                    case 3:
                        return "Played Completely";
                    case 4:
                        return "Seeked";
                    case 5:
                        return "Skipped";
                }
                return null;
            })
            .Where(g => g.Key != null)
            .Select(g => new { PlayEvent = g.Key!, Count = g.Count() });

        var sortedPlayEventCounts = isAscend
            ? playEventCounts.OrderBy(pe => pe.Count)
            : playEventCounts.OrderByDescending(pe => pe.Count);

        return sortedPlayEventCounts
            .Select(pe => new PlayTypeSummaryCount
            {
                PlayTypeCode = pe.PlayEvent switch
                {
                    "Paused" => 2,
                    "Played Completely" => 3,                    
                    "Seeked" => 4,
                    "Skipped" => 5,
                    _ => -1
                },
                PlayTypeDescription = pe.PlayEvent,
                Count = pe.Count
            })
            .ToList();
    } 

    public List<(string Hour, int PlayCount)> GetPlayCountByHourOfDay(
        List<string>? filterSongIdList = null,
        List<DateTime>? filterDates = null,
        bool isAscend = false)
    {
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(new List<int> { 3 }, filterSongIdList, filterDates);
        var playCountsByHour = filteredPlays
            .GroupBy(p => p.DateFinished.Hour)
            .Select(g => new
            {
                Hour = g.Key,
                Count = g.Count()
            });

        var sortedPlayCounts = isAscend
            ? playCountsByHour.OrderBy(pc => pc.Count)
            : playCountsByHour.OrderByDescending(pc => pc.Count);

        return sortedPlayCounts
            .Select(pc => ($"{pc.Hour}:00", pc.Count))
            .ToList();
    } 

    public List<(string DurationRange, int PlayCount)> GetPlayDurationDistribution(
        List<string>? filterSongIdList = null,
        List<DateTime>? filterDates = null,
        bool isAscend = false)
    {
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(new List<int> { 3 }, filterSongIdList, filterDates);
        var durationRanges = new[]
        {
            new { Min = 0, Max = 60, Label = "0-1 min" },
            new { Min = 60, Max = 180, Label = "1-3 mins" },
            new { Min = 180, Max = 300, Label = "3-5 mins" },
            new { Min = 300, Max = 600, Label = "5-10 mins" },
            new { Min = 600, Max = int.MaxValue, Label = "10+ mins" }
        };

        var playDurationDistribution = durationRanges
            .Select(range => new
            {
                Range = range.Label,
                Count = filteredPlays.Count(p => p.PositionInSeconds >= range.Min && p.PositionInSeconds < range.Max)
            })
            .OrderBy(r => isAscend ? r.Count : -r.Count)
            .ToList();

        return playDurationDistribution
            .Select(r => (r.Range, r.Count))
            .ToList();
    } 

    public List<DimmData> GetTopLatestDiscoveries(
        int top = 20,
        bool isAscend = false)
    {
        var plays = GetFilteredPlays(new List<int> { 3 }, null, null)
                    .GroupBy(p => p.SongId)
                    .Where(g => g.Count() >= 10)
                    .Select(g => new
                    {
                        SongId = g.Key,
                        FirstDimm = g.Min(p => p.DateFinished)
                    })
                    .ToList();

        if (plays.Count == 0)
            return new List<DimmData>();

        var sortedDiscoveries = isAscend
            ? plays.OrderBy(g => g.FirstDimm)
            : plays.OrderByDescending(g => g.FirstDimm);

        return sortedDiscoveries.Take(top)
            .Select(g => (new DimmData()
            {
                SongTitle = _songIdToTitleMap.TryGetValue(g.SongId, out string title) ? title : "Unknown",
                FirstDimmDate = g.FirstDimm.Date
            })).ToList();
    } 

    public List<DimmData> GetTopWeekPerTrack(
        int top = 20,
        List<string>? filterSongIdList = null,
        List<DateTime>? filterDates = null,
        bool isAscend = false)
    {
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(new List<int> { 3 }, filterSongIdList, filterDates);
        ConcurrentDictionary<string, HashSet<int>> songWeekCounts = new ConcurrentDictionary<string, HashSet<int>>();

        Parallel.ForEach(filteredPlays, play =>
        {
            int week = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                        play.DateStarted.LocalDateTime,
                        CalendarWeekRule.FirstDay,
                        DayOfWeek.Monday);

            songWeekCounts.AddOrUpdate(play.SongId,
                new HashSet<int> { week },
                (key, existingSet) =>
                {
                    lock (existingSet)
                    {
                        existingSet.Add(week);
                        return existingSet;
                    }
                });
        });

        var songUniqueWeeks = songWeekCounts.Select(kvp => new
        {
            SongId = kvp.Key,
            UniqueWeeks = kvp.Value.Count
        });

        var sortedWeeks = isAscend
            ? songUniqueWeeks.OrderBy(s => s.UniqueWeeks)
            : songUniqueWeeks.OrderByDescending(s => s.UniqueWeeks);

        return sortedWeeks.Take(top)
            .Select(s => (
            new DimmData()
            {
                SongTitle =
                _songIdToTitleMap.TryGetValue(s.SongId, out string title) ? title : "Unknown",
                UniqueWeeks = s.UniqueWeeks
            }))
            .ToList();
    } 

    public (
        Dictionary<DateTime, int> DimmsOverTime,
        Dictionary<DateTime, int> ArtistsOverTime,
        Dictionary<DateTime, int> SongsOverTime)
        GetDimmsArtistsSongsOverTime(
        List<string>? filterSongIdList = null,
        List<DateTime>? filterDates = null,
        bool isAscend = false)
    {
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(new List<int> { 3 }, filterSongIdList, filterDates);
        ConcurrentDictionary<DateTime, int> scrobbleCounts = new ConcurrentDictionary<DateTime, int>();
        ConcurrentDictionary<DateTime, ConcurrentBag<string>> artistSetOverTime = new ConcurrentDictionary<DateTime, ConcurrentBag<string>>();
        ConcurrentDictionary<DateTime, ConcurrentBag<string>> songSetOverTime = new ConcurrentDictionary<DateTime, ConcurrentBag<string>>();

        Parallel.ForEach(filteredPlays, play =>
        {
            DateTime date = play.DateStarted.LocalDateTime;
            scrobbleCounts.AddOrUpdate(date, 1, (key, count) => count + 1);

            string artistName = _songIdToArtistMap.TryGetValue(play.SongId, out string aName) ? aName : "Unknown";
            artistSetOverTime.AddOrUpdate(date, new ConcurrentBag<string> { artistName }, (key, bag) =>
            {
                bag.Add(artistName);
                return bag;
            });

            string songTitle = _songIdToTitleMap.TryGetValue(play.SongId, out string sTitle) ? sTitle : "Unknown";
            songSetOverTime.AddOrUpdate(date, new ConcurrentBag<string> { songTitle }, (key, bag) =>
            {
                bag.Add(songTitle);
                return bag;
            });
        });

        Dictionary<DateTime, int> artistsOverTime = artistSetOverTime.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Distinct(StringComparer.OrdinalIgnoreCase).Count());

        Dictionary<DateTime, int> songsOverTime = songSetOverTime.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Distinct(StringComparer.OrdinalIgnoreCase).Count());

        return (
            scrobbleCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            artistsOverTime,
            songsOverTime
        );
    } 

    public ObservableCollection<DimmData> GetDotPointsForTracksArtists(
        int minDimms = 10,
        List<string>? filterSongIdList = null,
        List<DateTime>? filterDates = null)
    {
        List<DimmData> filteredPlays = GetFilteredPlays(new List<int> { 3 }, filterSongIdList, filterDates)
                            .GroupBy(p => p.SongId)
                            .Where(g => g.Count() >= minDimms)
                            .Select(g => new DimmData { SongId = g.Key, DimmCount = g.Count() })
                            .ToList();

        if (filteredPlays.Count == 0)
            return new();

        ConcurrentBag<DimmData> dotPoints = new ConcurrentBag<DimmData>();

        Parallel.ForEach(filteredPlays, play =>
        {
            string artistName = _songIdToArtistMap.TryGetValue(play.SongId, out string aName) ? aName : "Unknown Artist";
            string songTitle = _songIdToTitleMap.TryGetValue(play.SongId, out string sTitle) ? sTitle : "Unknown Song";

            dotPoints.Add(new DimmData { ArtistName = artistName, SongTitle = songTitle, DimmCount = play.DimmCount });
        });

        return dotPoints.ToObservableCollection();
    } 

    [ObservableProperty]
    public partial ObservableCollection<DimmData>? DotPointsForArtist { get; set; } = new();

    public async Task CompareTwoSongsAsync(string songId1, string songId2, TimePeriod timePeriod)
    {
        if (string.IsNullOrEmpty(songId1) || string.IsNullOrEmpty(songId2))
        {
            return;
        }

        DateTime endDate = DateTime.Now.Date;
        DateTime startDate = timePeriod switch
        {
            TimePeriod.Day => endDate.AddDays(-1),
            TimePeriod.Week => endDate.AddDays(-7),
            TimePeriod.Month => endDate.AddMonths(-1),
            TimePeriod.Year => endDate.AddYears(-1),
            _ => endDate.AddMonths(-1),
        };

        AlbumArtistGenreSongLinkView? songLink1 = AllLinks?.FirstOrDefault(l => l.SongId.Equals(songId1, StringComparison.OrdinalIgnoreCase));
        AlbumArtistGenreSongLinkView? songLink2 = AllLinks?.FirstOrDefault(l => l.SongId.Equals(songId2, StringComparison.OrdinalIgnoreCase));

        if (songLink1 == null || songLink2 == null)
        {
            return;
        }

        _songIdToTitleMap.TryGetValue(songId1, out string? songTitle1);
        _songIdToTitleMap.TryGetValue(songId2, out string? songTitle2);

        List<PlayDateAndCompletionStateSongLinkView> plays = GetFilteredPlays(new List<int> { 3 }, null, null).ToList();

        List<PlayDateAndCompletionStateSongLinkView> playsSong1 = plays.Where(p =>
            p.SongId.Equals(songId1, StringComparison.OrdinalIgnoreCase) &&
            p.DateFinished.LocalDateTime.Date != DateTime.MinValue &&
            p.DateFinished.LocalDateTime >= startDate &&
            p.DateFinished.LocalDateTime <= endDate).ToList();

        List<PlayDateAndCompletionStateSongLinkView> playsSong2 = plays.Where(p => p.SongId.Equals(songId2, StringComparison.OrdinalIgnoreCase) &&
            p.DateFinished.LocalDateTime.Date != DateTime.MinValue &&
            p.DateFinished.LocalDateTime >= startDate &&
            p.DateFinished.LocalDateTime <= endDate).ToList();

        PlayStats stats1 = CalculatePlayStats(playsSong1);
        PlayStats stats2 = CalculatePlayStats(playsSong2);

        ComparativePlayStats comparativeStats = new ComparativePlayStats
        {
            Song1Id = songId1,
            Song1Title = songTitle1 ?? "Unknown",
            Song2Id = songId2,
            Song2Title = songTitle2 ?? "Unknown",
            TimePeriod = timePeriod,
            TotalPlaysSong1 = stats1.TotalPlays,
            TotalPlaysSong2 = stats2.TotalPlays,
            IsTotalPlaysIncrease = CompareValues(stats1.TotalPlays, stats2.TotalPlays, out double totalPlaysPercentage),

            CompletedPlaysSong1 = stats1.CompletedPlays,
            CompletedPlaysSong2 = stats2.CompletedPlays,
            IsCompletedPlaysIncrease = CompareValues(stats1.CompletedPlays, stats2.CompletedPlays, out double completedPlaysPercentage),
            CompletedPlaysPercentageChange = completedPlaysPercentage,

            NotCompletedPlaysSong1 = stats1.NotCompletedPlays,
            NotCompletedPlaysSong2 = stats2.NotCompletedPlays,
            IsNotCompletedPlaysIncrease = CompareValues(stats1.NotCompletedPlays, stats2.NotCompletedPlays, out double notCompletedPlaysPercentage),
            NotCompletedPlaysPercentageChange = notCompletedPlaysPercentage,

            AveragePositionSong1 = stats1.AveragePositionInSeconds,
            AveragePositionSong2 = stats2.AveragePositionInSeconds,
            IsAveragePositionIncrease = CompareValues(stats1.AveragePositionInSeconds, stats2.AveragePositionInSeconds, out double averagePositionPercentage),
            AveragePositionPercentageChange = averagePositionPercentage,

            TopSeekedPositionsSong1 = stats1.TopSeekedPositions,
            TopSeekedPositionsSong2 = stats2.TopSeekedPositions,
        };

        ComparativeStats = comparativeStats;
        OnPropertyChanged(nameof(ComparativeStats));
        await Task.CompletedTask;
    } 

    public List<DimmData> GetTopSongsWithMostSeeks(
        int top = 20,
        List<string>? filterSongIdList = null,
        List<DateTime>? filterDates = null,
        bool isAscend = false)
    {
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(new List<int> { 4 }, filterSongIdList, filterDates);
        var seekCounts = filteredPlays
            .GroupBy(p => p.SongId)
            .Select(g => new { SongId = g.Key, SeekCount = g.Count() });

        var sortedSeeks = isAscend
            ? seekCounts.OrderBy(g => g.SeekCount)
            : seekCounts.OrderByDescending(g => g.SeekCount);

        return sortedSeeks.Take(top)
            .Select(g => (new DimmData()
            {
                SongTitle = _songIdToTitleMap.TryGetValue(g.SongId, out string title) ? title : "Unknown",
                SeekCount = g.SeekCount
            }))
            .ToList();
    } 

    public double GetPercentagePlaysWithSeeksForSong(
        string songId,
        List<DateTime>? filterDates = null,
        bool isAscend = false)
    {
        if (string.IsNullOrEmpty(songId))
            return 0.0;

        List<PlayDateAndCompletionStateSongLinkView> filteredPlaysCompleted = GetFilteredPlays(new List<int> { 3 }, new List<string> { songId }, filterDates);
        List<PlayDateAndCompletionStateSongLinkView> filteredPlaysSeeked = GetFilteredPlays(new List<int> { 4 }, new List<string> { songId }, filterDates);

        int totalPlays = filteredPlaysCompleted.Count;
        if (totalPlays == 0)
            return 0.0;

        int seekPlays = filteredPlaysSeeked.Count;
        return (double)seekPlays / totalPlays * 100;
    } 

    public List<DimmData> GetSeekToEndRatioForAllSongs(
        double seekThreshold = 250.0,
        List<string>? filterSongIdList = null,
        List<DateTime>? filterDates = null,
        bool isAscend = false)
    {
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(new List<int> { 4 }, filterSongIdList, filterDates);
        var seekData = filteredPlays
            .GroupBy(p => p.SongId)
            .Select(g => new
            {
                SongId = g.Key,
                TotalSeeks = g.Count(),
                EndSeeks = g.Count(p => p.PositionInSeconds >= seekThreshold)
            });

        var ratioList = seekData
            .Select(g => new
            {
                g.SongId,
                Ratio = g.TotalSeeks > 0 ? (double)g.EndSeeks / g.TotalSeeks * 100 : 0.0
            })
            .OrderBy(g => isAscend ? g.Ratio : -g.Ratio)
            .Take(10)
            .ToList();

        return ratioList
            .Select(g => (new DimmData()
            {
                SongTitle = _songIdToTitleMap.TryGetValue(g.SongId, out string title) ? title : "Unknown",
                SeekToEndRatio = g.Ratio
            }))
            .ToList();
    } 

    public List<(string ArtistName, int PlayCount)> GetStatisticsForDifferentSpellings(
        List<string> artistNameVariants,
        bool combine = true,
        bool isAscend = false)
    {
        if (artistNameVariants == null || artistNameVariants.Count == 0)
            return new List<(string, int)>();

        HashSet<string> variantSet = new HashSet<string>(artistNameVariants, StringComparer.OrdinalIgnoreCase);
        string combinedName = "Combined";

        List<PlayDateAndCompletionStateSongLinkView> plays = GetFilteredPlays(new List<int> { 3 }, null, null)
                    .Where(p => _songIdToArtistMap.TryGetValue(p.SongId, out string artistName) &&
                                variantSet.Contains(artistName))
                    .ToList();

        ConcurrentDictionary<string, int> artistPlayCounts = new ConcurrentDictionary<string, int>();

        Parallel.ForEach(plays, play =>
        {
            if (_songIdToArtistMap.TryGetValue(play.SongId, out string artistName) && variantSet.Contains(artistName))
            {
                if (combine)
                {
                    artistPlayCounts.AddOrUpdate(combinedName, 1, (key, count) => count + 1);
                }
                else
                {
                    artistPlayCounts.AddOrUpdate(artistName, 1, (key, count) => count + 1);
                }
            }
        });

        IOrderedEnumerable<KeyValuePair<string, int>> sortedStats = isAscend
            ? artistPlayCounts.OrderBy(kvp => kvp.Value)
            : artistPlayCounts.OrderByDescending(kvp => kvp.Value);

        return sortedStats.Select(kvp => (kvp.Key, kvp.Value)).ToList();
    } 

    private Dictionary<string, List<PlayDateAndCompletionStateSongLinkView>> _playsBySongId
        = new Dictionary<string, List<PlayDateAndCompletionStateSongLinkView>>(StringComparer.OrdinalIgnoreCase);

    private Dictionary<DateTime, List<PlayDateAndCompletionStateSongLinkView>> _playsByDate
        = new Dictionary<DateTime, List<PlayDateAndCompletionStateSongLinkView>>();

    private Dictionary<int, List<PlayDateAndCompletionStateSongLinkView>> _playsByPlayType
        = new Dictionary<int, List<PlayDateAndCompletionStateSongLinkView>>();

    private readonly LruCache<PlayCacheKey, List<PlayDateAndCompletionStateSongLinkView>> _playCache
        = new(100);

    public List<DimmData> GetPlayEvents(
        List<string>? filterSongIdList = null,
        List<DateTime>? filterDates = null,
        bool isAscend = false)
    {
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(null, filterSongIdList, filterDates);
        IOrderedEnumerable<PlayDateAndCompletionStateSongLinkView> sortedPlays = isAscend
            
            ? filteredPlays.OrderBy(p => p.DateFinished)
            : filteredPlays.OrderByDescending(p => p.DateFinished);

        List<DimmData> playEvents = new List<DimmData>(sortedPlays.Count());

        foreach (PlayDateAndCompletionStateSongLinkView? play in sortedPlays)
        {
            string time = play.DateStarted.ToString("hh:mm:sstt", CultureInfo.InvariantCulture);
            string eventDescription = play.PlayType switch
            {
                0 => "Played",
                1 => "Paused",
                2 => "Resumed",
                3 => "Ended",
                4 => $"Seeked to {TimeSpan.FromSeconds(play.PositionInSeconds):mm\\:ss}",
                _ => "Unknown Event"
            };

            string description = $"{time} {eventDescription}";
            playEvents.Add(new DimmData() { PlayEventDescription = description, PlayEventCode = play.PlayType });
        }

        return playEvents;
    } 

    public IEnumerable<PlayCompletionStatusString> GetPlayCompletionStatus(
        List<string>? filterSongIdList = null,
        List<DateTime>? filterDates = null,
        bool isAscend = false)
    {
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(new List<int>{ 3}, filterSongIdList, filterDates)
            .Where(p => p.PlayType == 0 || p.PlayType == 3)
            .ToList();

        IEnumerable<PlayDateAndCompletionStateSongLinkView> sortedPlays = isAscend
            ? filteredPlays.OrderBy(p => p.DateFinished)
            : filteredPlays.OrderByDescending(p => p.DateFinished);

        List<PlayCompletionStatusString> playCompletionList = new List<PlayCompletionStatusString>();
        Dictionary<string, DateTimeOffset> songActivePlayMap = new Dictionary<string, DateTimeOffset>(StringComparer.OrdinalIgnoreCase);

        foreach (PlayDateAndCompletionStateSongLinkView play in sortedPlays)
        {
            string songId = play.SongId;

            if (play.PlayType == 0 && play.PositionInSeconds == 0)
            {
                if (songActivePlayMap.TryGetValue(songId, out DateTimeOffset value))
                {
                    string previousPlayDescription = value.ToString("F", CultureInfo.InvariantCulture);
                    playCompletionList.Add(new PlayCompletionStatusString() { PlayTypeDescription = previousPlayDescription + " Played", IsPlayedCompletely = false });

                    songActivePlayMap[songId] = play.DateStarted;
                }
                else
                {
                    songActivePlayMap.Add(songId, play.DateStarted);
                }
            }
            else if (play.PlayType == 3)
            {
                if (songActivePlayMap.TryGetValue(songId, out DateTimeOffset value))
                {
                    string playDescription = value.ToString("hh:mm:sstt", CultureInfo.InvariantCulture);
                    playCompletionList.Add(new PlayCompletionStatusString() 
                    { PlayTypeDescription = playDescription + " Ended", IsPlayedCompletely = true });

                    songActivePlayMap.Remove(songId);
                }
            }
        }

        foreach (KeyValuePair<string, DateTimeOffset> kvp in songActivePlayMap)
        {
            string description = kvp.Value.ToString("hh:mm:sstt", CultureInfo.InvariantCulture) + " Played";
            playCompletionList.Add(new PlayCompletionStatusString() { PlayTypeDescription = description, IsPlayedCompletely = false });
        }
        ColPlayCompletionStatus = playCompletionList.ToObservableCollection();
        return ColPlayCompletionStatus;
    } 

    [ObservableProperty]
    public partial ObservableCollection<PlayCompletionStatusString> ColPlayCompletionStatus { get; private set; } = new();

    private PlayStats CalculatePlayStats(List<PlayDateAndCompletionStateSongLinkView> plays)
    {
        PlayStats stats = new PlayStats
        {
            TotalPlays = plays.Count,
            CompletedPlays = plays.Count(p => p.WasPlayCompleted),
            AveragePositionInSeconds = plays.Count > 0 ? plays.Average(p => p.PositionInSeconds) : 0.0
        };

        List<SeekPositionStats> seekPositionCounts = plays
            .Where(p => p.PlayType == 4)
            .Select(p => Math.Floor(p.PositionInSeconds / 10) * 10)
            .GroupBy(pos => pos)
            .Select(g => new SeekPositionStats
            {
                PositionInSeconds = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(s => s.Count)
            .Take(5)
            .ToList();

        stats.TopSeekedPositions = seekPositionCounts;
        return stats;
    } 

    private bool CompareValues(double value1, double value2, out double percentageChange)
    {
        if (value1 == 0 && value2 == 0)
        {
            percentageChange = 0;
            return false;
        }
        if (value1 == 0)
        {
            percentageChange = 100;
            return true;
        }

        percentageChange = ((value2 - value1) / value1) * 100;
        return value2 > value1;
    } 

    [ObservableProperty]
    public partial Dictionary<DateTime, int> PlaysPerDay { get; set; } = new();

    [ObservableProperty]
    public partial SingleSongDailyStat SongStats { get; private set; } = new();

    [ObservableProperty]
    public partial ComparativePlayStats? ComparativeStats { get; private set; } = new();

    public ObservableCollection<DimmData>? GetLifetimeBingeSong(List<string>? filterSongNames = null, List<DateTime>? filterDates = null, bool isAscending = false)
    {
        List<string>? filterSongIds = null;
        if (filterSongNames != null && filterSongNames.Count != 0)
        {
            filterSongIds = DisplayedSongs
                .Where(s => filterSongNames.Contains(s.Title, StringComparer.OrdinalIgnoreCase))
                .Select(s => s.LocalDeviceId)
                .ToList()!;
        }

        List<int> playTypes = new List<int> { 3 };
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(playTypes, filterSongIds, filterDates);

        if (filteredPlays == null || filteredPlays.Count == 0)
            return null;

        Dictionary<string, double> songDurations = _songIdToDurationMap;
        ConcurrentDictionary<string, double> timePerSong = new ConcurrentDictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        Parallel.ForEach(filteredPlays, play =>
        {
            if (!songDurations.TryGetValue(play.SongId, out double duration))
                return;

            double timePlayed = 0.0;
            if (play.PlayType == 3)
            {
                timePlayed = duration;
            }

            timePerSong.AddOrUpdate(play.SongId, timePlayed, (key, existingTime) => existingTime + timePlayed);
        });

        if (timePerSong.Count == 0)
            return null;

        LifetimeBingeSongs = timePerSong
            .OrderByDescending(kvp => kvp.Value)
            .Select(kvp => new DimmData
            {
                SongTitle = _songIdToTitleMap.TryGetValue(kvp.Key, out string title) ? title : "Unknown",
                LifeTimeHours = kvp.Value / 3600.0
            })
            .Take(15)
            .ToObservableCollection();

        return LifetimeBingeSongs;
    } 

    [ObservableProperty]
    public partial ObservableCollection<DimmData> LifetimeBingeSongs { get; set; } = new();

    public double GetMusicalROI(List<string>? filterSongNames = null, List<DateTime>? filterDates = null, bool isAscending = false)
    {
        List<string>? filterSongIds = null;
        if (filterSongNames != null && filterSongNames.Count != 0)
        {
            filterSongIds = DisplayedSongs
                .Where(s => filterSongNames.Contains(s.Title, StringComparer.OrdinalIgnoreCase))
                .Select(s => s.LocalDeviceId)
                .ToList()!;
        }

        List<int> playTypes = new List<int> { 0, 4 };
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(playTypes, filterSongIds, filterDates);

        if (filteredPlays == null || filteredPlays.Count == 0)
            return 0.0;

        Dictionary<string, double> songDurations = _songIdToDurationMap;
        double listenedTime = 0.0;
        double skippedTime = 0.0;

        foreach (PlayDateAndCompletionStateSongLinkView play in filteredPlays)
        {
            if (!songDurations.TryGetValue(play.SongId, out double duration))
                continue;

            if (play.PlayType == 0)
            {
                listenedTime += duration;
            }
            else if (play.PlayType == 4)
            {
                skippedTime += play.PositionInSeconds;
            }
        }

        double totalInvestedTime = listenedTime + skippedTime;
        if (totalInvestedTime == 0.0)
            return 0.0;

        double roi = (listenedTime / totalInvestedTime) * 100.0;
        return roi;
    } 

    public double GetPauseResumeRatio(List<string>? filterSongNames = null, List<DateTime>? filterDates = null, bool isAscending = false)
    {
        List<string>? filterSongIds = null;
        if (filterSongNames != null && filterSongNames.Count != 0)
        {
            filterSongIds = DisplayedSongs
                .Where(s => filterSongNames.Contains(s.Title, StringComparer.OrdinalIgnoreCase))
                .Select(s => s.LocalDeviceId)
                .ToList()!;
        }

        List<int> playTypes = new List<int> { 1, 2 };
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(playTypes, filterSongIds, filterDates);

        if (filteredPlays == null || filteredPlays.Count == 0)
            return 0.0;

        int pauseCount = filteredPlays.Count(p => p.PlayType == 1);
        int resumeCount = filteredPlays.Count(p => p.PlayType == 2);

        if (pauseCount == 0)
            return 0.0;

        double ratio = ((double)resumeCount / pauseCount) * 100.0;
        return ratio;
    } 

    public int GetOopsIDidItAgainRatio(List<string>? filterSongNames = null, List<DateTime>? filterDates = null, bool isAscending = false)
    {
        List<string>? filterSongIds = null;
        if (filterSongNames != null && filterSongNames.Count != 0)
        {
            filterSongIds = DisplayedSongs
                .Where(s => filterSongNames.Contains(s.Title, StringComparer.OrdinalIgnoreCase))
                .Select(s => s.LocalDeviceId)
                .ToList()!;
        }

        List<int> playTypes = new List<int> { 0, 2, 3, 4 };
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(playTypes, filterSongIds, filterDates);

        if (filteredPlays == null || filteredPlays.Count == 0)
            return 0;

        List<IGrouping<string?, PlayDateAndCompletionStateSongLinkView>> playsBySong = filteredPlays
                         .GroupBy(p => p.SongId)
                         .ToList();

        if (playsBySong.Count == 0)
            return 0;

        int restartCount = 0;
        
        string? currentSong = null; // To track the current song
        //TODO : CHECK THIS TO TRACK LAST PLAYS
        PlayDateAndCompletionStateSongLinkView? lastPlay = null; // To track the last play

        foreach (var group in playsBySong)
        {
            // Order plays by DateStarted for each song group
            var sortedPlays = group
                              .Where(p => p.DateStarted != DateTimeOffset.MinValue)
                              .OrderBy(p => p.DateStarted)
                              .ToList();

            foreach (var play in sortedPlays)
            {
                // If it's a new song, reset tracking
                if (currentSong != play.SongId)
                {
                    currentSong = play.SongId;
                    lastPlay = play;
                    continue;
                }

                // Check if the current play is within 30 seconds of the previous play
                if (lastPlay != null && play.DateStarted - lastPlay.DateFinished <= TimeSpan.FromSeconds(30))
                {
                    restartCount++; // Increment restart count
                }

                // Update the last play for the current song
                lastPlay = play;
            }
        }


        return restartCount;
    } 

    [ObservableProperty]
    public partial int OopsIDidItAgainRatio { get; set; }

    public int GetReverseChronologyPlayStreak(List<string>? filterSongNames = null, List<DateTime>? filterDates = null, bool isAscending = false)
    {
        List<string>? filterSongIds = null;
        if (filterSongNames != null && filterSongNames.Count != 0)
        {
            filterSongIds = DisplayedSongs
                .Where(s => filterSongNames.Contains(s.Title, StringComparer.OrdinalIgnoreCase))
                .Select(s => s.LocalDeviceId)
                .ToList()!;
        }

        List<int> playTypes = new List<int> { 3 } ;
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(playTypes, filterSongIds, filterDates);

        if (filteredPlays == null || filteredPlays.Count == 0)
            return 0;

        List<PlayDateAndCompletionStateSongLinkView> orderedPlays = filteredPlays
            .Where(x=> x.DateStarted != DateTime.MinValue)
            .OrderBy(p => p.DateStarted).ToList();
        if (orderedPlays.Count == 0)
            return 0;

        int longestStreak = 1;
        int currentStreak = 1;

        string? firstSongId = orderedPlays[0].SongId;
        if (!_songIdToReleaseYearMap.TryGetValue(firstSongId, out int? previousReleaseYear))
        {
            previousReleaseYear = 0;
        }

        for (int i = 1; i < orderedPlays.Count; i++)
        {
            string? currentSongId = orderedPlays[i].SongId;
            if (!_songIdToReleaseYearMap.TryGetValue(currentSongId, out int? currentReleaseYear))
            {
                currentReleaseYear = 0;
            }

            if (currentReleaseYear <= previousReleaseYear)
            {
                currentStreak++;
                if (currentStreak > longestStreak)
                    longestStreak = currentStreak;
            }
            else
            {
                currentStreak = 1;
            }

            previousReleaseYear = currentReleaseYear;
        }

        return longestStreak;
    } 

    public IEnumerable<string> GetStatisticalOutlierSongs(List<string>? filterSongNames = null, List<DateTime>? filterDates = null, bool isAscending = false)
    {
        List<string>? filterSongIds = null;
        if (filterSongNames != null && filterSongNames.Count != 0)
        {
            filterSongIds = DisplayedSongs
                .Where(s => filterSongNames.Contains(s.Title, StringComparer.OrdinalIgnoreCase))
                .Select(s => s.LocalDeviceId)
                .ToList()!;
        }

        List<int> playTypes = new List<int> { 0, 2 };
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(playTypes, filterSongIds, filterDates);

        if (filteredPlays == null || filteredPlays.Count == 0)
            return new List<string>();

        List<int> playCounts = filteredPlays
                         .GroupBy(p => p.SongId)
                         .Select(g => g.Count())
                         .OrderBy(count => count)
                         .ToList();

        if (playCounts.Count < 4)
            return new List<string>();

        double q1 = Quantile(playCounts, 0.25);
        double q3 = Quantile(playCounts, 0.75);
        double iqr = q3 - q1;

        double lowerBound = q1 - 1.5 * iqr;
        double upperBound = q3 + 1.5 * iqr;

        List<int> outlierPlayCounts = playCounts.Where(count => count < lowerBound || count > upperBound).Distinct().ToList();

        if (outlierPlayCounts.Count == 0)
            return new List<string>();

        List<IGrouping<string?, PlayDateAndCompletionStateSongLinkView>> outlierSongIds = GetFilteredPlays(playTypes, filterSongIds, filterDates)
                              .GroupBy(p => p.SongId)
                              .Where(g => outlierPlayCounts.Contains(g.Count()))
                              .Select(g => g)
                              .Distinct()
                              .ToList();

        List<string> outlierSongTitles = outlierSongIds
                                .Select(g => _songIdToTitleMap.TryGetValue(g.Key, out string title) ? title : "Unknown")
                                .ToList();

        if (isAscending)
        {
            outlierSongTitles = outlierSongTitles.OrderBy(title => title).ToList();
        }
        else
        {
            outlierSongTitles = outlierSongTitles.OrderByDescending(title => title).ToList();
        }

        return outlierSongTitles;
    } 

    [ObservableProperty]
    public partial ObservableCollection<string> OutLiersSongs { get; set; } = new();

    [ObservableProperty]
    public partial List<DimmData> SeekToEndRatioForAllSongs { get; set; } = new();

    private double Quantile(List<int> sortedData, double quantile)
    {
        if (sortedData == null || sortedData.Count == 0)
            throw new ArgumentException("Data cannot be null or empty.");

        double position = (sortedData.Count + 1) * quantile;
        int index = (int)position;

        if (index < 1)
            return sortedData.First();
        if (index >= sortedData.Count)
            return sortedData.Last();

        double fraction = position - index;
        return sortedData[index - 1] + fraction * (sortedData[index] - sortedData[index - 1]);
    } 

    public double GetSeekToEndRatio(List<string>? filterSongNames = null, List<DateTime>? filterDates = null, bool isAscending = false)
    {
        List<string>? filterSongIds = null;
        if (filterSongNames != null && filterSongNames.Count != 0)
        {
            filterSongIds = DisplayedSongs
                .Where(s => filterSongNames.Contains(s.Title, StringComparer.OrdinalIgnoreCase))
                .Select(s => s.LocalDeviceId)
                .ToList()!;
        }

        List<int> playTypes = new List<int> { 0, 4 };
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(playTypes, filterSongIds, filterDates);

        if (filteredPlays == null || filteredPlays.Count == 0)
            return 0.0;

        List<IGrouping<string?, PlayDateAndCompletionStateSongLinkView>> playsBySong = filteredPlays
                         .GroupBy(p => p.SongId)
                         .ToList();

        if (playsBySong.Count == 0)
            return 0.0;

        int songsWithSeek = 0;
        int songsWithSeekAndFinish = 0;

        foreach (IGrouping<string?, PlayDateAndCompletionStateSongLinkView>? group in playsBySong)
        {
            bool hasSeek = group.Any(p => p.PlayType == 4);
            bool hasFinish = group.Any(p => p.PlayType == 0);

            if (hasSeek)
            {
                songsWithSeek++;
                if (hasFinish)
                    songsWithSeekAndFinish++;
            }
        }

        if (songsWithSeek == 0)
            return 0.0;

        double ratio = (double)songsWithSeekAndFinish / songsWithSeek;
        return ratio;
    } 

    public (int Hour, double Percentage)? GetHeatmapHero(List<string>? filterSongNames = null, List<DateTime>? filterDates = null, bool isAscending = false)
    {
        List<string>? filterSongIds = null;
        if (filterSongNames != null && filterSongNames.Count != 0)
        {
            filterSongIds = DisplayedSongs
                .Where(s => filterSongNames.Contains(s.Title, StringComparer.OrdinalIgnoreCase))
                .Select(s => s.LocalDeviceId)
                .ToList()!;
        }

        List<int> playTypes = new List<int> { 3 } ;
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(playTypes, filterSongIds, filterDates);

        if (filteredPlays == null || filteredPlays.Count == 0)
            return null;

        var playsByHour = filteredPlays
                          .GroupBy(p => p.DateStarted.ToLocalTime().Hour)
                          .Select(g => new { Hour = g.Key, Count = g.Count() })
                          .ToList();

        if (playsByHour.Count == 0)
            return null;

        var topHour = playsByHour
                      .OrderByDescending(g => g.Count)
                      .First();

        int totalPlays = filteredPlays.Count;
        double percentage = ((double)topHour.Count / totalPlays) * 100.0;

        return (topHour.Hour, percentage);
    } 

    public IEnumerable<string> GetBlackSwanSkips(List<string>? filterSongNames = null, List<DateTime>? filterDates = null, bool isAscending = false)
    {
        List<string>? filterSongIds = null;
        if (filterSongNames != null && filterSongNames.Count != 0)
        {
            filterSongIds = DisplayedSongs
                .Where(s => filterSongNames.Contains(s.Title, StringComparer.OrdinalIgnoreCase))
                .Select(s => s.LocalDeviceId)
                .ToList()!;
        }

        List<int> playTypes = new List<int> { 0, 1, 2, 3, 4 };
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(playTypes, filterSongIds, filterDates);

        if (filteredPlays == null || filteredPlays.Count == 0)
            return new List<string>();

        List<IGrouping<string?, PlayDateAndCompletionStateSongLinkView>> playsBySong = filteredPlays
                         .GroupBy(p => p.SongId)
                         .Where(g => g.Count() == 1)
                         .ToList();

        if (playsBySong.Count == 0)
            return new List<string>();

        List<string?> skippedSongs = playsBySong
                          .Where(g => g.Any(p => p.PlayType == 4))
                          .Select(g => g.Key)
                          .ToList();

        List<string> skippedSongTitles = skippedSongs
                                .Select(songId => _songIdToTitleMap.TryGetValue(songId, out string title) ? title : "Unknown")
                                .ToList();

        if (isAscending)
        {
            skippedSongTitles = skippedSongTitles.OrderBy(title => title).ToList();
        }
        else
        {
            skippedSongTitles = skippedSongTitles.OrderByDescending(title => title).ToList();
        }

        BlackSwanSkips = skippedSongTitles.ToObservableCollection();
        return BlackSwanSkips;
    } 


    public double GetGiniPlayIndex(List<string>? filterSongNames = null, List<DateTime>? filterDates = null, bool isAscending = false)
    {
        List<string>? filterSongIds = null;
        if (filterSongNames != null && filterSongNames.Count != 0)
        {
            filterSongIds = DisplayedSongs
                .Where(s => filterSongNames.Contains(s.Title, StringComparer.OrdinalIgnoreCase))
                .Select(s => s.LocalDeviceId)
                .ToList()!;
        }

        List<int> playTypes = new List<int> { 3 } ;
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(playTypes, filterSongIds, filterDates);

        if (filteredPlays == null || filteredPlays.Count == 0)
            return 0.0;

        List<int> playCounts = filteredPlays
                         .GroupBy(p => p.SongId)
                         .Select(g => g.Count())
                         .OrderBy(count => count)
                         .ToList();

        if (playCounts.Count == 0)
            return 0.0;

        playCounts.Sort();

        double cumulativeWeightedSum = 0.0;
        int n = playCounts.Count;
        double sum = playCounts.Sum();
        double mean = sum / n;

        for (int i = 0; i < n; i++)
        {
            cumulativeWeightedSum += (2 * (i + 1) - n - 1) * playCounts[i];
        }

        double gini = cumulativeWeightedSum / (n * n * mean);
        GiniPlayIndex = gini;
        return GiniPlayIndex;
    } 

    public int GetSeekSurgeMoments(List<string>? filterSongNames = null, List<DateTime>? filterDates = null, bool isAscending = false)
    {
        List<string>? filterSongIds = null;
        if (filterSongNames != null && filterSongNames.Count != 0)
        {
            filterSongIds = DisplayedSongs
                .Where(s => filterSongNames.Contains(s.Title, StringComparer.OrdinalIgnoreCase))
                .Select(s => s.LocalDeviceId)
                .ToList()!;
        }

        List<int> seekPlayTypes = new List<int> { 4 };
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(seekPlayTypes, filterSongIds, filterDates);

        if (filteredPlays == null || filteredPlays.Count == 0)
            return 0;

        List<DateTime> seekDays = filteredPlays
                       .Select(p => p.DateStarted.LocalDateTime)
                       .Distinct()
                       .OrderBy(d => d)
                       .ToList();

        if (seekDays.Count == 0)
            return 0;

        int longestStreak = 1;
        int currentStreak = 1;

        for (int i = 1; i < seekDays.Count; i++)
        {
            if ((seekDays[i] - seekDays[i - 1]).TotalDays == 1)
            {
                currentStreak++;
                if (currentStreak > longestStreak)
                    longestStreak = currentStreak;
            }
            else
            {
                currentStreak = 1;
            }
        }

        return longestStreak;
    } 

    [ObservableProperty]
    public partial double SeekSurgeMomentsStreakCount { get; set; } = 0.0;

    public int GetFibonacciPlayCount(List<string>? filterSongNames = null, List<DateTime>? filterDates = null, bool isAscending = false)
    {
        List<string>? filterSongIds = null;
        if (filterSongNames != null && filterSongNames.Count != 0)
        {
            filterSongIds = DisplayedSongs
                .Where(s => filterSongNames.Contains(s.Title, StringComparer.OrdinalIgnoreCase))
                .Select(s => s.LocalDeviceId)
                .ToList()!;
        }

        List<int> playTypes = new List<int> { 3 } ;
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(playTypes, filterSongIds, filterDates);

        if (filteredPlays == null || filteredPlays.Count == 0)
            return 0;

        List<int> playCounts = filteredPlays
                         .GroupBy(p => p.SongId)
                         .Select(g => g.Count())
                         .OrderByDescending(count => count)
                         .ToList();

        if (playCounts.Count == 0)
            return 0;

        int maxPossibleFibonacci = playCounts.Max();
        List<int> fibonacci = new List<int> { 1, 2 };
        while (fibonacci.Last() <= maxPossibleFibonacci)
        {
            int nextFib = fibonacci[^1] + fibonacci[^2];
            fibonacci.Add(nextFib);
        }

        fibonacci = fibonacci.Where(f => f <= maxPossibleFibonacci).ToList();

        int fibonacciPlayCount = 0;
        foreach (int fib in fibonacci.OrderByDescending(f => f))
        {
            int songsWithAtLeastFPlays = playCounts.Count(count => count >= fib);
            if (songsWithAtLeastFPlays >= fib)
            {
                fibonacciPlayCount = fib;
                break;
            }
        }
        FibonacciPlay = fibonacciPlayCount;
        return FibonacciPlay;
    } 

    [ObservableProperty]
    public partial int FibonacciPlay { get; set; } = 0;

    public double GetParetoPlayRatio(List<string>? filterSongNames = null, List<DateTime>? filterDates = null, bool isAscending = false)
    {
        List<string>? filterSongIds = null;
        if (filterSongNames != null && filterSongNames.Count != 0)
        {
            filterSongIds = DisplayedSongs
                .Where(s => filterSongNames.Contains(s.Title, StringComparer.OrdinalIgnoreCase))
                .Select(s => s.LocalDeviceId)
                .ToList()!;
        }

        List<int> playTypes = new List<int> { 3 } ;
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(playTypes, filterSongIds, filterDates);

        if (filteredPlays == null || filteredPlays.Count == 0)
            return 0.0;

        var playCounts = filteredPlays
                         .GroupBy(p => p.SongId)
                         .Select(g => new { SongId = g.Key, PlayCount = g.Count() })
                         .OrderByDescending(g => g.PlayCount)
                         .ToList();

        if (playCounts.Count == 0)
            return 0.0;

        int totalSongs = playCounts.Count;
        int top20PercentCount = (int)Math.Ceiling(totalSongs * 0.2);
        if (top20PercentCount == 0)
            top20PercentCount = 1;

        var topSongs = playCounts.Take(top20PercentCount).ToList();
        int topSongsPlayCount = topSongs.Sum(s => s.PlayCount);
        int totalPlayCount = playCounts.Sum(s => s.PlayCount);

        double paretoRatio = ((double)topSongsPlayCount / totalPlayCount) * 100.0;
        ParetoRatio = paretoRatio;
        return paretoRatio;
    } 

    [ObservableProperty]
    public partial double ParetoRatio { get; set; } = 0.0;

    public double GetSongLoyaltyIndex(List<string>? filterSongNames = null, List<DateTime>? filterDates = null, bool isAscending = false)
    {
        List<string>? filterSongIds = null;
        if (filterSongNames != null && filterSongNames.Count != 0)
        {
            filterSongIds = DisplayedSongs
                .Where(s => filterSongNames.Contains(s.Title, StringComparer.OrdinalIgnoreCase))
                .Select(s => s.LocalDeviceId)
                .ToList()!;
        }

        List<int> playTypes = new List<int> { 3 } ;
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(playTypes, filterSongIds, filterDates);

        if (filteredPlays == null || filteredPlays.Count == 0)
            return 0.0;

        var playCounts = filteredPlays
                         .GroupBy(p => p.SongId)
                         .Select(g => new { SongId = g.Key, PlayCount = g.Count() })
                         .OrderByDescending(g => g.PlayCount)
                         .ToList();

        if (playCounts.Count == 0)
            return 0.0;

        int totalSongs = playCounts.Count;
        int top10PercentCount = (int)Math.Ceiling(totalSongs * 0.1);

        if (top10PercentCount == 0)
            top10PercentCount = 1;

        HashSet<string?> topSongs = playCounts.Take(top10PercentCount).Select(g => g.SongId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<DateTime> distinctDays = filteredPlays
                           .Where(p => p.DateFinished != DateTimeOffset.MinValue)
                           .Select(p => p.DateFinished.Date)
                           .Distinct()
                           .ToList();

        if (distinctDays.Count == 0)
            return 0.0;

        int daysPlayedByTopSongs = filteredPlays
                                    .Where(p => topSongs.Contains(p.SongId) && p.DateFinished != DateTimeOffset.MinValue)
                                    .Select(p => p.DateFinished.Date)
                                    .Distinct()
                                    .Count();

        double loyaltyIndex = ((double)daysPlayedByTopSongs / distinctDays.Count) * 100.0;
        LoyaltyIndex = loyaltyIndex;
        return LoyaltyIndex;
    } 

    [ObservableProperty]
    public partial double LoyaltyIndex { get; set; } = 0.0;

    public ObservableCollection<DimmData> GetDailyListeningVolume(List<string>? filterSongNames = null, List<DateTime>? filterDates = null, bool isAscending = false)
    {
        List<string>? filterSongIds = null;
        if (filterSongNames != null && filterSongNames.Count != 0)
        {
            filterSongIds = DisplayedSongs
                .Where(s => filterSongNames.Contains(s.Title, StringComparer.OrdinalIgnoreCase))
                .Select(s => s.LocalDeviceId)
                .ToList()!;
        }

        List<int> playTypes = new List<int> { 0, 2, 4 };
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(playTypes, filterSongIds, filterDates);

        if (filteredPlays == null || filteredPlays.Count == 0)
            return null;

        Dictionary<string, double> songDurations = _songIdToDurationMap;

        var listeningVolume = filteredPlays
                              .Where(p => songDurations.ContainsKey(p.SongId) && p.DateStarted != DateTimeOffset.MinValue)
                              .GroupBy(p => p.DateStarted.LocalDateTime)
                              .Select(g => new
                              {
                                  Date = g.Key,
                                  TotalSeconds = g.Sum(p =>
                                      p.PlayType == 0 ? songDurations[p.SongId] :
                                      p.PlayType == 2 ? songDurations[p.SongId] * 0.5 :
                                      p.PlayType == 4 ? p.PositionInSeconds : 0.0)
                              }).ToList();

        Dictionary<DateTime, double> listeningVolumeDict = listeningVolume
                                  .ToDictionary(
                                      lv => lv.Date,
                                      lv => Math.Round(lv.TotalSeconds / 3600.0, 2)
                                  );

        IOrderedEnumerable<KeyValuePair<DateTime, double>> sortedListeningVolume = isAscending
            ? listeningVolumeDict.OrderBy(kvp => kvp.Key)
            : listeningVolumeDict.OrderByDescending(kvp => kvp.Key);

        DailyListeningVolume = sortedListeningVolume
            .Select(kvp => new DimmData { Date = kvp.Key, TotalListeningHours = kvp.Value })
            .ToObservableCollection();
        return DailyListeningVolume;
    } 

    [ObservableProperty]
    public partial ObservableCollection<DimmData> DailyListeningVolume { get; set; }

    public DimmData GetMostFrequentReleaseYear(List<string>? filterSongNames = null, List<DateTime>? filterDates = null, bool isAscending = false)
    {
        List<string>? filterSongIds = null;
        if (filterSongNames != null && filterSongNames.Count != 0)
        {
            filterSongIds = DisplayedSongs
                .Where(s => filterSongNames.Contains(s.Title, StringComparer.OrdinalIgnoreCase))
                .Select(s => s.LocalDeviceId)
                .ToList()!;
        }

        List<int> playTypes = new List<int> { 3 } ;
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(playTypes, filterSongIds, filterDates);

        if (filteredPlays == null || filteredPlays.Count == 0)
            return new();

        Dictionary<string, int?>? songToReleaseYear = _songIdToReleaseYearMap;
        List<DimmData> releaseYearCounts = filteredPlays
                                .Where(p => songToReleaseYear.ContainsKey(p.SongId))
                                .GroupBy(p => songToReleaseYear[p.SongId])
                                .Select(g => new DimmData() { Year = g.Key.ToString(), DimmCount = g.Count() })
                                .ToList();

        if (releaseYearCounts.Count == 0)
            return new();

        DimmData topReleaseYear = isAscending
            ? releaseYearCounts.OrderBy(g => g.DimmCount).First()
            : releaseYearCounts.OrderByDescending(g => g.DimmCount).First();

        return topReleaseYear;
    } 

    public double GetGenreConsistencyScore(List<string>? filterSongNames = null, List<DateTime>? filterDates = null, bool isAscending = false)
    {
        List<string>? filterSongIds = null;
        if (filterSongNames != null && filterSongNames.Count != 0)
        {
            filterSongIds = DisplayedSongs
                .Where(s => filterSongNames.Contains(s.Title, StringComparer.OrdinalIgnoreCase))
                .Select(s => s.LocalDeviceId)
                .ToList()!;
        }

        List<int> playTypes = new List<int> { 3 } ;
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(playTypes, filterSongIds, filterDates);

        if (filteredPlays == null || filteredPlays.Count == 0)
            return 0.0;

        Dictionary<string, string>? songToGenre = _songIdToGenreMap;
        var genrePlayCounts = filteredPlays
                              .Where(p => songToGenre.ContainsKey(p.SongId))
                              .GroupBy(p => songToGenre[p.SongId])
                              .Select(g => new { Genre = g.Key, Count = g.Count() })
                              .ToList();

        if (genrePlayCounts.Count == 0)
            return 0.0;

        double totalPlays = genrePlayCounts.Sum(g => g.Count);
        double entropy = 0.0;
        foreach (var genre in genrePlayCounts)
        {
            double p = genre.Count / totalPlays;
            entropy -= p * Math.Log(p, 2);
        }

        int distinctGenres = genrePlayCounts.Count;
        double maxEntropy = Math.Log(distinctGenres, 2);

        double normalizedEntropy = maxEntropy > 0 ? entropy / maxEntropy : 0.0;
        double consistencyScore = 1.0 - normalizedEntropy;

        GeneralGenreConsistencyScore = Math.Round(consistencyScore, 2);
        return GeneralGenreConsistencyScore;
    } 

    [ObservableProperty]
    public partial double GeneralGenreConsistencyScore { get; set; }

    public double GetReplayProbability(string songName, List<DateTime>? filterDates = null, bool isAscending = false)
    {
        if (string.IsNullOrEmpty(songName))
            return 0.0;

        SongModelView? song = DisplayedSongs.FirstOrDefault(s => s.Title.Equals(songName, StringComparison.OrdinalIgnoreCase));
        if (song == null)
            return 0.0;

        string songId = song.LocalDeviceId;
        List<int> playTypes = new List<int> { 0, 1, 2, 4 };
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(playTypes, new List<string> { songId }, filterDates);

        if (filteredPlays == null || filteredPlays.Count == 0)
            return 0.0;

        int totalAttempts = filteredPlays.Count;
        int successfulPlays = filteredPlays.Count(p => p.PlayType == 0);

        if (totalAttempts == 0)
            return 0.0;

        double probability = ((double)successfulPlays / totalAttempts) * 100.0;
        return Math.Round(probability, 2);
    } 

    public DimmData? GetPeakListeningSession(List<string>? filterSongNames = null, List<DateTime>? filterDates = null, bool isAscending = false)
    {
        List<string>? filterSongIds = null;
        if (filterSongNames != null && filterSongNames.Count != 0)
        {
            filterSongIds = DisplayedSongs
                .Where(s => filterSongNames.Contains(s.Title, StringComparer.OrdinalIgnoreCase))
                .Select(s => s.LocalDeviceId)
                .ToList()!;
        }

        List<int> playTypes = new List<int> { 3 } ;
        List<PlayDateAndCompletionStateSongLinkView> filteredPlays = GetFilteredPlays(playTypes, filterSongIds, filterDates).Where(p => p.DateStarted != DateTimeOffset.MinValue)
                            .OrderBy(p => p.DateStarted)
                            .ToList();

        if (filteredPlays == null || filteredPlays.Count == 0)
            return null;

        double longestDuration = 0.0;
        int longestConsecutivePlays = 0;
        double currentDuration = 0.0;
        int currentConsecutivePlays = 0;
        DateTimeOffset? sessionStart = null;

        foreach (PlayDateAndCompletionStateSongLinkView? play in filteredPlays)
        {
            if (sessionStart == null)
            {
                sessionStart = play.DateStarted;
                currentConsecutivePlays = 1;
                currentDuration = 0.0;
            }
            else
            {
                PlayDateAndCompletionStateSongLinkView? previousPlay = filteredPlays.FirstOrDefault(p => p.DateStarted < play.DateStarted && p.SongId == play.SongId);
                if (previousPlay != null)
                {
                    TimeSpan gap = play.DateStarted - previousPlay.DateStarted;
                    if (gap.TotalMinutes <= 5)
                    {
                        currentConsecutivePlays++;
                        currentDuration += play.PositionInSeconds;
                    }
                    else
                    {
                        if (currentDuration > longestDuration)
                        {
                            longestDuration = currentDuration;
                            longestConsecutivePlays = currentConsecutivePlays;
                        }

                        sessionStart = play.DateStarted;
                        currentConsecutivePlays = 1;
                        currentDuration = play.PositionInSeconds;
                    }
                }
                else
                {
                    sessionStart = play.DateStarted;
                    currentConsecutivePlays = 1;
                    currentDuration = play.PositionInSeconds;
                }
            }
        }

        if (currentDuration > longestDuration)
        {
            longestDuration = currentDuration;
            longestConsecutivePlays = currentConsecutivePlays;
        }

        return new DimmData() { PeakSessionStartDate = sessionStart.ToString(), DurationInSecond = longestDuration, ConsecutivePlays = longestConsecutivePlays };
    } 
}

public class PlayDetail
{
    public string SongId { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTimeOffset DatePlayed { get; set; }
    public DateTimeOffset DateFinished { get; set; }
    public double PositionInSeconds { get; set; }
}

public class ExtendedPlayStats : PlayStats
{
    public List<PlayDetail> PlayDetails { get; set; } = new();
}  

public partial class SingleSongDailyStat : ObservableObject
{
    [ObservableProperty]
    public partial DateTime? WalkDateTime { get; set; }

    [ObservableProperty]
    public partial DateOnly? WalkDateOnly { get; set; }

    [ObservableProperty]
    public partial TimeOnly? WalkTimeOnly { get; set; }

    [ObservableProperty]
    public partial string? SongTitle { get; set; }

    [ObservableProperty]
    public partial string? ArtistName { get; set; }

    [ObservableProperty]
    public partial string? AlbumName { get; set; }

    [ObservableProperty]
    public partial string? EventSummary { get; set; }

    [ObservableProperty]
    public partial string? TopSeekedPositions { get; set; }

    [ObservableProperty]
    public partial ExtendedPlayStats? PlayStats { get; set; }

    [ObservableProperty]
    public partial ComparativePlayStats? ComparisonStats { get; set; }

    [ObservableProperty]
    public partial double? PercentageListen { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DailyPercentage>? DailyListenPercentages { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DailyPercentage>? DailyCompletedListenPercentages { get; set; }
} 

public partial class ComparativePlayStats : ObservableObject
{
    public string Song1Id { get; set; } = string.Empty;
    public string Song1Title { get; set; } = string.Empty;
    public string Song2Id { get; set; } = string.Empty;
    public string Song2Title { get; set; } = string.Empty;
    public TimePeriod TimePeriod { get; set; }
    public int TotalPlaysSong1 { get; set; }
    public int TotalPlaysSong2 { get; set; }
    public bool IsTotalPlaysIncrease { get; set; }
    public double TotalPlaysPercentageChange { get; set; }
    public int CompletedPlaysSong1 { get; set; }
    public int CompletedPlaysSong2 { get; set; }
    public bool IsCompletedPlaysIncrease { get; set; }
    public double CompletedPlaysPercentageChange { get; set; }
    public int NotCompletedPlaysSong1 { get; set; }
    public int NotCompletedPlaysSong2 { get; set; }
    public bool IsNotCompletedPlaysIncrease { get; set; }
    public double NotCompletedPlaysPercentageChange { get; set; }
    public double AveragePositionSong1 { get; set; }
    public double AveragePositionSong2 { get; set; }
    public bool IsAveragePositionIncrease { get; set; }
    public double AveragePositionPercentageChange { get; set; }
    public List<SeekPositionStats> TopSeekedPositionsSong1 { get; set; } = new();
    public List<SeekPositionStats> TopSeekedPositionsSong2 { get; set; } = new();
} 

public class DailyPercentage
{
    public DateTime Date { get; set; }
    public double Percentage { get; set; }
} 

public class PlayStats
{
    public int TotalPlays { get; set; }
    public int CompletedPlays { get; set; }
    public int NotCompletedPlays => TotalPlays - CompletedPlays;
    public List<SeekPositionStats> TopSeekedPositions { get; set; } = new();
    public double AveragePositionInSeconds { get; set; }
} 

public class SeekPositionStats
{
    public double PositionInSeconds { get; set; }
    public int Count { get; set; }
} 

public enum TimePeriod
{
    Day,
    Week,
    Month,
    Year
} 

public sealed class PlayCacheKey : IEquatable<PlayCacheKey>
{
    public IReadOnlyList<string> SongIds { get; }
    public IReadOnlyList<DateTime> Dates { get; }
    public IReadOnlyList<int> PlayTypes { get; }

    public PlayCacheKey(
        List<string>? songIds,
        List<DateTime>? dates,
        List<int>? playTypes)
    {
        SongIds = songIds != null && songIds.Any()
            ? songIds.Select(id => id.ToLowerInvariant()).Distinct().OrderBy(id => id).ToList().AsReadOnly()
            : new List<string>().AsReadOnly();

        Dates = dates != null && dates.Any()
            ? dates.Select(d => d.Date).Distinct().OrderBy(d => d).ToList().AsReadOnly()
            : new List<DateTime>().AsReadOnly();

        PlayTypes = playTypes != null && playTypes.Any()
            ? playTypes.Distinct().OrderBy(pt => pt).ToList().AsReadOnly()
            : new List<int>().AsReadOnly();
    }

    public bool Equals(PlayCacheKey? other)
    {
        if (other == null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return SongIds.SequenceEqual(other.SongIds) &&
               Dates.SequenceEqual(other.Dates) &&
               PlayTypes.SequenceEqual(other.PlayTypes);
    }

    public override bool Equals(object? obj) => Equals(obj as PlayCacheKey);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            foreach (string id in SongIds)
                hash = hash * 23 + id.GetHashCode();
            foreach (DateTime date in Dates)
                hash = hash * 23 + date.GetHashCode();
            foreach (int pt in PlayTypes)
                hash = hash * 23 + pt.GetHashCode();
            return hash;
        }
    }
} 


public class LruCache<TKey, TValue>
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cacheMap;
    private readonly LinkedList<CacheItem> _lruList;
    private readonly object _lock = new object();

    public LruCache(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be greater than zero.");

        _capacity = capacity;
        _cacheMap = new Dictionary<TKey, LinkedListNode<CacheItem>>();
        _lruList = new LinkedList<CacheItem>();
    }

    public bool TryGet(TKey key, out TValue value)
    {
        lock (_lock)
        {
            if (_cacheMap.TryGetValue(key, out LinkedListNode<CacheItem>? node))
            {
                _lruList.Remove(node);
                _lruList.AddFirst(node);
                value = node.Value.Value;
                return true;
            }
            value = default!;
            return false;
        }
    } 

    public void Add(TKey key, TValue value)
    {
        lock (_lock)
        {
            if (_cacheMap.TryGetValue(key, out LinkedListNode<CacheItem>? existingNode))
            {
                existingNode.Value.Value = value;
                _lruList.Remove(existingNode);
                _lruList.AddFirst(existingNode);
            }
            else
            {
                if (_cacheMap.Count >= _capacity)
                {
                    LinkedListNode<CacheItem>? lruNode = _lruList.Last;
                    if (lruNode != null)
                    {
                        _cacheMap.Remove(lruNode.Value.Key);
                        _lruList.RemoveLast();
                    }
                }

                CacheItem cacheItem = new CacheItem { Key = key, Value = value };
                LinkedListNode<CacheItem> node = new LinkedListNode<CacheItem>(cacheItem);
                _lruList.AddFirst(node);
                _cacheMap[key] = node;
            }
        }
    } 

    private class CacheItem
    {
        public TKey Key;
        public TValue Value;
    } 
}
