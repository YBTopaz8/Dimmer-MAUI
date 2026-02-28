using Dimmer.ViewsAndPages.NativeViews.Adapters;
using Google.Android.Material.ProgressIndicator;

namespace Dimmer.ViewsAndPages.NativeViews.DimmerEvents;

public partial class HistoryFragment : Fragment
{
    private RecyclerView? _songListRecycler;
    private PlayEventsAdapter? _adapter;
    private BaseViewModelAnd? _viewModel;
    private LinearLayoutManager _layoutManager;
    private CompositeDisposable _disposables = new();

    // Programmatic view references
    private View _loadingOverlay;
    private View _emptyOverlay;
    private View _errorOverlay;

    // Unique IDs for view states
    private static readonly int IdRecycler = View.GenerateViewId();
    private static readonly int IdLoading = View.GenerateViewId();
    private static readonly int IdEmpty = View.GenerateViewId();

    public HistoryFragment(BaseViewModelAnd vm)
    {
        _viewModel = vm;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = RequireContext();

        // 1. Root Container (CoordinatorLayout for smooth scrolling behaviors)
        var root = new CoordinatorLayout(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(-1, -1)
        };
        root.SetBackgroundColor(UiBuilder.IsDark(ctx) ? Color.ParseColor("#0D0E20") : Color.ParseColor("#F5F5F5"));

        // 2. Main content area (FrameLayout to stack overlays)
        var mainStack = new FrameLayout(ctx)
        {
            LayoutParameters = new CoordinatorLayout.LayoutParams(-1, -1)
        };

        // 3. Setup RecyclerView
        _songListRecycler = new RecyclerView(ctx)
        {
            Id = IdRecycler,
            LayoutParameters = new FrameLayout.LayoutParams(-1, -1),
            HasFixedSize = true // Performance boost
        };

        _songListRecycler.SetItemViewCacheSize(10); // Don't cache too many off-screen items initially
        _songListRecycler.DrawingCacheEnabled = true;
        _songListRecycler.DrawingCacheQuality = DrawingCacheQuality.Low;
        _layoutManager = new LinearLayoutManager(ctx);
        _songListRecycler.SetLayoutManager(_layoutManager);

        // Add divider
        var divider = new DividerItemDecoration(ctx, DividerItemDecoration.Vertical);
        _songListRecycler.AddItemDecoration(divider);

        // 4. Create Overlays
        _loadingOverlay = CreateLoadingOverlay(ctx);
        _emptyOverlay = CreateEmptyOverlay(ctx);

        // 5. Build Hierarchy
        mainStack.AddView(_songListRecycler);
        mainStack.AddView(_loadingOverlay);
        mainStack.AddView(_emptyOverlay);
        root.AddView(mainStack);

        // 6. Initialize Logic
        SetupAdapterAndListeners();
        SubscribeToViewModelStates();

        return root;
    }

    private void SetupAdapterAndListeners()
    {
        _adapter = new PlayEventsAdapter(_viewModel.DimmerEvents, _viewModel.RealmFactory);
        _songListRecycler.SetAdapter(_adapter);

        // Infinite Scroll
        _songListRecycler.AddOnScrollListener(new EndlessScrollListener(_layoutManager, () =>
        {
            if (_viewModel.CanGoNext)
            {
                _viewModel.NextEvtPageCommand.Execute(null);
                _adapter.SetLoadingMore(true);
            }
        }));
    }
    private void SubscribeToViewModelStates()
    {
        _disposables = new CompositeDisposable();

        // Fix: Replace WhenAnyValue with your project's WhenPropertyChange extension
        _viewModel?.WhenPropertyChange(nameof(_viewModel.IsLoading), x => x.IsLoading)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(isLoading =>
            {
                _loadingOverlay.Visibility = isLoading ? ViewStates.Visible : ViewStates.Gone;
                _songListRecycler?.Alpha = isLoading ? 0.5f : 1.0f;

                if (!isLoading)
                {
                    _adapter?.SetLoadingMore(false);
                }
            })
            .DisposeWith(_disposables);

        // Observe Data count for Empty State
        _viewModel?.DimmerEvents.Connect()
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(_ =>
            {
                var isEmpty = _viewModel.DimmerEvents.Count == 0 && !_viewModel.IsLoading;
                if (_emptyOverlay != null)
                {
                    _emptyOverlay.Visibility = isEmpty ? ViewStates.Visible : ViewStates.Gone;
                }
            })
            .DisposeWith(_disposables);
    }

    // --- Programmatic UI Builders ---

    private LinearLayout CreateLoadingOverlay(Context ctx)
    {
        var root = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            Visibility = ViewStates.Gone
        };
        root.SetGravity(GravityFlags.Center);

        var progress = new LinearProgressIndicator(ctx)
        {
            Indeterminate = true,
            LayoutParameters = new LinearLayout.LayoutParams(AppUtil.DpToPx(200), -2)
        };

        var text = new TextView(ctx) { Text = "Loading history...", TextSize = 14 };
        text.SetPadding(0, 20, 0, 0);

        root.AddView(progress);
        root.AddView(text);
        return root;
    }

    private LinearLayout CreateEmptyOverlay(Context ctx)
    {
        var root = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            Visibility = ViewStates.Gone
        };
        root.SetGravity(GravityFlags.Center);

        var icon = new ImageView(ctx);
        icon.SetImageResource(Android.Resource.Drawable.IcMenuRecentHistory);
        icon.SetColorFilter(Color.Gray);

        var text = new TextView(ctx)
        {
            Text = "No history found",
            TextSize = 18,
            Typeface = Typeface.DefaultBold
        };
        text.SetTextColor(Color.Gray);

        root.AddView(icon, new LinearLayout.LayoutParams(AppUtil.DpToPx(80), AppUtil.DpToPx(80)));
        root.AddView(text);
        return root;
    }

    public override void OnResume()
    {
        base.OnResume();
        _viewModel?.ActivateHistory();
    }

    public override void OnDestroyView()
    {
        _disposables.Dispose();
        _adapter?.Dispose();
        _adapter = null;
        _songListRecycler = null;
        base.OnDestroyView();
    }
}

// --- High Performance Scroll Listener ---
public class EndlessScrollListener : RecyclerView.OnScrollListener
{
    private readonly LinearLayoutManager _layoutManager;
    private readonly Action _loadMoreAction;
    private bool _isCurrentlyLoading = false;

    public EndlessScrollListener(LinearLayoutManager layoutManager, Action loadMoreAction)
    {
        _layoutManager = layoutManager;
        _loadMoreAction = loadMoreAction;
    }

    public override void OnScrolled(RecyclerView recyclerView, int dx, int dy)
    {
        if (dy <= 0) return; // Only check when scrolling down

        var totalItemCount = _layoutManager.ItemCount;
        var lastVisibleItem = _layoutManager.FindLastVisibleItemPosition();

        // Trigger when 5 items from the bottom
        if (!_isCurrentlyLoading && totalItemCount <= (lastVisibleItem + 5))
        {
            _isCurrentlyLoading = true;
            _loadMoreAction?.Invoke();
        }
    }

    public void SetLoaded() => _isCurrentlyLoading = false;
}