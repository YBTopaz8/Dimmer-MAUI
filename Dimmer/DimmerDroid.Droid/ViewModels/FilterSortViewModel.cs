using DevExpress.Data.Filtering;
using DevExpress.Maui.CollectionView;

namespace Dimmer.ViewModels;

public  partial class FilterSortViewModel:ObservableObject
{
	
  private readonly DXCollectionView _collectionView;
    private readonly BaseViewModel _mainViewModel;

    [ObservableProperty]
    public partial ObservableCollection<SongModelView> FilteredSongs { get; set; }



    [ObservableProperty]
    public partial string FilterString {get;set;}

    [ObservableProperty]
    public partial string SelectedSortField {get;set;}

    [ObservableProperty]
    public partial DataSortOrder SelectedSortOrder {get;set;}

    [ObservableProperty]
    public partial string SelectedGroupField {get;set;}

    [ObservableProperty]
    public partial bool ShowInstrumentalOnly {get;set;}

    [ObservableProperty]
    public partial bool ShowHasLyricsOnly {get;set;}

    [ObservableProperty]
    public partial int MinRating { get; set; } = 0;

    [ObservableProperty]
    public partial int MaxRating { get; set; } = 5;

    [ObservableProperty]
public partial double MinDurationSeconds { get; set; } = 0;

    [ObservableProperty]
    public partial double MaxDurationSeconds { get; set; } = 3600; // 1 hour 

[ObservableProperty]
public partial int MinReleaseYear { get; set; } = 1950;

    [ObservableProperty]
    public partial int MaxReleaseYear { get; set; } = DateTime.Now.Year;

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<string> AvailableGenres {get;set;}

    [ObservableProperty]
    public partial ObservableCollection<string> SelectedGenres {get;set;}

    [ObservableProperty]
    public partial ObservableCollection<string> AvailableArtists {get;set;}

    [ObservableProperty]
    public partial ObservableCollection<string> SelectedArtists {get;set;}

    // Available sort fields
    public ObservableCollection<SortFieldInfo> SortFields { get; }
    public ObservableCollection<GroupFieldInfo> GroupFields { get; }
    public ObservableCollection<SortOrderInfo> SortOrders { get; }

    public FilterSortViewModel(DXCollectionView collectionView, BaseViewModel mainViewModel)
    {
        _collectionView = collectionView;
        _mainViewModel = mainViewModel;

        FilteredSongs = new ObservableCollection<SongModelView>();
        SelectedGenres = new ObservableCollection<string>();
        SelectedArtists = new ObservableCollection<string>();

        // Initialize sort fields
        SortFields = new ObservableCollection<SortFieldInfo>
        {
            new SortFieldInfo { FieldName = "Title", DisplayName = "Song Title" },
            new SortFieldInfo { FieldName = "ArtistName", DisplayName = "Artist Name" },
            new SortFieldInfo { FieldName = "AlbumName", DisplayName = "Album Name" },
            new SortFieldInfo { FieldName = "ReleaseYear", DisplayName = "Release Year" },
            new SortFieldInfo { FieldName = "DurationInSeconds", DisplayName = "Duration" },
            new SortFieldInfo { FieldName = "NumberOfTimesFaved", DisplayName = "Favorite Count" },
            new SortFieldInfo { FieldName = "PlayCount", DisplayName = "Play Count" },
            new SortFieldInfo { FieldName = "Rating", DisplayName = "Rating" },
            new SortFieldInfo { FieldName = "DateCreated", DisplayName = "Date Added" },
            new SortFieldInfo { FieldName = "LastPlayed", DisplayName = "Last Played" },
            new SortFieldInfo { FieldName = "BPM", DisplayName = "BPM" },
            new SortFieldInfo { FieldName = "TrackNumber", DisplayName = "Track #" }
        };

        // Initialize group fields
        GroupFields = new ObservableCollection<GroupFieldInfo>
        {
            new GroupFieldInfo { FieldName = "None", DisplayName = "No Grouping" },
            new GroupFieldInfo { FieldName = "ArtistName", DisplayName = "Group by Artist" },
            new GroupFieldInfo { FieldName = "AlbumName", DisplayName = "Group by Album" },
            new GroupFieldInfo { FieldName = "GenreName", DisplayName = "Group by Genre" },
            new GroupFieldInfo { FieldName = "ReleaseYear", DisplayName = "Group by Year" },
            new GroupFieldInfo { FieldName = "Rating", DisplayName = "Group by Rating" }
        };

        // Initialize sort orders
        SortOrders = new ObservableCollection<SortOrderInfo>
        {
            new SortOrderInfo { SortOrder = DataSortOrder.Ascending, DisplayName = "Ascending (A-Z)" },
            new SortOrderInfo { SortOrder = DataSortOrder.Descending, DisplayName = "Descending (Z-A)" }
        };

        // Set defaults
        SelectedSortField = "Title";
        SelectedSortOrder = DataSortOrder.Ascending;
        SelectedGroupField = "None";

        // Load available filters
        LoadAvailableFilters();
    }

