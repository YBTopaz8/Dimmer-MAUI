using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Text;
using System.Threading.Tasks;
using AndroidX.CoordinatorLayout.Widget;
using AndroidX.Core.Widget;
using AndroidX.RecyclerView.Widget;
using Bumptech.Glide;
using Google.Android.Material.Behavior;
using Google.Android.Material.Chip;
using Google.Android.Material.Floatingtoolbar;
using Google.Android.Material.Loadingindicator;
using static Android.Provider.DocumentsContract;

namespace Dimmer.ViewsAndPages.NativeViews.AlbumSection;

public partial class AlbumFragment : Fragment, IOnBackInvokedCallback
{
    private TextView songsLabel;
    private RecyclerView _recyclerView;
    private SongAdapter MyRecycleViewAdapter;
    private TextView artistsLabel;
    private TextView nameTxt;
    private NestedScrollView myScrollView;
    private LinearLayout root;
    private string _albumName;
    private string _albumId;
    private ChipGroup _artistChipGroup;
    private LoadingIndicator progressIndic;

    public AlbumFragment()
    {
        
    }
    public AlbumFragment(BaseViewModelAnd vm)
    {
        MyViewModel = vm;
        SelectedAlbum = vm.SelectedAlbum;
    }

    public BaseViewModelAnd MyViewModel { get; }
    public AlbumModelView? SelectedAlbum { get; }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context;

        // 1. THE ROOT: CoordinatorLayout (Required for "Floating" behavior)
        var coordinator = new CoordinatorLayout(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        };

