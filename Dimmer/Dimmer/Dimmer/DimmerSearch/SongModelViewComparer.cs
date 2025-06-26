using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch;
public class SongModelViewComparer : IComparer<SongModelView>
{
    private readonly List<(string FieldName, SortDirection Direction)> _sortFields;

    public SongModelViewComparer(List<SortClause> sortDirectives)
    {
        // If no sort is specified, create a default to prevent errors.
        if (sortDirectives == null || sortDirectives.Count == 0)
        {
            _sortFields = new List<(string, SortDirection)> { ("Title", SortDirection.Ascending) };
        }
        else
        {
            _sortFields = new List<(string, SortDirection)>();
            foreach (var directive in sortDirectives)
            {
                _sortFields.Add((directive.FieldName, directive.Direction));
            }
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

        foreach (var (fieldName, direction) in _sortFields)
        {
            // Use our robust helper to get the values to compare
            IComparable? valueX = SemanticQueryHelpers.GetComparableProp(x, fieldName);
            IComparable? valueY = SemanticQueryHelpers.GetComparableProp(y, fieldName);

            int result = Comparer<IComparable>.Default.Compare(valueX, valueY);

            if (result != 0)
            {
                // If descending, invert the result
                return direction == SortDirection.Descending ? -result : result;
            }
        }

        // If all sort fields are equal, the items are considered equal in sort order
        return 0;
    }
}