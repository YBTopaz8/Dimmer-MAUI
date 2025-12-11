using Google.Android.Material.Tabs;

using ScrollView = Android.Widget.ScrollView;

namespace Dimmer.ViewsAndPages.NativeViews.SingleSong;

public class ViewLastFmInfoFragment : Fragment
{
    private BaseViewModelAnd MyViewModel;
    private LinearLayout LastFmContent;
    private LinearLayout LocalStatsContent;

    public ViewLastFmInfoFragment(BaseViewModelAnd vm)
    {
        MyViewModel = vm;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context;
        var root = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        root.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);

        // 1. Tabs
        var tabLayout = new TabLayout(ctx);
        var tab1 = tabLayout.NewTab().SetText("LastFM Data");
        var tab2 = tabLayout.NewTab().SetText("Your Stats");
        tabLayout.AddTab(tab1);
        tabLayout.AddTab(tab2);
        root.AddView(tabLayout);

        // 2. Content Container
        var scroll = new ScrollView(ctx) { FillViewport = true };
        var contentFrame = new FrameLayout(ctx);
        contentFrame.SetPadding(30, 30, 30, 30);

        // Build Views
        LastFmContent = BuildLastFmView(ctx);
        LocalStatsContent = BuildLocalStatsView(ctx);

        contentFrame.AddView(LastFmContent);
        contentFrame.AddView(LocalStatsContent);
        LocalStatsContent.Visibility = ViewStates.Gone; // Hide initially

        scroll.AddView(contentFrame);
        root.AddView(scroll);

        // Tab Logic
        tabLayout.TabSelected += (s, e) =>
        {
            if (e.Tab.Position == 0)
            {
                LastFmContent.Visibility = ViewStates.Visible;
                LocalStatsContent.Visibility = ViewStates.Gone;
            }
            else
            {
                LastFmContent.Visibility = ViewStates.Gone;
                LocalStatsContent.Visibility = ViewStates.Visible;
            }
        };

        return root;
    }

    private LinearLayout BuildLastFmView(Context ctx)
    {
        var layout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };

        layout.AddView(new MaterialTextView(ctx) { Text = "Global LastFM Stats", TextSize = 22 });
        layout.AddView(CreateStatRow(ctx, "Listeners", "1,204,500"));
        layout.AddView(CreateStatRow(ctx, "Total Scrobbles", "14,000,000"));

        layout.AddView(new MaterialTextView(ctx) { Text = "Tags", TextSize = 18, Top = 20 });
        var tags = "Rock, Alternative, 90s, Grunge";
        layout.AddView(new TextView(ctx) { Text = tags});

        layout.AddView(new MaterialTextView(ctx) { Text = "Biography", TextSize = 18, Top = 20 });
        var bio = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.";
        layout.AddView(new TextView(ctx) { Text = bio});

        return layout;
    }

    private LinearLayout BuildLocalStatsView(Context ctx)
    {
        var layout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        var s = MyViewModel.SelectedSong;

        layout.AddView(new MaterialTextView(ctx) { Text = "Your Listening Habits", TextSize = 22 });

        layout.AddView(CreateStatRow(ctx, "Play Count", s?.PlayCount.ToString() ?? "0"));
        layout.AddView(CreateStatRow(ctx, "Skip Count", s?.SkipCount.ToString() ?? "0"));
        layout.AddView(CreateStatRow(ctx, "Is Favorite", s?.IsFavorite == true ? "Yes" : "No"));
        layout.AddView(CreateStatRow(ctx, "Date Added", s?.DateCreated?.ToString() ?? "Unknown"));

        return layout;
    }

    private View CreateStatRow(Context ctx, string label, string value)
    {
        var row = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        row.SetPadding(0, 10, 0, 10);
        var t1 = new TextView(ctx) { Text = label + ": ", Typeface = Typeface.DefaultBold };
        var t2 = new TextView(ctx) { Text = value };
        row.AddView(t1);
        row.AddView(t2);
        return row;
    }
}