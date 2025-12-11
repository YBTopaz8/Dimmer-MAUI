using System.Reactive.Disposables;

using Bumptech.Glide;

using Dimmer.Utils.UIUtils;

using Google.Android.Material.Chip;

using ImageButton = Android.Widget.ImageButton;
using ScrollView = Android.Widget.ScrollView;

namespace Dimmer.ViewsAndPages.NativeViews;



public partial class NowPlayingFragment : Fragment
{
    // --- UI Properties ---
    public MaterialTextView SongTitle { get; private set; }
    public ImageView CoverImageView { get; private set; }

    // Lyrics Lines
    public TextView LyricLinePrev { get; private set; }
    public TextView LyricLineCurrent { get; private set; }
    public TextView LyricLineNext { get; private set; }

    // Meta Data
    public TextView GenreText { get; private set; }
    public TextView YearText { get; private set; }

    // Controls
    public Slider SeekSlider { get; private set; }
    public Slider VolumeSlider { get; private set; }
    public ChipGroup ArtistChipGroup { get; private set; }
    public MaterialButton PlayPauseBtn { get; private set; }

    private ImageView _headerCover;
    private TextView _headerTitle, _headerArtist, _headerAlbum;
    private TextView _headerDuration, _headerTimeRemaining;
    private ImageView _mainCover;
    private WavySlider _seekSlider;
    private MaterialButton _playPauseBtn;
    // State
    private bool _isDraggingSeek = false;
    public BaseViewModelAnd MyViewModel { get; private set; }
    public CompositeDisposable SubsManager { get; } = new CompositeDisposable();

    private TextView _lyricPrev, _lyricCurr, _lyricNext;
    public NowPlayingFragment(BaseViewModelAnd viewModel) { MyViewModel = viewModel; }
    public NowPlayingFragment() { }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context;

        // Root Container (White/Black background based on theme)
        var root = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        root.LayoutParameters = new ViewGroup.LayoutParams(-1, -1);
        root.SetPadding(30, 60, 30, 30); // Global Padding

        // ---------------------------------------------------------
        // 1. Song Title (5% Height)
        // ---------------------------------------------------------
        SongTitle = new MaterialTextView(ctx)
        {
            TextSize = 24,
            Typeface = Typeface.DefaultBold,
            Gravity = GravityFlags.Center
        };
        SongTitle.SetSingleLine(true);
        SongTitle.Ellipsize = Android.Text.TextUtils.TruncateAt.Marquee;
        SongTitle.Selected = true; // For Marquee to work

        var titleParams = new LinearLayout.LayoutParams(-1, 0, 0.5f); // Weight 0.5 (~5%)
        root.AddView(SongTitle, titleParams);


        // ---------------------------------------------------------
        // 2. Cover Art Grid & Lyrics (30% Height)
        // ---------------------------------------------------------
        var coverContainer = new FrameLayout(ctx);
        var coverParams = new LinearLayout.LayoutParams(-1, 0, 3.0f); // Weight 3 (~30%)
        coverParams.SetMargins(0, 20, 0, 20);

        // Layer A: The Image
        var card = new MaterialCardView(ctx) { Radius = 40, Elevation = 0 }; // Round corners
        card.LayoutParameters = new FrameLayout.LayoutParams(-1, -1);

        CoverImageView = new ImageView(ctx);
        CoverImageView.LayoutParameters = new ViewGroup.LayoutParams(-1, -1);
        CoverImageView.SetScaleType(ImageView.ScaleType.CenterCrop);
        card.AddView(CoverImageView);
        coverContainer.AddView(card);

        // Layer B: Dark Scrim (Blur simulation)
        var scrim = new View(ctx);
        scrim.LayoutParameters = new FrameLayout.LayoutParams(-1, -1);
        scrim.SetBackgroundColor(Color.ParseColor("#99000000")); // 60% Black Overlay
        coverContainer.AddView(scrim);

        // Layer C: Lyrics (Vertical Stack, Centered)
        var lyricsStack = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        lyricsStack.SetGravity(GravityFlags.Center);
        lyricsStack.LayoutParameters = new FrameLayout.LayoutParams(-1, -1); // Fill parent

