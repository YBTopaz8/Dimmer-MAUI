using Dimmer.Charts;
using Dimmer.Charts.Services;

namespace Dimmer.ViewModel.StatsVMs;

public partial class AlbumStatsViewModel : ObservableObject, IDisposable
{
    private readonly AlbumStatsService _statsService;
    private readonly CompositeDisposable _disposables = new();

    // --- 6 COMMON PROPERTIES ---
    [ObservableProperty] public partial TextStat TextTotalTime { get; set; }
    [ObservableProperty] public partial TextStat TextLifespan { get; set; }
    [ObservableProperty] public partial IReadOnlyList<ChartPoint> ListPlaySkipRatio { get; set; }
    [ObservableProperty] public partial IReadOnlyList<ChartPoint> ListTimeOfDayHeatmap { get; set; }
    [ObservableProperty] public partial IReadOnlyList<ChartPoint> ListDayOfWeekHeatmap { get; set; }
    [ObservableProperty] public partial IReadOnlyList<TrendStat> ListMonthlyTrend { get; set; }

    // --- 10 SPECIFIC PROPERTIES ---
    [ObservableProperty] public partial TextStat TextGoldenRatioTrack { get; set; }
    [ObservableProperty] public partial TextStat TextAlbumCompletionRate { get; set; }
    [ObservableProperty] public partial TextStat TextFrontToBackIndex { get; set; }
    [ObservableProperty] public partial TextStat TextSinglesVsFiller { get; set; }
    [ObservableProperty] public partial TextStat TextListenThroughRate { get; set; }
    [ObservableProperty] public partial TextStat TextSkippedTrackId { get; set; }
    [ObservableProperty] public partial TextStat TextPeakDiscoveryDay { get; set; }
    [ObservableProperty] public partial IReadOnlyList<ChartPoint> ListPlaysPerTrack { get; set; }
    [ObservableProperty] public partial IReadOnlyList<ChartPoint> ListBpmFlow { get; set; }
    [ObservableProperty] public partial IReadOnlyList<InsightStat> ListInsights { get; set; }

    public AlbumStatsViewModel(AlbumStatsService statsService)
    {
        _statsService = statsService;

        // Common
        _statsService.TotalTime.Subscribe(v => TextTotalTime = v).DisposeWith(_disposables);
        _statsService.Lifespan.Subscribe(v => TextLifespan = v).DisposeWith(_disposables);
        _statsService.PlaySkipRatio.Subscribe(v => ListPlaySkipRatio = v).DisposeWith(_disposables);
        _statsService.TimeOfDayHeatmap.Subscribe(v => ListTimeOfDayHeatmap = v).DisposeWith(_disposables);
        _statsService.DayOfWeekHeatmap.Subscribe(v => ListDayOfWeekHeatmap = v).DisposeWith(_disposables);
        _statsService.MonthlyTrend.Subscribe(v => ListMonthlyTrend = v).DisposeWith(_disposables);

        // Specific
        _statsService.GoldenRatioTrack.Subscribe(v => TextGoldenRatioTrack = v).DisposeWith(_disposables);
        _statsService.AlbumCompletionRate.Subscribe(v => TextAlbumCompletionRate = v).DisposeWith(_disposables);
        _statsService.FrontToBackIndex.Subscribe(v => TextFrontToBackIndex = v).DisposeWith(_disposables);
        _statsService.SinglesVsFiller.Subscribe(v => TextSinglesVsFiller = v).DisposeWith(_disposables);
        _statsService.ListenThroughRate.Subscribe(v => TextListenThroughRate = v).DisposeWith(_disposables);
        _statsService.SkippedTrackId.Subscribe(v => TextSkippedTrackId = v).DisposeWith(_disposables);
        _statsService.PeakDiscoveryDay.Subscribe(v => TextPeakDiscoveryDay = v).DisposeWith(_disposables);
        _statsService.PlaysPerTrack.Subscribe(v => ListPlaysPerTrack = v).DisposeWith(_disposables);
        _statsService.BpmFlow.Subscribe(v => ListBpmFlow = v).DisposeWith(_disposables);
        _statsService.Insights.Subscribe(v => ListInsights = v).DisposeWith(_disposables);
    }

    public void LoadAlbum(ObjectId albumId) => _statsService.SetAlbumId(albumId);

    public void Dispose() => _disposables.Dispose();
}