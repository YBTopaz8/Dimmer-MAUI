using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utils.PageAnimations;
public interface IAnimationService
{
    List<AnimationSetting> GetAvailablePageAnimations();
    AnimationSetting GetDefaultPushEnterAnimation();
    AnimationSetting GetDefaultPushExitAnimation();
    AnimationSetting GetDefaultPopEnterAnimation();
    AnimationSetting GetDefaultPopExitAnimation();

    // Specific for HomePage defaults
    AnimationSetting GetHomePagePushEnterAnimation();
    AnimationSetting GetHomePagePushExitAnimation();
    AnimationSetting GetHomePagePopEnterAnimation();
    AnimationSetting GetHomePagePopExitAnimation();
}

