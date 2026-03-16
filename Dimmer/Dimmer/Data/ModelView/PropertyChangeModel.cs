using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Data.ModelView;


public partial class PropertyChangeModelView : ObservableObject
{
    public string PropertyName { get; set; }
    public string DisplayName { get; set; } // User-friendly name
    public object? OldValue { get; set; }
    public string OldValueDisplay => FormatValue(OldValue);
    public object? NewValue { get; set; }
    public string NewValueDisplay => FormatValue(NewValue);

    [ObservableProperty]
    public partial bool IsAccepted { get; set; }

    [ObservableProperty]
    public partial bool IsRejected { get; set;  }

    // Track if this was auto-accepted (like in "Accept All")
    public bool WasAutoAccepted { get; set; }


    // For UI sorting/grouping
    public string Category { get; set; } // "Basic Info", "Artists", "Album", etc.
    public int DisplayOrder { get; set; }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "<empty>",
            string str => string.IsNullOrEmpty(str) ? "<empty>" : str,
            DateTimeOffset dto => dto.ToString("g"),
            DateTime dt => dt.ToString("g"),
            bool b => b ? "Yes" : "No",
            int i when i == 0 => "0",
            double d when Math.Abs(d) < 0.01 => "0",
            Enum e => e.ToString(),
            _ => value.ToString() ?? "<unknown>"
        };
    }


}