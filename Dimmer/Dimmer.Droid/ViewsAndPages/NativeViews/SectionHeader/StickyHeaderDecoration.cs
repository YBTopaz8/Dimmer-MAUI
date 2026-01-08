using Android.Graphics;
using Android.Views;
using AndroidX.RecyclerView.Widget;

namespace Dimmer.ViewsAndPages.NativeViews.SectionHeader;

/// <summary>
/// ItemDecoration that makes section headers stick to the top while scrolling.
/// Uses a custom drawing approach to pin the current section header.
/// </summary>
internal class StickyHeaderDecoration : RecyclerView.ItemDecoration
{
    private readonly List<SectionHeaderModel> _sections;
    private readonly SectionHeaderViewHolder _stickyHeaderHolder;
    private readonly Context _context;

    public StickyHeaderDecoration(Context context, List<SectionHeaderModel> sections)
    {
        _context = context;
        _sections = sections;
        
        // Create a reusable ViewHolder for the sticky header
        _stickyHeaderHolder = SectionHeaderViewHolder.Create(context, null!);
    }

    public void UpdateSections(List<SectionHeaderModel> sections)
    {
        _sections.Clear();
        _sections.AddRange(sections);
    }

    public override void OnDrawOver(Canvas c, RecyclerView parent, RecyclerView.State state)
    {
        base.OnDrawOver(c, parent, state);

        if (_sections.Count == 0 || parent.ChildCount == 0)
        {
            return;
        }

        // Get the first visible item
        var topChild = parent.GetChildAt(0);
        if (topChild == null) return;

        var topChildPosition = parent.GetChildAdapterPosition(topChild);
        if (topChildPosition == RecyclerView.NoPosition) return;

        // Find which section the top visible item belongs to
        var currentSection = GetSectionForPosition(topChildPosition);
        if (currentSection == null) return;

        // Bind the sticky header
        _stickyHeaderHolder.Bind(currentSection, null);

        // Measure and layout the sticky header
        var headerView = _stickyHeaderHolder.ItemView;
        MeasureAndLayoutHeader(headerView, parent);

        // Calculate translation Y for the sticky header
        int translationY = 0;

        // Check if we need to push the sticky header up when approaching the next section
        for (int i = 0; i < parent.ChildCount; i++)
        {
            var child = parent.GetChildAt(i);
            if (child == null) continue;

            var childPosition = parent.GetChildAdapterPosition(child);
            if (childPosition == RecyclerView.NoPosition) continue;

            var childSection = GetSectionForPosition(childPosition);
            if (childSection != null && childSection != currentSection)
            {
                // This is the next section header
                var childTop = child.Top;
                var headerHeight = headerView.Height;

                if (childTop < headerHeight)
                {
                    // Push the sticky header up as the next header approaches
                    translationY = childTop - headerHeight;
                }
                break;
            }
        }

        // Draw the sticky header
        c.Save();
        c.Translate(0, translationY);
        headerView.Draw(c);
        c.Restore();
    }

    private void MeasureAndLayoutHeader(View header, RecyclerView parent)
    {
        var widthSpec = View.MeasureSpec.MakeMeasureSpec(parent.Width, MeasureSpecMode.Exactly);
        var heightSpec = View.MeasureSpec.MakeMeasureSpec(parent.Height, MeasureSpecMode.AtMost);

        header.Measure(widthSpec, heightSpec);
        header.Layout(0, 0, header.MeasuredWidth, header.MeasuredHeight);
    }

    private SectionHeaderModel? GetSectionForPosition(int position)
    {
        // Find the section that contains this position
        // Sections are ordered by AdapterPosition
        SectionHeaderModel? currentSection = null;

        foreach (var section in _sections)
        {
            if (position >= section.SongStartIndex && 
                position < section.SongStartIndex + section.SongCount)
            {
                currentSection = section;
                break;
            }
        }

        return currentSection ?? _sections.LastOrDefault();
    }
}
