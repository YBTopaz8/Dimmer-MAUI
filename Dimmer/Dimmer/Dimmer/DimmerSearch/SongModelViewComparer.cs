using Dimmer.DimmerSearch.AbstractQueryTree.NL;

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch;

public enum LimiterType { First, Last, Random }

public class LimiterClause
{
    public LimiterType Type { get; set; }
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
    public string PropertyName { get; }
    public SortDirection Direction { get; }
    public Func<SongModelView, object> Accessor { get; }

    public FieldDefinition Field { get; }
    public SortDescription(FieldDefinition field, SortDirection direction)
    {
        Field = field;
        Direction = direction;
        if (Field is not null)
        {

            Accessor = Field.PropertyExpression.Compile();

        }
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
            // For a random sort, we ignore all other sort descriptions.
            // We get or create a stable Guid for each song and compare those.
            // This is consistent and satisfies the IComparer contract.
            var xGuid = _randomSortKeys.GetOrAdd(x, _ => Guid.NewGuid());
            var yGuid = _randomSortKeys.GetOrAdd(y, _ => Guid.NewGuid());
            return xGuid.CompareTo(yGuid);
        }

        foreach (var desc in _sortDescriptions)
        {
            var valueX = desc.Accessor(x!);
            var valueY = desc.Accessor(y!);
            // --- End of Change ---

            int result;
            if (valueX is IComparable comparableX)
            {
                result = comparableX.CompareTo(valueY);
            }
            else
            {
                result = string.Compare(valueX?.ToString(), valueY?.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            if (result != 0)
            {
                return desc.Direction == SortDirection.Ascending ? result : -result;
            }
        }
        return 0;
    }
}