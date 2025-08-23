using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch.TQL.RealmSection;
/// <summary>
/// Represents the complete, executable plan for a TQL query, designed for the Realm database.
/// This replaces the in-memory ParsedQueryResult.
/// </summary>
public record RealmQueryPlan(
    string RqlFilter,
    IReadOnlyList<SortDescription> SortDescriptions,
    LimiterClause? Limiter,
    IQueryNode? CommandNode, 
    RandomChanceNode? ChanceNode,
    DaypartNode? DaypartNode,
    ShuffleNode? Shuffle,
    string? ErrorMessage = null,
    string? ErrorSuggestion = null
);



public record SavedQuery(string Name, string Tql);