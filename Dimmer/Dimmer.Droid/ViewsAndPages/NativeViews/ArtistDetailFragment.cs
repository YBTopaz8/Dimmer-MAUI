using System;



using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Android.Graphics;
using Android.OS;
using Android.Text;

using AndroidX.AppCompat.Content.Res;
using AndroidX.ConstraintLayout.Widget;
using AndroidX.DynamicAnimation;
using AndroidX.Fragment.App;
using AndroidX.Transitions;

using Google.Android.Material.AppBar;
using Google.Android.Material.Button;
using Google.Android.Material.Card;
using Google.Android.Material.ProgressIndicator;

using Java.Util.Streams;

using MongoDB.Bson;

using TransitionManager = AndroidX.Transitions.TransitionManager;



public class ArtistDetailFragment : Fragment
{
    private ImageView? _sharedImage;
    public static string TAG = "ArtistDetailFrag";
    public static string ArtistId;
    private string transitionName;
    FloatingActionButton fab;
    AppBarLayout appBarLayout;
    public ArtistDetailFragment(string transitionName)
    {
        this.transitionName = transitionName;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context!;
        
        var root = new LinearLayout(ctx)
        {
            Id = View.GenerateViewId(),
            Orientation = Orientation.Vertical,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        };
        root.SetBackgroundColor(Android.Graphics.Color.Black);

        _sharedImage = new ImageView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 300),
            TransitionName = transitionName// MUST match source fragment
        };
        _sharedImage.SetImageResource(Android.Resource.Drawable.IcMediaPlay);
        _sharedImage.SetScaleType(ImageView.ScaleType.CenterCrop);

        root.AddView(_sharedImage);
        var appBarContainer = CreateAppBar(ctx);
        root.AddView(appBarContainer);
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


    public View CreateAppBar(Context ctx)
    {
        var container = new FrameLayout(ctx)
        {
            LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        };
        container.TransitionName = "album_container";
        var appBarLayout = new AppBarLayout(ctx)
        {
            LayoutParameters = new AppBarLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)

        };

        var collapsingToolbarLayout = new CollapsingToolbarLayout(ctx)
        {
            LayoutParameters = new CollapsingToolbarLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
            {
            },
        };
        collapsingToolbarLayout.SetScrollIndicators(AppBarLayout.LayoutParams.ScrollFlagExitUntilCollapsed | AppBarLayout.LayoutParams.ScrollFlagSnap);

        var constraintLayout = new ConstraintLayout(ctx)
        {
            LayoutParameters = new ConstraintLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };

        var albumImage = new ImageView(ctx)
        {
            Id = View.GenerateViewId(),
            ContentDescription = "Cover Image",
        };
        var albumImageParams = new ConstraintLayout.LayoutParams(0, 0);
        albumImageParams.DimensionRatio = "H,1:1";
        albumImageParams.StartToStart = ConstraintLayout.LayoutParams.ParentId;
        albumImageParams.EndToEnd = ConstraintLayout.LayoutParams.ParentId;
        albumImageParams.TopToTop = ConstraintLayout.LayoutParams.ParentId;
        albumImage.LayoutParameters = albumImageParams;
        albumImage.TransitionName = "album_image";


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

        // Album info container
        var albumInfoCard = new MaterialCardView(ctx)
        {
            Id = View.GenerateViewId(),
            CardBackgroundColor = colorStateList, // adjust as needed
            Radius = 0f,
            StrokeWidth = 0
        };
        var albumInfoParams = new ConstraintLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            (int)(ctx.Resources.DisplayMetrics.Density * 196));
        albumInfoParams.TopToBottom = albumImage.Id;
        albumInfoCard.LayoutParameters = albumInfoParams;

        var albumDetails = new LinearLayout(ctx)
        {
            Id = View.GenerateViewId(),
            Orientation = Orientation.Vertical,

        };

        albumDetails.SetGravity(GravityFlags.CenterVertical);
        albumDetails.SetPadding((int)(ctx.Resources.DisplayMetrics.Density * 56), 0, 0, (int)(ctx.Resources.DisplayMetrics.Density * 16));


        var albumTitle = new TextView(ctx)
        {
            Id = View.GenerateViewId(),
            Text = "Metamorphosis",
            TextSize = 24f,
            Ellipsize = TextUtils.TruncateAt.End,
        };
        albumTitle.SetMaxLines(1);

        var albumArtist = new TextView(ctx)
        {
            Id = View.GenerateViewId(),
            Text = "Sandra Adams",
            TextSize = 16f,
            Ellipsize = TextUtils.TruncateAt.End,
        };
        albumArtist.SetMaxLines(1);

        albumDetails.AddView(albumTitle);
        albumDetails.AddView(albumArtist);
        albumInfoCard.AddView(albumDetails);

        // Music Player Container (initially gone)
        var musicPlayerContainer = new ConstraintLayout(ctx)
        {
            Id = View.GenerateViewId(),
            Visibility = ViewStates.Gone
        };
        musicPlayerContainer.SetBackgroundColor(Android.Graphics.Color.Black);
        musicPlayerContainer.TransitionName = "music_player_transition";


        var musicPlayerParams = new ConstraintLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            (int)(ctx.Resources.DisplayMetrics.Density * 196));
        musicPlayerParams.TopToBottom = albumImage.Id;
        musicPlayerContainer.LayoutParameters = musicPlayerParams;

        // LinearProgressIndicator
        var progressIndicator = new LinearProgressIndicator(ctx)
        {
            Id = View.GenerateViewId(),
            Indeterminate = false,
            Progress = 10
        };
        var progressParams = new ConstraintLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent);
        progressParams.StartToStart = ConstraintLayout.LayoutParams.ParentId;
        progressParams.EndToEnd = ConstraintLayout.LayoutParams.ParentId;
        progressParams.TopToTop = ConstraintLayout.LayoutParams.ParentId;
        progressIndicator.LayoutParameters = progressParams;
        musicPlayerContainer.AddView(progressIndicator);

        // Play Button
        var playButton = new MaterialButton(ctx)
        {
            Id = View.GenerateViewId(),
            Icon = AppCompatResources.GetDrawable(ctx, Resource.Drawable.playcircle)
        };
        var playButtonParams = new ConstraintLayout.LayoutParams((int)56, (int)56);
        playButtonParams.StartToStart = ConstraintLayout.LayoutParams.ParentId;
        playButtonParams.EndToEnd = ConstraintLayout.LayoutParams.ParentId;
        playButtonParams.TopToBottom = progressIndicator.Id;
        playButton.LayoutParameters = playButtonParams;
        musicPlayerContainer.AddView(playButton);

        // FAB
        fab = new FloatingActionButton(ctx)
        {
            Id = View.GenerateViewId(),
        };
        fab.SetImageResource(Resource.Drawable.playcircle);
        var fabParams = new ConstraintLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent,
            ViewGroup.LayoutParams.WrapContent);
        fabParams.BottomToTop = albumInfoCard.Id;
        fabParams.EndToEnd = ConstraintLayout.LayoutParams.ParentId;
        fabParams.TopToTop = albumInfoCard.Id;
        fab.LayoutParameters = fabParams;
        fab.TransitionName = "fab_transition";


        var musicPlayerEnterTransform = CreateMusicPlayerTransform(ctx, true, fab, musicPlayerContainer);
        var musicPlayerExitTransform = CreateMusicPlayerTransform(ctx, false, musicPlayerContainer, fab);


        fab.Click += (s, e) =>
        {

            TransitionManager.BeginDelayedTransition(container, musicPlayerEnterTransform);
            fab.Visibility = ViewStates.Gone;
            musicPlayerContainer.Visibility = ViewStates.Visible;
        };

        musicPlayerContainer.Click += (s, e) =>
        {
            TransitionManager.BeginDelayedTransition(container, musicPlayerExitTransform);
            musicPlayerContainer.Visibility = ViewStates.Gone;
            fab.Visibility = ViewStates.Visible;
        };

        // Toolbar
        var toolbar = new MaterialToolbar(ctx)
        {
            Id = View.GenerateViewId(),

        };
        toolbar.SetBackgroundColor(Android.Graphics.Color.Black);


        var toolbarParams = new CollapsingToolbarLayout.LayoutParams(
     ViewGroup.LayoutParams.MatchParent,
     (int)(ctx.Resources.DisplayMetrics.Density * 56)); // 56dp
        toolbar.LayoutParameters = toolbarParams;

        // Add children to constraint layout
        constraintLayout.AddView(albumImage);
        constraintLayout.AddView(albumInfoCard);
        constraintLayout.AddView(musicPlayerContainer);
        constraintLayout.AddView(fab);
        constraintLayout.AddView(toolbar);

        // Add constraint layout to collapsing toolbar
        collapsingToolbarLayout.AddView(constraintLayout);

        // Add collapsing toolbar to appbar
        appBarLayout.AddView(collapsingToolbarLayout);


        appBarLayout.AddOnOffsetChangedListener
            (new AppBarOffsetChangeListen(appBarLayout, fab, musicPlayerContainer));
        container.AddView(appBarLayout);
        return container;
    }

    class AppBarOffsetChangeListen : Java.Lang.Object, AppBarLayout.IOnOffsetChangedListener
    {
        AppBarLayout appBarLayout;
        FloatingActionButton fab;
        ConstraintLayout musicPlayerContainer;
        public AppBarOffsetChangeListen(AppBarLayout abl, FloatingActionButton f,
             ConstraintLayout mpc)
        {
            appBarLayout = abl;
            fab = f;
            this.musicPlayerContainer = mpc;
        }
        public void OnOffsetChanged(AppBarLayout? appBarL, int verticalOffset)
        {
            float percentage = Math.Abs(verticalOffset) / (float)appBarLayout.TotalScrollRange;
            if (percentage > 0.2f && fab.IsOrWillBeShown)
                fab.Hide();
            else if (percentage <= 0.2f && fab.IsOrWillBeHidden && musicPlayerContainer.Visibility != ViewStates.Visible)
                fab.Show();
        }
    }

    static MaterialContainerTransform CreateMusicPlayerTransform(Context ctx, bool entering, View start, View end)
    {
        var transform = new MaterialContainerTransform(ctx, entering);
        transform.StartView = start;
        transform.EndView = end;
        transform.AddTarget(end);
        transform.ScrimColor = Color.Transparent;
        transform.PathMotion = new MaterialArcMotion();
        return transform;
    }


}