using System.Threading.Channels;

namespace Dimmer.ViewsAndPages.NativeViews.Misc;


public class SongDetailOverlayFragment : Fragment
{
    private readonly BaseViewModelAnd MyViewModel;
    private readonly SongModelView _song;
    private readonly string _imageTransitionName;
    private readonly string _titleTransitionName;

    private FrameLayout _rootScrim;

    public SongDetailOverlayFragment(BaseViewModelAnd myViewModel, SongModelView song, string imageTransName, string titleTransName)
    {
        this.MyViewModel = myViewModel;
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
        var cardLp = new FrameLayout.LayoutParams(AppUtil.DpToPx(350), AppUtil.DpToPx(450))
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
            LayoutParameters = new LinearLayout.LayoutParams(AppUtil.DpToPx(150), AppUtil.DpToPx(150))
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
        ViewCompat.SetTransitionName(titleView, _titleTransitionName);

        var artistView = new MaterialTextView(ctx)
        {
            Text = _song.ArtistName,
            TextSize = 15,
            Typeface = Typeface.DefaultBold,
            Gravity = GravityFlags.CenterHorizontal
        };
        artistView.SetTextColor(Color.White);

        var albumView = new MaterialTextView(ctx)
        {
            Text = _song.AlbumName,
            TextSize = 15,
            Typeface = Typeface.DefaultBold,
            Gravity = GravityFlags.CenterHorizontal
        };
        albumView.SetTextColor(Color.White);

        var GenreNameView = new MaterialTextView(ctx)
        {
            Text = _song.GenreName,
            TextSize = 15,
            Typeface = Typeface.DefaultBold,
            Gravity = GravityFlags.CenterHorizontal
        };
        GenreNameView.SetTextColor(Color.White);

        var durationView = new Chip(ctx)
        {
            Text = _song.DurationFormatted,
            TextSize = 12,
            Typeface = Typeface.DefaultBold,
            Gravity = GravityFlags.CenterHorizontal
        };
        durationView.SetTextColor(Color.White);

        // Add some extra controls inside the popup (Play, Add to Queue, etc.)
      

        ChipGroup optionsChipGroup = new ChipGroup(ctx);
        optionsChipGroup.SetForegroundGravity(GravityFlags.CenterHorizontal);
        optionsChipGroup.SetBackgroundColor(Color.Transparent);
        //optionsChipGroup.AddView(vieww);

        var noteChip = new Chip(ctx);
        noteChip.SetChipIconResource(Resource.Drawable.pennewround);
        noteChip.LayoutParameters = new LinearLayout.LayoutParams(AppUtil.DpToPx(50),AppUtil.DpToPx(50)) { TopMargin = AppUtil.DpToPx(20) };
        noteChip.Click += (s, e) =>
        {
            Toast.MakeText(ctx, "note chip", ToastLength.Short);
        };

        var favoriteBtn = new Chip(ctx);
        favoriteBtn.SetChipIconResource(_song.IsFavorite ? Resource.Drawable.heart : Resource.Drawable.media3_icon_heart_unfilled);
        favoriteBtn.LayoutParameters = new LinearLayout.LayoutParams(AppUtil.DpToPx(50), AppUtil.DpToPx(50)) { TopMargin = AppUtil.DpToPx(20) };
        favoriteBtn.Click += async (s, e) =>
        {
            if (_song.IsFavorite)
            {
                await MyViewModel.AddFavoriteRatingToSongAsync(_song);
                favoriteBtn.SetChipIconResource(Resource.Drawable.media3_icon_heart_filled);
                Toast.MakeText(ctx, "song is faved", ToastLength.Short);
            }
            else
            {
                await MyViewModel.RemoveSongFromFavoriteAsync(_song);
                favoriteBtn.SetChipIconResource(Resource.Drawable.media3_icon_heart_unfilled);
                Toast.MakeText(ctx, "song is unfaved", ToastLength.Short);
            }
            
        };


        optionsChipGroup.Clickable = true;
        optionsChipGroup.AddView(noteChip);
        optionsChipGroup.AddView(favoriteBtn);
        
        cardContent.AddView(imgView);
        cardContent.AddView(titleView);
        cardContent.AddView(artistView);
        cardContent.AddView(albumView);
        cardContent.AddView(GenreNameView);

        

        ChipGroup statisChipGroup = new ChipGroup(ctx);
        statisChipGroup.Clickable = false;

        var playsCompletedChip = new Chip(ctx);
        playsCompletedChip.SetChipIconResource(Resource.Drawable.media3_icon_play);
        playsCompletedChip.Clickable = false;
        playsCompletedChip.Text = _song.PlayCompletedCount.ToString();
        statisChipGroup.AddView(playsCompletedChip);

        var pausedChip = new Chip(ctx);
        pausedChip.SetChipIconResource(Resource.Drawable.pausecircle);
        pausedChip.Clickable = false;
        pausedChip.Text = _song.PauseCount.ToString();
        statisChipGroup.AddView(pausedChip);


        var PlayStreakDaysChip = new Chip(ctx);
        PlayStreakDaysChip.SetChipIconResource(Resource.Drawable.calendardate);
        PlayStreakDaysChip.Clickable = false;
        PlayStreakDaysChip.Text = _song.PlayStreakDays.ToString();
        statisChipGroup.AddView(PlayStreakDaysChip);


        var SkipCountChip = new Chip(ctx);
        SkipCountChip.Clickable = false;
        SkipCountChip.SetChipIconResource(Resource.Drawable.forwardskip);
        SkipCountChip.Text = _song.SkipCount.ToString();
        statisChipGroup.AddView(SkipCountChip);

        var HasLyricsChip = new Chip(ctx);
        HasLyricsChip.Clickable = false;
        HasLyricsChip.SetChipIconResource(Resource.Drawable.lyrics);
        HasLyricsChip.Visibility = _song.HasLyrics ? ViewStates.Visible : ViewStates.Gone;

        statisChipGroup.AddView(HasLyricsChip);


        cardContent.AddView(optionsChipGroup);
        cardContent.AddView(statisChipGroup);
        

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