        LyricLinePrev = CreateLyricText(ctx, 16, 0.4f);
        LyricLineCurrent = CreateLyricText(ctx, 22, 1.0f, true); // Bold
        LyricLineNext = CreateLyricText(ctx, 16, 0.7f);

        lyricsStack.AddView(LyricLinePrev);
        lyricsStack.AddView(LyricLineCurrent);
        lyricsStack.AddView(LyricLineNext);
        coverContainer.AddView(lyricsStack);

        // Layer D: Meta Data (Bottom Left/Right)
        var metaContainer = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        var metaParams = new FrameLayout.LayoutParams(-1, -2) { Gravity = GravityFlags.Bottom };
        metaParams.SetMargins(30, 0, 30, 30);
        metaContainer.LayoutParameters = metaParams;

        GenreText = new TextView(ctx) { Text = "Genre", TextSize = 14 };
        GenreText.SetTextColor(Color.White);

        YearText = new TextView(ctx) { Text = "Year", TextSize = 14, Gravity = GravityFlags.End };
        YearText.SetTextColor(Color.White);

        metaContainer.AddView(GenreText, new LinearLayout.LayoutParams(0, -2, 1f));
        metaContainer.AddView(YearText, new LinearLayout.LayoutParams(0, -2, 1f));
        coverContainer.AddView(metaContainer);

        root.AddView(coverContainer, coverParams);


        // ---------------------------------------------------------
        // 3. Artist Chips & Action Buttons (Vertical Stack)
        // ---------------------------------------------------------
        var midSection = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        midSection.LayoutParameters = new LinearLayout.LayoutParams(-1, -2);
        midSection.SetPadding(0, 20, 0, 20);

        // Left Col: Actions (Fav, Lyrics)
        var actionStack = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        actionStack.LayoutParameters = new LinearLayout.LayoutParams(0, -2, 0.3f); // 30% width

        var favBtn = CreateIconPill(ctx, Resource.Drawable.media3_icon_star_filled, Color.Green);
        var lyricsBtn = CreateIconPill(ctx, Android.Resource.Drawable.IcMenuInfoDetails, Color.Green); // Placeholder icon

        actionStack.AddView(favBtn);
        actionStack.AddView(new Space(ctx) { LayoutParameters = new LinearLayout.LayoutParams(-1, 10) }); // Spacer
        actionStack.AddView(lyricsBtn);

        // Right Col: Artists Chips (Red border)
        var artistScroll = new ScrollView(ctx);
        artistScroll.LayoutParameters = new LinearLayout.LayoutParams(0, 250, 0.7f); // 70% width, fixed height constraint

        ArtistChipGroup = new ChipGroup(ctx);
        artistScroll.AddView(ArtistChipGroup);

        midSection.AddView(actionStack);
        midSection.AddView(artistScroll);
        root.AddView(midSection);


        // ---------------------------------------------------------
        // 4. Controls Row 1: Prev (30%) | Play (70%)
        // ---------------------------------------------------------
        var row1 = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        row1.LayoutParameters = new LinearLayout.LayoutParams(-1, -2);
        row1.SetGravity(GravityFlags.CenterVertical);

        var prevBtn = new MaterialButton(ctx);
        prevBtn.Text = "Prev";
        prevBtn.SetIconResource(Android.Resource.Drawable.IcMediaPrevious);
        prevBtn.LayoutParameters = new LinearLayout.LayoutParams(0, 150, 0.3f); // Height 150px
        prevBtn.Click += async (s, e) => await MyViewModel.PreviousTrackASync();

        PlayPauseBtn = new MaterialButton(ctx);
        PlayPauseBtn.Text = "Pause"; // Initial
        PlayPauseBtn.SetIconResource(Android.Resource.Drawable.IcMediaPause);
        PlayPauseBtn.CornerRadius = 75; // Pill shape
        PlayPauseBtn.LayoutParameters = new LinearLayout.LayoutParams(0, 150, 0.7f) { LeftMargin = 20 };
        PlayPauseBtn.Click += async (s, e) => await MyViewModel.PlayPauseToggleAsync();

