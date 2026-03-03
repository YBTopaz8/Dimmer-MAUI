using Android.Media;
using Microsoft.Maui.Controls.PlatformConfiguration.TizenSpecific;
using Microsoft.Maui.Graphics.Text;
using ProgressBar = Android.Widget.ProgressBar;

namespace Dimmer.ViewsAndPages.NativeViews.Adapters;

public partial class PlayEventsAdapter : RecyclerView.Adapter, IDisposable
{
    private readonly ReadOnlyObservableCollection<DimmerPlayEventView> _sourceList;
    private readonly IRealmFactory _realmFactory;
    private readonly List<DimmerPlayEventView> _displayItems = new();
    private readonly CompositeDisposable _cleanup = new();

    private const int TYPE_NORMAL = 0;
    private const int TYPE_LOADING = 1;
    private const int TYPE_ERROR = 2;

    private bool _isLoadingMore;
    private string _errorMessage;

    // Generated IDs for our programmatic views
    private static readonly int IdTitle = View.GenerateViewId();
    private static readonly int IdArtist = View.GenerateViewId();
    private static readonly int IdPlayType = View.GenerateViewId();
    private static readonly int IdTime = View.GenerateViewId();
    private static readonly int IdAlbumArt = View.GenerateViewId();
    private static readonly int IdCompleted = View.GenerateViewId();
    private static readonly int IdProgress = View.GenerateViewId();
    private static readonly int IdErrorText = View.GenerateViewId();
    private static readonly int IdRetryBtn = View.GenerateViewId();

    public PlayEventsAdapter(ReadOnlyObservableCollection<DimmerPlayEventView> sourceList, IRealmFactory realmFactory)
    {
        _sourceList = sourceList;
        _realmFactory = realmFactory;
        SubscribeToChanges();
    }

    private void SubscribeToChanges()
    {
        _sourceList.AsObservableChangeSet()
            .ObserveOn(RxSchedulers.UI)
            .Clone(_displayItems)
            .Subscribe(changes =>
            {
                NotifyRecyclerViewChanges(changes);
            })
            .DisposeWith(_cleanup);
    }

    private void NotifyRecyclerViewChanges(IChangeSet<DimmerPlayEventView> changes)
    {
        foreach (var change in changes)
        {
            switch (change.Reason)
            {
                case ListChangeReason.Add: NotifyItemInserted(change.Item.CurrentIndex); break;
                case ListChangeReason.Remove: NotifyItemRemoved(change.Item.CurrentIndex); break;
                case ListChangeReason.Replace: NotifyItemChanged(change.Item.CurrentIndex); break;
                case ListChangeReason.Moved: NotifyItemMoved(change.Item.PreviousIndex, change.Item.CurrentIndex); break;
                case ListChangeReason.Refresh: RefreshAllItems(); break;
            }
        }
    }

    private void RefreshAllItems()
    {
        _displayItems.Clear();
        _displayItems.AddRange(_sourceList);
        NotifyDataSetChanged();
    }

