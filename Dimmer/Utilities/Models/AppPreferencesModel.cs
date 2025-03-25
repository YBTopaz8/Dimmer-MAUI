using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer_MAUI.Utilities.Models;
public partial class AppPreferencesModel : RealmObject
{
    public bool ShowCloseConfirmation { get; set; }
    public double Volume { get; set; }
    public bool IsConnected { get; set; }
}
