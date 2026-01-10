using Android.Graphics;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Dimmer.WinUI.UiUtils;
using Google.Android.Material.Card;

namespace Dimmer.ViewsAndPages.NativeViews.SectionHeader;

/// <summary>
/// ViewHolder for section headers in the songs list
/// </summary>
internal class SectionHeaderViewHolder : RecyclerView.ViewHolder
{
    private readonly TextView _titleView;
    private readonly TextView _countView;
    private readonly MaterialCardView _container;
    private SectionHeaderModel? _currentHeader;

    public SectionHeaderViewHolder(View itemView, TextView titleView, TextView countView, MaterialCardView container) 
        : base(itemView)
    {
        _titleView = titleView;
        _countView = countView;
        _container = container;
    }

    public void Bind(SectionHeaderModel header, Action<SectionHeaderModel>? onHeaderClick)
    {
        _currentHeader = header;
        _titleView.Text = header.Title;
        _countView.Text = header.SongCount > 0 ? $"{header.SongCount} songs" : string.Empty;

        // Make header clickable if handler provided
        if (onHeaderClick != null)
        {
            _container.Clickable = true;
            _container.Click += (s, e) => onHeaderClick?.Invoke(header);
        }
    }

    public static SectionHeaderViewHolder Create(Context context, ViewGroup parent)
    {
        var card = new MaterialCardView(context)
        {
            Radius = 0,
            CardElevation = AppUtil.DpToPx(2),
            CardBackgroundColor = Android.Content.Res.ColorStateList.ValueOf(
                UiBuilder.IsDark(context) ? Color.ParseColor("#1A1A2E") : Color.ParseColor("#E8EAF6"))
        };

        var lp = new RecyclerView.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
        card.LayoutParameters = lp;

        var mainLayout = new LinearLayout(context)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        mainLayout.SetGravity(GravityFlags.CenterVertical);
        mainLayout.SetPadding(AppUtil.DpToPx(16), AppUtil.DpToPx(12), AppUtil.DpToPx(16), AppUtil.DpToPx(12));

        // Title
        var titleView = new TextView(context)
        {
            TextSize = 16,
            Typeface = Typeface.DefaultBold
        };
        titleView.SetTextColor(UiBuilder.IsDark(context) ? Color.White : Color.ParseColor("#1A237E"));

        var titleLp = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f);
        titleView.LayoutParameters = titleLp;

        // Count
        var countView = new TextView(context)
        {
            TextSize = 12
        };
        countView.SetTextColor(Color.Gray);

        mainLayout.AddView(titleView);
        mainLayout.AddView(countView);

        card.AddView(mainLayout);

        return new SectionHeaderViewHolder(card, titleView, countView, card);
    }
}
