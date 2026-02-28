namespace Dimmer.ViewsAndPages.NativeViews.Adapters;

internal partial class HomePageAdapter : RecyclerView.Adapter, IDisposable
{
    private readonly BehaviorSubject<AdapterMode> _mode = new(AdapterMode.Normal);
    public IObservable<AdapterMode> Mode => _mode.AsObservable();

    readonly HashSet<int> _selectedPositions = new HashSet<int>();
    public IObservable<Unit> SelectionChanged => _selectionChanged.AsObservable();
    public Subject<Unit> _selectionChanged => new();
    public List<SongModelView> Songs => _localSongs;
    private readonly List<SongModelView> _localSongs = new();

    public override int ItemCount => _localSongs.Count;
    public SongModelView GetItem(int position) => _localSongs[position];

    private readonly BehaviorSubject<bool> _isSourceCleared = new(true);
    private readonly BehaviorSubject<bool> _isAdapterReady = new(false);
    public IObservable<bool> IsSourceCleared => _isSourceCleared.AsObservable();
    public IObservable<bool> IsAdapterReady => _isAdapterReady.AsObservable();

    public static Action<View, string, int>? AdapterCallbacks;
    private Context ctx;
    public BaseViewModelAnd MyViewModel;
    private readonly IDimmerAudioService _audioService;
    private Fragment ParentFragement;

    private int _expandedPosition = -1;
    private int _previousExpandedPosition = -1;
    private readonly CompositeDisposable _disposables = new();
    private bool _isDisposed;
    private CancellationTokenSource _diffCts = new();

    public HomePageAdapter(Context ctx, BaseViewModelAnd vm, Fragment pFragment)
    {
        Debug.WriteLine(DateTime.Now + "starting adapter");

        Debug.WriteLine(DateTime.Now.TimeOfDay.Seconds + "starting adapter ctor");
        Debug.WriteLine(DateTime.Now.TimeOfDay.Microseconds + "starting adapter ctor");
        Debug.WriteLine(DateTime.Now.TimeOfDay.Milliseconds + "starting adapter ctor");
        MyViewModel = vm;
        _audioService = vm.AudioService;
        ParentFragement = pFragment;
        SetupReactivePipeline(vm);
    }

    private void SetupReactivePipeline(BaseViewModelAnd viewModel)
    {
        Debug.WriteLine(DateTime.Now + " starting adapter2");

        IObservable<IChangeSet<SongModelView>> sourceStream = viewModel.SearchResultsHolder.Connect();

        sourceStream
      .ObserveOn(RxSchedulers.UI) // <--- Stay on UI thread for instant response
      .Subscribe(changes =>
      {
          if (_isDisposed) return;

          // 1. Snapshot the items from the holder instantly
          var newList = viewModel.SearchResultsHolder.Items.ToList();

          // 2. Clear and Swap
          _localSongs.Clear();
          _localSongs.AddRange(newList);

          // 3. Instant redraw without the Task.Run/DiffUtil overhead
          NotifyDataSetChanged();

          if (!_isAdapterReady.Value) _isAdapterReady.OnNext(true);
          _isSourceCleared.OnNext(_localSongs.Count == 0);
      })
      .DisposeWith(_disposables);
        Observable.FromEventPattern<PlaybackEventArgs>(
                h => _audioService.PlaybackStateChanged += h,
                h => _audioService.PlaybackStateChanged -= h)
            .Select(evt => evt.EventArgs)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(x => HandlePlaybackStateChange(x))
            .DisposeWith(_disposables);
    }
    private void HandlePlaybackStateChange(PlaybackEventArgs x) { }

