using AndroidX.Lifecycle;
using Dimmer.DimmerSearch;
using Dimmer.UiUtils;

namespace Dimmer.ViewsAndPages.NativeViews.Misc;

public class ArtistPickerBottomSheet : BottomSheetDialogFragment
{
    public ArtistPickerBottomSheet(BaseViewModelAnd vm, string listOfArtistString)

    {
        MyViewModel = vm;
        ListOfArtistsInSong = listOfArtistString.Split(", ").ToList();
    }

    public BaseViewModelAnd MyViewModel { get; }
    public List<string> ListOfArtistsInSong { get; }

    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var context = Context;
        if (context == null) return null;
        var layout = new LinearLayout(context) { Orientation = Orientation.Vertical };
        layout.SetPadding(32, 32, 32, 32);

        // Title
        var title = new TextView(context) { Text = $"Select Artist", TextSize = 22 };
        title.SetPadding(8, 8, 8, 8);
        title.SetTypeface(null, Android.Graphics.TypefaceStyle.Bold);
        layout.AddView(title);

        // Search Bar
        var searchInput = UiBuilder.CreateInput(context, string.Empty, "");
        layout.AddView(searchInput);

        // RecyclerView for Artists
        var recyclerView = new RecyclerView(context);
        recyclerView.SetLayoutManager(new LinearLayoutManager(context));
        // In a real app, pass your ViewModel's Artist list here
        
        recyclerView.SetAdapter(new SimpleStringAdapter(ListOfArtistsInSong, (selected) => {
            // Handle Selection (Callback to parent fragment)
            MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.PresetQueries.ByArtist(selected));
            
            Toast.MakeText(context, $"Selected {selected}", ToastLength.Short)?.Show();
            Dismiss();
        }));

        layout.AddView(recyclerView, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 800)); // Fixed height or use weight

        return layout;
    }

    // Simple Adapter for the list
    class SimpleStringAdapter : RecyclerView.Adapter
    {
        private List<string> _items;
        private Action<string> _onClick;

        public SimpleStringAdapter(List<string> items, Action<string> onClick) { _items = items; _onClick = onClick; }

        public override int ItemCount => _items.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = holder as VHolder;
            vh.Text.Text = _items[position];
            vh.ItemView.Click += (s, e) =>
            {
                _onClick(_items[position]);
            };
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var splitBtn = new MaterialSplitButton(parent.Context);
            splitBtn.SetPadding(24, 24, 24, 24);
            splitBtn.Click += (s, e) =>
            {
                var popUp = new PopupMenu(parent.Context, parent);

                popUp.Show();
                Debug.WriteLine("Clickedd");
            };
            splitBtn.ContextClick += (s, e) =>
            {
                Debug.WriteLine("Context Clickedd");
            };

            var tv = new TextView(parent.Context) { TextSize = 18 };
            tv.SetPadding(24, 24, 24, 24);
            return new VHolder(tv);
        }

        class VHolder : RecyclerView.ViewHolder
        {
            public TextView Text => (TextView)ItemView;
            public VHolder(View v) : base(v) { }
        }
    }
}