




using Google.Android.Material.Transition;

using Kotlin.Text;

using MongoDB.Bson;

namespace Dimmer.ViewsAndPages.NativeViews;

public partial class HomePageFragment : Fragment, IOnBackInvokedCallback
{
    private RecyclerView _songList = null!;
    private TextView _emptyLabel = null!;
    private LinearLayout _bottomBar = null!;
    private TextView _titleTxt = null!;
    private TextView _albumTxt = null!;
    private TextView _currentTime = null!;
    private TextView _playCount = null!;
    private ImageView _albumArt = null!;
    private float _downX;
    private float _downY;
    private FloatingActionButton _pageFAB;
    private string _transitionName;

    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context!;

        View?.TransitionName = "homePageRoot";

        // ROOT FRAME (needed for FAB overlay)
        var root = new FrameLayout(ctx)
        {
            LayoutParameters = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };

        // MAIN COLUMN
        var column = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };

        // SEARCHBAR (top)
        var searchBar = new EditText(ctx)
        {
            Hint = "Search songs, artists, albums...",
            TextSize = 16f
        };
        
        searchBar.SetPadding(40, 30, 40, 30);

        var searchBorder = new FrameLayout(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                (int)(ctx.Resources.DisplayMetrics.Density * 60))
        };

        searchBorder.AddView(searchBar);

        // MIDDLE ZONE (RecyclerView + empty text)
        var middleContainer = new FrameLayout(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                0,
                1f)  // weight fills all remaining space
        };

        var recycler = new RecyclerView(ctx)
        {
            LayoutParameters = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };

        recycler.SetLayoutManager(new LinearLayoutManager(ctx));

        var emptyText = new TextView(ctx)
        {
            Text = "No songs found",
            Gravity = Android.Views.GravityFlags.Center,
            TextSize = 16f,
            Visibility = ViewStates.Gone
        };

        middleContainer.AddView(recycler);
        middleContainer.AddView(emptyText);

        // BOTTOM BAR
        var btmBar = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                (int)(ctx.Resources.DisplayMetrics.Density * 60)) // 50-60dp
        };
        btmBar.SetGravity(GravityFlags.CenterVertical);
        btmBar.SetBackgroundColor(Color.ParseColor("#303030"));

        
        // SAMPLE bottom bar content
        
         _albumArt = new ImageView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(
                (int)(ctx.Resources.DisplayMetrics.Density * 60),
                (int)(ctx.Resources.DisplayMetrics.Density * 50))
        };
        _albumArt.SetImageResource(Android.Resource.Drawable.IcMediaPlay);

        _albumArt.TransitionName = "homePageFAB";

        var textStack = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new LinearLayout.LayoutParams(
                0,
                ViewGroup.LayoutParams.WrapContent,
                1f)
        };

        var title = new TextView(ctx) { Text = "Song Title", TextSize = 19f };
        var album = new TextView(ctx) { Text = "Album", TextSize = 14f };

        textStack.AddView(title);
        textStack.AddView(album);

        var rightStack = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical
        };

        var currentTime = new TextView(ctx)
        {
            Text = "0:00",
            TextSize = 12f
        };
        currentTime.Click += (s, e) =>
        {
            Android.Widget.Toast.MakeText(ctx, "hey!", ToastLength.Short).Show();
        };

        var playCount = new TextView(ctx)
        {
            Text = "Plays: 0",
            TextSize = 12f
        };

        rightStack.AddView(currentTime);
        rightStack.AddView(playCount);

        btmBar.AddView(_albumArt);
        btmBar.AddView(textStack);
        btmBar.AddView(rightStack);

        // ADD TOP + MIDDLE + BOTTOM TO COLUMN
        column.AddView(searchBorder);
        column.AddView(middleContainer);
        column.AddView(btmBar);

        // ADD COLUMN TO ROOT
        root.AddView(column);

        // FAB
        _pageFAB = new FloatingActionButton(ctx)
        {
            LayoutParameters = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent,
                GravityFlags.Bottom | GravityFlags.End)
        };
        int fabMargin = (int)(ctx.Resources.DisplayMetrics.Density * 30);
        int fabMarginBottom = (int)(ctx.Resources.DisplayMetrics.Density * 70);
        ((FrameLayout.LayoutParams)_pageFAB.LayoutParameters).SetMargins(fabMargin, fabMargin, fabMargin, fabMarginBottom);
        _pageFAB.Click += (s, e) =>
        {
            Toast.MakeText(ctx, "Play/Pause clicked!", ToastLength.Short)?.Show();
            NavToAlbumaPage(_albumArt.TransitionName);
        };
        ColorStateList colorStateList = new ColorStateList(
            new int[][] {
                new int[] { } // default
            },
            new int[] {
                Color.White,
                Color.DarkSlateBlue,
                Color.Black
            }

        );
        _pageFAB.SetRippleColor(colorStateList);
        
        _pageFAB.SetBackgroundColor(Color.DarkSlateBlue);
        _pageFAB.SetImageResource(Android.Resource.Drawable.IcMediaPlay);

        root.AddView(_pageFAB);

        return root;
    }

    private void NavToAlbumaPage(string transitionName)
    {
        ArtistDetailFragment fragment = new ArtistDetailFragment(transitionName);


        var mcTAnim = new MaterialContainerTransform
        {
            DrawingViewId = TransitionActivity.MyStaticID,  // container for fragments
            ScrimColor = Color.Transparent,
            ContainerColor = Color.Transparent,
            FadeMode = MaterialContainerTransform.FadeModeThrough,
            StartShapeAppearanceModel = ShapeAppearanceModel.InvokeBuilder().SetAllCorners(CornerFamily.Rounded, 50f).Build(),
            EndShapeAppearanceModel = ShapeAppearanceModel.InvokeBuilder().SetAllCorners(CornerFamily.Rounded, 0f).Build(),
        };
        mcTAnim.PathMotion = new MaterialArcMotion();
        mcTAnim.SetDuration(750);
        mcTAnim.SetInterpolator(PublicStats.DecelerateInterpolator).SetDuration(280);

        _pageFAB?.Animate()?
        .Alpha(0f)
        .SetDuration(mcTAnim.Duration)
        .Start();
        

        fragment.SharedElementEnterTransition = mcTAnim;
        fragment.SharedElementReturnTransition = mcTAnim;

        var nonSharedEnterAnim= new Google.Android.Material.Transition.MaterialFadeThrough
        {
        
        };
        
        fragment.EnterTransition = nonSharedEnterAnim;
        fragment.ExitTransition = new Google.Android.Material.Transition.MaterialFadeThrough
        {
        };

        Hold enterHold = new Hold();
        enterHold.AddTarget(TransitionActivity.MyStaticID);
        enterHold.SetDuration(mcTAnim.Duration);
        ParentFragment?.ExitTransition = enterHold;

        ParentFragmentManager.BeginTransaction()
            .AddSharedElement(_albumArt!, transitionName)
            .Replace(TransitionActivity.MyStaticID, fragment)
            .AddToBackStack(null)
            .Commit();
    }


    private void OnBottomBarDrag(object? sender, View.TouchEventArgs e)
    {
        var v = _bottomBar;

        switch (e.Event!.Action)
        {
            case MotionEventActions.Down:
                _downX = e.Event.RawX - v.TranslationX;
                _downY = e.Event.RawY - v.TranslationY;
                break;

            case MotionEventActions.Move:
                v.TranslationX = e.Event.RawX - _downX;
                v.TranslationY = e.Event.RawY - _downY;
                break;

            case MotionEventActions.Up:
                var animX = new SpringAnimation(v, DynamicAnimation.TranslationX, 0);
                animX.Spring.SetStiffness(SpringForce.StiffnessLow);
                animX.Spring.SetDampingRatio(SpringForce.DampingRatioMediumBouncy);

                var animY = new SpringAnimation(v, DynamicAnimation.TranslationY, 0);
                animY.Spring.SetStiffness(SpringForce.StiffnessLow);
                animY.Spring.SetDampingRatio(SpringForce.DampingRatioMediumBouncy);

                animX.Start();
                animY.Start();
                break;

        }
    }

    private bool IsDark()
    {
        return (Resources?.Configuration?.UiMode & Android.Content.Res.UiMode.NightYes) != 0;
    }

    public void OnBackInvoked()
    {
        Toast.MakeText(Context!, "Back invoked in HomePageFragment", ToastLength.Short)?.Show();
    }
}