using AndroidX.CoordinatorLayout.Widget;
using AndroidX.ViewPager2.Adapter;
using AndroidX.ViewPager2.Widget;

using Bumptech.Glide;

using Google.Android.Material.AppBar;
using Google.Android.Material.Dialog;
using Google.Android.Material.Tabs;

using static Android.Widget.ImageView;




namespace Dimmer.ViewsAndPages.NativeViews.SingleSong;

public class SongDetailFragment : Fragment , IOnBackInvokedCallback
{
    private readonly string _transitionName;
    private readonly BaseViewModelAnd _viewModel;
    private SongModelView _song;

    // UI Components
    private ImageView _heroImage;
    private CollapsingToolbarLayout _collapsingToolbar;
    private TabLayout _tabLayout;
    private ViewPager2 _viewPager;

    // IMPORTANT: Needs to be class-level to be accessed by the Listener
    private AppBarLayout _appBarLayout;

    public SongDetailFragment(string transitionName, BaseViewModelAnd vm)
    {
        _transitionName = transitionName;
        _viewModel = vm;
        _song = vm.SelectedSong;
    }

    public void OnBackInvoked()
    {
        TransitionActivity myAct = Activity as TransitionActivity;
        myAct?.HandleBackPressInternal();
        //myAct.MoveTaskToBack
    }
    public override void OnResume()
    {
        base.OnResume();

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
        {
            Activity?.OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(
                (int)IOnBackInvokedDispatcher.PriorityDefault, this);
        }
    }
    public override void OnViewCreated(View view, Bundle? savedInstanceState)
    {
        base.OnViewCreated(view, savedInstanceState);

        PostponeEnterTransition();

        if (!string.IsNullOrEmpty(_song?.CoverImagePath))
            Glide.With(this).Load(_song.CoverImagePath).Into(_heroImage);


        view.ViewTreeObserver?.AddOnPreDrawListener(new OnPreDrawListenerImpl(view, this));

    }
    class OnPreDrawListenerImpl : Java.Lang.Object, ViewTreeObserver.IOnPreDrawListener
    {
        private readonly View _fragmentView;
        private readonly Fragment _parentFragment;
        public OnPreDrawListenerImpl(View fragmentView, Fragment parentFragment)
        {
            _fragmentView = fragmentView;
            _parentFragment = parentFragment;
        }
        public bool OnPreDraw()
        {
            _fragmentView.ViewTreeObserver?.RemoveOnPreDrawListener(this);
            _parentFragment.StartPostponedEnterTransition();
            return true;
        }
    }
    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context;
        var root = new CoordinatorLayout(ctx) { LayoutParameters = new ViewGroup.LayoutParams(-1, -1) };

        // --- 1. App Bar Setup ---
        _appBarLayout = new AppBarLayout(ctx)
        {
            LayoutParameters = new CoordinatorLayout.LayoutParams(-1, AppUtil.DpToPx(350))
        };

        _collapsingToolbar = new CollapsingToolbarLayout(ctx)
        {
            LayoutParameters = new AppBarLayout.LayoutParams(-1, -1)
            {
                ScrollFlags = AppBarLayout.LayoutParams.ScrollFlagScroll |
                              AppBarLayout.LayoutParams.ScrollFlagExitUntilCollapsed |
                              AppBarLayout.LayoutParams.ScrollFlagSnap // Snaps to open/closed
            }
        };
        
        // Initial Title
        _collapsingToolbar.SetTitle(_song?.Title ?? "Details");

        // MD3 Styling: Transparent when open (shows image), White when collapsed (shows toolbar)
        _collapsingToolbar.SetExpandedTitleColor(Color.Transparent);
        _collapsingToolbar.SetCollapsedTitleTextColor(Color.White);

