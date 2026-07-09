using Dimmer.Charts;
using Dimmer.Charts.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dimmer.ViewModel.StatsVMs;

public partial class SongStatsViewModel : ObservableObject, IDisposable
{
    private readonly SongStatsService _statsService;
    private readonly CompositeDisposable _disposables = new();
    // --- 8 NEW COMMON PROPERTIES ---
    [ObservableProperty] public partial IReadOnlyList<TrendStat> ListDailyTrend { get; set; }
    [ObservableProperty] public partial IReadOnlyList<TrendStat> ListWeeklyTrend { get; set; }
    [ObservableProperty] public partial TextStat TextWeekendVsWeekday { get; set; }
    [ObservableProperty] public partial TextStat TextAvgPlaysPerActiveDay { get; set; }
    [ObservableProperty] public partial TextStat TextLongestDrought { get; set; }
    [ObservableProperty] public partial TextStat TextConsistencyScore { get; set; }
    [ObservableProperty] public partial TextStat TextMaxSessionDuration { get; set; }
    [ObservableProperty] public partial TextStat TextPeakBingeIntensity { get; set; }

    // --- 8 NEW SPECIFIC PROPERTIES ---
    [ObservableProperty] public partial TextStat TextHourOfPower { get; set; }
    [ObservableProperty] public partial TextStat TextSeasonalVibe { get; set; }
    [ObservableProperty] public partial TextStat TextRepeatOffender { get; set; }
    [ObservableProperty] public partial TextStat TextTimeBias { get; set; }
    [ObservableProperty] public partial TextStat TextResurrection { get; set; }
    [ObservableProperty] public partial TextStat TextNextMilestone { get; set; }
    [ObservableProperty] public partial TextStat TextSkipTrend { get; set; }
    [ObservableProperty] public partial TextStat TextDiscoveryAnniversary { get; set; }
    [ObservableProperty] public partial bool IsLoading { get; set; }

    // --- 6 COMMON PROPERTIES ---
    [ObservableProperty] public partial TextStat TextTotalTime { get; set; }
    [ObservableProperty] public partial TextStat TextLifespan { get; set; }
    [ObservableProperty] public partial IReadOnlyList<ChartPoint> ListPlaySkipRatio { get; set; }
    [ObservableProperty] public partial IReadOnlyList<ChartPoint> ListTimeOfDayHeatmap { get; set; }
    [ObservableProperty] public partial IReadOnlyList<ChartPoint> ListDayOfWeekHeatmap { get; set; }
    [ObservableProperty] public partial IReadOnlyList<TrendStat> ListMonthlyTrend { get; set; }

    // --- 10 SPECIFIC PROPERTIES ---
    [ObservableProperty] public partial TextStat TextCompletionRate { get; set; }
    [ObservableProperty] public partial TextStat TextAvgListenDuration { get; set; }
    [ObservableProperty] public partial TextStat TextBingeFactor { get; set; }
    [ObservableProperty] public partial TextStat TextPredictability { get; set; }
    [ObservableProperty] public partial TextStat TextEddingtonNumber { get; set; }
    [ObservableProperty] public partial TextStat TextPlayStreak { get; set; }
    [ObservableProperty] public partial TextStat TextTimeToSkip { get; set; }
    [ObservableProperty] public partial IReadOnlyList<ChartPoint> ListActionRadar { get; set; }
    [ObservableProperty] public partial IReadOnlyList<ChartPoint> ListDropOffHeatmap { get; set; }
    [ObservableProperty] public partial IReadOnlyList<SongPairing> ListPerfectPairings { get; set; }

    public SongStatsViewModel(SongStatsService statsService)
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
        _statsService.CompletionRate.Subscribe(v => TextCompletionRate = v).DisposeWith(_disposables);
        _statsService.AvgListenDuration.Subscribe(v => TextAvgListenDuration = v).DisposeWith(_disposables);
        _statsService.BingeFactor.Subscribe(v => TextBingeFactor = v).DisposeWith(_disposables);
        _statsService.Predictability.Subscribe(v => TextPredictability = v).DisposeWith(_disposables);
        _statsService.EddingtonNumber.Subscribe(v => TextEddingtonNumber = v).DisposeWith(_disposables);
        _statsService.PlayStreak.Subscribe(v => TextPlayStreak = v).DisposeWith(_disposables);
        _statsService.TimeToSkip.Subscribe(v => TextTimeToSkip = v).DisposeWith(_disposables);
        _statsService.ActionRadar.Subscribe(v => ListActionRadar = v).DisposeWith(_disposables);
        _statsService.DropOffHeatmap.Subscribe(v => ListDropOffHeatmap = v).DisposeWith(_disposables);
        _statsService.PerfectPairings.Subscribe(v => ListPerfectPairings = v).DisposeWith(_disposables);

        // New Common
        _statsService.DailyTrend.Subscribe(v => ListDailyTrend = v).DisposeWith(_disposables);
        _statsService.WeeklyTrend.Subscribe(v => ListWeeklyTrend = v).DisposeWith //_statsService.WeekendVsWeekday.Subscribe(v => TextWeekendVsWeekday = v).DisposeWith(_disposables);
        //_statsService.AveragePlaysPerActiveDay.Subscribe(v => TextAvgPlaysPerActiveDay = v).DisposeWith(_disposables);
        //_statsService.LongestDrought.Subscribe(v => TextLongestDrought = v).DisposeWith(_disposables);
        //_statsService.ConsistencyScore.Subscribe(v => TextConsistencyScore = v).DisposeWith(_disposables);
        //_statsService.MaxSessionDuration.Subscribe(v => TextMaxSessionDuration = v).DisposeWith(_disposables);
        //_statsService.PeakBingeIntensity.Subscribe(v => TextPeakBingeIntensity = v).DisposeWith(_disposables);
(_disposables);
       
        //// New Specific
        //_statsService.HourOfPower.Subscribe(v => TextHourOfPower = v).DisposeWith(_disposables);
        //_statsService.SeasonalVibe.Subscribe(v => TextSeasonalVibe = v).DisposeWith(_disposables);
        //_statsService.RepeatOffender.Subscribe(v => TextRepeatOffender = v).DisposeWith(_disposables);
        //_statsService.TimeBias.Subscribe(v => TextTimeBias = v).DisposeWith(_disposables);
        //_statsService.Resurrection.Subscribe(v => TextResurrection = v).DisposeWith(_disposables);
        //_statsService.NextMilestone.Subscribe(v => TextNextMilestone = v).DisposeWith(_disposables);
        //_statsService.SkipTrend.Subscribe(v => TextSkipTrend = v).DisposeWith(_disposables);
        //_statsService.DiscoveryAnniversary.Subscribe(v => TextDiscoveryAnniversary = v).DisposeWith(_disposables);
    }



    public void LoadSong(ObjectId songId) => _statsService.SetSongId(songId);

    public void Dispose() => _disposables.Dispose();
}