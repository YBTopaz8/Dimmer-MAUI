using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Data.ModelView;

public partial class AppLogEntryView : ObservableObject
{

        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

        public string Category { get; set; } = string.Empty;

        public string Operation { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string LevelStr { get; set; } = DimmerLogLevel.Info.ToString();

        public int? ProgressValue { get; set; }
        public int? ProgressTotal { get; set; }

        public string ContextData { get; set; } = string.Empty;

        public string? ExceptionTrace { get; set; } // For Errors
        public string CorrelationId { get; set; } = string.Empty; // To group async steps together
      
        public string Id { get; set; }
}

