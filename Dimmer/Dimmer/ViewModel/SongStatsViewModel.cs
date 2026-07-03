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
    [ObservableProperty] public partial TextStat SkipRate { get; set; }
    [ObservableProperty] public partial IReadOnlyList<ChartPoint> ActionRadarData {get;set;}
    [ObservableProperty] public partial IReadOnlyList<InsightStat> SongInsights {get;set;}
    [ObservableProperty] public partial bool IsLoading {get;set;}

    public SongStatsViewModel(SongStatsService statsService)
    {
        _statsService = statsService;

        // --- SUBSCRIBE TO THE SERVICE STREAMS ---

        _statsService.SkipRate
            .Subscribe(stat => SkipRate = stat)
            .DisposeWith(_disposables);

        _statsService.ActionRadarChart
            .Subscribe(data => ActionRadarData = data)
            .DisposeWith(_disposables);

        _statsService.SongInsights
            .Subscribe(insights =>
            {
                SongInsights = insights;
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