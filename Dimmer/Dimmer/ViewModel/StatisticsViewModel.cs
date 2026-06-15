// Ensure your models/services namespaces are here (e.g., Dimmer.Data.ModelView, etc.)

namespace Dimmer.ViewModel;

#region --- Chart Data Models (POCOs for Syncfusion/DevExpress) ---
public class StackedStringChartPoint
{
    public string Label { get; set; } = string.Empty;
    public double Series1Value { get; set; } // e.g. Plays
    public double? Series2Value { get; set; } // e.g. Skips
    public double Series3Value { get; set; } // e.g. Pauses
}

public class ScatterChartPoint
{
    public string Label { get; set; } = string.Empty;
    public double XValue { get; set; }
    public double YValue { get; set; }
}
public class StringChartPoint
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
}

public class NumericChartPoint
{
    public double XValue { get; set; }
    public double YValue { get; set; }
}

public class DateChartPoint
{
    public DateTime Date { get; set; }
    public double Value { get; set; }
}

public class StackedDateChartPoint
{
    public DateTime Date { get; set; }
    public double Series1Value { get; set; } // e.g. Completed
    public double Series2Value { get; set; } // e.g. Skipped
}

public class RangeChartPoint
{
    public string Label { get; set; } = string.Empty;
    public double StartValue { get; set; }
    public double EndValue { get; set; }
}

#endregion

public partial class StatisticsViewModel : ObservableObject
{
    private readonly StatisticsService _statsService;
    private readonly ILogger<StatisticsViewModel> _logger;

    public StatisticsViewModel(StatisticsService statsService, ILogger<StatisticsViewModel> logger)
    {
        _statsService = statsService;
        _logger = logger;

        AvailableFilters = new ObservableCollection<DateRangeFilter>(Enum.GetValues<DateRangeFilter>());
        SelectedFilter = DateRangeFilter.Last30Days;
    }

    #region --- UI State & Bundle Properties ---

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsBusy { get; set; }
    #region --- ALBUM Chart Data Collections ---

    // 1. ALBUM DROP-OFF CURVE (Retention)
    // Chart: Syncfusion LineSeries / SplineSeries
    // XBindingPath="Label" YBindingPath="Value"
    [ObservableProperty] public partial ObservableCollection<StringChartPoint>? AlbumDropOffData { get; set; }

    // 2. PLAYS PER TRACK NUMBER
    // Chart: Syncfusion ColumnSeries
    // XBindingPath="Label" YBindingPath="Value"
    [ObservableProperty] public partial ObservableCollection<StringChartPoint>? AlbumPlaysPerTrackData { get; set; }

    // 3. SKIPS PER TRACK NUMBER
    // Chart: Syncfusion ColumnSeries (Maybe paint it Red)
    // XBindingPath="Label" YBindingPath="Value"
    [ObservableProperty] public partial ObservableCollection<StringChartPoint>? AlbumSkipsPerTrackData { get; set; }

    // 4. LYRICAL DENSITY PER TRACK
    // Chart: Syncfusion BarSeries (Horizontal bars)
    // XBindingPath="Label" YBindingPath="Value"
    [ObservableProperty] public partial ObservableCollection<StringChartPoint>? AlbumLyricalDensityData { get; set; }

    // 5. VOCAL VS INSTRUMENTAL
    // Chart: Syncfusion PieSeries / DoughnutSeries
    // XBindingPath="Label" YBindingPath="Value"
    [ObservableProperty] public partial ObservableCollection<StringChartPoint>? AlbumVocalVsInstrumentalData { get; set; }

    // 6. EVENT BREAKDOWN PER TRACK
    // Chart: Syncfusion StackingColumnSeries (3 Series total)
    // XBindingPath="Label" YBindingPath="Series1Value" (Plays), "Series2Value" (Skips), "Series3Value" (Pauses)
    [ObservableProperty] public partial ObservableCollection<StackedStringChartPoint>? AlbumEventBreakdownData { get; set; }

    #endregion

