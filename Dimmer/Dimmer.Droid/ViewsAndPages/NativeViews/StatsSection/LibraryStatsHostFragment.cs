using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AndroidX.CoordinatorLayout.Widget;
using AndroidX.ViewPager2.Adapter;
using AndroidX.ViewPager2.Widget;

using Google.Android.Material.AppBar;
using Google.Android.Material.Tabs;

namespace Dimmer.ViewsAndPages.NativeViews.StatsSection;


public class LibraryStatsHostFragment : Fragment
{
    private readonly BaseViewModelAnd _viewModel;
    private readonly StatisticsViewModel _statsViewModel;
    private TabLayout _tabLayout;
    private ViewPager2 _viewPager;

    public LibraryStatsHostFragment(BaseViewModelAnd vm, StatisticsViewModel statsViewModel)
    {
        _viewModel = vm;
        _statsViewModel = statsViewModel;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context;
        var root = new CoordinatorLayout(ctx) { LayoutParameters = new ViewGroup.LayoutParams(-1, -1) };
        root.SetBackgroundColor(Color.ParseColor("#000000")); // Or Theme Surface Color

        // --- 1. App Bar & Toolbar ---
        var appBar = new AppBarLayout(ctx)
        {
            LayoutParameters = new CoordinatorLayout.LayoutParams(-1, -2)
        };

        var toolbar = new MaterialToolbar(ctx);
        toolbar.Title = "Library Analytics";
        //toolbar.Subtitle = _viewModel.Stats?.Subtitle ?? "All Time";
        toolbar.SetNavigationIcon(Resource.Drawable.ic_arrow_back_black_24); // Ensure you have this drawable
        toolbar.NavigationClick += (s, e) => ParentFragmentManager.PopBackStack();
        appBar.AddView(toolbar);

        // --- 2. Tabs ---
        _tabLayout = new TabLayout(ctx);
        _tabLayout.TabMode = TabLayout.ModeScrollable;
        _tabLayout.SetBackgroundColor(Color.Transparent);
        _tabLayout.SetSelectedTabIndicatorColor(Color.ParseColor("#6750A4")); // MD3 Purple
        _tabLayout.SetTabTextColors(Color.Gray, Color.White);
        appBar.AddView(_tabLayout);

        root.AddView(appBar);

        // --- 3. ViewPager2 ---
        _viewPager = new ViewPager2(ctx);
        var vpParams = new CoordinatorLayout.LayoutParams(-1, -1);
        vpParams.Behavior = new AppBarLayout.ScrollingViewBehavior();
        _viewPager.LayoutParameters = vpParams;
        root.AddView(_viewPager);

        // --- 4. Adapter & Mediator ---
        var adapter = new LibraryStatsPagerAdapter(this, _viewModel, _statsViewModel);
        _viewPager.Adapter = adapter;

        new TabLayoutMediator(_tabLayout, _viewPager, new StatsTabStrategy()).Attach();

        return root;
    }

    // --- Inner Classes ---
    class StatsTabStrategy : Java.Lang.Object, TabLayoutMediator.ITabConfigurationStrategy
    {
        public void OnConfigureTab(TabLayout.Tab tab, int position)
        {
            tab.SetText(position switch
            {
                0 => "Health",
                1 => "Leaderboards",
                2 => "Habits",
                3 => "Insights",
                _ => ""
            });
        }
    }
}

class LibraryStatsPagerAdapter : FragmentStateAdapter
{
    private readonly BaseViewModelAnd _vm;
    private readonly StatisticsViewModel _viewModel;
    public LibraryStatsPagerAdapter(Fragment f, BaseViewModelAnd vm, StatisticsViewModel viewModel) : base(f)
    {
        _vm = vm;
        _viewModel = viewModel;
    }
    public override int ItemCount => 4;

    public override Fragment CreateFragment(int position) => position switch
    {
        0 => new StatsOverviewFragment(_vm,_viewModel),
        1 => new StatsLeaderboardFragment(_viewModel),
        2 => new StatsHabitsFragment(_vm,_viewModel),
        3 => new StatsInsightsFragment(_vm, _viewModel),
        _ => new Fragment()
    };
}