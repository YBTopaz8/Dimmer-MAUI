using Dimmer.Charts;
using Dimmer.Charts.Services;

namespace Dimmer.ViewModel.StatsVMs;


public partial class PlaylistStatsViewModel : ObservableObject, IDisposable
{
    private readonly PlaylistStatsService _statsService;
    private readonly CompositeDisposable _disposables = new();

    [ObservableProperty] public partial bool IsLoading { get; set; }

    // --- 6 COMMON PROPERTIES ---
    [ObservableProperty] public partial TextStat TextTotalTime { get; set; }
    [ObservableProperty] public partial TextStat TextLifespan { get; set; }
    [ObservableProperty] public partial IReadOnlyList<ChartPoint> ListPlaySkipRatio { get; set; }
    [ObservableProperty] public partial IReadOnlyList<ChartPoint> ListTimeOfDayHeatmap { get; set; }
    [ObservableProperty] public partial IReadOnlyList<ChartPoint> ListDayOfWeekHeatmap { get; set; }
    [ObservableProperty] public partial IReadOnlyList<TrendStat> ListMonthlyTrend { get; set; }

    // --- 10 SPECIFIC PROPERTIES ---
    [ObservableProperty] public partial TextStat TextDominantMood { get; set; }
    [ObservableProperty] public partial TextStat TextPlaylistVelocity { get; set; }
    [ObservableProperty] public partial TextStat TextWeakLink { get; set; }
    [ObservableProperty] public partial TextStat TextAcousticElectricSplit { get; set; }
    [ObservableProperty] public partial TextStat TextArtistDiversity { get; set; }
    [ObservableProperty] public partial TextStat TextAverageDuration { get; set; }
    [ObservableProperty] public partial TextStat TextTqlAccuracy { get; set; }
    [ObservableProperty] public partial TextStat TextLoyaltyScore { get; set; }
    [ObservableProperty] public partial IReadOnlyList<ChartPoint> ListEraDistribution { get; set; }
    [ObservableProperty] public partial IReadOnlyList<InsightStat> ListInsights { get; set; }

    public PlaylistStatsViewModel(PlaylistStatsService statsService)
    {
        _statsService = statsService;

        _statsService.IsLoading.Subscribe(v => IsLoading = v).DisposeWith(_disposables);

        // Common
        _statsService.TotalTime.Subscribe(v => TextTotalTime = v).DisposeWith(_disposables);
        _statsService.Lifespan.Subscribe(v => TextLifespan = v).DisposeWith(_disposables);
        _statsService.PlaySkipRatio.Subscribe(v => ListPlaySkipRatio = v).DisposeWith(_disposables);
        _statsService.TimeOfDayHeatmap.Subscribe(v => ListTimeOfDayHeatmap = v).DisposeWith(_disposables);
        _statsService.DayOfWeekHeatmap.Subscribe(v => ListDayOfWeekHeatmap = v).DisposeWith(_disposables);
        _statsService.MonthlyTrend.Subscribe(v => ListMonthlyTrend = v).DisposeWith(_disposables);

        // Specific
        _statsService.DominantMood.Subscribe(v => TextDominantMood = v).DisposeWith(_disposables);
        _statsService.PlaylistVelocity.Subscribe(v => TextPlaylistVelocity = v).DisposeWith(_disposables);
        _statsService.WeakLink.Subscribe(v => TextWeakLink = v).DisposeWith(_disposables);
        _statsService.AcousticElectricSplit.Subscribe(v => TextAcousticElectricSplit = v).DisposeWith(_disposables);
        _statsService.ArtistDiversity.Subscribe(v => TextArtistDiversity = v).DisposeWith(_disposables);
        _statsService.AverageDuration.Subscribe(v => TextAverageDuration = v).DisposeWith(_disposables);
        _statsService.TqlAccuracy.Subscribe(v => TextTqlAccuracy = v).DisposeWith(_disposables);
        _statsService.LoyaltyScore.Subscribe(v => TextLoyaltyScore = v).DisposeWith(_disposables);
        _statsService.EraDistribution.Subscribe(v => ListEraDistribution = v).DisposeWith(_disposables);
        _statsService.Insights.Subscribe(v => ListInsights = v).DisposeWith(_disposables);
    }

    public void LoadPlaylist(ObjectId playlistId) => _statsService.SetPlaylistId(playlistId);

    public void Dispose() => _disposables.Dispose();
}
