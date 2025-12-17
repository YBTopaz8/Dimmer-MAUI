using Bumptech.Glide;

using ScrollView = Android.Widget.ScrollView;

namespace Dimmer.ViewsAndPages.NativeViews.ArtistSection;

public class ArtistFragment : Fragment
{
    private BaseViewModelAnd MyViewModel;
    private string _artistName;
    private string _artistId;

    public ArtistFragment(BaseViewModelAnd vm, string artistName, string artistId)
    {
        MyViewModel = vm;
        _artistName = artistName;
        _artistId = artistId;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context;
        var scrollView = new ScrollView(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent),
            FillViewport = true
        };

        var root = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };

        // --- 1. HEADER SECTION ---
        var headerLayout = new FrameLayout(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, AppUtil.DpToPx(300))
        };

        var artistImage = new ImageView(ctx)
        {
            LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent),
        };
        artistImage.SetScaleType(ImageView.ScaleType.CenterCrop);
        // Mock loading artist image (replace with actual logic)
        Glide.With(ctx).Load(Resource.Drawable.musicnotess).Into(artistImage);

        // Gradient Overlay for text readability
        var overlay = new View(ctx) { LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent) };
        overlay.SetBackgroundResource(Android.Resource.Drawable.ScreenBackgroundDarkTransparent); // Or custom gradient drawable

        var nameTxt = new MaterialTextView(ctx)
        {
            Text = _artistName,
            TextSize = 32,
            Typeface = Android.Graphics.Typeface.DefaultBold,
            LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
            { Gravity = GravityFlags.Bottom | GravityFlags.Left }
        };
        nameTxt.SetPadding(40, 0, 0, 40);
        nameTxt.SetTextColor(Android.Graphics.Color.White);

        headerLayout.AddView(artistImage);
        headerLayout.AddView(overlay);
        headerLayout.AddView(nameTxt);
        root.AddView(headerLayout);

        // --- 2. ALBUMS (Horizontal List) ---
        var albumLabel = new MaterialTextView(ctx) { Text = "Albums", TextSize = 20 };
        albumLabel.SetPadding(30, 30, 30, 10);
        root.AddView(albumLabel);

        var albumsRecycler = new RecyclerView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, AppUtil.DpToPx(160))
        };
        albumsRecycler.SetLayoutManager(new LinearLayoutManager(ctx, LinearLayoutManager.Horizontal, false));

        // Filter songs by artist to get albums
        var artistSongs = MyViewModel.SearchResults.Where(s => s.ArtistName == _artistName).ToList();
        var albums = artistSongs.Select(s => s.AlbumName).Distinct().ToList();

        albumsRecycler.SetAdapter(new SimpleAlbumAdapter(albums));
        root.AddView(albumsRecycler);

        // --- 3. SONGS (Vertical List) ---
        var songsLabel = new MaterialTextView(ctx) { Text = "Top Songs", TextSize = 20 };
        songsLabel.SetPadding(30, 30, 30, 10);
        root.AddView(songsLabel);

        var songsRecycler = new RecyclerView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        songsRecycler.NestedScrollingEnabled = false; // Important inside ScrollView
        songsRecycler.SetLayoutManager(new LinearLayoutManager(ctx));

        // Re-using your existing SongAdapter logic, or a simplified one
        // Assuming SongAdapter exists as per your provided code
        // var songAdapter = new SongAdapter(ctx, MyViewModel, this); 
        // For this example, I'll use a placeholder to ensure it compiles without your full SongAdapter code
        songsRecycler.SetAdapter(new SimpleSongListAdapter(artistSongs.Take(5).ToList(), MyViewModel));
        root.AddView(songsRecycler);

        // --- 4. STATS SECTION ---
        var statsCard = new Google.Android.Material.Card.MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(16),
            CardElevation = AppUtil.DpToPx(6),
            UseCompatPadding = true,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        var statsLayout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        statsLayout.SetPadding(40, 40, 40, 40);

        statsLayout.AddView(new TextView(ctx) { Text = "Artist Stats", TextSize = 18, Typeface = Android.Graphics.Typeface.DefaultBold });
        statsLayout.AddView(CreateStatRow(ctx, "Total Plays", artistSongs.Sum(x => x.PlayCompletedCount).ToString()));
        statsLayout.AddView(CreateStatRow(ctx, "Total Skips", artistSongs.Sum(x => x.SkipCount).ToString()));
        statsLayout.AddView(CreateStatRow(ctx, "Library Tracks", artistSongs.Count.ToString()));

        statsCard.AddView(statsLayout);
        root.AddView(statsCard);

        // Add padding at bottom so player doesn't cover content
        root.SetPadding(0, 0, 0, AppUtil.DpToPx(100));

        scrollView.AddView(root);
        return scrollView;
    }

    private View CreateStatRow(Context ctx, string label, string value)
    {
        var row = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        row.SetPadding(0, 10, 0, 10);
        var t1 = new TextView(ctx) { Text = label + ": ", Typeface = Android.Graphics.Typeface.DefaultBold };
        var t2 = new TextView(ctx) { Text = value };
        row.AddView(t1);
        row.AddView(t2);
        return row;
    }

    // --- Simple Adapters for this View ---
    class SimpleAlbumAdapter : RecyclerView.Adapter
    {
        List<string> _albums;
        public SimpleAlbumAdapter(List<string> a) { _albums = a; }
        public override int ItemCount => _albums.Count;
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var card = new Google.Android.Material.Card.MaterialCardView(parent.Context)
            {
                Radius = 20,
                LayoutParameters = new ViewGroup.LayoutParams(300, 300)
            };

            var t = card.LayoutParameters;
            if(t is (ViewGroup.MarginLayoutParams))
            {
                var s = (ViewGroup.MarginLayoutParams)t;
                s.SetMargins(10, 10, 10, 10);
            }
            var txt = new TextView(parent.Context) { Gravity = GravityFlags.Center, TextSize = 12 };
            card.AddView(txt);
            return new VH(card, txt);
        }
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = holder as VH;
            vh.Text.Text = _albums[position];
            // Load Album Art here if available
        }
        class VH : RecyclerView.ViewHolder { public TextView Text; public VH(View v, TextView t) : base(v) { Text = t; } }
    }

    class SimpleSongListAdapter : RecyclerView.Adapter
    {
        List<SongModelView> _songs;
        BaseViewModelAnd _vm;
        public SimpleSongListAdapter(List<SongModelView> s, BaseViewModelAnd vm) { _songs = s; _vm = vm; }
        public override int ItemCount => _songs.Count;
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var tv = new TextView(parent.Context) { TextSize = 16 };
            tv.SetPadding(30, 20, 30, 20);
            return new VH(tv);
        }
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            (holder as VH).Text.Text = $"{position + 1}. {_songs[position].Title}";
            holder.ItemView.Click += async (s, e) => await _vm.PlaySongAsync(_songs[position], CurrentPage.AllSongs, _songs);
        }
        class VH : RecyclerView.ViewHolder { public TextView Text; public VH(View v) : base(v) { Text = (TextView)v; } }
    }
}