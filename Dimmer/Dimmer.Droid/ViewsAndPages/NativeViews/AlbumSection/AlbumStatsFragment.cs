using AndroidX.ViewPager2.Adapter;
using AndroidX.ViewPager2.Widget;
using Dimmer.ViewsAndPages.NativeViews.Stats;
using Google.Android.Material.AppBar;
using Google.Android.Material.Tabs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.ViewsAndPages.NativeViews.AlbumSection;

public class AlbumStatsFragment : Fragment
{
    private readonly StatisticsViewModel _viewModel;
    private readonly AlbumModel _targetAlbum;

    public AlbumStatsFragment(StatisticsViewModel viewModel, AlbumModel album)
    {
        _viewModel = viewModel;
        _targetAlbum = album;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = RequireContext();
        var root = new CoordinatorLayout(ctx) { LayoutParameters = new ViewGroup.LayoutParams(-1, -1) };

        var appBar = new AppBarLayout(ctx) { LayoutParameters = new CoordinatorLayout.LayoutParams(-1, AppUtil.DpToPx(300)) };
        var collapsing = new CollapsingToolbarLayout(ctx) { LayoutParameters = new AppBarLayout.LayoutParams(-1, -1) { ScrollFlags = AppBarLayout.LayoutParams.ScrollFlagScroll | AppBarLayout.LayoutParams.ScrollFlagExitUntilCollapsed } };

        collapsing.SetTitle(_targetAlbum.Name);
        collapsing.SetExpandedTitleColor(Color.Transparent);
        collapsing.SetCollapsedTitleTextColor(Color.Black);

        var heroImg = new ImageView(ctx) { LayoutParameters = new CollapsingToolbarLayout.LayoutParams(-1, -1) { CollapseMode = CollapsingToolbarLayout.LayoutParams.CollapseModeParallax } };
        heroImg.SetScaleType(ImageView.ScaleType.CenterCrop);
        Glide.With(this).Load(_targetAlbum.ImagePath).Into(heroImg);
        collapsing.AddView(heroImg);

        appBar.AddView(collapsing);
        root.AddView(appBar);

        var bodyLayout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        var e = bodyLayout.LayoutParameters = new CoordinatorLayout.LayoutParams(-1, -1);
        var ee = e as CoordinatorLayout.LayoutParams;

        ee?.Behavior = new AppBarLayout.ScrollingViewBehavior();

        var tabLayout = new TabLayout(ctx);
        var viewPager = new ViewPager2(ctx) { LayoutParameters = new LinearLayout.LayoutParams(-1, 0, 1f) };

        bodyLayout.AddView(tabLayout);
        bodyLayout.AddView(viewPager);
        root.AddView(bodyLayout);

        viewPager.Adapter = new AlbumStatsPagerAdapter(this, _viewModel);
        new TabLayoutMediator(tabLayout, viewPager, new AlbumTabStrategy()).Attach();

        return root;
    }

    public override void OnResume()
    {
        base.OnResume();
        _ = _viewModel.LoadAlbumStatsAsync(_targetAlbum);
    }

    class AlbumTabStrategy : Java.Lang.Object, TabLayoutMediator.ITabConfigurationStrategy
    {
        public void OnConfigureTab(TabLayout.Tab tab, int pos) => tab.SetText(pos switch { 0 => "Overview", 1 => "Trends", _ => "" });
    }
}

public class AlbumStatsPagerAdapter : FragmentStateAdapter
{
    private readonly StatisticsViewModel _vm;
    public AlbumStatsPagerAdapter(Fragment f, StatisticsViewModel vm) : base(f) { _vm = vm; }
    public override int ItemCount => 2;

    public override Fragment CreateFragment(int position)
    {
        return position switch
        {
            0 => new StatsDynamicTabFragment(_vm,
                gridExtractor: vm => {
                    var sum = vm.AlbumStats?.Summary;
                    if (sum == null) return null;
                    return new Dictionary<string, string> {
                        { "Total Plays", sum.TotalPlaysOnAlbum.ToString() },
                        { "Skips", sum.TotalSkipsOnAlbum.ToString() },
                        { "Time Listened", sum.TotalListeningTimeOnAlbumFormatted },
                        { "Top Track", sum.MostPlayedSongOnAlbumTitle ?? "N/A" }
                    };
                }, carouselExtractor: null),

            1 => new StatsDynamicTabFragment(_vm, null,
                carouselExtractor: vm => {
                    var data = vm.AlbumStats?.PlottableData;
                    if (data == null) return null;
                    return new Dictionary<string, List<DimmerStats>> {
                        { "Plays by Month", data.PlaysPerDayOfWeekForAlbum.ToDimmerStats() },
                        { "Time of Day", data.PlaysPerHourOfDayForAlbum.ToDimmerStats() }
                    };
                }),
            _ => new Fragment()
        };
    }
}