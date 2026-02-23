using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.CoordinatorLayout.Widget;
using AndroidX.ViewPager2.Adapter;
using AndroidX.ViewPager2.Widget;
using Dimmer.ViewModel;
using Google.Android.Material.AppBar;
using Google.Android.Material.Chip;
using Google.Android.Material.ProgressIndicator;
using Google.Android.Material.Tabs;
using Google.Android.Material.TextView;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Dimmer.ViewsAndPages.NativeViews.Stats;

public class GlobalLibraryStatsFragment : Fragment
{
    private readonly StatisticsViewModel _viewModel;
    private CompositeDisposable _disposables = new();
    private CircularProgressIndicator _loadingIndicator = null!;

    public GlobalLibraryStatsFragment(StatisticsViewModel viewModel) { _viewModel = viewModel; }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = RequireContext();
        var root = new CoordinatorLayout(ctx) { LayoutParameters = new ViewGroup.LayoutParams(-1, -1) };

        // --- HEADER ---
        var appBarLayout = new AppBarLayout(ctx) { LayoutParameters = new CoordinatorLayout.LayoutParams(-1, -2) };
        var headerContent = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        headerContent.SetPadding(40, 60, 40, 20);

        // Title & Loader Layout
        var titleRow = new LinearLayout(ctx) { Orientation = Orientation.Horizontal
        };

        titleRow.SetGravity(GravityFlags.Center);
        titleRow.AddView(new MaterialTextView(ctx) { Text = "Library Stats", TextSize = 32, Typeface = Typeface.DefaultBold, LayoutParameters = new LinearLayout.LayoutParams(0, -2, 1f) });

        _loadingIndicator = new CircularProgressIndicator(ctx) { Visibility = ViewStates.Invisible, LayoutParameters = new LinearLayout.LayoutParams(60, 60) };
        titleRow.AddView(_loadingIndicator);
        headerContent.AddView(titleRow);

        // FILTER CHIPS (Reactive!)
        var chipScroll = new HorizontalScrollView(ctx) { HorizontalScrollBarEnabled = false };
        var chipGroup = new ChipGroup(ctx) { SingleSelection = true };

        foreach (var filter in _viewModel.AvailableFilters)
        {
            var chip = new Chip(ctx) { Text = filter.ToString().Replace("Last", "Last "), Checkable = true, Checked = _viewModel.SelectedFilter == filter };
            chip.CheckedChange += (s, e) => { if (e.IsChecked) _viewModel.SelectedFilter = filter; };
            chipGroup.AddView(chip);
        }
        chipScroll.AddView(chipGroup);
        headerContent.AddView(chipScroll);

        var collapsingBar = new CollapsingToolbarLayout(ctx) { LayoutParameters = new AppBarLayout.LayoutParams(-1, -2) { ScrollFlags = AppBarLayout.LayoutParams.ScrollFlagScroll | AppBarLayout.LayoutParams.ScrollFlagEnterAlways } };
        collapsingBar.AddView(headerContent);
        appBarLayout.AddView(collapsingBar);
        root.AddView(appBarLayout);

        // --- TABS ---
        var bodyLayout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
       var e = bodyLayout.LayoutParameters = new CoordinatorLayout.LayoutParams(-1, -1);
        var ee = e as CoordinatorLayout.LayoutParams;

        ee?.Behavior = new AppBarLayout.ScrollingViewBehavior();

        var tabLayout = new TabLayout(ctx) { TabMode = TabLayout.ModeScrollable };
        var viewPager = new ViewPager2(ctx) { LayoutParameters = new LinearLayout.LayoutParams(-1, 0, 1f), OffscreenPageLimit = 4 };

        bodyLayout.AddView(tabLayout);
        bodyLayout.AddView(viewPager);
        root.AddView(bodyLayout);

        // ATTACH ADAPTER
        viewPager.Adapter = new LibraryStatsPagerAdapter(this, _viewModel);
        new TabLayoutMediator(tabLayout, viewPager, new TabStrategy()).Attach();

        return root;
    }

    public override void OnResume()
    {
        base.OnResume();
        _disposables = new CompositeDisposable();
        if (_viewModel.LibraryStats == null) _viewModel.LoadLibraryStatsCommand.Execute(null);

        _viewModel.WhenPropertyChange(nameof(_viewModel.IsBusy), v => _viewModel.IsBusy)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(isBusy => _loadingIndicator.Visibility = isBusy ? ViewStates.Visible : ViewStates.Invisible)
            .DisposeWith(_disposables);
    }

    public override void OnPause() { base.OnPause(); _disposables.Dispose(); }

    class TabStrategy : Java.Lang.Object, TabLayoutMediator.ITabConfigurationStrategy
    {
        public void OnConfigureTab(TabLayout.Tab tab, int pos) => tab.SetText(pos switch { 0 => "Overview", 1 => "Top Tracks", 2 => "Top Artists", 3 => "Trends", _ => "" });
    }
}


public class LibraryStatsPagerAdapter : FragmentStateAdapter
{
    private readonly StatisticsViewModel _vm;
    public LibraryStatsPagerAdapter(Fragment f, StatisticsViewModel vm) : base(f) { _vm = vm; }
    public override int ItemCount => 4;

    public override Fragment CreateFragment(int position)
    {
        return position switch
        {
            0 => new StatsDynamicTabFragment(_vm,
                gridExtractor: vm => new Dictionary<string, string> {
                    { "Hours Listened", vm.LibraryStats?.CollectionSummary?.TotalListeningTime.ToString() ?? "0" },
                    { "Total Plays", vm.LibraryStats?.CollectionSummary?.TotalPlayCount.ToString() ?? "0" },
                    { "Total Skips", vm.LibraryStats?.CollectionSummary?.TotalSkipCount.ToString() ?? "0" },
                    { "Distinct Artists", vm.LibraryStats?.CollectionSummary?.DistinctArtists.ToString() ?? "0" }
                },
                carouselExtractor   : null), // No carousels on Overview

            1 => new StatsDynamicTabFragment(_vm, null,
                carouselExtractor: vm => new Dictionary<string, List<DimmerStats>> {
                    { "Top Played Tracks", vm.LibraryStats?.TopSongsByPlays },
                    { "Most Time Listened", vm.LibraryStats?.TopSongsByTime },
                    { "Most Skipped", vm.LibraryStats?.TopSongsBySkips }
                }),

            2 => new StatsDynamicTabFragment(_vm, null,
                carouselExtractor: vm => new Dictionary<string, List<DimmerStats>> {
                    { "Top Artists", vm.LibraryStats?.TopArtistsByPlays },
                    { "Most Diverse Artists", vm.LibraryStats?.TopArtistsByVariety },
                    { "Artist Footprint", vm.LibraryStats?.ArtistFootprint }
                }),

            _ => new Fragment()
        };
    }
}