using System.Reactive.Disposables.Fluent;

using AndroidX.Core.View;
using AndroidX.Lifecycle;

using Bumptech.Glide;

using Dimmer.DimmerSearch;
using Dimmer.Utilities.Extensions;
using Dimmer.Utilities.TypeConverters;

using DynamicData;

using Google.Android.Material.Button;
using Google.Android.Material.Card;
using Google.Android.Material.TextView;

using ImageButton = Android.Widget.ImageButton;

namespace Dimmer.ViewsAndPages.NativeViews;

internal class SongAdapter : RecyclerView.Adapter
{
    public enum ViewId { Image, Title, Artist, Container }
    public static Action<View, string, int>? AdapterCallbacks;
    private Context ctx;
    public BaseViewModelAnd MyViewModel;
    private IEnumerable<SongModelView> _songs = Enumerable.Empty<SongModelView>();
    private readonly IDisposable _subscription;
    public IEnumerable<SongModelView> Songs => _songs;
    private Fragment ParentFragement;
    private void OnItemClick(View sharedView, string transitionName, int position)
    {
        var song = _songs.ElementAt(position);
        MyViewModel.SelectedSong = song;
        MyViewModel.NavigateToSingleSongPageFromHome(ParentFragement, transitionName, sharedView);
        //OpenDetailFragment(sharedView, transitionName);
    }
    public SongModelView GetItem(int position) => Songs.ElementAt(position);

    public SongAdapter(Context ctx, BaseViewModelAnd myViewModel, Fragment pFragment, string songsToWatch="main")
    {
        ParentFragement = pFragment;
        this.ctx = ctx;
        this.MyViewModel = myViewModel;

        if(songsToWatch=="main")
        {
            _songs = MyViewModel.SearchResults.ToList();
            _subscription = MyViewModel.SearchResultsHolder
           .Connect()
           .ObserveOn(RxSchedulers.UI)
           .Subscribe(changes =>
           {
               var diff = DiffUtil.CalculateDiff(new SongDiff(_songs.ToList(), MyViewModel.SearchResults.ToList()));
               _songs = MyViewModel.SearchResults.ToList();
               diff.DispatchUpdatesTo(this);
               //_songs = vm.SearchResults;   // update enumerable reference

           })
           ;
        }
        else if(songsToWatch=="queue")
        {
            _songs = MyViewModel.PlaybackQueue.ToList();
            _subscription = MyViewModel.PlaybackQueueSource
           .Connect()
           .ObserveOn(RxSchedulers.UI)
           .Subscribe(changes =>
           {
           
               var diff = DiffUtil.CalculateDiff(new SongDiff(_songs.ToList(), MyViewModel.PlaybackQueue.ToList()));
               _songs = MyViewModel.PlaybackQueue.ToList();
               diff.DispatchUpdatesTo(this);
               //_songs = vm.SearchResults;   // update enumerable reference

           });
        }
        
        //AdapterCallbacks = OnItemClick;
    }

    public override int ItemCount => _songs.Count();

    public override void OnBindViewHolder(AndroidX.RecyclerView.Widget.RecyclerView.ViewHolder holder, int position)
    {
        if (holder is SongViewHolder songHolder)
        {
            var isDarkTheme = ctx.Resources.Configuration.UiMode.HasFlag(Android.Content.Res.UiMode.NightYes);

            // find song in given position
            var song = _songs.ElementAt(position);
            //songHolder.Bind(song);
            
            songHolder.SongTitle.Text = song.Title;
            
            songHolder.AlbumNameView.Text = song.AlbumName ?? "Unknown Album";
            songHolder.ArtistNameView.Text = song.ArtistName ?? "Unknown Artist";


            var isCardView = songHolder.ContainerView.GetType() == typeof(CardView);
            if (isCardView)
            {
                CardView card = (CardView)songHolder.ContainerView;
                card.StrokeWidth = song.IsFavorite ? 4 : 1;

                var colorListBG = new ColorStateList(
            new int[][] {
                new int[] { } // default
            },
            new int[] {
                song.IsFavorite ? Color.DarkSlateBlue : Color.ParseColor("#FFFFFF")
                }
            );
                card.SetStrokeColor(colorListBG);
            }


            var dur = song.DurationInSeconds;
            if (dur is double duration)
            {
                TimeSpan time = TimeSpan.FromSeconds(duration);
                songHolder.SongDurationView.Text = time.ToString(@"mm\:ss");
            }

            // handle image
            if (!string.IsNullOrEmpty(song.CoverImagePath))
            {

                Glide.With(ctx)
                     .Load(song.CoverImagePath)
                     .Placeholder(Resource.Drawable.musicnotess)
                     .Into(songHolder.ImageBtn);

                //// Load from disk
                //var bmp = Android.Graphics.BitmapFactory.DecodeFile(song.CoverImagePath);
                //vh.ImageBtn.SetImageBitmap(bmp);
            }

            else
            {
                // Fallback placeholder
                songHolder.ImageBtn.SetImageResource(Resource.Drawable.musicnotess);
            }
            // ensure unique transition name
            var transitionName = $"sharedImage_{song.Id}";
            var songIdAsTag = song.Id.ToString();
            songHolder.ImageBtn.Tag = songIdAsTag;
            songHolder.ContainerView.Tag = songIdAsTag;
            ViewCompat.SetTransitionName(songHolder.ImageBtn, transitionName);
            


        }
    }