        // Hero Image
        _heroImage = new ImageView(ctx) { TransitionName = _transitionName };
        _heroImage.SetScaleType(ScaleType.FitCenter);
        var imgParams = new CollapsingToolbarLayout.LayoutParams(-1, -1)
        {
            CollapseMode = CollapsingToolbarLayout.LayoutParams.CollapseModeParallax
        };
        var androidDomColor = _viewModel.SelectedSecondDominantColor;
        if (androidDomColor is not null)
        {
            var domCol = androidDomColor.ToHex();
            _collapsingToolbar.SetBackgroundColor(Color.ParseColor(domCol));
            _heroImage.SetBackgroundColor(Color.ParseColor(domCol));
        }
        _collapsingToolbar.AddView(_heroImage, imgParams);

       
        // Toolbar (Back Button)
        var toolbar = new Google.Android.Material.AppBar.MaterialToolbar(ctx);
        var toolbarParams = new CollapsingToolbarLayout.LayoutParams(-1, AppUtil.DpToPx(56))
        {
            CollapseMode = CollapsingToolbarLayout.LayoutParams.CollapseModePin
        };
        toolbar.SetNavigationIcon(Resource.Drawable.ic_arrow_back_black_24);
        toolbar.NavigationClick += (s, e) => ParentFragmentManager.PopBackStack();
        _collapsingToolbar.AddView(toolbar, toolbarParams);

        _appBarLayout.AddView(_collapsingToolbar);
        root.AddView(_appBarLayout);

        // --- 2. Content Body (Tabs + ViewPager) ---
        var contentLinear = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new CoordinatorLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        };

        // This behavior ensures the Linear Layout sits BELOW the AppBar and scrolls with it
        var contentParams = (CoordinatorLayout.LayoutParams)contentLinear.LayoutParameters;
        var scrollBehav = new AppBarLayout.ScrollingViewBehavior(); 
        
        contentParams.Behavior = scrollBehav;

        // TabLayout
        _tabLayout = new TabLayout(ctx);
        _tabLayout.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);

        // MD3 Tab Styling
        _tabLayout.TabMode = TabLayout.ModeScrollable; // Allows many tabs
        _tabLayout.SetSelectedTabIndicatorColor(Color.ParseColor("#6750A4")); // MD3 Purple
        _tabLayout.SetTabTextColors(Color.Gray, Color.White);
       
        // ViewPager
        _viewPager = new ViewPager2(ctx);
        _viewPager.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            0,
            1f // Fill remaining space
        );

        contentLinear.AddView(_tabLayout);
        contentLinear.AddView(_viewPager)
            ;
        root.AddView(contentLinear);

        // --- 3. Logic Wiring ---
        var adapter = new SongDetailPagerAdapter(this, _viewModel);
        _viewPager.Adapter = adapter;
        

        new TabLayoutMediator(_tabLayout, _viewPager, new TabStrategy()).Attach();

        
        _tabLayout.TabSelected += (s, e) =>
        {
            var tab = e.Tab;
            if (tab != null)
            {
                if (tab.Position != 0)
                {
                    // Collapse header and change title
                    _appBarLayout.SetExpanded(false, true);
                    _collapsingToolbar.Title = tab.Text;
                }
                else
                {
                    _appBarLayout.SetExpanded(true, true);

                    _collapsingToolbar.Title = $"{_song?.Title} • {_song?.ArtistName}";
                }
            }
        };

        _tabLayout.TabReselected += (s, e) =>
        {
            // Expand header on reselect
            _appBarLayout.SetExpanded(true, true);
        };

        return root;
    }

    

    // --- Helpers & Inner Classes ---

    class TabStrategy : Java.Lang.Object, TabLayoutMediator.ITabConfigurationStrategy
    {
        public void OnConfigureTab(TabLayout.Tab tab, int position)
        {
            tab.SetText(position switch { 0 => "Overview", 1 => "Lyrics", 2 => "History", 3 => "Related", _ => "" });
        }
    }

  
// PAGER ADAPTER
    class SongDetailPagerAdapter : FragmentStateAdapter
    {
        private BaseViewModelAnd _vm;
        public SongDetailPagerAdapter(Fragment f, BaseViewModelAnd vm) : base(f) { _vm = vm; }
        public override int ItemCount => 4;
        public override Fragment CreateFragment(int position)
        {
            switch (position)
            {
                //case 0:

                //    return new SingleBigCardOverViewFragment(_vm);
                case 0:
                    return new SongOverviewFragment(_vm);
                case 1:
                    return new DownloadLyricsFragment(_vm);
                case 2:
                    return new SongPlayHistoryFragment(_vm);
                case 3:
                    return new SongRelatedFragment(_vm);
                default:
                    return new SongOverviewFragment(_vm);
            }
           
        }
    }
}