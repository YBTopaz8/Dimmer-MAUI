using System.Reactive.Disposables;
using System.Threading.Tasks;

using AndroidX.Fragment.App;

using Bumptech.Glide;

using Google.Android.Material.Button;
using Google.Android.Material.Card;
using Google.Android.Material.Carousel;
using Google.Android.Material.Chip;
using Google.Android.Material.Dialog;
using Google.Android.Material.TextView;

using DialogFragment = AndroidX.Fragment.App.DialogFragment;
using ImageButton = Android.Widget.ImageButton;
using ScrollView = Android.Widget.ScrollView;
using TimePicker = Android.Widget.TimePicker;

namespace Dimmer.ViewsAndPages.NativeViews;


public partial class NowPlayingFragment : Fragment
{
    // --- Public Properties for ViewModel Access ---
    public MaterialTextView SongTitle { get; private set; }
    public MaterialTextView ArtistName { get; private set; }
    public MaterialTextView AlbumName { get; private set; }
    public MaterialTextView FileFormatText { get; private set; }
    public TextView LyricsPlaceholder { get; private set; } // For lyrics
    public Slider SeekSlider { get; private set; }
    public Slider VolumeSlider { get; private set; }
    public ImageView CoverImageView { get; private set; } // Main Cover
    public bool IsDragging { get; private set; } = false;

    // --- Private Fields ---
    private FrameLayout _rootView;
    private LinearLayout _miniPlayerLayout;
    private ScrollView _mainPlayerLayout;
    private TextView _miniTitle, _miniArtist;
    private ImageView _miniPlayBtn, _miniArt;
    private MaterialTextView _currentTimeText,
        _totalTimeText;
    private MaterialButton _playPauseBtn;
    private bool _isDraggingSeek;

    public BaseViewModelAnd MyViewModel { get; private set; }

    public NowPlayingFragment(BaseViewModelAnd viewModel) { MyViewModel = viewModel; }
    public NowPlayingFragment() { }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context;
        _rootView = new FrameLayout(ctx) { LayoutParameters = new ViewGroup.LayoutParams(-1, -1) };

        _mainPlayerLayout = BuildMainPlayer(ctx);
        _mainPlayerLayout.Alpha = 0f;
        _mainPlayerLayout.Visibility = ViewStates.Invisible;

        _miniPlayerLayout = BuildMiniPlayer(ctx);
        _miniPlayerLayout.Alpha = 1f;

        _rootView.AddView(_mainPlayerLayout);
        _rootView.AddView(_miniPlayerLayout);

