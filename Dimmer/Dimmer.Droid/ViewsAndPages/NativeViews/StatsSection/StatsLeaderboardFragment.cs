
using ScrollView = Android.Widget.ScrollView;

namespace Dimmer.ViewsAndPages.NativeViews.StatsSection;

public class StatsLeaderboardFragment : Fragment
{
    private readonly StatisticsViewModel _vm;
    public StatsLeaderboardFragment(StatisticsViewModel vm) { _vm = vm; }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context;
        var scroll = new ScrollView(ctx) { LayoutParameters = new ViewGroup.LayoutParams(-1, -1) };
        var root = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        root.SetPadding(24, 0, 24, 24);

        if (_vm.Stats?.TopSongsByPlays != null)
        {
            root.AddView(CreateSectionHeader(ctx, "Top Songs"));
            root.AddView(CreateListCard(ctx, _vm.Stats.TopSongsByPlays, false));
        }

        if (_vm.Stats?.TopArtistsByPlays != null)
        {
            root.AddView(CreateSectionHeader(ctx, "Top Artists"));
            root.AddView(CreateListCard(ctx, _vm.Stats.TopArtistsByPlays, true));
        }

        if (_vm.Stats?.TopSongsByTime != null)
        {
            root.AddView(CreateSectionHeader(ctx, "Longest Listened"));
            root.AddView(CreateListCard(ctx, _vm.Stats.TopSongsByTime, false, true)); // IsTime = true
        }

        scroll.AddView(root);
        return scroll;
    }

    private View CreateListCard(Context ctx, System.Collections.IEnumerable items, bool isArtist, bool isTime = false)
    {
        var card = new MaterialCardView(ctx) { Radius = 16, Elevation = 2 };
        var listLayout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        listLayout.SetPadding(0, 8, 0, 8);

        int rank = 1;
        foreach (var item in items)
        {
            // Reflection or dynamic mapping needed if 'item' isn't strongly typed in this scope
            // Assuming 'item' is DimmerStats
            var statsItem = item as DimmerStats;
            if (statsItem == null) continue;

            string title = isArtist ? statsItem.Name : statsItem.Song?.Title ?? "Unknown";
            string sub = isArtist ? $"{statsItem.Count} Plays" : statsItem.Song?.ArtistName;
            string rightText = isTime
                ? TimeSpan.FromSeconds((long)statsItem.TotalSecondsNumeric).ToString(@"hh\:mm")
                : statsItem.Count.ToString();

            listLayout.AddView(CreateListItem(ctx, rank++, title, sub, rightText));

            // Limit to 5 or 10
            if (rank > 10) break;
        }

        card.AddView(listLayout);
        return card;
    }

    private View CreateListItem(Context ctx, int rank, string title, string subtitle, string value)
    {
        var row = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        row.SetPadding(24, 16, 24, 16);
        row.SetGravity (GravityFlags.CenterVertical);

        // Rank
        var rankTv = new TextView(ctx) { Text = rank.ToString(), TextSize = 14, Typeface = Typeface.DefaultBold };
        rankTv.SetWidth(AppUtil.DpToPx(30)); // Fixed width
        rankTv.Alpha = 0.5f;
        row.AddView(rankTv);

        // Texts
        var textLy = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        var param = new LinearLayout.LayoutParams(0, -2, 1f);
        textLy.LayoutParameters = param;

        textLy.AddView(new TextView(ctx) { Text = title, TextSize = 16, Typeface = Typeface.DefaultBold, Ellipsize = Android.Text.TextUtils.TruncateAt.End });
        if (!string.IsNullOrEmpty(subtitle))
            textLy.AddView(new TextView(ctx) { Text = subtitle, TextSize = 12, Alpha = 0.7f, Ellipsize = Android.Text.TextUtils.TruncateAt.End });

        row.AddView(textLy);

        // Value
        var valTv = new TextView(ctx) { Text = value, TextSize = 14, Typeface = Typeface.DefaultBold };
        valTv.SetTextColor(Color.ParseColor("#6750A4")); // Accent color
        row.AddView(valTv);

        return row;
    }

    private TextView CreateSectionHeader(Context ctx, string text)
    {
        var tv = new TextView(ctx) { Text = text, TextSize = 20, Typeface = Typeface.DefaultBold };
        tv.SetPadding(8, 32, 8, 16);
        return tv;
    }
}