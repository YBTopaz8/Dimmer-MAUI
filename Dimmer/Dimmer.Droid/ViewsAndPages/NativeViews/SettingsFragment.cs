

namespace Dimmer.ViewsAndPages.NativeViews;

internal class SettingsFragment  : Fragment, IOnBackInvokedCallback
{
    private BaseViewModelAnd _viewModel;
    private RecyclerView _folderRecycler;
    private Button _addFolderButton;
    private FolderAdapter _adapter;
    public SettingsFragment(BaseViewModelAnd myViewModel)
    {
        this._viewModel = myViewModel;
    }
    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context!;
        var root = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };

        _addFolderButton = new Button(ctx) { Text = "Add Folder" };
        _addFolderButton.Click += AddFolderButton_Click; 
        

        _folderRecycler = new RecyclerView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                0,
                1f)
        };
        _folderRecycler.SetLayoutManager(new LinearLayoutManager(ctx));

        _adapter = new FolderAdapter(_viewModel.FolderPaths);
        _folderRecycler.SetAdapter(_adapter);

        root.AddView(_addFolderButton);
        root.AddView(_folderRecycler);

        return root;
    }

    private async void AddFolderButton_Click(object? sender, EventArgs e)
    {
        
        await _viewModel.AddMusicFolderViaPickerAsync();
    }

    // Simple string adapter
    class FolderAdapter : RecyclerView.Adapter
    {
        private readonly IList<string> _items;

        public FolderAdapter(IList<string> items)
        {
            _items = items;
            NotifyDataSetChanged();
        }

        public override int ItemCount => _items.Count;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var txt = new TextView(parent.Context)
            {
                TextSize = 16f,
            };
            txt.SetPadding(20,20,20,20);
            return new SimpleVH(txt);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (holder is SimpleVH vh)
            {
                vh.TextView.Text = _items[position];
            }
        }

        class SimpleVH : RecyclerView.ViewHolder
        {
            public TextView TextView { get; }
            public SimpleVH(TextView v) : base(v) => TextView = v;
        }
    }

    public void OnBackInvoked()
    {
        Toast.MakeText(Context!, "Back invoked in Settings Fragment", ToastLength.Short)?.Show();
    }
}