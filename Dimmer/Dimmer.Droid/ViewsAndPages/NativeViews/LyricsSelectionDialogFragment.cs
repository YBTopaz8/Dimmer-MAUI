using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Button;
using Google.Android.Material.Dialog;

namespace Dimmer.ViewsAndPages.NativeViews;

/// <summary>
/// Dialog for selecting lyrics lines for story sharing (max 5 lines)
/// </summary>
public class LyricsSelectionDialogFragment : DialogFragment
{
    private readonly List<string> _allLyrics;
    private readonly Action<List<string>> _onSelectionComplete;
    private readonly HashSet<int> _selectedIndices = new();
    private LyricsSelectionAdapter? _adapter;
    private TextView? _counterText;
    private const int MaxSelection = 5;

    public LyricsSelectionDialogFragment(List<string> allLyrics, Action<List<string>> onSelectionComplete)
    {
        _allLyrics = allLyrics;
        _onSelectionComplete = onSelectionComplete;
    }

    public override Dialog OnCreateDialog(Bundle? savedInstanceState)
    {
        var builder = new MaterialAlertDialogBuilder(RequireContext());
        var inflater = RequireActivity().LayoutInflater;
        
        // Create custom view
        var view = CreateDialogView(inflater);
        
        builder.SetView(view);
        builder.SetTitle("Select Lyrics (Max 5 Lines)");
        builder.SetPositiveButton("Share", (sender, args) =>
        {
            var selectedLyrics = _selectedIndices
                .OrderBy(i => i)
                .Select(i => _allLyrics[i])
                .ToList();
            _onSelectionComplete(selectedLyrics);
        });
        builder.SetNegativeButton("Cancel", (sender, args) =>
        {
            _onSelectionComplete(new List<string>());
        });

        return builder.Create();
    }

    private View CreateDialogView(LayoutInflater inflater)
    {
        var context = RequireContext();
        var rootLayout = new LinearLayout(context)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent
            )
        };
        rootLayout.SetPadding(40, 40, 40, 40);

        // Counter text
        _counterText = new TextView(context)
        {
            Text = "0 / 5 lines selected",
            TextSize = 14,
            Gravity = GravityFlags.Center
        };
        _counterText.SetTextColor(Android.Graphics.Color.Gray);
        rootLayout.AddView(_counterText, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent
        )
        { BottomMargin = 20 });

        // RecyclerView for lyrics
        var recyclerView = new RecyclerView(context)
        {
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                (int)(400 * context.Resources.DisplayMetrics.Density) // Max height
            )
        };
        recyclerView.SetLayoutManager(new LinearLayoutManager(context));

        _adapter = new LyricsSelectionAdapter(_allLyrics, OnItemSelected);
        recyclerView.SetAdapter(_adapter);

        rootLayout.AddView(recyclerView);

        return rootLayout;
    }

    private void OnItemSelected(int position, bool isSelected)
    {
        if (isSelected)
        {
            if (_selectedIndices.Count >= MaxSelection)
            {
                Toast.MakeText(RequireContext(), $"Maximum {MaxSelection} lines allowed", ToastLength.Short)?.Show();
                _adapter?.NotifyItemChanged(position);
                return;
            }
            _selectedIndices.Add(position);
        }
        else
        {
            _selectedIndices.Remove(position);
        }

        UpdateCounter();
    }

    private void UpdateCounter()
    {
        if (_counterText != null)
        {
            _counterText.Text = $"{_selectedIndices.Count} / {MaxSelection} lines selected";
        }
    }

    private class LyricsSelectionAdapter : RecyclerView.Adapter
    {
        private readonly List<string> _lyrics;
        private readonly Action<int, bool> _onItemClick;
        private readonly HashSet<int> _selectedPositions = new();

        public LyricsSelectionAdapter(List<string> lyrics, Action<int, bool> onItemClick)
        {
            _lyrics = lyrics;
            _onItemClick = onItemClick;
        }

        public override int ItemCount => _lyrics.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (holder is LyricsViewHolder viewHolder)
            {
                var lyric = _lyrics[position];
                bool isSelected = _selectedPositions.Contains(position);
                
                viewHolder.TextView.Text = lyric;
                viewHolder.CheckBox.Checked = isSelected;
                
                // Set background color for selected items
                if (isSelected)
                {
                    viewHolder.ItemView.SetBackgroundColor(Android.Graphics.Color.Argb(50, 100, 100, 255));
                }
                else
                {
                    viewHolder.ItemView.SetBackgroundColor(Android.Graphics.Color.Transparent);
                }

                viewHolder.ItemView.Click -= viewHolder.OnItemClick;
                viewHolder.OnItemClick = (s, e) =>
                {
                    bool newState = !_selectedPositions.Contains(position);
                    
                    if (newState)
                    {
                        _selectedPositions.Add(position);
                    }
                    else
                    {
                        _selectedPositions.Remove(position);
                    }
                    
                    _onItemClick(position, newState);
                    NotifyItemChanged(position);
                };
                viewHolder.ItemView.Click += viewHolder.OnItemClick;
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var context = parent.Context!;
            var itemLayout = new LinearLayout(context)
            {
                Orientation = Orientation.Horizontal,
                LayoutParameters = new ViewGroup.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.WrapContent
                )
            };
            itemLayout.SetPadding(30, 25, 30, 25);

            var checkbox = new CheckBox(context)
            {
                LayoutParameters = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.WrapContent,
                    ViewGroup.LayoutParams.WrapContent
                )
                { Gravity = GravityFlags.CenterVertical }
            };
            checkbox.Clickable = false;
            checkbox.Focusable = false;

            var textView = new TextView(context)
            {
                LayoutParameters = new LinearLayout.LayoutParams(
                    0,
                    ViewGroup.LayoutParams.WrapContent,
                    1f
                )
                { LeftMargin = 30, Gravity = GravityFlags.CenterVertical },
                TextSize = 16
            };
            textView.SetMaxLines(3);
            textView.Ellipsize = Android.Text.TextUtils.TruncateAt.End;

            itemLayout.AddView(checkbox);
            itemLayout.AddView(textView);

            return new LyricsViewHolder(itemLayout, textView, checkbox);
        }

        private class LyricsViewHolder : RecyclerView.ViewHolder
        {
            public TextView TextView { get; }
            public CheckBox CheckBox { get; }
            public EventHandler? OnItemClick;

            public LyricsViewHolder(View itemView, TextView textView, CheckBox checkBox) : base(itemView)
            {
                TextView = textView;
                CheckBox = checkBox;
            }
        }
    }
}
