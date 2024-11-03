
using System;
using System.Linq;

namespace Dimmer_MAUI.ViewModels;

public partial class HomePageVM
{

    [ObservableProperty]
    ObservableCollection<SingleSongStatistics> topTenPlayedSongs = new();

    [RelayCommand]
    void ShowGeneralTopXSongs()
    {

        TopTenPlayedSongs = DisplayedSongs
            .Select(s => new SingleSongStatistics
            {
                Song = s,
                PlayCount = s.DatesPlayedAndWasPlayCompleted.Count()
                //PlayCount = s.DatesPlayed.Count(d => d.Date >= lastWeek && d.Date <= today),
            })
            .OrderByDescending(s => s.PlayCount)
            .Take(20)
            .ToObservableCollection();
        if (IsPlaying && CurrentQueue != 2)
        {
            ShowSingleSongStats(TemporarilyPickedSong);
        }
        else
        {
            ShowSingleSongStats(TopTenPlayedSongs.FirstOrDefault()?.Song);
        }
    }

    [RelayCommand]
    void ShowTopTenSongsForSpecificDay(DateTimeOffset? selectedDay)
    {
        if (selectedDay == null)
        {
            return;
        }

        TopTenPlayedSongs = SongsMgtService.AllSongs
    .Select(s => new SingleSongStatistics
    {
        Song = s,
        // Count only dates where the song was fully played (WasPlayCompleted is true)
        PlayCount = s.DatesPlayedAndWasPlayCompleted
            .Count(d => d.DatePlayed.Date == selectedDay.Value.Date && d.WasPlayCompleted == true)
    })
    .OrderByDescending(s => s.PlayCount)
    .Take(10)
    .ToObservableCollection();


        ShowSingleSongStats(TopTenPlayedSongs.FirstOrDefault()?.Song);

    }

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
    SingleSongStatistics songPickedForStats;
    [ObservableProperty]
    int numberOfTimesPlayed;
    [ObservableProperty]
    string? mostPlayedDay;
    [ObservableProperty]
    bool isChartVisible;
    [RelayCommand]
    void ShowSingleSongStats(SongsModelView? song)
    {
        IsChartVisible = false;
        if (song == null)
            return;

        SongPickedForStats ??= new SingleSongStatistics();
        SongPickedForStats.Song = song;

        LoadDailyStats(song);
        
        UpdateMostPlayedDay(song);
        UpdateNumberOfTimesPlayed(song);

    }

    public void LoadWeeklyStats(SongsModelView song, DateTimeOffset? startDate = null, DateTimeOffset? endDate=null)
    {
        startDate ??= DateTimeOffset.UtcNow.Date;
        endDate ??= DateTimeOffset.UtcNow.Date;
        SongPickedForStats.WeeklyStats = new ObservableCollection<WeeklyStats>();
        // Loop through each week in the specified range
        var currentStartDate = startDate.Value;
        while (currentStartDate <= endDate.Value.Date)
        {
            var weekEndDate = currentStartDate.AddDays(6); // Define the end date of the current week

            // Create and add each weekly stat entry
            SongPickedForStats.WeeklyStats.Add(new WeeklyStats(song, currentStartDate, weekEndDate));

            // Move to the next week
            currentStartDate = currentStartDate.AddDays(7);
        }
    }


    public void LoadDailyStats(SongsModelView song, DateTimeOffset? specificDate = null)
    {
        specificDate ??= DateTimeOffset.UtcNow.Date;

        SongPickedForStats.DailyStats = new DailyStats(song, specificDate.Value.Date);

        UpdateMostPlayedDay(song);
        UpdateNumberOfTimesPlayed(song);
    }

