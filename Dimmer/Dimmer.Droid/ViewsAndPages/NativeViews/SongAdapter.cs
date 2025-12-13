using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

using AndroidX.Core.View;

using Bumptech.Glide;

using Dimmer.DimmerSearch;

using DynamicData;

using MongoDB.Bson;

namespace Dimmer.ViewsAndPages.NativeViews;

internal class SongAdapter : RecyclerView.Adapter
{
    public enum ViewId { Image, Title, Artist, Container }
    public static Action<View, string, int>? AdapterCallbacks;
    private Context ctx;
    public BaseViewModelAnd MyViewModel;
    private IList<SongModelView> _songs = Enumerable.Empty<SongModelView>().ToList();
    private readonly IDisposable _subscription;
    public IList<SongModelView> Songs => _songs;
    private Fragment ParentFragement;

    // Accordion State
    private int _expandedPosition = -1;
    private int _previousExpandedPosition = -1;

    public SongModelView GetItem(int position) => Songs.ElementAt(position);

    public SongAdapter(Context ctx, BaseViewModelAnd myViewModel, Fragment pFragment, string songsToWatch = "main")
    {
        ParentFragement = pFragment;
        this.ctx = ctx;
        this.MyViewModel = myViewModel;

        IObservable<IChangeSet<SongModelView>> sourceStream;
        IEnumerable<SongModelView> sourceList;

        if (songsToWatch == "queue")
        {
            sourceStream = MyViewModel.PlaybackQueueSource.Connect();
            sourceList = MyViewModel.PlaybackQueue;
        }
        else
        {
            sourceStream = MyViewModel.SearchResultsHolder.Connect();
            sourceList = MyViewModel.SearchResults;
        }

        _subscription = sourceStream
            // 1. Throttle: Wait for a 200ms pause in updates to avoid spamming diff calculations 
            //    during rapid changes (like initial load or fast typing).
            .Throttle(TimeSpan.FromMilliseconds(200), RxSchedulers.Background)

            // 2. Project to a Task that calculates the Diff on a BACKGROUND THREAD
            .Select(_ =>
            {
                // Capture the NEW list snapshot (thread-safe copy)
                var newList = sourceList.ToList();
                // Capture the OLD list snapshot
                var oldList = _songs.ToList();

                return Observable.Start(() =>
                {
                    // HEAVY WORK: Calculate Diff on ThreadPool
                    var diffResult = DiffUtil.CalculateDiff(new SongDiff(oldList, newList));
                    return new { Diff = diffResult, Data = newList };
                }, RxSchedulers.Background);
            })
            // 3. Switch: If a new update comes while calculating, cancel the old calculation
            .Switch()
            // 4. Observe on UI Thread: Only for applying the visual update
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(result =>
            {
                // Update the backing field
                _songs = result.Data;
                _expandedPosition = -1;
                // Apply the diff (Fast, just dispatches notify events)
                result.Diff.DispatchUpdatesTo(this);
            });
    }
    public override int ItemCount => _songs.Count;
    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        if (holder is SongViewHolder songHolder)
        {
            var song = _songs[position];
            bool isExpanded = position == _expandedPosition;

            songHolder.Bind(song, isExpanded, position, (pos) => ToggleExpand(pos));
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
            _adapter.OnItemMove(source.BindingAdapterPosition, target.AdapterPosition);
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
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _subscription.Dispose();
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
        // --- UI CONSTRUCTION (MD3 Style) ---
        // Root Card
        var card = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(12),
            CardElevation = AppUtil.DpToPx(0), // Flat style is modern
            StrokeWidth = AppUtil.DpToPx(1),
            StrokeColor = Color.ParseColor("#E0E0E0")
        };
        card.SetCardBackgroundColor(Color.Transparent); // Let ripple show

        var lp = new RecyclerView.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
        lp.SetMargins(0, 8, 0, 8);
        card.LayoutParameters = lp;

        // Main Vertical Layout (Holds TopRow + HiddenActions)
        var mainContainer = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        mainContainer.Clickable = true;
        mainContainer.LayoutTransition = new LayoutTransition();

        mainContainer.LayoutParameters = new ViewGroup.LayoutParams(-1, -2);

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
        topRow.AddView(moreBtn);

        // --- EXPANDABLE ROW (Hidden by default) ---
        var expandRow = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        expandRow.SetGravity(GravityFlags.Center);
        expandRow.Visibility = ViewStates.Gone; // Hidden initially
        expandRow.LayoutParameters = new LinearLayout.LayoutParams(-1, -2);
        expandRow.SetPadding(0, 0, 0, 20);

        var playBtn = CreateActionButton("Play", Android.Resource.Drawable.IcMediaPlay);
        expandRow.AddView(playBtn);

        var favBtn = CreateActionButton("Fav", Android.Resource.Drawable.StarOff);
        expandRow.AddView(favBtn);

