


using Android.Content;
using Android.Graphics.Drawables.Shapes;
using Android.Text;
using Dimmer.UiUtils;
using Dimmer.Utils.Extensions;

namespace Dimmer.ViewsAndPages.NativeViews;

public partial class SongInfoBottomSheetFragment : BottomSheetDialogFragment
{
    private  TextView _songTitleTv;
    private BaseViewModelAnd MyViewModel;
    
    private SongModelView currentSong;
    private TextView artistTotalNumberOfEventsTV;
    private TextView artistTotalNumberOfEventsLabel;
    private TextView artistTotalNumberOfSkipsTV;

    public SongInfoBottomSheetFragment(BaseViewModelAnd vm, SongModelView currentSong)
    {
        this.currentSong = currentSong;
        MyViewModel = vm;
    }

    public TextView ArtisttotalNumberOfPlaysCompleted { get; private set; }
    public ImageView SongImg { get; private set; }

    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        //return base.OnCreateView(inflater, container, savedInstanceState);

        var ctx = Context;

       



        var horizontalLayout = new LinearLayout(ctx!)
        {
            Orientation = Orientation.Horizontal,            
        };
        horizontalLayout.SetPadding(20, 20, 20, 20);
        horizontalLayout.SetGravity(GravityFlags.Center);

        var totalNumberOfPlaysCompleted = new TextView(ctx!)
        {
            Text = $"{currentSong.PlayCompletedCount}",
            TextSize = 16f,
            Gravity = GravityFlags.Center
        };
        totalNumberOfPlaysCompleted.TooltipText = "Total Number of Plays Completed";
        horizontalLayout.AddView(totalNumberOfPlaysCompleted);

        var skipsCardView = new CardView(ctx!);
        

        var skipsCard = new LinearLayout(ctx!);
        skipsCard.SetPadding(10, 10, 10, 10);
        skipsCard.Orientation = Android.Widget.Orientation.Vertical;
        var totalNumberOfSkips = new TextView(ctx!)
        {
            Text = $"{currentSong.SkipCount}",
            TextSize = 16f,
            Gravity = GravityFlags.Center
        };
        var skipsLabel = new TextView(ctx!)
        {
            Text = "Skips",
            TextSize = 16f,
            Gravity = GravityFlags.Center
        };
        skipsCard.AddView(totalNumberOfSkips);
        skipsCard.AddView(skipsLabel);

        skipsCardView.AddView(skipsCard);
        horizontalLayout.AddView(skipsCardView);

        var totalNumberOfEventsCardView = new CardView(ctx!);
        var totalNumberOfEventsCard = new LinearLayout(ctx!);
        totalNumberOfEventsCard.SetPadding(10, 10, 10, 10);
        totalNumberOfEventsCard.Orientation = Android.Widget.Orientation.Vertical;
        var totalNumberOfEvents = new TextView(ctx!)
        {
            Text = $"{currentSong.PlayCount}",
            TextSize = 16f,
            Gravity = GravityFlags.Center
        };
        var totalNumberOfEventsLabel = new TextView(ctx!)
        {
            Text = "Total Events",
            TextSize = 16f,
            Gravity = GravityFlags.Center
        };

        totalNumberOfEventsCard.AddView(totalNumberOfEvents);
        totalNumberOfEventsCard.AddView(totalNumberOfEventsLabel);

        totalNumberOfEventsCardView.AddView(totalNumberOfEventsCard);
        horizontalLayout.AddView(totalNumberOfEventsCardView);
        
        var songCardVieww = new CardView(ctx!);
        var songCardView = new LinearLayout(ctx!);
        songCardView.Orientation = Android.Widget.Orientation.Vertical;

        var verticalLinearLayout = new LinearLayout(ctx!)
        {
            Orientation = Orientation.Vertical,
        };

        var horizontalStackLayout = new LinearLayout(Context);
        horizontalStackLayout.Orientation = Android.Widget.Orientation.Horizontal;
        horizontalLayout.SetGravity(GravityFlags.CenterHorizontal);
        // 3. Header: Title (Marquee)
        _songTitleTv = new TextView(ctx)
        {
            TextSize = 24,
            Typeface = Typeface.DefaultBold,
            Ellipsize = TextUtils.TruncateAt.Marquee,
            HorizontalFadingEdgeEnabled = true,
            Selected = true // Required for Marquee
        };
        _songTitleTv.SetSingleLine(true);
        _songTitleTv.SetTextColor(Android.Graphics.Color.White);
        _songTitleTv.Text = MyViewModel.SelectedSong.Title;
        SongImg = new ImageView(ctx);

        var llyt = new ViewGroup.LayoutParams(AppUtil.DpToPx(80), AppUtil.DpToPx(80));
        SongImg.SetScaleType(ImageView.ScaleType.CenterCrop);
        SongImg.LayoutParameters = llyt;

        if (MyViewModel.SelectedSong is not null)
        {
            SongImg.SetImageWithGlide(MyViewModel.SelectedSong.CoverImagePath);

        }
        horizontalStackLayout.AddView(SongImg);
        horizontalStackLayout.AddView(_songTitleTv);


        verticalLinearLayout.AddView(horizontalStackLayout);
        verticalLinearLayout.AddView(horizontalLayout);

        songCardVieww.AddView(verticalLinearLayout);
        songCardView.AddView(songCardVieww);

        var artistCardView = new LinearLayout(ctx!);
        artistCardView.Orientation = Android.Widget.Orientation.Vertical;