    private void OpenDetailFragment(View sharedView, string transitionName)
    {
        if (ctx is not AndroidX.Fragment.App.FragmentActivity activity) return;

        var fragment = new DetailFragment(transitionName, MyViewModel);

        var hostFragment = activity.SupportFragmentManager.FindFragmentById(TransitionActivity.MyStaticID);
        if (hostFragment == null) return;

        // source fragment’s exit transform
        var exit = new MaterialElevationScale(true);
        exit.SetDuration(200);
        var reenter = new MaterialElevationScale(false);
        reenter.SetDuration(200);
        hostFragment.ExitTransition = exit;
        hostFragment.ReenterTransition = reenter;

        activity.SupportFragmentManager
            .BeginTransaction()
            .SetReorderingAllowed(true)
            .AddSharedElement(sharedView, transitionName)
            .Replace(TransitionActivity.MyStaticID, fragment)
            .AddToBackStack(null)
            .Commit();
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
    ImageView imgBtn;
    LinearLayout row;
    MaterialTextView title;
    MaterialTextView artist;
    MaterialTextView album; 
    MaterialButton moreBtn;
    MaterialTextView durationTextView;
    public override AndroidX.RecyclerView.Widget.RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {

        var materialCardView = new MaterialCardView(ctx)
        {
            CardElevation = AppUtil.DpToPx(4),
            Radius = AppUtil.DpToPx(8),
        };
        
        var rippleColorList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.DarkSlateBlue);
        materialCardView.RippleColor = rippleColorList;
        
        

        var lyParams = new RecyclerView.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent
        );
        lyParams.SetMargins(AppUtil.DpToPx(5), AppUtil.DpToPx(5), AppUtil.DpToPx(5), AppUtil.DpToPx(5));

        // set color gray if light theme and dark gray if dark theme
        var isDarkTheme = ctx.Resources.Configuration.UiMode.HasFlag(Android.Content.Res.UiMode.NightYes);

        materialCardView.LayoutParameters = lyParams;
        materialCardView.StrokeWidth = 1;
        materialCardView.StrokeColor = isDarkTheme? Color.MidnightBlue : Color.Gray;
        
        row = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
         row.LayoutParameters = new ViewGroup.LayoutParams(
        ViewGroup.LayoutParams.MatchParent, // Width: Fill the screen
        ViewGroup.LayoutParams.WrapContent  // Height: Fit content
                );

        row.SetPadding(AppUtil.DpToPx(5), AppUtil.DpToPx(5), AppUtil.DpToPx(5), AppUtil.DpToPx(5));
        row.SetGravity(GravityFlags.CenterVertical|GravityFlags.FillHorizontal);

