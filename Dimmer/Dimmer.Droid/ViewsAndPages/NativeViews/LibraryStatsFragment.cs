using Google.Android.Material.ProgressIndicator;

using ScrollView = Android.Widget.ScrollView;

namespace Dimmer.ViewsAndPages.NativeViews;

public class LibraryStatsFragment : Fragment
{
    private BaseViewModelAnd MyViewModel;
    private StatisticsViewModel StatsViewModel;

    public TextView listeningHrs { get; private set; }
    public CollectionStatsSummary? LibColStats { get; set; }
    public LibraryStatsBundle? LibStats { get; set; }
    public string TotalPlaysValue { get; private set; } = 0.ToString();
    public string TotalSkipValue { get; private set; } = 0.ToString();
    public string TotalSongsCount { get; private set; } = 0.ToString();
    public string DistinctAlbums { get; private set; } = 0.ToString();
    public string AverageDuration { get; private set; } = 0.ToString();
    public string TotalListeningTime { get; private set; } = 0.ToString();
    public string SongsWithLyrics { get; private set; } = 0.ToString();
    public string SongsWithSyncedLyrics { get; private set; } = 0.ToString();
    public string DistinctArtists { get; private set; } = 0.ToString();
    public string SongsFavorited { get; private set; } = 0.ToString();
    public string SongsPlayedToCompletionCount { get; private set; } = 0.ToString();
    public string MostPlayedSongCount { get; private set; } = 0.ToString();
    public string MostSkippedSongCount { get; private set; } = 0.ToString();
    public string SongsPlayedToday { get; private set; } = 0.ToString();
    public string SongsPlayedAtNight { get; private set; } = 0.ToString();

    public LibraryStatsFragment(BaseViewModelAnd vm) 
    { 
        MyViewModel = vm;
        
    }
    public override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        StatsViewModel = MainApplication.ServiceProvider.GetService<StatisticsViewModel>()!;

        // Fire the task here
        Task.Run(() => StatsViewModel.LoadLibraryStatsCommand.Execute(null));
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context;
        var scroll = new ScrollView(ctx) { FillViewport = true };
        var root = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        root.SetPadding(30, 30, 30, 150);

        root.AddView(new MaterialTextView(ctx) { Text = "Library Overview", TextSize = 28 });

