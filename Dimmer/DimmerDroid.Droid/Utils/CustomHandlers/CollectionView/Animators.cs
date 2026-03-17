using Java.Lang;

namespace Dimmer.Utils.CustomHandlers.CollectionView;

public class SelectionAnimator : DefaultItemAnimator
{
    public override bool AnimateChange(RecyclerView.ViewHolder oldHolder, RecyclerView.ViewHolder newHolder, int fromX, int fromY, int toX, int toY)
    {
        // If the holders are the same, it means a content update.
        // This is not what we want for animating *between* two different selected items.
        // Our logic in SelectionChanged handles notifying both the old and new items,
        // so we will get called twice: once for the oldHolder and once for the newHolder.

        // The base implementation handles this poorly for our use case, so we override it completely.
        // We'll handle both the old and new item animations here.
        // Returning 'true' tells the RecyclerView we've handled the animation.

        if (oldHolder != null)
        {
            var oldView = oldHolder.ItemView;
            oldView.Animate()?.Cancel();

            // Animate the deselected view
            oldView.Animate()
                .Alpha(0f)
                .ScaleX(0.8f)
                .ScaleY(0.8f)
                .SetDuration(300)
                // *** CORRECTION HERE ***
                .WithEndAction(new Runnable(() =>
                {
                    // Clean up properties after animation
                    oldView.Alpha = 1f;
                    oldView.ScaleX = 1f;
                    oldView.ScaleY = 1f;
                    DispatchChangeFinished(oldHolder, true); // Notify animator this is done
                }))
                .Start();
        }

        if (newHolder != null)
        {
            var newView = newHolder.ItemView;
            // Prepare the initial state for the incoming view
            newView.Alpha = 0f;
            newView.ScaleX = 0.8f;
            newView.ScaleY = 0.8f;

            newView.Animate()?.Cancel();

            // Animate the newly selected view
            newView.Animate()
                .Alpha(1f)
                .ScaleX(1f)
                .ScaleY(1f)
                .SetDuration(300)
                // *** CORRECTION HERE ***
                .WithEndAction(new Runnable(() =>
                {
                    DispatchChangeFinished(newHolder, false); // Notify animator this is done
                }))
                .Start();
        }

        return true;
    }

    // We must override these to prevent default animations from interfering.
    // Returning false tells the RecyclerView we didn't handle the animation,
    // so it won't do anything, which is what we want.
    public override bool AnimateAdd(RecyclerView.ViewHolder? holder)
    {
        return false;
    }

    public override bool AnimateRemove(RecyclerView.ViewHolder? holder)
    {
        return false;
    }

    public override bool AnimateMove(RecyclerView.ViewHolder? holder, int fromX, int fromY, int toX, int toY)
    {
        return false;
    }
}