        // 2. THE CONTENT SCROLLER: NestedScrollView (Required to trigger hide-on-scroll)
        myScrollView = new NestedScrollView(ctx)
        {
            LayoutParameters = new CoordinatorLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent),
            FillViewport = true
        };


        root = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        // --- 1. HEADER SECTION ---
        var headerLayout = new FrameLayout(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, AppUtil.DpToPx(250))
        };

        var albumImage = new ImageView(ctx)
        {
            LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent),
        };
        albumImage.SetScaleType(ImageView.ScaleType.CenterCrop);
        

        
        Glide.With(ctx).Load(SelectedAlbum!.ImagePath).Into(albumImage);

        // Gradient Overlay for text readability
        var overlay = new View(ctx) { LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent) };


        nameTxt = new MaterialTextView(ctx)
        {
            Text = _albumName,
            TextSize = 32,
            Typeface = Android.Graphics.Typeface.DefaultBold,
            LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
            { Gravity = GravityFlags.Bottom | GravityFlags.Left }
            ,
            TransitionName = _albumId
        };
        nameTxt.SetPadding(40, 0, 0, 40);
        nameTxt.SetTextColor(Android.Graphics.Color.White);

        headerLayout.AddView(albumImage);
        headerLayout.AddView(overlay);
        headerLayout.AddView(nameTxt);
        root.AddView(headerLayout);

        // --- 2. ALBUMS (Horizontal List) ---
        artistsLabel = new MaterialTextView(ctx) { Text = $"{SelectedAlbum.Artists?.Count} Artists", TextSize = 20 };
        artistsLabel.SetPadding(30, 30, 30, 10);
        root.AddView(artistsLabel);



        _artistChipGroup = new ChipGroup(context: ctx);

        root.AddView(_artistChipGroup);


        // --- 4. STATS SECTION ---
        var statsCard = new Google.Android.Material.Card.MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(16),
            CardElevation = AppUtil.DpToPx(6),
            UseCompatPadding = true,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        var statsLayout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        statsLayout.SetPadding(40, 40, 40, 40);

        statsLayout.AddView(new TextView(ctx) { Text = "Album Stats", TextSize = 18, Typeface = Android.Graphics.Typeface.DefaultBold });

        if (SelectedAlbum.SongsInAlbum is not null)
        {

            statsLayout.AddView(CreateStatRow(ctx, "Total Plays", SelectedAlbum.SongsInAlbum.Sum(x => x.PlayCompletedCount).ToString()));
            statsLayout.AddView(CreateStatRow(ctx, "Total Skips", SelectedAlbum.SongsInAlbum.Sum(x => x.SkipCount).ToString()));
        }
        statsLayout.AddView(CreateStatRow(ctx, "Library Tracks", SelectedAlbum.SongsInAlbum.Count.ToString()));

        statsCard.AddView(statsLayout);
        root.AddView(statsCard);




        progressIndic = new LoadingIndicator(ctx);
        progressIndic.IndicatorSize = AppUtil.DpToPx(40);
        progressIndic.SetForegroundGravity(GravityFlags.CenterHorizontal);
        root.AddView(progressIndic);



        var recyclerContainer = new LinearLayout(ctx) { Orientation = Orientation.Vertical };

        songsLabel = new MaterialTextView(ctx) { Text = "Songs " + SelectedAlbum.SongsInAlbum.Count, TextSize = 20 }; // Update count later
        recyclerContainer.AddView(songsLabel);

        _recyclerView = new RecyclerView(ctx);
        _recyclerView.NestedScrollingEnabled = false; // LET THE SCROLLVIEW HANDLE SCROLLING
        _recyclerView.SetLayoutManager(new LinearLayoutManager(ctx));

        // Set Adapter immediately with empty data or loading state to prevent UI pop-in
        MyRecycleViewAdapter = new SongAdapter(ctx, MyViewModel, this, "artist");
        _recyclerView.SetAdapter(MyRecycleViewAdapter);

        recyclerContainer.AddView(_recyclerView);
        root.AddView(recyclerContainer);

        // Add root to ScrollView
        myScrollView.AddView(root);

        // Add ScrollView to Coordinator (Bottom Layer)
        coordinator.AddView(myScrollView);


        var fToolbarLayout = new FloatingToolbarLayout(ctx)
        {
            Id = View.GenerateViewId(),
            Clickable = true
        };

        // Position it Bottom|Center or Bottom|Right
        var ftbParams = new CoordinatorLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
        {
            BottomMargin = AppUtil.DpToPx(24)
        };
        ftbParams.Gravity = (int)(GravityFlags.Bottom | GravityFlags.Right); // Or CenterVertical | Right


        // ENABLE HIDE ON SCROLL
        ftbParams.Behavior = new HideViewOnScrollBehavior(ctx, null);
        fToolbarLayout.LayoutParameters = ftbParams;


        var verticalMenu = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical, // Or Vertical if you want a vertical bar
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
        };
        verticalMenu.SetPadding(10, 20, 10, 20);

        verticalMenu.AddView(CreateToolbarButton(ctx, Resource.Drawable.musicfilter, "Filter"));
        verticalMenu.AddView(CreateToolbarButton(ctx, Android.Resource.Drawable.IcDelete, "Delete"));
        verticalMenu.AddView(CreateToolbarButton(ctx, Android.Resource.Drawable.IcMenuShare, "Share"));

        fToolbarLayout.AddView(verticalMenu);

        // Add Toolbar to Coordinator (Top Layer)
        coordinator.AddView(fToolbarLayout);

        return coordinator;
    }
    private LinearLayout CreateStatRow(Context ctx, string label, string value)
    {
        var row = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        row.SetPadding(0, 10, 0, 10);
        var t1 = new TextView(ctx) { Text = label + ": ", Typeface = Android.Graphics.Typeface.DefaultBold };
        var t2 = new TextView(ctx) { Text = value };
        row.AddView(t1);
        row.AddView(t2);
        return row;
    }

    private Chip CreateToolbarButton(Context ctx, int iconRes, string desc)
    {
        var btn = new Chip(ctx);
        btn.SetChipIconResource(iconRes);
        btn.ContentDescription = desc;
        btn.SetBackgroundColor(Android.Graphics.Color.Transparent); // Make button background clear
        btn.ChipStrokeWidth = 0;
        //btn.SetBackgroundColor(MyViewModel.CurrentPlaySongDominantColor)
        // Size and Margin
        var lp = new LinearLayout.LayoutParams(AppUtil.DpToPx(48), AppUtil.DpToPx(48));
        lp.SetMargins(0, 0, 0, 10); // Vertical gap between buttons
        btn.LayoutParameters = lp;

        // Optional: Add Ripple
        TypedValue outValue = new TypedValue();
        ctx.Theme.ResolveAttribute(Android.Resource.Attribute.SelectableItemBackgroundBorderless, outValue, true);
        btn.SetBackgroundResource(outValue.ResourceId);

        return btn;
    }

    public override void OnResume()
    {
        base.OnResume();
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
        {
            Activity?.OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(
                (int)IOnBackInvokedDispatcher.PriorityDefault, this);
        }

        MyRecycleViewAdapter.IsSourceCleared.
           ObserveOn(RxSchedulers.UI)
           .Subscribe(observer =>
           {
               progressIndic.Visibility = ViewStates.Gone;
           }).DisposeWith(_disposables);
    }
    private readonly CompositeDisposable _disposables = new();
    public void OnBackInvoked()
    {
        TransitionActivity myAct = (Activity as TransitionActivity)!;
        myAct?.HandleBackPressInternal();
    }
}