        row1.AddView(prevBtn);
        row1.AddView(PlayPauseBtn);
        root.AddView(row1);


        // ---------------------------------------------------------
        // 5. Controls Row 2: Slider (70%) | Next (30%)
        // ---------------------------------------------------------
        var row2 = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        var row2Params = new LinearLayout.LayoutParams(-1, -2);
        row2Params.TopMargin = 30;
        row2.LayoutParameters = row2Params;
        row2.SetGravity(GravityFlags.CenterVertical);

        // Seek Slider
        SeekSlider = new Slider(ctx);
        SeekSlider.ValueFrom = 0f;
        SeekSlider.ValueTo = 100f;
        SeekSlider.LayoutParameters = new LinearLayout.LayoutParams(0, -2, 0.7f);
        // Note: Standard MD3 Slider. Custom "Wavy" requires custom View/Canvas drawing in Android.

        // Touch Logic for Seek
        SeekSlider.Touch += (s, e) =>
        {
            e.Handled = false; // Let slider update visual
            switch (e.Event.Action)
            {
                case MotionEventActions.Down:
                    _isDraggingSeek = true;
                    break;
                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                    _isDraggingSeek = false;
                    if (MyViewModel.CurrentPlayingSongView?.DurationInSeconds > 0)
                    {
                        var newPos = (SeekSlider.Value / 100f) * MyViewModel.CurrentPlayingSongView.DurationInSeconds;
                        MyViewModel.SeekTrackPositionCommand.Execute(newPos);
                    }
                    break;
            }
        };

        var nextBtn = new MaterialButton(ctx);
        nextBtn.Text = "Next";
        nextBtn.SetIconResource(Android.Resource.Drawable.IcMediaNext);
        nextBtn.LayoutParameters = new LinearLayout.LayoutParams(0, 150, 0.3f) { LeftMargin = 20 };
        nextBtn.Click += async (s, e) => await MyViewModel.NextTrackAsync();

        row2.AddView(SeekSlider);
        row2.AddView(nextBtn);
        root.AddView(row2);


        // ---------------------------------------------------------
        // 6. Volume Row: Icon(20) | Slider(60) | Speaker(20)
        // ---------------------------------------------------------
        var volRow = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        var volParams = new LinearLayout.LayoutParams(-1, -2);
        volParams.TopMargin = 30;
        volRow.LayoutParameters = volParams;
        volRow.SetGravity(GravityFlags.CenterVertical);

        var volIcon = new ImageButton(ctx) { Background = null };
        volIcon.SetImageResource(Android.Resource.Drawable.IcLockSilentModeOff); // Standard Vol Icon
        volIcon.LayoutParameters = new LinearLayout.LayoutParams(0, -2, 0.2f);
        volIcon.Click += (s, e) => ShowVolumeMenu(volIcon);

        VolumeSlider = new Slider(ctx) { ValueFrom = 0f, ValueTo = 1f, Value = 0.5f };
        VolumeSlider.LayoutParameters = new LinearLayout.LayoutParams(0, -2, 0.6f);

        var deviceIcon = new ImageButton(ctx) { Background = null };
        deviceIcon.SetImageResource(Android.Resource.Drawable.IcLockIdleAlarm); // Speaker/Device Icon
        deviceIcon.LayoutParameters = new LinearLayout.LayoutParams(0, -2, 0.2f);
        deviceIcon.Click += (s, e) => Toast.MakeText(ctx, "Show Audio Devices", ToastLength.Short).Show();

        volRow.AddView(volIcon);
        volRow.AddView(VolumeSlider);
        volRow.AddView(deviceIcon);
        root.AddView(volRow);


        // ---------------------------------------------------------
        // 7. Queue Button
        // ---------------------------------------------------------
        var queueBtn = new MaterialButton(ctx);
        queueBtn.Text = "Show Queue";
        var qParams = new LinearLayout.LayoutParams(-1, 120); // Fixed height
        qParams.TopMargin = 40;
        queueBtn.LayoutParameters = qParams;
        queueBtn.CornerRadius = 60; // Pill

