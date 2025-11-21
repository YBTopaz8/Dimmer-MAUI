
using System.ComponentModel;

using Android.Content;

using AndroidX.Core.View;
using AndroidX.RecyclerView.Widget;
using AndroidX.Transitions;

using Bumptech.Glide;

using Dimmer.Utilities.Extensions;
using Dimmer.ViewsAndPages.NativeViews.Activity;

using DynamicData;

using Google.Android.Material.Transition;

using Java.Security;

using Microsoft.Maui.Controls;

using ImageButton = Android.Widget.ImageButton;

namespace Dimmer.ViewsAndPages.NativeViews;

internal class SongAdapter : RecyclerView.Adapter
{
    private Context ctx;
    internal BaseViewModelAnd vm;
    private IEnumerable<SongModelView> _songs = Enumerable.Empty<SongModelView>();
    private readonly IDisposable _subscription;
    public IEnumerable<SongModelView> Songs => _songs;
    private Fragment ParentFragement;
    private void OnItemClick(View sharedView, string transitionName, int position)
    {
        var song = _songs.ElementAt(position);
        vm.SelectedSong = song;
        vm.NavigateToSingleSongPageFromHome(ParentFragement, transitionName, sharedView);
        //OpenDetailFragment(sharedView, transitionName);
    }
    public SongModelView GetItem(int position) => Songs.ElementAt(position);

    public SongAdapter(Context ctx, BaseViewModelAnd myViewModel, Fragment pFragment)
    {
        ParentFragement = pFragment;
        this.ctx = ctx;
        this.vm = myViewModel;
        _subscription = vm.SearchResultsHolder
       .Connect()
       .ObserveOn(RxSchedulers.UI)
       .Subscribe(changes =>
       {
           var diff = DiffUtil.CalculateDiff(new SongDiff(_songs.ToList(), vm.SearchResults.ToList()));
           _songs = vm.SearchResults.ToList();
           diff.DispatchUpdatesTo(this);
           //_songs = vm.SearchResults;   // update enumerable reference
           
       });
        AdapterCallbacks = OnItemClick;
    }

    public override int ItemCount => _songs.Count();

    public override void OnBindViewHolder(AndroidX.RecyclerView.Widget.RecyclerView.ViewHolder holder, int position)
    {
        if (holder is SongViewHolder vh)
        {
            // find song in given position
            var song = _songs.ElementAt(position);
            vm.SelectedSong = song;
            vh.SongTitle.Text = song.Title;
            vh.AlbumName.Text = song.AlbumName ?? "Unknown";
            // handle image
            if (!string.IsNullOrEmpty(song.CoverImagePath) && System.IO.File.Exists(song.CoverImagePath))
            {

                Glide.With(ctx)
                     .Load(song.CoverImagePath)
                     .Placeholder(Resource.Drawable.musicnotess)
                     .Into(vh.ImageBtn);

                //// Load from disk
                //var bmp = Android.Graphics.BitmapFactory.DecodeFile(song.CoverImagePath);
                //vh.ImageBtn.SetImageBitmap(bmp);
            }

            else
            {
                // Fallback placeholder
                vh.ImageBtn.SetImageResource(Resource.Drawable.musicnotess);
            }
            // ensure unique transition name
            var transitionName = $"sharedImage_{song.Id}";
            ViewCompat.SetTransitionName(vh.ImageBtn, transitionName);
            


        }
    }

    private void OpenDetailFragment(View sharedView, string transitionName)
    {
        if (ctx is not AndroidX.Fragment.App.FragmentActivity activity) return;

        var fragment = new DetailFragment(transitionName, vm);

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

    
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _subscription.Dispose();
    }
    ImageButton imgBtn;
    LinearLayout row;
    TextView title;
    TextView artist;
    TextView album;
    public override AndroidX.RecyclerView.Widget.RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        row = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        row.SetPadding(0, 20, 0, 20);
        row.SetGravity(GravityFlags.CenterVertical);

        imgBtn = new ImageButton(ctx);
        
        imgBtn.LayoutParameters = new LinearLayout.LayoutParams(150, 150);
        imgBtn.SetBackgroundColor(Android.Graphics.Color.Transparent);
        row.AddView(imgBtn);

        var textCol = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        title = new TextView(ctx) {  TextSize = 19 };
        artist = new TextView(ctx) { TextSize = 15 };
        album = new TextView(ctx) { TextSize = 11 };

        album.SetTextColor(Color.Gray);
        textCol.AddView(title);
        textCol.AddView(artist);
        textCol.AddView(album);
        var lp2 = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1);
        row.AddView(textCol, lp2);

        var moreBtn = new ImageButton(ctx);
        moreBtn.SetBackgroundColor(Color.Transparent);
        moreBtn.SetImageResource(Resource.Drawable.more1);
        
        row.AddView(moreBtn, new LinearLayout.LayoutParams(90, 90));

        return new SongViewHolder(row, imgBtn, title, album, artist);
    }

    private void ImgBtn_Click(object? sender, EventArgs e)
    {
        var imgBtn = (ImageButton)sender!;
        vm.NavigateToSingleSongPageFromHome(ParentFragement, imgBtn.TransitionName, sender as View);
    }

    class SongViewHolder : AndroidX.RecyclerView.Widget.RecyclerView.ViewHolder
    {
        public ImageButton ImageBtn { get; }
        public TextView SongTitle { get; }
        public TextView AlbumName { get; }
        public TextView ArtistName { get; }
        public View ContainerView => base.ItemView;

        public SongViewHolder(View itemView, ImageButton img, TextView title, TextView album, TextView artistName)
            : base(itemView)
        {
            ImageBtn = img;
            SongTitle = title;
            AlbumName = album;
            ArtistName = artistName;
            ImageBtn.Click += ImageBtn_Click;
            ContainerView.Click += ContainerView_Click;
        }

        private void ContainerView_Click(object? sender, EventArgs e)
        {
            //vm.PlaySong()
        }

        private void ImageBtn_Click(object? sender, EventArgs e)
        {
            var transitionName = ViewCompat.GetTransitionName(ImageBtn);
            if (transitionName is null) return;
            AdapterCallbacks?.Invoke(ImageBtn, transitionName, BindingAdapterPosition);

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
    public enum ViewId { Image, Title, Artist, Container }
    public static Action<View, string, int>? AdapterCallbacks;
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
    private readonly Handler handler = new Handler(Looper.MainLooper);
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