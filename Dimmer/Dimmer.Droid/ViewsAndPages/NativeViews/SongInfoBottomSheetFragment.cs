


using Dimmer.WinUI.UiUtils;

namespace Dimmer.ViewsAndPages.NativeViews;

public partial class SongInfoBottomSheetFragment : BottomSheetDialogFragment
{
    private BaseViewModelAnd MyViewModel;
    
    private SongModelView currentSong;

    public SongInfoBottomSheetFragment(BaseViewModelAnd vm, SongModelView currentSong)
    {
        this.currentSong = currentSong;
        MyViewModel = vm;
    }

    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        //return base.OnCreateView(inflater, container, savedInstanceState);

        var ctx = Context;

        var songTitle = new TextView(ctx!)
        {
            Text = currentSong.Title,
            TextSize = 20f,
            Typeface = Typeface.DefaultBold,
            Gravity = GravityFlags.Center
        };



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

        var skipsCard = new LinearLayout(ctx!);
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

        horizontalLayout.AddView(skipsCard);

        var totalNumberOfEventsCard = new LinearLayout(ctx!);
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

        horizontalLayout.AddView(totalNumberOfEventsCard);
        
        var songCardView = new LinearLayout(ctx!);
        
        var verticalLinearLayout = new LinearLayout(ctx!)
        {
            Orientation = Orientation.Vertical,
        };
        verticalLinearLayout.AddView(songTitle);
        verticalLinearLayout.AddView(horizontalLayout);

        songCardView.AddView(verticalLinearLayout);
        return songCardView;
    }
}