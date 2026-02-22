using Android.Views;
using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Card;
using Google.Android.Material.Divider;
using Bumptech.Glide.Load.Resource.Bitmap;

namespace Dimmer.ViewsAndPages.NativeViews.SingleSong;

public class SongRelatedFragment : Fragment
{
    private readonly BaseViewModelAnd _vm;
    public SongRelatedFragment(BaseViewModelAnd vm) { _vm = vm; }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context!;
        var scroll = new Android.Widget.ScrollView(ctx) { FillViewport = true };

        var root = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        root.LayoutParameters = new FrameLayout.LayoutParams(-1, -2);
        root.SetPadding(40, 20, 40, 200); // Bottom padding for safety

        var songInDb = _vm.RealmFactory.GetRealmInstance()!.Find<SongModel>(_vm.SelectedSong!.Id);
        if (songInDb == null) return scroll;

        // 1. MORE FROM ALBUM
        var albumName = _vm.SelectedSong?.AlbumName ?? "Unknown Album";
        root.AddView(CreateSectionHeader(ctx, "More from Album", albumName));

        var albumSongs = songInDb.Album?.SongsInAlbum.AsEnumerable().Select(x => x.ToSongModelView()).ToList() ?? new();
        var albumRecycler = CreateHorizontalRecycler(ctx);
        albumRecycler.SetAdapter(new ModernRelatedAdapter(albumSongs, _vm));
        root.AddView(albumRecycler);

        root.AddView(CreateDivider(ctx));

        // 2. MORE BY ARTIST
        var artistName = _vm.SelectedSong?.ArtistName ?? "Unknown Artist";
        root.AddView(CreateSectionHeader(ctx, "Artist Spotlight", artistName));

        var artistSongs = songInDb.Artist?.Songs.AsEnumerable().Select(x => x.ToSongModelView()).ToList() ?? new();
        var artistRecycler = CreateHorizontalRecycler(ctx);
        artistRecycler.SetAdapter(new ModernRelatedAdapter(artistSongs, _vm));
        root.AddView(artistRecycler);

        // 3. EXPLORE GENRE (Fixed missing logic)
        if (!string.IsNullOrEmpty(_vm.SelectedSong?.GenreName))
        {
            root.AddView(CreateDivider(ctx));
            root.AddView(CreateSectionHeader(ctx, "Explore Genre", _vm.SelectedSong.GenreName));

            var genreRecycler = CreateHorizontalRecycler(ctx);
            // Example of filtering DB by Genre Name
            var genreSongs = _vm.RealmFactory.GetRealmInstance()!.All<SongModel>()
                .Where(x => x.GenreName == _vm.SelectedSong.GenreName && x.Id != songInDb.Id)
                .Take(15) // Limit to 15 to keep it fast
                .AsEnumerable()
                .Select(x => x.ToSongModelView())
                .ToList();

            genreRecycler.SetAdapter(new ModernRelatedAdapter(genreSongs, _vm));
            root.AddView(genreRecycler);
        }

