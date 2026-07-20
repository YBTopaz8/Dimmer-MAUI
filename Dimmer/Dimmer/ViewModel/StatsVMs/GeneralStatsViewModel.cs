using Dimmer.Charts;
using Dimmer.Charts.Services;

namespace Dimmer.ViewModel.StatsVMs;

public partial class GeneralStatsViewModel : ObservableObject, IDisposable
{
    private readonly GeneralStatsService _statsService;
    private readonly CompositeDisposable _disposables = new();

    [ObservableProperty] public partial DateRangeFilter CurrentFilter { get; set; } = DateRangeFilter.AllTime;

    // --- 6 COMMON PROPERTIES ---
    [ObservableProperty] public partial TextStat TextTotalTime { get; set; }
    [ObservableProperty] public partial TextStat TextLifespan { get; set; }
    [ObservableProperty] public partial IReadOnlyList<ChartPoint> ListPlaySkipRatio { get; set; }
    [ObservableProperty] public partial IReadOnlyList<ChartPoint> ListTimeOfDayHeatmap { get; set; }
    [ObservableProperty] public partial IReadOnlyList<ChartPoint> ListDayOfWeekHeatmap { get; set; }
    [ObservableProperty] public partial IReadOnlyList<TrendStat> ListMonthlyTrend { get; set; }

    // --- 10 SPECIFIC PROPERTIES (Plus Leaderboards) ---
    [ObservableProperty] public partial TextStat TextIntrovertExtrovertScore { get; set; }
    [ObservableProperty] public partial TextStat TextSkipToCompletionRatio { get; set; }
    [ObservableProperty] public partial TextStat TextCenterOfGravityYear { get; set; }
    [ObservableProperty] public partial TextStat TextParetoPrinciple { get; set; }
    [ObservableProperty] public partial TextStat TextPrimaryDecade { get; set; }
    [ObservableProperty] public partial TextStat TextAdventurousness { get; set; }
    [ObservableProperty] public partial TextStat TextLibraryChurn { get; set; }

    [ObservableProperty] public partial IReadOnlyList<LeaderboardItem> ListTopSongs { get; set; }
    [ObservableProperty] public partial IReadOnlyList<LeaderboardItem> ListTopArtists { get; set; }
    [ObservableProperty] public partial IReadOnlyList<LeaderboardItem> ListMusicalTrinity { get; set; }
    [ObservableProperty] public partial IReadOnlyList<LeaderboardItem> ListForgottenGems { get; set; }
    [ObservableProperty] public partial IReadOnlyList<LeaderboardItem> ListFibonacciArtists { get; set; }
    [ObservableProperty] public partial IReadOnlyList<HealthIssue> ListLibraryHealth { get; set; }

    public GeneralStatsViewModel(GeneralStatsService statsService)
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
        _statsService.IntrovertExtrovertScore.Subscribe(v => TextIntrovertExtrovertScore = v).DisposeWith(_disposables);
        _statsService.SkipToCompletionRatio.Subscribe(v => TextSkipToCompletionRatio = v).DisposeWith(_disposables);
        _statsService.CenterOfGravityYear.Subscribe(v => TextCenterOfGravityYear = v).DisposeWith(_disposables);
        _statsService.ParetoPrinciple.Subscribe(v => TextParetoPrinciple = v).DisposeWith(_disposables);
        _statsService.PrimaryDecade.Subscribe(v => TextPrimaryDecade = v).DisposeWith(_disposables);
        _statsService.Adventurousness.Subscribe(v => TextAdventurousness = v).DisposeWith(_disposables);
        _statsService.LibraryChurn.Subscribe(v => TextLibraryChurn = v).DisposeWith(_disposables);

        _statsService.TopSongs.Subscribe(v => ListTopSongs = v).DisposeWith(_disposables);
        _statsService.TopArtists.Subscribe(v => ListTopArtists = v).DisposeWith(_disposables);
        _statsService.MusicalTrinity.Subscribe(v => ListMusicalTrinity = v).DisposeWith(_disposables);
        _statsService.ForgottenGems.Subscribe(v => ListForgottenGems = v).DisposeWith(_disposables);
        _statsService.FibonacciArtists.Subscribe(v => ListFibonacciArtists = v).DisposeWith(_disposables);
        _statsService.LibraryHealth.Subscribe(v => ListLibraryHealth = v).DisposeWith(_disposables);
    }

    [RelayCommand]
    public void ChangeDateFilter(DateRangeFilter filter)
    {
        CurrentFilter = filter;
        _statsService.SetDateFilter(filter); // Automatically re-calculates everything!
    }

    public void Dispose() => _disposables.Dispose();
}