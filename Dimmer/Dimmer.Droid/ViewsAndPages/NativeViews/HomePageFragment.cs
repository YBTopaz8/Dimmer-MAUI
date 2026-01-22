using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using Android.Text;
using Android.Views.InputMethods;
using AndroidX.CoordinatorLayout.Widget;
using AndroidX.Lifecycle;
using AndroidX.RecyclerView.Widget;
using CommunityToolkit.Maui.Views;
using Dimmer.DimmerSearch;
using Dimmer.UiUtils;
using Dimmer.Utilities;
using Dimmer.Utils;
using Dimmer.Utils.Extensions;
using Dimmer.ViewsAndPages.NativeViews.DimmerLive;
using Dimmer.ViewsAndPages.NativeViews.Misc;
using Dimmer.ViewsAndPages.ViewUtils;
using Google.Android.Material.Chip;
using Google.Android.Material.Loadingindicator;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Shapes;
using static Android.Webkit.WebSettings;
using static Dimmer.ViewsAndPages.NativeViews.SongAdapter;
using TextAlignment = Android.Views.TextAlignment;


namespace Dimmer.ViewsAndPages.NativeViews;

public partial class HomePageFragment : Fragment, IOnBackInvokedCallback
{
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

    public CoordinatorLayout? Root => root;
    public BaseViewModelAnd MyViewModel { get; private set; } = null!;
    private bool _isNavigating;
    private FabMorphMenu _morphMenu;

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
    private ImageView _backgroundImageView;

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context;

        // 1. Root: CoordinatorLayout (Crucial for FABs)
        root = new CoordinatorLayout(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        };

        root.SetBackgroundColor(UiBuilder.IsDark(ctx) ? Color.ParseColor("#0D0E20") : Color.ParseColor("#CAD3DA"));

        _backgroundImageView = new ImageView(ctx)
        {
            LayoutParameters = new FrameLayout.LayoutParams(-1, -1),

        };

        _backgroundImageView.SetScaleType(ImageView.ScaleType.CenterCrop);
       
