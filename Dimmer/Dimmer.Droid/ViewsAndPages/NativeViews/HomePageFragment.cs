using System.Threading.Tasks;

using Dimmer.Utilities.Extensions;
using BitmapFactory = Android.Graphics.BitmapFactory;
using static Dimmer.ViewsAndPages.NativeViews.SongAdapter;

namespace Dimmer.ViewsAndPages.NativeViews;

public partial class HomePageFragment : Fragment, IOnBackInvokedCallback
{

    string toSettingsTrans = "homePageFAB";

    public RecyclerView? _songListRecycler = null!;
    public TextView _emptyLabel = null!;
    public LinearLayout _bottomBar = null!;
    public TextView _titleTxt = null!;
    public TextView _albumTxt = null!;
    public TextView _artistTxt = null!;
    public TextView _playCount = null!;
    public ImageView _albumArt = null!;
    public float _downX;
    public float _downY;
    public FloatingActionButton _pageFAB = null!;
    public FloatingActionButton? cogButton = null!;
    FrameLayout? root;
    public TextView CurrentTimeTextView;
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
        
        var recyclerLayoutManager = new LinearLayoutManager(ctx);
        
        _songListRecycler.SetLayoutManager(recyclerLayoutManager);
        
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
                
                // Handle scroll events here
            });
        _adapter = new SongAdapter(ctx, MyViewModel, this);
        _songListRecycler.SetAdapter(_adapter);
        _songListRecycler.AddOnScrollListener(scrListener);

        
            var touch = new TouchListener(Context, _songListRecycler);



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

        
        
         _albumArt = new ImageView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(
                (int)(ctx.Resources.DisplayMetrics.Density * 80),
                (int)(ctx.Resources.DisplayMetrics.Density * 70))
        };
        _albumArt.Click += AlbumArt_Click;
          // handle image
        if (!string.IsNullOrEmpty(MyViewModel.CurrentPlayingSongView.CoverImagePath) && System.IO.File.Exists(MyViewModel.CurrentPlayingSongView.CoverImagePath))
        {
            // Load from disk
            var bmp = Android.Graphics.BitmapFactory.DecodeFile(MyViewModel.CurrentPlayingSongView.CoverImagePath);

            _albumArt.SetImageBitmap(bmp);
        }

        //_albumArt.TransitionName = "homePageFAB";

        var textStack = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new LinearLayout.LayoutParams(
                0,
                ViewGroup.LayoutParams.WrapContent,
                1f)
        };

        _titleTxt = new TextView(ctx) { Text = MyViewModel.CurrentPlayingSongView.Title, TextSize = 19f };
        _albumTxt = new TextView(ctx) { Text = MyViewModel.CurrentPlayingSongView.AlbumName, TextSize = 10f };
        _artistTxt = new TextView(ctx) { Text = MyViewModel.CurrentPlayingSongView.ArtistName, TextSize = 14f };

        textStack.AddView(_titleTxt);
        textStack.AddView(_artistTxt);
        textStack.AddView(_albumTxt);

        var rightStack = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical
        };

        CurrentTimeTextView = new TextView(ctx)
        {
            Text = MyViewModel.CurrentTrackDurationSeconds.ToString(),
            TextSize = 12f
        };
        CurrentTimeTextView.Click += CurrentTime_Click;
        

        var playCount = new TextView(ctx)
        {
            Text = $"Plays: {MyViewModel.CurrentPlayingSongView.PlayCompletedCount}",
            TextSize = 12f
        };

        rightStack.AddView(CurrentTimeTextView);
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

         cogButton = new FloatingActionButton(ctx)
        {
            LayoutParameters = new FrameLayout.LayoutParams(
        ViewGroup.LayoutParams.WrapContent,
        ViewGroup.LayoutParams.WrapContent,
        GravityFlags.Bottom | GravityFlags.End)
        };


        cogButton.TransitionName = "SongDetailsTrans";
        int cogMarginRight = fabMargin + (int)(ctx.Resources.DisplayMetrics.Density * 80); // spacing left of FAB
        ((FrameLayout.LayoutParams)cogButton.LayoutParameters).SetMargins(fabMargin, fabMargin, cogMarginRight, fabMarginBottom);
        cogButton.SetImageResource(Resource.Drawable.settings); // simple cog icon
        cogButton.SetBackgroundColor(Color.Gray);
        cogButton.Click += (s, e) =>
        {
            if (!IsAdded || _isNavigating) return;
            _isNavigating = true;

            var fragment = new SettingsFragment("SettingsTrans", MyViewModel);

            ParentFragmentManager.BeginTransaction()
                .Replace(TransitionActivity.MyStaticID, fragment )
                .AddToBackStack(null)
                .Commit();
        };



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
        root.AddView(cogButton);

        return root;
    }

    private void AlbumArt_Click(object? sender, EventArgs e)
    {
        MyViewModel.NavigateToNowPlayingFragmentFromHome
            (this, _albumArt,
            _titleTxt,_artistTxt,
            _albumTxt);
    }

    private async void Touch_SingleTap(int pos, View arg2, SongModelView song)
    {

        if (song != null)
        {
            await MyViewModel.PlaySong(song,CurrentPage.AllSongs,MyViewModel.SearchResults);
            _adapter.NotifyDataSetChanged();
        }
        Toast.MakeText(Context, $"Single tap {pos}", ToastLength.Short)?.Show();
    }

   
    private void PageFAB_Click(object? sender, EventArgs e)
    {
        if (_isNavigating || !IsAdded) return;

        _isNavigating = true;
        Toast.MakeText(Context!, "Play/Pause clicked!", ToastLength.Short)?.Show();
        NavToAlbumaPage(toSettingsTrans);
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

    public void NavToAlbumaPage(string transitionName)
    {
        if (!IsAdded || Activity == null) return;

        MyViewModel.NavigateToSingleSongPageFromHome(
            this,
            transitionName,_albumArt);
    }


    public override void OnResume()
    {
        base.OnResume();
        
        _pageFAB?.Animate()?.Alpha(1f)?.SetDuration(0)?.Start();
        _isNavigating = false;
    }
    public override void OnViewCreated(View view, Bundle? savedInstanceState)
    {
        base.OnViewCreated(view, savedInstanceState);
        view?.ViewTreeObserver?.GlobalLayout += HomePageFragment_GlobalLayout;

        _pageFAB.Alpha = 1f;
        _isNavigating = false;
        MyViewModel.CurrentPage = this;
        MyViewModel.SetupSubscriptions();
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
        CurrentTimeTextView.Click -= CurrentTime_Click;
        _pageFAB.Click -= PageFAB_Click;
        View?.ViewTreeObserver?.GlobalLayout -= HomePageFragment_GlobalLayout;
        _songListRecycler?    .SetAdapter(null);
        _albumArt.Click -= AlbumArt_Click;
        _songListRecycler = null;

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