
using ScrollView = Android.Widget.ScrollView;

namespace Dimmer.ViewsAndPages.NativeViews.StatsSection;

public class StatsInsightsFragment : Fragment
{
    private readonly BaseViewModelAnd _vm;
    private readonly StatisticsViewModel _statisticsVM;
    public StatsInsightsFragment(BaseViewModelAnd vm, StatisticsViewModel statsVM
        ) 
    { 
        _statisticsVM = statsVM;
        _vm = vm; 
}

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context;
        var scroll = new ScrollView(ctx) { LayoutParameters = new ViewGroup.LayoutParams(-1, -1) };
        var root = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        root.SetPadding(24, 24, 24, 24);

        // --- Rediscovered Gems (Gold) ---
        if (_statisticsVM.Stats?.TopRediscoveredSongs != null && _statisticsVM.Stats.TopRediscoveredSongs.Any())
        {
            root.AddView(CreateInsightCard(ctx, "Rediscovered Gems", "Long gap between plays",
                Color.Goldenrod, _statisticsVM.Stats.TopRediscoveredSongs, "days gap"));
        }

        // --- Burnout (Gray/DarkSlate) ---
        if (_statisticsVM.Stats?.TopBurnoutSongs != null && _statisticsVM.Stats.TopBurnoutSongs.Any())
        {
            root.AddView(CreateInsightCard(ctx, "Burnout Tracks", "High skip rate recently",
                Color.DarkSlateGray, _statisticsVM.Stats.TopBurnoutSongs, "% skip rate"));
        }

        // --- Most Skipped Artists (Red) ---
        if (_statisticsVM.Stats?.TopArtistsBySkips != null && _statisticsVM.Stats.TopArtistsBySkips.Any())
        {
            root.AddView(CreateInsightCard(ctx, "Skipped Artists", "Artists you skip often",
               Color.ParseColor("#CD5C5C"), _statisticsVM.Stats.TopArtistsBySkips, "skips")); // IndianRed
        }

        // --- Never Played ---
        var dustCard = new MaterialCardView(ctx) { Radius = 16 };
        dustCard.SetCardBackgroundColor(Color.ParseColor("#2A2A2A"));
        var dustLy = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        dustLy.SetPadding(32, 32, 32, 32);

        dustLy.AddView(new TextView(ctx) { Text = "Collection Dust", TextSize = 18, Typeface = Typeface.DefaultBold });
        var count = _statisticsVM.Stats?.CollectionSummary?.SongsNeverPlayed ?? 0;
        dustLy.AddView(new TextView(ctx) { Text = count.ToString("N0"), TextSize = 36, Typeface = Typeface.DefaultBold});
        dustLy.AddView(new TextView(ctx) { Text = "Songs in your library with 0 plays.", TextSize = 12, Alpha = 0.6f });

        dustCard.AddView(dustLy);
        //((LinearLayout.LayoutParams)dustCard.LayoutParameters).TopMargin = 24;
        root.AddView(dustCard);

        scroll.AddView(root);
        return scroll;
    }

    private View CreateInsightCard(Context ctx, string title, string sub, Color strokeColor, System.Collections.IEnumerable items, string suffix)
    {
        var card = new MaterialCardView(ctx) { Radius = 16 };
        card.StrokeColor = strokeColor;
        card.StrokeWidth = AppUtil.DpToPx(2); // Helper needed or use (int)(2 * resources.DisplayMetrics.Density)
        card.SetCardBackgroundColor(Color.Transparent); // Outlined style

        var lp = new LinearLayout.LayoutParams(-1, -2);
        lp.SetMargins(0, 0, 0, 24);
        card.LayoutParameters = lp;

        var mainLy = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        mainLy.SetPadding(24, 24, 24, 24);

        // Header
        var hTitle = new TextView(ctx) { Text = title, TextSize = 18, Typeface = Typeface.DefaultBold};
        var hSub = new TextView(ctx) { Text = sub, TextSize = 12, Alpha = 0.7f };
        mainLy.AddView(hTitle);
        mainLy.AddView(hSub);

        // Items
        var listLy = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        ((LinearLayout.LayoutParams)listLy.LayoutParameters).TopMargin = 16;

        int count = 0;
        foreach (var item in items)
        {
            if (count++ > 4) break;
            var statsItem = item as DimmerStats;
            if (statsItem == null) continue;

            var row = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
            row.SetPadding(0, 8, 0, 8);

            var t = new TextView(ctx) { Text = statsItem.Song?.Title ?? statsItem.Name, TextSize = 14, Typeface = Typeface.DefaultBold };
            t.LayoutParameters = new LinearLayout.LayoutParams(0, -2, 1f);

            var v = new TextView(ctx) { Text = $"{statsItem.Value} {suffix}", TextSize = 12 };

            row.AddView(t);
            row.AddView(v);
            listLy.AddView(row);
        }
        mainLy.AddView(listLy);
        card.AddView(mainLy);
        return card;
    }
}