        var imageCard = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(8), // Round corners for image
            CardElevation = 0,
            LayoutParameters = new LinearLayout.LayoutParams(AppUtil.DpToPx(50), AppUtil.DpToPx(50)) // Fixed size
        };
        var imgView = new ImageView(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(-1, -1)
        };

        imgView.SetScaleType(ImageView.ScaleType.CenterCrop);
        
        imageCard.AddView(imgView);

        
        row.AddView(imageCard);

        var textCol = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        title = new MaterialTextView(ctx) {  TextSize = 19 };

            var playingColorListBG = new ColorStateList(
        new int[][] {
                new int[] { } // default
        },
        new int[] {
                    isDarkTheme ? Color.White: Color.Black
            }
        );

        title.SetTextColor(playingColorListBG);
        artist = new MaterialTextView(ctx) { TextSize = 15 };
        album = new MaterialTextView(ctx) { TextSize = 11 };
        
        album.SetTextColor(Color.Gray);
        textCol.AddView(title);
        textCol.AddView(artist);
        textCol.AddView(album);
        var lp2 = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1);
        row.AddView(textCol, lp2);

        moreBtn = new MaterialButton(ctx);
        moreBtn.SetBackgroundColor(Color.Transparent);
        moreBtn.SetIconResource(Resource.Drawable.more1);

        durationTextView = new MaterialTextView(ctx);
        durationTextView.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent);

        var verticalLayout = new LinearLayout(ctx);
        verticalLayout.Orientation = Android.Widget.Orientation.Vertical;
        verticalLayout.AddView(durationTextView);
        verticalLayout.AddView(moreBtn);

        row.AddView(verticalLayout);

        materialCardView.AddView(row);

        imgBtn = imgView;

        return new SongViewHolder(MyViewModel, materialCardView, imgBtn, title, album, artist
            ,durationTextView);
    }


    class SongViewHolder : AndroidX.RecyclerView.Widget.RecyclerView.ViewHolder
    {
        public BaseViewModelAnd MyViewModel { get; }
        public ImageView ImageBtn { get; }
        public MaterialTextView SongTitle { get; }
        public MaterialTextView AlbumNameView { get; }
        public MaterialTextView ArtistNameView { get; }
        public MaterialTextView SongDurationView { get; }
        public View ContainerView => base.ItemView;

        public SongViewHolder(BaseViewModelAnd vm,View itemView, ImageView img, MaterialTextView title, MaterialTextView album, MaterialTextView artistName, MaterialTextView songDurView)
            : base(itemView)
        {
            MyViewModel = vm;
            ImageBtn = img;
            SongTitle = title;
            AlbumNameView = album;
            ArtistNameView = artistName;
            SongDurationView = songDurView;
            ArtistNameView.LongClickable = true;
            
            ArtistNameView.LongClick += ArtistName_LongClick;
            if(ImageBtn != null )
                ImageBtn.Click += ImageBtn_Click;
            
            ContainerView.Click += ContainerView_Click;
        }

        
        private void ArtistName_LongClick(object? sender, View.LongClickEventArgs e)
        {
            var send = (TextView)sender;
            var artistText = send.Text;
            MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.PresetQueries.ByArtist(artistText));
        }
      
        private async void ContainerView_Click(object? sender, EventArgs e)
        {
            var containerView = (View)sender;
            var songIdAsTag = containerView.Tag.ToString();
            var song= MyViewModel.SearchResults.FirstOrDefault(x=>x.Id.ToString()==songIdAsTag);
            if (song == null)
                return;
            MyViewModel.SelectedSong = song;
            // Or call your VM
            await MyViewModel.PlaySong(song, CurrentPage.AllSongs);
        }
      
        private void ImageBtn_Click(object? sender, EventArgs e)
        {
            var containerView = (View)sender;
            var songIdAsTag = containerView.Tag.ToString();
            var song = MyViewModel.SearchResults.FirstOrDefault(x => x.Id.ToString() == songIdAsTag);
            if (song == null)
                return;
            MyViewModel.SelectedSong = song;
            var sendBtn = sender as ImageView;
            //set songAsClicked
            if (sendBtn == null) return;

            var transitionName = ViewCompat.GetTransitionName(ImageBtn);
            if (transitionName is null) return;

            
            MyViewModel.NavigateToSingleSongPageFromHome((HomePageFragment)MyViewModel.CurrentPage
                , transitionName, sendBtn);
        }

        ~SongViewHolder()
        {
            ImageBtn.Click -= ImageBtn_Click;
        }
    }

    ~SongAdapter()
    {
        AdapterCallbacks = null;
    }
    class SongDiff : DiffUtil.Callback
    {
        List<SongModelView> oldList;
        List<SongModelView> newList;

        public SongDiff(List<SongModelView> oldList, List<SongModelView> newList)
        {
            this.oldList = oldList;
            this.newList = newList;
        }

        public override int OldListSize => oldList.Count;
        public override int NewListSize => newList.Count;

        public override bool AreItemsTheSame(int oldPos, int newPos)
            => oldList[oldPos].Id == newList[newPos].Id;

        public override bool AreContentsTheSame(int oldPos, int newPos)
            => oldList[oldPos].Equals(newList[newPos]);
    }

    public class ItemGestureListener : GestureDetector.SimpleOnGestureListener
    {   
        public event Action<int,View, SongModelView>? SingleTap;
        public event Action<int,View>? LongPressStage1;
        public event Action<int,View>? LongPressStage2;

        private readonly RecyclerView recycler;
        private readonly Handler handler = new Handler(Looper.MainLooper!);
        private int currentPos = -1;

        private const int Stage1Delay = 3000;
        private const int Stage2Delay = 6000;


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