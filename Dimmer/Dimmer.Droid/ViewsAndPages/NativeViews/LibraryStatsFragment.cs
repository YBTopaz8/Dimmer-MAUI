using Android.Animation;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Widget; // Required for NestedScrollView
using AndroidX.RecyclerView.Widget; // Required for RecyclerView
using Google.Android.Material.Card;
using Google.Android.Material.TextView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Dimmer.ViewsAndPages.NativeViews;

public class LibraryStatsFragment : Fragment
{
    private BaseViewModelAnd MyViewModel;
    private StatisticsViewModel StatsViewModel;
    private TextView _listeningHrsView;
    private CompositeDisposable _disposables = new();

    // Adapter for our RecyclerView
    private MetricsAdapter _metricsAdapter;

    public LibraryStatsFragment(BaseViewModelAnd vm)
    {
        MyViewModel = vm;
    }

    public override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        StatsViewModel = MainApplication.ServiceProvider.GetService<StatisticsViewModel>()!;

        // Safe to execute Task here instead of constructor
        Task.Run(() => StatsViewModel.LoadLibraryStatsCommand.Execute(null));
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = RequireContext();

        var scroll = new NestedScrollView(ctx) { FillViewport = true };

        var root = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        root.SetPadding(30, 30, 30, 150);

        root.AddView(new MaterialTextView(ctx) { Text = "Library Overview", TextSize = 28 });

