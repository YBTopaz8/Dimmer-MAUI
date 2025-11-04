
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Widget;

using AndroidX.Core.View;
using AndroidX.Fragment.App;
using AndroidX.Transitions;

using Google.Android.Material.Transition;

using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;

using Button = Android.Widget.Button;
using Orientation = Android.Widget.Orientation;

namespace Dimmer.Views.NativeViews;

public class TestPageOneFragment : Fragment
{

    private ImageView? _image;

    public override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // this is the outgoing transform
        var transform = new MaterialContainerTransform
        {
            DrawingViewId = TransitionActivity.MyStaticID,
           
            ScrimColor = Android.Graphics.Color.Transparent,
            FadeMode = MaterialContainerTransform.FadeModeThrough
        };
        transform.SetDuration(400);

        SharedElementReturnTransition = transform;
        ExitTransition = new Fade();
    }
    public override Android.Views.View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {

        var layout = new LinearLayout(Context)
        {
            Orientation = Orientation.Vertical
        };
        layout.SetBackgroundColor(Android.Graphics.Color.DarkSlateBlue);
        _image = new ImageView(Context);
        _image.SetImageResource(Resource.Drawable.dimmicoo);
        _image.TransitionName = "sharedImage";

        var btn = new Button(Context)
        {
            Text = "Open Details"
        };
        btn.Click += (s, e) => NavigateToDetails();


        layout.AddView(_image);
        layout.AddView(btn);
        return layout;
        //return base.OnCreateView(inflater, container, savedInstanceState);
    }

    private void NavigateToDetails()
    {
        //var detail = new DetailFragment();


        //// tell Android to postpone the transition until the view is ready
        //ParentFragmentManager.BeginTransaction()
        //    .SetReorderingAllowed(true)
        //    .AddSharedElement(_image!, ViewCompat.GetTransitionName(_image!)!)
        //    .Replace(TransitionActivity.MyStaticID, detail)
        //    .AddToBackStack(null)
        //    .Commit();

    }
}