using Microsoft.UI.Xaml.Media.Animation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.WinUI.Utils;
public interface IAnimationService
{
    // Gets all animations found in the Android project's resources
    List<AnimationSetting> GetAvailableAnimations();

    // Provides the ultimate fallback defaults
    AnimationSetting GetDefaultPushEnterAnimation();
    AnimationSetting GetDefaultPushExitAnimation();
    AnimationSetting GetDefaultPopEnterAnimation();
    AnimationSetting GetDefaultPopExitAnimation();

    // Special case for HomePage
    AnimationSetting GetHomePagePushEnterAnimation();
    AnimationSetting GetHomePagePushExitAnimation();
    AnimationSetting GetHomePagePopEnterAnimation();
    AnimationSetting GetHomePagePopExitAnimation();
}


public class AnimationSetting
{
    public string DisplayName { get; set; }
    public int ResourceId { get; set; } // Android Resource ID
    public NavigationTransitionInfo TransitionInfo { get; internal set; }
}
