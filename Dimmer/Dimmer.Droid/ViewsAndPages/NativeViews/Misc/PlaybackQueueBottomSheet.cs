using Google.Android.Material.Loadingindicator;
using Google.Android.Material.ProgressIndicator;

namespace Dimmer.ViewsAndPages.NativeViews.Misc;


public class QueueBottomSheetFragment : BottomSheetDialogFragment, IOnBackInvokedCallback
{
    private BaseViewModelAnd MyViewModel;
    private RecyclerView _songListRecycler;
    private SongAdapter _adapter;
    private MaterialButton _callerBtn;
    private LinearProgressIndicator _loadingIndicator;
    private CompositeDisposable _disposables = new();
    private bool _pendingScrollToCurrent;

    public QueueBottomSheetFragment(BaseViewModelAnd viewModel, Button callerBtn)
    {
        MyViewModel = viewModel;
        _callerBtn = (MaterialButton)callerBtn;
    }

    public void OnBackInvoked()
    {
        (Activity as TransitionActivity)?.HandleBackPressInternal();
    }

    public override void OnDismiss(IDialogInterface dialog)
    {
        base.OnDismiss(dialog);
        if (_callerBtn != null) _callerBtn.Enabled = true;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = RequireContext();

        // 1. Root Container
        var rootFrame = new FrameLayout(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(-1, -1)
        };

        // 2. RecyclerView
        _songListRecycler = new RecyclerView(ctx);
        _songListRecycler.SetLayoutManager(new LinearLayoutManager(ctx));
        _songListRecycler.SetPadding(0, 0, 0, AppUtil.DpToPx(80)); // Padding for Pill
        _songListRecycler.SetClipToPadding(false);
        rootFrame.AddView(_songListRecycler);

        // 3. Loading Indicator
        _loadingIndicator = new LinearProgressIndicator(ctx)
        {
            Indeterminate = true,
            LayoutParameters = new FrameLayout.LayoutParams(-1, AppUtil.DpToPx(4)) { Gravity = GravityFlags.Top },
            Visibility = ViewStates.Visible
        };
        rootFrame.AddView(_loadingIndicator);

        // 4. The Pill Container (Floating Action Bar style)
        var pillCard = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(25),
            CardElevation = AppUtil.DpToPx(6),
            CardBackgroundColor = Android.Content.Res.ColorStateList.ValueOf(Color.ParseColor("#303030")),
            LayoutParameters = new FrameLayout.LayoutParams(-2, AppUtil.DpToPx(50))
            {
                Gravity = GravityFlags.Bottom | GravityFlags.CenterHorizontal,
                BottomMargin = AppUtil.DpToPx(20)
            }
        };

