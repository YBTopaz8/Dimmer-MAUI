using Android.Views;
using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Card;
using Google.Android.Material.TextView;

namespace Dimmer.ViewsAndPages.NativeViews.SingleSong;

public class SongPlayHistoryFragment : Fragment
{
    private BaseViewModelAnd _vm;
    public SongPlayHistoryFragment(BaseViewModelAnd vm) { _vm = vm; }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context!;

        // Use a FrameLayout to hold either the Recycler or the Empty State
        var root = new FrameLayout(ctx) { LayoutParameters = new ViewGroup.LayoutParams(-1, -1) };

        var recycler = new RecyclerView(ctx);
        recycler.SetLayoutManager(new LinearLayoutManager(ctx));
        recycler.SetPadding(40, 40, 40, 200); // Bottom padding for scrolling
        recycler.SetClipToPadding(false);

        // Fetch events
        var song = _vm.SelectedSong;
        var songIndDb = _vm.RealmFactory.GetRealmInstance()!.Find<SongModel>(song.Id)
                     ?? _vm.RealmFactory.GetRealmInstance()!.All<SongModel>().FirstOrDefaultNullSafe(x => x.TitleDurationKey == song.TitleDurationKey);

        List<DimmerPlayEventView> evts = new();
        if (songIndDb != null)
        {
            evts = songIndDb.PlayHistory.AsEnumerable()
                .Select(x => x.ToDimmerPlayEventView())
                .OrderByDescending(x => x.DatePlayed) // Newest first!
                .ToList();
        }

        if (evts.Count > 0)
        {
            // FIX: Actually attach the adapter!
            recycler.SetAdapter(new PlayHistoryAdapter(evts));
            root.AddView(recycler);
        }
        else
        {
            // POLISH: Beautiful Empty State
            var emptyLayout = new LinearLayout(ctx)
            {
                Orientation = Orientation.Vertical,
                LayoutParameters = new FrameLayout.LayoutParams(-2, -2) { Gravity = GravityFlags.Center }
            };
            emptyLayout.SetGravity(GravityFlags.Center);

            var emptyIcon = new ImageView(ctx);
            emptyIcon.SetImageResource(Resource.Drawable.time); // Make sure you have this icon
            emptyIcon.ImageTintList = Android.Content.Res.ColorStateList.ValueOf(Color.Gray);
            emptyIcon.LayoutParameters = new LinearLayout.LayoutParams(AppUtil.DpToPx(64), AppUtil.DpToPx(64)) { BottomMargin = AppUtil.DpToPx(16) };

            var emptyText = new MaterialTextView(ctx)
            {
                Text = "No play history yet.",
                TextSize = 18,
                Typeface = Typeface.DefaultBold
            };
            emptyText.SetTextColor(Color.Gray);

            var emptySubText = new MaterialTextView(ctx)
            {
                Text = "Listen to this track to build history.",
                TextSize = 14,
                Alpha = 0.7f
            };

            emptyLayout.AddView(emptyIcon);
            emptyLayout.AddView(emptyText);
            emptyLayout.AddView(emptySubText);
            root.AddView(emptyLayout);
        }

        return root;
    }

    class PlayHistoryAdapter : RecyclerView.Adapter
    {
        private readonly List<DimmerPlayEventView> _items;
        public PlayHistoryAdapter(List<DimmerPlayEventView> items) { _items = items; }
        public override int ItemCount => _items.Count;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var ctx = parent.Context!;
            var card = new MaterialCardView(ctx)
            {
                Radius = AppUtil.DpToPx(12),
                CardElevation = 0,
                StrokeWidth = AppUtil.DpToPx(1),
                StrokeColor = Color.ParseColor("#20808080") // Subtle border
            };
            card.SetCardBackgroundColor(Color.Transparent);
            var lp = new ViewGroup.MarginLayoutParams(-1, -2) { BottomMargin = AppUtil.DpToPx(12) };
            card.LayoutParameters = lp;

            var ly = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
            ly.SetPadding(AppUtil.DpToPx(16), AppUtil.DpToPx(16), AppUtil.DpToPx(16), AppUtil.DpToPx(16));
            ly.SetGravity(GravityFlags.CenterVertical);

            var vLine = new View(ctx) { LayoutParameters = new LinearLayout.LayoutParams(AppUtil.DpToPx(4), -1) { RightMargin = AppUtil.DpToPx(16) } };

            var textStack = new LinearLayout(ctx) { Orientation = Orientation.Vertical, LayoutParameters = new LinearLayout.LayoutParams(0, -2, 1) };

            var type = new MaterialTextView(ctx) { Typeface = Typeface.DefaultBold, TextSize = 16 };
            var date = new MaterialTextView(ctx) { TextSize = 14, Alpha = 0.8f };

            textStack.AddView(type);
            textStack.AddView(date);

            ly.AddView(vLine);
            ly.AddView(textStack);
            card.AddView(ly);

            return new VH(card, type, date, vLine);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = holder as VH;
            var item = _items[position];

            vh.Type.Text = ((PlayType)item.PlayType).ToString();

            // Format like "Today at 2:30 PM" or "Oct 24, 2025"
            vh.Date.Text = item.DatePlayed.Date == DateTime.Today
                ? $"Today at {item.DatePlayed:t}"
                : item.DatePlayed.ToString("MMM dd, yyyy • h:mm tt");

            PlayType playType = (PlayType)item.PlayType;
            Color statusColor = Color.Gray;

            switch (playType)
            {
                case PlayType.Play: statusColor = Color.ParseColor("#4A90E2"); break; // Blue
                case PlayType.Pause: statusColor = Color.ParseColor("#F5A623"); break; // Orange
                case PlayType.Completed: statusColor = Color.ParseColor("#7ED321"); break; // Green
                case PlayType.Seeked: statusColor = Color.ParseColor("#9013FE"); break; // Purple
                case PlayType.Skipped: statusColor = Color.ParseColor("#D0021B"); break; // Red
                case PlayType.Favorited: statusColor = Color.ParseColor("#FF4081"); break; // Pink
            }

            vh.Type.SetTextColor(statusColor);
            vh.VLine.SetBackgroundColor(statusColor); // Color code the side-line
        }

        class VH : RecyclerView.ViewHolder
        {
            public TextView Type, Date;
            public View VLine;
            public VH(View v, TextView t, TextView d, View line) : base(v)
            { Type = t; Date = d; VLine = line; }
        }
    }
}