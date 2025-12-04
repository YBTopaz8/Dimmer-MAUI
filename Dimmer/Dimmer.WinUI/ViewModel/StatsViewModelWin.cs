using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dimmer.Utilities.Extensions;

using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

using SkiaSharp;

using LinearGradientPaint = LiveChartsCore.SkiaSharpView.Painting.LinearGradientPaint;

namespace Dimmer.WinUI.ViewModel;

public partial class StatsViewModelWin : StatisticsViewModel
{

    public ISeries[] DailyActivitySeries { get; set; } // 1. Area
    public ICartesianAxis[] DailyDateAxis { get; set; }

    public ISeries[] GenreRadarSeries { get; set; }    // 2. Polar
    public IPolarAxis[] GenrePolarAxis { get; set; }

    public ISeries[] HourlyClockSeries { get; set; }   // 3. Radial
    public ICartesianAxis[] HourlyAxis { get; set; }

    public ISeries[] TopArtistsSeries { get; set; }    // 4. Row
    public ICartesianAxis[] TopArtistsAxis { get; set; }

    public ISeries[] TopAlbumsSeries { get; set; }     // 5. Row
    public ICartesianAxis[] TopAlbumsAxis { get; set; }

    public ISeries[] DecadeSeries { get; set; }        // 6. Column
    public ICartesianAxis[] DecadeAxis { get; set; }

    public ISeries[] PlaySkipSeries { get; set; }      // 7. Pie

    public ISeries[] DeviceSeries { get; set; }        // 8. Donut

    public ISeries[] DiscoverySeries { get; set; }     // 9. Stacked

    public ISeries[] StreakSeries { get; set; }        // 10. Line

    public ISeries[] DayOfWeekSeries { get; set; }     // 11. Column
    public ICartesianAxis[] DayOfWeekAxis { get; set; }

    public ISeries[] ReplaySeries { get; set; }        // 12. Bar


    // --- Chart Properties ---
    public ISeries[] HourlyActivitySeries { get; set; }
    public ICartesianAxis[] HourlyXAxes { get; set; }


    // --- 9. Negative Engagement (Row - Top Skipped Artists) ---
    public ISeries[] SkippedArtistsSeries { get; set; }
    public ICartesianAxis[] SkippedArtistsAxis { get; set; }

    // --- 10. Nostalgia (Row - Rediscovered Songs) ---
    public ISeries[] RediscoveredSeries { get; set; }
    public ICartesianAxis[] RediscoveredAxis { get; set; }

    // --- 11. Fatigue (Row - Burnout Songs) ---
    public ISeries[] BurnoutSeries { get; set; }
    public ICartesianAxis[] BurnoutAxis { get; set; }

    // --- 12. Time Investment (Column - Songs by Duration) ---
    public ISeries[] TimeInvestmentSeries { get; set; }
    public ICartesianAxis[] TimeInvestmentAxis { get; set; }
    [ObservableProperty]
    public partial LibraryStatsBundle? Stats { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }
    public ISeries[] PlayTypeSeries { get; set; }

    public ISeries[] DropOffSeries { get; set; }
    public ICartesianAxis[] DropOffXAxes { get; set; }
    StatisticsService _statsService;
    BaseViewModelWin _baseVM;
    public StatsViewModelWin(StatisticsService statsService,  ILogger<StatisticsViewModel> logger
        ,BaseViewModelWin baseVM) : base(statsService, logger)
    {
        _statsService= statsService;
        HourlyXAxes = new ICartesianAxis[] { new Axis { Labels = Enumerable.Range(0, 24).Select(x => $"{x}:00").ToList() } };
        LoadLibraryStats(DateRangeFilter.AllTime);
    }

