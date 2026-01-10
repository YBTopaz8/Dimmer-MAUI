using AndroidX.RecyclerView.Widget;

namespace Dimmer.ViewsAndPages.NativeViews.SectionHeader;

/// <summary>
/// Adapter for section headers in the songs list.
/// Works with ConcatAdapter to display headers alongside songs.
/// </summary>
internal class SectionHeaderAdapter : RecyclerView.Adapter
{
    private readonly Context _context;
    private List<SectionHeaderModel> _sections = new();
    private Action<SectionHeaderModel>? _onHeaderClick;

    public SectionHeaderAdapter(Context context)
    {
        _context = context;
    }

    public void SetSections(List<SectionHeaderModel> sections)
    {
        _sections = sections;
        NotifyDataSetChanged();
    }

    public void SetOnHeaderClickListener(Action<SectionHeaderModel> listener)
    {
        _onHeaderClick = listener;
    }

    public override int ItemCount => _sections.Count;

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        if (holder is SectionHeaderViewHolder headerHolder && position < _sections.Count)
        {
            headerHolder.Bind(_sections[position], _onHeaderClick);
        }
    }

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        return SectionHeaderViewHolder.Create(_context, parent);
    }
}
