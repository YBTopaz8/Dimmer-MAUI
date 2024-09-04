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
            .Take(10)
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
}

public class SingleSongStatistics
{
    public SongsModelView Song { get; set; }
    public int PlayCount { get; set; }
}