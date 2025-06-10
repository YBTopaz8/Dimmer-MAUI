using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utils.PageAnimations;

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