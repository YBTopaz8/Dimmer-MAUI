using AndroidX.Fragment.App;

using Resource = Microsoft.Maui.Resource; // For Fragment, FragmentManager, FragmentTransaction

namespace Dimmer.CustomShellRenderers;
internal interface IShellPageTransition
{
    void Transition(FragmentManager fragmentManager, FragmentTransaction transaction,
                       Fragment oldFragment, Fragment newFragment, bool newFragmentPoppingIn, bool newFragmentNeedsPushAnimation);
}
public class MyCustomPageTransition : Java.Lang.Object, IShellPageTransition
{
    // These would be Android Animation Resource IDs (e.g., defined in Platforms/Android/Resources/anim/)
    // For a C#-only approach for the ANIMATIONS THEMSELVES (not just triggering them),
    // this IShellPageTransition is more about *orchestrating* which pre-defined animations run.
    // To create anims purely in C#, you'd need to do more when the fragment's view is created.
    // This interface is primarily for telling the FragmentTransaction WHICH anims to use.

    private int _enterAnim = Resource.Animation.m3_motion_fade_enter; // Example XML anim
    private int _exitAnim = Resource.Animation.m3_motion_fade_exit;
    private int _popEnterAnim = Resource.Animation.m3_bottom_sheet_slide_out;
    private int _popExitAnim = Resource.Animation.m3_bottom_sheet_slide_in;

    public MyCustomPageTransition()
    {
        // Optionally load animation types/durations from PublicStats to change _enterAnim etc.
        // But IShellPageTransition is usually for pre-defined XML anims.
        // If PublicStats.SomePageTransitionType == "Cube", then load cube animation IDs.
    }

    public void Transition(FragmentManager fragmentManager, FragmentTransaction transaction,
                           Fragment oldFragment, Fragment newFragment,
                           bool newFragmentPoppingIn, bool newFragmentNeedsPushAnimation)
    {
        if (newFragmentNeedsPushAnimation) // Navigating forward
        {
            transaction.SetCustomAnimations(_enterAnim, _exitAnim, _popEnterAnim, _popExitAnim);
        }
        else if (newFragmentPoppingIn) // Navigating back (popping)
        {
            // Android handles pop animations based on what was set during the push.
            // Or you can set specific ones again if needed, but usually not required here.
            // transaction.SetCustomAnimations(_popEnterAnim, _popExitAnim); // Redundant if set during push
        }
        else // Replacing without push/pop (e.g., tab switch, which has its own anim)
        {
            transaction.SetTransition((int)global::Android.App.FragmentTransit.FragmentFade); // Default fade for tab-like switches
        }
    }
}