using Dimmer.Utils.CustomShellUtils.Enums;

namespace Dimmer.Utils.CustomShellUtils.Models;
public class Transition
{
    public TransitionType CurrentPage { get; set; } = TransitionType.FadeOut;
    public TransitionType NextPage { get; set; } = TransitionType.FadeIn;

    public int DurationAndroid { get; set; } = 500;
    public Android.Views.Animations.Animation? CurrentPageAndroid { get; set; }
    public Android.Views.Animations.Animation? NextPageAndroid { get; set; }
}
