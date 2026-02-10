using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Threading.Tasks;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.Content;
using AndroidX.Core.View;

using Bumptech.Glide;

using Dimmer.DimmerAudio;
using Dimmer.UiUtils;
using Dimmer.Utilities;
using Dimmer.Utils.Extensions;
using Dimmer.ViewsAndPages.NativeViews.Misc;
using DynamicData;
using Google.Android.Material.Chip;
using Google.Android.Material.ProgressIndicator;
using Google.Android.Material.Tooltip;
using ImageButton = Android.Widget.ImageButton;
using Math = System.Math;
using ScrollView = Android.Widget.ScrollView;

namespace Dimmer.ViewsAndPages.NativeViews;




public partial class NowPlayingFragment : Fragment, IOnBackInvokedCallback
{
    private readonly BaseViewModelAnd MyViewModel;
    private readonly CompositeDisposable _disposables = new();

    // --- UI REFERENCES ---
    private FrameLayout _rootView;
    private ImageView _backgroundImageView;

    // Mini Player Views
    private LinearLayout _miniPlayerContainer;
    private ImageView _miniCover;
    private TextView _miniTitle, _miniArtist;
    private MaterialButton _miniPlayBtn;
    private Button _shuffleBtn;
    MaterialButton _skipPrevBtn;
    MaterialButton _skipNextBtn;
    // Expanded Player Views
    private ScrollView _expandedContainer;
    private MaterialTextView _expandedTitle;
    private ChipGroup _artistChipGroup;
    private ImageView _mainCoverImage;
    private MaterialCardView _lyricsCard;
    private TextView _currentLyricText;
    

    // Controls
    private Slider _seekSlider;
    private TextView _currentTimeText, _totalTimeText;
    private MaterialButton _prevBtn, _playPauseBtn, _nextBtn;
    //private MaterialButton _queueBtn, _detailsBtn, _optionsBtn;
    private Slider _volumeSlider;
    private MaterialButton _queueBtn, _loveBtn, _toggleLyricsViewBtn;

    // State
    private bool _isDraggingSeek = false;
    private bool _isDraggingVolume;
    private Button _repeatBtn;
    private TextView _currentMiniLyricText;

    public DimmerSliderListener SeekListener { get; private set; }
    public CardView formatCard { get; private set; }
    public TextView formatViewText { get; private set; }
    public Button audioDevicesPill { get; private set; }
    public Chip SelectedAudioTextView { get; private set; }

    public NowPlayingFragment()
    {

        MyViewModel ??= MainApplication.ServiceProvider.GetService<BaseViewModelAnd>()!;
    }
    public NowPlayingFragment(BaseViewModelAnd viewModel)
    {
        MyViewModel = viewModel;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        Context ctx = Context!;

        // Root is a FrameLayout to stack Mini and Expanded views
        var root = new FrameLayout(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        };
        root.SetBackgroundColor(UiBuilder.IsDark(ctx) ? Color.ParseColor("#0D0E20") : Color.ParseColor("#CAD3DA"));


        _backgroundImageView = new ImageView(ctx)
        {
            LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent),

        }; 
        _backgroundImageView.SetScaleType(ImageView.ScaleType.CenterCrop);
       
        root.AddView(_backgroundImageView);
        _rootView = root;

        // 1. Build Mini Player (Visible when collapsed)
        _miniPlayerContainer = CreateMiniPlayer(ctx);
        if (_miniPlayerContainer is null)
        {
            root.AddView(_miniPlayerContainer);
        }
        else
        {
            root.AddView(_miniPlayerContainer);
        }
        // 2. Build Expanded Player (Visible when sliding up)
        _expandedContainer = CreateExpandedPlayer(ctx);
        _expandedContainer.Alpha = 0f; // Hidden initially
        _expandedContainer.Visibility = ViewStates.Invisible;
        root.AddView(_expandedContainer);

