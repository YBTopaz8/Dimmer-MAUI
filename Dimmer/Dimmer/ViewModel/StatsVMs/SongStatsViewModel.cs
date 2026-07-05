using Dimmer.Charts;
using Dimmer.Charts.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dimmer.ViewModel;


public partial class SongStatsViewModel : ObservableObject, IDisposable
{
    private readonly SongStatsService _statsService;
    private readonly CompositeDisposable _disposables = new();

    // --- UI PROPERTIES TO BIND TO XAML ---
    [ObservableProperty] public partial TextStat TextCompletionRate { get; set; }
    [ObservableProperty] public partial TextStat TextBingeFactor { get; set; }
    [ObservableProperty] public partial TextStat TextAvgListenDuration { get; set; }
    [ObservableProperty] public partial TextStat TextPredictability { get; set; }
    [ObservableProperty] public partial IReadOnlyList<ChartPoint> ListActionRadar { get;set;}
    [ObservableProperty] public partial IReadOnlyList<ChartPoint> ListDropOffHeatmap { get;set;}
    [ObservableProperty] public partial IReadOnlyList<TrendStat> ListWeeklyTrend { get;set;}
    [ObservableProperty] public partial IReadOnlyList<TrendStat> ListMonthlyTrend { get;set;}

    [ObservableProperty] public partial IReadOnlyList<SongPairing> ListPerfectPairings { get;set;}
    [ObservableProperty] public partial IReadOnlyList<PlaySession> ListWalkthrough { get;set;}
    [ObservableProperty] public partial IReadOnlyList<InsightStat> ListInsights { get;set;}
    [ObservableProperty] public partial bool IsLoading {get;set;}

    public SongStatsViewModel(SongStatsService statsService)
    {
        _statsService = statsService;

        // --- SUBSCRIBE TO THE SERVICE STREAMS ---

        _statsService.TextCompletionRate
            .Subscribe(stat => TextCompletionRate = stat)
            .DisposeWith(_disposables);

        _statsService.TextPredictability
            .Subscribe(stat => TextPredictability = stat)
            .DisposeWith(_disposables);

        _statsService.TextBingeFactor
            .Subscribe(stat => TextBingeFactor = stat)
            .DisposeWith(_disposables);

        _statsService.TextAvgListenDuration
            .Subscribe(stat => TextAvgListenDuration = stat)
            .DisposeWith(_disposables);

        _statsService.ListActionRadar
            .Subscribe(stat => ListActionRadar = stat)
            .DisposeWith(_disposables);

        _statsService.ListDropOffHeatmap
            .Subscribe(data => ListDropOffHeatmap = data)
            .DisposeWith(_disposables);

        _statsService.ListWeeklyTrend
            .Subscribe(data => ListWeeklyTrend = data)
            .DisposeWith(_disposables);

        _statsService.ListMonthlyTrend
            .Subscribe(data => ListMonthlyTrend = data)
            .DisposeWith(_disposables);

        _statsService.ListPerfectPairings
            .Subscribe(data => ListPerfectPairings = data)
            .DisposeWith(_disposables);

        _statsService.ListWalkthrough
            .Subscribe(data => ListWalkthrough = data)
            .DisposeWith(_disposables);

        _statsService.ListInsights
            .Subscribe(insights =>
            {
                ListInsights = insights;
                IsLoading = false; // Turn off loading spinner when data arrives!
            })
            .DisposeWith(_disposables);
    }

    // --- THE TRIGGER ---
    // Call this when the page opens!
    public void LoadSong(ObjectId songId)
    {
        IsLoading = true;
        _statsService.SetSongId(songId); // Tells the service to start calculating!
    }

    // Clean up memory when the page is closed
    public void Dispose()
    {
        _disposables.Dispose();
    }
}