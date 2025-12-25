using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

using AndroidX.Core.Content;
using AndroidX.Core.View;

using Bumptech.Glide;

using Dimmer.DimmerAudio;
using Dimmer.ViewsAndPages.NativeViews.Misc;
using Dimmer.WinUI.UiUtils;

using Google.Android.Material.Chip;

using Kotlin.Jvm;

using static Android.Provider.DocumentsContract;

using ImageButton = Android.Widget.ImageButton;
using ScrollView = Android.Widget.ScrollView;

namespace Dimmer.ViewsAndPages.NativeViews;




public partial class NowPlayingFragment : Fragment, IOnBackInvokedCallback
{
    private readonly BaseViewModelAnd _viewModel;
    private readonly CompositeDisposable _disposables = new();

    // --- UI REFERENCES ---
    private View _rootView;

    // Mini Player Views
    private View _miniPlayerContainer;
    private ImageView _miniCover;
    private TextView _miniTitle, _miniArtist;
    private MaterialButton _miniPlayBtn;
    MaterialButton _skipPrevBtn;
    MaterialButton _skipNextBtn;
    // Expanded Player Views
    private View _expandedContainer;
    private MaterialTextView _expandedTitle;
    private ChipGroup _artistChipGroup;
    private ImageView _mainCoverImage;
    private MaterialCardView _lyricsCard;
    private TextView _currentLyricText;
    private TextView _genreText, _yearText;

    // Controls
    private Slider _seekSlider;
    private TextView _currentTimeText, _totalTimeText;
    private MaterialButton _prevBtn, _playPauseBtn, _nextBtn;
    private MaterialButton _queueBtn, _detailsBtn, _optionsBtn;
    private Slider _volumeSlider;
    private MaterialButton _currentDeviceBtn, _shareBtn, _changeDeviceBtn;

    // State
    private bool _isDraggingSeek = false;
    private bool _isDraggingVolume;
    public NowPlayingFragment()
    {
        
    }
    public NowPlayingFragment(BaseViewModelAnd viewModel)
    {
        _viewModel = viewModel;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context;

        // Root is a FrameLayout to stack Mini and Expanded views
        var root = new FrameLayout(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        };
        root.SetBackgroundColor(UiBuilder.IsDark(this.Resources.Configuration) ? Android.Graphics.Color.ParseColor("#1E1E1E") : Android.Graphics.Color.White);
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

    private LinearLayout CreateMiniPlayer(Context ctx)
    {
        if(_viewModel is null)return null!;
        var layout = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, AppUtil.DpToPx(70)) { Gravity = GravityFlags.Top },
            WeightSum = 10
        };
        layout.SetPadding(20, 10, 20, 10);
        //layout.SetBackgroundColor(UiBuilder.IsDark(ctx) ? Color.ParseColor("#291B22") : Color.ParseColor("#DEDFF0"));
        layout.SetBackgroundColor(UiBuilder.IsDark(ctx) ? Color.ParseColor("#1a1a1a") : Color.ParseColor("#DEDFF0"));


        // Mini Cover
        var card = new MaterialCardView(ctx) { Radius = AppUtil.DpToPx(8), Elevation = 0 };
        _miniCover = new ImageView(ctx) { };
        _miniCover.SetScaleType(ImageView.ScaleType.CenterCrop);

        // Set Transition Name
        var tName = $"sharedImage_{_viewModel.CurrentPlayingSongView.Id}";
        ViewCompat.SetTransitionName(_miniCover, tName);
        card.Click += (s, e) =>
        {
            string? tName = ViewCompat.GetTransitionName(_miniCover);
            if (tName != null)
            {
                _viewModel.SelectedSong = _viewModel.CurrentPlayingSongView;
                _viewModel.NavigateToSingleSongPageFromHome(this, tName, _miniCover);
            }
        }
        ;

        card.AddView(_miniCover, new ViewGroup.LayoutParams(AppUtil.DpToPx(60), AppUtil.DpToPx(60)));
       