        return _rootView;
    }

    // --- BUILDERS ---

    private LinearLayout BuildMiniPlayer(Context ctx)
    {
        var layout = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new FrameLayout.LayoutParams(-1, AppUtil.DpToPx(85)),
            
        };
        layout.SetGravity(GravityFlags.CenterVertical);
        layout.SetBackgroundColor(Android.Graphics.Color.ParseColor("#252525"));
        layout.Click += (s, e) => (Activity as TransitionActivity)?.TogglePlayer();

        _miniArt = new ImageView(ctx) { LayoutParameters = new LinearLayout.LayoutParams(AppUtil.DpToPx(50), AppUtil.DpToPx(50)) };
        _miniArt.SetScaleType(ImageView.ScaleType.CenterCrop);

        var infoStack = new LinearLayout(ctx) { Orientation = Orientation.Vertical, LayoutParameters = new LinearLayout.LayoutParams(0, -2, 1f) };
        infoStack.SetPadding(20, 0, 20, 0);
        _miniTitle = new TextView(ctx) { TextSize = 16, Typeface = Android.Graphics.Typeface.DefaultBold };
        _miniTitle.SetSingleLine(true);

        _miniArtist = new TextView(ctx) { TextSize = 12};
        _miniArtist.SetSingleLine(true);

        infoStack.AddView(_miniTitle); infoStack.AddView(_miniArtist);

        _miniPlayBtn = new ImageView(ctx) { LayoutParameters = new LinearLayout.LayoutParams(AppUtil.DpToPx(40), AppUtil.DpToPx(40)) };
        _miniPlayBtn.SetImageResource(Android.Resource.Drawable.IcMediaPlay);
        _miniPlayBtn.Click += async (s, e) => await MyViewModel.PlayPauseToggleAsync();
        _miniPlayBtn.LongClick += (s, e) =>
        {
            var popup = new PopupMenu(Context, _miniPlayBtn);
            if (popup is null) return;
            popup.Menu.Add("Add to Favorites");
            popup.Menu.Add("Go to Artist");
            popup.Show();
        };
        layout.AddView(_miniArt); layout.AddView(infoStack); layout.AddView(_miniPlayBtn);
        return layout;
    }
    public CompositeDisposable SubsManager => _subsManager;
    private readonly CompositeDisposable _subsManager = new CompositeDisposable();

    public override void OnResume()
    {
        base.OnResume();


        var songSubscription = MyViewModel.CurrentSongChanged
           .Where(song => song != null)
           .Select(song => song!)
           .DistinctUntilChanged(s => s.FilePath)
           .ObserveOn(RxSchedulers.UI)
          
       .Subscribe(song =>
       {

           //SongTitle.Text = song.Title;
           //ArtistName.Text = song.AlbumName;
           //AlbumName.Text = song.ArtistName;
           ////if (homeFrag._playCount is not null)
           ////    homeFrag._playCount.Text = song.PlayCount.ToString();


           //Glide.With(Context).Load(MyViewModel.CurrentPlayingSongView.CoverImagePath)
           //.Into(CoverImageView);

       },
       error =>
       {
           // Always good to catch errors in image loading so stream doesn't die
           Console.WriteLine($"Image Load Error: {error.Message}");
       });

        SubsManager.Add(songSubscription);


        // 2. Position Timer Subscription
        var positionSub = MyViewModel.AudioEnginePositionObservable
            .Sample(TimeSpan.FromMilliseconds(250)) // 4 times a second is plenty for smooth UI
            .ObserveOn(RxSchedulers.UI) // Ensure we are on UI thread
            .Subscribe(songPosition =>
            {
                
            });

        SubsManager.Add(positionSub);
    }
    
    public override void OnDestroy()
    {
        base.OnDestroy();
    }
    private ScrollView BuildMainPlayer(Context ctx)
    {
        var scroll = new ScrollView(ctx) { LayoutParameters = new FrameLayout.LayoutParams(-1, -1), FillViewport = true };
        var stack = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        stack.SetPadding(40, 60, 40, 40);

        // Header (Collapse Btn)
        var collapseBtn = new MaterialButton(ctx) { Text = "Collapse" };
        collapseBtn.Click += (s, e) => (Activity as TransitionActivity)?.CollapsePlayer();
        stack.AddView(collapseBtn);

        // Cover Art
        var card = new MaterialCardView(ctx) { Radius = AppUtil.DpToPx(20), Elevation = 10, LayoutParameters = new LinearLayout.LayoutParams(-1, AppUtil.DpToPx(350)) };
        CoverImageView = new ImageView(ctx) { LayoutParameters = new ViewGroup.LayoutParams(-1, -1) };
        CoverImageView.SetScaleType(ImageView.ScaleType.CenterCrop);

        CoverImageView.Clickable = true;
        CoverImageView.Click += (s, e) =>
        {
            // 1. Trigger the ViewModel event
            MyViewModel.TriggerScrollToCurrentSong();

            // 2. Collapse the sheet so the user can see the scrolling happen
            (Activity as TransitionActivity)?.CollapsePlayer();

            // 3. Optional: Show a small toast
            Toast.MakeText(ctx, "Locating song...", ToastLength.Short)?.Show();
        };



        card.AddView(CoverImageView);
        stack.AddView(card);


        var volumeLabel = new MaterialTextView(ctx) { Text = "Volume", TextSize = 12, Gravity = GravityFlags.Center };
        stack.AddView(volumeLabel);

        VolumeSlider = new Slider(ctx) { ValueFrom = 0f, ValueTo = 1f, Value = 0.5f };
        stack.AddView(VolumeSlider);


        // Info
        SongTitle = new MaterialTextView(ctx) { TextSize = 24, Typeface = Android.Graphics.Typeface.DefaultBold, Gravity = GravityFlags.Center };
        ArtistName = new MaterialTextView(ctx) { TextSize = 18, Gravity = GravityFlags.Center };
        AlbumName = new MaterialTextView(ctx) { TextSize = 14, Gravity = GravityFlags.Center, Alpha = 0.7f };
        FileFormatText = new MaterialTextView(ctx) { TextSize = 12, Gravity = GravityFlags.Center };

        stack.AddView(SongTitle); stack.AddView(ArtistName); stack.AddView(AlbumName); stack.AddView(FileFormatText);

        // --- FIXED SLIDER LOGIC ---
        SeekSlider = new Slider(ctx) { ValueFrom = 0f, ValueTo = 100f, Value = 0f };

        // Use a wrapper listener to handle Start/Stop tracking
        SeekSlider.Touch += (s, e) =>
        {
            // IMPORTANT: e.Handled = false ensures the Slider still processes the touch 
            // and moves the thumb visually. We are just "listening" in.
            e.Handled = false;

            if (e.Event.Action == MotionEventActions.Down)
            {
                // User touched the slider
                _isDraggingSeek = true;
            }
            else if (e.Event.Action == MotionEventActions.Up || e.Event.Action == MotionEventActions.Cancel)
            {
                // User let go
                _isDraggingSeek = false;

                if (MyViewModel.CurrentPlayingSongView?.DurationInSeconds > 0)
                {
                    // Execute Seek Command
                    var newPos = (SeekSlider.Value / 100f) * MyViewModel.CurrentPlayingSongView.DurationInSeconds;
                    MyViewModel.SeekTrackPositionCommand.Execute(newPos);
                }
            }
        };
        stack.AddView(SeekSlider);

        // Timers
        var timeRow = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        _currentTimeText = new MaterialTextView(ctx) { Text = "0:00" };
        _totalTimeText = new MaterialTextView(ctx) { Text = "-:--", Gravity = GravityFlags.End, LayoutParameters = new LinearLayout.LayoutParams(0, -2, 1f) };
        timeRow.AddView(_currentTimeText); timeRow.AddView(_totalTimeText);
        stack.AddView(timeRow);

        // Controls
        var controls = new LinearLayout(ctx) { Orientation = Orientation.Horizontal};
        controls.SetGravity(GravityFlags.Center);
        var prev = new MaterialButton(ctx) { Text = "Prev" };
        prev.Click += async (s, e) => await MyViewModel.PreviousTrackASync();
        _playPauseBtn = new MaterialButton(ctx) { Text = "Pause" };
        _playPauseBtn.Click += async (s, e) => await MyViewModel.PlayPauseToggleAsync();
        var next = new MaterialButton(ctx) { Text = "Next" };
        next.Click += async (s, e) => await MyViewModel.NextTrackAsync();

        controls.AddView(prev); controls.AddView(_playPauseBtn); controls.AddView(next);
        stack.AddView(controls);

        // Lyrics Placeholder
        LyricsPlaceholder = new TextView(ctx) { Text = "Lyrics...", Gravity = GravityFlags.Center, Top = 20 };
        stack.AddView(LyricsPlaceholder);

        scroll.AddView(stack);
        return scroll;
    }


    // --- Animation Logic ---
    public void AnimateTransition(float slideOffset)
    {
        _miniPlayerLayout.Alpha = Math.Max(0f, 1f - (slideOffset * 3f));
        _miniPlayerLayout.Visibility = _miniPlayerLayout.Alpha > 0 ? ViewStates.Visible : ViewStates.Gone;
        _mainPlayerLayout.Alpha = slideOffset;
        _mainPlayerLayout.Visibility = slideOffset > 0 ? ViewStates.Visible : ViewStates.Invisible;
    }
    public void SetInputActive(bool active) { SeekSlider?.Enabled = active; }
}