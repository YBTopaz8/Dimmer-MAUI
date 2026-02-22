namespace Dimmer.ViewsAndPages.NativeViews.Misc;


public class SongDetailOverlayFragment : Fragment
{
    private readonly SongModelView _song;
    private readonly string _imageTransitionName;
    private readonly string _titleTransitionName;

    private FrameLayout _rootScrim;

    public SongDetailOverlayFragment(SongModelView song, string imageTransName, string titleTransName)
    {
        _song = song;
        _imageTransitionName = imageTransName;
        _titleTransitionName = titleTransName;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context!;

        // 1. The Background Scrim (Full Screen)
        _rootScrim = new FrameLayout(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(-1, -1)
        };

        // API 31+ Real Blur, Pre-31 Dark Scrim
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.S)
        {
            _rootScrim.SetBackgroundColor(Color.ParseColor("#66000000"));
           

        }
        else
        {
            _rootScrim.SetBackgroundColor(Color.ParseColor("#AA000000")); // Just dark
        }

        // Fade in the scrim manually
        _rootScrim.Alpha = 0f;
        _rootScrim.Animate()?.Alpha(1f).SetDuration(300).Start();

        // Click background to dismiss (reverse animation)
        _rootScrim.Click += (s, e) =>
        {
            Dismiss();
        };

        // 2. The Centered Card (The popup content)
        var card = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(20),
            CardElevation = AppUtil.DpToPx(12),
            
        };
        card.SetCardBackgroundColor(Color.ParseColor("#1E1E1E")); // Dark theme parity
        var cardLp = new FrameLayout.LayoutParams(AppUtil.DpToPx(320), ViewGroup.LayoutParams.WrapContent)
        {
            Gravity = GravityFlags.Center
        };
        card.LayoutParameters = cardLp;

        // Prevent clicks on the card from dismissing the fragment
        card.Click += (s, e) => 
        { 
            /* Consume click */ 
        };

        var cardContent = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new ViewGroup.LayoutParams(-1, -2)
        };
        cardContent.SetPadding(AppUtil.DpToPx(24), AppUtil.DpToPx(24), AppUtil.DpToPx(24), AppUtil.DpToPx(24));

        // 3. The Shared Image
        var imgView = new ImageView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(AppUtil.DpToPx(200), AppUtil.DpToPx(200))
            {
                Gravity = GravityFlags.CenterHorizontal,
                BottomMargin = AppUtil.DpToPx(16)
            }
        };
        imgView.SetScaleType(ImageView.ScaleType.CenterCrop);
        // CRITICAL: Set the exact same transition name sent from the Adapter
        ViewCompat.SetTransitionName(imgView, _imageTransitionName);

        if (!string.IsNullOrEmpty(_song.CoverImagePath))
            Glide.With(this).Load(_song.CoverImagePath).Into(imgView);

        // 4. The Shared Title
        var titleView = new MaterialTextView(ctx)
        {
            Text = _song.Title,
            TextSize = 22,
            Typeface = Typeface.DefaultBold,
            Gravity = GravityFlags.CenterHorizontal
        };
        titleView.SetTextColor(Color.White);
        // CRITICAL: Set the exact same transition name sent from the Adapter
        ViewCompat.SetTransitionName(titleView, _titleTransitionName);

        // Add some extra controls inside the popup (Play, Add to Queue, etc.)
        var playBtn = new MaterialButton(ctx) { Text = "Play Now" };
        playBtn.LayoutParameters = new LinearLayout.LayoutParams(-1, -2) { TopMargin = AppUtil.DpToPx(20) };
        playBtn.Click += (s, e) =>
        {
            // Call ViewModel to play
            Dismiss();
        };
        ChipGroup optionsChipGroup = new ChipGroup(ctx);
        cardContent.AddView(imgView);
        cardContent.AddView(titleView);
        cardContent.AddView(playBtn);
        card.AddView(cardContent);

        _rootScrim.AddView(card);

        return _rootScrim;
    }

    public void Dismiss()
    {
        // Fade out scrim
        _rootScrim.Animate().Alpha(0f).SetDuration(250).Start();
        // Pop the backstack to trigger the return Shared Element Transition
        ParentFragmentManager.PopBackStack();
    }
}