        layout.AddView(card, new LinearLayout.LayoutParams(AppUtil.DpToPx(60), AppUtil.DpToPx(60)) { Gravity = GravityFlags.CenterVertical });


        // Text Info
        var textStack = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        textStack.SetPadding(30, 0, 0, 0);
        _miniTitle = new TextView(ctx) { TextSize = 18, Typeface = Android.Graphics.Typeface.DefaultBold, Ellipsize = Android.Text.TextUtils.TruncateAt.Marquee };
        _miniTitle.SetSingleLine(true);
        _miniTitle.Selected = true; // For marquee

        _miniArtist = new TextView(ctx) { TextSize = 14, Alpha = 0.7f };
        textStack.AddView(_miniTitle);
        textStack.AddView(_miniArtist);

        var textParams = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 6f) { Gravity = GravityFlags.CenterVertical };
        layout.AddView(textStack, textParams);

        
        _miniPlayBtn= CreateControlButton(ctx, Resource.Drawable.media3_icon_play, 40, true);
        _miniPlayBtn.Click += async (s, e) =>
        {
            await _viewModel.PlayPauseToggleAsync();
        };

        _skipPrevBtn = CreateControlButton(ctx, Resource.Drawable.media3_icon_previous, 30);
        _skipPrevBtn.Click += async (s, e) =>
        {
            await _viewModel.PreviousTrackAsync();
        };
        
        _skipNextBtn = CreateControlButton(ctx,Resource.Drawable.media3_icon_next, 35);
        _skipNextBtn.Click += async (s, e) =>
        {
            await _viewModel.NextTrackAsync();
        };
        var lyParams= new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f) { Gravity = GravityFlags.Right };
        lyParams.SetMargins(10, 10, 10, 10);
        layout.AddView(_skipPrevBtn, lyParams);
        layout.AddView(_miniPlayBtn, lyParams);
        layout.AddView(_skipNextBtn, lyParams);

        return layout;
    }

    private ScrollView CreateExpandedPlayer(Context ctx)
    { 
        var scroll = new ScrollView(ctx) { FillViewport = true };
        scroll.LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);

        var root = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        root.SetPadding(40, 80, 40, 40); // Top padding for dragging handle area

        //root.SetBackgroundColor(UiBuilder.ThemedBGColor(ctx));
        root.SetBackgroundColor(UiBuilder.IsDark(ctx) ? Color.ParseColor("#1a1a1a") : Color.ParseColor("#DEDFF0"));


        // --- A. Marquee Title ---
        _expandedTitle = new MaterialTextView(ctx) { TextSize = 24, Typeface = Android.Graphics.Typeface.DefaultBold, Gravity = GravityFlags.Center };
        _expandedTitle.SetSingleLine(true);
        _expandedTitle.Ellipsize = Android.Text.TextUtils.TruncateAt.Marquee;
        _expandedTitle.Selected = true;
        root.AddView(_expandedTitle);

        // --- B. Image & Lyrics Grid ---
        var gridFrame = new FrameLayout(ctx);

        var gridParams = new LinearLayout.LayoutParams(-1, AppUtil.DpToPx(420));
        gridParams.SetMargins(0, 30, 0, 30);
        gridFrame.LayoutParameters = gridParams;

        var imgCard = new MaterialCardView(ctx) { Radius = AppUtil.DpToPx(20), Elevation = 4 };
        _mainCoverImage = new ImageView(ctx);
        _mainCoverImage.SetScaleType(ImageView.ScaleType.CenterCrop);
        imgCard.AddView(_mainCoverImage, new FrameLayout.LayoutParams(-1, -1));
        gridFrame.AddView(imgCard);

        // 2. Lyrics Overlay (MUST be added after image to be on top)
        _lyricsCard = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(12),
            CardBackgroundColor = ColorStateList.ValueOf(Color.ParseColor("#AA000000")), // Darker semi-transparent
            Elevation = 8,
            Visibility = ViewStates.Invisible // Hidden by default
        };
        var lParams = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
        {
            Gravity = GravityFlags.Center,
            LeftMargin = 40,
            RightMargin = 40
        };
        _currentLyricText = new TextView(ctx) { TextSize = 20, Gravity = GravityFlags.Center, Typeface = Typeface.DefaultBold };
        _currentLyricText.SetTextColor(Color.White);
        _currentLyricText.SetPadding(40, 30, 40, 30);
        _lyricsCard.AddView(_currentLyricText);

        gridFrame.AddView(_lyricsCard, lParams); // This sits on top of the image
        


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

        // --- C. Artist Row (Chips + Vertical Action Stack) ---
        var artistActionRow = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        artistActionRow.SetGravity(GravityFlags.CenterVertical);

        // 1. Artist Chips (Left side, weighted)
        var chipScroll = new HorizontalScrollView(ctx) { ScrollBarSize = 0 };
        _artistChipGroup = new ChipGroup(ctx) { SingleLine = true };
        chipScroll.AddView(_artistChipGroup);
        artistActionRow.AddView(chipScroll, new LinearLayout.LayoutParams(0, -2, 1f));

        // 2. Vertical Action Stack (Right side)
        var actionStack = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        actionStack.SetGravity(GravityFlags.Center);

        var favBtn = new ImageButton(ctx) { Background = null };
        favBtn.SetImageResource(Resource.Drawable.heart);
        favBtn.SetPadding(10, 10, 10, 10);
        favBtn.Click += async (s, e) => await _viewModel.AddFavoriteRatingToSong(_viewModel.CurrentPlayingSongView);

        var lyrBtn = new ImageButton(ctx) { Background = null };
        lyrBtn.SetImageResource(Resource.Drawable.lyrics); // Ensure this drawable exists
        lyrBtn.SetPadding(10, 10, 10, 10);
        lyrBtn.Click += (s, e) => {
            _lyricsCard.Visibility = _lyricsCard.Visibility == ViewStates.Visible ? ViewStates.Invisible : ViewStates.Visible;
        };

        actionStack.AddView(favBtn, new LinearLayout.LayoutParams(AppUtil.DpToPx(40), AppUtil.DpToPx(40)));
        actionStack.AddView(lyrBtn, new LinearLayout.LayoutParams(AppUtil.DpToPx(40), AppUtil.DpToPx(40)));

        artistActionRow.AddView(actionStack);
        root.AddView(artistActionRow);

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

        _prevBtn = CreateControlButton(ctx, Resource.Drawable.media3_icon_previous, 60);
        _playPauseBtn = CreateControlButton(ctx, Resource.Drawable.media3_icon_play, 80, true); // Larger, filled
        _nextBtn = CreateControlButton(ctx, Resource.Drawable.media3_icon_next, 60);

        controlsRow.AddView(_prevBtn);
        controlsRow.AddView(_playPauseBtn);
        controlsRow.AddView(_nextBtn);
        root.AddView(controlsRow);


        var volumeRow = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        volumeRow.SetGravity(GravityFlags.CenterVertical);
        volumeRow.SetPadding(0, 0, 0, 20);

        var volDownIcon = new ImageView(ctx);
        volDownIcon.SetImageResource(Resource.Drawable.volumesmall); // Make sure you have this icon
        volDownIcon.ImageTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Gray);
        volDownIcon.Click += (s, e) =>
        {
            _viewModel.DecreaseVolumeLevel();
        };

        _volumeSlider = new Slider(ctx);
        _volumeSlider.ValueFrom = 0; _volumeSlider.ValueTo = 100;
        _volumeSlider.Value = 50; // Default

        var volUpIcon = new ImageView(ctx);
        volUpIcon.SetImageResource(Resource.Drawable.volumeloud); // Make sure you have this icon
        volUpIcon.ImageTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Gray);
        volUpIcon.Click += (s, e) =>
        {
            _viewModel.IncreaseVolumeLevel();
        };


        volumeRow.AddView(volDownIcon, new LinearLayout.LayoutParams(AppUtil.DpToPx(24), AppUtil.DpToPx(24)));
        volumeRow.AddView(_volumeSlider, new LinearLayout.LayoutParams(0, -2, 1f)); // Stretch slider
        volumeRow.AddView(volUpIcon, new LinearLayout.LayoutParams(AppUtil.DpToPx(24), AppUtil.DpToPx(24)));

        root.AddView(volumeRow);

        var pillsRow = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        pillsRow.SetGravity(GravityFlags.Center);
        // Add some space at bottom for gesture bar
        pillsRow.SetPadding(0, 0, 0, AppUtil.DpToPx(30));

        // 1. Current Device Pill
        _currentDeviceBtn = CreatePillButton(ctx, "This Phone", Resource.Drawable.ic_vol_type_speaker_dark);
        // 2. Share Pill
        _shareBtn = CreatePillButton(ctx, "Share", Resource.Drawable.shared);
        _shareBtn.Click += async (s, e) =>
        {
            await _viewModel.ShareSongViewClipboard(_viewModel.CurrentPlayingSongView);
        };

        // 3. Switch Device Pill
        _changeDeviceBtn = CreatePillButton(ctx, "Cast", Resource.Drawable.sharing); // Or 'Change' icon

        // Distribute evenly with weights or margins
        var pillParams = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f);
        pillParams.SetMargins(AppUtil.DpToPx(4), 0, AppUtil.DpToPx(4), 0);

        pillsRow.AddView(_currentDeviceBtn, pillParams);
        pillsRow.AddView(_shareBtn, pillParams);
        pillsRow.AddView(_changeDeviceBtn, pillParams);

        root.AddView(pillsRow);

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

        _playPauseBtn.Click += async (s, e) =>
        {
            await _viewModel.PlayPauseToggleAsync();


        };
        _prevBtn.Click += async (s, e) => await _viewModel.PreviousTrackAsync();
        _nextBtn.Click += async (s, e) => await _viewModel.NextTrackAsync();

        _queueBtn.Click += (s, e) =>
        {
            var queueSheet = new QueueBottomSheetFragment(_viewModel);
            queueSheet.Show(ParentFragmentManager, "QueueSheet");
        };

        // Slider Logic
        _seekSlider.Touch += (s, e) =>
        {
            switch (e.Event?.Action)
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
        
        _volumeSlider.Touch += async (s, e) =>
        {
            switch (e.Event?.Action)
            {
                case MotionEventActions.Down: _isDraggingVolume = true; break;
                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                    _isDraggingVolume = false;
                    await Task.Delay(350); // delay so the value updates and we now get it to set new vol
                    var newVolume = (_volumeSlider.Value / 100);
                    _viewModel.SetVolumeLevel((double)newVolume);
                    break;
                default:
                    break;
            }
            e.Handled = false;
        };
        return scroll;
    }
    private MaterialButton CreatePillButton(Context ctx, string text, int iconRes)
    {
        var btn = new MaterialButton(ctx, null, Resource.Attribute.materialButtonTonalStyle); // Tonal style looks good for pills
        btn.Text = text;
        btn.SetIconResource(iconRes);
        btn.IconGravity = MaterialButton.IconGravityTextStart;
        btn.IconPadding = AppUtil.DpToPx(4);
        btn.CornerRadius = AppUtil.DpToPx(50); // Fully rounded (Pill)
        btn.SetPadding(AppUtil.DpToPx(12), 0, AppUtil.DpToPx(12), 0);
        btn.TextSize = 12;
        btn.SetSingleLine(true);
        btn.Ellipsize = Android.Text.TextUtils.TruncateAt.End;
        btn.InsetTop = 0;
        btn.InsetBottom = 0;
        return btn;
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
            btn.IconTint = Android.Content.Res.ColorStateList.ValueOf(UiBuilder.IsDark(this.Resources.Configuration) ? Android.Graphics.Color.White : Android.Graphics.Color.Black);
            btn.StrokeWidth = AppUtil.DpToPx(1);
            btn.StrokeColor = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Gray);
        }
        return btn;
    }

    public override void OnResume()
    {
        base.OnResume();

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
        {
            Activity?.OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(
                (int)IOnBackInvokedDispatcher.PriorityDefault, this);
        }


        // 1. Observe Playing Song
        _viewModel.CurrentSongChanged
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(UpdateSongUI)
            .DisposeWith(_disposables);


        _viewModel.AudioEngineIsPlayingObservable
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(isPlaying =>
            {
                if (_playPauseBtn == null || _miniPlayBtn == null) return;

                int iconRes = isPlaying
                    ? Resource.Drawable.media3_icon_pause
                    : Resource.Drawable.media3_icon_circular_play;

                // Native Android calls - much safer for local icons
                _playPauseBtn.SetIconResource(iconRes);
                //_miniPlayBtn.Icon = ContextCompat.GetDrawable(Context, iconRes);
                _miniPlayBtn.SetIconResource(iconRes);

                // Set Transition Name
                var tName = $"sharedImage_{_viewModel.CurrentPlayingSongView.Id}";
                ViewCompat.SetTransitionName(_miniCover, tName);
            })
            .DisposeWith(_disposables);
        Observable.FromEventPattern<(double newVol, bool isDeviceMuted, int devMavVol)>(
           h => _viewModel.AudioService.DeviceVolumeChanged += h,
           h => _viewModel.AudioService.DeviceVolumeChanged -= h)
           .Select(evt => evt.EventArgs)
           .ObserveOn(RxSchedulers.UI)
           .Subscribe(UpdateDeviceVolumeChangedUI, ex => Debug.WriteLine("error on vol changed"))
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
        if(_viewModel.IsDimmerPlaying)
        {
            if (_playPauseBtn == null || _miniPlayBtn == null) return;
            _miniPlayBtn.SetIconResource(Resource.Drawable.media3_icon_pause);
            _playPauseBtn.SetIconResource(Resource.Drawable.media3_icon_pause);
        }

        _viewModel.CurrentPlayingSongView
            .WhenPropertyChange
            (nameof(SongModelView.CoverImagePath), s => s.CoverImagePath)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(path =>
            {
                if (!string.IsNullOrEmpty(path))
                {
                    Glide.With(this).Load(path).Into(_miniCover);
                    Glide.With(this).Load(path).Into(_mainCoverImage);
                }
                else
                {
                    _miniCover.SetImageResource(Resource.Drawable.musicnotess);
                    _mainCoverImage.SetImageResource(Resource.Drawable.musicnotess);
                }
            });


    }

    private void UpdateDeviceVolumeChangedUI((double newVol, bool isDeviceMuted, int devMavVol) tuple)
    {
        var newVal = tuple.Item1;
        var devMaxVol = tuple.Item3;
        _volumeSlider.ValueTo = devMaxVol;
        _volumeSlider.Value = (float)newVal;
    }
    private void UpdateVolumeChangedUI(double obj)
    {
        _volumeSlider.ValueTo=(float)obj;
    }

    public override void OnPause()
    {
        base.OnPause();
        _disposables.Clear();
    }

    private void UpdateSongUI(SongModelView? song)
    {
        if (song == null) return;

        var songInDB = _viewModel.RealmFactory.GetRealmInstance()
            .Find<SongModel>(song.Id);

        
        // Mini Player
        _miniTitle.Text = song.Title;
        _miniArtist.Text = song.OtherArtistsName;

        // Expanded Player
        _expandedTitle.Text = song.Title;
        _genreText.Text = song.GenreName;
        _yearText.Text = song.ReleaseYear?.ToString();

        var totalTs = TimeSpan.FromSeconds(song.DurationInSeconds);
        _totalTimeText.Text = $"{totalTs.Minutes}:{totalTs.Seconds:D2}";

        // Artist Chips
        _artistChipGroup.RemoveAllViews();
        var chip = new Chip(Context) { Text = song.OtherArtistsName };
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

    public void OnBackInvoked()
    {
        var myAct = this.Activity as TransitionActivity;
        if (myAct != null)
        {
            if(myAct.SheetBehavior.State == BottomSheetBehavior.StateExpanded)
            {
                myAct.TogglePlayer();
                return;
            }
        }
    }
}