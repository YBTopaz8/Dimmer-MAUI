using Google.Android.Material.Button;
using Google.Android.Material.Card;
using Google.Android.Material.Chip;
using Google.Android.Material.TextView;

using ScrollView = Android.Widget.ScrollView;
using Slider = Google.Android.Material.Slider.Slider;

namespace Dimmer.ViewsAndPages.NativeViews;

public partial class NowPlayingFragment : Fragment
{
    FloatingActionButton fabMD3;
    private FrameLayout root;
    public TextView LyricsPlaceholder;

    public BaseViewModelAnd MyViewModel { get; private set; } = null!;
    public MaterialTextView SongTitle { get; private set; }
    public ImageView AlbumArtImage { get; private set; }
    public FrameLayout LyricsOverlay { get; private set; }
    public MaterialTextView ArtistName { get; private set; }
    public ImageView FavIcon { get; private set; }
    public MaterialTextView AlbumName { get; private set; }
    public MaterialTextView PlayCount { get; private set; }
    public MaterialTextView FileFormatText { get; private set; }
    public Slider SeekSlider { get; private set; }
    public Slider VolumeSlider { get; private set; }
    public bool IsDragging { get; private set; } = false;
    public string ArtTransitionName { get; set; }
    public string TitleTransitionName { get; set; }
    public string ArtistTransitionName { get; set; }
    public string AlbumTransitionName { get; set; }
    public NowPlayingFragment()
    {
        
    }

    public NowPlayingFragment(BaseViewModelAnd viewModel)
    {
        MyViewModel = viewModel;
    }
    public override View OnCreateView(LayoutInflater? inflater, ViewGroup? container, Bundle? savedInstanceState)
    {


        var ctx = Context!;
        // 1. MAIN SCROLL VIEW (Vertical)
        var mainScrollView = new ScrollView(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent),
            FillViewport = true
        };