        scroll.AddView(root);
        return scroll;
    }

    private View CreateDivider(Context ctx)
    {
        var div = new MaterialDivider(ctx) { Alpha = 0.1f };
        div.SetPadding(0, AppUtil.DpToPx(24), 0, AppUtil.DpToPx(24));
        return div;
    }

    private View CreateSectionHeader(Context ctx, string title, string subtitle)
    {
        var layout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        layout.SetPadding(0, AppUtil.DpToPx(10), 0, AppUtil.DpToPx(10));

        var titleView = new TextView(ctx) { Text = title, TextSize = 12 };
        titleView.SetTextColor(Color.Gray);
        titleView.SetTypeface(Typeface.DefaultBold, TypefaceStyle.Normal);
        titleView.LetterSpacing = 0.05f;

        var subView = new TextView(ctx) { Text = subtitle, TextSize = 20 };
        subView.SetTypeface(Typeface.DefaultBold, TypefaceStyle.Normal);
        subView.SetTextColor(UiBuilder.IsDark(this.View) ? Color.White : Color.Black);

        layout.AddView(titleView);
        layout.AddView(subView);
        return layout;
    }

    private RecyclerView CreateHorizontalRecycler(Context ctx)
    {
        var recycler = new RecyclerView(ctx);
        var layoutManager = new LinearLayoutManager(ctx, LinearLayoutManager.Horizontal, false);
        recycler.SetLayoutManager(layoutManager);
        recycler.LayoutParameters = new LinearLayout.LayoutParams(-1, -2);
        recycler.SetClipToPadding(false);
        recycler.SetPadding(0, 10, 0, 10);

        var snapHelper = new LinearSnapHelper();
        snapHelper.AttachToRecyclerView(recycler);

        // FIX: The Magic Touch Listener that prevents ViewPager2 swipe conflicts!
        recycler.AddOnItemTouchListener(new HorizontalScrollTouchListener());

        return recycler;
    }

    // --- FIX FOR VIEWPAGER2 SWIPE CONFLICT ---
    class HorizontalScrollTouchListener : Java.Lang.Object, RecyclerView.IOnItemTouchListener
    {
        private float _startX;
        private float _startY;

        public bool OnInterceptTouchEvent(RecyclerView rv, MotionEvent e)
        {
            switch (e.ActionMasked)
            {
                case MotionEventActions.Down:
                    _startX = e.GetX();
                    _startY = e.GetY();
                    // Don't intercept immediately, just record start
                    rv.Parent?.RequestDisallowInterceptTouchEvent(true);
                    break;
                case MotionEventActions.Move:
                    float dx = Math.Abs(e.GetX() - _startX);
                    float dy = Math.Abs(e.GetY() - _startY);

                    // If user is swiping horizontally more than vertically, lock the ViewPager
                    if (dx > dy)
                    {
                        rv.Parent?.RequestDisallowInterceptTouchEvent(true);
                    }
                    else
                    {
                        rv.Parent?.RequestDisallowInterceptTouchEvent(false);
                    }
                    break;
                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                    rv.Parent?.RequestDisallowInterceptTouchEvent(false);
                    break;
            }
            return false;
        }
        public void OnTouchEvent(RecyclerView rv, MotionEvent e) { }
        public void OnRequestDisallowInterceptTouchEvent(bool disallowIntercept) { }
    }

    // --- MODERN ADAPTER ---
    class ModernRelatedAdapter : RecyclerView.Adapter
    {
        private readonly List<SongModelView> _items;
        private readonly BaseViewModelAnd _vm;

        public ModernRelatedAdapter(List<SongModelView> items, BaseViewModelAnd vm)
        {
            _items = items;
            _vm = vm;
        }

        public override int ItemCount => _items.Count;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var ctx = parent.Context!;
            var card = new MaterialCardView(ctx) { Radius = AppUtil.DpToPx(12), CardElevation = 0, StrokeWidth = 0 };
            card.SetCardBackgroundColor(Color.Transparent);

            var lp = new RecyclerView.LayoutParams(AppUtil.DpToPx(140), -2); // Slightly wider card
            lp.SetMargins(0, 0, AppUtil.DpToPx(16), 0);
            card.LayoutParameters = lp;

            var layout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };

            var img = new ImageView(ctx) { LayoutParameters = new LinearLayout.LayoutParams(-1, AppUtil.DpToPx(140)) };
            img.SetScaleType(ImageView.ScaleType.CenterCrop);

            var title = new TextView(ctx) { TextSize = 14 };
            title.SetMaxLines(1);
            title.SetTypeface(Typeface.DefaultBold, TypefaceStyle.Normal);
            title.SetPadding(0, AppUtil.DpToPx(8), 0, 0);
            title.Ellipsize = Android.Text.TextUtils.TruncateAt.End;

            var artist = new TextView(ctx) { TextSize = 12, Alpha = 0.6f };
            artist.SetMaxLines(1);
            artist.Ellipsize = Android.Text.TextUtils.TruncateAt.End;

            layout.AddView(img);
            layout.AddView(title);
            layout.AddView(artist);
            card.AddView(layout);

            return new RelatedVH(card, img, title, artist);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = (RelatedVH)holder;
            var item = _items[position];

            vh.Title.Text = item.Title;
            vh.Artist.Text = item.ArtistName;

            Glide.With(vh.ItemView.Context)
                .Load(item.CoverImagePath)
                .Transform(new CenterCrop(), new RoundedCorners(AppUtil.DpToPx(12)))
                .Placeholder(Resource.Drawable.musicaba) // Placeholder
                .Into(vh.Image);

            // Setup click handler to play song
            vh.ItemView.Click -= vh.OnClick; // Prevent memory leak
            vh.OnClick = async (s, e) =>
            {
                // Note: passing _items makes it so the new queue is just these related songs!
                await _vm.PlaySongAsync(item, CurrentPage.SingleSongPage, new System.Collections.ObjectModel.ObservableCollection<SongModelView>(_items));
            };
            vh.ItemView.Click += vh.OnClick;
        }

        class RelatedVH : RecyclerView.ViewHolder
        {
            public ImageView Image { get; }
            public TextView Title { get; }
            public TextView Artist { get; }
            public EventHandler? OnClick;

            public RelatedVH(View v, ImageView i, TextView t, TextView a) : base(v)
            { Image = i; Title = t; Artist = a; }
        }
    }
}