    public new void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _disposables.Dispose();
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _disposables.Dispose();
        base.Dispose(disposing);
    }

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        if (holder is SongViewHolder songHolder)
        {
            var song = Songs[position];
            bool isExpanded = position == _expandedPosition;
            songHolder.Bind(song, isExpanded, ToggleExpand);
        }
    }

    private void ToggleExpand(int position)
    {
        _previousExpandedPosition = _expandedPosition;
        _expandedPosition = _expandedPosition == position ? -1 : position;

        if (_previousExpandedPosition != -1) NotifyItemChanged(_previousExpandedPosition);
        if (_expandedPosition != -1) NotifyItemChanged(_expandedPosition);
    }

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        if (ctx is null && parent.Context is not null) ctx = parent.Context;

        var card = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(12),
            CardElevation = AppUtil.DpToPx(0),
            StrokeWidth = AppUtil.DpToPx(0),
            StrokeColor = Color.Transparent,
            Clickable = true,
            Focusable = true,
            LayoutParameters = new RecyclerView.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent) { TopMargin = 8, BottomMargin = 8 }
        };

        var mainContainer = new LinearLayout(ctx) { Orientation = Orientation.Vertical, Clickable = true, LayoutTransition = new LayoutTransition() };
        mainContainer.LayoutParameters = new ViewGroup.LayoutParams(-1, -2);

        // TOP ROW
        var topRow = new LinearLayout(ctx) { Orientation = Orientation.Horizontal, Clickable = true };
        topRow.SetGravity(GravityFlags.CenterVertical);
        topRow.SetPadding(20, 20, 20, 20);

        var imgCard = new MaterialCardView(ctx) { Radius = AppUtil.DpToPx(8), CardElevation = 0 };
        var imgView = new ImageView(ctx);
        imgView.SetScaleType(ImageView.ScaleType.CenterCrop);
        imgCard.AddView(imgView, new ViewGroup.LayoutParams(AppUtil.DpToPx(56), AppUtil.DpToPx(56)));
        topRow.AddView(imgCard);

        var textLayout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        var txtLp = new LinearLayout.LayoutParams(0, -2, 1f) { LeftMargin = AppUtil.DpToPx(16) };
        textLayout.LayoutParameters = txtLp;

        var title = new MaterialTextView(ctx) { TextSize = 16, Typeface = Typeface.DefaultBold, Ellipsize = TextUtils.TruncateAt.End };
        title.SetMaxLines(1);
        var artist = new TextView(ctx) { TextSize = 14, Selected = true, Ellipsize = TextUtils.TruncateAt.Marquee };
        artist.SetTextColor(Color.Gray);

        textLayout.AddView(title);
        textLayout.AddView(artist);
        topRow.AddView(textLayout);

        var moreBtn = new MaterialButton(ctx, null, Resource.Attribute.materialIconButtonStyle);
        moreBtn.SetIconResource(Resource.Drawable.more1);
        moreBtn.IconTint = Android.Content.Res.ColorStateList.ValueOf(Color.Gray);

        var durationView = new TextView(ctx) { TextSize = 10, Typeface = Typeface.DefaultBold, Gravity = GravityFlags.CenterHorizontal };
        durationView.SetTextColor(Color.Gray);

        var playCountView = new TextView(ctx) { TextSize = 10, Typeface = Typeface.DefaultBold, Gravity = GravityFlags.CenterHorizontal };
        durationView.SetTextColor(Color.Gray);

        var rightLayout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        rightLayout.AddView(moreBtn);
        rightLayout.AddView(durationView);
        //rightLayout.AddView(playCountView);
        topRow.AddView(rightLayout);

        // EXPANDABLE ROW 1
        var expandRow = new LinearLayout(ctx) { Orientation = Orientation.Horizontal, Visibility = ViewStates.Gone };
        expandRow.SetGravity(GravityFlags.Center);
        expandRow.SetPadding(0, 0, 0, 20);

        var favBtn = CreateActionButton("Fav", Resource.Drawable.heart);
        var statsBtn = CreateActionButton("Stats", Resource.Drawable.stats);
        var lyricsBtn = CreateActionButton("Note", Resource.Drawable.pen);
        var infoBtn = CreateActionButton("Info", Resource.Drawable.infocircle);

        expandRow.AddView(favBtn);
        expandRow.AddView(statsBtn);
        expandRow.AddView(lyricsBtn);
        expandRow.AddView(infoBtn);

        // EXPANDABLE ROW 2
        var expandRowTwo = new LinearLayout(ctx) { Orientation = Orientation.Vertical, Visibility = ViewStates.Gone };
        expandRowTwo.SetGravity(GravityFlags.Center);
        expandRowTwo.SetPadding(0, 0, 0, 20);

        expandRowTwo.AddView(new ChipGroup(ctx));
        expandRowTwo.AddView(CreateActionButton("album", Resource.Drawable.musicalbum));

        mainContainer.AddView(topRow);
        mainContainer.AddView(expandRow);
        mainContainer.AddView(expandRowTwo);
        card.AddView(mainContainer);

        return new SongViewHolder(MyViewModel, ParentFragement, card, imgView, title, artist, moreBtn, durationView, playCountView, expandRow,expandRowTwo, favBtn, lyricsBtn, infoBtn, statsBtn, topRow);
    }

    private MaterialButton CreateActionButton(string text, int iconId)
    {
        var btn = new MaterialButton(ctx, null, Resource.Attribute.materialButtonOutlinedStyle);
        btn.Text = text;
        btn.SetTextColor(UiBuilder.IsDark(btn) ? Color.Gray : Color.ParseColor("#294159"));
        btn.SetIconResource(iconId);
        btn.IconTint = Android.Content.Res.ColorStateList.ValueOf(UiBuilder.IsDark(btn) ? Color.Gray : Color.ParseColor("#294159"));
        btn.SetPadding(30, 0, 30, 0);
        var lp = new LinearLayout.LayoutParams(-2, -2);
        lp.RightMargin = 10;
        btn.LayoutParameters = lp;
        btn.IconSize = AppUtil.DpToPx(30);
        return btn;
    }

    // --- HIGH PERFORMANCE VIEWHOLDER: Native Click Interfaces Applied ---
    class SongViewHolder : RecyclerView.ViewHolder, View.IOnClickListener, View.IOnLongClickListener
    {
        private readonly BaseViewModelAnd MyViewModel;
        private readonly Fragment _parentFrag;
        private readonly ImageView _img;
        private readonly TextView _title;
        private readonly TextView _artist;
        private readonly View _expandRow;
        private readonly MaterialButton _moreBtn;
        private readonly TextView _durationView;
        private readonly object _playCountView;
        private readonly MaterialCardView _container;
        private readonly LinearLayout _topRow;

        private readonly Button _favBtn;
        private readonly Button _infoBtn;
        private readonly Button _lyricsBtn;
        private readonly Button _statsBtn;

        private SongModelView? _currentSong;
        private Action<int>? _expandAction;

        public SongViewHolder(BaseViewModelAnd vm, Fragment parentFrag, MaterialCardView container,
            ImageView img, TextView title, TextView artist, MaterialButton moreBtn, TextView durationView, TextView playCountView,
            View expandRow, LinearLayout expandRow1, Button favBtn, Button lyrBtn, Button infoBtn, Button statsBtn, LinearLayout topRow)
            : base(container)
        {
            MyViewModel = vm;
            _parentFrag = parentFrag;
            _container = container;
            _img = img;
            _title = title;
            _artist = artist;
            _moreBtn = moreBtn;
            _durationView = durationView;
            _playCountView = playCountView;
            _expandRow = expandRow;
            _favBtn = favBtn;
            _infoBtn = infoBtn;
            _lyricsBtn = lyrBtn;
            _statsBtn = statsBtn;
            _topRow = topRow;

            // NO C# += DELEGATES. Bind to native Java interface.
            _topRow.SetOnClickListener(this);
            _moreBtn.SetOnClickListener(this);
            _moreBtn.SetOnLongClickListener(this);
            _lyricsBtn.SetOnClickListener(this);
            _infoBtn.SetOnClickListener(this);
            _statsBtn.SetOnClickListener(this);
            _img.SetOnClickListener(this);
            _artist.SetOnLongClickListener(this);
            _favBtn.SetOnClickListener(this);
            _favBtn.SetOnLongClickListener(this);
        }

        public void Bind(SongModelView song, bool isExpanded, Action<int> onExpandToggle)
        {
            _currentSong = song;
            _expandAction = onExpandToggle;

            _title.Text = song.Title;
            _artist.Text = song.OtherArtistsName ?? "Unknown";
            _durationView.Text = $"{song.DurationFormatted}";
            _durationView.Text = $"{song.PlayCompletedCount}";

            if (song.HasSyncedLyrics)
            {
                _container.StrokeWidth = 4;
                _container.SetStrokeColor(AppUtil.ToColorStateList(Color.DarkSlateBlue));
            }
            else
            {
                _container.StrokeWidth = isExpanded ? 3 : 0;
                _container.StrokeColor = isExpanded ? Color.DarkSlateBlue : Color.ParseColor("#E0E0E0");
            }

            ViewCompat.SetTransitionName(_img, $"sharedImage_{song.Id}");

            if (!string.IsNullOrEmpty(song.CoverImagePath))
            {
                var domCol = MyViewModel.CurrentPlaySongDominantColor;
                if (domCol is not null)
                {
                    _container.SetBackgroundColor(Color.Argb(10, (int)domCol.Red, (int)domCol.Green, (int)domCol.Blue));
                }
                _img.SetImageWithGlide(song.CoverImagePath);
            }
            else
            {
                _img.SetImageResource(Resource.Drawable.musicnotess);
            }

            _expandRow.Visibility = isExpanded ? ViewStates.Visible : ViewStates.Gone;
        }

        // --- CENTRALIZED CLICK HANDLER ---
        public void OnClick(View? v)
        {
            if (_currentSong == null) return;

            if (v == _topRow)
            {
                _ = MyViewModel.PlaySongAsync(_currentSong); // Fire and forget
            }
            else if (v == _moreBtn)
            {
                string imgTransName = $"img_morph_{_currentSong.Id}";
                string titleTransName = $"title_morph_{_currentSong.Id}";
                _img.TransitionName = imgTransName;
                _title.TransitionName = titleTransName;

                var popupFragment = new SongDetailOverlayFragment(_currentSong, imgTransName, titleTransName);
                AnimationHelper.ShowMorphingFragment(_parentFrag.ParentFragmentManager, popupFragment,
                    (_img, imgTransName), (_title, titleTransName));
            }
            else if (v == _lyricsBtn)
            {
                MyViewModel._lyricsMgtFlow.LoadLyrics(_currentSong.SyncLyrics);
                MyViewModel.SelectedSong = _currentSong;
                MyViewModel.NavigateToAnyPageOfGivenType(_parentFrag, new LyricsViewFragment(MyViewModel), "toLyricsFromNP");
            }
            else if (v == _infoBtn)
            {
                MyViewModel.SelectedSong = _currentSong;
                var songContexFragment = new SongContextMenuFragment(MyViewModel);
                songContexFragment.Show(_parentFrag.ParentFragmentManager, "infoDiag");
            }
            else if (v == _statsBtn)
            {
                var infoSheet = new SongInfoBottomSheetFragment(MyViewModel, _currentSong);
                infoSheet.Show(_parentFrag.ParentFragmentManager, "SongInfoSheet");
            }
            else if (v == _img)
            {
                MyViewModel.SelectedSong = _currentSong;
                string? tName = ViewCompat.GetTransitionName(_img);
                if (tName != null)
                    MyViewModel.NavigateToSingleSongPageFromHome(_parentFrag, tName, _img);
                else
                    (_parentFrag.Activity as TransitionActivity)?.HandleBackPressInternal();
            }
            else if (v == _favBtn)
            {
                _ = MyViewModel.AddFavoriteRatingToSong(_currentSong);
                _favBtn.Text = !_currentSong.IsFavorite ? "Unfav" : "Fav";
                _favBtn.SetIconResource(_currentSong.IsFavorite ? Resource.Drawable.heartlock : Resource.Drawable.heart);
                _favBtn.IconTint = _currentSong.IsFavorite ? AppUtil.ToColorStateList(Color.DarkSlateBlue) : AppUtil.ToColorStateList(Color.Gray);
                v.PerformHapticFeedback(FeedbackConstants.Confirm);
            }
        }

        // --- CENTRALIZED LONG CLICK HANDLER ---
        public bool OnLongClick(View? v)
        {
            if (_currentSong == null) return false;

            if (v == _moreBtn)
            {
                _container.PerformHapticFeedback(FeedbackConstants.LongPress);
                ShowPlaybackOptionsMenu();
                return true;
            }
            else if (v == _artist)
            {
                if (_currentSong.ArtistName != null)
                {
                    var artistPickBtmSheet = new ArtistPickerBottomSheet(MyViewModel, _currentSong.ArtistsInDB(MyViewModel.RealmFactory));
                    artistPickBtmSheet.Show(_parentFrag.ParentFragmentManager, "QueueSheet");
                }
                return true;
            }
            else if (v == _favBtn)
            {
                _ = MyViewModel.RemoveSongFromFavorite(_currentSong);
                var iconRes = _currentSong.IsFavorite ? Resource.Drawable.heartlock : Resource.Drawable.heart;
                _favBtn.Text = !_currentSong.IsFavorite ? "Unfav" : "Fav";
                _favBtn.SetIconResource(iconRes);
                v.PerformHapticFeedback(FeedbackConstants.Reject);
                return true;
            }
            return false;
        }

        private void ShowPlaybackOptionsMenu()
            {
                if (_currentSong == null)
                    return;

                var ctx = _container.Context;
                if (ctx == null)
                    return;

                // Create a bottom sheet dialog with playback options
                var dialog = new BottomSheetDialog(ctx);

                // Create the layout programmatically
                var mainLayout = new LinearLayout(ctx)
                {
                    Orientation = Orientation.Vertical,
                    LayoutParameters = new ViewGroup.LayoutParams(
                        ViewGroup.LayoutParams.MatchParent,
                        ViewGroup.LayoutParams.WrapContent)
                };
                mainLayout.SetPadding(AppUtil.DpToPx(24), AppUtil.DpToPx(16), AppUtil.DpToPx(24), AppUtil.DpToPx(24));

                // Title
                var titleView = new MaterialTextView(ctx)
                {
                    Text = "Playback Options",
                    TextSize = 20,
                    Typeface = Typeface.DefaultBold
                };
                titleView.SetForegroundGravity(GravityFlags.CenterHorizontal | GravityFlags.CenterVertical);
                titleView.SetPadding(0, 0, 0, AppUtil.DpToPx(16));
                mainLayout.AddView(titleView);

                // Play Now button
                var playNowBtn = new MaterialButton(ctx)
                {
                    Text = "Play Now",
                    LayoutParameters = new LinearLayout.LayoutParams(
                        ViewGroup.LayoutParams.MatchParent,
                        ViewGroup.LayoutParams.WrapContent)
                    {
                        BottomMargin = AppUtil.DpToPx(8)
                    }
                };
                playNowBtn.SetIconResource(Resource.Drawable.play);
                playNowBtn.Click += async (s, e) =>
                {
                    await MyViewModel.PlaySongWithActionAsync(_currentSong, Dimmer.Utilities.Enums.PlaybackAction.PlayNow);
                    Toast.MakeText(ctx, $"Playing {_currentSong.Title}", ToastLength.Short)?.Show();
                    dialog.Dismiss();
                };
                mainLayout.AddView(playNowBtn);

                // Play Next button
                var playNextBtn = new MaterialButton(ctx)
                {
                    Text = "Play Next",
                    LayoutParameters = new LinearLayout.LayoutParams(
                        ViewGroup.LayoutParams.MatchParent,
                        ViewGroup.LayoutParams.WrapContent)
                    {
                        BottomMargin = AppUtil.DpToPx(8)
                    }
                };
                playNextBtn.SetIconResource(Resource.Drawable.media3_icon_next);
                playNextBtn.Click += async (s, e) =>
                {
                    await MyViewModel.PlaySongWithActionAsync(_currentSong, Dimmer.Utilities.Enums.PlaybackAction.PlayNext);
                    Toast.MakeText(ctx, $"Added {_currentSong.Title} to play next", ToastLength.Short)?.Show();
                    dialog.Dismiss();
                };
                mainLayout.AddView(playNextBtn);

                // Add to Queue button
                var addToQueueBtn = new MaterialButton(ctx)
                {
                    Text = "Add to Queue",
                    LayoutParameters = new LinearLayout.LayoutParams(
                        ViewGroup.LayoutParams.MatchParent,
                        ViewGroup.LayoutParams.WrapContent)
                    {
                        BottomMargin = AppUtil.DpToPx(8)
                    }
                };
                addToQueueBtn.SetIconResource(Resource.Drawable.media3_icon_queue_add);
                addToQueueBtn.Click += async (s, e) =>
                {
                    await MyViewModel.PlaySongWithActionAsync(_currentSong, Dimmer.Utilities.Enums.PlaybackAction.AddToQueue);
                    Toast.MakeText(ctx, $"Added {_currentSong.Title} to queue", ToastLength.Short)?.Show();
                    dialog.Dismiss();
                };
                mainLayout.AddView(addToQueueBtn);

                // View in Queue button
                var viewInQueueBtn = new MaterialButton(ctx)
                {
                    Text = "View in Queue",
                    LayoutParameters = new LinearLayout.LayoutParams(
                        ViewGroup.LayoutParams.MatchParent,
                        ViewGroup.LayoutParams.WrapContent)
                };
                viewInQueueBtn.SetIconResource(Resource.Drawable.eye);
                viewInQueueBtn.Click += (s, e) =>
                {
                    MyViewModel.SelectedSong = _currentSong;
                    var queueSheet = new QueueBottomSheetFragment(MyViewModel, viewInQueueBtn);
                    viewInQueueBtn.Enabled = false;
                    queueSheet.Show(_parentFrag.ParentFragmentManager, "QueueSheet");
                    queueSheet.ScrollToSong(_currentSong);
                    dialog.Dismiss();
                };
                mainLayout.AddView(viewInQueueBtn);

                dialog.SetContentView(mainLayout);
                dialog.Show();

            }



        }

        ~HomePageAdapter()
        {
            AdapterCallbacks = null;
        }
        class SongDiff : DiffUtil.Callback
        {
            private readonly IList<SongModelView> _oldList;
            private readonly IList<SongModelView> _newList;

            public SongDiff(IList<SongModelView> oldList, IList<SongModelView> newList)
            {
                _oldList = oldList;
                _newList = newList;
            }

            public override int OldListSize => _oldList.Count;
            public override int NewListSize => _newList.Count;

            public override bool AreItemsTheSame(int oldPos, int newPos)
            {
                return _oldList[oldPos].Id == _newList[newPos].Id;
            }

            public override bool AreContentsTheSame(int oldPos, int newPos)
            {
                var oldItem = _oldList[oldPos];
                var newItem = _newList[newPos];

            // Compare only what is visible in the ViewHolder to be fast
            return oldItem.TitleDurationKey == newItem.TitleDurationKey &&
                       oldItem.ArtistName == newItem.ArtistName &&
                       oldItem.IsFavorite == newItem.IsFavorite &&
                       oldItem.CoverImagePath == newItem.CoverImagePath;
            }
        }

        public class ItemGestureListener : GestureDetector.SimpleOnGestureListener
        {
            public event Action<int, View, SongModelView>? SingleTap;
            public event Action<int, View>? LongPressStage1;
            public event Action<int, View>? LongPressStage2;

            private readonly RecyclerView recycler;
            private readonly Handler handler = new Handler(Looper.MainLooper!);

            public ItemGestureListener(RecyclerView rv)
            {
                recycler = rv;
            }
            public override bool OnSingleTapUp(MotionEvent e)
            {
                var child = recycler.FindChildViewUnder(e.GetX(), e.GetY());
                if (child == null) return false;

                int pos = recycler.GetChildAdapterPosition(child);

                Debug.WriteLine(child.GetType().FullName);
                // get adapter
                var adapter = recycler.GetAdapter() as SongAdapter; // replace SongAdapter with your actual adapter type

                var song = adapter?.GetItem(pos); // this assumes your adapter has a GetItem method
                if (song == null) return false;

                SingleTap?.Invoke(pos, child, song); // pass song too if you want

                return true;
            }



            public void CancelTimers()
            {
                handler.RemoveCallbacksAndMessages(null);
            }
        }

        public class TouchListener : Java.Lang.Object, RecyclerView.IOnItemTouchListener
        {
            private readonly GestureDetector detector;
            private readonly ItemGestureListener listener;

            public TouchListener(Context ctx, RecyclerView rv)
            {

                listener = new ItemGestureListener(rv);
                detector = new GestureDetector(ctx, listener);
            }

            public event Action<int, View, SongModelView>? SingleTap
            {
                add => listener.SingleTap += value;
                remove => listener.SingleTap -= value;
            }

            public event Action<int, View>? LongPressStage1
            {
                add => listener.LongPressStage1 += value;
                remove => listener.LongPressStage1 -= value;
            }

            public event Action<int, View>? LongPressStage2
            {
                add => listener.LongPressStage2 += value;
                remove => listener.LongPressStage2 -= value;
            }

            public bool OnInterceptTouchEvent(RecyclerView rv, MotionEvent e)
            {
                detector.OnTouchEvent(e);

                if (e.Action == MotionEventActions.Cancel)
                    listener.CancelTimers();

                return false;
            }

            public void OnTouchEvent(RecyclerView rv, MotionEvent e)
            {
                detector.OnTouchEvent(e);
                if (e.Action == MotionEventActions.Cancel)
                    listener.CancelTimers();
            }

            public void OnRequestDisallowInterceptTouchEvent(bool disallowIntercept) { }
        }

        public enum AdapterMode
        {
            Normal,
            MultiSelect,
            SingleSelect
        }


    }
