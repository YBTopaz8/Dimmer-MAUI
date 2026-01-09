using Android.Graphics;
using Android.Widget;
using AndroidX.CardView.Widget;
using Bumptech.Glide;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.Button;
using Hqub.Lastfm.Entities;

namespace Dimmer.ViewsAndPages.NativeViews.DimmerLive;

public class LastFmTrackInfoBottomSheet : BottomSheetDialogFragment
{
    private readonly Track _track;
    private ImageView _coverArtImage;
    private TextView _titleText;
    private TextView _artistText;
    private TextView _albumText;
    private LinearLayout _lovedPanel;
    private LinearLayout _nowPlayingPanel;
    private TextView _listenersText;
    private TextView _playCountText;

    public LastFmTrackInfoBottomSheet(Track track)
    {
        _track = track;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context;
        var root = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent)
        };
        root.SetPadding(AppUtil.DpToPx(20), AppUtil.DpToPx(20), AppUtil.DpToPx(20), AppUtil.DpToPx(20));

        // Header
        var headerText = new TextView(ctx)
        {
            Text = "Track Information from Last.fm",
            TextSize = 18,
            Typeface = Typeface.DefaultBold,
            Gravity = GravityFlags.Center
        };
        headerText.SetTextColor(Color.White);
        root.AddView(headerText);

        // Cover Art Container
        var coverContainer = new FrameLayout(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                AppUtil.DpToPx(200))
        };

        var coverCard = new CardView(ctx)
        {
            LayoutParameters = new FrameLayout.LayoutParams(
                AppUtil.DpToPx(200),
                AppUtil.DpToPx(200))
            {
                Gravity = GravityFlags.Center
            },
            Radius = AppUtil.DpToPx(8)
        };

        _coverArtImage = new ImageView(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };
        _coverArtImage.SetScaleType(ImageView.ScaleType.CenterCrop);

        coverCard.AddView(_coverArtImage);
        coverContainer.AddView(coverCard);
        root.AddView(coverContainer);

        // Title
        _titleText = new TextView(ctx)
        {
            TextSize = 20,
            Typeface = Typeface.DefaultBold,
            Gravity = GravityFlags.Center
        };
        _titleText.SetTextColor(Color.White);
        _titleText.SetPadding(0, AppUtil.DpToPx(12), 0, AppUtil.DpToPx(4));
        root.AddView(_titleText);

        // Artist
        _artistText = new TextView(ctx)
        {
            TextSize = 16,
            Gravity = GravityFlags.Center
        };
        _artistText.SetTextColor(Color.Gray);
        _artistText.SetPadding(0, 0, 0, AppUtil.DpToPx(4));
        root.AddView(_artistText);

        // Album
        _albumText = new TextView(ctx)
        {
            TextSize = 14,
            Gravity = GravityFlags.Center
        };
        _albumText.SetTextColor(Color.DarkGray);
        _albumText.SetPadding(0, 0, 0, AppUtil.DpToPx(12));
        root.AddView(_albumText);

        // Status Icons Container
        var statusContainer = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent)
        };
        statusContainer.SetGravity(GravityFlags.Center);
        statusContainer.SetPadding(0, 0, 0, AppUtil.DpToPx(12));

        // Loved Panel
        _lovedPanel = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            Visibility = ViewStates.Gone
        };
        _lovedPanel.SetPadding(AppUtil.DpToPx(8), 0, AppUtil.DpToPx(8), 0);

        var lovedIcon = new ImageView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(
                AppUtil.DpToPx(20),
                AppUtil.DpToPx(20))
        };
        lovedIcon.SetImageResource(Resource.Drawable.heartlock);
        lovedIcon.ImageTintList = Android.Content.Res.ColorStateList.ValueOf(Color.Red);

        var lovedText = new TextView(ctx)
        {
            Text = " Loved",
            TextSize = 14
        };
        lovedText.SetTextColor(Color.White);

        _lovedPanel.AddView(lovedIcon);
        _lovedPanel.AddView(lovedText);
        statusContainer.AddView(_lovedPanel);

        // Now Playing Panel
        _nowPlayingPanel = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            Visibility = ViewStates.Gone
        };
        _nowPlayingPanel.SetPadding(AppUtil.DpToPx(8), 0, AppUtil.DpToPx(8), 0);

        var nowPlayingIcon = new ImageView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(
                AppUtil.DpToPx(20),
                AppUtil.DpToPx(20))
        };
        nowPlayingIcon.SetImageResource(Resource.Drawable.playcircle);
        nowPlayingIcon.ImageTintList = Android.Content.Res.ColorStateList.ValueOf(Color.Green);

        var nowPlayingText = new TextView(ctx)
        {
            Text = " Now Playing",
            TextSize = 14
        };
        nowPlayingText.SetTextColor(Color.White);

        _nowPlayingPanel.AddView(nowPlayingIcon);
        _nowPlayingPanel.AddView(nowPlayingText);
        statusContainer.AddView(_nowPlayingPanel);

        root.AddView(statusContainer);

        // Statistics Container
        var statsContainer = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent),
            WeightSum = 2
        };
        statsContainer.SetPadding(0, AppUtil.DpToPx(12), 0, AppUtil.DpToPx(12));

        // Listeners Card
        var listenersCard = CreateStatCard(ctx, "Listeners");
        _listenersText = listenersCard.FindViewById<TextView>(Resource.Id.stat_value);
        statsContainer.AddView(listenersCard);

        // Play Count Card
        var playCountCard = CreateStatCard(ctx, "Total Scrobbles");
        _playCountText = playCountCard.FindViewById<TextView>(Resource.Id.stat_value);
        statsContainer.AddView(playCountCard);

        root.AddView(statsContainer);

        // Info Message
        var infoCard = new MaterialCardView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent),
            Radius = AppUtil.DpToPx(8),
            CardElevation = AppUtil.DpToPx(2),
            CardBackgroundColor = Android.Content.Res.ColorStateList.ValueOf(Color.ParseColor("#2196F3"))
        };

        var infoLayout = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical
        };
        infoLayout.SetPadding(AppUtil.DpToPx(12), AppUtil.DpToPx(12), AppUtil.DpToPx(12), AppUtil.DpToPx(12));

        var infoTitle = new TextView(ctx)
        {
            Text = "Track not available locally",
            TextSize = 14,
            Typeface = Typeface.DefaultBold
        };
        infoTitle.SetTextColor(Color.White);

        var infoMessage = new TextView(ctx)
        {
            Text = "This track is not in your local library.",
            TextSize = 12
        };
        infoMessage.SetTextColor(Color.White);

        infoLayout.AddView(infoTitle);
        infoLayout.AddView(infoMessage);
        infoCard.AddView(infoLayout);
        root.AddView(infoCard);

        // Populate data
        PopulateTrackInfo();

        return root;
    }

    private CardView CreateStatCard(Context ctx, string label)
    {
        var card = new MaterialCardView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(
                0,
                ViewGroup.LayoutParams.WrapContent,
                1f),
            Radius = AppUtil.DpToPx(8),
            CardElevation = AppUtil.DpToPx(2),
            CardBackgroundColor = Android.Content.Res.ColorStateList.ValueOf(Color.ParseColor("#303030"))
        };
        ((LinearLayout.LayoutParams)card.LayoutParameters).SetMargins(
            AppUtil.DpToPx(4), 0, AppUtil.DpToPx(4), 0);

        var layout = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical
        };
        layout.SetGravity(GravityFlags.Center);
        layout.SetPadding(AppUtil.DpToPx(12), AppUtil.DpToPx(12), AppUtil.DpToPx(12), AppUtil.DpToPx(12));

        var labelText = new TextView(ctx)
        {
            Text = label,
            TextSize = 12,
            Gravity = GravityFlags.Center
        };
        labelText.SetTextColor(Color.Gray);

        var valueText = new TextView(ctx)
        {
            Text = "N/A",
            TextSize = 18,
            Typeface = Typeface.DefaultBold,
            Gravity = GravityFlags.Center,
            Id = Resource.Id.stat_value
        };
        valueText.SetTextColor(Color.White);

        layout.AddView(labelText);
        layout.AddView(valueText);
        card.AddView(layout);

        return card;
    }

    private void PopulateTrackInfo()
    {
        if (_track == null)
            return;

        // Title
        _titleText.Text = _track.Name ?? "Unknown Track";

        // Artist
        _artistText.Text = _track.Artist?.Name ?? "Unknown Artist";

        // Album
        if (!string.IsNullOrEmpty(_track.Album?.Name))
        {
            _albumText.Text = _track.Album.Name;
        }
        else
        {
            _albumText.Visibility = ViewStates.Gone;
        }

        // Cover Art
        if (_track.Images != null && _track.Images.Count > 0)
        {
            var imageUrl = _track.Images.LastOrDefault()?.Url;
            if (!string.IsNullOrEmpty(imageUrl))
            {
                Glide.With(this)
                    .Load(imageUrl)
                    .Placeholder(Resource.Drawable.musicnotess)
                    .Into(_coverArtImage);
            }
        }
        else
        {
            _coverArtImage.SetImageResource(Resource.Drawable.musicnotess);
        }

        // Loved Status
        if (_track.UserLoved)
        {
            _lovedPanel.Visibility = ViewStates.Visible;
        }

        // Now Playing Status
        if (_track.NowPlaying)
        {
            _nowPlayingPanel.Visibility = ViewStates.Visible;
        }

        // Statistics
        if (_track.Statistics != null)
        {
            if (_track.Statistics.Listeners > 0)
                _listenersText.Text = _track.Statistics.Listeners.ToString("N0");

            if (_track.Statistics.PlayCount > 0)
                _playCountText.Text = _track.Statistics.PlayCount.ToString("N0");
        }
    }
}
