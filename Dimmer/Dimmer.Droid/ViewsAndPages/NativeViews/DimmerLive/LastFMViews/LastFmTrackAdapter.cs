using Bumptech.Glide.Load.Resource.Bitmap;
using Hqub.Lastfm.Entities;

namespace Dimmer.ViewsAndPages.NativeViews.DimmerLive.LastFMViews;

public class LastFmTrackAdapter : RecyclerView.Adapter
{
    private readonly Context _ctx;
    private List<Track> _items;
    private readonly BaseViewModelAnd _vm;

    public LastFmTrackAdapter(Context ctx, List<Track> items, BaseViewModelAnd vm)
    { _ctx = ctx; _items = items; _vm = vm; }

    public override int ItemCount => _items.Count;

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        var card = new MaterialCardView(_ctx) { Radius = 30, CardElevation = 0 };
        card.SetCardBackgroundColor(Color.ParseColor("#05808080"));
        var lp = new RecyclerView.LayoutParams(-1, -2);
        lp.SetMargins(0, 10, 0, 10);
        card.LayoutParameters = lp;

        var lay = new LinearLayout(_ctx) { Orientation = Orientation.Horizontal };
        lay.SetPadding(20, 20, 20, 20);
        lay.SetGravity(GravityFlags.CenterVertical);

        var img = new ImageView(_ctx) { LayoutParameters = new LinearLayout.LayoutParams(AppUtil.DpToPx(50), AppUtil.DpToPx(50)) };

        var textLay = new LinearLayout(_ctx) { Orientation = Orientation.Vertical };
        textLay.LayoutParameters = new LinearLayout.LayoutParams(0, -2, 1f);
        textLay.SetPadding(30, 0, 20, 0);

        var title = new TextView(_ctx) { TextSize = 15, Typeface = Typeface.DefaultBold };
        var artist = new TextView(_ctx) { TextSize = 13, Alpha = 0.6f };
        textLay.AddView(title);
        textLay.AddView( artist);

        var status = new TextView(_ctx) { TextSize = 10, Gravity = GravityFlags.End };
        status.SetTextColor(Color.Gray);

        lay.AddView(img);
        lay.AddView(textLay);
        lay.AddView(status);
        card.AddView(lay);
        return new TrackVH(card, img, title, artist, status);
    }

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        var vh = (TrackVH)holder;
        var track = _items[position];

        vh.Title.Text = track.Name;
        vh.Artist.Text = track.Artist.Name;

        if (track.NowPlaying)
        {
            vh.Status.Text = "NOW PLAYING";
            vh.Status.SetTextColor(Color.ParseColor("#D51007"));
            vh.Status.Animate().Alpha(0.4f).SetDuration(1000).WithEndAction(new Java.Lang.Runnable(() => vh.Status.Animate().Alpha(1f).SetDuration(1000))).Start();
        }
        else
        {
            vh.Status.Text = track.Date?.ToLocalTime().ToString("t") ?? "";
            vh.Status.SetTextColor(Color.Gray);
        }

        Glide.With(_ctx)
            .Load(track.Images.LastOrDefault()?.Url)
            .Transform(new CenterCrop(), new RoundedCorners(20))
            .Into(vh.Img);

        // Wiki Support (On Long Click)
        vh.ItemView.LongClick += (s, e) => {
            if (track.Wiki != null) ShowWikiDialog(track.Wiki.Summary);
        };
    }

    private void ShowWikiDialog(string html)
    {
        var tv = new TextView(_ctx);
        tv.SetPadding(50, 50, 50, 50);
        tv.TextFormatted = Android.Text.Html.FromHtml(html);
        tv.MovementMethod = Android.Text.Method.LinkMovementMethod.Instance;

        new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(_ctx)
            .SetTitle("Track Info")
            .SetView(tv)
            .SetPositiveButton("Close", (s, e) => { })
            .Show();
    }

    class TrackVH : RecyclerView.ViewHolder
    {
        public ImageView Img; public TextView Title, Artist, Status;
        public TrackVH(View v, ImageView i, TextView t, TextView a, TextView s) : base(v)
        { Img = i; Title = t; Artist = a; Status = s; }
    }
}