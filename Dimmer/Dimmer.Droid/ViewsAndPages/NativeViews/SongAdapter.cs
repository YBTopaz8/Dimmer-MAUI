

namespace Dimmer.ViewsAndPages.NativeViews;


internal partial class SongAdapter : RecyclerView.Adapter, IDisposable
{
    private readonly BehaviorSubject<AdapterMode> _mode = new(AdapterMode.Normal);
    public IObservable<AdapterMode> Mode => _mode.AsObservable();

    readonly HashSet<int> _selectedPositions = new HashSet<int>();
    public IObservable<Unit> SelectionChanged => _selectionChanged.AsObservable();
    public Subject<Unit> _selectionChanged => new();

    public override int ItemCount => _songs?.Count ?? 0;

    private readonly BehaviorSubject<bool> _isSourceCleared = new(true);
    private readonly BehaviorSubject<bool> _isAdapterReady = new(false);
    public IObservable<bool> IsSourceCleared => _isSourceCleared.AsObservable();
    public IObservable<bool> IsAdapterReady => _isAdapterReady.AsObservable();

    private ReadOnlyObservableCollection<SongModelView> _songs;
    public static Action<View, string, int>? AdapterCallbacks;

    private Context ctx;
    public BaseViewModelAnd MyViewModel;
    private SongsToWatchSource songSource;
    private readonly IDimmerAudioService _audioService;
    public IList<SongModelView> Songs => _songs;
    private Fragment ParentFragement;

    // Accordion State
    private int _expandedPosition = -1;
    private int _previousExpandedPosition = -1;

    private readonly CompositeDisposable _disposables = new();
    private bool _isDisposed;

    public SongModelView GetItem(int position) => Songs.ElementAt(position);

    public SongAdapter(Context ctx, BaseViewModelAnd vm, Fragment pFragment, SongsToWatchSource songsToWatch = SongsToWatchSource.HomePage)
    {
        MyViewModel = vm;
        songSource = songsToWatch;
        _audioService = vm.AudioService;
        ParentFragement = pFragment;

        SetupReactivePipeline(vm, songsToWatch);
    }