        // Total Hours Card
        var summaryCard = new Google.Android.Material.Card.MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(16),
            CardElevation = AppUtil.DpToPx(4),
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, AppUtil.DpToPx(150))
        };
        ((LinearLayout.LayoutParams)summaryCard.LayoutParameters).SetMargins(0, 30, 0, 30);
        summaryCard.SetBackgroundColor(Color.DarkSlateBlue);

        var summaryLy = new LinearLayout(ctx) { Orientation = Orientation.Vertical};
        summaryLy.SetGravity(GravityFlags.Center);
        listeningHrs = new TextView(ctx) { Text = StatsViewModel.LibraryStats?.CollectionSummary?.TotalListeningTime.ToString(), TextSize = 48, Typeface = Typeface.DefaultBold };
        summaryLy.AddView(listeningHrs);
        summaryLy.AddView(new TextView(ctx) { Text =  "Hours Listened"});
        summaryCard.AddView(summaryLy);
        root.AddView(summaryCard);



        // 2. THE DASHBOARD GRID (Listening Stats)
        var statsTitle = CreateHeader(ctx, "Listening Insights");
        root.AddView(statsTitle);

        var statsGrid = new GridLayout(ctx) { ColumnCount = 2, AlignmentMode = GridAlign.Bounds };

        // Card 1: Playback Vitality
        var totalPlays = CreateMetricCard(ctx, "Total Plays", TotalPlaysValue, Resource.Drawable.playcircle, Color.ParseColor("#4CAF50"));
        statsGrid.AddView(totalPlays);

        // Card 2: Skip Tendency (Red/Orange if high)
        var skipColor = double.Parse(TotalSkipValue) > 0.5 ? Color.Red : Color.ParseColor("#FF9800");
        statsGrid.AddView(CreateMetricCard(ctx, "Skip Rate", $"{TotalPlaysValue:P0}", Resource.Drawable.media3_icon_skip_forward, skipColor));

        // Card 3: Completion
        statsGrid.AddView(CreateMetricCard(ctx, "TotalSongs", TotalSongsCount, Android.Resource.Drawable.StatSysUploadDone, Color.ParseColor("#2196F3")));
        statsGrid.AddView(CreateMetricCard(ctx, "DistinctArtists", DistinctArtists, Android.Resource.Drawable.StatSysUploadDone, Color.ParseColor("#2196F3")));
        statsGrid.AddView(CreateMetricCard(ctx, "DistinctAlbums", DistinctAlbums, Android.Resource.Drawable.StatSysUploadDone, Color.ParseColor("#2196F3")));
        statsGrid.AddView(CreateMetricCard(ctx, "AverageDuration", AverageDuration, Android.Resource.Drawable.StatSysUploadDone, Color.ParseColor("#2196F3")));
        statsGrid.AddView(CreateMetricCard(ctx, "TotalListeningTime", TotalListeningTime, Android.Resource.Drawable.StatSysUploadDone, Color.ParseColor("#2196F3")));
        statsGrid.AddView(CreateMetricCard(ctx, "SongsWithLyrics", SongsWithLyrics, Android.Resource.Drawable.StatSysUploadDone, Color.ParseColor("#2196F3")));
        statsGrid.AddView(CreateMetricCard(ctx, "SongsWithSyncedLyrics", SongsWithSyncedLyrics, Android.Resource.Drawable.StatSysUploadDone, Color.ParseColor("#2196F3")));
        statsGrid.AddView(CreateMetricCard(ctx, "SongsFavorited", SongsFavorited, Android.Resource.Drawable.StatSysUploadDone, Color.ParseColor("#2196F3")));
        statsGrid.AddView(CreateMetricCard(ctx, "SongsPlayedToCompletionCount", SongsPlayedToCompletionCount, Android.Resource.Drawable.StatSysUploadDone, Color.ParseColor("#2196F3")));
        statsGrid.AddView(CreateMetricCard(ctx, "MostPlayedSongCount", MostPlayedSongCount, Android.Resource.Drawable.StatSysUploadDone, Color.ParseColor("#2196F3")));
        statsGrid.AddView(CreateMetricCard(ctx, "MostSkippedSongCount", MostSkippedSongCount, Android.Resource.Drawable.StatSysUploadDone, Color.ParseColor("#2196F3")));
        statsGrid.AddView(CreateMetricCard(ctx, "SongsPlayedToday", SongsPlayedToday, Android.Resource.Drawable.StatSysUploadDone, Color.ParseColor("#2196F3")));
        statsGrid.AddView(CreateMetricCard(ctx, "SongsPlayedAtNight", SongsPlayedAtNight, Android.Resource.Drawable.StatSysUploadDone, Color.ParseColor("#2196F3")));

        // Card 4: Engagement Score

        root.AddView(statsGrid);


        scroll.AddView(root);
        return scroll;
    }

    public override void OnResume()
    {
        base.OnResume();
        StatsViewModel.WhenPropertyChange(nameof(StatsViewModel.LibraryStats),
            newVal => StatsViewModel.LibraryStats)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(vall =>
            {
                if (vall is not null)
                {
                    LibColStats = vall.CollectionSummary!;
                    LibStats = vall;
                    TotalPlaysValue = LibColStats.TotalPlayCount.ToString();
                    TotalSkipValue = LibColStats.TotalSkipCount.ToString();
                    TotalSongsCount = LibColStats.TotalSongs.ToString();
                    DistinctAlbums = LibColStats.DistinctAlbums.ToString();
                    AverageDuration = LibColStats.AverageDuration.ToString();
                    SongsWithLyrics = LibColStats.SongsWithLyrics.ToString();
                    TotalListeningTime = LibColStats.TotalListeningTime.ToString();
                    SongsWithSyncedLyrics = LibColStats.SongsWithSyncedLyrics.ToString();
                    DistinctArtists = LibColStats.DistinctArtists.ToString();
                    SongsFavorited = LibColStats.SongsFavorited.ToString();
                    SongsPlayedToCompletionCount = LibColStats.SongsPlayedToCompletionCount.ToString();
                    MostPlayedSongCount = LibColStats.MostPlayedSongCount.ToString();
                    MostSkippedSongCount = LibColStats.MostSkippedSongCount.ToString();
                    SongsPlayedToday = LibColStats.SongsPlayedToday.ToString();
                    SongsPlayedAtNight = LibColStats.SongsPlayedAtNight.ToString();

                }
            });
    }
    private MaterialCardView CreateMetricCard(Context ctx, string label, string value, int iconRes, Color themeColor)
    {
        var card = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(20),
            CardElevation = 0,
            StrokeWidth = 3,
        };
        card.SetStrokeColor(ColorStateList.ValueOf(Color.ParseColor("#10808080")));

        var lp = new GridLayout.LayoutParams();
        lp.Width = 0;
        lp.Height = ViewGroup.LayoutParams.WrapContent;
        lp.ColumnSpec = GridLayout.InvokeSpec(GridLayout.Undefined, 1f);
        lp.SetMargins(12, 12, 12, 12);
        card.LayoutParameters = lp;

        var layout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        layout.SetPadding(30, 30, 30, 30);

        var icon = new ImageView(ctx);
        icon.SetImageResource(iconRes);
        icon.SetColorFilter(themeColor);
        icon.LayoutParameters = new LinearLayout.LayoutParams(60, 60) { BottomMargin = 16 };

        var valTxt = new TextView(ctx) { Text = value, TextSize = 20, Typeface = Typeface.DefaultBold };
        var labTxt = new TextView(ctx) { Text = label, TextSize = 12, Alpha = 0.6f };

        layout.AddView(icon);
        layout.AddView(valTxt);
        layout.AddView(labTxt);
        card.AddView(layout);

        // Animation on Tap
        card.Click += (s, e) => {
            card.Animate().ScaleX(0.95f).ScaleY(0.95f).SetDuration(100).WithEndAction(new Java.Lang.Runnable(() => {
                card.Animate().ScaleX(1f).ScaleY(1f).SetDuration(100);
            }));
        };

        return card;
    }

    private LinearLayout CreateDnaRow(Context ctx, string label, string? val)
    {
        var row = new LinearLayout(ctx) { Orientation = Orientation.Horizontal, WeightSum = 1 };
        row.SetPadding(0, 8, 0, 8);
        var t1 = new TextView(ctx) { Text = label, LayoutParameters = new LinearLayout.LayoutParams(0, -2, 0.4f), Alpha = 0.6f };
        var t2 = new TextView(ctx) { Text = val, LayoutParameters = new LinearLayout.LayoutParams(0, -2, 0.6f), Typeface = Typeface.Monospace };
        row.AddView(t1);
        row.AddView(t2);
        return row;
    }

    private TextView CreateHeader(Context ctx, string text)
    {
        var tv = new TextView(ctx) { Text = text.ToUpper(), TextSize = 12, Typeface = Typeface.DefaultBold };
        tv.LetterSpacing = 0.1f;
        tv.SetPadding(10, 40, 0, 10);
        tv.SetTextColor(Color.ParseColor("#808080"));
        return tv;
    }
    private LinearLayout CreateGenreProgress(Context ctx, string label, int progress, Color color)
    {
        var ly = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        ly.SetPadding(0, 20, 0, 10);

        var txt = new TextView(ctx) { Text = label };
        var bar = new LinearProgressIndicator(ctx)
        {
            Progress = progress,
            TrackCornerRadius = AppUtil.DpToPx(4),
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, AppUtil.DpToPx(10))
        };
        
        bar.SetIndicatorColor(color);

        ly.AddView(txt);
        ly.AddView(bar);
        return ly;
    }
}