using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

using Android.Views.InputMethods;

using AndroidX.CoordinatorLayout.Widget;
using AndroidX.RecyclerView.Widget;

using Dimmer.ViewsAndPages.NativeViews.DimmerLive;
using Dimmer.ViewsAndPages.NativeViews.Misc;
using Dimmer.ViewsAndPages.ViewUtils;
using Dimmer.WinUI.UiUtils;

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
    CoordinatorLayout? root;
    public TextView CurrentTimeTextView;
    public ExtendedFloatingActionButton? fab;
    private float dX, dY;
    public CoordinatorLayout? Root => root;
    public BaseViewModelAnd MyViewModel { get; private set; } = null!;
    private bool _isNavigating;
    private FabMorphMenu _morphMenu;
    private TextView _pageStatusText;
    private MaterialButton _prevBtn;
    private MaterialButton _nextBtn;
    private MaterialButton _jumpBtn;

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


    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context;

        // 1. Root: CoordinatorLayout (Crucial for FABs)
         root = new CoordinatorLayout(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        };

        root.SetBackgroundColor(UiBuilder.IsDark(ctx) ? Color.ParseColor("#0D0E20") : Color.ParseColor("#CAD3DA"));

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
        //_songListRecycler.AddOnScrollListener(new LoadMoreListener(MyViewModel));

        //var pagerView = CreatePaginationBar(MyViewModel,ctx);


        // Add Recycler to Content
        contentLinear.AddView(_songListRecycler);
        //contentLinear.AddView(pagerView);

        // Add Content to Root
        root.AddView(contentLinear);



        // --- 5. Extended FAB ---
        fab = new Google.Android.Material.FloatingActionButton.ExtendedFloatingActionButton(ctx);
        ;
        fab.Extended = false;
        fab.SetIconResource(Resource.Drawable.musicaba); 
        

        var fabParams = new CoordinatorLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
        fabParams.Gravity = (int)(GravityFlags.Bottom | GravityFlags.End);
        // Lift FAB above the MiniPlayer (approx 90dp)
        fabParams.SetMargins(0, 0, AppUtil.DpToPx(20), AppUtil.DpToPx(90));
        fab.LayoutParameters = fabParams;

        SetupSwipeableFab(fab);


        // Add FAB to Root
        root.AddView(fab);

        // --- 6. Handle Insets ---
        AndroidX.Core.View.ViewCompat.SetOnApplyWindowInsetsListener(root, new HeaderInsetsListener(headerLayout));

        return root;
    }
    private void SetupSwipeableFab(View view)
    {
        float startRawX = 0;
        float startRawY = 0;

        const int SwipeThreshold = 50;

        view.Touch += (s, e) =>
        {
            switch (e.Event?.Action)
            {
                

                case MotionEventActions.Down:
                    startRawX = e.Event.RawX;
                    startRawY = e.Event.RawY;
                    
                    break;

                case MotionEventActions.Move:

                    float curRawX = e.Event.RawX;
                    float curRawY = e.Event.RawY;

                    float deltaX = curRawX - startRawX;
                    float deltaY = curRawY - startRawY;


                    view.TranslationX = deltaX;
                    view.TranslationY = deltaY;
                    break;

                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                    // 1. Calculate final deltas
                    float finalDeltaX = view.TranslationX;
                    float finalDeltaY = view.TranslationY;

                    // 2. Check X-Axis Logic (Left/Right)
                    if (Math.Abs(finalDeltaX) > SwipeThreshold)
                    {
                        if (finalDeltaX > 0)
                        {

                            if (MyViewModel.CanGoNextSong)
                            {
                                Android.Widget.Toast.MakeText(Context, "Next Page >>", ToastLength.Short)?.Show();
                                MyViewModel?.NextSongPageCommand?.Execute(null);
                                fab?.Text = $"{MyViewModel?.CurrentSongPage} of {MyViewModel?.TotalSongPages}";
                                fab?.Extend();
                            }
                        }
                        else
                        {
                            if (MyViewModel.CanGoPrevSong)
                            {
                                Android.Widget.Toast.MakeText(Context, "<< Prev Page", ToastLength.Short)?.Show();
                                MyViewModel?.PrevSongPage();
                                fab?.Text = $"{MyViewModel?.CurrentSongPage} of {MyViewModel?.TotalSongPages}";
                                fab?.Extend();

                            }
                        }
                    }

                    if (Math.Abs(finalDeltaY) > SwipeThreshold)
                    {
                        if (finalDeltaY < 0)
                        {

                            var queueSheet = new QueueBottomSheetFragment(MyViewModel);
                            queueSheet.ScrollToSong();

                            queueSheet.Show(ParentFragmentManager, "QueueSheet");
                            
                        }
                        else
                        {

                            MyViewModel.JumpToCurrentSongPage();
                            //_songListRecycler.SmoothScrollToPosition(MyViewModel.songpo)
                        }
                    }


                    view.Animate()?
                        .TranslationX(0)
                        .TranslationY(0)
                        .SetDuration(300) 

                        .SetInterpolator(new Android.Views.Animations.OvershootInterpolator(1.0f))
                        .Start();

                    break;
            }
        };
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
        MyViewModel.PropertyChanged += ViewModel_PropertyChanged;

        _isNavigating = false;

    }
    public override void OnPause()
    {
        base.OnPause();
        MyViewModel.PropertyChanged -= ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MyViewModel.SongPageStatus) ||
            e.PropertyName == nameof(MyViewModel.CanGoNextSong) ||
            e.PropertyName == nameof(MyViewModel.CanGoPrevSong))
        {
            
        }
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
            _songListRecycler?.ScrollToPosition(currentlyPlayingIndex);

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
                    _songListRecycler.ScrollToPosition(index);

                    // Flash the item? (Requires access to ViewHolder, maybe for later)
                }
            }
        })
        .DisposeWith(CompositeDisposables);


        _morphMenu = new FabMorphMenu(Context, root, fab)
        .AddItem("Settings", Resource.Drawable.settings, () =>
        {
            // Logic copied from your old ShowFabMenu
            if (Activity is TransitionActivity act)
                act.NavigateTo(new SettingsFragment("sett", MyViewModel), "SettingsFragment");
        })
        .AddItem("Search (TQL)", Resource.Drawable.searchd, () =>
        {
            var searchSheet = new TqlSearchBottomSheet(MyViewModel);
            searchSheet.Show(ParentFragmentManager, "TqlSearchSheet");
        })
        .AddItem("Scroll to Playing", Resource.Drawable.eye, () =>
        {

            var songPos = MyViewModel.SearchResults.IndexOf(MyViewModel.CurrentPlayingSongView);
            _songListRecycler?.SmoothScrollToPosition(songPos);

            Toast.MakeText(Context, $"Scrolled To Song {MyViewModel.CurrentPlayingSongView.Title}", ToastLength.Short)?.Show();
        })
        .AddItem("View Queue", Resource.Drawable.playlistminimalistic3, () =>
        {
            var queueSheet = new QueueBottomSheetFragment(MyViewModel);
            queueSheet.Show(ParentFragmentManager, "QueueSheet");
        })
        .AddItem("Login", Resource.Drawable.user, () =>
        {
            MyViewModel.NavigateToAnyPageOfGivenType(this, new LoginFragment("IntoLogin", MyViewModel), "loginPageTag");
        });


        PostponeEnterTransition();
        _songListRecycler?.ViewTreeObserver?.AddOnPreDrawListener(new MyPreDrawListener(_songListRecycler, this));
   
    
    
    }
    private void ScrollToCurrent()
    {
        if (MyViewModel.CurrentPlayingSongView == null) return;

        // Since we are using the "queue" mode in adapter, we need to find the index in PlaybackQueue
        var index = MyViewModel.SearchResults.IndexOf(MyViewModel.CurrentPlayingSongView);

        if (index >= 0)
        {
            _songListRecycler?.SmoothScrollToPosition(index);
            Toast.MakeText(Context, "Scrolled to current song", ToastLength.Short)?.Show();
        }
    }
    private void Fab_LongClick(object? sender, View.LongClickEventArgs e)
    {
        ScrollToCurrent();
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
    
    private void CurrentTime_Click(object? sender, EventArgs e)
    {
        Android.Widget.Toast.MakeText(Context, "hey!", ToastLength.Short).Show();
    }



    public void NavToAlbumaPage(string transitionName)
    {
        if (!IsAdded || Activity == null) return;

    }

    class MyPreDrawListener : Java.Lang.Object, ViewTreeObserver.IOnPreDrawListener
    {
        private readonly RecyclerView _rv;
        private readonly Fragment _frag;
        public MyPreDrawListener(RecyclerView rv, Fragment frag) { _rv = rv; _frag = frag; }

        public bool OnPreDraw()
        {
            _rv?.ViewTreeObserver?.RemoveOnPreDrawListener(this);
            // Tell the framework the view is ready, start the animation
            _frag.StartPostponedEnterTransition();
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