    #region --- ARTIST Chart Data Collections ---

    // 1. ERA / DECADE DISTRIBUTION
    // Chart: Syncfusion ColumnSeries (Histogram style)
    // XBindingPath="Label" YBindingPath="Value"
    [ObservableProperty] public partial ObservableCollection<StringChartPoint>? ArtistDecadeData { get; set; }

    // 2. PLAYS PER MONTH (VELOCITY)
    // Chart: Syncfusion SplineAreaSeries
    // XBindingPath="Label" YBindingPath="Value"
    [ObservableProperty] public partial ObservableCollection<StringChartPoint>? ArtistPlaysPerMonthData { get; set; }

    // 3. HOURLY LISTENING PREFERENCE FOR ARTIST
    // Chart: Syncfusion PolarChart / RadarSeries OR ColumnSeries
    // XBindingPath="Label" YBindingPath="Value"
    [ObservableProperty] public partial ObservableCollection<StringChartPoint>? ArtistHourlyData { get; set; }

    // 4. TOP 10 OBSESSION RANKED SONGS
    // Chart: Syncfusion BarSeries (Horizontal)
    // XBindingPath="Label" YBindingPath="Value"
    [ObservableProperty] public partial ObservableCollection<StringChartPoint>? ArtistObsessionData { get; set; }

    // 5. COLLABORATOR NETWORK
    // Chart: Syncfusion PieSeries / DoughnutSeries
    // XBindingPath="Label" YBindingPath="Value"
    [ObservableProperty] public partial ObservableCollection<StringChartPoint>? ArtistCollaboratorData { get; set; }

    // 6. GENRE BLENDING
    // Chart: Syncfusion PieSeries / DoughnutSeries
    // XBindingPath="Label" YBindingPath="Value"
    [ObservableProperty] public partial ObservableCollection<StringChartPoint>? ArtistGenreBlendingData { get; set; }

    // 7. VIBE / BPM VS ENGAGEMENT
    // Chart: Syncfusion ScatterSeries (Enable Tooltips to see the Song Label!)
    // XBindingPath="XValue" (BPM) YBindingPath="YValue" (Engagement Score)
    [ObservableProperty] public partial ObservableCollection<ScatterChartPoint>? ArtistBpmScatterData { get; set; }

    #endregion
    public bool IsIdle => !IsBusy;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial DateRangeFilter SelectedFilter { get; set; }

    public ObservableCollection<DateRangeFilter> AvailableFilters { get; }

    [ObservableProperty] public partial LibraryStatsBundle? LibraryStats { get; set; }
    [ObservableProperty] public partial SongStatsBundle? SongStats { get; set; }
    [ObservableProperty] public partial ArtistStatsBundle? ArtistStats { get; set; }
    [ObservableProperty] public partial AlbumStatsBundle? AlbumStats { get; set; }

    #endregion

    #region --- Chart Data Collections (For XAML Binding) ---

    // SINGLE SONG CHARTS
    [ObservableProperty] public partial ObservableCollection<StringChartPoint>? SongActionRadarData { get; set; }
    [ObservableProperty] public partial ObservableCollection<StringChartPoint>? SongFunnelData { get; set; }
    [ObservableProperty] public partial ObservableCollection<NumericChartPoint>? SongSkipHotspotsData { get; set; }
    [ObservableProperty] public partial ObservableCollection<NumericChartPoint>? SongReplayHotspotsData { get; set; }
    [ObservableProperty] public partial ObservableCollection<StringChartPoint>? SongHourlyData { get; set; }
    [ObservableProperty] public partial ObservableCollection<StringChartPoint>? SongDayOfWeekData { get; set; }
    [ObservableProperty] public partial ObservableCollection<DateChartPoint>? SongCumulativePlaysData { get; set; }
    [ObservableProperty] public partial ObservableCollection<StackedDateChartPoint>? SongCompletedVsSkippedData { get; set; }
    [ObservableProperty] public partial ObservableCollection<StringChartPoint>? SongTimeSpentGaugeData { get; set; }
    [ObservableProperty] public partial ObservableCollection<DateChartPoint>? SongRatingTrendData { get; set; }
    [ObservableProperty] public partial ObservableCollection<RangeChartPoint>? SongLoopSegmentData { get; set; }
    [ObservableProperty] public partial ObservableCollection<DateChartPoint>? SongPlayVelocityData { get; set; }
    [ObservableProperty] public partial ObservableCollection<StringChartPoint>? SongEngagementBreakdownData { get; set; }
    [ObservableProperty] public partial ObservableCollection<DateChartPoint>? SongBurnoutCurveData { get; set; }
    [ObservableProperty] public partial ObservableCollection<DateChartPoint>? SongStreakTimelineData { get; set; }



