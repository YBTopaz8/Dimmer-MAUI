using ReactiveUI;

namespace Dimmer.Data.ModelView; 
public class InteractiveChartPoint
{
    public object XValue { get; }
    public double YValue { get; }

    // We add these two optional properties for the pipeline to use
    internal ObservableAsPropertyHelper<int> ObservableCount { get; }
    internal int SortKey { get; }

    public InteractiveChartPoint(object xValue, double yValue)
    {
        XValue = xValue;
        YValue = yValue;
    }
    public InteractiveChartPoint(string label, double value, SongModelView song)
    {
        Label = label;
        Value = value;

        // Populate the rich data
        SongId = song.Id;
        ImagePath = song.CoverImagePath;
        FullSong = song;
    }
    // The new constructor our pipeline will use
    internal InteractiveChartPoint(object xValue, ObservableAsPropertyHelper<int> observableCount, int sortKey)
    {
        XValue = xValue;
        ObservableCount = observableCount;
        SortKey = sortKey;
        // The YValue will be set in the final transform
    }
    public string Label { get; }      // X-Axis: The song's title
    public double Value { get; }      // Y-Axis: The number of skips

    // --- RICH Data for YOU to USE ---
    public ObjectId SongId { get; }   // The ID for navigation or lookups
    public string ImagePath { get; }  // The cover art to show in a popup

    // We can even hold the full object if needed!
    public SongModelView FullSong { get; }
}