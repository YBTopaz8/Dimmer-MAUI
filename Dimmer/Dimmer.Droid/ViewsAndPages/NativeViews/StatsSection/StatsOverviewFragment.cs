using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Maui.Graphics.Text;

using ScrollView = Android.Widget.ScrollView;

namespace Dimmer.ViewsAndPages.NativeViews.StatsSection;

public class StatsOverviewFragment : Fragment
{
    private readonly BaseViewModelAnd _vm;
    StatisticsViewModel _statsViewModel;
    public StatsOverviewFragment(BaseViewModelAnd vm,
        StatisticsViewModel viewModel) { 
        _vm = vm; 
        _statsViewModel = viewModel;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context;
        var scroll = new ScrollView(ctx) { LayoutParameters = new ViewGroup.LayoutParams(-1, -1) };
        var root = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        root.SetPadding(24, 24, 24, 24);

        var stats = _statsViewModel.Stats?.CollectionSummary;
        if (stats == null) return new TextView(ctx) { Text = "No Data" };

        // --- SECTION 1: BIG KPIs (2x2 Grid) ---
        // Row 1
        var row1 = new LinearLayout(ctx) { Orientation = Orientation.Horizontal, WeightSum = 2 };
        row1.AddView(CreateKpiCard(ctx, "Total Tracks", stats.TotalSongs.ToString("N0"), $"Played Today: {stats.SongsPlayedToday}", Color.ParseColor("#4CAF50")));
        row1.AddView(CreateKpiCard(ctx, "Total Plays", stats.TotalPlayCount.ToString("N0"), $"Skips: {stats.TotalSkipCount}", Color.ParseColor("#E57373"))); // Red 300
        root.AddView(row1);

        // Row 2
        var row2 = new LinearLayout(ctx) { Orientation = Orientation.Horizontal, WeightSum = 2 };
        ((LinearLayout.LayoutParams)row2.LayoutParameters).TopMargin = 16;
        var totalTimeStr = TimeSpan.FromSeconds(stats.TotalListeningTime).ToString(@"dd\.hh\:mm");
        row2.AddView(CreateKpiCard(ctx, "Total Time", totalTimeStr, $"Avg: {TimeSpan.FromSeconds(stats.AverageDuration):mm\\:ss}", Color.Gray));
        row2.AddView(CreateKpiCard(ctx, "Quality", $"{stats.AverageRating:F1}", $"Favorites: {stats.SongsFavorited}", Color.Gold));
        root.AddView(row2);


        // --- SECTION 2: METADATA ---
        root.AddView(CreateHeader(ctx, "Library Composition"));
        var metaCard = new MaterialCardView(ctx) { Radius = 24, Elevation = 4 };
        var metaGrid = new GridLayout(ctx) { ColumnCount = 2, RowCount = 2 };
        metaGrid.SetPadding(24, 24, 24, 24);

        metaGrid.AddView(CreateMetaItem(ctx, "Artists", stats.DistinctArtists.ToString("N0")));
        metaGrid.AddView(CreateMetaItem(ctx, "Albums", stats.DistinctAlbums.ToString("N0")));
        metaGrid.AddView(CreateMetaItem(ctx, "Lyrics", stats.SongsWithLyrics.ToString("N0")));
        metaGrid.AddView(CreateMetaItem(ctx, "Synced", stats.SongsWithSyncedLyrics.ToString("N0")));

        metaCard.AddView(metaGrid);
        root.AddView(metaCard);


        // --- SECTION 3: EXTREMES (Most Played/Skipped) ---
        root.AddView(CreateHeader(ctx, "Extremes"));
        var extremeCard = new MaterialCardView(ctx) { Radius = 24, Elevation = 4 };
        var extremeLy = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        extremeLy.SetPadding(24, 24, 24, 24);

        // Most Played
        extremeLy.AddView(CreateRankRow(ctx, "Most Played", stats.MostPlayedSongTitle, stats.MostPlayedSongCount.ToString() + " Plays", Color.Teal));

        // Divider
        var div = new View(ctx) { LayoutParameters = new LinearLayout.LayoutParams(-1, 2) };
        div.SetBackgroundColor(Color.LightGray);
        ((LinearLayout.LayoutParams)div.LayoutParameters).SetMargins(0, 16, 0, 16);
        extremeLy.AddView(div);

        // Most Skipped
        extremeLy.AddView(CreateRankRow(ctx, "Most Skipped", stats.MostSkippedSongTitle, stats.MostSkippedSongCount.ToString() + " Skips", Color.Red));

        extremeCard.AddView(extremeLy);
        root.AddView(extremeCard);

        scroll.AddView(root);
        return scroll;
    }

    // --- Helpers ---

    private View CreateKpiCard(Context ctx, string title, string bigVal, string subVal, Color subColor)
    {
        var card = new MaterialCardView(ctx) { Radius = 24, Elevation = 0 }; // Flat style often looks better in grids
        card.SetCardBackgroundColor(Color.ParseColor("#1E1E1E")); // Dark surface
        var lp = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f);
        lp.SetMargins(8, 8, 8, 8);
        card.LayoutParameters = lp;

        var ly = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        ly.SetPadding(24, 24, 24, 24);

        ly.AddView(new TextView(ctx) { Text = title, TextSize = 12, Typeface = Typeface.DefaultBold, Alpha = 0.7f });
        ly.AddView(new TextView(ctx) { Text = bigVal, TextSize = 22, Typeface = Typeface.DefaultBold }); // Big Number
        
        var labelView = new TextView(ctx) { Text = subVal, TextSize = 11 };
        labelView.SetTextColor(subColor);
        ly.AddView(labelView);
        card.AddView(ly);
        return card;
    }

    private View CreateMetaItem(Context ctx, string label, string val)
    {
        var ly = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        var paramsG = new GridLayout.LayoutParams();
        paramsG.SetGravity(GravityFlags.FillHorizontal);
        paramsG.Width = 0;
        paramsG.ColumnSpec = GridLayout.InvokeSpec(GridLayout.Undefined, 1f);
        ly.LayoutParameters = paramsG;
        ly.SetPadding(0, 0, 0, 24);

        ly.AddView(new TextView(ctx) { Text = label, TextSize = 11, Alpha = 0.6f });
        ly.AddView(new TextView(ctx) { Text = val, TextSize = 16, Typeface = Typeface.DefaultBold });
        return ly;
    }

    private View CreateRankRow(Context ctx, string label, string title, string count, Color color)
    {
        var ly = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        var labelView = new TextView(ctx) { Text = label, TextSize = 10, Typeface = Typeface.DefaultBold };

        labelView.SetTextColor(color);

            ly.AddView(new TextView(ctx) { Text = label, TextSize = 10, Typeface = Typeface.DefaultBold });

        var titleView = new TextView(ctx) { Text = title, TextSize = 16, Typeface = Typeface.DefaultBold, Ellipsize = Android.Text.TextUtils.TruncateAt.End };
        titleView.SetMaxLines(1);
        ly.AddView(titleView);
        ly.AddView(new TextView(ctx) { Text = count, TextSize = 12, Alpha = 0.7f });
        return ly;
    }

    private TextView CreateHeader(Context ctx, string text)
    {
        var tv = new TextView(ctx) { Text = text, TextSize = 18, Typeface = Typeface.DefaultBold };
        var lp = new LinearLayout.LayoutParams(-1, -2);
        lp.SetMargins(8, 32, 8, 16);
        tv.LayoutParameters = lp;
        return tv;
    }
}