    #endregion

    #region --- Data Builders ---

    SongModelView? currentSong;
    private void BuildSingleSongCharts(SongModelView song)
    {
        if (currentSong is not null && currentSong.TitleDurationKey == song.TitleDurationKey && SongActionRadarData is not null)
        {
            return;
        }
        currentSong = song;
        var events = song.PlayEvents.Where(e => e.EventDate.HasValue).OrderBy(e => e.EventDate).ToList();

        SongActionRadarData = new ObservableCollection<StringChartPoint>
        {
            new() { Label = "Pauses", Value = song.PauseCount },
            new() { Label = "Resumes", Value = song.ResumeCount },
            new() { Label = "Seeks", Value = song.SeekCount },
            new() { Label = "Skips", Value = song.SkipCount },
            new() { Label = "Repeats", Value = song.RepeatCount }
        };

        SongFunnelData = new ObservableCollection<StringChartPoint>
        {
            new() { Label = "Total Plays", Value = song.PlayCount },
            new() { Label = "Completed", Value = song.PlayCompletedCount },
            new() { Label = "Favorited", Value = song.NumberOfTimesFaved },
            new() { Label = "Playlists", Value = song.PlaylistsHavingSong?.Count ?? 0 }
        };

        SongSkipHotspotsData = new ObservableCollection<NumericChartPoint>(
            events.Where(e => e.PlayType == (int)PlayType.Skipped)
                  .Select((e, index) => new NumericChartPoint { XValue = e.PositionInSeconds, YValue = index })
        );

        var seeks = events.Where(e => e.PlayType == (int)PlayType.Seeked || e.PlayType == (int)PlayType.Restarted);
        SongReplayHotspotsData = new ObservableCollection<NumericChartPoint>(
            seeks.GroupBy(e => Math.Floor(e.PositionInSeconds / 5) * 5)
                 .Select(g => new NumericChartPoint { XValue = g.Key, YValue = g.Count() })
                 .OrderBy(p => p.XValue)
        );

        var hourlyPlays = new double[24];
        foreach (var e in events) hourlyPlays[e.EventDate.Value.ToLocalTime().Hour]++;

        SongHourlyData = new ObservableCollection<StringChartPoint>(
            Enumerable.Range(0, 24).Select(h => new StringChartPoint { Label = $"{h}:00", Value = hourlyPlays[h] })
        );

        var dayPlays = new double[7];
        foreach (var e in events) dayPlays[(int)e.EventDate.Value.DayOfWeek]++;
        var dayLabels = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        SongDayOfWeekData = new ObservableCollection<StringChartPoint>(
            Enumerable.Range(0, 7).Select(d => new StringChartPoint { Label = dayLabels[d], Value = dayPlays[d] })
        );

        var cumulativePoints = new List<DateChartPoint>();
        double runningTotal = 0;
        foreach (var e in events.Where(x => x.PlayType == (int)PlayType.Completed))
        {
            runningTotal++;
            cumulativePoints.Add(new DateChartPoint { Date = e.EventDate.Value.DateTime, Value = runningTotal });
        }
        SongCumulativePlaysData = new ObservableCollection<DateChartPoint>(cumulativePoints);

        var monthlyGroups = events.GroupBy(e => new DateTime(e.EventDate.Value.Year, e.EventDate.Value.Month, 1)).OrderBy(g => g.Key);
        SongCompletedVsSkippedData = new ObservableCollection<StackedDateChartPoint>(
            monthlyGroups.Select(g => new StackedDateChartPoint
            {
                Date = g.Key,
                Series1Value = g.Count(x => x.PlayType == (int)PlayType.Completed),
                Series2Value = g.Count(x => x.PlayType == (int)PlayType.Skipped)
            })
        );

        double totalHours = TimeSpan.FromSeconds(song.TotalPlayDurationSeconds).TotalHours;
        SongTimeSpentGaugeData = new ObservableCollection<StringChartPoint>
        {
            new() { Label = "Hours Listened", Value = totalHours }
        };

        if (song.UserNoteAggregatedCol != null && song.UserNoteAggregatedCol.Any())
        {
            SongRatingTrendData = new ObservableCollection<DateChartPoint>(
                song.UserNoteAggregatedCol.OrderBy(n => n.CreatedAt)
                    .Select(n => new DateChartPoint { Date = n.CreatedAt.DateTime, Value = n.UserRating })
            );
        }

        if (song.SegmentEndBehavior == SegmentEndBehavior.LoopSegment && song.SegmentStartTime.HasValue)
        {
            SongLoopSegmentData = new ObservableCollection<RangeChartPoint>
            {
                new() { Label = "Looped Portion", StartValue = song.SegmentStartTime.Value, EndValue = song.SegmentEndTime ?? song.DurationInSeconds }
            };
        }

        var dailyPlays = events.GroupBy(e => e.EventDate.Value.Date)
                               .Select(g => new DateChartPoint { Date = g.Key, Value = g.Count() })
                               .OrderBy(p => p.Date).ToList();
        SongPlayVelocityData = new ObservableCollection<DateChartPoint>(dailyPlays);

     

        var burnoutPoints = new List<DateChartPoint>();
        for (int i = 10; i < events.Count; i++)
        {
            var window = events.Skip(i - 10).Take(10);
            var skipCount = window.Count(e => e.PlayType == (int)PlayType.Skipped);
            burnoutPoints.Add(new DateChartPoint { Date = events[i].EventDate.Value.DateTime, Value = skipCount });
        }
        SongBurnoutCurveData = new ObservableCollection<DateChartPoint>(burnoutPoints);

        var streaks = new List<DateChartPoint>();
        int currentStreak = 1;
        for (int i = 1; i < dailyPlays.Count; i++)
        {
            var prevDate = dailyPlays[i - 1].Date;
            var currDate = dailyPlays[i].Date;
            if ((currDate - prevDate).TotalDays == 1) currentStreak++;
            else
            {
                if (currentStreak > 1) streaks.Add(new DateChartPoint { Date = prevDate, Value = currentStreak });
                currentStreak = 1;
            }
        }
        SongStreakTimelineData = new ObservableCollection<DateChartPoint>(streaks);
    }


