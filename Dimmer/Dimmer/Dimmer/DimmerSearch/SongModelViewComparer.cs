using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch;
public class SongModelViewComparer : IComparer<SongModelView>
{



    private readonly List<SortClause> _sortDirectives;

    // This dictionary will hold the random "sort key" for each song.
    // We use a special object to ensure it's unique to this specific sort operation.
    private readonly object _randomSortSessionKey = new();

    public SongModelViewComparer(List<SortClause>? sortDirectives)
    {
        _sortDirectives = sortDirectives ?? new List<SortClause>();

        // If there are no sort directives, default to sorting by title
        if (_sortDirectives.Count == 0)
        {
            _sortDirectives.Add(new SortClause { FieldName = "Title", Direction = SortDirection.Ascending });
        }
    }

    public int Compare(SongModelView? x, SongModelView? y)
    {
        if (x == null && y == null)
            return 0;
        if (x == null)
            return -1;
        if (y == null)
            return 1;

        // --- NEW: Special handling for Random sort ---
        // If the primary sort is Random, we use a different logic.
        if (_sortDirectives[0].Direction == SortDirection.Random)
        {
            // We can't use a simple new Random().Next() because the sort algorithm
            // needs a consistent value for each item during the sort pass.
            // We use a trick with GetHashCode for pseudo-randomness that is stable
            // for the lifetime of the object, but we mix it with our session key
            // to ensure a DIFFERENT random order each time the user types "shuffle".
            int xRandomKey = (x.GetHashCode() ^ _randomSortSessionKey.GetHashCode());
            int yRandomKey = (y.GetHashCode() ^ _randomSortSessionKey.GetHashCode());
            return xRandomKey.CompareTo(yRandomKey);
        }
        // --- End of new logic ---

        // The existing logic for standard sorting
        foreach (var directive in _sortDirectives)
        {
            IComparable? xValue = SemanticQueryHelpers.GetComparableProp(x, directive.FieldName);
            IComparable? yValue = SemanticQueryHelpers.GetComparableProp(y, directive.FieldName);

            int comparison = Comparer.Default.Compare(xValue, yValue);

            if (comparison != 0)
            {
                // If descending, just flip the result
                return directive.Direction == SortDirection.Descending ? -comparison : comparison;
            }
        }

        // If all sort criteria are equal, they are considered equal in sort order
        return 0;
    }
}