using AndroidX.Interpolator.View.Animation;
using Fade = AndroidX.Transitions.Fade;

namespace Dimmer.Utils;


public static class AnimationHelper
{
    /// <summary>
    /// Executes a Morph (Shared Element) transition from one Fragment to another.
    /// </summary>
    public static void ShowMorphingFragment(
        FragmentManager fragmentManager,
        Fragment destinationFragment,
        params (View SourceView, string TransitionName)[] sharedElements)
    {
        // 1. Define the Morphing Animation
        var morphTransition = new TransitionSet();
        morphTransition.AddTransition(new ChangeBounds());
        morphTransition.AddTransition(new ChangeTransform());
        morphTransition.AddTransition(new ChangeImageTransform());

        // Spring-like bouncy interpolator for a modern feel
        morphTransition.SetInterpolator(new FastOutSlowInInterpolator());
        morphTransition.SetDuration(350);

        // 2. Apply to the Destination Fragment
        destinationFragment.SharedElementEnterTransition = morphTransition;
        destinationFragment.SharedElementReturnTransition = morphTransition;

        // Optional: Fade out the old fragment slightly
        var dTrans = new Fade ();
        dTrans.SetDuration(300);
        destinationFragment.EnterTransition = dTrans;

        destinationFragment.ReturnTransition = dTrans;

        // 3. Build and Execute Transaction
        var transaction = fragmentManager.BeginTransaction();
        transaction.SetReorderingAllowed(true); // REQUIRED for shared elements

        // Add all shared elements to the transaction
        foreach (var element in sharedElements)
        {
            ViewCompat.SetTransitionName(element.SourceView, element.TransitionName);
            transaction.AddSharedElement(element.SourceView, element.TransitionName);
        }

        // Add the overlay fragment (Assuming your activity has a root container with this ID)
        // If you don't have a specific container, Android.Resource.Id.Content is the root of the screen.
        transaction.Add(Android.Resource.Id.Content, destinationFragment, "MorphOverlay");
        transaction.AddToBackStack("MorphOverlay");
        transaction.Commit();
    }
}