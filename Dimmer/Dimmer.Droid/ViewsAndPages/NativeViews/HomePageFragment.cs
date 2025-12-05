using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

using Android.Graphics;
using Android.Views.InputMethods;

using Bumptech.Glide;

using Google.Android.Material.Button;
using Google.Android.Material.Card;
using Google.Android.Material.Chip;
using Google.Android.Material.TextField;

using static Dimmer.ViewsAndPages.NativeViews.SongAdapter;

namespace Dimmer.ViewsAndPages.NativeViews;

public partial class HomePageFragment : Fragment, IOnBackInvokedCallback
{

    string toSettingsTrans = "homePageFAB";

    public RecyclerView? _songListRecycler = null!;
    public TextView _emptyLabel = null!;
    public TextView _titleTxt = null!;
    public TextView _albumTxt = null!;
    public TextView _artistTxt = null!;
    public TextView _playCount = null!;
    public ImageView _albumArt = null!;
    public float _downX;
    public float _downY;
    private TextInputEditText _searchBar;
    public FloatingActionButton? cogButton = null!;
    FrameLayout? root;
    public TextView CurrentTimeTextView;
    public FrameLayout? Root => root;
    public BaseViewModelAnd MyViewModel { get; private set; } = null!;
    private bool _isNavigating;

    public HomePageFragment()
    {
        
    }
    public override void OnAttach(Context context)
    {
        base.OnAttach(context);
        if (MyViewModel == null)
        {
            try
            {
                if (MainApplication.ServiceProvider != null)
                {
                    MyViewModel = MainApplication.ServiceProvider.GetRequiredService<BaseViewModelAnd>();
                }
            }
            catch (Exception ex)
            {
                Android.Widget.Toast.MakeText(context, $"DI FAILED: {ex.Message}", Android.Widget.ToastLength.Long)?.Show();
                Console.WriteLine($"HomePageFragment Injection Failed: {ex}");
            }
        }
    }
    public HomePageFragment(BaseViewModelAnd myViewModel)
    {
        MyViewModel = myViewModel;


    }
    private CancellationTokenSource? _searchCts;
    private SongAdapter _adapter;
    private TextInputEditText searchBar;

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context;

