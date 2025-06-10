using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utils.PageAnimations;
public static class AnimationManager
{
    // Public method to save the full animation profile for a page type
    public static void SetPageAnimations(Type pageType, AnimationSetting? pushEnter, AnimationSetting? pushExit, AnimationSetting? popEnter, AnimationSetting? popExit)
    {
        string pageIdentifier = GetPageIdentifier(pageType);

        Preferences.Set($"{pageIdentifier}_PushEnterAnim", pushEnter?.ResourceId ?? default(int));
        Preferences.Set($"{pageIdentifier}_PushExitAnim", pushExit?.ResourceId ?? default);
        Preferences.Set($"{pageIdentifier}_PopEnterAnim", popEnter?.ResourceId ?? default(int));
        Preferences.Set($"{pageIdentifier}_PopExitAnim", popExit?.ResourceId ?? default(int));
    }

    // Public method to load the full animation profile for a page type
    public static PageAnimationProfile GetPageAnimations(Type pageType, IAnimationService animationService)
    {
        string pageIdentifier = GetPageIdentifier(pageType);

        var availableAnims = animationService.GetAvailableAnimations();

        var profile = new PageAnimationProfile();

        // Load saved IDs and find the matching AnimationSetting object
        int pushEnterId = Preferences.Get($"{pageIdentifier}_PushEnterAnim", animationService.GetDefaultPushEnterAnimation().ResourceId);
        profile.PushEnter = availableAnims.FirstOrDefault(a => a.ResourceId == pushEnterId) ?? animationService.GetDefaultPushEnterAnimation();

        int pushExitId = Preferences.Get($"{pageIdentifier}_PushExitAnim", animationService.GetDefaultPushExitAnimation().ResourceId);
        profile.PushExit = availableAnims.FirstOrDefault(a => a.ResourceId == pushExitId) ?? animationService.GetDefaultPushExitAnimation();

        int popEnterId = Preferences.Get($"{pageIdentifier}_PopEnterAnim", animationService.GetDefaultPopEnterAnimation().ResourceId);
        profile.PopEnter = availableAnims.FirstOrDefault(a => a.ResourceId == popEnterId) ?? animationService.GetDefaultPopEnterAnimation();

        int popExitId = Preferences.Get($"{pageIdentifier}_PopExitAnim", animationService.GetDefaultPopExitAnimation().ResourceId);
        profile.PopExit = availableAnims.FirstOrDefault(a => a.ResourceId == popExitId) ?? animationService.GetDefaultPopExitAnimation();

        return profile;
    }

    private static string GetPageIdentifier(Type pageType)
    {
        return pageType.FullName; // A unique and reliable identifier for a page
    }
}

// Helper class to hold a page's full animation settings
public class PageAnimationProfile
{
    public AnimationSetting PushEnter { get; set; }
    public AnimationSetting PushExit { get; set; }
    public AnimationSetting PopEnter { get; set; }
    public AnimationSetting PopExit { get; set; }
}