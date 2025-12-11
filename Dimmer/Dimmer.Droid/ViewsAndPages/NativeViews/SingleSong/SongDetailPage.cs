using AndroidX.CoordinatorLayout.Widget;
using AndroidX.ViewPager2.Adapter;
using AndroidX.ViewPager2.Widget;

using Bumptech.Glide;

using Google.Android.Material.AppBar;
using Google.Android.Material.Dialog;
using Google.Android.Material.Tabs;

using static Android.Widget.ImageView;




namespace Dimmer.ViewsAndPages.NativeViews.SingleSong;

public class SongDetailPage : Fragment
{
    private readonly string _transitionName;
    private readonly BaseViewModelAnd _viewModel;
    private SongModelView _song;

    // UI
    private ImageView _heroImage;
    private CollapsingToolbarLayout _collapsingToolbar;
    private TabLayout _tabLayout;
    private ViewPager2 _viewPager;

    public SongDetailPage(string transitionName, BaseViewModelAnd vm)
    {
        _transitionName = transitionName;
        _viewModel = vm;
        _song = vm.SelectedSong;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context;
        var root = new CoordinatorLayout(ctx) { LayoutParameters = new ViewGroup.LayoutParams(-1, -1) };

        // 1. App Bar (Collapsing Image)
        var appBar = new AppBarLayout(ctx) { LayoutParameters = new CoordinatorLayout.LayoutParams(-1, AppUtil.DpToPx(350)) };
        _collapsingToolbar = new CollapsingToolbarLayout(ctx)
        {
            LayoutParameters = new AppBarLayout.LayoutParams(-1, -1) { ScrollFlags = AppBarLayout.LayoutParams.ScrollFlagScroll | AppBarLayout.LayoutParams.ScrollFlagExitUntilCollapsed }
        };
        _collapsingToolbar.SetTitle(_song?.Title ?? "Details");
        _collapsingToolbar.SetExpandedTitleColor(Color.Transparent); // Show custom view when expanded
        _collapsingToolbar.SetCollapsedTitleTextColor(Color.White);

        // Hero Image
        _heroImage = new ImageView(ctx) {  TransitionName = _transitionName };
        _heroImage.SetScaleType(ScaleType.CenterCrop);
        var imgParams = new CollapsingToolbarLayout.LayoutParams(-1, -1) { CollapseMode = CollapsingToolbarLayout.LayoutParams.CollapseModeParallax };
        _collapsingToolbar.AddView(_heroImage, imgParams);

        // Load Image
        if (!string.IsNullOrEmpty(_song?.CoverImagePath)) Glide.With(this).Load(_song.CoverImagePath).Into(_heroImage);

        // Toolbar
        var toolbar = new Google.Android.Material.AppBar.MaterialToolbar(ctx);
        var toolbarParams = new CollapsingToolbarLayout.LayoutParams(-1, AppUtil.DpToPx(56)) { CollapseMode = CollapsingToolbarLayout.LayoutParams.CollapseModePin };
        toolbar.SetNavigationIcon(Resource.Drawable.ic_arrow_back_black_24); // Ensure drawable
        toolbar.NavigationClick += (s, e) => ParentFragmentManager.PopBackStack();
        _collapsingToolbar.AddView(toolbar, toolbarParams);

        appBar.AddView(_collapsingToolbar);
        root.AddView(appBar);

        // 2. Content (Tabs + ViewPager)
        var contentLinear = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        var contentParams = new CoordinatorLayout.LayoutParams(-1, -1);
        contentParams.Behavior = new AppBarLayout.ScrollingViewBehavior();
        contentLinear.LayoutParameters = contentParams;

        _tabLayout = new TabLayout(ctx);
        _viewPager = new ViewPager2(ctx);

        contentLinear.AddView(_tabLayout);
        contentLinear.AddView(_viewPager);
        root.AddView(contentLinear);

        // Setup Adapter
        var adapter = new SongDetailPagerAdapter(this, _viewModel);
        _viewPager.Adapter = adapter;

        new TabLayoutMediator(_tabLayout, _viewPager, new TabStrategy()).Attach();

        return root;
    }

    class TabStrategy : Java.Lang.Object, TabLayoutMediator.ITabConfigurationStrategy
    {
        public void OnConfigureTab(TabLayout.Tab tab, int position)
        {
            tab.SetText(position switch { 0 => "Overview", 1 => "Lyrics", 2 => "History", 3 => "Related", _ => "" });
        }
    }
}

// PAGER ADAPTER
class SongDetailPagerAdapter : FragmentStateAdapter
{
    private BaseViewModelAnd _vm;
    public SongDetailPagerAdapter(Fragment f, BaseViewModelAnd vm) : base(f) { _vm = vm; }
    public override int ItemCount => 4;
    public override Fragment CreateFragment(int position) => position switch
    {
        0 => new SongOverviewFragment(_vm),
        1 => new LyricsEditorFragment(), // Reuse existing
        2 => new SongPlayHistoryFragment(_vm), // NEW
        3 => new AllArtistsFragment(), // Placeholder for Related
        _ => new Fragment()
    };
}