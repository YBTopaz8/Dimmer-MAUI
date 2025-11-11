using Android.Views.Animations;

using Dimmer.Utils.CustomShellUtils.Enums;
using Dimmer.Utils.CustomShellUtils.Models;

namespace Dimmer.Utils;
public static class HelperConverter
{
    private static Transitions _transitions = new Transitions();
    public static ConfigAndroid GetRoot()
    {





        var config = new ConfigAndroid();
        var animation = _transitions;
        config.AbovePage = animation.Root.AbovePage;
        config.Duration = animation.Root.DurationAndroid;

        config.AnimationIn = animation.Root.NextPageAndroid is null
        ? GetAnimation(animation.Root.NextPage)
        : animation.Root.NextPageAndroid;

        config.AnimationOut = animation.Root.CurrentPageAndroid is null
        ? GetAnimation(animation.Root.CurrentPage)
        : animation.Root.CurrentPageAndroid;

        return config;
    }

    public static ConfigAndroid GetPush()
    {
        var config = new ConfigAndroid();
        var animation = _transitions;

        config.AnimationIn = animation.Push.NextPageAndroid is null
        ? GetAnimation(animation.Push.NextPage)
        : animation.Push.NextPageAndroid;

        config.AnimationOut = animation.Push.CurrentPageAndroid is null
        ? GetAnimation(animation.Push.CurrentPage)
        : animation.Push.CurrentPageAndroid;

        return config;
    }

    public static ConfigAndroid GetPop()
    {
        var config = new ConfigAndroid();
        Transitions? animation = _transitions;

        config.AnimationIn = animation.Pop.NextPageAndroid is null
        ? GetAnimation(animation.Pop.NextPage)
        : animation.Pop.NextPageAndroid;

        config.AnimationOut = animation.Pop.CurrentPageAndroid is null
        ? GetAnimation(animation.Pop.CurrentPage)
        : animation.Pop.CurrentPageAndroid;

        return config;
    }

    private static Android.Views.Animations.Animation? GetAnimation(TransitionType anim)
    {
        int animRes = 0;
        switch (anim)
        {
            case TransitionType.FadeIn:
                animRes = Resource.Animation.m3_motion_fade_enter;
                break;
            case TransitionType.FadeOut:
                animRes = Resource.Animation.m3_motion_fade_exit;
                break;
            case TransitionType.BottomIn:
                animRes = Resource.Animation.m3_bottom_sheet_slide_in;
                break;
            case TransitionType.BottomOut:
                animRes = Resource.Animation.m3_bottom_sheet_slide_out;
                break;
            case TransitionType.TopIn:
                animRes = Resource.Animation.abc_slide_in_top;
                break;
            case TransitionType.TopOut:
                animRes = Resource.Animation.abc_slide_out_top;
                break;
            case TransitionType.LeftIn:
                animRes = Resource.Animation.m3_side_sheet_enter_from_left;
                break;
            case TransitionType.LeftOut:
                animRes = Resource.Animation.m3_side_sheet_exit_to_left;
                break;
            case TransitionType.RightIn:
                animRes = Resource.Animation.m3_side_sheet_enter_from_right;
                break;
            case TransitionType.RightOut:
                animRes = Resource.Animation.m3_side_sheet_exit_to_right;
                break;
            case TransitionType.ScaleIn:
            case TransitionType.ScaleOut:
            case TransitionType.None:
            default:
                animRes = Resource.Animation.m3_motion_fade_enter;
                break;
        }

        var animObj = AnimationUtils.LoadAnimation(Platform.AppContext, animRes);

        // Example: always use fast_out_slow_in for modern feel
        var interpolator = AnimationUtils.LoadInterpolator(Platform.AppContext, Android.Resource.Interpolator.Bounce);
        animObj.Interpolator = interpolator;

        return animObj;
    }

    public class ConfigAndroid
    {
        public Android.Views.Animations.Animation? AnimationIn;
        public Android.Views.Animations.Animation? AnimationOut;
        public PageType AbovePage;
        public int Duration;

    }

}