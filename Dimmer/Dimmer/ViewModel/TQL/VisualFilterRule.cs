using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dimmer.ViewModel.TQL;

public partial class VisualFilterRule : ObservableObject
{
    // The field alias (e.g., "ar", "g", "fav", "len")
    [ObservableProperty] public partial string FieldAlias { get; set; } = string.Empty;

    // The readable name (e.g., "Artist", "Genre", "Favorites")
    [ObservableProperty] public partial  string DisplayField { get; set; } = string.Empty;

    // The value (e.g., "Drake", "true", ">300")
    [ObservableProperty] public partial string Value { get; set; } = string.Empty;

    // 0 = Include (AND), 1 = Add (OR), 2 = Exclude (NOT)
    [ObservableProperty] public partial int LogicState { get; set; } = 0;

    // Used for DevExpress UI coloring (Green for AND, Blue for OR, Red for NOT)
    public Color ChipColor => LogicState switch
    {
        0 => Colors.DarkGreen,   // AND
        1 => Colors.DarkBlue,    // OR
        2 => Colors.DarkRed,     // NOT
        _ => Colors.Gray
    };

    public string LogicPrefix => LogicState switch
    {
        0 => "",         // AND is implicit in TQL
        1 => "add ",     // OR
        2 => "exclude ", // NOT
        _ => ""
    };

    // Generates the TQL snippet: e.g., "exclude ar:drake"
    public string ToTqlSnippet()
    {
        // Wrap values with spaces in quotes
        var safeValue = Value.Contains(" ") ? $"\"{Value}\"" : Value;
        return $"{LogicPrefix}{FieldAlias}:{safeValue}";
    }
}