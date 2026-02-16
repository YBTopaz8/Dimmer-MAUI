using Google.Android.Material.MaterialSwitch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.ViewsAndPages.NativeViews.Misc;

public class SortBottomSheetFragment : BottomSheetDialogFragment
{
    private readonly Action<string> _onSortSelected;
    private MaterialSwitch _descendingSwitch;

    // Map Display Name to Property Name
    private readonly Dictionary<string, string> _sortOptions = new()
        {
            { "Title", "Title" },
            { "Artist", "OtherArtistsName" },
            { "Album", "AlbumName" },
            { "Duration", "DurationInSeconds" },
            { "Date Added", "DateCreated" },
            { "Release Year", "ReleaseYear" },
            { "Play Count", "PlayCount" },
            { "Rating", "Rating" },
            { "BPM", "BPM" },
            { "File Size", "FileSize" }
        };

    public SortBottomSheetFragment(Action<string> onSortSelected)
    {
        _onSortSelected = onSortSelected;
    }

    public override View OnCreateView(LayoutInflater? inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context;
        var rootLayout = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        rootLayout.SetPadding(0, 20, 0, 40);

        // 1. Drag Handle (Visual Indicator)
        var handle = new View(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(AppUtil.DpToPx(32), AppUtil.DpToPx(4)) { Gravity = GravityFlags.CenterHorizontal }
        };
        handle.SetBackgroundResource(Resource.Drawable.m3_bottom_sheet_drag_handle); // Make sure you have a drawable or set a gray color
        handle.SetBackgroundColor(Android.Graphics.Color.LightGray);
        ((LinearLayout.LayoutParams)handle.LayoutParameters).SetMargins(0, 0, 0, 30);
        rootLayout.AddView(handle);

        // 2. Header & Toggle Row
        var headerRow = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        headerRow.SetGravity(GravityFlags.CenterVertical);
        headerRow.SetPadding(40, 0, 40, 20);

        var title = new MaterialTextView(ctx, null, Resource.Attribute.textAppearanceHeadline6) { Text = "Sort By" };
        title.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f);

        title.SetTextColor(UiBuilder.IsDark(container) ? Color.White : Color.Black);
        _descendingSwitch = new MaterialSwitch(ctx);
        _descendingSwitch.Text = "Descending";
        _descendingSwitch.Checked = true; // Default to Descending (usually preferred for dates/ratings)
        _descendingSwitch.CheckedChange += (s, e) =>
        {
            switch (_descendingSwitch.Checked)
            {
                case true:
                    _descendingSwitch.Text = "Descending";

                    break;
                default:
                    _descendingSwitch.Text = "Ascending";
                    break;
            }
        }
        ;
        headerRow.AddView(title);
        headerRow.AddView(_descendingSwitch);
        rootLayout.AddView(headerRow);

        // 3. RecyclerView
        var recyclerView = new RecyclerView(ctx);
        recyclerView.SetLayoutManager(new LinearLayoutManager(ctx));
        recyclerView.SetAdapter(new SortOptionAdapter(_sortOptions.Keys.ToList(), OnItemClick));

        rootLayout.AddView(recyclerView);

        return rootLayout;
    }

    private void OnItemClick(string displayKey)
    {
        if (_sortOptions.TryGetValue(displayKey, out string propertyName))
        {
            // Construct TQL: "desc title" or "asc title"
            string direction = _descendingSwitch.Checked ? "desc" : "asc";

            // Map specific properties to short-codes if needed, or use the raw property name
            // Based on your TQL examples, simple property names work with the direction prefix.
            // e.g. "desc Year" or "asc Title"
            string tqlCommand = $"{direction} {propertyName}";

            _onSortSelected?.Invoke(tqlCommand);
            Dismiss();
        }
    }

    // --- Internal Adapter ---
    private class SortOptionAdapter : RecyclerView.Adapter
    {
        private readonly List<string> _items;
        private readonly Action<string> _clickListener;

        public SortOptionAdapter(List<string> items, Action<string> clickListener)
        {
            _items = items;
            _clickListener = clickListener;
        }

        public override int ItemCount => _items.Count;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var tv = new MaterialTextView(parent.Context, null, Resource.Attribute.textAppearanceBody1);
            tv.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            tv.SetPadding(60, 30, 60, 30);
            tv.Clickable = true;
            tv.Focusable = true;
            tv.SetTextColor(UiBuilder.IsDark(parent) ? Color.White : Color.Black);
            // Add a ripple effect
            var attrs = new[] { Android.Resource.Attribute.SelectableItemBackground };
            var typedArray = parent.Context.ObtainStyledAttributes(attrs);
            tv.Background = typedArray.GetDrawable(0);
            typedArray.Recycle();

            return new SimpleViewHolder(tv);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var tv = (TextView)holder.ItemView;
            tv.Text = _items[position];
            tv.Click += (s, e) => _clickListener(_items[position]);
        }

        class SimpleViewHolder : RecyclerView.ViewHolder { public SimpleViewHolder(View v) : base(v) { } }
    }
}