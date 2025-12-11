using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

using Bumptech.Glide;

using Dimmer.Utils.UIUtils;
using Dimmer.ViewsAndPages.NativeViews.Misc;

using Google.Android.Material.Chip;

using ImageButton = Android.Widget.ImageButton;
using ScrollView = Android.Widget.ScrollView;

namespace Dimmer.ViewsAndPages.NativeViews;




public class NowPlayingFragment : Fragment
{
    private readonly BaseViewModelAnd _viewModel;
    private readonly CompositeDisposable _disposables = new();

    // --- UI REFERENCES ---
    private View _rootView;

    // Mini Player Views
    private View _miniPlayerContainer;
    private ImageView _miniCover;
    private TextView _miniTitle, _miniArtist;
    private ImageButton _miniPlayBtn;

    // Expanded Player Views
    private View _expandedContainer;
    private MaterialTextView _expandedTitle;
    private ChipGroup _artistChipGroup;
    private ImageView _mainCoverImage;
    private MaterialCardView _lyricsCard;
    private TextView _currentLyricText;
    private TextView _genreText, _yearText;

    // Controls
    private Slider _seekSlider, _volumeSlider;
    private TextView _currentTimeText, _totalTimeText;
    private MaterialButton _prevBtn, _playPauseBtn, _nextBtn;
    private MaterialButton _queueBtn, _detailsBtn, _optionsBtn;

    // State
    private bool _isDraggingSeek = false;

    public NowPlayingFragment(BaseViewModelAnd viewModel)
    {
        _viewModel = viewModel;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context;

        // Root is a FrameLayout to stack Mini and Expanded views
        var root = new FrameLayout(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        };
        root.SetBackgroundColor(IsDark() ? Android.Graphics.Color.ParseColor("#1E1E1E") : Android.Graphics.Color.White);
        _rootView = root;

        // 1. Build Mini Player (Visible when collapsed)
        _miniPlayerContainer = CreateMiniPlayer(ctx);
        root.AddView(_miniPlayerContainer);

        // 2. Build Expanded Player (Visible when sliding up)
        _expandedContainer = CreateExpandedPlayer(ctx);
        _expandedContainer.Alpha = 0f; // Hidden initially
        _expandedContainer.Visibility = ViewStates.Invisible;
        root.AddView(_expandedContainer);

        return root;
    }

