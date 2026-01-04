namespace Dimmer.ViewsAndPages.NativeViews.Misc;

public class QueueBottomSheetFragment : BottomSheetDialogFragment
{
    private BaseViewModelAnd MyViewModel;
    private RecyclerView _recyclerView;
    private SongAdapter _adapter;
    private bool _pendingScrollToCurrent;
    public QueueBottomSheetFragment(BaseViewModelAnd viewModel)
    {
        MyViewModel = viewModel;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context;

        // 1. Root Container (FrameLayout allows stacking the Pill over the List)
        var rootFrame = new FrameLayout(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };
        // Optional: Set a background if needed, though BottomSheet usually handles this
        rootFrame.SetBackgroundColor(Color.Transparent);

        // 2. The Recycler View (The Queue)
        _recyclerView = new RecyclerView(ctx);
        _recyclerView.SetLayoutManager(new LinearLayoutManager(ctx));

        // Pass "queue" to your adapter to bind to PlaybackSource
        _adapter = new SongAdapter(ctx, MyViewModel, this, "queue");
        _recyclerView.SetAdapter(_adapter);

        // Add padding at bottom so the last item isn't covered by the pill
        _recyclerView.SetPadding(0, 0, 0, AppUtil.DpToPx(80));
        _recyclerView.SetClipToPadding(false);

        var listParams = new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent);
        _recyclerView.LayoutParameters = listParams;

        rootFrame.AddView(_recyclerView);

        // 3. The Pill Container (MaterialCardView)
        var pillCard = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(25), // High radius for Pill shape
            CardElevation = AppUtil.DpToPx(6),
            CardBackgroundColor = Android.Content.Res.ColorStateList.ValueOf(Color.ParseColor("#303030")) // Dark grey pill
        };

        var pillParams = new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent,
            AppUtil.DpToPx(50)); // Fixed height for the pill

        // --- POSITIONING: Absolute Bottom Center ---
        pillParams.Gravity = GravityFlags.Bottom | GravityFlags.CenterHorizontal;
        pillParams.SetMargins(0, 0, 0, AppUtil.DpToPx(20)); // Lift it up slightly
        pillCard.LayoutParameters = pillParams;

        // 4. Horizontal Layout inside the Pill
        var pillContent = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.MatchParent)
        };
        pillContent.SetGravity(GravityFlags.Center);
        pillContent.SetPadding(AppUtil.DpToPx(15), 0, AppUtil.DpToPx(15), 0);

        // 5. The Eye Button
        var eyeBtn = new MaterialButton(ctx, null, Resource.Attribute.borderlessButtonStyle);
        eyeBtn.IconTint = Android.Content.Res.ColorStateList.ValueOf(Color.White);
        eyeBtn.Text = "Scroll To"; // Optional text, or remove for icon only
        eyeBtn.SetIconResource(Resource.Drawable.eye);
        eyeBtn.IconSize = AppUtil.DpToPx(18);
        eyeBtn.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Color.Transparent);

        // Button Action: Scroll to currently playing
        eyeBtn.Click += (s, e) => ScrollToSong();

       


        pillContent.AddView(eyeBtn);
        pillCard.AddView(pillContent);

        // Add Pill to Root (It draws on top of RecyclerView because it's added last)
        rootFrame.AddView(pillCard);

        return rootFrame;
    }


    public override void OnViewCreated(View? view, Bundle? savedInstanceState)
    {
        base.OnViewCreated(view, savedInstanceState);
        
        // Enable drag & drop for queue reordering
        if (_recyclerView != null && _adapter != null)
        {
            var callback = new SongAdapter.SimpleItemTouchHelperCallback(_adapter);
            var itemTouchHelper = new ItemTouchHelper(callback);
            itemTouchHelper.AttachToRecyclerView(_recyclerView);
        }
        
        if (_pendingScrollToCurrent)
        {
            
            view.Post(() =>
            {
                ScrollToSong();
            });
        }
    }



    public void ScrollToSong(SongModelView? requestedSong=null)
    {
        if (_recyclerView == null)
        {
            _pendingScrollToCurrent = true;
            return;
        }
        if (MyViewModel.CurrentPlayingSongView == null) return;
        requestedSong ??= MyViewModel.CurrentPlayingSongView;

        // Since we are using the "queue" mode in adapter, we need to find the index in PlaybackQueue
        var index = MyViewModel.PlaybackQueue.IndexOf(requestedSong);
        if (_pendingScrollToCurrent)
        {
            _recyclerView.ScrollToPosition(index); 
            _pendingScrollToCurrent = false;
        }
        else
        {
            _recyclerView.SmoothScrollToPosition(index); 
        }
    }
}