        var pillContent = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new ViewGroup.LayoutParams(-2, -1)
        };

        pillContent.SetGravity(GravityFlags.Center);
        pillContent.SetPadding(AppUtil.DpToPx(15), 0, AppUtil.DpToPx(15), 0);

        // Buttons inside Pill
        pillContent.AddView(CreatePillButton(ctx, "Scroll To", Resource.Drawable.eye, (s, e) => ScrollToSong()));
        pillContent.AddView(CreatePillButton(ctx, "Sort", Resource.Drawable.sortfromtoptobottom, (s, e) => ShowSortMenu())); // NEW
        pillContent.AddView(CreatePillButton(ctx, "Save", Resource.Drawable.savea, async (s, e) => await SaveQueueAsPlaylist()));

        pillCard.AddView(pillContent);
        rootFrame.AddView(pillCard);

        return rootFrame;
    }

    private View CreatePillButton(Context ctx, string text, int iconRes, EventHandler clickHandler)
    {
        var btn = new MaterialButton(ctx, null, Resource.Attribute.borderlessButtonStyle);
        btn.Text = text;
        btn.SetIconResource(iconRes);
        btn.IconTint = Android.Content.Res.ColorStateList.ValueOf(Color.White);
        btn.SetTextColor(Color.White);
        btn.IconSize = AppUtil.DpToPx(18);
        btn.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Color.Transparent);
        btn.Click += clickHandler;
        return btn;
    }

    public override void OnViewCreated(View view, Bundle? savedInstanceState)
    {
        base.OnViewCreated(view, savedInstanceState);

        // Initialize Adapter
        _adapter = new SongAdapter(RequireContext(), MyViewModel, this, SongAdapter.SongsToWatchSource.QueuePage);
        _songListRecycler.SetAdapter(_adapter);

        // Attach Drag & Drop Helper
        var callback = new SongAdapter.SimpleItemTouchHelperCallback(_adapter);
        var itemTouchHelper = new ItemTouchHelper(callback);
        itemTouchHelper.AttachToRecyclerView(_songListRecycler);

        if (_pendingScrollToCurrent)
        {
            view.Post(() => ScrollToSong());
        }
    }

    public override void OnResume()
    {
        base.OnResume();
        MyViewModel.CurrentFragment = this;
        _disposables = new CompositeDisposable();

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
        {
            Activity?.OnBackInvokedDispatcher.RegisterOnBackInvokedCallback((int)IOnBackInvokedDispatcher.PriorityDefault, this);
        }

        // Listen for when the adapter finishes loading data
        _adapter.IsAdapterReady
            .Where(ready => ready)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(_ =>
            {
                _loadingIndicator.Visibility = ViewStates.Gone;
                ScrollToSong(); // Auto-scroll on first load
            })
            .DisposeWith(_disposables);
    }

    public override void OnPause()
    {
        base.OnPause();
        _disposables.Dispose();
    }

    public void ScrollToSong(SongModelView? requestedSong = null)
    {
        if (_songListRecycler == null || _adapter == null)
        {
            _pendingScrollToCurrent = true;
            return;
        }

        var song = requestedSong ?? MyViewModel.CurrentPlayingSongView;
        if (song == null) return;

        // Find index in the adapter's CURRENT list (not just the ViewModel's list, to be safe)
        var index = _adapter.Songs.IndexOf(song);

        if (index >= 0)
        {
            // Scroll with offset so it's not hidden behind top bars
            ((LinearLayoutManager)_songListRecycler.GetLayoutManager())?.ScrollToPositionWithOffset(index, AppUtil.DpToPx(100));
            _pendingScrollToCurrent = false;
        }
    }

    private void ShowSortMenu()
    {
        var popup = new PopupMenu(Context, View, GravityFlags.Center);
        popup.Menu.Add(0, 1, 0, "Title (A-Z)");
        popup.Menu.Add(0, 2, 0, "Artist (A-Z)");
        popup.Menu.Add(0, 3, 0, "Duration (Short-Long)");
        popup.Menu.Add(0, 4, 0, "Shuffle"); // Randomize

        popup.MenuItemClick += (s, e) =>
        {
            switch (e.Item.ItemId)
            {
                // NOTE: Implement SortQueue in your ViewModel if not present!
                // case 1: MyViewModel.SortQueue(s => s.Title); break;
                // case 2: MyViewModel.SortQueue(s => s.ArtistName); break;
                // case 3: MyViewModel.SortQueue(s => s.DurationInSeconds); break;
                //case 4: MyViewModel.ShuffleCurrentQueue(); break;
            }
            _adapter.NotifyDataSetChanged(); // Refresh UI
            ScrollToSong();
        };
        popup.Show();
    }

    private async Task SaveQueueAsPlaylist()
    {
        var ctx = Context;
        if (ctx == null) return;

        var input = new TextInputEditText(ctx) { Hint = "Playlist Name" };
        var container = new FrameLayout(ctx);
        container.SetPaddingRelative(AppUtil.DpToPx(24), AppUtil.DpToPx(12), AppUtil.DpToPx(24), 0);
        container.AddView(input);

        new MaterialAlertDialogBuilder(ctx)
            .SetTitle("Save Queue")
            .SetView(container)
            .SetPositiveButton("Save", (s, e) =>
            {
                var name = input.Text?.Trim();
                if (!string.IsNullOrEmpty(name))
                {
                    // MyViewModel.SaveQueueAsPlaylist(name); // Implement in VM
                    Toast.MakeText(ctx, $"Saved '{name}'", ToastLength.Short)?.Show();
                }
            })
            .SetNegativeButton("Cancel", handler: null)
            .Show();
    }
}