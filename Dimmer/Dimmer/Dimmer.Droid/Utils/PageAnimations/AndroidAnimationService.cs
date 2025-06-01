using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utils.PageAnimations;
public partial class AndroidAnimationService : IAnimationService
{
    // Define your default animations here based on your Resource.Animation values
    private readonly AnimationSetting _defaultPushEnter = new AnimationSetting { DisplayName = "Slide In Right (Default)", ResourceId = Resource.Animation.m3_motion_fade_enter };
    private readonly AnimationSetting _defaultPushExit = new AnimationSetting { DisplayName = "Slide Out Left (Default)", ResourceId = Resource.Animation.m3_motion_fade_exit };
    private readonly AnimationSetting _defaultPopEnter = new AnimationSetting { DisplayName = "Slide In Left (Default)", ResourceId = Resource.Animation.nav_default_enter_anim };
    private readonly AnimationSetting _defaultPopExit = new AnimationSetting { DisplayName = "Slide Out Right (Default)", ResourceId = Resource.Animation.nav_default_exit_anim };

    private readonly AnimationSetting _homePagePushEnter = new AnimationSetting { DisplayName = "Home: Slide Down In", ResourceId = Resource.Animation.mtrl_bottom_sheet_slide_out };
    private readonly AnimationSetting _homePagePushExit = new AnimationSetting { DisplayName = "Home: Slide Up Out", ResourceId = Resource.Animation.mtrl_bottom_sheet_slide_in };
    private readonly AnimationSetting _homePagePopEnter = new AnimationSetting { DisplayName = "Home: Slide Down In (Pop)", ResourceId = Resource.Animation.mtrl_bottom_sheet_slide_out };
    private readonly AnimationSetting _homePagePopExit = new AnimationSetting { DisplayName = "Home: Slide Up Out (Pop)", ResourceId = Resource.Animation.mtrl_bottom_sheet_slide_in };
    public List<AnimationSetting> GetAvailablePageAnimations()
    {
        // Populate this list with all the Resource.Animation values you want to expose
        return new List<AnimationSetting>
        {
            new AnimationSetting { DisplayName = "None", ResourceId = Resource.Animation.none }, // Important to have a "none"
            new AnimationSetting { DisplayName = "Fade In/Out", ResourceId = Resource.Animation.abc_fade_in }, // Note: you'll need separate In/Out
            new AnimationSetting { DisplayName = "Slide In From Bottom", ResourceId = Resource.Animation.abc_slide_in_bottom },
            new AnimationSetting { DisplayName = "Slide Out To Bottom", ResourceId = Resource.Animation.abc_slide_out_bottom },
            new AnimationSetting { DisplayName = "Slide In From Top", ResourceId = Resource.Animation.abc_slide_in_top },
            new AnimationSetting { DisplayName = "Slide Out To Top", ResourceId = Resource.Animation.abc_slide_out_top },
            new AnimationSetting { DisplayName = "Slide In From Right", ResourceId = Resource.Animation.m3_side_sheet_enter_from_right },
            new AnimationSetting { DisplayName = "Slide Out To Left", ResourceId = Resource.Animation.m3_side_sheet_exit_to_right },
            new AnimationSetting { DisplayName = "Slide In From Left", ResourceId = Resource.Animation.m3_side_sheet_enter_from_right },
            new AnimationSetting { DisplayName = "Slide Out To Right", ResourceId = Resource.Animation.m3_side_sheet_exit_to_right },
            new AnimationSetting { DisplayName = "Material Bottom Sheet Slide In (Up)", ResourceId = Resource.Animation.mtrl_bottom_sheet_slide_in },
            new AnimationSetting { DisplayName = "Material Bottom Sheet Slide Out (Down)", ResourceId = Resource.Animation.mtrl_bottom_sheet_slide_out },
            new AnimationSetting { DisplayName = "Material3 Fade Enter", ResourceId = Resource.Animation.m3_motion_fade_enter },
            new AnimationSetting { DisplayName = "Material3 Fade Exit", ResourceId = Resource.Animation.m3_motion_fade_exit },
            // ... Add all relevant animations from your list
        };
    }

    public AnimationSetting GetDefaultPushEnterAnimation()
    {
        return _defaultPushEnter;
    }

    public AnimationSetting GetDefaultPushExitAnimation()
    {
        return _defaultPushExit;
    }

    public AnimationSetting GetDefaultPopEnterAnimation()
    {
        return _defaultPopEnter;
    }

    public AnimationSetting GetDefaultPopExitAnimation()
    {
        return _defaultPopExit;
    }

    public AnimationSetting GetHomePagePushEnterAnimation()
    {
        return _homePagePushEnter;
    }

    public AnimationSetting GetHomePagePushExitAnimation()
    {
        return _homePagePushExit;
    }

    public AnimationSetting GetHomePagePopEnterAnimation()
    {
        return _homePagePopEnter;
    }

    public AnimationSetting GetHomePagePopExitAnimation()
    {
        return _homePagePopExit;
    }

    public List<AnimationSetting> GetAllDefaultAnimations()
    {
        return new List<AnimationSetting>
        {
            GetDefaultPushEnterAnimation(),
            GetDefaultPushExitAnimation(),
            GetDefaultPopEnterAnimation(),
            GetDefaultPopExitAnimation(),
            GetHomePagePushEnterAnimation(),
            GetHomePagePushExitAnimation(),
            GetHomePagePopEnterAnimation(),
            GetHomePagePopExitAnimation()
        };
    }
}