
using Dimmer.ViewsAndPages.NativeViews.Activity;

using AlertDialog = Android.App.AlertDialog;

namespace Dimmer.ViewsAndPages.NativeViews;


public class DetailFragment : Fragment, IOnBackInvokedCallback
{
    private readonly string _transitionName;
    public BaseViewModelAnd MyViewModel { get; }
    private readonly SongModelView selectedSong;
    public DetailFragment()
    {
        
    }
    public DetailFragment(string transitionName, BaseViewModelAnd vm)
    {
        MyViewModel = vm;
        if (vm.SelectedSong == null)
            { 
        var popUpDialog = new AlertDialog.Builder(Context)
            .SetTitle("Error")
            .SetMessage("No song selected. Returning to previous screen.")
            .SetPositiveButton("OK", (sender, args) =>
            {
                // Dismiss dialog and navigate back
                Activity?.OnBackPressed();
            })
            .Create();
            popUpDialog.Show();
            return;
        }
        selectedSong = vm.SelectedSong;
        _transitionName = transitionName;
    }
    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var layout = new FrameLayout(Context);
        layout.SetBackgroundColor(Color.White);

        //_sharedImage.SetImageResource(Android.Resource.Drawable.IcMediaPlay);


        var image = new ImageView(Context);
        image.TransitionName = _transitionName;
        // handle image
        if (!string.IsNullOrEmpty(selectedSong.CoverImagePath) && System.IO.File.Exists(selectedSong.CoverImagePath))
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
        PostponeEnterTransition();
        SharedElementReturnTransition = returnTrans;
    }

    public override void OnViewCreated(View view, Bundle? savedInstanceState)
    {
        base.OnViewCreated(view, savedInstanceState);
        //View 
    }

    public void OnBackInvoked()
    {
        Toast.MakeText(Context!, "Back invoked in HomePageFragment", ToastLength.Short)?.Show();

    }

    //class OnPreDrawListenerImpl : Java.Lang.Object, ViewTreeObserver.IOnPreDrawListener
    //{
    //    private readonly View _fragmentView;
    //    private readonly DetailFragment _parentFragment;
    //    public OnPreDrawListenerImpl(View fragmentView, DetailFragment parentFragment)
    //    {
    //        _fragmentView = fragmentView;
    //        _parentFragment = parentFragment;
    //    }
    //    //public bool OnPreDraw()

    //    //    _fragmentView.ViewTreeObserver.RemoveOnPreDrawListener(this);
    //    //    _parentFragment.StartPostponedEnterTransition();
    //    //    return true;
    //    //}
    //}
}