    public void LoadMonthlyStats(SongsModelView song, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {
        
        startDate ??= new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        endDate ??= new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero)
                        .AddMonths(1).AddDays(-1); // End of the current month

        SongPickedForStats.MonthlyStats = new ObservableCollection<MonthlyStats>();

        
        var currentMonth = new DateTimeOffset(startDate.Value.Year, startDate.Value.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var lastMonth = new DateTimeOffset(endDate.Value.Year, endDate.Value.Month, 1, 0, 0, 0, TimeSpan.Zero);

        while (currentMonth <= lastMonth)
        {
            
            var monthEndDate = currentMonth.AddMonths(1).AddDays(-1);

            
            SongPickedForStats.MonthlyStats.Add(new MonthlyStats(song, currentMonth, monthEndDate));

            
            currentMonth = currentMonth.AddMonths(1);
        }
    }


    public void LoadYearlyStats(SongsModelView song, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {
        
        startDate ??= new DateTimeOffset(DateTimeOffset.Now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        endDate ??= new DateTimeOffset(DateTimeOffset.Now.Year, 12, 31, 23, 59, 59, TimeSpan.Zero);

        SongPickedForStats.YearlyStats = new ObservableCollection<YearlyStats>();

        
        var currentYear = new DateTimeOffset(startDate.Value.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var lastYear = new DateTimeOffset(endDate.Value.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);

        while (currentYear <= lastYear)
        {
            
            var yearEndDate = new DateTimeOffset(currentYear.Year, 12, 31, 23, 59, 59, TimeSpan.Zero);

            // Create and add each yearly stat entry
            SongPickedForStats.YearlyStats.Add(new YearlyStats(song, currentYear, yearEndDate));

            // Move to the start of the next year
            currentYear = currentYear.AddYears(1);
        }
    }

    private void UpdateMostPlayedDay(SongsModelView song)
    {
        var mostPlayedDay = song.DatesPlayedAndWasPlayCompleted?
            .GroupBy(entry => entry.DatePlayed.DayOfWeek)
            .OrderByDescending(group => group.Count())
            .FirstOrDefault();

        MostPlayedDay = mostPlayedDay?.Key.ToString() ?? "Never Played Yet";
    }

    private void UpdateNumberOfTimesPlayed(SongsModelView song)
    {
        NumberOfTimesPlayed = song.DatesPlayedAndWasPlayCompleted?
            .Count() ?? 0;
    }

    [ObservableProperty]
    int currentNowPlayingStatsViewIndex=0;
    void RefreshStatView()
    {

        UpdateMostPlayedDay(SongPickedForStats.Song);
        UpdateNumberOfTimesPlayed(SongPickedForStats.Song);

        switch (CurrentNowPlayingStatsViewIndex)
        {
            case 0:
                LoadDailyStats(SelectedSongToOpenBtmSheet);
                break;
            case 1:
                LoadWeeklyStats(SelectedSongToOpenBtmSheet);
                break;
            case 2:
                LoadMonthlyStats(SelectedSongToOpenBtmSheet);
                break;
            case 3:
                LoadYearlyStats(SelectedSongToOpenBtmSheet);
                break;
            default:
                break;
        }
    }





    

    [RelayCommand]
    async Task NavigateToSingleSongStatsPage(SongsModelView song)
    {
        SongPickedForStats.Song = song;
#if ANDROID
        //await Shell.Current.GoToAsync(nameof(SingleSongStatsPageM));
#elif WINDOWS
        await Shell.Current.GoToAsync(nameof(SingleSongStatsPageD));
#endif
    }
    [ObservableProperty]
    ObservableCollection<string> statsFilters = new ObservableCollection<string>{"Daily","Weekly", "Monthly", "Yearly"};

}

public partial class SingleSongStatistics : ObservableObject
{
    [ObservableProperty]
    SongsModelView? song;
    [ObservableProperty]
    ObservableCollection<WeeklyStats> weeklyStats;
    [ObservableProperty]
    DailyStats dailyStats;
    [ObservableProperty]
    ObservableCollection<MonthlyStats> monthlyStats;
    [ObservableProperty]
    ObservableCollection<YearlyStats> yearlyStats;
    [ObservableProperty]
    int playCount;
}

public partial class YearlyStats : ObservableObject
{
    [ObservableProperty]
    MonthlyStats? monthlies;

    [ObservableProperty]
    string month;

    [ObservableProperty]
    int count;

    [ObservableProperty]
    double totalPlayTime;

    public YearlyStats(SongsModelView model, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {

        startDate ??= DateTimeOffset.UtcNow.Date;
        endDate ??= DateTimeOffset.UtcNow.Date;
        var yearlyPlays = model.DatesPlayedAndWasPlayCompleted?
            .Where(entry => entry.DatePlayed.Date >= startDate && entry.DatePlayed.Date <= endDate)
            .ToList() ?? new List<PlayDateAndIsPlayCompletedModelView>();

        Month = startDate.Value.Year.ToString();
        Count = yearlyPlays.Count;
        TotalPlayTime = Count * model.DurationInSeconds / 60; // Convert seconds to minutes
    }

}


public partial class MonthlyStats : ObservableObject
{
    [ObservableProperty]
    WeeklyStats? weeklies;

    [ObservableProperty]
    string month;

    [ObservableProperty]
    int count;

    [ObservableProperty]
    double totalPlayTime;

    public MonthlyStats(SongsModelView model, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {
        startDate ??= DateTimeOffset.UtcNow.Date;
        endDate ??= DateTimeOffset.UtcNow.Date;
        {
            var monthlyPlays = model.DatesPlayedAndWasPlayCompleted?
                .Where(entry => entry.DatePlayed.Date >= startDate.Value.Date && entry.DatePlayed.Date <= endDate.Value.Date)
                .ToList() ?? new List<PlayDateAndIsPlayCompletedModelView>();

            Month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(startDate.Value.Month);
            Count = monthlyPlays.Count;
            TotalPlayTime = Count * model.DurationInSeconds / 60; // Convert seconds to minutes
        }
    }

}
public partial class DailyStats : ObservableObject
{
    [ObservableProperty]
    ObservableCollection<PlayDateAndIsPlayCompletedModelView>? playDates;

    [ObservableProperty]
    double totalPlayTime;

    [ObservableProperty]
    ObservableCollection<DataForChart> colforStats;

    public DailyStats(SongsModelView model, DateTimeOffset? specificDate = null)
    {
        specificDate ??= DateTimeOffset.UtcNow.Date;

        // Filter play dates for the specific date
        PlayDates = new ObservableCollection<PlayDateAndIsPlayCompletedModelView>(
            model.DatesPlayedAndWasPlayCompleted?
                .Where(entry => entry.DatePlayed.Date == specificDate.Value.Date) ??
                Enumerable.Empty<PlayDateAndIsPlayCompletedModelView>()
        );

        // Calculate total play time in minutes
        TotalPlayTime = PlayDates.Count * model.DurationInSeconds / 60;

        // Initialize ColforStats collection
        ColforStats = new ObservableCollection<DataForChart>();

        // Populate ColforStats initially
        RefreshColforStats();
    }

    public void RefreshColforStats()
    {
        // Clear any existing items
        ColforStats.Clear();

        // Count completed and incomplete plays
        int completedCount = PlayDates.Count(entry => entry.WasPlayCompleted);
        int incompleteCount = PlayDates.Count(entry => !entry.WasPlayCompleted);
        // Add data for completed plays
        ColforStats.Add(new DataForChart
        {
            LabelForNumberOfCompletedPlays = $"Completed Plays",
            TheCount = completedCount
        });

        // Add data for incomplete plays
        ColforStats.Add(new DataForChart
        {
            LabelForNumberOfCompletedPlays = $"Incomplete Plays",
            TheCount = incompleteCount
        });
        
    }

    
}

public class DataForChart
{
    public string LabelForNumberOfCompletedPlays { get; set; } // Label, e.g., "Completed Plays" or "Incomplete Plays"
    public int TheCount { get; set; }                          // Count of plays
}
public partial class WeeklyStats : ObservableObject
{
    [ObservableProperty]
    DateTimeOffset? datePlayed;

    [ObservableProperty]
    string week;

    [ObservableProperty]
    int count;

    [ObservableProperty]
    double totalPlayTime;

    public WeeklyStats(SongsModelView model, DateTimeOffset? startDate=null, DateTimeOffset? endDate = null)
    {
        startDate ??= DateTimeOffset.UtcNow.Date;
        endDate ??= DateTimeOffset.UtcNow.Date;
        var weeklyPlays = model.DatesPlayedAndWasPlayCompleted?
            .Where(entry => entry.DatePlayed.Date >= startDate.Value.Date && entry.DatePlayed.Date <= endDate.Value.Date)
            .ToList() ?? new List<PlayDateAndIsPlayCompletedModelView>();

        DatePlayed = startDate;
        Week = $"Week of {startDate:MMMM dd}";
        Count = weeklyPlays.Count;
        TotalPlayTime = Count * model.DurationInSeconds / 60; // Convert seconds to minutes
    }

}