        // --- TOP SUMMARY CARD ---
        var summaryCard = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(16),
            CardElevation = AppUtil.DpToPx(4),
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, AppUtil.DpToPx(150))
        };
        ((LinearLayout.LayoutParams)summaryCard.LayoutParameters).SetMargins(0, 30, 0, 30);
        summaryCard.SetBackgroundColor(Color.DarkSlateBlue);

        var summaryLy = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        summaryLy.SetGravity(GravityFlags.Center);

        _listeningHrsView = new TextView(ctx) { TextSize = 48, Typeface = Typeface.DefaultBold };
        StartShimmer(_listeningHrsView); // Shimmer for main header

        summaryLy.AddView(_listeningHrsView);
        summaryLy.AddView(new TextView(ctx) { Text = "Hours Listened" });
        summaryCard.AddView(summaryLy);
        root.AddView(summaryCard);

        // --- DASHBOARD GRID (RECYCLER VIEW) ---
        root.AddView(CreateHeader(ctx, "Listening Insights"));

        // Initialize Data Source
        var initialMetrics = new List<MetricItem>
        {
            new("TotalPlays", "Total Plays", Resource.Drawable.playcircle, Color.ParseColor("#4CAF50")),
            new("SkipRate", "Skip Tendency", Resource.Drawable.media3_icon_skip_forward, Color.ParseColor("#FF9800")),
            new("TotalSongs", "Total Songs", Android.Resource.Drawable.StatSysUploadDone, Color.ParseColor("#2196F3")),
            new("DistinctArtists", "Distinct Artists", Android.Resource.Drawable.StatSysUploadDone, Color.ParseColor("#2196F3")),
            new("DistinctAlbums", "Distinct Albums", Android.Resource.Drawable.StatSysUploadDone, Color.ParseColor("#2196F3")),
            new("AverageDuration", "Average Duration", Android.Resource.Drawable.StatSysUploadDone, Color.ParseColor("#2196F3")),
            new("SongsWithLyrics", "Songs w/ Lyrics", Android.Resource.Drawable.StatSysUploadDone, Color.ParseColor("#2196F3")),
            new("SongsWithSyncedLyrics", "Songs w/ Sync Lyrics", Resource.Drawable.lyrics, Color.ParseColor("#2196F3")),
            new("SongsFavorited", "Favorited", Resource.Drawable.favlove, Color.ParseColor("#2196F3")),
            new("SongsPlayedToday", "Played Today", Android.Resource.Drawable.StatSysUploadDone, Color.ParseColor("#2196F3")),
            new("TotalSongsCount", "Total Songs Count", Resource.Drawable.songss, Color.ParseColor("#2196F3"))
        };

        // Initialize Adapter & RecyclerView
        _metricsAdapter = new MetricsAdapter(ctx, initialMetrics);

        var recyclerView = new RecyclerView(ctx)
        {
            NestedScrollingEnabled = false, // Let the NestedScrollView handle scrolling
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        recyclerView.SetLayoutManager(new GridLayoutManager(ctx, 2));
        recyclerView.SetAdapter(_metricsAdapter);

        root.AddView(recyclerView);
        scroll.AddView(root);
        return scroll;
    }

    public override void OnResume()
    {
        base.OnResume();

        var observableStream = StatsViewModel.WhenPropertyChange(nameof(StatsViewModel.LibraryStats), newVal => StatsViewModel.LibraryStats)
            .ObserveOn(RxSchedulers.UI);

        // FIX: Explicitly call System.ObservableExtensions.Subscribe to fix ambiguity with AudioSwitcher
        var sub = System.ObservableExtensions.Subscribe(observableStream, stats =>
        {
            if (stats?.CollectionSummary != null)
            {
                var sum = stats.CollectionSummary;

                // Stop Shimmer and update header
                UpdateTopHeaderView(_listeningHrsView, sum.TotalListeningTime.ToString());

                // Bulk update the RecyclerView Data
                _metricsAdapter.UpdateMetric("TotalPlays", sum.TotalPlayCount.ToString());
                _metricsAdapter.UpdateMetric("SkipRate", sum.TotalSkipCount.ToString());
                _metricsAdapter.UpdateMetric("TotalSongs", sum.TotalSongs.ToString());
                _metricsAdapter.UpdateMetric("DistinctArtists", sum.DistinctArtists.ToString());
                _metricsAdapter.UpdateMetric("DistinctAlbums", sum.DistinctAlbums.ToString());
                _metricsAdapter.UpdateMetric("AverageDuration", sum.AverageDuration.ToString());
                _metricsAdapter.UpdateMetric("SongsWithLyrics", sum.SongsWithLyrics.ToString());
                _metricsAdapter.UpdateMetric("SongsWithSyncedLyrics", sum.SongsWithSyncedLyrics.ToString());
                _metricsAdapter.UpdateMetric("SongsFavorited", sum.SongsFavorited.ToString());
                _metricsAdapter.UpdateMetric("SongsPlayedToday", sum.SongsPlayedToday.ToString());
                _metricsAdapter.UpdateMetric("TotalSongsCount", sum.TotalSongs.ToString());
            }
        });

        _disposables.Add(sub);
    }

    public override void OnPause()
    {
        base.OnPause();
        // Prevent memory leaks!
        _disposables.Clear();
    }

    private void UpdateTopHeaderView(TextView tv, string newValue)
    {
        tv.Animation?.Cancel();
        tv.Alpha = 1f;
        tv.Text = newValue;
    }

    private void StartShimmer(TextView view)
    {
        view.Text = "----";
        var animator = ObjectAnimator.OfFloat(view, "alpha", 0.3f, 1f);
        animator?.SetDuration(800);
        animator?.RepeatMode = ValueAnimatorRepeatMode.Reverse;
        animator?.RepeatCount = ValueAnimator.Infinite;
        animator?.Start();
    }

    private TextView CreateHeader(Context ctx, string text)
    {
        var tv = new TextView(ctx) { Text = text.ToUpper(), TextSize = 12, Typeface = Typeface.DefaultBold };
        tv.LetterSpacing = 0.1f;
        tv.SetPadding(10, 40, 0, 10);
        tv.SetTextColor(Color.ParseColor("#808080"));
        return tv;
    }
}

// --- NEW DATA MODEL ---
public class MetricItem
{
    public string Key { get; }
    public string Label { get; }
    public string? Value { get; set; } // Null implies it's still loading (Shimmer)
    public int IconRes { get; }
    public Color ThemeColor { get; }

    public MetricItem(string key, string label, int iconRes, Color themeColor)
    {
        Key = key;
        Label = label;
        IconRes = iconRes;
        ThemeColor = themeColor;
        Value = null;
    }
}

public class MetricsAdapter : RecyclerView.Adapter
{
    private readonly Context _ctx;
    private readonly List<MetricItem> _items;

    public MetricsAdapter(Context ctx, List<MetricItem> items)
    {
        _ctx = ctx;
        _items = items;
    }

    public override int ItemCount => _items.Count;

