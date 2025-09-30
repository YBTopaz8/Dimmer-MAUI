using CommunityToolkit.Mvvm.Input;

using Dimmer.Utilities.StatsUtils;
using Dimmer.Utilities.StatsUtils.Albums;
using Dimmer.Utilities.StatsUtils.Artists;

using DynamicData;
using DynamicData.Binding;

using ReactiveUI;

using System.Reactive.Disposables;

namespace Dimmer.ViewModel;

public partial class StatisticsViewModel : ObservableObject
{
    private readonly StatisticsService _statsService;
    private readonly ILogger<StatisticsViewModel> _logger;

    public StatisticsViewModel(StatisticsService statsService, ILogger<StatisticsViewModel> logger)
    {
        _statsService = statsService;
        _logger = logger;

        // Initialize the collection for the UI's filter picker
        AvailableFilters = new ObservableCollection<DateRangeFilter>(Enum.GetValues<DateRangeFilter>());

        // Set a default filter
        SelectedFilter = DateRangeFilter.Last30Days;
    }

    #region --- UI State Properties ---

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    public partial bool IsBusy {get;set; }

    public bool IsIdle => !IsBusy;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial DateRangeFilter SelectedFilter {get;set;}

    public ObservableCollection<DateRangeFilter> AvailableFilters { get; }

    #endregion

    #region --- Data Bundle Properties ---

    // These hold the final, calculated data for the UI to bind to.
    [ObservableProperty]
    public partial LibraryStatsBundle? LibraryStats { get; set; }

    [ObservableProperty]
    public partial SongStatsBundle? SongStats {get;set;}

    [ObservableProperty]
    public partial ArtistStatsBundle? ArtistStats { get; set; }

    [ObservableProperty]
    public partial AlbumStatsBundle? AlbumStats{get;set;}

    #endregion

    #region --- Commands ---

    /// <summary>
    /// Command to load the main library statistics. Called on page load and when the filter changes.
    /// </summary>
    [RelayCommand]
    private void LoadLibraryStatsAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        StatusMessage = "Calculating library overview...";
        try
        {
            // Clear old stats to prevent showing stale data
            ClearAllStats();
            LibraryStats = _statsService.GetLibraryStatistics(SelectedFilter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load library statistics.");
            StatusMessage = "Error loading library stats.";
        }
        finally
        {
            IsBusy = false;
            StatusMessage = string.Empty;
        }
    }

    /// <summary>
    /// Command to load stats for a specific song.
    /// </summary>
    [RelayCommand]
    private async Task LoadSongStatsAsync(SongModelView? song)
    {
        if (song is null || IsBusy)
            return;

        IsBusy = true;
        StatusMessage = $"Loading stats for {song.Title}...";
        try
        {
            ClearAllStats();
            // Note: The service method is synchronous in your example.
            // If it becomes async, you'll need to 'await' it.
            SongStats = await Task.Run(() => _statsService.GetSongStatisticsAsync(song.Id, SelectedFilter));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load song statistics for {SongId}", song.Id);
            StatusMessage = "Error loading song stats.";
        }
        finally
        {
            IsBusy = false;
            StatusMessage = string.Empty;
        }
    }

    /// <summary>
    /// Command to load stats for a specific artist.
    /// </summary>
    [RelayCommand]
    private async Task LoadArtistStatsAsync(ArtistModel? artist)
    {
        if (artist is null || IsBusy)
            return;

        IsBusy = true;
        StatusMessage = $"Loading stats for {artist.Name}...";
        try
        {
            ClearAllStats();
            ArtistStats = _statsService.GetArtistStatisticsAsync(artist.Id, SelectedFilter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load artist statistics for {ArtistId}", artist.Id);
            StatusMessage = "Error loading artist stats.";
        }
        finally
        {
            IsBusy = false;
            StatusMessage = string.Empty;
        }
    }

    /// <summary>
    /// Command to load stats for a specific album.
    /// </summary>
    [RelayCommand]
    private async Task LoadAlbumStatsAsync(AlbumModel? album)
    {
        if (album is null || IsBusy)
            return;

        IsBusy = true;
        StatusMessage = $"Loading stats for {album.Name}...";
        try
        {
            ClearAllStats();
            AlbumStats = _statsService.GetAlbumStatisticsAsync(album.Id, SelectedFilter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load album statistics for {AlbumId}", album.Id);
            StatusMessage = "Error loading album stats.";
        }
        finally
        {
            IsBusy = false;
            StatusMessage = string.Empty;
        }
    }

    #endregion

    /// <summary>
    /// This is automatically called by the CommunityToolkit source generator
    /// whenever the SelectedFilter property changes.
    /// </summary>
    async partial void OnSelectedFilterChanged(DateRangeFilter value)
    {
        // When the user changes the time filter, we should re-load whatever
        // stats are currently being displayed.
        if (LibraryStats is not null)
            LoadLibraryStatsAsync();
        else if (SongStats is not null)
            await LoadSongStatsAsync(new SongModelView { Id = new ObjectId(SongStats.Summary.SongTitle) /* hack - needs proper ID*/ }); // You'd need a way to get the original song ID
        else if (ArtistStats is not null)
            await LoadArtistStatsAsync(new ArtistModel { Id = ArtistStats.Summary.ArtistId });
        else if (AlbumStats is not null)
            await LoadAlbumStatsAsync(new AlbumModel { Id = AlbumStats.Summary.AlbumId });
    }

    private void ClearAllStats()
    {
        LibraryStats = null;
        SongStats = null;
        ArtistStats = null;
        AlbumStats = null;
    }
}