using AndroidX.SwipeRefreshLayout.Widget;
using Bumptech.Glide;
using Bumptech.Glide.Load.Resource.Bitmap;
using Google.Android.Material.Tabs;
using Hqub.Lastfm.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.ViewsAndPages.NativeViews.DimmerLive.LastFMViews;

public class LastFmInfoFragment : Fragment
{
    private readonly BaseViewModelAnd _viewModel;
    private SwipeRefreshLayout _swipeRefresh;
    private RecyclerView _tracksRecycler;
    private LastFmTrackAdapter _adapter;
    private TabLayout _tabLayout;
    private LinearLayout _dashboardContent;

    public LastFmInfoFragment(BaseViewModelAnd vm) { _viewModel = vm; }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context!;
        var scroll = new Android.Widget.ScrollView(ctx) { FillViewport = true };

        var root = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        root.LayoutParameters = new FrameLayout.LayoutParams(-1, -2); // Fixes scrolling

        // 1. PROFILE HERO SECTION
        var user = _viewModel.CurrentUserLocal?.LastFMAccountInfo;
        if (user != null)
        {
            root.AddView(CreateHeroHeader(ctx, user));

            // 2. DASHBOARD METRICS (Fitbit Style)
            root.AddView(CreateSectionHeader(ctx, "Your Scrobbles", "DASHBOARD"));
            root.AddView(CreateStatsGrid(ctx, user));
        }

        // 3. TABBED CONTENT
        _tabLayout = new TabLayout(ctx);
        _tabLayout.LayoutParameters = new LinearLayout.LayoutParams(-1, -2);
        _tabLayout.AddTab(_tabLayout.NewTab().SetText("Recent"));
        _tabLayout.AddTab(_tabLayout.NewTab().SetText("Top Tracks"));
        _tabLayout.AddTab(_tabLayout.NewTab().SetText("Top Albums"));
        _tabLayout.SetSelectedTabIndicatorColor(Color.ParseColor("#D51007"));
        root.AddView(_tabLayout);

        // 4. RECYCLER VIEW FOR LISTS
        _swipeRefresh = new SwipeRefreshLayout(ctx);
        _tracksRecycler = new RecyclerView(ctx);
        _tracksRecycler.SetLayoutManager(new LinearLayoutManager(ctx));
        _tracksRecycler.SetPadding(20, 20, 20, 40);
        _tracksRecycler.SetClipToPadding(false);

        _adapter = new LastFmTrackAdapter(ctx, new List<Track>(), _viewModel);
        _tracksRecycler.SetAdapter(_adapter);

        _swipeRefresh.AddView(_tracksRecycler);
        _swipeRefresh.Refresh += (s, e) => LoadCurrentTabData();

        root.AddView(_swipeRefresh);

        scroll.AddView(root);
        return scroll;
    }

    private View CreateHeroHeader(Context ctx, LastFMUserView user)
    {
        var root = new FrameLayout(ctx);
        root.LayoutParameters = new LinearLayout.LayoutParams(-1, AppUtil.DpToPx(240));

        var backdrop = new ImageView(ctx) { LayoutParameters = new FrameLayout.LayoutParams(-1, -1) };
        backdrop.SetScaleType(ImageView.ScaleType.CenterCrop);
        backdrop.Alpha = 0.2f;

        var content = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        content.SetGravity(GravityFlags.Center);

        var avatar = new ImageView(ctx);
        avatar.LayoutParameters = new LinearLayout.LayoutParams(AppUtil.DpToPx(100), AppUtil.DpToPx(100));

        var name = new TextView(ctx) { Text = user.Name, TextSize = 24, Typeface = Typeface.DefaultBold };
        name.SetTextColor(Color.White);

        var since = new TextView(ctx) { Text = $"Scrobbling since {user.Registered:MMM yyyy}", TextSize = 12, Alpha = 0.7f };
        since.SetTextColor(Color.White);

        content.AddView(avatar);
        content.AddView(name);
        content.AddView(since);

        root.AddView(backdrop);
        root.AddView(content);

        Glide.With(this).Load(user.Image?.Url).Transform(new CircleCrop()).Into(avatar);
        Glide.With(this).Load(user.Image?.Url).Into(backdrop);

        return root;
    }

    private View CreateStatsGrid(Context ctx, LastFMUserView user)
    {
        var grid = new GridLayout(ctx) { ColumnCount = 2 };
        grid.SetPadding(20, 0, 20, 20);

        grid.AddView(CreateMetricCard(ctx, "Total Scrobbles", user.Playcount.ToString("N0"), Resource.Drawable.playcircle, Color.ParseColor("#D51007")));

        double days = (DateTime.Now - user.Registered).TotalDays;
        string avg = (user.Playcount / (days > 0 ? days : 1)).ToString("F1");
        grid.AddView(CreateMetricCard(ctx, "Daily Avg", avg, Resource.Drawable.mtrl_ic_arrow_drop_up, Color.ParseColor("#FF9800")));

        return grid;
    }

    private View CreateMetricCard(Context ctx, string label, string value, int iconRes, Color themeColor)
    {
        var card = new MaterialCardView(ctx) { Radius = 40, CardElevation = 0 };
        card.SetCardBackgroundColor(Color.ParseColor("#08808080"));
        var lp = new GridLayout.LayoutParams(GridLayout.InvokeSpec(GridLayout.Undefined, 1f), GridLayout.InvokeSpec(GridLayout.Undefined, 1f));
        lp.SetMargins(10, 10, 10, 10);
        card.LayoutParameters = lp;

        var lay = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        lay.SetPadding(30, 30, 30, 30);

        var valTxt = new TextView(ctx) { Text = value, TextSize = 18, Typeface = Typeface.DefaultBold };
        var labTxt = new TextView(ctx) { Text = label, TextSize = 11, Alpha = 0.6f };

        lay.AddView(valTxt);
        lay.AddView(labTxt);
        card.AddView(lay);
        return card;
    }

    private View CreateSectionHeader(Context ctx, string title, string subtitle)
    {
        var tv = new TextView(ctx) { Text = $"{subtitle} • {title}".ToUpper() };
        tv.SetPadding(40, 40, 40, 10);
        tv.TextSize = 10;
        tv.LetterSpacing = 0.15f;
        tv.SetTextColor(Color.Gray);
        return tv;
    }

    private void LoadCurrentTabData() { /* Implementation to fetch from ViewModel */ _swipeRefresh.Refreshing = false; }
}