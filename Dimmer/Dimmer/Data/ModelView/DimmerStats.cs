namespace Dimmer.Data.ModelView;
public partial class DimmerStats : ObservableObject
{
    [ObservableProperty]
    public partial SongModelView Song { get; set; }
    [ObservableProperty]
    public partial ArtistModelView? SongArtist { get; set; }
    [ObservableProperty]
    public partial AlbumModelView? SongAlbum { get; set; }
    [ObservableProperty]
    public partial int? Count { get; set; }
    [ObservableProperty]
    public partial int? YValueInt { get; set; }
    [ObservableProperty]
    public partial int NumberOfTracks { get; set; }
    [ObservableProperty]
    public partial string? TotalSeconds { get; set; }
    [ObservableProperty]
    public partial string? Name { get; set; }
    [ObservableProperty]
    public partial string? ArtistName { get; set; }
    [ObservableProperty]
    public partial string? AlbumName { get; set; }
    [ObservableProperty]
    public partial string? GenreName { get; set; }
    [ObservableProperty]
    public partial double? TotalSecondsNumeric { get; set; }
    [ObservableProperty]
    public partial double? Value { get; set; }

    /// <summary>
    /// A generic date value (e.g., First/Last Played Date).
    /// </summary>
    [ObservableProperty]
    public partial DateTimeOffset? DateValue {get;set;}

    /// <summary>
    /// A generic TimeSpan value (e.g., Average Listen Duration).
    /// </summary>
    [ObservableProperty]
    public partial TimeSpan? TimeSpanValue {get;set;}

    /// <summary>
    /// A collection of data points for creating charts/plots.
    /// </summary>
    [ObservableProperty]
    public partial List<ChartDataPoint>? PlotData {get;set;}


    /// <summary>A title for the statistic, making the object self-describing (e.g., "Your Busiest Listening Day").</summary>
    [ObservableProperty]
    public partial string? StatTitle { get; set; }

    /// <summary>A brief explanation of what the stat means.</summary>
    [ObservableProperty]
    public partial string? StatExplanation {get;set;}

    /// <summary>A value to compare the user against (e.g., a global average).</summary>
    [ObservableProperty]
    public partial double ComparisonValue { get; set; }

    /// <summary>The label for the comparison value (e.g., "Global Average").</summary>
    [ObservableProperty]
    public partial string? ComparisonLabel {get;set;}
 
    public double SecondaryValue { get; set; } // For more complex charts

    // For categorical or time-series data
    public DateTimeOffset Date { get; set; }
    public int DayOfYear { get; set; }
    public string Category { get; set; } = string.Empty;
    public int Low { get; internal set; }
    public int High { get; internal set; }
    public double HighDouble { get; internal set; }
    public double LowDouble { get; internal set; }
    public int Size { get; internal set; }
    public int Open { get; internal set; }
    public int Close { get; internal set; }

    public bool IsLongListeningDay { get; set; }

    public object? XValue { get; set; } // For numerical or DateTime X-Axes. Use this for XBindingPath.

    // For Numerical Y-Axes
    public double YValue { get; set; } // Primary Y-Axis value. Use this for YBindingPath.
public bool IsSummary { get; set; }      // For WaterfallSeries' SummaryBindingPath
    public List<SongModelView> ContributingSongs { get; internal set; } = new();
    public double SizeDouble { get; internal set; }
    public string? StatTitle2 { get; internal set; }
    public List<DimmerStats>? ChildStats { get; internal set; }
}
public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTimeOffset SortKey { get; set; } // Optional, for sorting time-series data
}