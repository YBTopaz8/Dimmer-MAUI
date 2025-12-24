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
    public partial bool IsLoading { get; set; }
    public ISeries[] PlayTypeSeries { get; set; }

    public ISeries[] DropOffSeries { get; set; }
    public ICartesianAxis[] DropOffXAxes { get; set; }
    StatisticsService _statsService;

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
            RxSchedulers.UI.ScheduleToUI(() =>
            {
                Stats = bundle;
                //ConstructAllCharts();
                IsLoading = false;
            });
        });
    }
}
