using System.Reflection;

using Humanizer;
namespace Dimmer.Utils.PageAnimations;
public partial class AndroidAnimationService : IAnimationService
{
    // --- DEFAULTS AND SPECIAL CASES ---
    // These are your hardcoded fallbacks if nothing is set.
    private readonly AnimationSetting _defaultPushEnter = new() { DisplayName = "Default Slide In Right", ResourceId = Resource.Animation.m3_bottom_sheet_slide_in };
    private readonly AnimationSetting _defaultPushExit = new() { DisplayName = "Default Slide Out Left", ResourceId = Resource.Animation.m3_bottom_sheet_slide_out };
    private readonly AnimationSetting _defaultPopEnter = new() { DisplayName = "Default Slide In Left", ResourceId = Resource.Animation.m3_bottom_sheet_slide_in };
    private readonly AnimationSetting _defaultPopExit = new() { DisplayName = "Default Slide Out Right", ResourceId = Resource.Animation.m3_bottom_sheet_slide_out };

    private readonly AnimationSetting _homePagePushEnter = new() { DisplayName = "Home: Sheet Up In", ResourceId = Resource.Animation.m3_side_sheet_enter_from_left };
    private readonly AnimationSetting _homePagePushExit = new() { DisplayName = "Home: Fade Out", ResourceId = Resource.Animation.m3_motion_fade_exit };
    private readonly AnimationSetting _homePagePopEnter = new() { DisplayName = "Home: Fade In", ResourceId = Resource.Animation.m3_side_sheet_enter_from_right };
    private readonly AnimationSetting _homePagePopExit = new() { DisplayName = "Home: Sheet Down Out", ResourceId = Resource.Animation.m3_motion_fade_exit };

    // --- DYNAMICALLY LOADED ANIMATIONS (CACHED) ---
    private static List<AnimationSetting> _availableAnimations;

    public List<AnimationSetting> GetAvailableAnimations()
    {
        // Use a cached list so we only run reflection once.
        if (_availableAnimations != null)
        {
            return _availableAnimations;
        }

        var animationFields = typeof(Resource.Animation).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly);

        var animations = new List<AnimationSetting>();

        // Add a crucial "None" option. ResourceId = 0 means no animation.
        animations.Add(new AnimationSetting { DisplayName = "None", ResourceId = 0 });

        foreach (var field in animationFields)
        {
            animations.Add(new AnimationSetting
            {
                // Use Humanizer to make "m3_motion_fade_enter" into "M3 Motion Fade Enter"
                DisplayName = field.Name.Humanize(LetterCasing.Title),
                ResourceId = (int)field.GetValue(null)
            });
        }

        _availableAnimations = [.. animations.OrderBy(a => a.DisplayName)];
        return _availableAnimations;
    }

    // --- GETTERS FOR DEFAULTS ---
    public AnimationSetting GetDefaultPushEnterAnimation() => _defaultPushEnter;
    public AnimationSetting GetDefaultPushExitAnimation() => _defaultPushExit;
    public AnimationSetting GetDefaultPopEnterAnimation() => _defaultPopEnter;
    public AnimationSetting GetDefaultPopExitAnimation() => _defaultPopExit;

    public AnimationSetting GetHomePagePushEnterAnimation() => _homePagePushEnter;
    public AnimationSetting GetHomePagePushExitAnimation() => _homePagePushExit;
    public AnimationSetting GetHomePagePopEnterAnimation() => _homePagePopEnter;
    public AnimationSetting GetHomePagePopExitAnimation() => _homePagePopExit;
}