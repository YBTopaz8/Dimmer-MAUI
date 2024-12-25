using System.Collections.Concurrent;
using System.Linq;

namespace Dimmer_MAUI.ViewModels;

public partial class HomePageVM
{
  
    [ObservableProperty]
    public partial ObservableCollection<DimmData>? HourlyPlayEventDataPlot { get; set; }

    public void InitializeHourlyPlayEventData(ObservableCollection<PlayDataLink> playData)
    {           
        HourlyPlayEventDataPlot = playData
            .GroupBy(p => new { p.SongId, Hour = p.DateFinished.Hour })
            .Select(g => g.First())
            .GroupBy(p => p.DateFinished.Hour)
            .Select(g => new DimmData
            {
                TimeKey = g.Key.ToString(),
                DimmCount = g.Count()
            })
            .ToObservableCollection();

    }
    // Graphs: Bar chart of play counts per hour, Line chart of hourly play counts, Heatmap of hourly listening activity.
    // Summary: This shows when you listen to music most often during the day, highlighting your peak listening hours.

    [ObservableProperty]
    public partial ObservableCollection<DimmData>? DailyPlayEventDataPlot { get; set; }

    // Graphs: Bar chart of play counts per day, Line chart of daily play counts over time, Calendar heatmap of daily listening.
    

    // Graphs: Bar chart of play counts per month, Line chart of monthly play counts over time, Time series of monthly listening activity.
    // Summary: This shows how much you listen to music each month, giving you a view of your monthly listening patterns.

}

