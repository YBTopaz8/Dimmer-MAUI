using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Bumptech.Glide;
using Google.Android.Material.Card;
using Google.Android.Material.TextView;
using Microsoft.Maui.Graphics.Text;
using System.Collections.Generic;

namespace Dimmer.ViewsAndPages.NativeViews.Stats;

public static class StatsUIEngine
{
    // 1. Builds a Grid of Simple Metrics (e.g., "Total Plays: 50")
    public static View BuildMetricsGrid(Context ctx, Dictionary<string, string> metrics)
    {
        var grid = new GridLayout(ctx)
        {
            ColumnCount = 2,
            AlignmentMode = GridAlign.Bounds,
            LayoutParameters = new LinearLayout.LayoutParams(-1, -2) { TopMargin = 20, BottomMargin = 40 }
        };

        foreach (var metric in metrics)
        {
            var card = new MaterialCardView(ctx)
            {
                Radius = AppUtil.DpToPx(16),
                CardElevation = 0,
                StrokeWidth = 2,
            };
            card.SetStrokeColor(Android.Content.Res.ColorStateList.ValueOf(Color.ParseColor("#20808080")));

            var lp = new GridLayout.LayoutParams();
            lp.Width = 0;
            lp.Height = ViewGroup.LayoutParams.WrapContent;
            lp.ColumnSpec = GridLayout.InvokeSpec(GridLayout.Undefined, 1f);
            lp.SetMargins(12, 12, 12, 12);
            card.LayoutParameters = lp;

            var ly = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
            ly.SetPadding(30, 30, 30, 30);

            var valTv = new TextView(ctx) { Text = metric.Value, TextSize = 22, Typeface = Typeface.DefaultBold};
            valTv.SetTextColor(Color.DarkSlateBlue);
            var lblTv = new TextView(ctx) { Text = metric.Key, TextSize = 12, Alpha = 0.7f };

            ly.AddView(valTv);
            ly.AddView(lblTv);
            card.AddView(ly);
            grid.AddView(card);
        }
        return grid;
    }

    // 2. Builds a Horizontal Scrolling Carousel (Netflix Style)
    public static View BuildHorizontalCarousel(Context ctx, string title, List<DimmerStats> data)
    {
        var sectionLy = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        sectionLy.LayoutParameters = new LinearLayout.LayoutParams(-1, -2) { BottomMargin = AppUtil.DpToPx(30) };

        var titleTv = new MaterialTextView(ctx) { Text = title, TextSize = 20, Typeface = Typeface.DefaultBold };
        titleTv.SetPadding(40, 0, 0, 20);
        sectionLy.AddView(titleTv);

        var rv = new RecyclerView(ctx);
        rv.SetLayoutManager(new LinearLayoutManager(ctx, LinearLayoutManager.Horizontal, false));
        rv.HasFixedSize = true;

        // Zero-allocation adapter
        rv.SetAdapter(new DimmerStatsCarouselAdapter(ctx, data));

        // Snap to cards like Spotify
        new PagerSnapHelper().AttachToRecyclerView(rv);

        sectionLy.AddView(rv);
        return sectionLy;
    }
}

// 3. Ultra-Fast Zero-Allocation Carousel Adapter
public class DimmerStatsCarouselAdapter : RecyclerView.Adapter
{
    private readonly Context _ctx;
    private readonly List<DimmerStats> _items;

    public DimmerStatsCarouselAdapter(Context ctx, List<DimmerStats> items) { _ctx = ctx; _items = items; }
    public override int ItemCount => _items.Count;

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        var card = new MaterialCardView(_ctx)
        {
            Radius = AppUtil.DpToPx(16),
            CardElevation = 0,
            StrokeWidth = 2,
            LayoutParameters = new ViewGroup.LayoutParams(AppUtil.DpToPx(140), AppUtil.DpToPx(180))
        };
        card.SetStrokeColor(Android.Content.Res.ColorStateList.ValueOf(Color.ParseColor("#20808080")));
        //((ViewGroup.MarginLayoutParams)card.LayoutParameters).SetMargins(AppUtil.DpToPx(16), 0, AppUtil.DpToPx(8), 0);

        var ly = new LinearLayout(_ctx) { Orientation = Orientation.Vertical
        };

        //        ly.SetGravity(GravityFlags.Center);
        ly.SetPadding(20, 20, 20, 20);

        var imgCard = new MaterialCardView(_ctx) { Radius = AppUtil.DpToPx(40), CardElevation = 0 };
        var img = new ImageView(_ctx) { LayoutParameters = new LinearLayout.LayoutParams(AppUtil.DpToPx(80), AppUtil.DpToPx(80)) };
        img.SetScaleType(ImageView.ScaleType.CenterCrop);
        imgCard.AddView(img);

        var txtLabel = new TextView(_ctx) { TextSize = 14, Typeface = Typeface.DefaultBold, Gravity = GravityFlags.Center };
        txtLabel.SetMaxLines(2);
        txtLabel.Ellipsize = Android.Text.TextUtils.TruncateAt.End;
        txtLabel.SetPadding(0, 10, 0, 5);
        var txtValue = new TextView(_ctx) { TextSize = 12, Alpha = 0.7f };

        ly.AddView(imgCard); ly.AddView(txtLabel); ly.AddView(txtValue);
        card.AddView(ly);

        return new DimmerStatsViewHolder(card, img, txtLabel, txtValue);
    }
    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        if (holder is DimmerStatsViewHolder vh)
        {
            var item = _items[position];
            vh.Label.Text = item.Name;
            vh.Value.Text = item.Value.ToString();

            // Load Image with Glide
            if (!string.IsNullOrEmpty(item.Song?.CoverImagePath))
                Glide.With(_ctx).Load(item.Song.CoverImagePath).Placeholder(Resource.Drawable.musicnotess).Into(vh.Image);
            else
                vh.Image.SetImageResource(Resource.Drawable.musicnotess);
        }
    }

}

class DimmerStatsViewHolder : RecyclerView.ViewHolder
{
    public ImageView Image { get; }
    public TextView Label { get; }
    public TextView Value { get; }
    public DimmerStatsViewHolder(View itemView, ImageView image, TextView label, TextView value) : base(itemView)
    { Image = image; Label = label; Value = value; }
}
public static class StatsDataMapper
{
    public static List<DimmerStats> ToDimmerStats(this IEnumerable<LabelValue> source)
    {
        return source?.Select(x => new DimmerStats
        {
            Name = x.Label,
            ValueStr = x.Value.ToString(),
        }).ToList() ?? new List<DimmerStats>();
    }
}