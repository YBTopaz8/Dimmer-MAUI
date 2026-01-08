using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

using AndroidX.Core.View;
using AndroidX.Lifecycle;

using Bumptech.Glide;

using Dimmer.DimmerSearch;
using Dimmer.ViewsAndPages.NativeViews.Misc;
using Dimmer.ViewsAndPages.NativeViews.SectionHeader;
using Dimmer.WinUI.UiUtils;

using DynamicData;

using MongoDB.Bson;

namespace Dimmer.ViewsAndPages.NativeViews;

internal class SongAdapter : RecyclerView.Adapter
{
    private const int VIEW_TYPE_HEADER = 0;
    private const int VIEW_TYPE_SONG = 1;

    private ReadOnlyObservableCollection<SongModelView> _songs;
    private List<ListItem> _listItems = new();
    private List<SectionHeaderModel> _sections = new();

    public enum ViewId { Image, Title, Artist, Container }
    public static Action<View, string, int>? AdapterCallbacks;
    private Context ctx;
    public BaseViewModelAnd MyViewModel;
    private readonly IDisposable _subscription;
    public IList<SongModelView> Songs => _songs;
    private Fragment ParentFragement;

    // Accordion State
    private int _expandedPosition = -1;
    private int _previousExpandedPosition = -1;

    /// <summary>
    /// Gets the song at the specified position in the original songs list (not the flattened list with headers)
    /// </summary>
    public SongModelView GetItem(int position) => Songs.ElementAt(position);

    /// <summary>
    /// Gets the song from a flattened list position (accounting for headers)
    /// </summary>
    public SongModelView? GetSongAtFlatPosition(int flatPosition)
    {
        if (flatPosition >= 0 && flatPosition < _listItems.Count)
        {
            var item = _listItems[flatPosition];
            if (item.Type == ListItem.ItemType.Song)
            {
                return item.Song;
            }
        }
        return null;
    }

    IObservable<IChangeSet<SongModelView>> sourceStream;
    IEnumerable<SongModelView> sourceList;
    public SongAdapter(Context ctx, BaseViewModelAnd myViewModel, Fragment pFragment, string songsToWatch = "main")
    {
        ParentFragement = pFragment;
        this.ctx = ctx;
        this.MyViewModel = myViewModel;

        IObservable<IChangeSet<SongModelView>> sourceStream = songsToWatch == "queue"
           ? myViewModel.PlaybackQueueSource.Connect()
           : myViewModel.SearchResultsHolder.Connect();

        // 3. The "DynamicData" Pipeline
        sourceStream
            .ObserveOn(RxSchedulers.UI) // Must be on UI thread to update RecyclerView
            .Bind(out _songs)           // Automatically keeps _songs in sync with the source
            .Subscribe(changes =>
            {
                // Rebuild list items with sections
                RebuildListItems();
                NotifyDataSetChanged(); // Full refresh for now - can be optimized later
            })
            .DisposeWith(_disposables);

        // Listen to CurrentQueryPlan changes to update sections
        if (songsToWatch == "main")
        {
            myViewModel.WhenPropertyChange(nameof(BaseViewModelAnd.CurrentQueryPlan), vm => vm.CurrentQueryPlan)
                .ObserveOn(RxSchedulers.UI)
                .Subscribe(_ =>
                {
                    RebuildListItems();
                    NotifyDataSetChanged();
                })
                .DisposeWith(_disposables);
        }
    }

    private void RebuildListItems()
    {
        _listItems.Clear();
        _sections.Clear();

        if (_songs == null || _songs.Count == 0)
        {
            return;
        }

        // Compute sections based on current query plan
        _sections = SectionGroupingHelper.ComputeSections(_songs, MyViewModel.CurrentQueryPlan);

        // Build flat list with headers and songs
        foreach (var section in _sections)
        {
            // Add header
            _listItems.Add(ListItem.CreateHeader(section));

            // Add songs in this section
            for (int i = section.SongStartIndex; i < section.SongStartIndex + section.SongCount && i < _songs.Count; i++)
            {
                _listItems.Add(ListItem.CreateSong(_songs[i]));
            }
        }
    }

