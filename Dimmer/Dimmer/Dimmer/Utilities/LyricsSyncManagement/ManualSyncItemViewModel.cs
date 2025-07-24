using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.LyricsSyncManagement;
public class ManualSyncItemViewModel : ObservableObject
{
    public string Text { get; set; }
    [ObservableProperty] private TimeSpan? _timestamp;
}