        var addBtn = CreateActionButton("Add", Android.Resource.Drawable.IcInputAdd);
        expandRow.AddView(addBtn);
        // Assemble
        mainContainer.AddView(topRow);
        mainContainer.AddView(expandRow);
        card.AddView(mainContainer);

        return new SongViewHolder(MyViewModel, ParentFragement, card, imgView, title, artist, moreBtn, expandRow, (Button)playBtn, (Button)favBtn);
    }


    private View CreateActionButton(string text, int iconId)
    {
        var btn = new MaterialButton(ctx, null, Resource.Attribute.materialButtonOutlinedStyle);
        btn.Text = text;

        btn.SetIconResource(iconId);
        btn.SetPadding(30, 0, 30, 0);
        var lp = new LinearLayout.LayoutParams(-2, -2);
        lp.RightMargin = 10;
        btn.LayoutParameters = lp;
        return btn;
    }


    class SongViewHolder : AndroidX.RecyclerView.Widget.RecyclerView.ViewHolder
    {
        private readonly SerialDisposable _itemSubscription = new SerialDisposable();


        private readonly BaseViewModelAnd _vm;
        private readonly Fragment _parentFrag;
        private readonly ImageView _img;
        private readonly TextView _title, _artist;
        private readonly View _expandRow;
        private readonly MaterialButton _moreBtn;
        private readonly MaterialCardView _container;
        public View ContainerView => base.ItemView;
        public Action<View, string, string> OnNavigateRequest;

        private readonly Button _playBtn;
        private readonly Button _favBtn;
        public SongViewHolder(BaseViewModelAnd vm, Fragment parentFrag, MaterialCardView container, ImageView img, TextView title, TextView artist, MaterialButton moreBtn, View expandRow,
            Button playBtn, Button favBtn)
            : base(container)
        {
            _vm = vm;
            _parentFrag = parentFrag;
            _container = container;
            _img = img;
            _title = title;
            _artist = artist;
            _moreBtn = moreBtn;
            _expandRow = expandRow;
            _playBtn = playBtn;
            _favBtn = favBtn;
        }

        public void Bind(SongModelView song, bool isExpanded, int position, Action<int> onExpandToggle)
        {
            var sessionDisposable = new CompositeDisposable();
            _title.Text = song.Title;
            _artist.Text = song.OtherArtistsName ?? "Unknown";

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

            _artist.SetOnClickListener(null);
            // Accordion Visibility
            _expandRow.Visibility = isExpanded ? ViewStates.Visible : ViewStates.Gone;
            _container.StrokeColor = isExpanded ? Color.DarkSlateBlue : Color.ParseColor("#E0E0E0");
            _container.StrokeWidth = isExpanded ? 4 : 2;



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

            song.WhenPropertyChange(nameof(SongModelView.CoverImagePath), s => s.CoverImagePath)
                .ObserveOn(RxSchedulers.UI)
                .Subscribe(path =>
                {
                    if (!string.IsNullOrEmpty(path))
                    {
                        Glide.With(_img.Context).Load(path)
                             .Placeholder(Resource.Drawable.musicnotess)
                             .Into(_img);
                    }
                    else
                    {
                        _img.SetImageResource(Resource.Drawable.musicnotess);
                    }
                })
                .DisposeWith(sessionDisposable);







            // --- CLICK LOGIC ---

            // 1. More Button -> Toggles Accordion
            _moreBtn.SetOnClickListener(null);
            _moreBtn.Click += (s, e) =>
            {
                onExpandToggle(BindingAdapterPosition);
            };


            _img.SetOnClickListener(null);
           
            _img.Click += (s, e) =>
            {
                _vm.SelectedSong = song;
                _vm.NavigateToSingleSongPageFromHome(_parentFrag, tName, _img);
            };


            _artist.SetOnClickListener(null);
            _artist.LongClickable = true;
            
                _artist.LongClick += (s, e) =>
                {
                    var query = $"artist:\"{song.ArtistName}\"";
                    _vm.SearchSongForSearchResultHolder(query);
                };
            

            _container.SetOnClickListener(null);
            
                _container.Click += async (s,e) =>
                {
                    await _vm.PlaySongAsync(song);
                };

            _playBtn.SetOnClickListener(null);
            _playBtn.Click += async (s, e) => await _vm.PlaySongAsync(song);

            _favBtn.Text = song.IsFavorite ? "Unfav" : "Fav";
            _favBtn.SetOnClickListener(null);
            _favBtn.Click += async (s, e) =>
            {
                await _vm.AddFavoriteRatingToSong(song);
            };


            _itemSubscription.Disposable = sessionDisposable;

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _itemSubscription.Dispose();
            base.Dispose(disposing);
        }




        ~SongViewHolder()
        {
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