using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dimmer.DimmerSearch.TQLDoc;

using Google.Android.Material.Card;
using Google.Android.Material.Chip;

namespace Dimmer.TQL;


public class TqlGuideAdapter : RecyclerView.Adapter
{
    private readonly List<TqlHelpItem> _items;
    private readonly Action<string> _onExampleClicked;

    public TqlGuideAdapter(List<TqlHelpItem> items, Action<string> onExampleClicked)
    {
        _items = items;
        _onExampleClicked = onExampleClicked;
    }

    public override int ItemCount => _items.Count;

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        var ctx = parent.Context!;

        // 1. Card Container
        var card = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(16),
            CardElevation = AppUtil.DpToPx(2),
            LayoutParameters = new ViewGroup.MarginLayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        ((ViewGroup.MarginLayoutParams)card.LayoutParameters!).SetMargins(0, 0, 0, AppUtil.DpToPx(12));
        card.SetContentPadding(AppUtil.DpToPx(16), AppUtil.DpToPx(16), AppUtil.DpToPx(16), AppUtil.DpToPx(16));

        // 2. Linear Layout inside Card
        var layout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };

        // 3. Title Row (Title + Category Chip)
        var titleRow = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
        };
        titleRow.SetGravity(GravityFlags.CenterVertical);

        var title = new TextView(ctx)
        {
            TextSize = 18,
            Typeface = Android.Graphics.Typeface.DefaultBold
        };
        title.SetTextColor(Android.Graphics.Color.Black); // Or your theme color

        var spacer = new View(ctx) { LayoutParameters = new LinearLayout.LayoutParams(0, 0) { Weight = 1 } };

        var categoryChip = new Chip(ctx)
        {
            Clickable = false,
            Focusable = false,
        };
        categoryChip.EnsureAccessibleTouchTarget(AppUtil.DpToPx(32)); // Helper for height

        titleRow.AddView(title);
        titleRow.AddView(spacer);
        titleRow.AddView(categoryChip);

        // 4. Description
        var desc = new TextView(ctx)
        {
            TextSize = 14
        };
        desc.SetPadding(0, AppUtil.DpToPx(8), 0, AppUtil.DpToPx(12));

        // 5. Code Block (The clickable part)
        var codeFrame = new FrameLayout(ctx)
        {
            Background = new Android.Graphics.Drawables.GradientDrawable()
        };
        // Simple gray background for code
        ((Android.Graphics.Drawables.GradientDrawable)codeFrame.Background).SetColor(Android.Graphics.Color.ParseColor("#F0F0F0"));
        ((Android.Graphics.Drawables.GradientDrawable)codeFrame.Background).SetCornerRadius(AppUtil.DpToPx(8));
        codeFrame.SetPadding(AppUtil.DpToPx(12), AppUtil.DpToPx(8), AppUtil.DpToPx(12), AppUtil.DpToPx(8));

        var codeTxt = new TextView(ctx)
        {
            TextSize = 14,
            Typeface = Android.Graphics.Typeface.Monospace
        };
        codeTxt.SetTextColor(Android.Graphics.Color.ParseColor("#333333"));
        codeFrame.AddView(codeTxt);

        layout.AddView(titleRow);
        layout.AddView(desc);
        layout.AddView(codeFrame);
        card.AddView(layout);

        return new HelpViewHolder(card, title, desc, codeTxt, categoryChip, codeFrame);
    }

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        if (holder is HelpViewHolder vh)
        {
            var item = _items[position];
            vh.Title.Text = item.Title;
            vh.Description.Text = item.Description;
            vh.Code.Text = item.ExampleQuery;
            vh.Category.Text = item.Category.ToString();

            // Interaction
            vh.CodeContainer.Click += (s, e) => _onExampleClicked(item.ExampleQuery);
            vh.ItemView.Click += (s, e) => _onExampleClicked(item.ExampleQuery);
        }
    }

    private class HelpViewHolder : RecyclerView.ViewHolder
    {
        public TextView Title { get; }
        public TextView Description { get; }
        public TextView Code { get; }
        public Chip Category { get; }
        public ViewGroup CodeContainer { get; }

        public HelpViewHolder(View view, TextView title, TextView desc, TextView code, Chip cat, ViewGroup codeCont) : base(view)
        {
            Title = title;
            Description = desc;
            Code = code;
            Category = cat;
            CodeContainer = codeCont;
        }
    }
}