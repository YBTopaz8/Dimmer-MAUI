
using Dimmer.ViewsAndPages.NativeViews.Activity;

namespace Dimmer.ViewsAndPages.NativeViews;


public class DetailFragment : Fragment
{
    private readonly string _transitionName;
    public BaseViewModelAnd MyViewModel { get; }

    public DetailFragment(string transitionName)
    {
        _transitionName = transitionName;
        MyViewModel = IPlatformApplication.Current!.Services!.GetService<BaseViewModelAnd>()!;
    }
    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var layout = new FrameLayout(Context);
        layout.SetBackgroundColor(Color.White);



        var image = new ImageView(Context);
        image.TransitionName = _transitionName;
        // handle image
        if (!string.IsNullOrEmpty(MyViewModel.SelectedSong.CoverImagePath) && System.IO.File.Exists(MyViewModel.SelectedSong.CoverImagePath))
        {
            // Load from disk
            var bmp = Android.Graphics.BitmapFactory.DecodeFile(MyViewModel.SelectedSong.CoverImagePath);

            image.SetImageBitmap(bmp);
        }

        else
        {
            // Fallback placeholder
            image.SetImageResource(Resource.Drawable.musicnotess);
        }


        image.TransitionName = "sharedImage";
        image.LayoutParameters = new FrameLayout.LayoutParams(
            FrameLayout.LayoutParams.MatchParent,
            700
        );
        layout.AddView(image);

        return layout;
    }

    public override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var enter = new MaterialContainerTransform(Context!, true)
        {
            DrawingViewId = Resource.Id.content,
            ScrimColor = Color.Transparent,
            FadeMode = MaterialContainerTransform.FadeModeIn
        };
        enter.SetDuration(400);

        var returnTrans = new MaterialContainerTransform(Context!, false)
        {
            DrawingViewId = Resource.Id.content,
            ScrimColor = Color.Transparent,
            FadeMode = MaterialContainerTransform.FadeModeOut
        };
        returnTrans.SetDuration(300);

        SharedElementEnterTransition = enter;
        SharedElementReturnTransition = returnTrans;
    }
}