        // Root
        var root = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        };

        // 1. Search Bar
        var searchCard = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(25),
            CardElevation = AppUtil.DpToPx(4),
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        ((LinearLayout.LayoutParams)searchCard.LayoutParameters).SetMargins(20, 40, 20, 20);

        _searchBar = new TextInputEditText(ctx)
        {
            Hint = "Search library...",
            Background = null // Removes the underline bar you hated
        };
        _searchBar.SetPadding(40, 30, 40, 30);

        searchCard.AddView(_searchBar);

        // 2. RecyclerView
        _songListRecycler = new RecyclerView(ctx);
        _songListRecycler.SetLayoutManager(new LinearLayoutManager(ctx));
        _songListRecycler.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);

        // Attach Adapter
        var adapter = new SongAdapter(ctx, MyViewModel, this);
        _songListRecycler.SetAdapter(adapter);

        root.AddView(_songListRecycler);


        var helpBtn = new Google.Android.Material.Button.MaterialButton(ctx, null, Resource.Attribute.materialIconButtonStyle)
        {
            Icon = AndroidX.Core.Content.ContextCompat.GetDrawable(ctx, Android.Resource.Drawable.IcMenuHelp), // Use a real drawable resource
            IconTint = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.DarkGray),
            LayoutParameters = new LinearLayout.LayoutParams(AppUtil.DpToPx(50), AppUtil.DpToPx(50))
        };
        ((LinearLayout.LayoutParams)helpBtn.LayoutParameters).Gravity = GravityFlags.CenterVertical;
        helpBtn.Click += (s, e) => OpenTqlGuide();

        // Add helpBtn to the searchCard or the layout next to it
        // If searchCard is horizontal LinearLayout, add it there.
        // If not, you might want to wrap SearchBar and HelpBtn in a Horizontal LinearLayout.
        var headerLayout = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        headerLayout.AddView(searchCard); // Adjust params to weight=1
        headerLayout.AddView(helpBtn);
        root.AddView(headerLayout, 0); // Add at top

        return root;
    }
    public void OpenTqlGuide()
    {
        var guideFrag = new TqlGuideFragment(MyViewModel);

        ParentFragmentManager.BeginTransaction()
            .SetReorderingAllowed(true)
            // This adds the fragment on top (like a full screen dialog)
            .Add(Android.Resource.Id.Content, guideFrag)
            .AddToBackStack("TqlGuide")
            .Commit();
    }

    public override void OnResume()
    {
        base.OnResume();
        
        _isNavigating = false;
    }
    public override void OnViewCreated(View view, Bundle? savedInstanceState)
    {
        base.OnViewCreated(view, savedInstanceState);
        //PostponeEnterTransition();


        //view.ViewTreeObserver.AddOnPreDrawListener(new MyPreDrawListener(this, view));

        _isNavigating = false;
        MyViewModel.CurrentPage = this;

        this.View!.Tag = "HomePageFragment";
        MyViewModel.SetupSubscriptions();


        var currentlyPlayingIndex = MyViewModel.SearchResults.IndexOf(MyViewModel.CurrentPlayingSongView);
        if (currentlyPlayingIndex >= 0)
            _songListRecycler?.SmoothScrollToPosition(currentlyPlayingIndex);

        MyViewModel.ScrollToCurrentSongRequest
        .ObserveOn(RxSchedulers.UI) // Ensure runs on UI thread
        .Subscribe(_ =>
        {
            if (_songListRecycler != null && _adapter != null)
            {
                var index = MyViewModel.SearchResults.IndexOf(MyViewModel.CurrentPlayingSongView);
                if (index >= 0)
                {
                    // Smooth scroll looks nicer
                    _songListRecycler.SmoothScrollToPosition(index);

                    // Flash the item? (Requires access to ViewHolder, maybe for later)
                }
            }
        })
        .DisposeWith(CompositeDisposables);
    }

    protected CompositeDisposable CompositeDisposables { get; } = new CompositeDisposable();

    public override void OnDestroyView()
    {
        base.OnDestroyView();
        _isNavigating = false;
        _searchCts?.Cancel();
        _songListRecycler?.SetAdapter(null);
        _songListRecycler = null;

    }

    private void _pageFAB_LongClick(object? sender, View.LongClickEventArgs e)
    {
        if (searchBar.RequestFocus())
        {
            InputMethodManager? imm = Context!.GetSystemService(Context.InputMethodService) as InputMethodManager;
            if (imm is null) return;
            imm.ShowSoftInput(searchBar, ShowFlags.Implicit);
        }


    }

    private void AlbumArt_Click(object? sender, EventArgs e)
    {
        MyViewModel.NavigateToNowPlayingFragmentFromHome
            (this, _albumArt,
            _titleTxt, _artistTxt,
            _albumTxt);
    }

    private async void Touch_SingleTap(int pos, View arg2, SongModelView song)
    {

        if (song != null)
        {
            await MyViewModel.PlaySong(song, CurrentPage.AllSongs, MyViewModel.SearchResults);
            _adapter.NotifyDataSetChanged();
        }
        Toast.MakeText(Context, $"Single tap {pos}", ToastLength.Short)?.Show();
    }

    
    public void PageFAB_Click(object? sender, EventArgs e)
    {
        var graphFrag = new GraphExplorerFragment(MyViewModel);
        ParentFragmentManager.BeginTransaction()
            .Replace(TransitionActivity.MyStaticID, graphFrag)
            .AddToBackStack(null)
            .Commit();
        return;

        var bottomSheetDialog = new BottomSheetDialog(Context!);

        var layout = new LinearLayout(Context!)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent)
        };
        layout.SetPadding(0, 20, 0, 40); // Add some bottom padding for safety

        var title = new TextView(Context!)
        {
            Text = $"Current Queue {MyViewModel.PlaybackQueue.Count} Songs",
            TextSize = 20f,
            Gravity = GravityFlags.Center,

        };
        title.SetPadding(0, 20, 0, 20);

        title.SetTypeface(null, TypefaceStyle.Bold);

        layout.AddView(title);
        var recyclerView = new RecyclerView(Context!)
        {
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent)
        };
        recyclerView.SetLayoutManager(new LinearLayoutManager(Context!));
        var adapter = new SongAdapter(Context!, MyViewModel, this, "queue");
        recyclerView.SetAdapter(adapter);
        var callback = new SimpleItemTouchHelperCallback(adapter);
        var itemTouchHelper = new ItemTouchHelper(callback);
        itemTouchHelper.AttachToRecyclerView(recyclerView);


        var behavior = bottomSheetDialog.Behavior;

        var displayMetrics = Context.Resources.DisplayMetrics;
        int height = displayMetrics.HeightPixels;
        behavior.PeekHeight = (int)(height * 0.6); // 60% of screen height
        behavior.State = BottomSheetBehavior.StateCollapsed;

        recyclerView.NestedScrollingEnabled = true;

        var index = MyViewModel.PlaybackQueue.IndexOf(MyViewModel.CurrentPlayingSongView);
        int currentIndex = index; // Assuming you have this in VM
        if (currentIndex >= 0 && currentIndex < adapter.ItemCount)
        {
            recyclerView.ScrollToPosition(currentIndex);
        }

        layout.AddView(recyclerView);
        bottomSheetDialog.SetContentView(layout);
        bottomSheetDialog.Show();

        // Dismiss when touching outside
        bottomSheetDialog.SetCanceledOnTouchOutside(true);
        
        bottomSheetDialog.DismissWithAnimation = true;
        bottomSheetDialog.DismissEvent += BottomSheetDialog_DismissEvent;


    }

    private void BottomSheetDialog_DismissEvent(object? sender, EventArgs e)
    {
        var ctx = Context;
        if (ctx != null)
        {
            _adapter = new SongAdapter(ctx, MyViewModel, this
                );
            _songListRecycler?.SetAdapter(_adapter);
        }
    }

    private void CurrentTime_Click(object? sender, EventArgs e)
    {
        Android.Widget.Toast.MakeText(Context, "hey!", ToastLength.Short).Show();
    }

    partial class RecyclerViewOnScrollListener : RecyclerView.OnScrollListener
    {
        private readonly Action<int> _onScrolledAction;
        private readonly Action<int> _scrollStateChanged = _ => { };

        public RecyclerViewOnScrollListener(Action<int> onScrolledAction)
        {
            _onScrolledAction = onScrolledAction;
        }
        public override void OnScrolled(RecyclerView recyclerView, int dx, int dy)
        {
            base.OnScrolled(recyclerView, dx, dy);
            _onScrolledAction(dy);
        }
        public override void OnScrollStateChanged(RecyclerView rv, int newState)
        {

            if (newState == RecyclerView.ScrollStateIdle)
            {
                // load higher-quality images

            }
            else if (newState == RecyclerView.ScrollStateDragging)
            {
                // pause image loading
            }
            else if (newState == RecyclerView.ScrollStateSettling)
            {
                // pause image loading
            }
            _scrollStateChanged(newState);


            base.OnScrollStateChanged(rv, newState);
        }
    }

    public void NavToAlbumaPage(string transitionName)
    {
        if (!IsAdded || Activity == null) return;

        MyViewModel.NavigateToSingleSongPageFromHome(
            this,
            transitionName, _albumArt);
    }

    public class MyPreDrawListener : Java.Lang.Object, ViewTreeObserver.IOnPreDrawListener
    {
        private readonly Fragment _fragment;
        private readonly View _view;

        public MyPreDrawListener(Fragment fragment, View view)
        {
            _fragment = fragment;
            _view = view;
        }

        public bool OnPreDraw()
        {
            // Remove listener so it only fires once
            _view.ViewTreeObserver.RemoveOnPreDrawListener(this);

            // 3. Tell transition system: "Okay, views are ready. Start the animation!"
            _fragment.StartPostponedEnterTransition();
            return true;
        }
    }

    public  bool IsDark()
    {
        return (Resources?.Configuration?.UiMode & Android.Content.Res.UiMode.NightYes) != 0;
    }

    public void OnBackInvoked()
    {
        Toast.MakeText(Context!, "Back invoked in HomePageFragment", ToastLength.Short)?.Show();
    }
}