        root.AddView(_backgroundImageView);
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
            Hint = "type to search",
            Background = null,
            TextSize = 14
        };
        _searchBar.SetTextColor(!UiBuilder.IsDark(root) ? Color.Black : Color.White);
        _searchBar.SetPadding(40, 30, 40, 30);
        
        _searchBar.TextChanged += _searchBar_TextChanged;
        
        _searchBar.FocusChange += (s, e) =>
        {
            var newFocus = e.HasFocus;
            if (!newFocus)
            {
                loadingIndic.Visibility = ViewStates.Gone;
                TQLChipHLayout.Visibility = ViewStates.Visible;
            }
            else
            {
                TQLChipHLayout.Visibility = ViewStates.Visible;
            }
        };


        searchCard.AddView(_searchBar);




        // Help Button
         QueueBtn = new Google.Android.Material.Button.MaterialButton(ctx, null, Resource.Attribute.materialIconButtonStyle)
        {
            Icon = AndroidX.Core.Content.ContextCompat.GetDrawable(ctx, Resource.Drawable.playlista),
            IconTint = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.DarkSlateBlue),
            LayoutParameters = new LinearLayout.LayoutParams(AppUtil.DpToPx(50), AppUtil.DpToPx(50))
        };
        QueueBtn.Click += async (s, e) =>
        {

            var queueSheet = new QueueBottomSheetFragment(MyViewModel,QueueBtn);
            queueSheet.Show(ParentFragmentManager, "QueueSheet");
            QueueBtn.Enabled = false;
            await Task.Delay(800);
            queueSheet.ScrollToSong();
        };
        QueueBtn.LongClickable = true;
        QueueBtn.LongClick += (s, e) =>
        {
            MyViewModel.CopyAllSongsInNowPlayingQueueToMainSearchResult();
            Toast.MakeText(ctx, "Copied Queue to Main UI", ToastLength.Short);
            QueueBtn.PerformHapticFeedback(FeedbackConstants.Confirm);
        };

        loadingIndic = new LoadingIndicator(ctx);
        loadingIndic.Visibility = ViewStates.Gone;
        loadingIndic.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.WrapContent, AppUtil.DpToPx(80));

        var mtlChip = new Chip(ctx);
        var lyParam = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);

        mtlChip.SetChipIconResource(Resource.Drawable.searchd);

        // Add items to Header
        headerLayout.AddView(menuBtn);
        headerLayout.AddView(searchCard);
        headerLayout.AddView(QueueBtn);



        var bottomLayout = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal
        };
        bottomLayout.SetGravity(GravityFlags.CenterHorizontal);

        bottomLayout.SetPadding(20, 20, 20, 20);

        songsTotal = new TextView(ctx, null, Resource.Attribute.titleTextStyle);
        Color col;
        if (UiBuilder.IsDark(root))
        {
            col = Color.White;
        }
        else
        {
            col = Color.Black;
        }

        songsTotal.SetTextColor(col);
        songsTotal.TextAlignment = TextAlignment.TextStart;



        TqlLine = new TextView(ctx, null, Resource.Attribute.titleTextStyle);
        TqlLine.Text = "test";
        TqlLine.TextSize = 14;

        TqlLine.SetTextColor(col);

        

        bottomLayout.AddView(loadingIndic);

        bottomLayout.AddView(songsTotal);




        // Add Header to Content
        contentLinear.AddView(headerLayout);
        contentLinear.AddView(bottomLayout);
 
        var lastLayout = new LinearLayout(ctx);

        lastLayout.SetGravity(GravityFlags.CenterHorizontal);
        lastLayout.AddView(TqlLine);
        
        TQLChipHLayout = new LinearLayout(ctx);
        TQLChipHLayout.Orientation = Android.Widget.Orientation.Horizontal;
        TQLChipHLayout.SetGravity(GravityFlags.CenterHorizontal);
        var FirstTQLChip = new Chip(ctx);

        FirstTQLChip.SetChipIconResource(Resource.Drawable.heart)
            ; FirstTQLChip.Text = "My Fav";

        FirstTQLChip.Click += (s, e) =>
        {
            _searchBar.Text = FirstTQLChip.Text;
            
        };
        var SecondTQLChip = new Chip(ctx);
        SecondTQLChip.SetChipIconResource(Resource.Drawable.sortbytime);
        SecondTQLChip.Click += (S, e) =>
        {
            _searchBar.Text = _searchBar.Text +" desc added";
        };

        TQLChipHLayout.AddView(FirstTQLChip);
        TQLChipHLayout.AddView(SecondTQLChip);
        TQLChipHLayout.Visibility = ViewStates.Gone;

        contentLinear.AddView(lastLayout);
        contentLinear.AddView(TQLChipHLayout);

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
        var vLinearLayout = new LinearLayout(ctx);



        fab = new Google.Android.Material.FloatingActionButton.ExtendedFloatingActionButton(ctx);
        ;
        fab.Extended = true;

        fab.SetBackgroundColor(UiBuilder.IsDark(ctx) ? Color.ParseColor("#1a1a1a") : Color.ParseColor("#DEDFF0"));
        fab.SetIconResource(Resource.Drawable.musicaba); 
        

        var fabParams = new CoordinatorLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
        fabParams.Gravity = (int)(GravityFlags.Bottom | GravityFlags.End);
        // Lift FAB above the MiniPlayer (approx 90dp)
        fabParams.SetMargins(0, 0, AppUtil.DpToPx(40), AppUtil.DpToPx(90));
        fab.LayoutParameters = fabParams;

        SetupClickableFab(fab);


        // Add FAB to Root
        root.AddView(fab);

        // --- 6. Handle Insets ---
        AndroidX.Core.View.ViewCompat.SetOnApplyWindowInsetsListener(root, new HeaderInsetsListener(headerLayout));

        return root;
    }
    private void SetupClickableFab(View view)
    {
      
        view.Click += (s, e) =>
        {

            if (MyViewModel.CurrentPlayingSongView == null) return;
            var requestedSong = MyViewModel.CurrentPlayingSongView;

            // Since we are using the "queue" mode in adapter, we need to find the index in PlaybackQueue
            var index = MyViewModel.SearchResults.IndexOf(requestedSong);
            if (index == -1) return;
            view.PerformHapticFeedback(FeedbackConstants.LongPress);
            _songListRecycler?.ScrollToPosition(index);

            var specificView = _songListRecycler.FindViewHolderForAdapterPosition(index);

            var typew = specificView.GetType();
            var type = specificView.ItemViewType.GetType();
            if (type == null) return;
            if(type == typeof(CardView))
            {
                var specificCard = specificView.ItemView as CardView;
                specificCard?.SetBackgroundColor(Color.DarkSlateBlue);
            }
            Debug.WriteLine(type);
        };
        view.LongClickable = true;
        view.LongClick += (s, e)
            =>
        {
            _searchBar.RequestFocusFromTouch();
        };
    }





    private void _searchBar_TextChanged(object? sender, Android.Text.TextChangedEventArgs e)
    {
        var NewText = e.Text?.ToString();
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
       
        _isNavigating = false;


        MyViewModel.WhenPropertyChange(nameof(MyViewModel.CurrentPlayingSongView), newVl => MyViewModel.CurrentPlayingSongView)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(async currSong =>
            {
                var art = currSong.Artist;
                var alb= currSong.Album;
                var artImgPath = art?.ImagePath;
                var albImgPath = alb?.ImagePath;
                if (MyViewModel.CurrentPlayingSongView.CoverImagePath is not null)
                {

                    //if(UiBuilder.IsDark(this.View))
                    //{

                    //    await _backgroundImageView.SetImageWithStringPathViaGlideAndFilterEffect(MyViewModel.CurrentPlayingSongView.CoverImagePath,
                    //         Utilities.FilterType.DarkAcrylic);
                        
                    //}
                    //else
                    //{
                    //    await _backgroundImageView.SetImageWithStringPathViaGlideAndFilterEffect(MyViewModel.CurrentPlayingSongView.CoverImagePath,
                    //         Utilities.FilterType.Glassy);
                    //}
                }
                //currSong.IsCurrentPlayingHighlight= true; 
            });

        MyViewModel.WhenPropertyChange(nameof(MyViewModel.IsLibraryEmpty), newVl => MyViewModel.IsLibraryEmpty)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(count =>
            {
            });

        MyViewModel.WhenPropertyChange(nameof(MyViewModel.CurrentTqlQueryUI), newVl => MyViewModel.CurrentTqlQueryUI)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(tql =>
            {
                if (string.IsNullOrEmpty(tql))
                {
                    TqlLine.Animate()?.Alpha(0).SetDuration(300);
                    TqlLine.Visibility = ViewStates.Gone;
                }
                else
                {
                    TqlLine.Text = tql;
                    TqlLine.Visibility = ViewStates.Visible;

                    TqlLine.Animate()?.Alpha(1).SetDuration(150);
                }
                //currentTql.Text = tql;
            });


        MyViewModel.WhenPropertyChange(nameof(MyViewModel.PlaybackQueue), newVl => MyViewModel.PlaybackQueue)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(pbQueue =>
            {
                if(pbQueue is not null)
                    QueueBtn.TooltipText = $"{MyViewModel.PlaybackQueue.Count} Songs in Queue";
            });


                MyViewModel.WhenPropertyChange(nameof(MyViewModel.IsTqlBusy), newVl => MyViewModel.IsTqlBusy)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(isBusy =>
            {
                switch (isBusy)
                {
                    case true:

                        loadingIndic.Visibility = ViewStates.Visible;
                        break;

                    default:
                        var songCount = MyViewModel.SearchResults.Count;
                        if (songCount > 1)
                        {
                            songsTotal.Text = $"{MyViewModel.SearchResults.Count} Songs";
                        }
                        else if (songCount == 1)
                        {
                            songsTotal.Text = $"1 Song";

                        }
                        else if (songCount < 1)
                        {

                            songsTotal.Text = $"No Song";
                        }
                        loadingIndic.Visibility = ViewStates.Gone;
                        break;
                }
            });

    }
    public override void OnPause()
    {
        base.OnPause();
        MyViewModel.PropertyChanged -= ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        //if (e.PropertyName == nameof(MyViewModel.SongPageStatus) ||
        //    e.PropertyName == nameof(MyViewModel.CanGoNextSong) ||
        //    e.PropertyName == nameof(MyViewModel.CanGoPrevSong))
        //{
            
        //}
    }

    public override void OnViewCreated(View view, Bundle? savedInstanceState)
    {
        base.OnViewCreated(view, savedInstanceState);
        //PostponeEnterTransition();


        //view.ViewTreeObserver.AddOnPreDrawListener(new MyPreDrawListener(this, view));

        _isNavigating = false;
        MyViewModel.CurrentFragment = this;

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


        
        PostponeEnterTransition();
        _songListRecycler?.ViewTreeObserver?.AddOnPreDrawListener(new MyPreDrawListener(_songListRecycler, this));
   
    
    
    }
    public void ScrollToCurrent()
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
   

    protected CompositeDisposable CompositeDisposables { get; } = new CompositeDisposable();
    public LoadingIndicator loadingIndic { get; private set; }
    public TextView songsTotal { get; private set; }
    public TextView currentTql { get; private set; }
    public Button QueueBtn { get; private set; }
    public LinearLayout TQLChipHLayout { get; private set; }
    public TextView TqlLine { get; private set; }

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
        MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.PresetQueries.DescAdded());
        RxSchedulers.UI.ScheduleTo(()=> Toast.MakeText(Context!, "Reset TQL", ToastLength.Short)?.Show());
    }
}