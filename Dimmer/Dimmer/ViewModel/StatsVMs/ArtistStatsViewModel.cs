using Dimmer.Charts;
using Dimmer.Charts.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dimmer.ViewModel.StatsVMs;


public partial class ArtistStatsViewModel : ObservableObject, IDisposable
{
    private readonly ArtistStatsService _artistService;
    private readonly CompositeDisposable _disposables = new();

    // The UI will bind to these!
    [ObservableProperty] public partial TextStat TotalTime { get; set; }
    [ObservableProperty] public partial TextStat ObsessionScore { get; set; }
    [ObservableProperty] public partial TextStat EddingtonNumber { get; set; }

    [ObservableProperty] public partial IReadOnlyList<ChartPoint> HourlyPreferenceChart { get; set; }
    [ObservableProperty] public partial IReadOnlyList<ChartPoint> DeviceFootprintChart { get; set; }

    [ObservableProperty] public partial IReadOnlyList<LeaderboardItem> TopSongs { get; set; }

    public ArtistStatsViewModel(ArtistStatsService artistService)
    {
        _artistService = artistService;

        // Wire up the pipelines to the properties!
        _artistService.TotalListeningTime.Subscribe(v => TotalTime = v).DisposeWith(_disposables);
        _artistService.ObsessionScore.Subscribe(v => ObsessionScore = v).DisposeWith(_disposables);
        _artistService.ArtistEddington.Subscribe(v => EddingtonNumber = v).DisposeWith(_disposables);

        _artistService.HourlyPreference.Subscribe(v => HourlyPreferenceChart = v).DisposeWith(_disposables);
        _artistService.DeviceFootprint.Subscribe(v => DeviceFootprintChart = v).DisposeWith(_disposables);

        _artistService.TopSongs.Subscribe(v => TopSongs = v).DisposeWith(_disposables);
    }

    // Call this when the Page Navigates to an Artist!
    public void LoadArtist(ObjectId artistId)
    {
        _artistService.SetArtistId(artistId);
    }

    public void Dispose()
    {
        _disposables.Dispose(); // Cleans up Rx memory
    }
}