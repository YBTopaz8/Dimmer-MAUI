using Dimmer.Data.ModelView.DimmerSearch;
using Dimmer.DimmerSearch.TQL;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.WinUI.ViewModel;



public partial class ActiveFilterViewModel(string field, string displayText, string tqlClause, IRelayCommand<ActiveFilterViewModel> removeCommand) : IQueryComponentViewModel
{
    public string Field { get; } = field;
    public string DisplayText { get; } = displayText;
    public string TqlClause { get; } = tqlClause;
    public IRelayCommand<ActiveFilterViewModel> RemoveCommand { get; } = removeCommand;
}


public partial class LogicalJoinerViewModel(Action onToggled) : IQueryComponentViewModel
{
    public LogicalOperator Operator { get; private set; } = LogicalOperator.And;
    public string OperatorText => Operator.ToString().ToUpper();

    [RelayCommand]
    private void ToggleOperator()
    {
        Operator = (Operator == LogicalOperator.And) ? LogicalOperator.Or : LogicalOperator.And;
        onToggled?.Invoke(); // Notify the parent VM to rebuild the query
    }
}