        return root;
    }
    
    private LinearLayout CreateMiniPlayer(Context ctx)
    {
        if(MyViewModel is null) return new LinearLayout(ctx);

        var layout = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, AppUtil.DpToPx(70)) { Gravity = GravityFlags.Top },
            WeightSum = 10
        };
        layout.SetPadding(20, 10, 20, 10);
        layout.SetBackgroundColor(Color.Transparent);


        // Mini Cover
        var card = new MaterialCardView(ctx) { Radius = AppUtil.DpToPx(8), Elevation = 0 };
        _miniCover = new ImageView(ctx) { };
        _miniCover.SetScaleType(ImageView.ScaleType.CenterCrop);
        _miniCover.SetBackgroundColor(Color.Transparent);
        // Set Transition Name
        var tName = $"sharedImage_{MyViewModel.CurrentPlayingSongView.Id}";
        ViewCompat.SetTransitionName(_miniCover, tName);
        card.Click += (s, e) =>
        {
            if (string.IsNullOrEmpty(MyViewModel.CurrentPlayingSongView.TitleDurationKey)) return;
            string? tName = ViewCompat.GetTransitionName(_miniCover);
            if (tName != null)
            {
                MyViewModel.SelectedSong = MyViewModel.CurrentPlayingSongView;
                MyViewModel.NavigateToSingleSongPageFromHome(this, tName, _miniCover);
            }
        }
        ;

        card.AddView(_miniCover, new ViewGroup.LayoutParams(AppUtil.DpToPx(60), AppUtil.DpToPx(60)));
       

        layout.AddView(card, new LinearLayout.LayoutParams(AppUtil.DpToPx(60), AppUtil.DpToPx(60)) { Gravity = GravityFlags.CenterVertical });


        // Text Info
        var textStack = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        textStack.SetPadding(30, 0, 0, 0);
        _miniTitle = new TextView(ctx) { TextSize = 20, Typeface = Android.Graphics.Typeface.DefaultBold, Ellipsize = Android.Text.TextUtils.TruncateAt.Marquee };
        _miniTitle.Selected = true; // For marquee
        _miniTitle.SetSingleLine(true);

        _miniArtist = new TextView(ctx) { TextSize = 14, Alpha = 0.7f };
        _miniArtist.SetSingleLine(true);
        _miniArtist.Selected = true; // For marquee
        _miniArtist.Clickable = true;

        _miniArtist.Click += (s, e) =>
        {
            switch (_currentMiniLyricText.Visibility)
            {
                case ViewStates.Gone:                    
                case ViewStates.Invisible:
                    _currentMiniLyricText.Visibility = ViewStates.Gone;
                    break;
                case ViewStates.Visible:
                    _currentMiniLyricText.Visibility = ViewStates.Visible;
                    break;
                default:
                    break;
            }

        };
        _miniArtist.LongClickable = true;
        _miniArtist.LongClick += (s, e) =>
        {

            var artistPickBtmSheet = new ArtistPickerBottomSheet(MyViewModel, MyViewModel.CurrentPlayingSongView.ArtistsInDB(MyViewModel.RealmFactory));

            artistPickBtmSheet.Show(this.ParentFragmentManager, "QueueSheet");
        };

        _currentMiniLyricText = new TextView(ctx) { TextSize = 14, Alpha = 0.7f };
        _currentMiniLyricText.Selected = true; // For marquee
        _currentMiniLyricText.SetSingleLine(true);
        _currentMiniLyricText.Clickable = true;
        _currentMiniLyricText.Visibility = ViewStates.Gone;

        _currentMiniLyricText.LongClick += (s, e) =>
        {
            MyViewModel.NavigateToAnyPageOfGivenType(this, new LyricsViewFragment(MyViewModel),
            "ToLyricsFromMiniPlayer");
        };
        _currentMiniLyricText.Click += (s, e) =>
        {
            switch (_currentMiniLyricText.Visibility)
            {
                case ViewStates.Gone:
                case ViewStates.Invisible:
                    _currentMiniLyricText.Visibility = ViewStates.Visible;
                    break;
                case ViewStates.Visible:
                    _currentMiniLyricText.Visibility = ViewStates.Gone;
                    break;
                default:
                    break;
            }


        };
        _miniTitle.Click += (sender, e) =>
        {

            TransitionActivity act = this.Activity as TransitionActivity;
            act.TogglePlayer();
        };
        textStack.AddView(_miniTitle);
        textStack.AddView(_currentMiniLyricText);
        textStack.AddView(_miniArtist);

        var textParams = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 7f) { Gravity = GravityFlags.CenterVertical };
        layout.AddView(textStack, textParams);

        
        _miniPlayBtn= CreateControlButton(ctx, Resource.Drawable.media3_icon_play, 50, true);
     
        _miniPlayBtn.Click += async (s, e) =>
        {
            await MyViewModel.PlayPauseToggleAsync();
        };
        _miniPlayBtn.IconSize = AppUtil.DpToPx(20);
        _miniPlayBtn.StrokeWidth = 0;
       
        _skipPrevBtn = CreateControlButton(ctx, Resource.Drawable.media3_icon_previous, AppUtil.DpToPx(50));
        _skipPrevBtn.StrokeWidth = 0;
        _skipPrevBtn.Click += async (s, e) =>
        {
            await MyViewModel.PreviousTrackAsync();
        };
        //_skipPrevBtn.SetWidth(AppUtil.DpToPx(30));

        _skipPrevBtn.IconTint = AppUtil.ToColorStateList(Color.DarkSlateBlue);
        
        _skipNextBtn = CreateControlButton(ctx,Resource.Drawable.media3_icon_next, 50);
        _skipNextBtn.Click += async (s, e) =>
        {
            await MyViewModel.NextTrackAsync();
        };
        _skipNextBtn.StrokeWidth = 0;
        _skipNextBtn.IconTint = AppUtil.ToColorStateList(Color.DarkSlateBlue);


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

        if (MyViewModel.CurrentPlaySongDominantColor is not null)
        {
            root.SetBackgroundColor(Color.ParseColor(

            MyViewModel.CurrentPlaySongDominantColor.ToHex())
);
        }
        //root.SetBackgroundColor(UiBuilder.IsDark(ctx) ? Color.ParseColor("#1a1a1a") : Color.ParseColor("#DEDFF0"));


        // --- A. Marquee Title ---
        _expandedTitle = new MaterialTextView(ctx) { TextSize = 28, Typeface = Android.Graphics.Typeface.DefaultBold, Gravity = GravityFlags.Center };
        _expandedTitle.SetSingleLine(true);
        _expandedTitle.Ellipsize = Android.Text.TextUtils.TruncateAt.Marquee;
        _expandedTitle.Selected = true;

        var transName = $"sharedImage_{MyViewModel.CurrentPlayingSongView.Id}";
        ViewCompat.SetTransitionName(_expandedTitle, transName);
       
       
    

        _expandedTitle.Click += (s, e) =>
        {
            string? tName = ViewCompat.GetTransitionName(_miniCover);
            if (tName != null)
            {
                MyViewModel.SelectedSong = MyViewModel.CurrentPlayingSongView;
                MyViewModel.NavigateToSingleSongPageFromHome(this, tName, _miniCover);
            }
            var act = Activity as TransitionActivity;
            if (act != null)
            {
                act.TogglePlayer();
            }
        };

        root.AddView(_expandedTitle);

        // --- B. Carousel ViewPager2 with Lyrics Overlay ---
        var carouselFrame = UiBuilder.CreateCard(ctx);
        
        carouselFrame.SetBackgroundColor(Color.Transparent);
        carouselFrame.StrokeWidth = 0;
        var LL = new LinearLayout.LayoutParams(-1, AppUtil.DpToPx(400));

        var carouselParams = LL;
        carouselParams.SetMargins(0, 30, 0, 10);
        carouselFrame.SetPadding(0, 0, 0, 0);
        carouselFrame.LayoutParameters = carouselParams;

        _mainCoverImage = new ImageView(ctx);
        carouselFrame.Radius = AppUtil.DpToPx(20);

        carouselFrame.AddView(_mainCoverImage, new FrameLayout.LayoutParams(-1, -1));


        _lyricsCard = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(12),
            CardBackgroundColor = ColorStateList.ValueOf(Color.ParseColor("#AA000000")),
            Elevation = 8,
            Visibility = ViewStates.Invisible
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
        _lyricsCard.Click += (s, e) =>
        {
            MyViewModel.SelectedSong = MyViewModel.CurrentPlayingSongView;
            MyViewModel.NavigateToAnyPageOfGivenType(this, new LyricsViewFragment(MyViewModel),"toLyricsFromNP");
            if (Activity is TransitionActivity act)
            {
                act.TogglePlayer();
            }
        };

        carouselFrame.AddView(_lyricsCard, lParams);

        // Meta Data (Bottom of Carousel)
        var metaLay = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        var metaParams = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent) { Gravity = GravityFlags.Bottom };
        metaParams.SetMargins(20, 0, 20, 20);
        

        carouselFrame.AddView(metaLay);

        root.AddView(carouselFrame);

        



        // --- C. Artist Row (Chips + Vertical Action Stack) ---
        var artistActionRow = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        artistActionRow.SetGravity( GravityFlags.CenterHorizontal);

     
        _artistChipGroup = new ChipGroup(context: ctx) { SingleLine = true };
        artistActionRow.AddView(_artistChipGroup);
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
        _seekSlider.Value = 1;
        _seekSlider.ValueTo = 100;
        // Logic attached in Resume
        root.AddView(_seekSlider);

        // --- E. Playback Controls (MD3 Style) ---
        var controlsRow = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        controlsRow.SetGravity(GravityFlags.Center);
        controlsRow.SetPadding(0, 20, 0, 40);
        _shuffleBtn = CreateControlButton(ctx, Resource.Drawable.media3_icon_shuffle_off, 40);
       
        _prevBtn = CreateControlButton(ctx, Resource.Drawable.media3_icon_previous, 60);
        _playPauseBtn = CreateControlButton(ctx, Resource.Drawable.media3_icon_play, 80, true)
            ; // Larger, filled
        _nextBtn = CreateControlButton(ctx, Resource.Drawable.media3_icon_next, 60);

        _repeatBtn = CreateControlButton(ctx, Resource.Drawable.media3_icon_repeat_all, 45);
        controlsRow.AddView(_shuffleBtn);
        controlsRow.AddView(_prevBtn);
        controlsRow.AddView(_playPauseBtn);
        controlsRow.AddView(_nextBtn);
        controlsRow.AddView(_repeatBtn);


        root.AddView(controlsRow);


        var volumeRow = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        volumeRow.SetGravity(GravityFlags.CenterVertical);
        volumeRow.SetPadding(0, 0, 0, 20);

        var volDownIcon = new ImageView(ctx);
        volDownIcon.SetImageResource(Resource.Drawable.volumesmall); // Make sure you have this icon
        volDownIcon.ImageTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Gray);
       

        _volumeSlider = new Slider(ctx);
        _volumeSlider.ValueFrom = 0; _volumeSlider.ValueTo = 100;
        _volumeSlider.Value = 50; // Default

        var volUpIcon = new ImageView(ctx);
        volUpIcon.SetImageResource(Resource.Drawable.volumeloud); // Make sure you have this icon
        volUpIcon.ImageTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Gray);
        volUpIcon.Click += (s, e) =>
        {
            MyViewModel.IncreaseVolumeLevel();
        };


        SelectedAudioTextView = new Chip(ctx);
        SelectedAudioTextView.Text = MyViewModel.SelectedAudioDevice?.Name;
        SelectedAudioTextView.Click += (s, e) =>
        {
            
        };
        //volumeRow.AddView(_volumeSlider, new LinearLayout.LayoutParams(0, -2, 1f)); // Stretch slider
        //volumeRow.AddView(volUpIcon, new LinearLayout.LayoutParams(AppUtil.DpToPx(24), AppUtil.DpToPx(24)));

        volumeRow.AddView(volDownIcon, new LinearLayout.LayoutParams(AppUtil.DpToPx(24), AppUtil.DpToPx(24)));
        volumeRow.AddView(SelectedAudioTextView, new LinearLayout.LayoutParams(AppUtil.DpToPx(24), AppUtil.DpToPx(24)));
        root.AddView(volumeRow);


        formatCard = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(12),
            CardBackgroundColor = ColorStateList.ValueOf(Color.ParseColor("#AA000000")),
            Elevation = 8,
            Visibility = ViewStates.Invisible
        };
        var formatParams = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
        {
            Gravity = GravityFlags.Center,
            LeftMargin = 40,
            RightMargin = 40
        };
        formatViewText = new TextView(ctx) { TextSize = 20, Gravity = GravityFlags.Center, Typeface = Typeface.DefaultBold };
        formatViewText.SetTextColor(Color.White);
        formatViewText.SetPadding(40, 30, 40, 30);
        formatCard.AddView(formatViewText);

        root.AddView(formatCard);

        var pillsRow = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        pillsRow.SetGravity(GravityFlags.Center);
        // Add some space at bottom for gesture bar
        pillsRow.SetPadding(0, 0, 0, AppUtil.DpToPx(30));

     
        _queueBtn= CreatePillButton(ctx, "Queue", Resource.Drawable.media3_icon_queue_next);
        _queueBtn.Click += async (s, e) =>
        {
            var queueSheet = new QueueBottomSheetFragment(MyViewModel, _queueBtn);
            _queueBtn.Enabled = false;
            queueSheet.Show(ParentFragmentManager, "QueueSheet");

            await Task.Delay(1200);
            queueSheet.ScrollToSong(MyViewModel.CurrentPlayingSongView);
        };
     
        // 2. Share Pill
        _loveBtn = CreatePillButton(ctx, "Love", Resource.Drawable.favlove);
        _loveBtn.Checkable = true;
        _loveBtn.Click += async (s, e) =>
        {
            if (!_loveBtn.Checked)
            {
                await MyViewModel.AddFavoriteRatingToSong(MyViewModel.CurrentPlayingSongView);
                _loveBtn.Checked = true;
                _loveBtn.PerformHapticFeedback(FeedbackConstants.Confirm);
            }
        };
        _loveBtn.LongClickable = true;
        _loveBtn.LongClick += async (sender, e) =>
        {

            if (_loveBtn.Checked)
            {
                await MyViewModel.RemoveSongFromFavorite(MyViewModel.CurrentPlayingSongView);
                _loveBtn.Checked = false;
                _loveBtn.PerformHapticFeedback(FeedbackConstants.Reject);
            }
        };

        _loveBtn.CheckedChange +=  (s, e) =>
        {
            var btn = e.P0;
            var isChecked = e.P1;
            

             UpdateLoveBtnUI(isChecked);

        };


        
        _toggleLyricsViewBtn = CreatePillButton(ctx, "Lyrics", Resource.Drawable.lyrics);
        _toggleLyricsViewBtn.Click += (s, e)
            =>
        {
            MyViewModel.SelectedSong = MyViewModel.CurrentPlayingSongView;
            MyViewModel._lyricsMgtFlow.LoadLyrics(MyViewModel.CurrentPlayingSongView.SyncLyrics);
            MyViewModel.NavigateToAnyPageOfGivenType(this, new LyricsViewFragment(MyViewModel), "toLyricsFromNP");
            if (Activity is TransitionActivity act)
            {
                act.TogglePlayer();
            }
        };
        
        _toggleLyricsViewBtn.LongClick += (s, e) =>
        {
            bool showingLyrics = _lyricsCard.Visibility == ViewStates.Visible;

            if (!showingLyrics)
            {
              
                _lyricsCard.Visibility = ViewStates.Visible;

                _mainCoverImage.Animate()?.Alpha(0.7f).SetDuration(150)
                .WithStartAction(new Java.Lang.Runnable(() =>
                {
                    _lyricsCard.Animate()?.Alpha(1f).SetDuration(250);
                }))
                .WithEndAction(new Java.Lang.Runnable(() =>
                {
                    _toggleLyricsViewBtn.SetTextColor(Color.Gray);
                }));
            }
         
            else
            {
                _lyricsCard.Animate()?.Alpha(0f).SetDuration(200)
                .WithEndAction(new Java.Lang.Runnable(() =>
                {
                    _lyricsCard.Visibility = ViewStates.Gone;

                    _mainCoverImage.Alpha = 0.7f;
                    _mainCoverImage.Animate()?.Alpha(1f).SetDuration(200);

                    _toggleLyricsViewBtn.SetTextColor(
                        UiBuilder.IsDark(this.View) ? Color.White : Color.Black);
                }));
            }
        };

        audioDevicesPill = CreatePillButton(ctx, "Info", Resource.Drawable.ic_vol_type_speaker_dark);
        audioDevicesPill.Click += (s, e) =>
        {
            var dialog = ShowDifferentAudioDevicesDialog();


            dialog?.Show();
        };

        // Distribute evenly with weights or margins
        var pillParams = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f);
        pillParams.SetMargins(AppUtil.DpToPx(4), 0, AppUtil.DpToPx(4), 0);

        pillsRow.AddView(_queueBtn, pillParams);
        pillsRow.AddView(_loveBtn, pillParams);
        pillsRow.AddView(_toggleLyricsViewBtn, pillParams);
        pillsRow.AddView(audioDevicesPill, pillParams);
        root.AddView(pillsRow);

        //// --- F. Bottom Actions (Queue, View Song, Options) ---
        //var actionRow = new LinearLayout(ctx) { Orientation = Orientation.Horizontal, WeightSum = 3 };

        //_queueBtn = new MaterialButton(ctx, null, Resource.Attribute.materialIconButtonFilledStyle) { Text = "Queue" }; // Use Icon Button Style if possible
        //_queueBtn.SetIconResource(Resource.Drawable.hamburgermenu); // Ensure drawable

        //_detailsBtn = new MaterialButton(ctx) { Text = "Details" };
        //_optionsBtn = new MaterialButton(ctx) { Text = "..." };

        //// Distribute buttons
        //var p = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1);
        //p.SetMargins(10, 0, 10, 0);

        //actionRow.AddView(_queueBtn, p);
        //actionRow.AddView(_detailsBtn, p);
        //actionRow.AddView(_optionsBtn, p);

        //root.AddView(actionRow);

        // --- G. Volume (Optional) ---
        // _volumeSlider = ... (similar logic)

        scroll.AddView(root);

        _playPauseBtn.Click += async (s, e) =>
        {
            await MyViewModel.PlayPauseToggleAsync();


        };
        _shuffleBtn.Click += (s, e) =>
        {
            MyViewModel.ToggleShuffle();
        };

        _prevBtn.Click += async (s, e) => await MyViewModel.PreviousTrackAsync();
        _nextBtn.Click += async (s, e) => await MyViewModel.NextTrackAsync();

        
        _repeatBtn.Click += (s, e) =>
        {
            MyViewModel.ToggleRepeatMode();
        };
        // Slider Logic

        //_seekSlider.AddOnChangeListener
        SeekListener = new DimmerSliderListener(
    onDragStart: () =>
    {
        SeekListener.TotalDurationInSeconds = MyViewModel.CurrentPlayingSongView.DurationInSeconds;
        _isDraggingSeek = true;
        
    },
    onDragStop: (value) =>
    {
        _isDraggingSeek = false;
        if (MyViewModel.CurrentPlayingSongView != null)
        {
            // value is 0-100 (percentage)
            var newPos = (value / 100f) * MyViewModel.CurrentPlayingSongView.DurationInSeconds;
            MyViewModel.SeekTrackPositionCommand.Execute(newPos);
        }
    },
    onValueChange: (value, fromUser) =>
    {
        // Only update the Text Label, do NOT set _seekSlider.Value here
        if (fromUser && MyViewModel.CurrentPlayingSongView != null)
        {
            // Calculate actual seconds based on the percentage (value)
            var currentSeconds = (value / 100f) * MyViewModel.CurrentPlayingSongView.DurationInSeconds;

            var ts = TimeSpan.FromSeconds(currentSeconds);
            _currentTimeText.Text = $"{ts.Minutes}:{ts.Seconds:D2}";
        }
    }
);

        SeekListener.DataType = SliderDataType.Time;
        // 2. Register for BOTH events
        _seekSlider.AddOnSliderTouchListener(SeekListener);
        _seekSlider.AddOnChangeListener(SeekListener);
    

        var volumeListener = new DimmerSliderListener(
    onDragStart: () => { _isDraggingVolume = true; },
    onDragStop: (value) =>
    {
        _isDraggingVolume = false;
        // Logic happens immediately on lift, no need for Task.Delay
        var newVolume = (value / 100f);
        MyViewModel.SetVolumeLevel((double)newVolume);
    },
    onValueChange: null // We don't need real-time feedback for volume
);

        volumeListener.DataType = SliderDataType.Percentage;
        _volumeSlider.AddOnSliderTouchListener(volumeListener);
        // We don't strictly need AddOnChangeListener for volume if we only save on drop
        return scroll;
    }

    private void UpdateLoveBtnUI(bool isChecked)
    {
        if (isChecked)
        {
            _loveBtn.Text = "UnLove";
            _loveBtn.SetIconResource(Resource.Drawable.heartlock);
            _loveBtn.SetBackgroundColor(Color.DarkSlateBlue);
            
        }
        else
        {
            _loveBtn.Text = "Love";
            _loveBtn.SetIconResource(Resource.Drawable.heart);
            _loveBtn.SetBackgroundColor(Color.Gray);
           
        }
    }

    public MaterialAlertDialogBuilder? ShowDifferentAudioDevicesDialog()
    {
        if (MyViewModel.AudioDevices is null) return null;
        if (MyViewModel.SelectedAudioDevice is null) return null;
        var builder = new MaterialAlertDialogBuilder(Activity!);
        builder.SetTitle("Select Audio Device");

        var albumNamesArray = MyViewModel.AudioDevices.Select(x=>x.Name).ToArray();
        int currentIndex = MyViewModel.AudioDevices.IndexOf(MyViewModel.SelectedAudioDevice);
        builder.SetSingleChoiceItems(albumNamesArray, currentIndex, async (sender, args) =>
        {
            // Handle album selection
            var selectedDeviceName = albumNamesArray.ElementAt(args.Which);
            var selDev = MyViewModel.AudioDevices.First(x => x.Name == selectedDeviceName);
            MyViewModel.SetPreferredAudioDevice(selDev);
            
            
        });
        
        builder.SetNegativeButton("Cancel", (sender, args) =>
        {
            //Dismiss();
        })
           .Create();
        return builder;
    }
    private bool UnSplashToButton(int[] loc)
    {
        float finalRadius = (float)Math.Sqrt(_rootView.Width * _rootView.Width +
            _rootView.Height * _rootView.Height);
        var anim = ViewAnimationUtils.CreateCircularReveal(_rootView,
            loc[0] + _loveBtn.Width / 2,
            loc[1] + _loveBtn.Height / 2, finalRadius,
            0f);
        if (anim is null) return false;
        anim.SetDuration(550);
        anim.Start();
      
        return true;
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
        //btn.SetHeight(AppUtil.DpToPx(23));
        //btn.SetWidth(AppUtil.DpToPx(23));
        btn.TextSize = 14;
        btn.SetSingleLine(true);
        btn.Ellipsize = Android.Text.TextUtils.TruncateAt.End;
        btn.InsetTop = 0;
        btn.InsetBottom = 0;
        return btn;
    }

    private MaterialButton CreateControlButton(Context ctx, int iconRes, int sizeDp, bool isPrimary = false, int IconSize=45,
        int strokeWidth =0 )
    {
        var btn = new MaterialButton(ctx);
        btn.Icon = AndroidX.Core.Content.ContextCompat.GetDrawable(ctx, iconRes);
        btn.IconGravity = MaterialButton.IconGravityTextStart;
        btn.IconPadding = 0;
        btn.InsetTop = 0;
        btn.InsetBottom = 0;
        btn.IconSize = AppUtil.DpToPx(IconSize);
        var sizePx = AppUtil.DpToPx(IconSize);
        btn.LayoutParameters = new LinearLayout.LayoutParams(sizePx, sizePx) { LeftMargin = 20, RightMargin = 20 };
        btn.SetWidth(sizeDp);
        btn.SetHeight(sizeDp);
        btn.CornerRadius = sizeDp/2;
        btn.StrokeWidth=0;
        
        if (isPrimary)
        {
            btn.SetBackgroundColor(Android.Graphics.Color.DarkSlateBlue);
            btn.IconTint = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.White);
        }
        else
        {
            btn.SetBackgroundColor(Android.Graphics.Color.Transparent);
            btn.IconTint = Android.Content.Res.ColorStateList.ValueOf(UiBuilder.IsDark(btn) ? Android.Graphics.Color.White : Android.Graphics.Color.Black);
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

        MyViewModel.WhenPropertyChange
            (nameof(MyViewModel.CurrentPlayingSongView), s => MyViewModel.CurrentPlayingSongView)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(song =>
            {
                UpdateSongUI(song);
                // Only update mini cover, carousel handles its own images
                if (!string.IsNullOrEmpty(song.CoverImagePath))
                {
                    Glide.With(this).Load(song.CoverImagePath).Into(_miniCover);
                }
                else
                {
                    _miniCover.SetImageResource(Resource.Drawable.musicnotess);
                }
                if (song is null)
                {
                    _mainCoverImage.SetImageWithGlide(null);

                    return;
                }
                if (MyViewModel.CurrentPlayingSongView.CoverImagePath is not null && MyViewModel.OldSongValue?.AlbumName != song.AlbumName)
                {

                    _mainCoverImage.SetImageWithGlide(MyViewModel.CurrentPlayingSongView.CoverImagePath);


                }
            }).DisposeWith(_disposables);





        MyViewModel.AudioEngineIsPlayingObservable
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
                var tName = $"sharedImage_{MyViewModel.CurrentPlayingSongView.Id}";
                ViewCompat.SetTransitionName(_miniCover, tName);
            })
            .DisposeWith(_disposables);


        Observable.FromEventPattern<(double newVol, bool isDeviceMuted, int devMavVol)>(
           h => MyViewModel.AudioService.DeviceVolumeChanged += h,
           h => MyViewModel.AudioService.DeviceVolumeChanged -= h)
           .Select(evt => evt.EventArgs)
           .ObserveOn(RxSchedulers.UI)
           .Subscribe( s=>
           {
               UpdateDeviceVolumeChangedUI(s);
               }, ex => Debug.WriteLine("error on vol changed"))
           .DisposeWith(_disposables);
    



        // 2. Observe Playback Position (for Slider & Lyrics)
        MyViewModel.AudioEnginePositionObservable
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(pos =>
            {
                if (!_isDraggingSeek && MyViewModel.CurrentPlayingSongView?.DurationInSeconds > 0)
                {
                    var perc = (pos / MyViewModel.CurrentPlayingSongView.DurationInSeconds) * 100;
                    _seekSlider.Value = (int)Math.Clamp(perc, 0, 100);

                    var ts = TimeSpan.FromSeconds(pos);
                    _currentTimeText.Text = $"{ts.Minutes}:{ts.Seconds:D2}";
                }

                if (MyViewModel.CurrentLine != null)
                {

                    _currentLyricText.Text = MyViewModel.CurrentLine.Text;
                    _currentMiniLyricText.Text = MyViewModel.CurrentLine.Text;
                    _lyricsCard.Visibility = ViewStates.Visible;
                }
                else
                {
                    _lyricsCard.Visibility = ViewStates.Invisible;
                }
            })
            .DisposeWith(_disposables);
        if(MyViewModel.IsDimmerPlaying)
        {
            if (_playPauseBtn == null || _miniPlayBtn == null) return;
            _miniPlayBtn.SetIconResource(Resource.Drawable.media3_icon_pause);
            _playPauseBtn.SetIconResource(Resource.Drawable.media3_icon_pause);
        }

        MyViewModel.WhenPropertyChange(nameof(MyViewModel.CurrentPlayingSongView.HasSyncedLyrics), song => MyViewModel.CurrentPlayingSongView)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(async s =>
            {
                if(!string.IsNullOrEmpty(MyViewModel.CurrentPlayingSongView.CoverImagePath))
                {
                    var glassy = await ImageFilterUtils.ApplyFilter(MyViewModel.CurrentPlayingSongView.CoverImagePath,
                        FilterType.Ocean);
                    Glide.With(this.Context!)
                    .Load(glassy)
                    .Into(_mainCoverImage);
                    
                }
            });

        MyViewModel.WhenPropertyChange(nameof(MyViewModel.CurrentRepeatMode), newVl => MyViewModel.CurrentRepeatMode)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(rpMode =>
            {
                switch (rpMode)
                {
                    case Utilities.Enums.RepeatMode.All:
                        _repeatBtn.SetIconResource(Resource.Drawable.media3_icon_repeat_all);
                        _repeatBtn.IconTint = AppUtil.ToColorStateList(Color.DarkSlateBlue);
                        break;
                    case Utilities.Enums.RepeatMode.Off:
                        _repeatBtn.SetIconResource(Resource.Drawable.media3_icon_repeat_off);
                        break;
                    case Utilities.Enums.RepeatMode.One:
                        _repeatBtn.SetIconResource(Resource.Drawable.media3_icon_repeat_one);
                        break;
                    case Utilities.Enums.RepeatMode.Custom:
                        break;
                    default:
                        break;
                }
            }).DisposeWith(_disposables);



        MyViewModel.WhenPropertyChange(nameof(MyViewModel.IsShuffleActive), newVl => MyViewModel.IsShuffleActive)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(IsShuffleActive =>
            {
                if (IsShuffleActive)
                {
                    _shuffleBtn.SetIconResource(Resource.Drawable.media3_icon_shuffle_on);
                    _shuffleBtn.SetButtonIconColor(Color.DarkSlateBlue);
                }
                else
                {
                    _shuffleBtn.SetIconResource(Resource.Drawable.media3_icon_shuffle_off);
                    _shuffleBtn.SetButtonIconColor(Color.Gray);
                }
            }).DisposeWith(_disposables);


    }

    private void UpdateDeviceVolumeChangedUI((double newVol, bool isDeviceMuted, int devMavVol) tuple)
    {
        var newVal = tuple.Item1;
        var devMaxVol = tuple.Item3;
        _volumeSlider.ValueTo = devMaxVol;
        _volumeSlider.Value = (float)newVal;
        SelectedAudioTextView.Text = MyViewModel.SelectedAudioDevice?.Name;
    }

    public override void OnPause()
    {
        base.OnPause();
        _disposables.Clear();
    }

    private void UpdateSongUI(SongModelView? song)
    {
        if (song == null) return;

        UpdateLoveBtnUI(song.IsFavorite);

        // Mini Player
        _miniTitle.Text = song.Title;
        var finalArtName = song.HasSyncedLyrics ? "🎙️ " + song.OtherArtistsName : song.OtherArtistsName;
        _miniArtist.Text = finalArtName;

        // Expanded Player
        _expandedTitle.Text = song.Title;

        var totalTs = TimeSpan.FromSeconds(song.DurationInSeconds);
        _totalTimeText.Text = $"{totalTs.Minutes}:{totalTs.Seconds:D2}";

        // Artist Chips
        _artistChipGroup.RemoveAllViews();
       
        if (!string.IsNullOrEmpty(song.TitleDurationKey))
        {
            var artInDb = song.ArtistsInDB(MyViewModel.RealmFactory);

            if (artInDb is not null)
            {
                foreach (var art in artInDb)
                {
                    var chip = new Chip(Context) { Text = art.Name };

                    chip.Click += async (s, e) =>
                    {
                        
                        MyViewModel.NavigateToArtistPage(this, art.Id.ToString(), art, (Chip)s!);

                        TransitionActivity act = (this.Activity as TransitionActivity)!;
                        act.TogglePlayer();
                    };
                    _artistChipGroup.AddView(chip);
                }
            }
            song.ArtistToSong = song.ArtistsInDB(MyViewModel.RealmFactory)!.ToObservableCollection()!;
       
            _artistChipGroup.Click += (s, e) =>
            {
                if (artInDb is not null)
                {
                    var artistPickBtmSheet = new ArtistPickerBottomSheet(MyViewModel, artInDb);

                    artistPickBtmSheet.Show(this.ParentFragmentManager, "QueueSheet");
                }
            };

        }
        if (song.HasLyrics) 
        { 
            _toggleLyricsViewBtn.SetBackgroundColor(Color.DarkSlateBlue); 
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

        formatViewText.Text = $"{MyViewModel.CurrentPlayingSongView.FileFormat} • {MyViewModel.CurrentPlayingSongView.BPM}";
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