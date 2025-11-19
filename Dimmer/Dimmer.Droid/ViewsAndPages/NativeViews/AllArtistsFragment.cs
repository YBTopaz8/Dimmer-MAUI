using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Android.OS;

using AndroidX.Activity;
using AndroidX.Core.View;
using AndroidX.Fragment.App;
using AndroidX.Lifecycle;
using AndroidX.RecyclerView.Widget;
using AndroidX.Transitions;

using Dimmer.ViewsAndPages.NativeViews.Activity;

using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Shape;
using Google.Android.Material.Transition;

using static Android.Provider.DocumentsContract;
using static Android.Provider.Telephony.Mms;
using static Microsoft.Maui.LifecycleEvents.AndroidLifecycle;

using Color = Android.Graphics.Color;
using Orientation = Android.Widget.Orientation;
using View = Android.Views.View;

namespace Dimmer.ViewsAndPages.NativeViews;


public class AllArtistsFragment : Fragment
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
        root.SetBackgroundColor(Android.Graphics.Color.Gray);

        _sharedImage = new ImageView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(500, 500),
            TransitionName = "artistImage" // MUST match target fragment
        };
        _sharedImage.SetImageResource(Resource.Drawable.musical_notes);
        _sharedImage.SetScaleType(ImageView.ScaleType.CenterCrop);

        root.AddView(_sharedImage);

        _sharedImage.Click += (_, __) =>
        {
            var detailFrag = new ArtistDetailFragment();

            // Enable shared element transition
            var mcTAnim = new MaterialContainerTransform
            {
                DrawingViewId = TransitionActivity.MyStaticID,  // host container
                ScrimColor = Color.Transparent,               // optional fade behind
                ContainerColor = Color.DarkSlateBlue,                 // target fragment background
                                                                      //        StartShapeAppearanceModel = ShapeAppearanceModel.Builder()
                                                                      //.SetAllCorners(CornerFamily.Rounded, 50f) // e.g., circle / rounded image
                                                                      //.Build(),
                                                                      //        EndShapeAppearanceModel = ShapeAppearanceModel.Builder()
                                                                      //.SetAllCorners(CornerFamily.Rounded, 0f)  // rectangle for full screen card
                                                                      //.Build()
            };
            mcTAnim.SetDuration(450L);
            mcTAnim.FadeMode = MaterialContainerTransform.FadeModeIn;
            mcTAnim.SetInterpolator(new Android.Views.Animations.OvershootInterpolator());
            mcTAnim.StartShapeAppearanceModel = ShapeAppearanceModel.InvokeBuilder().SetAllCorners(CornerFamily.Rounded, 50f).Build();
            mcTAnim.EndShapeAppearanceModel = ShapeAppearanceModel.InvokeBuilder().SetAllCorners(CornerFamily.Rounded, 0f).Build();

            detailFrag.SharedElementEnterTransition = mcTAnim;
            detailFrag.SharedElementReturnTransition = mcTAnim;
            detailFrag.EnterTransition = new MaterialFade();
            detailFrag.ReturnTransition = new MaterialFade();

            ParentFragmentManager!
                .BeginTransaction()
                .AddSharedElement(_sharedImage, "artistImage")
                .Replace(TransitionActivity.MyStaticID, detailFrag)
                .AddToBackStack(null)
                .Commit();
        };

        return root;
    }
}