using Dimmer.DimmerSearch.TQL;

using System.Collections.Concurrent;

namespace Dimmer.DimmerSearch;

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
    public LimiterClause()
    {

    }
}

// For the Sorter
public enum SortDirection { Ascending, Descending, Random }

public class SortDescription
{
    public FieldDefinition Field { get; }
    public SortDirection Direction { get; }
    public string PropertyName => Field.PrimaryName;
    public SortDescription(FieldDefinition field, SortDirection direction)
    {
        Field = field;
        Direction = direction;
      
    }
}

// A full implementation of the comparer class needed by the pipeline
public class SongModelViewComparer : IComparer<SongModelView>
{

    public IReadOnlyList<SortDescription> SortDescriptions => _sortDescriptions;
    private readonly List<SortDescription> _sortDescriptions;
    private readonly bool _isRandomSort = new();
    private readonly ConcurrentDictionary<SongModelView, Guid> _randomSortKeys = new();


    // The constructor is now much simpler.
    public SongModelViewComparer(List<SortDescription>? descriptions)
    {
        _sortDescriptions = descriptions ?? new List<SortDescription>();
        _isRandomSort = _sortDescriptions.Any(d => d.Direction == SortDirection.Random);
    }

    public int Compare(SongModelView? x, SongModelView? y)
    {
        if (x is null && y is null)
            return 0;
        if (x is null)
            return 1;
        if (y is null)
            return -1;
        if (_isRandomSort)
        {

            var xGuid = _randomSortKeys.GetOrAdd(x, _ => Guid.NewGuid());
            var yGuid = _randomSortKeys.GetOrAdd(y, _ => Guid.NewGuid());
            return xGuid.CompareTo(yGuid);
        }

        foreach (var desc in _sortDescriptions)
        {
            var valueX = SemanticQueryHelpers.GetComparableProp(x!, desc.PropertyName);
            var valueY = SemanticQueryHelpers.GetComparableProp(y!, desc.PropertyName);
            // --- End of Change ---

            int result;
            if (valueX is null && valueY is null)
            {
                result = 0;
            }
            else if (valueX is null)
            {
                result = -1; // Nulls are usually considered "less than" non-nulls
            }
            else if (valueY is null)
            {
                result = 1;
            }
            else if (valueX is IComparable comparableX)
            {
                result = comparableX.CompareTo(valueY);
            }
            else
            {
                result = string.Compare(valueX.ToString(), valueY.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            // If we found a difference on this level, we're done. Return the result.
            if (result != 0)
            {
                return desc.Direction == SortDirection.Ascending ? result : -result;
            }
        }
        return 0;
    }


    public SongModelViewComparer Inverted()
    {
        var invertedDescriptions = _sortDescriptions.Select(desc =>
        {
            // For random sort, the direction doesn't matter, keep it as is.
            if (desc.Direction == SortDirection.Random)
                return desc;

            // Invert Ascending to Descending and vice-versa.
            var invertedDirection = desc.Direction == SortDirection.Ascending
                ? SortDirection.Descending
                : SortDirection.Ascending;

            return new SortDescription(desc.Field, invertedDirection);

        }).ToList();

        return new SongModelViewComparer(invertedDescriptions);
    }
}