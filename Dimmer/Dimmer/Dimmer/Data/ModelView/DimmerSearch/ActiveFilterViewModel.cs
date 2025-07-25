﻿using CommunityToolkit.Mvvm.Input;

using Dimmer.DimmerSearch.TQL;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using static SkiaSharp.SKPath;

namespace Dimmer.Data.ModelView.DimmerSearch;
public partial class ActiveFilterViewModel : ObservableObject, IQueryComponentViewModel
{
    /// <summary>
    /// The TQL field name (e.g., "fav", "year"). Used for uniqueness checks.
    /// </summary>
    public string Field { get; }

    /// <summary>
    /// The user-friendly text displayed on the chip (e.g., "Is Favorite", "Year > 2000").
    /// </summary>
    public string DisplayText { get; }

    /// <summary>
    /// The actual TQL clause this filter generates (e.g., "fav:true").
    /// </summary>
    public string TqlClause { get; }

    /// <summary>
    /// A command to remove this specific filter from the active list.
    /// </summary>
    public ICommand RemoveCommand { get; }

    public ActiveFilterViewModel(string field, string displayText, string tqlClause, Action<ActiveFilterViewModel> onRemove)
    {
        Field = field;
        DisplayText = displayText;
        TqlClause = tqlClause;
        RemoveCommand = new RelayCommand(() => onRemove(this));
    }


}

public partial class LogicalJoinerViewModel : ObservableObject, IQueryComponentViewModel
{
    [ObservableProperty]
    public partial LogicalOperator Operator { get; set; } // Your existing LogicalOperator enum

    public ICommand ToggleOperatorCommand { get; }

    public LogicalJoinerViewModel()
    {
        Operator = LogicalOperator.And;
        ToggleOperatorCommand = new RelayCommand(() => Operator = Operator == LogicalOperator.And ? LogicalOperator.Or : LogicalOperator.And);
    }
}