    [ObservableProperty]
    public partial SongQuickStatsBundle? QuickStats { get; set; }


    [RelayCommand]
    public async Task LoadSongQuickStatsAsync(SongModelView? song)
    {
        if (song is null || IsBusy) return;
        if (currentSong is not null && currentSong.TitleDurationKey == song.TitleDurationKey && QuickStats is not null)
        {
            return;
        }
            currentSong = song;
        IsBusy = true;
        try
        {
            // We don't call ClearAllStats() here because this is just a popup overlay!
            // We run it on a background thread so the popup UI doesn't stutter while opening
            QuickStats = await Task.Run(() => _statsService.GetSongQuickSummary(song.Id));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load quick stats for {SongTitle}", song.Title);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void BuildAlbumAndArtistCharts()
    {
        // ==========================================
        // ALBUM CHARTS MAPPING
        // ==========================================
        if (AlbumStats is not null)
        {
            if (AlbumStats.AlbumDropOffCurve != null)
                AlbumDropOffData = new ObservableCollection<StringChartPoint>(
                    AlbumStats.AlbumDropOffCurve.Select(x => new StringChartPoint { Label = x.Label, Value = x.Value }));

            if (AlbumStats.PlottableData?.PlaysPerTrackNumber != null)
                AlbumPlaysPerTrackData = new ObservableCollection<StringChartPoint>(
                    AlbumStats.PlottableData.PlaysPerTrackNumber.Select(x => new StringChartPoint { Label = x.Label, Value = x.Value }));

            if (AlbumStats.PlottableData?.SkipsPerTrackNumber != null)
                AlbumSkipsPerTrackData = new ObservableCollection<StringChartPoint>(
                    AlbumStats.PlottableData.SkipsPerTrackNumber.Select(x => new StringChartPoint { Label = x.Label, Value = x.Value }));

            if (AlbumStats.LyricalDensityPerTrack != null)
                AlbumLyricalDensityData = new ObservableCollection<StringChartPoint>(
                    AlbumStats.LyricalDensityPerTrack.Select(x => new StringChartPoint { Label = x.Label, Value = x.Value }));

            if (AlbumStats.InstrumentalVsVocalPlays != null)
                AlbumVocalVsInstrumentalData = new ObservableCollection<StringChartPoint>(
                    AlbumStats.InstrumentalVsVocalPlays.Select(x => new StringChartPoint { Label = x.Label, Value = x.Value }));

            if (AlbumStats.TrackEventBreakdown != null)
                AlbumEventBreakdownData = new ObservableCollection<StackedStringChartPoint>(
                    AlbumStats.TrackEventBreakdown.Select(x => new StackedStringChartPoint
                    {
                        Label = x.ComparisonLabel ?? "Unknown",
                        Series1Value = x.IntValue,         // Plays
                        Series2Value = x.DoubleValue,      // Skips
                        Series3Value = x.SecondaryValue    // Pauses
                    }));
        }

        // ==========================================
        // ARTIST CHARTS MAPPING
        // ==========================================
        if (ArtistStats is not null)
        {
            if (ArtistStats.DecadeDistribution != null)
                ArtistDecadeData = new ObservableCollection<StringChartPoint>(
                    ArtistStats.DecadeDistribution.Select(x => new StringChartPoint { Label = x.Label, Value = x.Value }));

            if (ArtistStats.PlottableData?.PlaysPerMonth != null)
                ArtistPlaysPerMonthData = new ObservableCollection<StringChartPoint>(
                    ArtistStats.PlottableData.PlaysPerMonth.Select(x => new StringChartPoint { Label = x.Label, Value = x.Value }));

            if (ArtistStats.PlottableData?.PlaysPerHourOfDay != null)
                ArtistHourlyData = new ObservableCollection<StringChartPoint>(
                    ArtistStats.PlottableData.PlaysPerHourOfDay.Select(x => new StringChartPoint { Label = x.Label, Value = x.Value }));

            if (ArtistStats.ObsessionRankedSongs != null)
                ArtistObsessionData = new ObservableCollection<StringChartPoint>(
                    ArtistStats.ObsessionRankedSongs.Select(x => new StringChartPoint { Label = x.Label, Value = x.Value }));

            if (ArtistStats.CollaboratorNetwork != null)
                ArtistCollaboratorData = new ObservableCollection<StringChartPoint>(
                    ArtistStats.CollaboratorNetwork.Select(x => new StringChartPoint { Label = x.Label, Value = x.Value }));

            if (ArtistStats.GenreBlending != null)
                ArtistGenreBlendingData = new ObservableCollection<StringChartPoint>(
                    ArtistStats.GenreBlending.Select(x => new StringChartPoint { Label = x.Label, Value = x.Value }));

          

        }
    }

    #endregion

    #region --- Commands ---

    [RelayCommand]
    private void LoadLibraryStats()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Calculating library overview...";
        try
        {
            ClearAllStats();
            LibraryStats = _statsService.GetLibraryStatistics(SelectedFilter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load library statistics.");
            StatusMessage = "Error loading library stats.";
        }
        finally
        {
            IsBusy = false;
            StatusMessage = string.Empty;
        }
    }

    public async Task LoadSongStatsAsync(SongModelView? song)
    {
        if (song is null || IsBusy) return;
        IsBusy = true;
        StatusMessage = $"Loading stats for {song.Title}...";
        try
        {
            ClearAllStats();
            SongStats = _statsService.GetSongStatistics(song.Id, SelectedFilter);
            BuildSingleSongCharts(song);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load song statistics for {SongId}", song.Id);
            StatusMessage = "Error loading song stats.";
        }
        finally
        {
            IsBusy = false;
            StatusMessage = string.Empty;
        }
    }

    [RelayCommand]
    public async Task LoadArtistStatsAsync(ArtistModelView? artist)
    {
        if (artist is null || IsBusy) return;
        IsBusy = true;
        StatusMessage = $"Loading stats for {artist.Name}...";
        try
        {
            ClearAllStats();
            ArtistStats = _statsService.GetArtistStatisticsAsync(artist.Id, SelectedFilter);
            //BuildAlbumAndArtistCharts();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load artist stats for {ArtistId}", artist.Id);
            StatusMessage = "Error loading artist stats.";
        }
        finally
        {
            IsBusy = false;
            StatusMessage = string.Empty;
        }
    }

    [RelayCommand]
    public async Task LoadAlbumStatsAsync(AlbumModelView? album)
    {
        if (album is null || IsBusy) return;
        IsBusy = true;
        StatusMessage = $"Loading stats for {album.Name}...";
        try
        {
            ClearAllStats();
            //AlbumStats = _statsService.GetAlbumStatisticsAsync(album.Id, SelectedFilter);
            //BuildAlbumAndArtistCharts();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load album stats for {AlbumId}", album.Id);
            StatusMessage = "Error loading album stats.";
        }
        finally
        {
            IsBusy = false;
            StatusMessage = string.Empty;
        }
    }

    #endregion

    async partial void OnSelectedFilterChanged(DateRangeFilter value)
    {
        if (LibraryStats is not null)
            LoadLibraryStats();
        else if (SongStats is not null)
            await LoadSongStatsAsync(new SongModelView { Id = new ObjectId(SongStats.Summary.SongTitle) }); // Assumes you parse original ID back
        else if (ArtistStats is not null)
            await LoadArtistStatsAsync(new ArtistModelView { Id = ArtistStats.Summary.ArtistId });
        //else if (AlbumStats is not null)
        //    await LoadAlbumStatsAsync(new AlbumModelView { Id = AlbumStats.Summary.AlbumId });
    }

    private void ClearAllStats()
    {
        LibraryStats = null;
        SongStats = null;
        ArtistStats = null;
        AlbumStats = null;

        // Clear charting collections so old charts don't stay on screen when changing pages
        SongActionRadarData = null;
        SongFunnelData = null;
        SongSkipHotspotsData = null;
        SongReplayHotspotsData = null;
        SongHourlyData = null;
        SongDayOfWeekData = null;
        SongCumulativePlaysData = null;
        SongCompletedVsSkippedData = null;
        SongTimeSpentGaugeData = null;
        SongRatingTrendData = null;
        SongLoopSegmentData = null;
        SongPlayVelocityData = null;
        SongEngagementBreakdownData = null;
        SongBurnoutCurveData = null;
        SongStreakTimelineData = null;
        AlbumDropOffData = null;
        AlbumPlaysPerTrackData = null;
        AlbumSkipsPerTrackData = null;
        AlbumLyricalDensityData = null;
        AlbumVocalVsInstrumentalData = null;
        AlbumEventBreakdownData = null;

        ArtistDecadeData = null;
        ArtistPlaysPerMonthData = null;
        ArtistHourlyData = null;
        ArtistObsessionData = null;
        ArtistCollaboratorData = null;
        ArtistGenreBlendingData = null;
        ArtistBpmScatterData = null;
    }
}