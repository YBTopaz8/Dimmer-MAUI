using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;
using Bumptech.Glide;
using Dimmer.Data.ModelView;
using Dimmer.ViewModels;
using Dimmer.ViewsAndPages.NativeViews.Activity;
using Google.Android.Material.Card;

namespace Dimmer.ViewsAndPages.NativeViews;

public class NowPlayingCarouselAdapter : RecyclerView.Adapter
{
    private List<SongModelView> _songs = new();
    private readonly Context _context;
    private readonly BaseViewModelAnd _viewModel;

    public NowPlayingCarouselAdapter(Context context, BaseViewModelAnd viewModel)
    {
        _context = context;
        _viewModel = viewModel;
    }

    public void UpdateSongs(List<SongModelView> songs)
    {
        _songs = songs ?? new List<SongModelView>();
        NotifyDataSetChanged();
    }

    public override int ItemCount => _songs.Count;

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        if (holder is CarouselViewHolder viewHolder && position < _songs.Count)
        {
            var song = _songs[position];
            viewHolder.Bind(song, _context);
        }
    }

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        return new CarouselViewHolder(CreateCarouselItem(parent.Context!), _viewModel);
    }

    private FrameLayout CreateCarouselItem(Context ctx)
    {
        var wrapper = new FrameLayout(ctx);
        wrapper.LayoutParameters = new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent);

        var card = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(20),
            Elevation = 8,
            Id = View.GenerateViewId()
        };

        var cardParams = new FrameLayout.LayoutParams(
        AppUtil.DpToPx(450),
        AppUtil.DpToPx(400)
    );

        // Center the card inside the wrapper
        cardParams.Gravity = GravityFlags.Center;

        wrapper.AddView(card, cardParams);
        var image = new ImageView(ctx)
        {
            Id = View.GenerateViewId()
        };
        image.SetScaleType(ImageView.ScaleType.CenterCrop);

        card.AddView(image, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent
        ));

        return wrapper;
    }

    private class CarouselViewHolder : RecyclerView.ViewHolder
    {
        private readonly ImageView _image;
        private readonly BaseViewModelAnd _viewModel;

        public CarouselViewHolder(FrameLayout itemView, BaseViewModelAnd viewModel) : base(itemView)
        {
            _viewModel = viewModel;

            var wrapper = itemView as ViewGroup;
            var card = wrapper?.GetChildAt(0) as MaterialCardView;
            _image = card?.GetChildAt(0) as ImageView ?? throw new InvalidOperationException("ImageView not found");
        }

        public void Bind(SongModelView song, Context context)
        {
            if (!string.IsNullOrEmpty(song.CoverImagePath))
            {
                Glide.With(context).Load(song.CoverImagePath).Into(_image);
            }
            else
            {
                //_image.SetImageResource(Resource.Drawable.musicnotess);
                _image.Background = null;
            }


        }

        

    }
}
