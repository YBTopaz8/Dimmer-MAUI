using Bumptech.Glide;
using Bumptech.Glide.Load.Resource.Bitmap;
using Dimmer.UiUtils;
using Google.Android.Material.Divider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        root.SetPadding(40, 20, 40, 40);

        // 1. SECTION: MORE FROM ALBUM
        var albumName = _vm.SelectedSong?.AlbumName ?? "Unknown Album";
        root.AddView(CreateSectionHeader(ctx, "More from Album", albumName));

        var albumRecycler = CreateHorizontalRecycler(ctx);
        var songInDb = _vm.RealmFactory.GetRealmInstance().Find<SongModel>(_vm.SelectedSong!.Id);
        var albumSongs = songInDb.Album.SongsInAlbum.AsEnumerable().Select(x => x.ToSongModelView()).ToList();
        albumRecycler.SetAdapter(new ModernRelatedAdapter(albumSongs, _vm));
        root.AddView(albumRecycler);

        var divb = new MaterialDivider(ctx) { Alpha = 0.1f };
        divb.SetPadding(0, 40, 0, 40);
        root.AddView(divb);

        // 2. SECTION: MORE BY ARTIST
        var artistName = _vm.SelectedSong?.ArtistName ?? "Unknown Artist";
        root.AddView(CreateSectionHeader(ctx, "Artist Spotlight", artistName));

        var artistRecycler = CreateHorizontalRecycler(ctx);
        // Logic to get artist songs (Example: from the same artist in DB)
        var artistSongs = songInDb.Artist.Songs.AsEnumerable().Select(x => x.ToSongModelView()!).ToList();
        if (artistSongs is not  null && artistSongs.Count > 0)
        {
            artistRecycler.SetAdapter(new ModernRelatedAdapter(artistSongs, _vm));
        }
        root.AddView(artistRecycler);

        // 3. SECTION: SIMILAR GENRE
        if (!string.IsNullOrEmpty(_vm.SelectedSong?.GenreName))
        {

            var diva = new MaterialDivider(ctx) { Alpha = 0.1f };
            diva.SetPadding(0, 40, 0, 40);
            root.AddView(diva);
            root.AddView(CreateSectionHeader(ctx, "Explore Genre", _vm.SelectedSong.GenreName));
            var genreRecycler = CreateHorizontalRecycler(ctx);
           
            root.AddView(genreRecycler);
        }

        scroll.AddView(root);
        return scroll;
    }

    private View CreateSectionHeader(Context ctx, string title, string subtitle)
    {
        var layout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        layout.SetPadding(0, 20, 0, 10);

        var titleView = new TextView(ctx) { Text = title, TextSize = 14 };
        titleView.SetTextColor(Color.ParseColor("#808080")); // Muted label
        titleView.SetTypeface(Typeface.DefaultBold, TypefaceStyle.Normal);
        titleView.LetterSpacing = 0.05f;

        var subView = new TextView(ctx) { Text = subtitle, TextSize = 22 };
        subView.SetTypeface(Typeface.DefaultBold, TypefaceStyle.Normal);
        subView.SetTextColor(UiBuilder.IsDark(ctx) ? Color.White : Color.Black);

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

        // This makes it feel like Spotify - cards snap to the center/start
        var snapHelper = new LinearSnapHelper();
        snapHelper.AttachToRecyclerView(recycler);

        return recycler;
    }

    // --- RE-ENGINEERED ADAPTER ---
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
            var ctx = parent.Context;

            var card = new MaterialCardView(ctx)
            {
                Radius = AppUtil.DpToPx(16),
                CardElevation = 0,
                StrokeWidth = 0,
            };
            card.SetCardBackgroundColor(Color.Transparent);
            var lp = new RecyclerView.LayoutParams(AppUtil.DpToPx(160), -2);
            lp.SetMargins(0, 0, AppUtil.DpToPx(16), 0);
            card.LayoutParameters = lp;

            var layout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };

            // Image with rounded corners via Glide
            var img = new ImageView(ctx) { LayoutParameters = new LinearLayout.LayoutParams(-1, AppUtil.DpToPx(160)) };
            img.SetScaleType(ImageView.ScaleType.CenterCrop);

            var title = new TextView(ctx) { TextSize = 14 };
            title.SetMaxLines (1);
            title.SetTypeface(Typeface.DefaultBold, TypefaceStyle.Normal);
            title.SetPadding(4, 12, 4, 0);
            title.Ellipsize = Android.Text.TextUtils.TruncateAt.End;

            var artist = new TextView(ctx) { TextSize = 12, Alpha = 0.6f};
            artist.SetMaxLines(1);
            artist.SetPadding(4, 4, 4, 8);
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

            // Use Glide for professional image loading with rounded transformation
            Glide.With(vh.ItemView.Context)
                .Load(item.CoverImagePath)
                .Transform(new CenterCrop(), new RoundedCorners(AppUtil.DpToPx(16)))
                .Placeholder(Resource.Drawable.musicaba)
                .Into(vh.Image);

            vh.ItemView.Click += async (s, e) =>
            {
                await _vm.PlaySongAsync(item, CurrentPage.SingleSongPage, _items);
            };
        }

        class RelatedVH : RecyclerView.ViewHolder
        {
            public ImageView Image { get; }
            public TextView Title { get; }
            public TextView Artist { get; }
            public RelatedVH(View v, ImageView i, TextView t, TextView a) : base(v)
            { Image = i; Title = t; Artist = a; }
        }
    }
}