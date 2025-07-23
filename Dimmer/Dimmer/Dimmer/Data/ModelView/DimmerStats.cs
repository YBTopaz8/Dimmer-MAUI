﻿namespace Dimmer.Data.ModelView;
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
    public partial int NumberOfTracks { get; set; }
    [ObservableProperty]
    public partial string? TotalSeconds { get; set; }
    [ObservableProperty]
    public partial string? Name { get; set; }
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

    /// <summary>
    /// A title for the statistic, making the object self-describing.
    /// </summary>
    [ObservableProperty]
    public partial string? StatTitle {get;set;}
  
    public double SecondaryValue { get; set; } // For more complex charts

    // For categorical or time-series data
    public DateTimeOffset Date { get; set; }
    public string Category { get; set; } = string.Empty;
    public int Low { get; internal set; }
    public int High { get; internal set; }
    public double HighDouble { get; internal set; }
    public double LowDouble { get; internal set; }
    public int Size { get; internal set; }
    public int Open { get; internal set; }
    public int Close { get; internal set; }
}
public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTimeOffset SortKey { get; set; } // Optional, for sorting time-series data
}