    public void LoadLibraryStats(DateRangeFilter filter)
    {
        IsLoading = true;
        // Run on background to keep UI responsive
        Task.Run(() =>
        {
            var bundle = _statsService.GetLibraryStatistics(filter);

            // Dispatch to UI Thread to build charts
            RxSchedulers.UI.Schedule(() =>
            {
                Stats = bundle;
                ConstructAllCharts();
                IsLoading = false;
            });
        });
    }
    private void ConstructAllCharts()
    {
        if (Stats == null) return;

        // --- 1. Daily Activity (Spline Area) ---
        // Assuming Stats.DailyListeningRoutineOHLC contains daily counts
        if (Stats.DailyListeningRoutineOHLC != null)
        {
            var values = Stats.DailyListeningRoutineOHLC.Select(x => x.Count ?? 0).ToArray();
            var labels = Stats.DailyListeningRoutineOHLC.Select(x => x.DateValue?.ToString("dd MMM") ?? "").ToArray();

            DailyActivitySeries = new ISeries[]
            {
                new LineSeries<int>
                {
                    Values = values,
                    Fill = new LinearGradientPaint(new[]{ SKColors.MediumPurple.WithAlpha(150), SKColors.MediumPurple.WithAlpha(20) }, new SKPoint(0.5f, 0), new SKPoint(0.5f, 1)),
                    Stroke = new SolidColorPaint(SKColors.MediumPurple) { StrokeThickness = 3 },
                    GeometrySize = 0,
                    LineSmoothness = 1
                }
            };
            DailyDateAxis = new ICartesianAxis[] { new Axis { Labels = labels, LabelsRotation = 15 } };
            OnPropertyChanged(nameof(DailyActivitySeries));
            OnPropertyChanged(nameof(DailyDateAxis));
        }

        // --- 2. Genres (Radar) ---
        // Assuming GetTopGenres() returns DimmerStats with StatTitle=Name, Count=Plays
        var genres = Stats.GenrePopularityOverTime? // or create a specific GetTopGenres in service
                     .Take(5).ToList() ?? new List<DimmerStats>();

        GenreRadarSeries = new ISeries[]
        {
            new PolarLineSeries<double>
            {
                Values = genres.Select(g => (double)(g.Count ?? 0)).ToArray(),
                Fill = new SolidColorPaint(SKColors.Teal.WithAlpha(50)),
                IsClosed = true
            }
        };
        GenrePolarAxis = new PolarAxis[] { new PolarAxis { Labels = genres.Select(g => g.StatTitle).ToArray() } };
        OnPropertyChanged(nameof(GenreRadarSeries));
        OnPropertyChanged(nameof(GenrePolarAxis));

        // --- 3. Hourly Clock (Column) ---
        // Assuming you add GetHourlyDistribution() to LibraryStatsBundle
        var hourlyData = new int[24]; // Populate this from your stats service
        // Mocking for example (Replace with Stats.HourlyStats):
        for (int i = 0; i < 24; i++) hourlyData[i] = new Random().Next(10, 100);

        HourlyClockSeries = new ISeries[]
        {
            new ColumnSeries<int>
            {
                Values = hourlyData,
                Stroke = null,
                Fill = new SolidColorPaint(SKColors.CornflowerBlue),
                Rx = 50, Ry = 50 // Rounded bars
            }
        };
        HourlyAxis = new ICartesianAxis[] { new Axis { Labels = Enumerable.Range(0, 24).Select(h => $"{h}h").ToList() } };
        OnPropertyChanged(nameof(HourlyClockSeries));
        OnPropertyChanged(nameof(HourlyAxis));

        // --- 4. Top Artists (Row Series) ---
        var artists = Stats.TopArtistsByPlays?.Take(5).Reverse().ToList() ?? new List<DimmerStats>();
        TopArtistsSeries = new ISeries[]
        {
            new RowSeries<int>
            {
                Values = artists.Select(a => a.Count ?? 0).ToArray(),
                Fill = new SolidColorPaint(SKColors.PaleVioletRed),
                DataLabelsSize = 14,
                DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End
            }
        };
        TopArtistsAxis = new ICartesianAxis[] { new Axis { Labels = artists.Select(a => a.ArtistName ?? "Unknown").ToArray() } };
        OnPropertyChanged(nameof(TopArtistsSeries));
        OnPropertyChanged(nameof(TopArtistsAxis));

        // --- 5. Top Albums (Row Series) ---
        var albums = Stats.TopAlbumsByPlays?.Take(5).Reverse().ToList() ?? new List<DimmerStats>();
        TopAlbumsSeries = new ISeries[]
        {
            new RowSeries<int>
            {
                Values = albums.Select(a => a.Count ?? 0).ToArray(),
                Fill = new SolidColorPaint(SKColors.SeaGreen)
            }
        };
        TopAlbumsAxis = new ICartesianAxis[] { new Axis { Labels = albums.Select(a => a.AlbumName ?? "Unknown").ToArray() } };
        OnPropertyChanged(nameof(TopAlbumsSeries));
        OnPropertyChanged(nameof(TopAlbumsAxis));

        // --- 6. Play vs Skip (Pie) ---
        // Calculate totals from collection summary
        double totalPlays = Stats.CollectionSummary?.TotalPlayCount ?? 100;
        double totalSkips = Stats.TopSongsBySkips?.Sum(s => s.Count ?? 0) ?? 0; // Approx

        PlaySkipSeries = new ISeries[]
        {
            new PieSeries<double> { Values = new[]{ totalPlays }, Name = "Completed", Fill = new SolidColorPaint(SKColors.DodgerBlue) },
            new PieSeries<double> { Values = new[]{ totalSkips }, Name = "Skipped", Fill = new SolidColorPaint(SKColors.Crimson) }
        };
        OnPropertyChanged(nameof(PlaySkipSeries));

        // --- 7. Decades (Column) ---
        // Access your GetMusicByDecade() result here. 
        // For now, mocking structure:
        DecadeSeries = new ISeries[]
        {
            new ColumnSeries<int> { Values = new[] { 50, 120, 300, 150 }, Name = "Plays" }
        };
        DecadeAxis = new ICartesianAxis[] { new Axis { Labels = new[] { "80s", "90s", "00s", "10s" } } };
        OnPropertyChanged(nameof(DecadeSeries));
        OnPropertyChanged(nameof(DecadeAxis));

        var daysData = Stats.OverallListeningByDayOfWeek ?? new List<DimmerStats>();

        // Ensure we order them correctly (Sunday -> Saturday) if they aren't already
        // Assuming StatTitle contains "Sunday", "Monday", etc.
        var sorter = new Dictionary<string, int> {
            {"Sunday",0}, {"Monday",1}, {"Tuesday",2}, {"Wednesday",3}, {"Thursday",4}, {"Friday",5}, {"Saturday",6}
        };

        var sortedDays = daysData
            .OrderBy(d => sorter.ContainsKey(d.StatTitle ?? "") ? sorter[d.StatTitle] : 99)
            .ToList();

        DayOfWeekSeries = new ISeries[]
        {
            new ColumnSeries<int>
            {
                Values = sortedDays.Select(d => d.Count ?? 0).ToArray(),
                Name = "Plays",
                Fill = new LinearGradientPaint(
                    new SKColor[] { SKColors.DeepSkyBlue, SKColors.DeepSkyBlue.WithAlpha(100) },
                    new SKPoint(0.5f, 0), new SKPoint(0.5f, 1)),
                Rx = 4, Ry = 4
            }
        };
        DayOfWeekAxis = new ICartesianAxis[]
        {
            new Axis
            {
                Labels = sortedDays.Select(d => d.StatTitle?.Substring(0, 3) ?? "???").ToArray(), // "Mon", "Tue"
                LabelsPaint = new SolidColorPaint(SKColors.Gray)
            }
        };
        OnPropertyChanged(nameof(DayOfWeekSeries));
        OnPropertyChanged(nameof(DayOfWeekAxis));


        // --- 9. Negative Engagement (Top Skipped Artists) ---
        // Data: Stats.TopArtistsBySkips
        var skippedArtists = Stats.TopArtistsBySkips?.Take(5).Reverse().ToList() ?? new List<DimmerStats>();

        SkippedArtistsSeries = new ISeries[]
        {
            new RowSeries<int>
            {
                Values = skippedArtists.Select(a => a.Count ?? 0).ToArray(),
                Name = "Skips",
                Fill = new SolidColorPaint(SKColors.IndianRed),
                DataLabelsSize = 12,
                DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End
            }
        };
        SkippedArtistsAxis = new ICartesianAxis[]
        {
            new Axis { Labels = skippedArtists.Select(a => a.ArtistName ?? "Unknown").ToArray() }
        };
        OnPropertyChanged(nameof(SkippedArtistsSeries));
        OnPropertyChanged(nameof(SkippedArtistsAxis));


        // --- 10. Nostalgia (Rediscovered Songs) ---
        // Data: Stats.TopRediscoveredSongs (Songs you stopped playing but started again)
        var rediscovered = Stats.TopRediscoveredSongs?.Take(5).Reverse().ToList() ?? new List<DimmerStats>();

        RediscoveredSeries = new ISeries[]
        {
            new RowSeries<int>
            {
                Values = rediscovered.Select(s => s.Count ?? 0).ToArray(), // Play count since rediscovery
                Name = "Plays",
                Fill = new SolidColorPaint(SKColors.Goldenrod),
                Stroke = null
            }
        };
        RediscoveredAxis = new ICartesianAxis[]
        {
            new Axis { Labels = rediscovered.Select(s => s.Song?.Title ?? "Unknown").ToArray() }
        };
        OnPropertyChanged(nameof(RediscoveredSeries));
        OnPropertyChanged(nameof(RediscoveredAxis));


        // --- 11. Fatigue (Burnout Songs) ---
        // Data: Stats.TopBurnoutSongs (Songs with high skip rate recently)
        var burnout = Stats.TopBurnoutSongs?.Take(5).Reverse().ToList() ?? new List<DimmerStats>();

        BurnoutSeries = new ISeries[]
        {
            new RowSeries<int>
            {
                Values = burnout.Select(s => s.Count ?? 0).ToArray(), // Skip count or 'Burnout Score'
                Name = "Skips",
                Fill = new SolidColorPaint(SKColors.DarkSlateGray),
                DataLabelsPaint = new SolidColorPaint(SKColors.White)
            }
        };
        BurnoutAxis = new ICartesianAxis[]
        {
            new Axis { Labels = burnout.Select(s => s.Song?.Title ?? "Unknown").ToArray() }
        };
        OnPropertyChanged(nameof(BurnoutSeries));
        OnPropertyChanged(nameof(BurnoutAxis));


        // --- 12. Time Investment (Top Songs by Time) ---
        // Data: Stats.TopSongsByTime (Duration * Plays)
        // This is different from play count; it favors long songs (Prog Rock, DJ Sets)
        var timeSongs = Stats.TopSongsByTime?.Take(5).ToList() ?? new List<DimmerStats>();

        TimeInvestmentSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                // Assuming 'Value' holds hours or minutes, or we calculate from TimeSpanValue if available
                // Fallback: use Count if Value is null, though typically this stat should populate Value.
                Values = timeSongs.Select(s => s.Value ?? (double)(s.Count ?? 0)).ToArray(),
                Name = "Hours",
                Fill = new LinearGradientPaint(
                    new SKColor[] { SKColors.MediumSpringGreen, SKColors.Teal },
                    new SKPoint(0.5f, 0), new SKPoint(0.5f, 1))
            }
        };
        TimeInvestmentAxis = new ICartesianAxis[]
        {
            new Axis
            {
                Labels = timeSongs.Select(s => s.Song?.Title ?? s.StatTitle ?? "?").ToArray(),
                LabelsRotation = 15
            }
        };
        OnPropertyChanged(nameof(TimeInvestmentSeries));
        OnPropertyChanged(nameof(TimeInvestmentAxis));
    }
}