    private void LoadAvailableFilters()
    {
        AvailableGenres = new ObservableCollection<string>(
            _mainViewModel.SearchResults.Select(s => s.GenreName)
            .Where(g => !string.IsNullOrEmpty(g))
            .Distinct()
            .OrderBy(g => g)
        );

        AvailableArtists = new ObservableCollection<string>(
            _mainViewModel.SearchResults.Select(s => s.OtherArtistsName)
            .Where(a => !string.IsNullOrEmpty(a))
            .Distinct()
            .OrderBy(a => a)
        );
    }

    [RelayCommand]
    public void ApplyFilters()
    {
        var filters = new List<CriteriaOperator>();

        // Genre filter
        if (SelectedGenres.Any())
        {
            var genreConditions = SelectedGenres.Select(genre =>
                new BinaryOperator("GenreName", genre, BinaryOperatorType.Equal));
            filters.Add(new GroupOperator(GroupOperatorType.Or, genreConditions));
        }

        // Artist filter
        if (SelectedArtists.Any())
        {
            var artistConditions = SelectedArtists.Select(artist =>
                new BinaryOperator("ArtistName", artist, BinaryOperatorType.Equal));
            filters.Add(new GroupOperator(GroupOperatorType.Or, artistConditions));
        }

        // Instrumental filter
        if (ShowInstrumentalOnly)
        {
            filters.Add(new BinaryOperator("IsInstrumental", true, BinaryOperatorType.Equal));
        }

        // Has Lyrics filter
        if (ShowHasLyricsOnly)
        {
            filters.Add(new BinaryOperator("HasLyrics", true, BinaryOperatorType.Equal));
        }

        // Rating range
        if (MinRating > 0 || MaxRating < 5)
        {
            filters.Add(new BetweenOperator("Rating", MinRating, MaxRating));
        }

        // Duration range
        if (MinDurationSeconds > 0 || MaxDurationSeconds < 3600)
        {
            filters.Add(new BetweenOperator("DurationInSeconds", MinDurationSeconds, MaxDurationSeconds));
        }

        // Release year range
        if (MinReleaseYear > 1950 || MaxReleaseYear < DateTime.Now.Year)
        {
            filters.Add(new BetweenOperator("ReleaseYear", MinReleaseYear, MaxReleaseYear));
        }

        // Search text (searches multiple fields)
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLower();
            var searchConditions = new List<CriteriaOperator>
            {
                new FunctionOperator(FunctionOperatorType.Contains, new OperandProperty("Title"), new ConstantValue(searchLower)),
                new FunctionOperator(FunctionOperatorType.Contains, new OperandProperty("ArtistName"), new ConstantValue(searchLower)),
                new FunctionOperator(FunctionOperatorType.Contains, new OperandProperty("AlbumName"), new ConstantValue(searchLower)),
                new FunctionOperator(FunctionOperatorType.Contains, new OperandProperty("GenreName"), new ConstantValue(searchLower))
            };
            filters.Add(new GroupOperator(GroupOperatorType.Or, searchConditions));
        }

        // Combine all filters with AND
        if (filters.Any())
        {
            FilterString = new GroupOperator(GroupOperatorType.And, filters).ToString();
            _collectionView.FilterString = FilterString;
        }
        else
        {
            FilterString = string.Empty;
            _collectionView.FilterString = string.Empty;
        }

        ApplySort();
        ApplyGrouping();
    }

    [RelayCommand]
    public void ApplySort()
    {
        _collectionView.SortDescriptions.Clear();

        if (!string.IsNullOrEmpty(SelectedSortField))
        {
            _collectionView.SortDescriptions.Add(new DevExpress.Maui.CollectionView.SortDescription
            {
                FieldName = SelectedSortField,
                SortOrder = SelectedSortOrder
            });
        }
    }

    [RelayCommand]
    public void ApplyGrouping()
    {
        _collectionView.GroupDescription = new();

        if (SelectedGroupField != "None" && !string.IsNullOrEmpty(SelectedGroupField))
        {
            _collectionView.GroupDescription= new GroupDescription
            {
                FieldName = SelectedGroupField
            };
        }
    }

    [RelayCommand]
    public void ResetAllFilters()
    {
        // Reset all filter properties
        SelectedGenres.Clear();
        SelectedArtists.Clear();
        ShowInstrumentalOnly = false;
        ShowHasLyricsOnly = false;
        MinRating = 0;
        MaxRating = 5;
        MinDurationSeconds = 0;
        MaxDurationSeconds = 3600;
        MinReleaseYear = 1950;
        MaxReleaseYear = DateTime.Now.Year;
        SearchText = string.Empty;

        // Reset sort
        SelectedSortField = "Title";
        SelectedSortOrder = DataSortOrder.Ascending;

        // Reset grouping
        SelectedGroupField = "None";

        // Apply reset
        ApplyFilters();
    }
}

// Helper classes
public class SortFieldInfo
{
    public string FieldName { get; set; }
    public string DisplayName { get; set; }
}

public class GroupFieldInfo
{
    public string FieldName { get; set; }
    public string DisplayName { get; set; }
}

public class SortOrderInfo
{
    public DataSortOrder SortOrder { get; set; }
    public string DisplayName { get; set; }
}