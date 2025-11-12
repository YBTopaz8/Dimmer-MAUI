using Microsoft.UI.Xaml.Media.Animation;

namespace Dimmer.WinUI.Utils;
public partial class WindowsAnimationService : IAnimationService
{

    private static List<AnimationSetting> _availableAnimations;

    // --- DEFAULTS AND SPECIAL CASES ---
    private readonly AnimationSetting _defaultPushEnter = new() { DisplayName = "Default (Entrance)", TransitionInfo = new EntranceNavigationTransitionInfo() };
    private readonly AnimationSetting _defaultPushExit = new() { DisplayName = "None" }; // Push Exit is not a concept here
    private readonly AnimationSetting _defaultPopEnter = new() { DisplayName = "None" }; // Pop Enter is not a concept here
    private readonly AnimationSetting _defaultPopExit = new() { DisplayName = "Default (Entrance)", TransitionInfo = new EntranceNavigationTransitionInfo() };

    // --- GETTERS FOR DEFAULTS ---
    public AnimationSetting GetDefaultPushEnterAnimation() => _defaultPushEnter;
    public AnimationSetting GetDefaultPushExitAnimation() => _defaultPushExit;
    public AnimationSetting GetDefaultPopEnterAnimation() => _defaultPopEnter;
    public AnimationSetting GetDefaultPopExitAnimation() => _defaultPopExit;

    // Special HomePage cases (can be the same or different)
    public AnimationSetting GetHomePagePushEnterAnimation() => new() { DisplayName = "Drill In", TransitionInfo = new DrillInNavigationTransitionInfo() };
    public AnimationSetting GetHomePagePushExitAnimation() => new() { DisplayName = "None" };
    public AnimationSetting GetHomePagePopEnterAnimation() => new() { DisplayName = "None" };
    public AnimationSetting GetHomePagePopExitAnimation() => new() { DisplayName = "Drill In", TransitionInfo = new DrillInNavigationTransitionInfo() };

    public List<AnimationSetting> GetAvailableAnimations()
    {
        if (_availableAnimations != null)
        {
            return _availableAnimations;
        }

        // The list of animations available on Windows
        _availableAnimations = new List<AnimationSetting>
        {
            new() { DisplayName = "None", TransitionInfo = new SuppressNavigationTransitionInfo() },
            new() { DisplayName = "Default (Entrance)", TransitionInfo = new EntranceNavigationTransitionInfo() },
            new() { DisplayName = "Drill In", TransitionInfo = new DrillInNavigationTransitionInfo() },
            new() { DisplayName = "Slide From Right", TransitionInfo = new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight } },
            new() { DisplayName = "Slide From Left", TransitionInfo = new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromLeft } },
            new() { DisplayName = "Continuum", TransitionInfo = new ContinuumNavigationTransitionInfo() },
        };

        return _availableAnimations.OrderBy(a => a.DisplayName).ToList();
    }
    
}