    public List<SectionHeaderModel> GetSections() => _sections;
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (disposing) _subscription.Dispose();
            _disposables.Dispose();
        }
        base.Dispose(disposing);
    }
    private readonly CompositeDisposable _disposables = new();

    public override int ItemCount => _listItems.Count;

    public override int GetItemViewType(int position)
    {
        if (position < _listItems.Count)
        {
            return _listItems[position].Type == ListItem.ItemType.Header ? VIEW_TYPE_HEADER : VIEW_TYPE_SONG;
        }
        return VIEW_TYPE_SONG;
    }

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        if (position >= _listItems.Count) return;

        var listItem = _listItems[position];

        if (holder is SectionHeaderViewHolder headerHolder && listItem.Type == ListItem.ItemType.Header)
        {
            headerHolder.Bind(listItem.Header!, (section) =>
            {
                // Show bottom sheet menu when header is clicked
                var menuSheet = new SectionHeaderMenuBottomSheet(
                    MyViewModel,
                    section,
                    (RecyclerView)holder.ItemView.Parent!,
                    onExitShuffle: () =>
                    {
                        // Exit shuffle by applying a default sort
                        MyViewModel.SearchSongForSearchResultHolder("sort:added desc");
                    },
                    onChangeSortMode: (sortQuery) =>
                    {
                        MyViewModel.SearchSongForSearchResultHolder(sortQuery);
                    });

                menuSheet.Show(ParentFragement.ParentFragmentManager, "SectionHeaderMenu");
            });
        }
        else if (holder is SongViewHolder songHolder && listItem.Type == ListItem.ItemType.Song)
        {
            var song = listItem.Song!;
            bool isExpanded = position == _expandedPosition;

            songHolder.Bind(song, isExpanded, (pos) => ToggleExpand(pos));
        }
    }

    public class SimpleItemTouchHelperCallback : ItemTouchHelper.Callback
    {
        private readonly SongAdapter _adapter;
        public SimpleItemTouchHelperCallback(SongAdapter adapter) => _adapter = adapter;

        public override int GetMovementFlags(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
        {
            int dragFlags = ItemTouchHelper.Up | ItemTouchHelper.Down;
            int swipeFlags = ItemTouchHelper.Start | ItemTouchHelper.End; // Enable Swipe to Remove?
            return MakeMovementFlags(dragFlags, swipeFlags);
        }

        public override bool OnMove(RecyclerView recyclerView, RecyclerView.ViewHolder source, RecyclerView.ViewHolder target)
        {
            // Notify Adapter to swap items in the ObservableCollection
            _adapter.OnItemMove(source.BindingAdapterPosition, target.BindingAdapterPosition);
            return true;
        }

        public override void OnSwiped(RecyclerView.ViewHolder viewHolder, int direction)
        {
            // Notify Adapter to remove item
            //_adapter.OnItemDismiss(viewHolder.AdapterPosition);
        }
    }

    public bool OnItemMove(int fromPosition, int toPosition)
    {
        // 1. Move the item in the actual data list
        // If it's an ObservableCollection, use Move for efficiency
       
        MyViewModel.PlaybackQueueSource.Edit(upd=>
            {
                //MyViewModel.PlaybackQueueSource.RemoveAt(fromPosition);
                //MyViewModel.PlaybackQueueSource.Insert(toPosition, item);
            });

        // 2. Notify the RecyclerView that the item moved visually
        // IMPORTANT: Do NOT call NotifyDataSetChanged(), it breaks animations.
        NotifyItemMoved(fromPosition, toPosition);

        return true;
    }

    public void OnItemDismiss(int position)
    {
        // 1. Remove from data source
        MyViewModel.PlaybackQueueSource.Edit(upd=>
            {
                MyViewModel.PlaybackQueueSource.RemoveAt(position);
            });

        // 2. Notify RecyclerView
        NotifyItemRemoved(position);

        // 3. Optional: Notify Playback service that the queue changed?
        // _viewModel.UpdateQueueService(); 
    }
  

    private void ToggleExpand(int position)
    {
        _previousExpandedPosition = _expandedPosition;

        if (_expandedPosition == position)
        {
            // Collapse current
            _expandedPosition = -1;
        }
        else
        {
            // Expand new
            _expandedPosition = position;
        }

        // Only notify the rows that changed to animate/update efficiently
        if (_previousExpandedPosition != -1) NotifyItemChanged(_previousExpandedPosition);
        if (_expandedPosition != -1) NotifyItemChanged(_expandedPosition);
    }

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        if (viewType == VIEW_TYPE_HEADER)
        {
            return SectionHeaderViewHolder.Create(ctx, parent);
        }

        // VIEW_TYPE_SONG - create song view holder
        // Root Card
        var card = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(12),
            CardElevation = AppUtil.DpToPx(0), // Flat style is modern
            StrokeWidth = AppUtil.DpToPx(0),
            StrokeColor = Color.Transparent,
            Clickable = true,
            Focusable = true
        };
        //var attrs = new int[] { Android.Resource.Attribute.SelectableItemBackground };
        //var typedArray = ctx.ObtainStyledAttributes(attrs);
        //var rippleDrawable = typedArray.GetDrawable(0);
        //typedArray.Recycle();
        //card.Foreground = rippleDrawable;
        //card.SetCardBackgroundColor(Color.Transparent); // Let ripple show

        var lp = new RecyclerView.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
        lp.SetMargins(0, 8, 0, 8);
        card.LayoutParameters = lp;

        // Main Vertical Layout (Holds TopRow + HiddenActions)
        var mainContainer = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        mainContainer.Clickable = true;
        mainContainer.LayoutTransition = new LayoutTransition();

        mainContainer.LayoutParameters = new ViewGroup.LayoutParams(-1, -2);
        mainContainer.SetBackgroundColor(UiBuilder.ThemedBGColor(ctx));

        // --- TOP ROW (Visible) ---
        var topRow = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        topRow.SetGravity(GravityFlags.CenterVertical);
        topRow.SetPadding(20, 20, 20, 20);
        topRow.LayoutParameters = new LinearLayout.LayoutParams(-1, -2);
        topRow.Clickable = true;
        topRow.Click += (o, s) =>
        {
            card.PerformClick();
        };

        // Image
        var imgCard = new MaterialCardView(ctx) { Radius = AppUtil.DpToPx(8), CardElevation = 0 };
        var imgView = new ImageView(ctx);
        imgView.SetScaleType(ImageView.ScaleType.CenterCrop);
        imgCard.AddView(imgView, new ViewGroup.LayoutParams(AppUtil.DpToPx(56), AppUtil.DpToPx(56)));
        topRow.AddView(imgCard);

        // Texts
        var textLayout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        var txtLp = new LinearLayout.LayoutParams(0, -2, 1f); // Weight 1
        txtLp.LeftMargin = AppUtil.DpToPx(16);
        textLayout.LayoutParameters = txtLp;

        var title = new MaterialTextView(ctx) { TextSize = 16, Typeface = Typeface.DefaultBold };
        title.SetMaxLines(1);
        title.Ellipsize = Android.Text.TextUtils.TruncateAt.End;

        var artist = new MaterialTextView(ctx) { TextSize = 14 };
        artist.SetTextColor(Color.Gray);

        textLayout.AddView(title);
        textLayout.AddView(artist);
        topRow.AddView(textLayout);

        // More Button (The Trigger)
        var moreBtn = new MaterialButton(ctx, null, Resource.Attribute.materialIconButtonStyle);
        moreBtn.SetIconResource(Resource.Drawable.more1); // Ensure this drawable exists
        moreBtn.IconTint = Android.Content.Res.ColorStateList.ValueOf(Color.Gray);
        moreBtn.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Color.Transparent);
        moreBtn.RippleColor = Android.Content.Res.ColorStateList.ValueOf(Color.LightGray);
        var durationView = new TextView(ctx) { TextSize = 16, Typeface = Typeface.DefaultBold };
        durationView.SetTextColor(UiBuilder.IsDark(this.ParentFragement.Resources.Configuration) ? Color.Gray : Color.Black);
        durationView.TextSize = 10;
        durationView.Gravity = GravityFlags.CenterHorizontal;

        var rightLinearLayout = new LinearLayout(ctx)
        { 
            Orientation = Android.Widget.Orientation.Vertical 
        };
        rightLinearLayout.AddView(moreBtn);
        rightLinearLayout.AddView(durationView);



        topRow.AddView(rightLinearLayout);
        // --- EXPANDABLE ROW (Hidden by default) ---
        var expandRow = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        expandRow.SetGravity(GravityFlags.Center);
        expandRow.Visibility = ViewStates.Gone; // Hidden initially
        expandRow.LayoutParameters = new LinearLayout.LayoutParams(-1, -2);
        expandRow.SetPadding(0, 0, 0, 20);

        var playBtn = CreateActionButton("Play Next", Resource.Drawable.exo_icon_play);
        expandRow.AddView(playBtn);

        var favBtn = CreateActionButton("Fav", Resource.Drawable.heart);
        expandRow.AddView(favBtn);

        var infoBtn = CreateActionButton("Info", Resource.Drawable.infocircle);
        MaterialButton? LyricsBtn = CreateActionButton("Lyrics", Resource.Drawable.lyrics);
        
        expandRow.AddView(infoBtn);
        expandRow.AddView(LyricsBtn);
        // Assemble
        mainContainer.AddView(topRow);
        mainContainer.AddView(expandRow);

        card.AddView(mainContainer);

        return new SongViewHolder(MyViewModel, ParentFragement, card, imgView, title, artist, moreBtn, durationView,expandRow, (Button)playBtn, (Button)favBtn, (Button)infoBtn,
            LyricsBtn);
    }


    private MaterialButton CreateActionButton(string text, int iconId)
    {
        var btn = new MaterialButton(ctx, null, Resource.Attribute.materialButtonOutlinedStyle);
        btn.Text = text;
        btn.SetTextColor(UiBuilder.IsDark(this.ParentFragement.Resources.Configuration) ? Color.Gray : Color.ParseColor("#294159"));
        btn.SetIconResource(iconId);

        btn.IconTint = Android.Content.Res.ColorStateList.ValueOf(UiBuilder.IsDark(this.ParentFragement.Resources.Configuration) ? Color.Gray : Color.ParseColor("#294159"));
        btn.SetPadding(30, 0, 30, 0);
        var lp = new LinearLayout.LayoutParams(-2, -2);
        lp.RightMargin = 10;
        btn.LayoutParameters = lp;
        btn.IconSize = AppUtil.DpToPx(20);
        return btn;
    }


    class SongViewHolder : AndroidX.RecyclerView.Widget.RecyclerView.ViewHolder
    {
        private readonly SerialDisposable _itemSubscription = new SerialDisposable();


        private readonly BaseViewModelAnd _viewModel;
        private readonly Fragment _parentFrag;
        private readonly ImageView _img;
        private readonly TextView _title, _artist;
        private readonly View _expandRow;
        private readonly MaterialButton _moreBtn;
        private readonly TextView _durationView;
        private readonly MaterialCardView _container;
        public View ContainerView => base.ItemView;

        private readonly Button _playNextBtn;
        private readonly Button _favBtn;
        private readonly Button _infoBtn;
        private readonly Button lyricsBtn;
        private SongModelView? _currentSong;
        private Action<int>? _expandAction;

        public SongViewHolder(BaseViewModelAnd vm, Fragment parentFrag, MaterialCardView container, ImageView img, TextView title, TextView artist, MaterialButton moreBtn, TextView durationView,
 View expandRow, Button playBtn, Button favBtn, Button infoBtn, Button lyrBtn)
            : base(container)
        {
            _viewModel = vm;
            _parentFrag = parentFrag;
            _container = container;
            _img = img;
            _title = title;
            _artist = artist;
            _moreBtn = moreBtn;
            _durationView= durationView;
            _expandRow = expandRow;
            _playNextBtn = playBtn;
            _favBtn = favBtn;
            _infoBtn = infoBtn;
            lyricsBtn = lyrBtn;

            _moreBtn.Click += (s, e) =>
            {
                // Always invoke the latest action with the current position
                _expandAction?.Invoke(BindingAdapterPosition);
            };

            lyrBtn.Click += (s, e) =>
            {
                _viewModel._lyricsMgtFlow.LoadLyrics(_currentSong?.SyncLyrics);
                _viewModel.SelectedSong = _currentSong;
                _viewModel.NavigateToAnyPageOfGivenType(this._parentFrag, new LyricsViewFragment(_viewModel), "toLyricsFromNP");
                
            };

            // 2. Container Click (Play)
            _container.Click += async (s, e) =>
            {
                if (_currentSong != null)
                    await _viewModel.PlaySongAsync(_currentSong);
            };

            _container.LongClick += (s, e) =>
            {
                _container.PerformHapticFeedback(FeedbackConstants.LongPress);
                // view in playbackQUeue
                _viewModel.SelectedSong=_currentSong;

                var queueSheet = new QueueBottomSheetFragment(_viewModel);
                queueSheet.Show(parentFrag.ParentFragmentManager, "QueueSheet");

                queueSheet.ScrollToSong(_currentSong);
            };

            _infoBtn.Click += (s, e) =>
            {
                var infoSheet = new SongInfoBottomSheetFragment(_viewModel, _currentSong);
                infoSheet.Show(_parentFrag.ParentFragmentManager, "SongInfoSheet");
            };


            // 3. Play Button
            _playNextBtn.Click += async (s, e) =>
            {
                if (_currentSong != null)
                {
                    _viewModel.SetAsNextToPlayInQueue(_currentSong);
                    
                }
                    
            };

            lyrBtn.Click += async (s, e) =>
            {
                if (_currentSong != null)
                {
                    await _viewModel.ShareSongViewClipboard(_currentSong);
                }
            };




            // 4. Image Click (Navigate)
            _img.Click += (s, e) =>
            {
                if (_currentSong != null)
                {
                    _viewModel.SelectedSong = _currentSong;
                    // Note: Transition name must be updated in Bind, but we can read it from the view here
                    string? tName = ViewCompat.GetTransitionName(_img);
                    if (tName != null)
                    {
                        _viewModel.NavigateToSingleSongPageFromHome(_parentFrag, tName, _img);
                    }
                }
            };

            // 5. Artist Long Click
            _artist.LongClickable = true;
            _artist.LongClick += (s, e) =>
            {
                if (_currentSong?.ArtistName != null)
                {
                    var query = $"artist:\"{_currentSong.ArtistName}\"";
                    _viewModel.SearchSongForSearchResultHolder(query);
                }
            };

            // 6. Fav Button
            _favBtn.Click += async (s, e) =>
            {
                if (_currentSong != null)
                {
                    await _viewModel.AddFavoriteRatingToSong(_currentSong);
                    // Instant visual feedback
                    _favBtn.Text = !_currentSong.IsFavorite ? "Unfav" : "Fav";
                    _favBtn.SetIconResource(_currentSong.IsFavorite ? Resource.Drawable.heartlock : Resource.Drawable.heart);
                    _favBtn.IconTint = _currentSong.IsFavorite ? AppUtil.ToColorStateList(Color.DarkSlateBlue) : AppUtil.ToColorStateList(Color.Gray) ;
                }
            };
            _favBtn.LongClick += async (s, e) =>
            {
                if (_currentSong != null)
                {
                    await _viewModel.RemoveSongFromFavorite(_currentSong);
                    var iconRes = _currentSong.IsFavorite ? Resource.Drawable.heartlock : Resource.Drawable.heart;
                    // Instant visual feedback
                    _favBtn.Text = !_currentSong.IsFavorite ? "Unfav" : "Fav";
                    _favBtn.SetIconResource(iconRes);
                    UiBuilder.ShowSnackBar(
    _favBtn, 
    _currentSong.IsFavorite ? "Added to Favorites" : "Removed from Favorites",
    textColor: Color.Black,
    iconResId: iconRes
);
                }
            };
        }

        public void Bind(SongModelView song, bool isExpanded, Action<int> onExpandToggle)
        {
            _currentSong = song;
            _expandAction = onExpandToggle;
            var sessionDisposable = new CompositeDisposable();
            _title.Text = song.Title;
            _artist.Text = song.OtherArtistsName ?? "Unknown";
            _durationView.Text = $"{song.DurationFormatted}";

            if (song.HasSyncedLyrics)
            {
                _container.StrokeWidth = 4;
                _container.SetStrokeColor(AppUtil.ToColorStateList(Color.DarkSlateBlue));
            }

            // Set Transition Name
            var tName = $"sharedImage_{song.Id}";
            ViewCompat.SetTransitionName(_img, tName);

            // Image Loading
            if (!string.IsNullOrEmpty(song.CoverImagePath))
            {
                Glide.With(_img.Context).Load(song.CoverImagePath)
                     .Placeholder(Resource.Drawable.musicnotess).Into(_img);
            }
            else
            {
                _img.SetImageResource(Resource.Drawable.musicnotess);
            }

            // Accordion Visibility
            _expandRow.Visibility = isExpanded ? ViewStates.Visible : ViewStates.Gone;
            _container.StrokeColor = isExpanded ? Color.DarkSlateBlue : Color.ParseColor("#E0E0E0");
            _container.StrokeWidth = isExpanded ? 3 : 0;


            song.WhenPropertyChange(nameof(SongModelView.IsFavorite), s=>s.IsFavorite)
                .ObserveOn(RxSchedulers.UI)
                .Subscribe(IsFavorite =>
                {
                    if (IsFavorite)
                    {
                        _moreBtn.CornerRadius = AppUtil.DpToPx(10);
                        _moreBtn.StrokeWidth = AppUtil.DpToPx(1);
                        _moreBtn.SetStrokeColorResource(Resource.Color.m3_ref_palette_pink80);
                    }
                    else
                    {
                        _moreBtn.StrokeWidth = AppUtil.DpToPx(0);

                    }
                });
            song.WhenPropertyChange(nameof(SongModelView.IsCurrentPlayingHighlight), s => s.IsCurrentPlayingHighlight)
                        .ObserveOn(RxSchedulers.UI) // Ensure UI Thread
                        .Subscribe(isPlaying =>
                        {
                            if (isPlaying)
                            {
                                _title.SetTextColor(Color.DarkSlateBlue); // Highlight Text
                                                                          // _img.SetImageResource(Resource.Drawable.equalizer_anim); // Maybe show animation?
                            }
                            else
                            {
                                // Reset to normal
                                var isDark = _container.Context.Resources.Configuration.UiMode.HasFlag(Android.Content.Res.UiMode.NightYes);
                                _title.SetTextColor(isDark ? Color.White : Color.Black);
                            }
                        })
                        .DisposeWith(sessionDisposable);

            song.WhenPropertyChange(nameof(SongModelView.HasSyncedLyrics), s => s.HasSyncedLyrics)
                        .ObserveOn(RxSchedulers.UI) // Ensure UI Thread
                        .Subscribe(hasSyncLyrics =>
                        {
                            if (hasSyncLyrics)
                            {
                                _container.StrokeWidth = 1;
                            }
                            else
                            {
                                //_container.StrokeWidth = 0;
                                

                            }
                        })
                        .DisposeWith(sessionDisposable);

            song.WhenPropertyChange(nameof(SongModelView.CoverImagePath), s => s.CoverImagePath)
                .ObserveOn(RxSchedulers.UI)
                .Subscribe(path =>
                {
                    if (!string.IsNullOrEmpty(path))
                    {
                        Glide.With(_img.Context).Load(path)
                             .Placeholder(Resource.Drawable.musicnotess)
                             .Into(_img);
                        if(song == _viewModel.CurrentPlayingSongView)
                        {
                          
                        }
                    }
                    else
                    {
                        _img.SetImageResource(Resource.Drawable.musicnotess);
                    }
                })
                .DisposeWith(sessionDisposable);







            _itemSubscription.Disposable = sessionDisposable;

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _itemSubscription.Dispose();
            base.Dispose(disposing);
        }




    }

    ~SongAdapter()
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

        // Fast check: Are these the same object (ID)?
        public override bool AreItemsTheSame(int oldPos, int newPos)
        {
            return _oldList[oldPos].Id == _newList[newPos].Id;
        }

        // Slower check: Did the visual content change?
        public override bool AreContentsTheSame(int oldPos, int newPos)
        {
            var oldItem = _oldList[oldPos];
            var newItem = _newList[newPos];

            // Compare only what is visible in the ViewHolder to be fast
            return oldItem.Title == newItem.Title &&
                   oldItem.ArtistName == newItem.ArtistName &&
                   oldItem.IsFavorite == newItem.IsFavorite &&
                   oldItem.CoverImagePath == newItem.CoverImagePath;
        }
    }

    public class ItemGestureListener : GestureDetector.SimpleOnGestureListener
    {   
        public event Action<int,View, SongModelView>? SingleTap;
        public event Action<int,View>? LongPressStage1;
        public event Action<int,View>? LongPressStage2;

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

            Console.WriteLine(child.GetType().FullName);
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

}