        var artistTitle = new TextView(ctx!)
        {
            Text = currentSong.OtherArtistsName,
            TextSize = 18f,
            Typeface = Typeface.DefaultBold,
            Gravity = GravityFlags.Center
        };


        var artistHL = new LinearLayout(ctx!)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new ViewGroup.LayoutParams(
              ViewGroup.LayoutParams.MatchParent,
              150)
              //ViewGroup.LayoutParams.WrapContent)
        };
        artistHL.SetPadding(20, 20, 20, 20);
        artistHL.SetGravity(GravityFlags.Center);

        ArtisttotalNumberOfPlaysCompleted = new TextView(ctx!)
        {
            Text = $"{currentSong.PlayCompletedCount}",
            TextSize = 16f,
            Gravity = GravityFlags.Center
        };
        ArtisttotalNumberOfPlaysCompleted.TooltipText = "Total Number of Plays Completed";
        artistHL.AddView(ArtisttotalNumberOfPlaysCompleted);

        var artistSkipsCardView = new MaterialCardView(ctx!);
        var artistSkipsCard = new LinearLayout(ctx!);
        skipsCard.Orientation = Android.Widget.Orientation.Vertical;
        artistTotalNumberOfSkipsTV = new TextView(ctx!)
        {
            Text = $"{currentSong.SkipCount}",
            TextSize = 16f,
            Gravity = GravityFlags.Center
        };
        var artistSkipsLabel = new TextView(ctx!)
        {
            Text = "Skips",
            TextSize = 16f,
            Gravity = GravityFlags.Center
        };
        artistSkipsCard.AddView(artistTotalNumberOfSkipsTV);
        artistSkipsCard.AddView(artistSkipsLabel);
        artistSkipsCardView.AddView(artistSkipsCard);
        artistHL.AddView(artistSkipsCardView);

        var artistTotalNumberOfEventsCardView = new MaterialCardView(ctx!);
        artistTotalNumberOfEventsCardView.StrokeWidth = 1;
        artistTotalNumberOfEventsCardView.Radius = AppUtil.DpToPx(8);

        var artistTotalNumberOfEventsCard = new LinearLayout(ctx!);
        artistTotalNumberOfEventsCard.Orientation = Android.Widget.Orientation.Vertical;
         artistTotalNumberOfEventsTV = new TextView(ctx!)
        {
            Text = $"{currentSong.PlayCount}",
            TextSize = 16f,
            Gravity = GravityFlags.Center
        };
        artistTotalNumberOfEventsLabel = new TextView(ctx!)
        {
            Text = "Total Events",
            TextSize = 16f,
            Gravity = GravityFlags.Center
        };

        artistTotalNumberOfEventsCard.AddView(artistTotalNumberOfEventsTV);
        artistTotalNumberOfEventsCard.AddView(artistTotalNumberOfEventsLabel);
        artistTotalNumberOfEventsCardView.AddView(artistTotalNumberOfEventsCard);
        artistHL.AddView(artistTotalNumberOfEventsCardView);

        artistCardView.AddView(artistTitle);
        artistCardView.AddView(artistHL);

        var mainScrollView = new Android.Widget.ScrollView(ctx!);
       
        var rootFrame = new FrameLayout(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(
              ViewGroup.LayoutParams.MatchParent,
              ViewGroup.LayoutParams.MatchParent)
        };
        
        // 3. The Pill Container (MaterialCardView)
        var pillCard = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(25), // High radius for Pill shape
            CardElevation = AppUtil.DpToPx(6),
            CardBackgroundColor = Android.Content.Res.ColorStateList.ValueOf(Color.ParseColor("#303030")) // Dark grey pill
        };

        var pillParams = new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent,
            AppUtil.DpToPx(50)); // Fixed height for the pill

        // --- POSITIONING: Absolute Bottom Center ---
        pillParams.Gravity = GravityFlags.Bottom | GravityFlags.CenterHorizontal;
        pillParams.SetMargins(0, 0, 0, AppUtil.DpToPx(20)); // Lift it up slightly
        pillCard.LayoutParameters = pillParams;

        // 4. Horizontal Layout inside the Pill
        var pillContent = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.MatchParent)
        };
        pillContent.SetGravity(GravityFlags.Center);
        pillContent.SetPadding(AppUtil.DpToPx(15), 0, AppUtil.DpToPx(15), 0);

        // 5. The Eye Button
        var eyeBtn = new MaterialButton(ctx, null, Resource.Attribute.borderlessButtonStyle);
        eyeBtn.IconTint = Android.Content.Res.ColorStateList.ValueOf(Color.White);
        eyeBtn.Text = "Scroll To"; // Optional text, or remove for icon only
        eyeBtn.SetIconResource(Resource.Drawable.eye);
        eyeBtn.IconSize = AppUtil.DpToPx(18);
        eyeBtn.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Color.Transparent);

        // Button Action: Scroll to currently playing
        




        pillContent.AddView(eyeBtn);
        pillCard.AddView(pillContent);

        var linLayout = new LinearLayout(ctx!)
        {
            Orientation = Orientation.Vertical,
        };
        linLayout.SetPadding(20, 20, 20, 20);
        linLayout.AddView(songCardView);
        linLayout.AddView(artistCardView);

        rootFrame.AddView(linLayout);
        //rootFrame.AddView(pillCard);

        mainScrollView.AddView(rootFrame);
        return mainScrollView;
    }
}