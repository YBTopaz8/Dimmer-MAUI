using Android.Views;
using AndroidX.RecyclerView.Widget;
using Bumptech.Glide;
using Dimmer.Data.ModelView;
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

    private View CreateCarouselItem(Context ctx)
    {
        var card = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(20),
            Elevation = 8,
            Id = View.GenerateViewId()
        };

        var layoutParams = new ViewGroup.MarginLayoutParams(
            AppUtil.DpToPx(350),
            AppUtil.DpToPx(350)
        );
        layoutParams.SetMargins(AppUtil.DpToPx(20), 0, AppUtil.DpToPx(20), 0);
        card.LayoutParameters = layoutParams;

        var image = new ImageView(ctx)
        {
            Id = View.GenerateViewId()
        };
        image.SetScaleType(ImageView.ScaleType.CenterCrop);

        card.AddView(image, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent
        ));

        return card;
    }

    private class CarouselViewHolder : RecyclerView.ViewHolder
    {
        private readonly ImageView _image;
        private readonly BaseViewModelAnd _viewModel;

        public CarouselViewHolder(View itemView, BaseViewModelAnd viewModel) : base(itemView)
        {
            var card = itemView as MaterialCardView;
            _image = card?.GetChildAt(0) as ImageView ?? throw new InvalidOperationException("ImageView not found");
            _viewModel = viewModel;
        }

        public void Bind(SongModelView song, Context context)
        {
            if (!string.IsNullOrEmpty(song.CoverImagePath))
            {
                Glide.With(context).Load(song.CoverImagePath).Into(_image);
            }
            else
            {
                _image.SetImageResource(Resource.Drawable.musicnotess);
            }

            ItemView.Click -= OnItemClick;
            ItemView.Click += OnItemClick;
        }

        private void OnItemClick(object? sender, EventArgs e)
        {
            _viewModel.SelectedSong = _viewModel.CurrentPlayingSongView;
            if (ItemView.Context is TransitionActivity activity)
            {
                _viewModel.NavigateToAnyPageOfGivenType(
                    activity.SupportFragmentManager.Fragments.FirstOrDefault() as Fragment,
                    new SongDetailFragment("toSingleSongDetailsFromNowPlaying", _viewModel),
                    "toSingleSongDetailsFromNowPlaying"
                );
                activity.TogglePlayer();
            }
        }
    }
}
