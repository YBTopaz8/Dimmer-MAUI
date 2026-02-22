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

namespace Dimmer.ViewsAndPages.NativeViews.ArtistSection;


public class ArtistStatsFragment : Fragment
{
    private readonly StatisticsViewModel _viewModel;
    private readonly ArtistModel _targetArtist;

    public ArtistStatsFragment(StatisticsViewModel viewModel, ArtistModel artist)
    {
        _viewModel = viewModel;
        _targetArtist = artist;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = RequireContext();
        var root = new CoordinatorLayout(ctx) { LayoutParameters = new ViewGroup.LayoutParams(-1, -1) };

        // Header (CollapsingToolbar)
        var appBar = new AppBarLayout(ctx) { LayoutParameters = new CoordinatorLayout.LayoutParams(-1, AppUtil.DpToPx(250)) };
        var collapsing = new CollapsingToolbarLayout(ctx) { LayoutParameters = new AppBarLayout.LayoutParams(-1, -1) { ScrollFlags = AppBarLayout.LayoutParams.ScrollFlagScroll | AppBarLayout.LayoutParams.ScrollFlagExitUntilCollapsed } };

        collapsing.SetTitle(_targetArtist.Name);
        collapsing.SetExpandedTitleColor(Color.White);
        collapsing.SetCollapsedTitleTextColor(Color.Black);

        // Hero Image
        var heroImg = new ImageView(ctx) { LayoutParameters = new CollapsingToolbarLayout.LayoutParams(-1, -1) { CollapseMode = CollapsingToolbarLayout.LayoutParams.CollapseModeParallax } };
        heroImg.SetScaleType(ImageView.ScaleType.CenterCrop);
        Glide.With(this).Load(_targetArtist.ImagePath).Into(heroImg);
        collapsing.AddView(heroImg);

        appBar.AddView(collapsing);
        root.AddView(appBar);

        // Body (Tabs)
        var bodyLayout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        var e = bodyLayout.LayoutParameters = new CoordinatorLayout.LayoutParams(-1, -1);
        var ee = e as CoordinatorLayout.LayoutParams;

        ee?.Behavior = new AppBarLayout.ScrollingViewBehavior();

        var tabLayout = new TabLayout(ctx);
        var viewPager = new ViewPager2(ctx) { LayoutParameters = new LinearLayout.LayoutParams(-1, 0, 1f) };

        bodyLayout.AddView(tabLayout);
        bodyLayout.AddView(viewPager);
        root.AddView(bodyLayout);

        viewPager.Adapter = new ArtistStatsPagerAdapter(this, _viewModel);
        new TabLayoutMediator(tabLayout, viewPager, new ArtistTabStrategy()).Attach();

        return root;
    }

    public override void OnResume()
    {
        base.OnResume();
        // Load the specific artist's stats when fragment opens
        _ = _viewModel.LoadArtistStatsAsync(_targetArtist);
    }

    class ArtistTabStrategy : Java.Lang.Object, TabLayoutMediator.ITabConfigurationStrategy
    {
        public void OnConfigureTab(TabLayout.Tab tab, int pos) => tab.SetText(pos switch { 0 => "Insights", 1 => "Top Tracks", _ => "" });
    }
}

public class ArtistStatsPagerAdapter : FragmentStateAdapter
{
    private readonly StatisticsViewModel _vm;
    public ArtistStatsPagerAdapter(Fragment f, StatisticsViewModel vm) : base(f) { _vm = vm; }
    public override int ItemCount => 2;

    public override Fragment CreateFragment(int position)
    {
        return position switch
        {
            // TAB 1: Text Grid Metrics from Summary
            0 => new StatsDynamicTabFragment(_vm,
                gridExtractor: vm => {
                    var sum = vm.ArtistStats?.Summary;
                    if (sum == null) return new Dictionary<string, string>();
                    return new Dictionary<string, string> {
                        { "Total Plays", sum.TotalPlaysAcrossAllSongs.ToString() },
                        { "Time Listened", sum.TotalListeningTimeFormatted },
                        { "Unique Songs Played", $"{sum.UniqueSongsPlayed} / {sum.TotalSongsInLibrary}" },
                        { "Most Played", sum.MostPlayedSongTitle ?? "N/A" },
                        { "Eddington Number", sum.ArtistEddingtonNumber.ToString() }
                    };
                }, carouselExtractor: null),

            // TAB 2: Dynamic Carousels using PlottableData
            1 => new StatsDynamicTabFragment(_vm, null,
                carouselExtractor: vm => {
                    var data = vm.ArtistStats?.PlottableData;
                    if (data == null) return null;
                    return new Dictionary<string, List<DimmerStats>>
                    {
                        // NOTE: You will need a mapper here if PlottableData returns LabelValue instead of DimmerStats
                        // { "Plays by Day of Week", data.PlaysPerDayOfWeek.Select(x => new DimmerStats { Label = x.Label, Value = x.Value.ToString() }).ToList() }
                    };
                }),
            _ => new Fragment()
        };
    }
}