        root.AddView(queueBtn);

        return root;
    }

    // --- Helpers ---

    private TextView CreateLyricText(Context ctx, float size, float alpha, bool bold = false)
    {
        var tv = new TextView(ctx)
        {
            TextSize = size,
            Alpha = alpha,
            Gravity = GravityFlags.Center
        };
        tv.SetTextColor(Color.White);
        if (bold) tv.Typeface = Typeface.DefaultBold;
        tv.SetPadding(20, 10, 20, 10);
        return tv;
    }

    private MaterialButton CreateIconPill(Context ctx, int iconRes, Color bgColor)
    {
        var btn = new MaterialButton(ctx);
        btn.IconGravity = MaterialButton.IconGravityTextStart;
        btn.SetIconResource(iconRes);
        btn.SetBackgroundColor(bgColor);
        btn.IconTint = Android.Content.Res.ColorStateList.ValueOf(Color.White);
        btn.CornerRadius = 30;
        btn.LayoutParameters = new LinearLayout.LayoutParams(-1, 100);
        return btn;
    }

    private void AddArtistChip(string artistName)
    {
        var chip = new Chip(Context);
        chip.Text = artistName;
        chip.Clickable = true;

        // Style: Red Border
        chip.ChipStrokeWidth = 5f; // 2dp approx
        chip.ChipStrokeColor = Android.Content.Res.ColorStateList.ValueOf(Color.Red);
        chip.ChipBackgroundColor = Android.Content.Res.ColorStateList.ValueOf(Color.Transparent);

        // Vertical stacking is handled by container, but ChipGroup usually flows horizontally.
        // If you want vertical chips, use a vertical LinearLayout inside the ScrollView instead of ChipGroup.
        // Assuming standard ChipGroup flow for now.

        ArtistChipGroup.AddView(chip);
    }

    private void ShowVolumeMenu(View anchor)
    {
        var popup = new PopupMenu(Context, anchor);
        popup.Menu.Add("Mute");
        popup.Menu.Add("Max Volume");
        popup.Show();
    }

    // --- Logic & Subscriptions ---

    public override void OnResume()
    {
        base.OnResume();

        // Song Info Subscription
        var songSub = MyViewModel.CurrentSongChanged
            .Where(s => s != null)
            .DistinctUntilChanged(s => s.FilePath)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(song =>
            {
                SongTitle.Text = song.Title;
                GenreText.Text = song.GenreName ?? "Unknown Genre";
                YearText.Text = song.ReleaseYear?.ToString() ?? "----";

                // Update Cover
                if (!string.IsNullOrEmpty(song.CoverImagePath))
                {
                    Glide.With(Context).Load(song.CoverImagePath).Into(CoverImageView);
                }

                // Update Play/Pause Button State
                PlayPauseBtn.Text = "Pause"; // Assuming auto-play
                PlayPauseBtn.SetIconResource(Android.Resource.Drawable.IcMediaPause);

                // Populate Artists (Clear old, add new)
                ArtistChipGroup.RemoveAllViews();
                if (!string.IsNullOrEmpty(song.ArtistName))
                {
                    // Split if multiple, otherwise just add one
                    AddArtistChip(song.ArtistName);
                }
            });

        SubsManager.Add(songSub);

        // Timer/Seek Subscription
        var posSub = MyViewModel.AudioEnginePositionObservable
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(pos =>
            {
                if (!_isDraggingSeek && MyViewModel.CurrentPlayingSongView?.DurationInSeconds > 0)
                {
                    var percent = (pos / MyViewModel.CurrentPlayingSongView.DurationInSeconds) * 100;
                    SeekSlider.Value = (float)Math.Min(100, Math.Max(0, percent));
                }

                // Update Lyrics Mock Logic (Replace with actual synced lyrics logic)
                LyricLineCurrent.Text = MyViewModel.CurrentLine?.Text;
            });

        SubsManager.Add(posSub);
    }

    public override void OnDestroy()
    {
        SubsManager.Clear();
        base.OnDestroy();
    }
}