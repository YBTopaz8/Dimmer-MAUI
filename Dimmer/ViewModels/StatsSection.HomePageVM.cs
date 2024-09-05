using Microcharts;
using SkiaSharp;

namespace Dimmer_MAUI.ViewModels;

public partial class HomePageVM
{

    [ObservableProperty]
    ObservableCollection<SingleSongStatistics> topTenPlayedSongs = new();
    [RelayCommand]
    void ShowGeneralTopTenSongs()
    {
        TopTenPlayedSongs = SongsMgtService.AllSongs
            .Select(s => new SingleSongStatistics
            {
                Song = s,
                PlayCount = s.DatesPlayed.Count,
            })
            .OrderByDescending(s => s.PlayCount)
            .Take(15)
            .ToObservableCollection();
        ShowSingleSongStats(TopTenPlayedSongs.FirstOrDefault()?.Song);
    }

    [RelayCommand]
    void ShowTopTenSongsForSpecificDay(DateTime? selectedDay)
    {
        if (selectedDay == null)
        {
            return;
        }        

        TopTenPlayedSongs = SongsMgtService.AllSongs
        .Select(s => new SingleSongStatistics
        {
            Song = s,
            PlayCount = s.DatesPlayed.Count(d => d.Date == selectedDay.Value.Date), // Filter by specific date
        })
        .OrderByDescending(s => s.PlayCount)
        .Take(10)
        .ToObservableCollection();
        ShowSingleSongStats(TopTenPlayedSongs.FirstOrDefault()?.Song);

    }

    Dictionary<string, SKColor> dayColors = new Dictionary<string, SKColor>
    {
        { "Monday", SKColor.Parse("#3498db") },    // Blue
        { "Tuesday", SKColor.Parse("#2ecc71") },   // Green
        { "Wednesday", SKColor.Parse("#f1c40f") }, // Yellow
        { "Thursday", SKColor.Parse("#e67e22") },  // Orange
        { "Friday", SKColor.Parse("#e74c3c") },    // Red
        { "Saturday", SKColor.Parse("#9b59b6") },  // Purple
        { "Sunday", SKColor.Parse("#34495e") }     // Dark Blue
    };

    Dictionary<string, int> dayCounts = new Dictionary<string, int>
    {
        { "Monday", 0 },
        { "Tuesday", 0 },
        { "Wednesday", 0 },
        { "Thursday", 0 },
        { "Friday", 0 },
        { "Saturday", 0 },
        { "Sunday", 0 }
    };

    [ObservableProperty]
    SongsModelView songPickedForStats;
    [ObservableProperty]
    string? mostPlayedDay;
    [RelayCommand]
    void ShowSingleSongStats(SongsModelView? song)
    {
        
        if (song == null)
        { return; }
        SongPickedForStats = song;
        if(song.DatesPlayed == null || song.DatesPlayed.Count < 1 )
        { 
            song = TemporarilyPickedSong;
        }
        MostPlayedDay = song.DatesPlayed.GroupBy(d => d.Date) // Group by date only (ignoring time)
        .OrderByDescending(g => g.Count()) // Order by count descending
        .FirstOrDefault().Key.DayOfWeek.ToString();

        dayCounts = new Dictionary<string, int>
        {
            { "Monday", 0 },
            { "Tuesday", 0 },
            { "Wednesday", 0 },
            { "Thursday", 0 },
            { "Friday", 0 },
            { "Saturday", 0 },
            { "Sunday", 0 }
        };


        foreach (var date in song.DatesPlayed)
        {
            var day = date.Date.ToString("dddd");

            // Increment the count for this day
            if (dayCounts.ContainsKey(day))
            {
                dayCounts[day]++;
            }
        }
        ChartEntries?.Clear();
        ChartEntries = new();
        // Iterate through the dictionary to create ChartEntry objects
        foreach (var dayCount in dayCounts)
        {
            var dayName = dayCount.Key; // Get the day name
            var color = dayColors.ContainsKey(dayName) ? dayColors[dayName] : SKColor.Parse("#95a5a6"); // Default Grey

            ChartEntries.Add(new ChartEntry(dayCount.Value)
            {
                Label = dayCount.Key,
                ValueLabel = dayCount.Value.ToString(),
                Color = color
            });
        }
               
        // Create the donut chart
        DChart = new DonutChart
        {            
            BackgroundColor = SKColor.Empty,
            Entries = ChartEntries,
            HoleRadius = 0.2f, 
            LabelTextSize = 15,
            
        };

        LChart = new LineChart
        {
            Entries = ChartEntries,
            BackgroundColor = SKColor.Empty,
            LabelOrientation = Orientation.Horizontal,
        };
    }


    List<ChartEntry> lineChartEntries;
    [ObservableProperty]
    public LineChart lChart;

    List<ChartEntry> ChartEntries;
    [ObservableProperty]
    public DonutChart dChart;
}

public class SingleSongStatistics
{
    public SongsModelView Song { get; set; }
    public int PlayCount { get; set; }
}

//{
//var groupedDates = TemporarilyPickedSong.DatesPlayed
//    .GroupBy(date => date.Date)
//    .OrderBy(group => group.Key);

//ChartItems?.Clear();
//DateTime startDate = groupedDates.First().Key;
//DateTime endDate = groupedDates.Last().Key;

//// Track all dates between startDate and endDate
//var allDates = Enumerable.Range(0, (endDate - startDate).Days + 1)
//    .Select(offset => startDate.AddDays(offset));

//    foreach (var date in allDates)
//    {
//        int playCount = groupedDates.FirstOrDefault(g => g.Key == date)?.Count() ?? 0;

//ChartItems?.Add(new ChartItem
//        {
//            Value = playCount,
//            Label = date.ToShortDateString(),
//        });
//    }