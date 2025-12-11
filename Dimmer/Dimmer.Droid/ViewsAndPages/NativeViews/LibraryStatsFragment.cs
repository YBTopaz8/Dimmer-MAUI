using Google.Android.Material.ProgressIndicator;

using ScrollView = Android.Widget.ScrollView;

namespace Dimmer.ViewsAndPages.NativeViews;

public class LibraryStatsFragment : Fragment
{
    private BaseViewModelAnd MyViewModel;
    public LibraryStatsFragment(BaseViewModelAnd vm) { MyViewModel = vm; }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
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
        summaryLy.AddView(new TextView(ctx) { Text = "142", TextSize = 48, Typeface = Typeface.DefaultBold });
        summaryLy.AddView(new TextView(ctx) { Text = "Hours Listened"});
        summaryCard.AddView(summaryLy);
        root.AddView(summaryCard);

        // Genre Breakdown
        root.AddView(new MaterialTextView(ctx) { Text = "Top Genres", TextSize = 20 });

        root.AddView(CreateGenreProgress(ctx, "Rock", 80, Color.Red));
        root.AddView(CreateGenreProgress(ctx, "Jazz", 45, Color.Blue));
        root.AddView(CreateGenreProgress(ctx, "Electronic", 30, Color.Green));
        root.AddView(CreateGenreProgress(ctx, "Classical", 15, Color.Orange));

        scroll.AddView(root);
        return scroll;
    }

    private View CreateGenreProgress(Context ctx, string label, int progress, Color color)
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