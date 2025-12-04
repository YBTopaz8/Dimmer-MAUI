using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Google.Android.Material.Card;

using ScrollView = Android.Widget.ScrollView;

namespace Dimmer.ViewsAndPages.NativeViews.ArtistSection;


public class ArtistEventsStatsFragment : Fragment
{
    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context;
        var scroll = new ScrollView(ctx) { FillViewport = true };
        var root = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        root.SetPadding(20, 20, 20, 150); // Bottom padding for player

        // --- 1. DETAILED STATS GRID ---
        var statsTitle = new MaterialTextView(ctx) { Text = "Performance Metrics", TextSize = 22, Typeface = Typeface.DefaultBold };
        root.AddView(statsTitle);

        var grid = new GridLayout(ctx) { ColumnCount = 2, RowCount = 2 };
        grid.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);

        grid.AddView(CreateStatCard(ctx, "Monthly Listeners", "1.2M", Color.DarkSlateBlue));
        grid.AddView(CreateStatCard(ctx, "Followers", "450K", Color.DarkGray));
        grid.AddView(CreateStatCard(ctx, "World Rank", "#340", Color.DarkGray));
        grid.AddView(CreateStatCard(ctx, "Top City", "London", Color.DarkSlateBlue));

        root.AddView(grid);

        // --- 2. EVENTS LIST ---
        var eventsTitle = new MaterialTextView(ctx) { Text = "Upcoming Events & Releases", TextSize = 22, Typeface = Typeface.DefaultBold };
        ((LinearLayout.LayoutParams)eventsTitle.LayoutParameters).TopMargin = AppUtil.DpToPx(24);
        root.AddView(eventsTitle);

        var eventsRecycler = new RecyclerView(ctx);
        eventsRecycler.SetLayoutManager(new LinearLayoutManager(ctx));
        eventsRecycler.NestedScrollingEnabled = false;

        // Mock Events
        var events = new List<ArtistEvent>
        {
            new ArtistEvent { Date = "Oct 12", Title = "World Tour: London", Type = "Concert" },
            new ArtistEvent { Date = "Oct 15", Title = "World Tour: Paris", Type = "Concert" },
            new ArtistEvent { Date = "Nov 01", Title = "New Single Release", Type = "Release" },
            new ArtistEvent { Date = "Dec 25", Title = "Holiday Special", Type = "TV" }
        };

        eventsRecycler.SetAdapter(new EventsAdapter(events));
        root.AddView(eventsRecycler);

        scroll.AddView(root);
        return scroll;
    }

    private View CreateStatCard(Context ctx, string title, string value, Color color)
    {
        var card = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(12),
            CardElevation = AppUtil.DpToPx(4),
            LayoutParameters = new GridLayout.LayoutParams()
            {
                Width = AppUtil.DpToPx(160),
                Height = AppUtil.DpToPx(100),
                RightMargin = 10,
                BottomMargin = 10
            }
        };
        card.SetBackgroundColor(color);

        var ly = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
        };
        ly.SetGravity(GravityFlags.Center);

        var t1 = new TextView(ctx) { Text = value, TextSize = 24,  Typeface = Typeface.DefaultBold };
        var t2 = new TextView(ctx) { Text = title, TextSize = 12, };

        ly.AddView(t1);
        ly.AddView(t2);
        card.AddView(ly);
        return card;
    }

    class ArtistEvent { public string Date { get; set; } public string Title { get; set; } public string Type { get; set; } }

    class EventsAdapter : RecyclerView.Adapter
    {
        List<ArtistEvent> _items;
        public EventsAdapter(List<ArtistEvent> i) { _items = i; }
        public override int ItemCount => _items.Count;
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var ly = new LinearLayout(parent.Context) { Orientation = Orientation.Horizontal, WeightSum = 10 };
            ly.SetPadding(20, 30, 20, 30);

            var date = new TextView(parent.Context) { TextSize = 14, Typeface = Typeface.DefaultBold };
            date.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 2);

            var info = new LinearLayout(parent.Context) { Orientation = Orientation.Vertical };
            info.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 8);

            var title = new TextView(parent.Context) { TextSize = 16 };
            var type = new TextView(parent.Context) { TextSize = 12 };
            info.AddView(title); info.AddView(type);

            ly.AddView(date);
            ly.AddView(info);
            return new VH(ly, date, title, type);
        }
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = holder as VH;
            var item = _items[position];
            vh.Date.Text = item.Date;
            vh.Title.Text = item.Title;
            vh.Type.Text = item.Type.ToUpper();
        }
        class VH : RecyclerView.ViewHolder
        {
            public TextView Date, Title, Type;
            public VH(View v, TextView d, TextView t, TextView ty) : base(v) { Date = d; Title = t; Type = ty; }
        }
    }
}