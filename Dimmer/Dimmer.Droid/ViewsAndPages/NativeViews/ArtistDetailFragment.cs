using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Android.Graphics;
using Android.OS;

using AndroidX.DynamicAnimation;
using AndroidX.Fragment.App;

using Java.Util.Streams;

using ImageButton = Android.Widget.ImageButton;
using Orientation = Android.Widget.Orientation;
using View = Android.Views.View;


namespace Dimmer.ViewsAndPages.NativeViews;

public class ArtistDetailFragment : Fragment
{
    private ImageView? _sharedImage;

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context!;
        var root = new LinearLayout(ctx)
        {
            Id = View.GenerateViewId(),
            Orientation = Orientation.Vertical,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        };
        root.SetBackgroundColor(Android.Graphics.Color.Orange);

        _sharedImage = new ImageView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 800),
            TransitionName = "artistImage" // MUST match source fragment
        };
        _sharedImage.SetImageResource(Resource.Drawable.musical_notes);
        _sharedImage.SetScaleType(ImageView.ScaleType.CenterCrop);

        root.AddView(_sharedImage);

        // Back button
        var backBtn = new Button(ctx)
        {
            Text = "Back",
        };
        //FragmentManager is deprecated, use
        //ParentFragmentManager.
        //ChildFragmentManager.
        backBtn.Click += (_, __) => ParentFragmentManager!.PopBackStack();
        root.AddView(backBtn);

        return root;
    }
}