    private void SetupReactivePipeline(BaseViewModelAnd viewModel, SongsToWatchSource songsToWatch)
    {
        IObservable<IChangeSet<SongModelView>> sourceStream = songsToWatch switch
        {
            SongsToWatchSource.QueuePage => viewModel.PlaybackQueueSource.Connect(),
            SongsToWatchSource.ArtistPage => GetArtistSongsStream(viewModel),
            SongsToWatchSource.AlbumPage => GetAlbumSongsStream(viewModel),
            SongsToWatchSource.HomePage => viewModel.SearchResultsHolder.Connect(),
            _ => viewModel.SearchResultsHolder.Connect(),
        };

        sourceStream.ObserveOn(RxSchedulers.UI)
            .Do(s => Debug.WriteLine($"Song Count in adapter: {viewModel.SearchResults.Count}"))
            .Bind(out _songs)
            .Subscribe(changes =>
            {
                if (_isDisposed) return;
                if (!_isAdapterReady.Value) _isAdapterReady.OnNext(true);

                foreach (var change in changes)
                {
                    switch (change.Reason)
                    {
                        case ListChangeReason.AddRange:
                            NotifyItemRangeInserted(change.Range.Index, change.Range.Count);
                            _isSourceCleared.OnNext(true);
                            break;
                        case ListChangeReason.RemoveRange:
                            NotifyItemRangeRemoved(change.Range.Index, change.Range.Count);
                            break;
                        case ListChangeReason.Refresh:
                            NotifyItemChanged(change.Item.CurrentIndex);
                            break;
                        case ListChangeReason.Add:
                            NotifyItemInserted(change.Item.CurrentIndex);
                            break;
                        case ListChangeReason.Remove:
                            NotifyItemRemoved(change.Item.CurrentIndex);
                            break;
                        case ListChangeReason.Moved:
                            NotifyItemMoved(change.Item.PreviousIndex, change.Item.CurrentIndex);
                            break;
                        case ListChangeReason.Replace:
                            NotifyItemChanged(change.Item.CurrentIndex);
                            break;
                        case ListChangeReason.Clear:
                            _isSourceCleared.OnNext(true);
                            NotifyDataSetChanged();
                            break;
                    }
                }
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

    private IObservable<IChangeSet<SongModelView>> GetAlbumSongsStream(BaseViewModelAnd viewModel)
    {
        var selAlb = viewModel.SelectedAlbum;
        var realm = viewModel.RealmFactory.GetRealmInstance();
        var albumInDB = realm.Find<AlbumModel>(selAlb!.Id);

        if (albumInDB != null)
            return albumInDB.SongsInAlbum!.AsObservableChangeSet()!.Transform(model => model.ToSongModelView()!)!;
        return Observable.Return(ChangeSet<SongModelView>.Empty);
    }

    private IObservable<IChangeSet<SongModelView>> GetArtistSongsStream(BaseViewModelAnd viewModel)
    {
        var selArt = viewModel.SelectedArtist;
        var realm = viewModel.RealmFactory.GetRealmInstance();
        var artistEntry = realm.Find<ArtistModel>(selArt.Id);

        if (artistEntry != null)
        {
            return artistEntry.Songs
                .AsObservableChangeSet()
                .Transform(model => model.ToSongModelView()!)
                .ObserveOn(RxSchedulers.Background)
                .ObserveOn(RxSchedulers.UI)!;
        }
        return Observable.Return(ChangeSet<SongModelView>.Empty);
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
            var song = _songs[position];
            bool isExpanded = position == _expandedPosition;
            songHolder.Bind(song, isExpanded, ToggleExpand, songSource);
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
        if (ctx is null && parent.Context is not null)
            ctx = parent.Context;

        var card = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(12),
            CardElevation = AppUtil.DpToPx(0),
            StrokeWidth = AppUtil.DpToPx(0),
            StrokeColor = Color.Transparent,
            Clickable = true,
            Focusable = true
        };

        var lp = new RecyclerView.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
        lp.SetMargins(0, 8, 0, 8);
        card.LayoutParameters = lp;

        var mainContainer = new LinearLayout(ctx) { Orientation = Orientation.Vertical, Clickable = true, LayoutTransition = new LayoutTransition() };
        mainContainer.LayoutParameters = new ViewGroup.LayoutParams(-1, -2);

        // --- TOP ROW ---
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

        var rightLayout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        rightLayout.AddView(moreBtn);
        rightLayout.AddView(durationView);
        topRow.AddView(rightLayout);

        // --- EXPANDABLE ROW ---
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

        // --- EXPANDABLE ROW TWO ---
        var expandRowTwo = new LinearLayout(ctx) { Orientation = Orientation.Vertical, Visibility = ViewStates.Gone };
        expandRowTwo.SetGravity(GravityFlags.Center);
        expandRowTwo.SetPadding(0, 0, 0, 20);

        expandRowTwo.AddView(new ChipGroup(ctx));
        expandRowTwo.AddView(CreateActionButton("album", Resource.Drawable.musicalbum));

        mainContainer.AddView(topRow);
        mainContainer.AddView(expandRow);
        mainContainer.AddView(expandRowTwo);
        card.AddView(mainContainer);

        return new SongViewHolder(MyViewModel, ParentFragement, card, imgView, title, artist, moreBtn, durationView, expandRow, favBtn, lyricsBtn, infoBtn, statsBtn, topRow);
    }

    private MaterialButton CreateActionButton(string text, int iconId)
    {
        var btn = new MaterialButton(ctx, null, Resource.Attribute.materialButtonOutlinedStyle);
        btn.Text = text;
        btn.SetTextColor(UiBuilder.IsDark(btn) ? Color.Gray : Color.ParseColor("#294159"));
        btn.SetIconResource(iconId);
        btn.IconTint = Android.Content.Res.ColorStateList.ValueOf(UiBuilder.IsDark(btn) ? Color.Gray : Color.ParseColor("#294159"));
        btn.SetPadding(30, 0, 30, 0);
        var lp = new LinearLayout.LayoutParams(-2, -2) { RightMargin = 10 };
        btn.LayoutParameters = lp;
        btn.IconSize = AppUtil.DpToPx(30);
        return btn;
    }

    // =========================================================================
    // HIGH PERFORMANCE VIEWHOLDER (ZERO C# DELEGATE ALLOCATIONS)
    // =========================================================================
    class SongViewHolder : RecyclerView.ViewHolder, View.IOnClickListener, View.IOnLongClickListener
    {
        private readonly SerialDisposable _itemSubscription = new SerialDisposable();

        private readonly BaseViewModelAnd MyViewModel;
        private readonly Fragment _parentFrag;
        private readonly ImageView _img;
        private readonly TextView _title;
        private readonly TextView _artist;
        private readonly View _expandRow;
        private readonly MaterialButton _moreBtn;
        private readonly TextView _durationView;
        private readonly MaterialCardView _container;
        private readonly LinearLayout _topRow;

        private readonly Button _favBtn;
        private readonly Button _infoBtn;
        private readonly Button _lyricsBtn;
        private readonly Button _statsBtn;

        private SongModelView? _currentSong;
        private Action<int>? _expandAction;
        private SongsToWatchSource _mode;

        public SongViewHolder(BaseViewModelAnd vm, Fragment parentFrag, MaterialCardView container,
            ImageView img, TextView title, TextView artist, MaterialButton moreBtn, TextView durationView,
            View expandRow, Button favBtn, Button lyrBtn, Button infoBtn, Button statsBtn, LinearLayout topRow)
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
            _expandRow = expandRow;
            _favBtn = favBtn;
            _infoBtn = infoBtn;
            _lyricsBtn = lyrBtn;
            _statsBtn = statsBtn;
            _topRow = topRow;

            // NATIVE JAVA CLICK MAPPING (Zero GC Allocation during scroll)
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

        public void Bind(SongModelView song, bool isExpanded, Action<int> onExpandToggle, SongsToWatchSource mode)
        {
            _currentSong = song;
            _expandAction = onExpandToggle;
            _mode = mode;

            var sessionDisposable = new CompositeDisposable();

            _title.Text = song.Title;
            _artist.Text = song.OtherArtistsName ?? "Unknown";
            _durationView.Text = $"{song.DurationFormatted}";

            if (song.HasSyncedLyrics)
            {
                _container.StrokeWidth = 4;
                _container.SetStrokeColor(AppUtil.ToColorStateList(Color.DarkSlateBlue));
            }
            else
            {
                _container.StrokeColor = isExpanded ? Color.DarkSlateBlue : Color.ParseColor("#E0E0E0");
                _container.StrokeWidth = isExpanded ? 3 : 0;
            }

            ViewCompat.SetTransitionName(_img, $"sharedImage_{song.Id}");
            _expandRow.Visibility = isExpanded ? ViewStates.Visible : ViewStates.Gone;

            // Reactive UI Bindings inside row
            song.WhenPropertyChange(nameof(SongModelView.IsFavorite), s => s.IsFavorite)
                .ObserveOn(RxSchedulers.UI)
                .Subscribe(IsFavorite =>
                {
                    if (IsFavorite)
                    {
                        _moreBtn.CornerRadius = AppUtil.DpToPx(10);
                        _moreBtn.StrokeWidth = AppUtil.DpToPx(1);
                        _favBtn.StrokeWidth = 0;
                        _moreBtn.SetStrokeColorResource(Resource.Color.m3_ref_palette_pink80);
                    }
                    else
                    {
                        _moreBtn.StrokeWidth = AppUtil.DpToPx(0);
                    }
                }).DisposeWith(sessionDisposable);

            song.WhenPropertyChange(nameof(SongModelView.IsCurrentPlayingHighlight), s => s.IsCurrentPlayingHighlight)
                .ObserveOn(RxSchedulers.UI)
                .Subscribe(isPlaying =>
                {
                    if (isPlaying)
                    {
                        _title.SetTextColor(Color.DarkSlateBlue);
                    }
                    else
                    {
                        var isDark = _container.Context.Resources.Configuration.UiMode.HasFlag(Android.Content.Res.UiMode.NightYes);
                        _title.SetTextColor(isDark ? Color.White : Color.Black);
                    }
                }).DisposeWith(sessionDisposable);

            song.WhenPropertyChange(nameof(SongModelView.CoverImagePath), s => s.CoverImagePath)
                .ObserveOn(RxSchedulers.UI)
                .Subscribe(path =>
                {
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (song.TitleDurationKey == MyViewModel.CurrentPlayingSongView?.TitleDurationKey)
                        {
                            MyViewModel.CurrentCoverImagePath = path;
                            MyViewModel.CurrentPlayingSongView.CoverImagePath = path;
                        }
                        Glide.With(_img.Context).Load(path).Placeholder(Resource.Drawable.musicnotess).Into(_img);
                    }
                    else
                    {
                        _img.SetImageResource(Resource.Drawable.musicnotess);
                    }
                }).DisposeWith(sessionDisposable);

            _itemSubscription.Disposable = sessionDisposable;
        }

        // --- CENTRALIZED FAST CLICK ROUTER ---
        public void OnClick(View? v)
        {
            if (_currentSong == null) return;

            if (v == _topRow)
            {
                _ = MyViewModel.PlaySongAsync(_currentSong);
            }
            else if (v == _moreBtn)
            {
                _expandAction?.Invoke(BindingAdapterPosition);
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
                {
                    if (_mode == SongsToWatchSource.HomePage)
                        MyViewModel.NavigateToSingleSongPageFromHome(_parentFrag, tName, _img);
                }
                else
                {
                    (_parentFrag.Activity as TransitionActivity)?.HandleBackPressInternal();
                }
            }
            else if (v == _favBtn)
            {
                _ = MyViewModel.AddFavoriteRatingToSongAsync(_currentSong);
                _favBtn.Text = !_currentSong.IsFavorite ? "Unfav" : "Fav";
                _favBtn.SetIconResource(_currentSong.IsFavorite ? Resource.Drawable.heartlock : Resource.Drawable.heart);
                _favBtn.IconTint = _currentSong.IsFavorite ? AppUtil.ToColorStateList(Color.DarkSlateBlue) : AppUtil.ToColorStateList(Color.Gray);
                v.PerformHapticFeedback(FeedbackConstants.Confirm);
                UiBuilder.ShowSnackBar(_parentFrag.View, _currentSong.IsFavorite ? $"Added to Favorites" : $"Removed from Favorites", textColor: Color.Black);
            }
        }

        // --- CENTRALIZED FAST LONG CLICK ROUTER ---
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
                _ = MyViewModel.RemoveSongFromFavoriteAsync(_currentSong);
                var iconRes = _currentSong.IsFavorite ? Resource.Drawable.heartlock : Resource.Drawable.heart;
                _favBtn.Text = !_currentSong.IsFavorite ? "Unfav" : "Fav";
                _favBtn.SetIconResource(iconRes);
                v.PerformHapticFeedback(FeedbackConstants.Reject);
                UiBuilder.ShowSnackBar(_parentFrag.View, _currentSong.IsFavorite ? $"Added to Favorites" : $"Removed from Favorites", textColor: Color.Black, iconResId: iconRes);
                return true;
            }
            return false;
        }

