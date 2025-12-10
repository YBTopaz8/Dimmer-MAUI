using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

using Android.Graphics;
using Android.Views.InputMethods;

using AndroidX.CoordinatorLayout.Widget;

using Bumptech.Glide;

using Dimmer.ViewsAndPages.NativeViews.Misc;
using Dimmer.ViewsAndPages.ViewUtils;

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


    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context;

        // 1. Root: CoordinatorLayout (Crucial for FABs)
        var root = new CoordinatorLayout(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        };

        // 2. Main Content Container (Linear Layout inside Coordinator)
        var contentLinear = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new CoordinatorLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        };

        // --- 3. Header Section (Menu + Search + Help) ---
        var headerLayout = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal
        };
        headerLayout.SetGravity(GravityFlags.CenterVertical);
        // Initial padding (will be updated by Insets logic below)
        headerLayout.SetPadding(20, 20, 20, 20);

        // Menu Button
        var menuBtn = new Google.Android.Material.Button.MaterialButton(ctx, null, Resource.Attribute.materialIconButtonStyle)
        {
            Icon = AndroidX.Core.Content.ContextCompat.GetDrawable(ctx, Resource.Drawable.hamburgermenu),
            IconTint = Android.Content.Res.ColorStateList.ValueOf(Color.Gray),
            LayoutParameters = new LinearLayout.LayoutParams(AppUtil.DpToPx(50), AppUtil.DpToPx(50))
        };
        menuBtn.Click += (s, e) => { if (Activity is TransitionActivity act) act.OpenDrawer(); };

        // Search Card
        var searchCard = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(25),
            CardElevation = AppUtil.DpToPx(4),
            LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f) // Weight 1
        };
        ((LinearLayout.LayoutParams)searchCard.LayoutParameters).SetMargins(10, 0, 10, 0);

        _searchBar = new TextInputEditText(ctx)
        {
            Hint = "Search library...",
            Background = null,
            TextSize = 14
        };
        _searchBar.SetPadding(40, 30, 40, 30);
        _searchBar.TextChanged += _searchBar_TextChanged;
        searchCard.AddView(_searchBar);

        // Help Button
        var helpBtn = new Google.Android.Material.Button.MaterialButton(ctx, null, Resource.Attribute.materialIconButtonStyle)
        {
            Icon = AndroidX.Core.Content.ContextCompat.GetDrawable(ctx, Android.Resource.Drawable.IcMenuHelp),
            IconTint = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.DarkGray),
            LayoutParameters = new LinearLayout.LayoutParams(AppUtil.DpToPx(50), AppUtil.DpToPx(50))
        };
        helpBtn.Click += (s, e) => OpenTqlGuide();

        // Add items to Header
        headerLayout.AddView(menuBtn);
        headerLayout.AddView(searchCard);
        headerLayout.AddView(helpBtn);

        // Add Header to Content
        contentLinear.AddView(headerLayout);

        // --- 4. RecyclerView ---
        _songListRecycler = new RecyclerView(ctx);
        _songListRecycler.SetLayoutManager(new LinearLayoutManager(ctx));
        _songListRecycler.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);

        // Add Bottom Padding to Recycler so last item isn't hidden behind MiniPlayer/FAB
        _songListRecycler.SetPadding(0, 0, 0, AppUtil.DpToPx(160));
        _songListRecycler.SetClipToPadding(false);

        var adapter = new SongAdapter(ctx, MyViewModel, this);
        _songListRecycler.SetAdapter(adapter);

        // Add Recycler to Content
        contentLinear.AddView(_songListRecycler);

        // Add Content to Root
        root.AddView(contentLinear);

        // --- 5. Extended FAB ---
        var fab = new Google.Android.Material.FloatingActionButton.ExtendedFloatingActionButton(ctx);
        fab.Text = "Actions";
        fab.SetIconResource(Resource.Drawable.addpl); 
        fab.Extend();

        var fabParams = new CoordinatorLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
        fabParams.Gravity = (int)(GravityFlags.Bottom | GravityFlags.End);
        // Lift FAB above the MiniPlayer (approx 90dp)
        fabParams.SetMargins(0, 0, AppUtil.DpToPx(20), AppUtil.DpToPx(90));
        fab.LayoutParameters = fabParams;

        fab.Click += (s, e) => ShowFabMenu(ctx, fab);

        // Add FAB to Root
        root.AddView(fab);

        // --- 6. Handle Insets ---
        AndroidX.Core.View.ViewCompat.SetOnApplyWindowInsetsListener(root, new HeaderInsetsListener(headerLayout));

        return root;
    }

    private void _searchBar_TextChanged(object? sender, Android.Text.TextChangedEventArgs e)
    {
        var NewText = e.Text?.ToString();
        var started = e.Start;
        var AfterCount = e.AfterCount;
        var BeforeCount = e.BeforeCount;
        MyViewModel.SearchSongForSearchResultHolder(NewText);
    }

    class HeaderInsetsListener : Java.Lang.Object, AndroidX.Core.View.IOnApplyWindowInsetsListener
    {
        private readonly View _header;
        public HeaderInsetsListener(View header) { _header = header; }

        public AndroidX.Core.View.WindowInsetsCompat OnApplyWindowInsets(View? v, AndroidX.Core.View.WindowInsetsCompat? insets)
        {
            var bars = insets.GetInsets(AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars());
            // Apply Top Padding to the Header Layout only
            _header.SetPadding(_header.PaddingLeft, bars.Top + AppUtil.DpToPx(10), _header.PaddingRight, _header.PaddingBottom);
            return insets;
        }
    }
    private void ShowFabMenu(Context ctx, View anchor)
    {
        var popup = new Android.Widget.PopupMenu(ctx, anchor);
        popup.Menu.Add(0, 1, 0, "Go to Settings");
        popup.Menu.Add(0, 2, 0, "Search Library (TQL)");
        popup.Menu.Add(0, 3, 0, "Scroll to Playing");
        popup.Menu.Add(0, 4, 0, "View Queue");

        popup.MenuItemClick += (s, e) =>
        {
            switch (e.Item.ItemId)
            {
                case 1: // Settings
                    if (Activity is TransitionActivity act)
                        act.NavigateTo(new SettingsFragment("sett", MyViewModel), "SettingsFragment");
                    break;
                case 2: // Search
                        //_searchBar?.RequestFocus();
                        //// Show Keyboard
                        //var imm = (InputMethodManager)ctx.GetSystemService(Context.InputMethodService);
                        //imm?.ShowSoftInput(_searchBar, ShowFlags.Implicit);
                    var searchSheet = new TqlSearchBottomSheet(MyViewModel);
                    searchSheet.Show(ParentFragmentManager, "TqlSearchSheet");
                    break;
                case 3: // Scroll To
                    MyViewModel.TriggerScrollToCurrentSong();
                    break;
                case 4: // Scroll To
                    var queueSheet = new QueueBottomSheetFragment(MyViewModel);
                    queueSheet.Show(ParentFragmentManager, "QueueSheet");
                    break;
            }
        };
        popup.Show();
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
        if (_searchBar.RequestFocus())
        {
            InputMethodManager? imm = Context!.GetSystemService(Context.InputMethodService) as InputMethodManager;
            if (imm is null) return;
            imm.ShowSoftInput(_searchBar, ShowFlags.Implicit);
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
            _view.ViewTreeObserver?.RemoveOnPreDrawListener(this);

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