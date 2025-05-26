using Dimmer.Utils.CustomShellUtils.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.TimeZoneInfo;

namespace Dimmer.Utils.CustomShellUtils.Models;
public class Transition
{
    public TransitionType CurrentPage { get; set; } = TransitionType.None;
    public TransitionType NextPage { get; set; } = TransitionType.None;

    public int DurationAndroid { get; set; } = 500;
    public Android.Views.Animations.Animation? CurrentPageAndroid { get; set; }
    public Android.Views.Animations.Animation? NextPageAndroid { get; set; }
}