        private void ShowPlaybackOptionsMenu()
        {
            if (_currentSong == null || _container.Context == null) return;
            var ctx = _container.Context;

            var dialog = new BottomSheetDialog(ctx);
            var mainLayout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
            mainLayout.SetPadding(AppUtil.DpToPx(24), AppUtil.DpToPx(16), AppUtil.DpToPx(24), AppUtil.DpToPx(24));

            var titleView = new MaterialTextView(ctx) { Text = "Playback Options", TextSize = 20, Typeface = Typeface.DefaultBold };
            titleView.SetPadding(0, 0, 0, AppUtil.DpToPx(16));
            mainLayout.AddView(titleView);

            var playNowBtn = new MaterialButton(ctx) { Text = "Play Now" };
            playNowBtn.SetIconResource(Resource.Drawable.play);
            playNowBtn.Click += async (s, e) => { await MyViewModel.PlaySongWithActionAsync(_currentSong, Dimmer.Utilities.Enums.PlaybackAction.PlayNow); dialog.Dismiss(); };
            mainLayout.AddView(playNowBtn);

            var playNextBtn = new MaterialButton(ctx) { Text = "Play Next" };
            playNextBtn.SetIconResource(Resource.Drawable.media3_icon_next);
            playNextBtn.Click += async (s, e) => { await MyViewModel.PlaySongWithActionAsync(_currentSong, Dimmer.Utilities.Enums.PlaybackAction.PlayNext); dialog.Dismiss(); };
            mainLayout.AddView(playNextBtn);

            var addToQueueBtn = new MaterialButton(ctx) { Text = "Add to Queue" };
            addToQueueBtn.SetIconResource(Resource.Drawable.media3_icon_queue_add);
            addToQueueBtn.Click += async (s, e) => { await MyViewModel.PlaySongWithActionAsync(_currentSong, Dimmer.Utilities.Enums.PlaybackAction.AddToQueue); dialog.Dismiss(); };
            mainLayout.AddView(addToQueueBtn);

            dialog.SetContentView(mainLayout);
            dialog.Show();
        }
    }

    // Touch, Swipe, and Move Helpers remain unchanged below
    public class SimpleItemTouchHelperCallback : ItemTouchHelper.Callback
    {
        private readonly SongAdapter _adapter;
        public SimpleItemTouchHelperCallback(SongAdapter adapter) => _adapter = adapter;

        public override int GetMovementFlags(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
        {
            int dragFlags = ItemTouchHelper.Up | ItemTouchHelper.Down;
            int swipeFlags = ItemTouchHelper.Start | ItemTouchHelper.End;
            return MakeMovementFlags(dragFlags, swipeFlags);
        }

        public override bool OnMove(RecyclerView recyclerView, RecyclerView.ViewHolder source, RecyclerView.ViewHolder target)
        {
            _adapter.OnItemMove(source.BindingAdapterPosition, target.BindingAdapterPosition);
            return true;
        }

        public override void OnSwiped(RecyclerView.ViewHolder viewHolder, int direction) => _adapter.OnItemDismiss(viewHolder.BindingAdapterPosition);
    }

    public bool OnItemMove(int fromPosition, int toPosition)
    {
        MyViewModel.MoveSongInQueue(fromPosition, toPosition);
        NotifyItemMoved(fromPosition, toPosition);
        return true;
    }

    public void OnItemDismiss(int position)
    {
        MyViewModel.PlaybackQueueSource.Edit(upd => MyViewModel.PlaybackQueueSource.RemoveAt(position));
        NotifyItemRemoved(position);
    }

    // Touch Listeners...
    public enum AdapterMode { Normal, MultiSelect, SingleSelect }
    public enum SongsToWatchSource { HomePage, QueuePage, AlbumPage, ArtistPage, PlaylistPage }
}