    public void UpdateMetric(string key, string newValue)
    {
        var itemIndex = _items.FindIndex(x => x.Key == key);
        if (itemIndex >= 0 && _items[itemIndex].Value != newValue)
        {
            _items[itemIndex].Value = newValue;
            NotifyItemChanged(itemIndex); // Only updates the exact UI element that changed
        }
    }

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        var card = new MaterialCardView(_ctx)
        {
            Radius = AppUtil.DpToPx(20),
            CardElevation = 0,
            StrokeWidth = 3,
        };
        card.SetStrokeColor(ColorStateList.ValueOf(Color.ParseColor("#10808080")));

        var lp = new GridLayoutManager.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
        lp.SetMargins(12, 12, 12, 12);
        card.LayoutParameters = lp;

        var layout = new RelativeLayout(_ctx);
        layout.SetPadding(30, 30, 30, 30);

        var icon = new ImageView(_ctx) { Id = View.GenerateViewId() };
        var iconLp = new RelativeLayout.LayoutParams(60, 60);
        iconLp.AddRule(LayoutRules.AlignParentTop);
        iconLp.AddRule(LayoutRules.AlignParentLeft);
        iconLp.BottomMargin = 16;
        icon.LayoutParameters = iconLp;

        var valTxt = new TextView(_ctx) { Id = View.GenerateViewId(), TextSize = 20, Typeface = Typeface.DefaultBold };
        var valLp = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
        valLp.AddRule(LayoutRules.Below, icon.Id);
        valLp.AddRule(LayoutRules.AlignParentLeft);
        valTxt.LayoutParameters = valLp;

        var labTxt = new TextView(_ctx) { Id = View.GenerateViewId(), TextSize = 12, Alpha = 0.6f };
        var labLp = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
        labLp.AddRule(LayoutRules.Below, valTxt.Id);
        labLp.AddRule(LayoutRules.AlignParentLeft);
        labTxt.LayoutParameters = labLp;

        layout.AddView(icon);
        layout.AddView(valTxt);
        layout.AddView(labTxt);
        card.AddView(layout);

        return new MetricViewHolder(card, icon, valTxt, labTxt);
    }

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        if (holder is MetricViewHolder vh)
        {
            var item = _items[position];

            vh.Icon.SetImageResource(item.IconRes);
            vh.Icon.SetColorFilter(item.ThemeColor);
            vh.LabTxt.Text = item.Label;

            // Shimmer Logic
            if (item.Value == null)
            {
                vh.ValTxt.Text = "----";
                StartShimmer(vh.ValTxt);
            }
            else
            {
                // Stop Shimmer and show text
                vh.ValTxt.Animation?.Cancel();
                vh.ValTxt.Alpha = 1f;
                vh.ValTxt.Text = item.Value;

                // Nice scale pop when data loads
                vh.ValTxt.Animate()?.ScaleX(1.1f).ScaleY(1.1f).SetDuration(150).WithEndAction(new Java.Lang.Runnable(() => {
                    vh.ValTxt.Animate()?.ScaleX(1f).ScaleY(1f).SetDuration(150);
                }));
            }
        }
    }

    private void StartShimmer(TextView view)
    {
        var animator = ObjectAnimator.OfFloat(view, "alpha", 0.3f, 1f);
        animator?.SetDuration(800);
        animator?.RepeatMode = ValueAnimatorRepeatMode.Reverse;
        animator?.RepeatCount = ValueAnimator.Infinite;
        animator?.Start();
    }
}

public class MetricViewHolder : RecyclerView.ViewHolder
{
    public ImageView Icon { get; }
    public TextView ValTxt { get; }
    public TextView LabTxt { get; }

    public MetricViewHolder(View itemView, ImageView icon, TextView valTxt, TextView labTxt) : base(itemView)
    {
        Icon = icon;
        ValTxt = valTxt;
        LabTxt = labTxt;

        // Button Animation on tap
        itemView.Click += (s, e) => {
            itemView.Animate()?.ScaleX(0.95f).ScaleY(0.95f).SetDuration(100).WithEndAction(new Java.Lang.Runnable(() => {
                itemView.Animate()?.ScaleX(1f).ScaleY(1f).SetDuration(100);
            }));
        };
    }
}