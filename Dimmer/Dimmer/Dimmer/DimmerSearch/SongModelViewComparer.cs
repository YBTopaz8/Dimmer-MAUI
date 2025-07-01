using System;
using System.Collections;
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
    public SortDescription(string propertyName, SortDirection direction)
    {
        PropertyName = propertyName;
        Direction = direction;
    }
}

// A full implementation of the comparer class needed by the pipeline
public class SongModelViewComparer : IComparer<SongModelView>
{
    private readonly List<SortDescription> _sortDescriptions;
    private readonly bool _isRandomSort;
    private readonly Random _random = new();

    public SongModelViewComparer(List<SortDescription>? descriptions)
    {
        _sortDescriptions = descriptions ?? new List<SortDescription>();
        // Check if random/shuffle was the primary sort mode
        _isRandomSort = _sortDescriptions.Any(d => d.PropertyName == "Random");
        if (_isRandomSort)
        {
            _sortDescriptions.Clear(); // Random sort overrides everything
        }
    }

    public int Compare(SongModelView? x, SongModelView? y)
    {
        if (x is null && y is null)
            return 0;
        if (x is null)
            return 1; // Nulls go to the end
        if (y is null)
            return -1;

        if (_isRandomSort)
        {
            return _random.Next(-1, 2); // A simple way to shuffle
        }

        foreach (var desc in _sortDescriptions)
        {
            var propInfo = typeof(SongModelView).GetProperty(desc.PropertyName);
            if (propInfo == null)
                continue; // Skip if property doesn't exist

            var valueX = propInfo.GetValue(x);
            var valueY = propInfo.GetValue(y);

            int result;
            if (valueX is IComparable comparableX)
            {
                result = comparableX.CompareTo(valueY);
            }
            else // Fallback to string comparison
            {
                result = string.Compare(valueX?.ToString(), valueY?.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            if (result != 0)
            {
                return desc.Direction == SortDirection.Ascending ? result : -result;
            }
        }
        return 0; // They are equal according to all criteria
    }
}