
using ProgressBar = Android.Widget.ProgressBar;
using ScrollView = Android.Widget.ScrollView;

namespace Dimmer.ViewsAndPages.NativeViews.StatsSection;

public class StatsHabitsFragment : Fragment
{
    private readonly BaseViewModelAnd _vm;
    private readonly StatisticsViewModel _statisticsVM;
    public StatsHabitsFragment(BaseViewModelAnd vm, StatisticsViewModel statisticsVM) 

    { 
        _vm = vm; 
        _statisticsVM = statisticsVM;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context;
        var scroll = new ScrollView(ctx) { LayoutParameters = new ViewGroup.LayoutParams(-1, -1) };
        var root = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        root.SetPadding(24, 24, 24, 24);

        var summary = _statisticsVM.Stats?.CollectionSummary;
        if (summary == null) return root;

        // --- Temporal Habits Card ---
        var card = new MaterialCardView(ctx) { Radius = 16 };
        var ly = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        ly.SetPadding(24, 24, 24, 24);

        ly.AddView(new TextView(ctx) { Text = "Temporal Habits", TextSize = 18, Typeface = Typeface.DefaultBold });

        ly.AddView(CreateRow(ctx, "Played Today", summary.SongsPlayedToday.ToString()));
        ly.AddView(CreateRow(ctx, "Night Owl (10pm-5am)", summary.SongsPlayedAtNight.ToString(), Color.Purple));
        ly.AddView(CreateRow(ctx, "Avg Track Length", TimeSpan.FromSeconds(summary.AverageDuration).ToString(@"mm\:ss")));

        // Visual Bar for 'Played Today' (Mocking the ProgressBar)
        var progress = new ProgressBar(ctx, null, Android.Resource.Attribute.ProgressBarStyleHorizontal);
        progress.Max = 100;
        progress.Progress = summary.SongsPlayedToday; // Arbitrary scale
        progress.ProgressTintList = Android.Content.Res.ColorStateList.ValueOf(Color.ParseColor("#6750A4"));
        ((LinearLayout.LayoutParams)progress.LayoutParameters).TopMargin = 16;
        ly.AddView(progress);

        card.AddView(ly);
        root.AddView(card);

        // --- Day Breakdown ---
        var daysHeader = new TextView(ctx) { Text = "Daily Routine", TextSize = 18, Typeface = Typeface.DefaultBold };
        ((LinearLayout.LayoutParams)daysHeader.LayoutParameters).SetMargins(0, 32, 0, 16);
        root.AddView(daysHeader);

        var daysCard = new MaterialCardView(ctx) { Radius = 16 };
        var daysLy = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        daysLy.SetPadding(24, 12, 24, 12);

        if (_statisticsVM.Stats?.OverallListeningByDayOfWeek != null)
        {
            foreach (var dayStat in _statisticsVM.Stats.OverallListeningByDayOfWeek)
            {
                // dayStat.XValue is Day Name, YValue is Count
                daysLy.AddView(CreateRow(ctx, dayStat.XValue.ToString(), dayStat.YValue.ToString()));
            }
        }
        daysCard.AddView(daysLy);
        root.AddView(daysCard);

        scroll.AddView(root);
        return scroll;
    }

    private View CreateRow(Context ctx, string label, string val, Color? valColor = null)
    {
        var row = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        row.SetPadding(0, 12, 0, 12);

        var lbl = new TextView(ctx) { Text = label, TextSize = 14 };
        lbl.LayoutParameters = new LinearLayout.LayoutParams(0, -2, 1f);

        var v = new TextView(ctx) { Text = val, TextSize = 16, Typeface = Typeface.DefaultBold };
        if (valColor.HasValue) v.SetTextColor(valColor.Value);

        row.AddView(lbl);
        row.AddView(v);
        return row;
    }
}