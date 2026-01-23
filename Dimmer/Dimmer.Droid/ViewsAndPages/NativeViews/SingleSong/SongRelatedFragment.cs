using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Bumptech.Glide;
using Dimmer.UiUtils;

namespace Dimmer.ViewsAndPages.NativeViews.SingleSong;

public class SongRelatedFragment : Fragment
{
    private readonly BaseViewModelAnd _vm;
    public SongRelatedFragment(BaseViewModelAnd vm) { _vm = vm; }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context;
        var scroll = new Android.Widget.ScrollView(ctx);
        var root = new LinearLayout(ctx) { Orientation = Orientation.Vertical};
        root.SetPadding(20,20, 20, 20);
        // 1. More from Album
        root.AddView(new TextView(ctx) { Text = "From the same Album", TextSize = 18, Typeface = Typeface.DefaultBold });
        var albumRecycler = new RecyclerView(ctx) { LayoutParameters = new LinearLayout.LayoutParams(-1, AppUtil.DpToPx(180)) }; // Horizontal Height
        albumRecycler.SetLayoutManager(new LinearLayoutManager(ctx, LinearLayoutManager.Horizontal, false));

        // Filter Logic (Simulated - ensure VM has this data ready)
        var albumSongs = _vm.SearchResults.Where(s => s.AlbumName == _vm.SelectedSong?.AlbumName && s.Id != _vm.SelectedSong?.Id).ToList();
        // Reuse your SimpleSongListAdapter but adapted for horizontal cards, or create a specific one
        albumRecycler.SetAdapter(new HorizontalCoverAdapter(albumSongs, _vm));
        root.AddView(albumRecycler);

        // 2. Similar Tracks (LastFM)
        var space = new Space(ctx) { LayoutParameters = new LinearLayout.LayoutParams(-1, 40) };
        root.AddView(space);

        root.AddView(new TextView(ctx) { Text = "Similar Tracks", TextSize = 18, Typeface = Typeface.DefaultBold });
        // You would bind this to _vm.SelectedSongLastFMData.SimilarTracks
        // For now, placeholder text to indicate parity intent
        root.AddView(new TextView(ctx) { Text = "Similar tracks feature coming soon via LastFM API linkage."});

        scroll.AddView(root);
        return scroll;
    }

    // Simple Horizontal Adapter for Album Peers
    class HorizontalCoverAdapter : RecyclerView.Adapter
    {
        List<SongModelView> _items;
        BaseViewModelAnd _vm;
        public HorizontalCoverAdapter(List<SongModelView> items, BaseViewModelAnd vm) { _items = items; _vm = vm; }
        public override int ItemCount => _items.Count;
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var ctx = parent.Context;
            var ll = new LinearLayout(ctx);

            var img = new ImageView(parent.Context) { LayoutParameters = new ViewGroup.LayoutParams(300, 300)};
            img.SetScaleType(ImageView.ScaleType.CenterCrop);
            
            ll.AddView(img);
            var txtView = new TextView(ctx);
            txtView.Selected = true;
            txtView.Ellipsize = Android.Text.TextUtils.TruncateAt.Marquee;
            ll.AddView(txtView);
            
           MaterialCardView? cardView = UiBuilder.CreateCard(ctx);


            cardView.AddView(ll);
            
            return new SimpleVH(cardView, img, txtView); // Reuse SimpleVH or define local
        }
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var img = (ImageView)holder.ItemView;
            if (!string.IsNullOrEmpty(_items[position].CoverImagePath))
                Glide.With(img.Context).Load(_items[position].CoverImagePath).Into(img);

            img.Click += async (s, e) =>
            {
                await _vm.PlaySongAsync(_items[position], CurrentPage.AllSongs);
            };
        }
        class SimpleVH : RecyclerView.ViewHolder 
        {
            private readonly CardView _container;
            private readonly ImageView img;
            private readonly TextView txtView;

            public SimpleVH(MaterialCardView v, ImageView img, TextView txtView) : base(v) 
            {
                this._container = v;
                this.img = img;
                this.txtView = txtView;
            } 
        }
    }
}