    public override int ItemCount => _displayItems.Count;

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        return viewType switch
        {
            TYPE_LOADING => new LoadingViewHolder(CreateLoadingView(parent.Context)),
            TYPE_ERROR => new ErrorViewHolder(CreateErrorView(parent.Context)),
            _ => new PlayEventViewHolder(CreatePlayEventView(parent.Context), _realmFactory)
        };
    }

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        if (holder is PlayEventViewHolder playHolder) playHolder.Bind(_displayItems[position]);
        else if (holder is LoadingViewHolder loadingHolder) loadingHolder.Bind(_isLoadingMore);
        else if (holder is ErrorViewHolder errorHolder) errorHolder.Bind(_errorMessage);
    }

    // --- Programmatic UI Builders ---

    private View CreatePlayEventView(Context ctx)
    {
        var card = new MaterialCardView(ctx)
        {
            Radius = 20f,
            CardElevation = 2f,
            LayoutParameters = new RecyclerView.LayoutParams(-1, -2) { TopMargin = 10, BottomMargin = 10, LeftMargin = 20, RightMargin = 20 }
        };
        card.SetContentPadding(20, 20, 20, 20);

        var root = new RelativeLayout(ctx);

        var albumArt = new ImageView(ctx) { Id = IdAlbumArt };
        var artParams = new RelativeLayout.LayoutParams(AppUtil.DpToPx(50), AppUtil.DpToPx(50));
        artParams.AddRule(LayoutRules.AlignParentLeft);
        artParams.AddRule(LayoutRules.CenterVertical);
        albumArt.LayoutParameters = artParams;
        albumArt.SetScaleType(ImageView.ScaleType.CenterCrop);

        var textContainer = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        var textParams = new RelativeLayout.LayoutParams(-1, -2);
        textParams.AddRule(LayoutRules.RightOf, IdAlbumArt);
        textParams.LeftMargin = 30;
        textParams.RightMargin = 100; // Leave space for time/indicator
        textContainer.LayoutParameters = textParams;

        var title = new TextView(ctx) { Id = IdTitle, TextSize = 16, Typeface = Typeface.DefaultBold };
        var artist = new TextView(ctx) { Id = IdArtist, TextSize = 13, Alpha = 0.7f };
        var playType = new TextView(ctx) { Id = IdPlayType, TextSize = 11};
        playType.SetTextColor(Color.DarkSlateBlue);

        textContainer.AddView(title);
        textContainer.AddView(artist);
        textContainer.AddView(playType);

        var timeView = new TextView(ctx) { Id = IdTime, TextSize = 10 };
        var timeParams = new RelativeLayout.LayoutParams(-2, -2);
        timeParams.AddRule(LayoutRules.AlignParentRight);
        timeParams.AddRule(LayoutRules.AlignParentTop);
        timeView.LayoutParameters = timeParams;

        var completedInd = new View(ctx) { Id = IdCompleted, Background = new ColorDrawable(Color.Green) };
        var indParams = new RelativeLayout.LayoutParams(20, 20);
        indParams.AddRule(LayoutRules.AlignParentRight);
        indParams.AddRule(LayoutRules.AlignParentBottom);
        completedInd.LayoutParameters = indParams;

        root.AddView(albumArt);
        root.AddView(textContainer);
        root.AddView(timeView);
        root.AddView(completedInd);

        card.AddView(root);
        return card;
    }

    private View CreateLoadingView(Context ctx)
    {
        var root = new FrameLayout(ctx) { LayoutParameters = new ViewGroup.LayoutParams(-1, AppUtil.DpToPx(60)) };
        var pb = new ProgressBar(ctx) { Id = IdProgress, Indeterminate = true };
        var lp = new FrameLayout.LayoutParams(AppUtil.DpToPx(40), AppUtil.DpToPx(40)) { Gravity = GravityFlags.Center };
        pb.LayoutParameters = lp;
        root.AddView(pb);
        return root;
    }

    private View CreateErrorView(Context ctx)
    {
        var root = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        root.SetGravity(GravityFlags.Center);
        root.LayoutParameters = new ViewGroup.LayoutParams(-1, -2);
        root.SetPadding(40, 40, 40, 40);

        var tv = new TextView(ctx) { Id = IdErrorText, TextAlignment = Android.Views.TextAlignment.Center };
        var btn = new MaterialButton(ctx) { Id = IdRetryBtn, Text = "Retry" };

        root.AddView(tv);
        root.AddView(btn);
        return root;
    }

    public void Dispose() => _cleanup.Dispose();


    public void SetLoadingMore(bool isLoading)
    {
        if (_isLoadingMore == isLoading) return;
        _isLoadingMore = isLoading;

        // If you decide to add a loading footer item later, 
        // you would call NotifyItemInserted/Removed here.
        Debug.WriteLine($"[Adapter] Loading more state changed to: {isLoading}");
    }

    // --- ViewHolders ---

    public class PlayEventViewHolder : RecyclerView.ViewHolder
    {
        private readonly TextView _titleView, _artistView, _playTypeView, _timeView;
        private readonly ImageView _albumArtView;
        private readonly View _completedIndicator;
        private DimmerPlayEventView _currentItem;

        public PlayEventViewHolder(View v, IRealmFactory realmFactory) : base(v)
        {
            _titleView = v.FindViewById<TextView>(IdTitle);
            _artistView = v.FindViewById<TextView>(IdArtist);
            _playTypeView = v.FindViewById<TextView>(IdPlayType);
            _timeView = v.FindViewById<TextView>(IdTime);
            _albumArtView = v.FindViewById<ImageView>(IdAlbumArt);
            _completedIndicator = v.FindViewById<View>(IdCompleted);
            v.Click += (s, e) => MessagingCenter.Send(this, "PlayEventSelected", _currentItem);
        }

        public void Bind(DimmerPlayEventView item)
        {
            _currentItem = item;
            _titleView.Text = item.SongViewObject?.Title ?? "Unknown Song";
            _artistView.Text = item.SongViewObject?.ArtistName ?? "Unknown Artist";
            _playTypeView.Text = item.PlayTypeStr;
            _timeView.Text = item.EventDate?.ToString("t");
            _completedIndicator.Visibility = item.WasPlayCompleted ? ViewStates.Visible : ViewStates.Gone;

            if (!string.IsNullOrEmpty(item.SongViewObject?.CoverImagePath))
                Bumptech.Glide.Glide.With(ItemView.Context).Load(item.SongViewObject.CoverImagePath).Into(_albumArtView);
            else
                _albumArtView.SetImageResource(Android.Resource.Drawable.IcMenuGallery);
        }

        public void Unbind() => Bumptech.Glide.Glide.With(ItemView.Context).Clear(_albumArtView);
    }

    public class LoadingViewHolder : RecyclerView.ViewHolder
    {
        private readonly ProgressBar _pb;
        public LoadingViewHolder(View v) : base(v) => _pb = v.FindViewById<ProgressBar>(IdProgress);
        public void Bind(bool isLoading) => ItemView.Visibility = isLoading ? ViewStates.Visible : ViewStates.Gone;
    }

    public class ErrorViewHolder : RecyclerView.ViewHolder
    {
        private readonly TextView _tv;
        public ErrorViewHolder(View v) : base(v)
        {
            _tv = v.FindViewById<TextView>(IdErrorText);
            v.FindViewById<Button>(IdRetryBtn).Click += (s, e) => MessagingCenter.Send(this, "RetryLoad");
        }
        public void Bind(string message) => _tv.Text = message;
    }
}