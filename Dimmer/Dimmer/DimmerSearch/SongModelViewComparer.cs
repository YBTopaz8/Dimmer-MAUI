
using Dimmer.DimmerSearch;
using Dimmer.DimmerSearch.TQL;

public enum LimiterType { First, Last, Random }

public class LimiterClause
{
    public LimiterType Type { get; set; } = LimiterType.Random;
    public int Count { get; set; }
    public LimiterClause(LimiterType type, int count)
    {
        Type = type;
        Count = count;
    }
    public LimiterClause() { }
}

public enum SortDirection { Ascending, Descending, Random }

public class SortDescription
{
    public FieldDefinition Field { get; }
    public SortDirection Direction { get; }
    public string PropertyName => Field.PropertyName;
    public SortDescription(FieldDefinition field, SortDirection direction)
    {
        Field = field;
        Direction = direction;
    }
}

public class SongModelViewComparer : IComparer<SongModelView>
{
    public IReadOnlyList<SortDescription> SortDescriptions { get; }

    public SongModelViewComparer(List<SortDescription>? descriptions)
    {
        // This constructor intentionally ignores any 'Random' sort descriptions.
        // The responsibility for random sorting belongs to the ViewModel's data pipeline.
        SortDescriptions = descriptions?.Where(d => d.Direction != SortDirection.Random).ToList()
                           ?? new List<SortDescription>();
    }

    public int Compare(SongModelView? x, SongModelView? y)
    {
        if (x is null && y is null)
            return 0;
        if (x is null)
            return 1;
        if (y is null)
            return -1;

        foreach (var desc in SortDescriptions)
        {
            var valueX = SemanticQueryHelpers.GetComparableProp(x, desc.PropertyName);
            var valueY = SemanticQueryHelpers.GetComparableProp(y, desc.PropertyName);

            int result = Comparer<IComparable>.Default.Compare(valueX, valueY);

            if (result != 0)
            {
                // Ternary expression is cleaner here
                return desc.Direction == SortDirection.Ascending ? result : -result;
            }
        }

        // As a final tie-breaker, sort by title to ensure a stable sort order.
        return string.Compare(x.Title, y.Title, StringComparison.OrdinalIgnoreCase);
    }

    public SongModelViewComparer Inverted()
    {
        var invertedDescriptions = SortDescriptions.Select(desc =>
        {
            var invertedDirection = desc.Direction == SortDirection.Ascending
                ? SortDirection.Descending
                : SortDirection.Ascending;
            return new SortDescription(desc.Field, invertedDirection);
        }).ToList();
        return new SongModelViewComparer(invertedDescriptions);
    }
}

public class RandomSongComparer : IComparer<SongModelView>
{
    private readonly Random _random = new();

    public int Compare(SongModelView? x, SongModelView? y)
    {
        if (x == null && y == null)
            return 0;
        if (x == null)
            return -1;
        if (y == null)
            return 1;

        // Use a more efficient random comparison
        return _random.Next(-1, 2); // Returns -1, 0, or 1
    }
}