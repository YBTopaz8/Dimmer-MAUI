
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

            int result;
            if (valueX is null && valueY is null)
                result = 0;
            else if (valueX is null)
                result = -1;
            else if (valueY is null)
                result = 1;
            else if (valueX is IComparable comparableX)
                result = comparableX.CompareTo(valueY);
            else
                result = string.Compare(valueX.ToString(), valueY.ToString(), StringComparison.OrdinalIgnoreCase);

            if (result != 0)
            {
                return desc.Direction == SortDirection.Ascending ? result : -result;
            }
        }
        return 0;
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
    public int Compare(SongModelView? x, SongModelView? y)
    {
        // Guid.NewGuid() is an excellent way to get a random, well-distributed sort key.
        return Guid.NewGuid().CompareTo(Guid.NewGuid());
    }
}