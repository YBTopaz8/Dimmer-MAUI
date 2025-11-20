




using Google.Android.Material.Transition;

using Kotlin.Text;

using MongoDB.Bson;

namespace Dimmer.ViewsAndPages.NativeViews;

public partial class HomePageFragment : Fragment, IOnBackInvokedCallback
{
    private RecyclerView _songListRecycler = null!;
    private TextView _emptyLabel = null!;
    private LinearLayout _bottomBar = null!;
    private TextView _titleTxt = null!;
    private TextView _albumTxt = null!;
    private TextView _currentTime = null!;
    private TextView _playCount = null!;
    private ImageView _albumArt = null!;
    private float _downX;
    private float _downY;
    private FloatingActionButton _pageFAB = null!;
    FrameLayout? root;
    TextView currentTimeTextView;
    public FrameLayout? Root => root;
    public BaseViewModelAnd MyViewModel { get; private set; } = null!;
    private bool _isNavigating;

    public HomePageFragment(BaseViewModelAnd myViewModel)
    {
        MyViewModel = myViewModel;
    }
    private CancellationTokenSource? _searchCts;
    private SongAdapter _adapter;
    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        PostponeEnterTransition();
        var ctx = Context!;


        // ROOT FRAME (needed for FAB overlay)
         root = new FrameLayout(ctx)
        {
            LayoutParameters = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };

        // MAIN COLUMN
        var column = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };

        // SEARCHBAR (top)
        var searchBar = new EditText(ctx)
        {
            Hint = "Search songs, artists, albums...",
            TextSize = 16f
        };
        
        searchBar.SetPadding(40, 30, 40, 30);

        searchBar.TextChanged += (s, e) =>
        {
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();

            var text = e?.Text?.ToString()?.Trim() ?? "";

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(250, _searchCts.Token); // debounce

                    MyViewModel.SearchSongForSearchResultHolder(text); // implement this in VM

                    Activity?.RunOnUiThread(() =>
                    {
                        //_adapter.UpdateData(results);
                        //emptyText.Visibility = results.Count == 0 ? ViewStates.Visible : ViewStates.Gone;
                    });
                }
                catch (TaskCanceledException) { }
            });
        };



        var searchBorder = new FrameLayout(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                (int)(ctx.Resources.DisplayMetrics.Density * 60))
        };

        searchBorder.AddView(searchBar);

        // MIDDLE ZONE (RecyclerView + empty text)
        var middleContainer = new FrameLayout(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                0,
                1f)  // weight fills all remaining space
        };

        _songListRecycler = new RecyclerView(ctx)
        {
            LayoutParameters = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };

        _songListRecycler.SetLayoutManager(new LinearLayoutManager(ctx));

        var emptyText = new TextView(ctx)
        {
            Text = "No songs found",
            Gravity = Android.Views.GravityFlags.Center,
            TextSize = 16f,
            Visibility = ViewStates.Gone
        };
        var scrListener = new RecyclerViewOnScrollListener(
            (dy) =>
            {
                Console.WriteLine($"Scrolled by {dy} pixels");
                // Handle scroll events here
            });
        _adapter = new SongAdapter(ctx, MyViewModel, MyViewModel.SearchResults);
        _songListRecycler.SetAdapter(_adapter);
        _songListRecycler.AddOnScrollListener(scrListener);
        middleContainer.AddView(_songListRecycler);
        middleContainer.AddView(emptyText);

        // BOTTOM BAR
        var btmBar = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                (int)(ctx.Resources.DisplayMetrics.Density * 90)) // 50-60dp
        };
        btmBar.SetGravity(GravityFlags.CenterVertical);
        btmBar.SetBackgroundColor(Color.ParseColor("#303030"));

        
        // SAMPLE bottom bar content
        
         _albumArt = new ImageView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(
                (int)(ctx.Resources.DisplayMetrics.Density * 60),
                (int)(ctx.Resources.DisplayMetrics.Density * 50))
        };
        _albumArt.SetImageResource(Android.Resource.Drawable.IcMediaPlay);

        _albumArt.TransitionName = "homePageFAB";

        var textStack = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new LinearLayout.LayoutParams(
                0,
                ViewGroup.LayoutParams.WrapContent,
                1f)
        };

        var title = new TextView(ctx) { Text = "Song Title", TextSize = 19f };
        var album = new TextView(ctx) { Text = "Album", TextSize = 14f };

        textStack.AddView(title);
        textStack.AddView(album);

        var rightStack = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical
        };

        currentTimeTextView = new TextView(ctx)
        {
            Text = "0:00",
            TextSize = 12f
        };
        currentTimeTextView.Click += CurrentTime_Click;
        

        var playCount = new TextView(ctx)
        {
            Text = "Plays: 0",
            TextSize = 12f
        };

        rightStack.AddView(currentTimeTextView);
        rightStack.AddView(playCount);

        btmBar.AddView(_albumArt);
        btmBar.AddView(textStack);
        btmBar.AddView(rightStack);

        // ADD TOP + MIDDLE + BOTTOM TO COLUMN
        column.AddView(searchBorder);
        column.AddView(middleContainer);
        column.AddView(btmBar);

        // ADD COLUMN TO ROOT
        root.AddView(column);

        // FAB
        _pageFAB = new FloatingActionButton(ctx)
        {
            LayoutParameters = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent,
                GravityFlags.Bottom | GravityFlags.End)
        };
        int fabMargin = (int)(ctx.Resources.DisplayMetrics.Density * 30);
        int fabMarginBottom = (int)(ctx.Resources.DisplayMetrics.Density * 70);
        ((FrameLayout.LayoutParams)_pageFAB.LayoutParameters).SetMargins(fabMargin, fabMargin, fabMargin, fabMarginBottom);
        _pageFAB.Click += PageFAB_Click;
        
        ColorStateList colorStateList = new ColorStateList(
            new int[][] {
                new int[] { } // default
            },
            new int[] {
                Color.White,
                Color.DarkSlateBlue,
                Color.Black
            }

        );

        _pageFAB.SetRippleColor(colorStateList);
        
        _pageFAB.SetBackgroundColor(Color.DarkSlateBlue);
        _pageFAB.SetImageResource(Android.Resource.Drawable.IcMediaPlay);

        root.AddView(_pageFAB);

        
        return root;
    }

    private void PageFAB_Click(object? sender, EventArgs e)
    {
        if (_isNavigating || !IsAdded) return;

        _isNavigating = true;
        Toast.MakeText(Context!, "Play/Pause clicked!", ToastLength.Short)?.Show();
        NavToAlbumaPage(_albumArt.TransitionName);
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
            else if(newState == RecyclerView.ScrollStateDragging)
            {
                // pause image loading
            }
            else if(newState == RecyclerView.ScrollStateSettling)
            {
                // pause image loading
            }
            _scrollStateChanged(newState);


                base.OnScrollStateChanged(rv, newState);
        }
    }

    private void NavToAlbumaPage(string transitionName)
    {
        if (!IsAdded || Activity == null) return;

        ArtistDetailFragment fragment = new ArtistDetailFragment(transitionName);


        var mcTAnim = new MaterialContainerTransform
        {
            DrawingViewId = TransitionActivity.MyStaticID,  // container for fragments
            ScrimColor = Color.Transparent,
            ContainerColor = Color.Transparent,
            FadeMode = MaterialContainerTransform.FadeModeThrough,
            StartShapeAppearanceModel = ShapeAppearanceModel.InvokeBuilder().SetAllCorners(CornerFamily.Rounded, 50f).Build(),
            EndShapeAppearanceModel = ShapeAppearanceModel.InvokeBuilder().SetAllCorners(CornerFamily.Rounded, 0f).Build(),
        };
        mcTAnim.PathMotion = new MaterialArcMotion();
        mcTAnim.SetDuration(380)
            .SetInterpolator(PublicStats.DecelerateInterpolator);
        
        _pageFAB?.Animate()?
        .Alpha(0f)
        .SetDuration(mcTAnim.Duration)
        .Start();
        

        fragment.SharedElementEnterTransition = mcTAnim;
        fragment.SharedElementReturnTransition = mcTAnim.Clone();

        var nonSharedEnterAnim= new Google.Android.Material.Transition.MaterialFadeThrough
        {
        
        };
        nonSharedEnterAnim.SetDuration(200);
        fragment.EnterTransition = nonSharedEnterAnim;

        var nonShareExitAnim = new Google.Android.Material.Transition.MaterialFadeThrough
        {
        };
        nonShareExitAnim.SetDuration(180);
        fragment.ExitTransition = nonShareExitAnim;




        Hold enterHold = new Hold();
        enterHold.AddTarget(TransitionActivity.MyStaticID);
        enterHold.SetDuration(mcTAnim.Duration);
        ParentFragment?.ExitTransition = enterHold;

        if (_albumArt is not null)
        {
            ParentFragmentManager.BeginTransaction()
                .AddSharedElement(_albumArt, transitionName)
                .Replace(TransitionActivity.MyStaticID, fragment)
                .AddToBackStack(null)
                .Commit();
        }
    }


    public override void OnResume()
    {
        base.OnResume();
        if (_songListRecycler is not null)
        {
            for (int i = 0; i < _songListRecycler.ChildCount; i++)
            {
                var child = _songListRecycler.GetChildAt(i);
                if (child != null)
                {
                    var vh = _songListRecycler.GetChildViewHolder(child);
                    if (vh is null) return;
                    vh.IsRecyclable = true;
                }
            }
        }
        _pageFAB?.Animate()?.Alpha(1f)?.SetDuration(0)?.Start();
        _isNavigating = false;
    }
    public override void OnViewCreated(View view, Bundle? savedInstanceState)
    {
        base.OnViewCreated(view, savedInstanceState);
        view?.ViewTreeObserver?.GlobalLayout += HomePageFragment_GlobalLayout;

        _pageFAB.Alpha = 1f;
        _isNavigating = false;
    }

    private void HomePageFragment_GlobalLayout(object? sender, EventArgs e)
    {
         StartPostponedEnterTransition();
    }

    public override void OnDestroyView()
    {
        base.OnDestroyView();
        _isNavigating = false;
        _searchCts?.Cancel();
        currentTimeTextView.Click -= CurrentTime_Click;
        _pageFAB.Click -= PageFAB_Click;
        View?.ViewTreeObserver?.GlobalLayout -= HomePageFragment_GlobalLayout;
        _songListRecycler.SetAdapter(null);

    }
    private bool IsDark()
    {
        return (Resources?.Configuration?.UiMode & Android.Content.Res.UiMode.NightYes) != 0;
    }

    public void OnBackInvoked()
    {
        Toast.MakeText(Context!, "Back invoked in HomePageFragment", ToastLength.Short)?.Show();
    }
}