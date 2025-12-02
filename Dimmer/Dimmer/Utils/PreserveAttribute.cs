using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utils;

[AttributeUsage(AttributeTargets.All)]
[EditorBrowsable(EditorBrowsableState.Never)]
public class PreserveAttribute : Attribute
{
    public bool AllMembers;
    public bool Conditional;
}