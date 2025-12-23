using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Text;
using System.Threading.Tasks;

using Bumptech.Glide;

using Dimmer.WinUI.UiUtils;

using DynamicData;

namespace Dimmer.ViewsAndPages.NativeViews.DimsSection;


internal class PlayEventAdapter : RecyclerView.Adapter
{
    private readonly ReadOnlyObservableCollection<DimmerPlayEventView> _events;
    private readonly CompositeDisposable _disposables = new();
    private Context _ctx;
    private BaseViewModelAnd _vm;
    private Fragment _parent;

    public PlayEventAdapter(Context ctx, BaseViewModelAnd vm, Fragment parent)
    {
        _ctx = ctx;
        _vm = vm;
        _parent = parent;

        _vm.DimmerPlayEvtsHolder.Connect()
            .ObserveOn(RxSchedulers.UI)            
            .Bind(out _events)
            .Subscribe(_ => NotifyDataSetChanged()) // Simplified for brevity, use change-tracking logic if needed
            .DisposeWith(_disposables);
    }

    public override int ItemCount => _events.Count;

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        // 1. Root Container (The "Row")
        var rowRoot = new MaterialCardView(_ctx)
        {
            Radius = 0, // Table rows usually sharp
            CardElevation = 0,
            StrokeWidth = 1,
            StrokeColor = Color.ParseColor("#10000000"),
            LayoutParameters = new RecyclerView.LayoutParams(-1, AppUtil.DpToPx(120)) // Height matching XAML
        };

        var layout = new LinearLayout(_ctx) { Orientation = Orientation.Horizontal };
        layout.SetGravity(GravityFlags.CenterVertical);
        layout.SetPadding(20, 10, 20, 10);

        // COLUMN 1: Image (Fixed Width/Height)
        var imgBorder = new MaterialCardView(_ctx) { Radius = AppUtil.DpToPx(8), CardElevation = 0 };
        var songImg = new ImageView(_ctx);
        songImg.SetScaleType(ImageView.ScaleType.CenterCrop);
        imgBorder.AddView(songImg, new ViewGroup.LayoutParams(AppUtil.DpToPx(90), AppUtil.DpToPx(90)));
        layout.AddView(imgBorder);

        // COLUMN 2: Texts (Weight = 1, like Width="*")
        var textCol = new LinearLayout(_ctx) { Orientation = Orientation.Vertical };
        var textParams = UiBuilder.Params(0, -2, AppUtil.DpToPx(12));
        textParams.Weight = 1;
        textCol.LayoutParameters = textParams;
        // Title + Fav Row
        var titleRow = new LinearLayout(_ctx) { Orientation = Orientation.Horizontal };
        var titleTxt = new TextView(_ctx) { TextSize = 20, Typeface = Typeface.DefaultBold };
        titleTxt.SetTextColor(UiBuilder.IsDark(_ctx) ? Color.White : Color.Black);

        var favIcon = new TextView(_ctx) { Text = "❤", TextSize = 16 }; // Use FontIcon if available
        favIcon.SetTextColor(Color.DarkSlateBlue);
        favIcon.Rotation = 37;
        titleRow.AddView(titleTxt);
        titleRow.AddView(favIcon);

        var artistTxt = new TextView(_ctx) { TextSize = 18 };
        var albumTxt = new TextView(_ctx) { TextSize = 12 };
        albumTxt.SetTypeface(null, TypefaceStyle.Italic);
        albumTxt.SetTextColor(Color.Gray);

        textCol.AddView(titleRow);
        textCol.AddView(artistTxt);
        textCol.AddView(albumTxt);
        layout.AddView(textCol);

        // COLUMN 3: More (Width="Auto")
        var moreCol = new LinearLayout(_ctx) { Orientation = Orientation.Vertical };
        moreCol.SetGravity(GravityFlags.End | GravityFlags.CenterVertical);

        var timeAgoTxt = new TextView(_ctx) { TextSize = 12 };
        var moreBtn = UiBuilder.CreateMaterialButton(_ctx, _ctx.Resources.Configuration, sizeDp: 40, iconRes: Resource.Drawable.more1);

        moreCol.AddView(timeAgoTxt);
        moreCol.AddView(moreBtn);
        layout.AddView(moreCol);

        rowRoot.AddView(layout);
        return new PlayEventViewHolder(rowRoot, songImg, titleTxt, favIcon, artistTxt, albumTxt, timeAgoTxt, moreBtn);
    }

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        if (holder is PlayEventViewHolder vh)
        {
            var item = _events[position];
            vh.Bind(item, _vm);
        }
    }

    class PlayEventViewHolder : RecyclerView.ViewHolder
    {
        ImageView _img;
        TextView _title, _fav, _artist, _album, _time;
        MaterialButton _more;

        public PlayEventViewHolder(View v, ImageView i, TextView t, TextView f, TextView art, TextView alb, TextView tm, MaterialButton m) : base(v)
        {
            _img = i; _title = t; _fav = f; _artist = art; _album = alb; _time = tm; _more = m;
        }

        public void Bind(DimmerPlayEventView item, BaseViewModelAnd vm)
        {
            _title.Text = item.SongViewObject?.Title;
            _artist.Text = item.SongViewObject?.OtherArtistsName;
            _album.Text = item.SongViewObject?.AlbumName;
            _fav.Visibility = (item.SongViewObject?.IsFavorite ?? false) ? ViewStates.Visible : ViewStates.Gone;

            // TimeAgo Logic (Mimicking your XAML converter)
            _time.Text = GetTimeAgo(item.EventDate);

            if (!string.IsNullOrEmpty(item.CoverImagePath))
                Glide.With(ItemView.Context).Load(item.CoverImagePath).Into(_img);

            // Click Handlers
            _title.Click += (s, e) => { /* SongTitle_Click logic */ };
            _artist.Click += (s, e) => { /* SongArtists_Click logic */ };
            _more.Click += (s, e) => { /* MoreBtn_Click logic */ };
        }

        private string GetTimeAgo(DateTimeOffset? date)
        {
            if (date == null) return "";
            var span = DateTimeOffset.Now - date.Value;
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            return date.Value.ToString("MMM dd");
        }
    }
}