        // 2. MAIN CONTAINER (Vertical Stack)
        var mainStack = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent)
        };
        mainStack.SetPadding(DpToPx(20), DpToPx(20), DpToPx(20), DpToPx(20));

        // --- ELEMENT 1: Song Title ---
        SongTitle = new MaterialTextView(ctx)
        {
            Text = MyViewModel.CurrentPlayingSongView.Title,
            TextSize = 24f,
            Gravity = GravityFlags.Center,
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent)
        };
        ((LinearLayout.LayoutParams)SongTitle.LayoutParameters).BottomMargin = DpToPx(16);
        if (!string.IsNullOrEmpty(TitleTransitionName))
            SongTitle.TransitionName = TitleTransitionName;

        // --- ELEMENT 2: CardView with Image + Overlay ---
        var albumCard = new MaterialCardView(ctx)
        {
            Radius = DpToPx(24),
            CardElevation = DpToPx(8),
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                DpToPx(350)) // Fixed Height 350dp
        };
        ((LinearLayout.LayoutParams)albumCard.LayoutParameters).BottomMargin = DpToPx(24);

        // Internal Frame to stack Image and Lyrics
        var cardInternalFrame = new FrameLayout(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };

        // Layer A: Album Art
        AlbumArtImage = new ImageView(ctx)
        {
            LayoutParameters = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };
        AlbumArtImage.SetScaleType(ImageView.ScaleType.CenterCrop);
        if (!string.IsNullOrEmpty(MyViewModel.CurrentPlayingSongView.CoverImagePath) && System.IO.File.Exists(MyViewModel.CurrentPlayingSongView.CoverImagePath))
        {
            // Load from disk
            var bmp = Android.Graphics.BitmapFactory.DecodeFile(MyViewModel.CurrentPlayingSongView.CoverImagePath);

            AlbumArtImage.SetImageBitmap(bmp);
        }

        if (!string.IsNullOrEmpty(ArtTransitionName))
            AlbumArtImage.TransitionName = ArtTransitionName;


        // Layer B: Border/Lyrics Layout (Superposed)
        LyricsOverlay = new FrameLayout(ctx)
        {
            LayoutParameters = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };
        // Semi-transparent dark overlay to make lyrics readable
        LyricsOverlay.SetBackgroundColor(Color.ParseColor("#66000000"));

        // Add a sample text to the overlay to show it works
         LyricsPlaceholder = new TextView(ctx)
        {
            Text = "Lyrics will appear here...",
            Gravity = GravityFlags.Center,
            LayoutParameters = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent,
                GravityFlags.Center)
        };
        LyricsPlaceholder.SetTextColor(Color.White);
        LyricsOverlay.AddView(LyricsPlaceholder);

        cardInternalFrame.AddView(AlbumArtImage);
        cardInternalFrame.AddView(LyricsOverlay);
        albumCard.AddView(cardInternalFrame);


        // --- ELEMENT 3: Artist + Fav Icon (Row) ---
        var artistRow = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };

        ArtistName = new MaterialTextView(ctx)
        {
            Text = MyViewModel.CurrentPlayingSongView.ArtistName,
            TextSize = 18f,
            LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f) // Weight 1
        };
        if (!string.IsNullOrEmpty(ArtistTransitionName))
            ArtistName.TransitionName = ArtistTransitionName;


        FavIcon = new ImageView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(DpToPx(24), DpToPx(24))
        };
        FavIcon.SetImageResource(Resource.Drawable.heart); // Replace with your Heart icon
        FavIcon.ImageTintList = ColorStateList.ValueOf(Color.DarkSlateBlue);

        artistRow.AddView(ArtistName);
        artistRow.AddView(FavIcon);


        // --- ELEMENT 4: Album + Play Count (Row) ---
        var albumRow = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        ((LinearLayout.LayoutParams)albumRow.LayoutParameters).TopMargin = DpToPx(8);

        AlbumName = new MaterialTextView(ctx)
        {
            Text = MyViewModel.CurrentPlayingSongView.AlbumName,
            TextSize = 14f,
            LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f)
        };
        AlbumName.SetTextColor(Color.Gray);

        PlayCount = new MaterialTextView(ctx)
        {
            Text = $"Plays: {MyViewModel.CurrentPlayingSongView.PlayCompletedCount}",
            TextSize = 12f,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
        };

        albumRow.AddView(AlbumName);
        albumRow.AddView(PlayCount);


        // --- ELEMENT 5: 25% Btn | 75% Btn ---
        var rowBtnA = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        ((LinearLayout.LayoutParams)rowBtnA.LayoutParameters).TopMargin = DpToPx(20);

        var btn1_25 = new MaterialButton(ctx)
        {
            Text = "Opt",
            LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 0.25f) // 25%
        };

        var btn1_75 = new MaterialButton(ctx)
        {
            Text = "Main Action",
            LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 0.75f) // 75%
        };
        // Add margin between
        ((LinearLayout.LayoutParams)btn1_75.LayoutParameters).LeftMargin = DpToPx(8);

        rowBtnA.AddView(btn1_25);
        rowBtnA.AddView(btn1_75);


        // --- ELEMENT 6: 75% Btn | 25% Btn ---
        var rowBtnB = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        ((LinearLayout.LayoutParams)rowBtnB.LayoutParameters).TopMargin = DpToPx(8);

        var btn2_75 = new MaterialButton(ctx)
        {
            Text = "Secondary Action",
            LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 0.75f) // 75%
        };

        var btn2_25 = new MaterialButton(ctx)
        {
            Text = "X",
            LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 0.25f) // 25%
        };
        ((LinearLayout.LayoutParams)btn2_25.LayoutParameters).LeftMargin = DpToPx(8);

        rowBtnB.AddView(btn2_75);
        rowBtnB.AddView(btn2_25);


        // --- ELEMENT 7: File Format Text ---
        FileFormatText = new MaterialTextView(ctx)
        {
            Text = "FLAC 16bit - 44.1kHz",
            TextSize = 12f,
            Gravity = GravityFlags.Center,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        FileFormatText.SetTextColor(Color.DarkGray);
        ((LinearLayout.LayoutParams)FileFormatText.LayoutParameters).TopMargin = DpToPx(16);

        
        // --- ELEMENT 8: Seek Slider ---
        SeekSlider = new Slider(ctx)
        {
            ValueFrom = 0f,
            ValueTo = 100f,
            Value = 30f,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
       

        ((LinearLayout.LayoutParams)SeekSlider.LayoutParameters).TopMargin = DpToPx(16);
        SeekSlider.Touch += (sender, e) =>
        {
            // 1. IMPORTANT: Set Handled to false so the Slider still slides!
            e.Handled = false;

            if (e.Event?.Action == MotionEventActions.Down)
            {
                IsDragging = true; // User grabbed the handle
            }
            else if (e.Event?.Action == MotionEventActions.Up || e.Event?.Action == MotionEventActions.Cancel)
            {
                IsDragging = false; // User let go
                if (sender is Slider s)
                {
                    OnSeekCompleted(s.Value);
                }
            }
        };
        // --- ELEMENT 9: Volume Row (10% | 80% | 10%) ---
        var volRow = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        ((LinearLayout.LayoutParams)volRow.LayoutParameters).TopMargin = DpToPx(8);
        volRow.SetGravity(GravityFlags.CenterVertical);

        var volDownImg = new ImageView(ctx) { LayoutParameters = new LinearLayout.LayoutParams(0, DpToPx(24), 0.1f) };
        volDownImg.SetImageResource(Android.Resource.Drawable.IcLockSilentModeOff); // Replace with vol- icon

        VolumeSlider = new  Slider(ctx)
        {
            ValueFrom = 0f,
            ValueTo = 100f,
            Value = 50f,
            LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 0.8f)
        };
        // Optional: Add listener for volume too

        var volUpImg = new ImageView(ctx) { LayoutParameters = new LinearLayout.LayoutParams(0, DpToPx(24), 0.1f) };
        volUpImg.SetImageResource(Android.Resource.Drawable.IcLockSilentModeOff); // Replace with vol+ icon

        volRow.AddView(volDownImg);
        volRow.AddView(VolumeSlider);
        volRow.AddView(volUpImg);


        // --- ELEMENT 10: Right Aligned Chips (View Queue, Sleep) ---
        var chipsLayout = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        chipsLayout.SetGravity(GravityFlags.End); // Push to Right
        ((LinearLayout.LayoutParams)chipsLayout.LayoutParameters).TopMargin = DpToPx(16);
        ((LinearLayout.LayoutParams)chipsLayout.LayoutParameters).BottomMargin = DpToPx(40); // Bottom padding for scroll

        var queueChip = new Chip(ctx) { Text = "View Queue" };
        queueChip.SetEnsureMinTouchTargetSize(false); // Compact style

        var sleepChip = new Chip(ctx) { Text = "Sleep Timer" };
        sleepChip.SetEnsureMinTouchTargetSize(false);
        var sleepChipLyParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
        sleepChipLyParams.LeftMargin = DpToPx(8);
        sleepChip.LayoutParameters = sleepChipLyParams;

        chipsLayout.AddView(queueChip);
        chipsLayout.AddView(sleepChip);


        // --- BUILD TREE ---
        mainStack.AddView(SongTitle);
        mainStack.AddView(albumCard);
        mainStack.AddView(artistRow);
        mainStack.AddView(albumRow);
        mainStack.AddView(rowBtnA);
        mainStack.AddView(rowBtnB);
        mainStack.AddView(FileFormatText);
        mainStack.AddView(SeekSlider);
        mainStack.AddView(volRow);
        mainStack.AddView(chipsLayout);

        mainScrollView.AddView(mainStack);

        return mainScrollView;
    }

    // --- HELPER: Handle Slider Drop (Debounced) ---
    private void OnSeekCompleted(double value)
    {
        // This method fires ONLY when the user lets go of the handle
        // Implement your ViewModel command here
        Toast.MakeText(Context, $"Seek to: {value}", ToastLength.Short)?.Show();
        var position = MyViewModel.CurrentPlayingSongView.DurationInSeconds * (value / 100.0);
        MyViewModel.SeekTrackPositionCommand.Execute(value);
    }

    // --- HELPER: DP to PX ---
    private int DpToPx(int dp)
    {
        return (int)(dp * Resources!.DisplayMetrics!.Density);
    }


}

