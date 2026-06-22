using System;
using System.Collections.Generic;
using System.Text;



namespace Dimmer.ViewModel.TQL;

public partial class TqlBuilderViewModel : ObservableObject
{
    private readonly BaseViewModel _baseVM;
    private readonly AutocompleteEngine _autocompleteEngine;

    [ObservableProperty]
    public partial string SearchQueryText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<string> SearchSuggestions { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<VisualFilterRule> VisualFilters { get; set; } = new();

    // Data sources for Autocomplete (Populate these from _baseVM's repos)
    public ObservableCollection<string> LiveArtists { get; set; } = new();
    public ObservableCollection<string> LiveAlbums { get; set; } = new();
    public ObservableCollection<string> LiveGenres { get; set; } = new();

    public TqlBuilderViewModel(BaseViewModel baseVM, AutocompleteEngine autocompleteEngine)
    {
        _baseVM = baseVM;
        _autocompleteEngine = autocompleteEngine;

        // Optional: Pre-populate live sources here or via an Init method
        // LiveArtists = new ObservableCollection<string>(...);
    }

    // --- 1. AUTOCOMPLETE PIPELINE ---

    public void RequestSuggestions(string text, int cursorPosition)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            SearchSuggestions.Clear();
            return;
        }

        var newSuggestions = AutocompleteEngine.GetSuggestions(
            LiveArtists, LiveAlbums, LiveGenres, text, cursorPosition);

        SearchSuggestions.Clear();
        foreach (var s in newSuggestions) SearchSuggestions.Add(s);
    }

    public void AcceptSuggestion(string suggestion, string currentText, int cursorPosition)
    {
        int wordStart = currentText.LastIndexOf(' ', Math.Max(0, cursorPosition - 1)) + 1;
        string before = currentText.Substring(0, wordStart);
        string after = currentText.Substring(cursorPosition);

        SearchQueryText = $"{before}{suggestion} {after}".Trim();
        SearchSuggestions.Clear();

        ExecuteSearch();
    }

    // --- 2. EXECUTION & TWO-WAY SYNC ---

    [RelayCommand]
    public void ExecuteSearch()
    {
        // 1. Tell BaseViewModel to do the heavy lifting
        _baseVM.SearchToTQL(SearchQueryText);

        // 2. Reverse Sync: Parse the typed text back into Visual Chips
        SyncTextToChips();
    }

    private void SyncTextToChips()
    {
        VisualFilters.Clear();
        if (string.IsNullOrWhiteSpace(SearchQueryText)) return;

        try
        {
            // Use your existing parser!
            var astNode = new AstParser(SearchQueryText).Parse();
            var extractedChips = FlattenAstToChips(astNode);

            foreach (var chip in extractedChips)
            {
                VisualFilters.Add(chip);
            }
        }
        catch
        {
            // If the query is mid-typing or invalid, don't crash, just don't update chips.
        }
    }

    private void SyncChipsToText()
    {
        if (VisualFilters.Count == 0)
        {
            SearchQueryText = string.Empty;
        }
        else
        {
            SearchQueryText = string.Join(" ", VisualFilters.Select(v => v.ToTqlSnippet()));
        }

        _baseVM.SearchToTQL(SearchQueryText);
    }

    // --- 3. CHIP MANAGEMENT ---

    [RelayCommand]
    public void AddVisualFilter(VisualFilterRule rule)
    {
        VisualFilters.Add(rule);
        SyncChipsToText();
    }

    [RelayCommand]
    public void RemoveVisualFilter(VisualFilterRule rule)
    {
        VisualFilters.Remove(rule);
        SyncChipsToText();
    }

    // Recursive helper to turn AST back into Visual UI models
    private List<VisualFilterRule> FlattenAstToChips(IQueryNode node, int currentLogicState = 0)
    {
        var chips = new List<VisualFilterRule>();

        switch (node)
        {
            case LogicalNode logicNode:
                int leftLogic = currentLogicState;
                int rightLogic = logicNode.Operator == LogicalOperator.Or ? 1 : 0; // 1 = Add(OR), 0 = Include(AND)

                chips.AddRange(FlattenAstToChips(logicNode.Left, leftLogic));
                chips.AddRange(FlattenAstToChips(logicNode.Right, rightLogic));
                break;

            case NotNode notNode:
                chips.AddRange(FlattenAstToChips(notNode.NodeToNegate, 2)); // 2 = Exclude(NOT)
                break;

            case ClauseNode clauseNode:
                if (clauseNode.Operator == "matchall") break; // Ignore implicit matchalls

                // Lookup nice name
                string displayName = FieldRegistry.FieldsByAlias.TryGetValue(clauseNode.Field, out var def)
                    ? def.PrimaryName : clauseNode.Field;

                chips.Add(new VisualFilterRule
                {
                    FieldAlias = clauseNode.Field,
                    DisplayField = displayName,
                    Value = clauseNode.Value.ToString() ?? "",
                    LogicState = clauseNode.IsNegated ? 2 : currentLogicState
                });
                break;
        }
        return chips;
    }
}