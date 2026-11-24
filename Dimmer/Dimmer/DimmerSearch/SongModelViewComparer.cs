using System.Runtime.CompilerServices;

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

public enum TQLSortDirection { Ascending, Descending, Random }

public class TQLSortDescription
{
    public FieldDefinition Field { get; }
    public TQLSortDirection Direction { get; }
    public string PropertyName => Field.PropertyName;
    public TQLSortDescription(FieldDefinition fieldDefinition, TQLSortDirection direction)
    {
        Field = fieldDefinition;
        Direction = direction;
    }
}

public partial class SongModelComparer : IComparer<SongModel>
{
    public IReadOnlyList<TQLSortDescription> SortDescriptions { get; }

    public SongModelComparer(List<TQLSortDescription>? descriptions)
    {
        // This constructor intentionally ignores any 'Random' sort descriptions.
        // The responsibility for random sorting belongs to the ViewModel's data pipeline.
        SortDescriptions = descriptions?.Where(d => d.Direction != TQLSortDirection.Random).ToList()
                           ?? new List<TQLSortDescription>();
    }

    public int Compare(SongModel? x, SongModel? y)
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
                return desc.Direction == TQLSortDirection.Ascending ? result : -result;
            }
        }

        // As a final tie-breaker, sort by title to ensure a stable sort order.
        return string.Compare(x.Title, y.Title, StringComparison.OrdinalIgnoreCase);
    }

    public SongModelComparer Inverted()
    {
        var invertedDescriptions = SortDescriptions.Select(desc =>
        {
            var invertedDirection = desc.Direction == TQLSortDirection.Ascending
                ? TQLSortDirection.Descending
                : TQLSortDirection.Ascending;
            return new TQLSortDescription(desc.Field, invertedDirection);
        }).ToList();
        return new SongModelComparer(invertedDescriptions);
    }
}

public partial class RandomSongComparer : IComparer<SongModel>
{
    private readonly int _seed = Random.Shared.Next();

    public int Compare(SongModel? x, SongModel? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        int idX = RuntimeHelpers.GetHashCode(x);
        int idY = RuntimeHelpers.GetHashCode(y);

        int keyX = HashCode.Combine(_seed, idX);
        int keyY = HashCode.Combine(_seed, idY);

        int cmp = keyX.CompareTo(keyY);
        return cmp != 0 ? cmp : idX.CompareTo(idY);
    }
}