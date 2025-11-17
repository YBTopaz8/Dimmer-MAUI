using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.WinUI.Utils.CustomHandlers;

public class MyColumnFilterHandler : ColumnFilterHandler
{
    private readonly Dictionary<TableViewColumn, HashSet<object?>> _lastSelectedValues = new();

    public MyColumnFilterHandler(global::WinUI.TableView.TableView tableView) : base(tableView)
    {
    }

    public event Action<TableViewColumn?, object?, bool>? FilterChanged;
    // bool = true if added, false if removed

    public override void ApplyFilter(TableViewColumn column)
    {
        // Get previous state
        _lastSelectedValues.TryGetValue(column, out var previous);
        previous ??= new HashSet<object?>();

        // Current selection
        SelectedValues.TryGetValue(column, out var currentList);
        var current = currentList != null ? new HashSet<object?>(currentList) : new HashSet<object?>();

        // Detect added
        foreach (var added in current.Except(previous))
            FilterChanged?.Invoke(column, added, true);

        // Detect removed
        foreach (var removed in previous.Except(current))
            FilterChanged?.Invoke(column, removed, false);

        // Update last state
        _lastSelectedValues[column] = current;

        // Call base logic
        base.ApplyFilter(column);
    }
}