    private View CreateMiniPlayer(Context ctx)
    {
        var layout = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, AppUtil.DpToPx(70)) { Gravity = GravityFlags.Top },
            WeightSum = 10
        };
        layout.SetPadding(20, 10, 20, 10);
        layout.SetBackgroundColor(IsDark() ? Android.Graphics.Color.ParseColor("#2D2D2D") : Android.Graphics.Color.LightGray);

        // Mini Cover
        var card = new MaterialCardView(ctx) { Radius = AppUtil.DpToPx(8), Elevation = 0 };
        _miniCover = new ImageView(ctx) { };
        _miniCover.SetScaleType(ImageView.ScaleType.CenterCrop);
        card.AddView(_miniCover, new ViewGroup.LayoutParams(AppUtil.DpToPx(50), AppUtil.DpToPx(50)));
        layout.AddView(card, new LinearLayout.LayoutParams(AppUtil.DpToPx(50), AppUtil.DpToPx(50)) { Gravity = GravityFlags.CenterVertical });

        // Text Info
        var textStack = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        textStack.SetPadding(30, 0, 0, 0);
        _miniTitle = new TextView(ctx) { TextSize = 16, Typeface = Android.Graphics.Typeface.DefaultBold, Ellipsize = Android.Text.TextUtils.TruncateAt.Marquee };
        _miniTitle.SetSingleLine(true);
        _miniTitle.Selected = true; // For marquee

        _miniArtist = new TextView(ctx) { TextSize = 12, Alpha = 0.7f };
        textStack.AddView(_miniTitle);
        textStack.AddView(_miniArtist);

        var textParams = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 7f) { Gravity = GravityFlags.CenterVertical };
        layout.AddView(textStack, textParams);

        // Play Button
        _miniPlayBtn = new ImageButton(ctx) { Background = null };
        _miniPlayBtn.SetImageResource(Android.Resource.Drawable.IcMediaPlay);
        _miniPlayBtn.Click += async (s, e) =>
        {
            await _viewModel.PlayPauseToggleAsync();
            await Task.Delay(1000);
            if(_viewModel.IsDimmerPlaying)
                Glide.With(this).Load(Resource.Drawable.media3_icon_pause).Into(_miniPlayBtn);
            else
                Glide.With(this).Load(Resource.Drawable.media3_icon_play).Into(_miniPlayBtn);
        };
        layout.AddView(_miniPlayBtn, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 2f) { Gravity = GravityFlags.CenterVertical });

        return layout;
    }

    private View CreateExpandedPlayer(Context ctx)
    {
        var scroll = new ScrollView(ctx) { FillViewport = true };
        scroll.LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);

        var root = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        root.SetPadding(40, 80, 40, 40); // Top padding for dragging handle area

        // --- A. Marquee Title ---
        _expandedTitle = new MaterialTextView(ctx) { TextSize = 24, Typeface = Android.Graphics.Typeface.DefaultBold, Gravity = GravityFlags.Center };
        _expandedTitle.SetSingleLine(true);
        _expandedTitle.Ellipsize = Android.Text.TextUtils.TruncateAt.Marquee;
        _expandedTitle.Selected = true;
        root.AddView(_expandedTitle);

        // --- B. Image & Lyrics Grid ---
        var gridFrame = new FrameLayout(ctx);
        var gridParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, AppUtil.DpToPx(350)); // Fixed height or adjust based on screen
        gridParams.TopMargin = 40;
        gridParams.BottomMargin = 40;
        gridFrame.LayoutParameters = gridParams;

        // 1. Main Cover Image
        var imgCard = new MaterialCardView(ctx) { Radius = AppUtil.DpToPx(24), Elevation = 10 };
        _mainCoverImage = new ImageView(ctx) { };
        _mainCoverImage.SetScaleType(ImageView.ScaleType.CenterCrop);
        imgCard.AddView(_mainCoverImage, new ViewGroup.LayoutParams(-1, -1));
        gridFrame.AddView(imgCard, new FrameLayout.LayoutParams(-1, -1));

        // 2. Lyrics Overlay (Centered Card)
        _lyricsCard = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(13),
            CardBackgroundColor = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.ParseColor("#CC000000")), // Semi-transparent black
            Elevation = 20
        };
        var lyricsParams = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent) { Gravity = GravityFlags.Center };
        // Constrain max width for lyrics card
        lyricsParams.LeftMargin = 40;
        lyricsParams.RightMargin = 40;
        _lyricsCard.LayoutParameters = lyricsParams;

        _currentLyricText = new TextView(ctx)
        {
            TextSize = 18,
            Gravity = GravityFlags.Center,
            Typeface = Android.Graphics.Typeface.DefaultBold
        };
        _currentLyricText.SetTextColor(Android.Graphics.Color.White);
        _currentLyricText.SetPadding(30, 20, 30, 20);

        _lyricsCard.AddView(_currentLyricText);
        gridFrame.AddView(_lyricsCard);

        // 3. Meta Data (Bottom of Grid)
        var metaLay = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        var metaParams = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent) { Gravity = GravityFlags.Bottom };
        metaParams.SetMargins(20, 0, 20, 20);
        metaLay.LayoutParameters = metaParams;

        _genreText = new TextView(ctx) { };
        _yearText = new TextView(ctx) { Gravity = GravityFlags.End };

        metaLay.AddView(_genreText, new LinearLayout.LayoutParams(0, -2, 1));
        metaLay.AddView(_yearText, new LinearLayout.LayoutParams(0, -2, 1));
        gridFrame.AddView(metaLay);

        root.AddView(gridFrame);

        // --- C. Artist Chips ---
        var chipScroll = new HorizontalScrollView(ctx) { ScrollBarSize = 0 };
        _artistChipGroup = new ChipGroup(ctx) { SingleLine = true };
        chipScroll.AddView(_artistChipGroup);
        root.AddView(chipScroll);

        // --- D. Progress Slider ---
        var timeLay = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        timeLay.SetPadding(0, 20, 0, 0);
        _currentTimeText = new TextView(ctx) { Text = "0:00", TextSize = 12 };
        _totalTimeText = new TextView(ctx) { Text = "0:00", TextSize = 12, Gravity = GravityFlags.End };

        timeLay.AddView(_currentTimeText, new LinearLayout.LayoutParams(0, -2, 1));
        timeLay.AddView(_totalTimeText, new LinearLayout.LayoutParams(0, -2, 1));
        root.AddView(timeLay);

        _seekSlider = new Slider(ctx);
        _seekSlider.ValueFrom = 0;
        _seekSlider.ValueTo = 100;
        // Logic attached in Resume
        root.AddView(_seekSlider);

        // --- E. Playback Controls (MD3 Style) ---
        var controlsRow = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        controlsRow.SetGravity(GravityFlags.Center);
        controlsRow.SetPadding(0, 20, 0, 40);

        _prevBtn = CreateControlButton(ctx, Android.Resource.Drawable.IcMediaPrevious, 60);
        _playPauseBtn = CreateControlButton(ctx, Android.Resource.Drawable.IcMediaPlay, 80, true); // Larger, filled
        _nextBtn = CreateControlButton(ctx, Android.Resource.Drawable.IcMediaNext, 60);

        controlsRow.AddView(_prevBtn);
        controlsRow.AddView(_playPauseBtn);
        controlsRow.AddView(_nextBtn);
        root.AddView(controlsRow);

        // --- F. Bottom Actions (Queue, View Song, Options) ---
        var actionRow = new LinearLayout(ctx) { Orientation = Orientation.Horizontal, WeightSum = 3 };

        _queueBtn = new MaterialButton(ctx, null, Resource.Attribute.materialIconButtonFilledStyle) { Text = "Queue" }; // Use Icon Button Style if possible
        _queueBtn.SetIconResource(Resource.Drawable.hamburgermenu); // Ensure drawable

        _detailsBtn = new MaterialButton(ctx) { Text = "Details" };
        _optionsBtn = new MaterialButton(ctx) { Text = "..." };

        // Distribute buttons
        var p = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1);
        p.SetMargins(10, 0, 10, 0);

        actionRow.AddView(_queueBtn, p);
        actionRow.AddView(_detailsBtn, p);
        actionRow.AddView(_optionsBtn, p);

        root.AddView(actionRow);

        // --- G. Volume (Optional) ---
        // _volumeSlider = ... (similar logic)

        scroll.AddView(root);
        return scroll;
    }

    private MaterialButton CreateControlButton(Context ctx, int iconRes, int sizeDp, bool isPrimary = false)
    {
        var btn = new MaterialButton(ctx);
        btn.Icon = AndroidX.Core.Content.ContextCompat.GetDrawable(ctx, iconRes);
        btn.IconGravity = MaterialButton.IconGravityTextStart;
        btn.IconPadding = 0;
        btn.InsetTop = 0;
        btn.InsetBottom = 0;

        var sizePx = AppUtil.DpToPx(sizeDp);
        btn.LayoutParameters = new LinearLayout.LayoutParams(sizePx, sizePx) { LeftMargin = 20, RightMargin = 20 };
        btn.CornerRadius = sizePx / 2;

        if (isPrimary)
        {
            btn.SetBackgroundColor(Android.Graphics.Color.DarkSlateBlue);
            btn.IconTint = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.White);
        }
        else
        {
            btn.SetBackgroundColor(Android.Graphics.Color.Transparent);
            btn.IconTint = Android.Content.Res.ColorStateList.ValueOf(IsDark() ? Android.Graphics.Color.White : Android.Graphics.Color.Black);
            btn.StrokeWidth = AppUtil.DpToPx(1);
            btn.StrokeColor = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Gray);
        }
        return btn;
    }

    public override void OnResume()
    {
        base.OnResume();

        // 1. Observe Playing Song
        _viewModel.CurrentSongChanged
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(song => UpdateSongUI(song))
            .DisposeWith(_disposables);

        // 2. Observe Playback Position (for Slider & Lyrics)
        _viewModel.AudioEnginePositionObservable
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(pos =>
            {
                if (!_isDraggingSeek && _viewModel.CurrentPlayingSongView?.DurationInSeconds > 0)
                {
                    var perc = (pos / _viewModel.CurrentPlayingSongView.DurationInSeconds) * 100;
                    _seekSlider.Value = (float)Math.Clamp(perc, 0, 100);

                    var ts = TimeSpan.FromSeconds(pos);
                    _currentTimeText.Text = $"{ts.Minutes}:{ts.Seconds:D2}";
                }

                // Update Lyric Text (Mock logic - replace with real synchronized lyric find)
                if (_viewModel.CurrentLine != null)
                {
                    _currentLyricText.Text = _viewModel.CurrentLine.Text;
                    _lyricsCard.Visibility = ViewStates.Visible;
                }
                else
                {
                    _lyricsCard.Visibility = ViewStates.Invisible;
                }
            })
            .DisposeWith(_disposables);

        // 3. Bind Buttons
        _playPauseBtn.Click += async (s, e) =>
        {
            await _viewModel.PlayPauseToggleAsync();

            await Task.Delay(1000);
            if (_viewModel.IsDimmerPlaying)
                Glide.With(this).Load(Resource.Drawable.media3_icon_pause).Into(_miniPlayBtn);
            else
                Glide.With(this).Load(Resource.Drawable.media3_icon_play).Into(_miniPlayBtn);
        };
        _prevBtn.Click += async (s, e) => await _viewModel.PreviousTrackASync();
        _nextBtn.Click += async (s, e) => await _viewModel.NextTrackAsync();

        _queueBtn.Click += (s, e) =>
        {
            var queueSheet = new QueueBottomSheetFragment(_viewModel);
            queueSheet.Show(ParentFragmentManager, "QueueSheet");
        };

        // Slider Logic
        _seekSlider.Touch += (s, e) =>
        {
            switch (e.Event.Action)
            {
                case MotionEventActions.Down: _isDraggingSeek = true; break;
                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                    _isDraggingSeek = false;
                    if (_viewModel.CurrentPlayingSongView != null)
                    {
                        var newPos = (_seekSlider.Value / 100) * _viewModel.CurrentPlayingSongView.DurationInSeconds;
                        _viewModel.SeekTrackPositionCommand.Execute(newPos);
                    }
                    break;
            }
            e.Handled = false;
        };
    }

    public override void OnPause()
    {
        base.OnPause();
        _disposables.Clear();
    }

    private void UpdateSongUI(SongModelView? song)
    {
        if (song == null) return;

        // Mini Player
        _miniTitle.Text = song.Title;
        _miniArtist.Text = song.ArtistName;

        // Expanded Player
        _expandedTitle.Text = song.Title;
        _genreText.Text = song.GenreName;
        _yearText.Text = song.ReleaseYear?.ToString();

        var totalTs = TimeSpan.FromSeconds(song.DurationInSeconds);
        _totalTimeText.Text = $"{totalTs.Minutes}:{totalTs.Seconds:D2}";

        // Artist Chips
        _artistChipGroup.RemoveAllViews();
        var chip = new Chip(Context) { Text = song.ArtistName };
        _artistChipGroup.AddView(chip);

        // Load Images
        if (!string.IsNullOrEmpty(song.CoverImagePath))
        {
            Glide.With(this).Load(song.CoverImagePath).Into(_miniCover);
            Glide.With(this).Load(song.CoverImagePath).Into(_mainCoverImage);
        }
        else
        {
            _miniCover.SetImageResource(Resource.Drawable.musicnotess);
            _mainCoverImage.SetImageResource(Resource.Drawable.musicnotess);
        }
    }

    // --- ANIMATION LOGIC ---
    // Called by TransitionActivity's BottomSheetCallback
    public void AnimateTransition(float slideOffset)
    {
        // slideOffset: 0.0 (Collapsed) -> 1.0 (Expanded)

        // Fade out Mini Player (0 -> 0.2 fade out fast)
        _miniPlayerContainer.Alpha = 1f - (slideOffset * 5f);
        _miniPlayerContainer.Visibility = _miniPlayerContainer.Alpha <= 0 ? ViewStates.Invisible : ViewStates.Visible;

        // Fade in Expanded Player (0.2 -> 1.0)
        _expandedContainer.Alpha = Math.Max(0, (slideOffset - 0.2f) / 0.8f);
        _expandedContainer.Visibility = _expandedContainer.Alpha <= 0 ? ViewStates.Invisible : ViewStates.Visible;
    }

    private bool IsDark() => (Resources.Configuration.UiMode & Android.Content.Res.UiMode.NightMask) == Android.Content.Res.UiMode.NightYes;
}