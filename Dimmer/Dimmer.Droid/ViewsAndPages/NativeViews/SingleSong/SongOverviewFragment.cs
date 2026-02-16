using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AndroidX.Lifecycle;

using CommunityToolkit.Diagnostics;
using Dimmer.UiUtils;
using Dimmer.ViewsAndPages.NativeViews.AlbumSection;
using Dimmer.ViewsAndPages.NativeViews.ArtistSection;
using Google.Android.Material.Chip;
using Microsoft.Maui;

namespace Dimmer.ViewsAndPages.NativeViews.SingleSong;

public partial class SongOverviewFragment : Fragment
{
    private BaseViewModelAnd MyViewModel;

    public SongModelView
        SelectedSong { get; }

    private StatisticsViewModel statisticsViewModel;

    public Chip titleText { get; private set; }
    public Button AlbumText { get; private set; }

    public SongOverviewFragment(BaseViewModelAnd vm) { MyViewModel = vm;

        if(vm.SelectedSong == null)
            throw new ArgumentNullException(nameof(vm.SelectedSong),"Specifically Selected Song");
        SelectedSong = MyViewModel.SelectedSong!;
        statisticsViewModel = MainApplication.ServiceProvider.GetService<StatisticsViewModel>()!;
    }
    private View CreateArtistChips(Context ctx)
    {
        // ChipGroup handles the wrapping of chips automatically
        var chipGroup = new Google.Android.Material.Chip.ChipGroup(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(-1, -2) { BottomMargin = 20 }
        };
        chipGroup.SetChipSpacing(AppUtil.DpToPx(8));

        // Get the list of artists from your existing logic
        List<ArtistModelView?> artists = SelectedSong.ArtistsInDB(MyViewModel.RealmFactory)!;

        foreach (var art in artists)
        {
            if (art is null) continue;

            var chip = new Google.Android.Material.Chip.Chip(ctx)
            {
                Text = art.Name,
                Clickable = true,
                Checkable = false,
            };

            // MD3 Styling: Outlined chips look great for metadata
            chip.SetChipDrawable(Google.Android.Material.Chip.ChipDrawable.CreateFromAttributes(ctx, null, 0,
                Resource.Style.Widget_Material3_Chip_Suggestion_Elevated));

            // Add a small artist/person icon
            chip.SetChipIconResource(Resource.Drawable.abc_ic_menu_copy_mtrl_am_alpha); // Replace with your artist icon
            //chip.SetChipIconVisible(0);

            // Navigation Logic
            chip.Click += (s, e) =>
            {
                MyViewModel.SetSelectedArtist(art);
                MyViewModel.NavigateToArtistPage(this, art.Id.ToString(), art, ((View?)s)!);
            };

            chipGroup.AddView(chip);
        }

        return chipGroup;
    }
    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context;
        var scroll = new Android.Widget.ScrollView(ctx)
        {
            FillViewport = true,
            LayoutParameters = new ViewGroup.LayoutParams(-1, -1)
        };
        // Main Container
        var root = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        // 2. Give the root LinearLayout WrapContent height so it can grow
        root.LayoutParameters = new FrameLayout.LayoutParams(-1, -2);
        root.SetPadding(32, 32, 32, 32);

        // 1. PRIMARY METADATA (Artist/Album Chips)
        var chipContainer = new LinearLayout(ctx) { Orientation = Orientation.Horizontal }
        ;
        chipContainer.AddView(CreateArtistChips(ctx));
        root.AddView(chipContainer);

        // 2. THE DASHBOARD GRID (Listening Stats)
        var statsTitle = CreateHeader(ctx, "Listening Insights");
        root.AddView(statsTitle);

        var statsGrid = new GridLayout(ctx) { ColumnCount = 2, AlignmentMode = GridAlign.Bounds };

        // Card 1: Playback Vitality
        statsGrid.AddView(CreateMetricCard(ctx, "Total Plays", SelectedSong.PlayCount.ToString(), Resource.Drawable.playcircle, Color.ParseColor("#4CAF50")));

        // Card 2: Skip Tendency (Red/Orange if high)
        var skipColor = SelectedSong.SkipRate > 0.5 ? Color.Red : Color.ParseColor("#FF9800");
        statsGrid.AddView(CreateMetricCard(ctx, "Skip Rate", $"{SelectedSong.SkipRate:P0}", Resource.Drawable.media3_icon_skip_forward, skipColor));

        // Card 3: Completion
        statsGrid.AddView(CreateMetricCard(ctx, "Finished", SelectedSong.PlayCompletedCount.ToString(), Android.Resource.Drawable.StatSysUploadDone, Color.ParseColor("#2196F3")));

        // Card 4: Engagement Score
        statsGrid.AddView(CreateMetricCard(ctx, "Engagement", SelectedSong.EngagementScore.ToString("F1"), Resource.Drawable.mtrl_ic_arrow_drop_up, Color.ParseColor("#9C27B0")));

        root.AddView(statsGrid);

        // 3. AUDIO DNA (Technical Specs)
        root.AddView(CreateHeader(ctx, "Audio DNA"));
        var dnaCard = new MaterialCardView(ctx) { Radius = 24, StrokeWidth = 2 };
        dnaCard.SetStrokeColor(ColorStateList.ValueOf(Color.ParseColor("#33808080")));

        var dnaLayout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        dnaLayout.SetPadding(40, 40, 40, 40);

        dnaLayout.AddView(CreateDnaRow(ctx, "Format", $"{SelectedSong.FileFormat.ToUpper()} ({SelectedSong.BitDepth} bit)"));
        dnaLayout.AddView(CreateDnaRow(ctx, "Sample Rate", $"{SelectedSong.SampleRate / 1000:F1} kHz"));
        dnaLayout.AddView(CreateDnaRow(ctx, "Bitrate", $"{SelectedSong.BitRate} kbps"));
        dnaLayout.AddView(CreateDnaRow(ctx, "Channels", SelectedSong.NbOfChannels == 2 ? "Stereo" : "Mono"));

        dnaCard.AddView(dnaLayout);
        root.AddView(dnaCard);

        // 4. USER CONTEXT (Notes & Achievements)
        if (SelectedSong.UserNoteAggregatedCol?.Any() == true)
        {
            root.AddView(CreateHeader(ctx, "Your Notes"));
        }

        scroll.AddView(root);
        return scroll;
    }
    private View CreateMetricCard(Context ctx, string label, string value, int iconRes, Color themeColor)
    {
        var card = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(20),
            CardElevation = 0,
            StrokeWidth = 3,
        };
        card.SetStrokeColor(ColorStateList.ValueOf(Color.ParseColor("#10808080")));

        var lp = new GridLayout.LayoutParams();
        lp.Width = 0;
        lp.Height = ViewGroup.LayoutParams.WrapContent;
        lp.ColumnSpec = GridLayout.InvokeSpec(GridLayout.Undefined, 1f);
        lp.SetMargins(12, 12, 12, 12);
        card.LayoutParameters = lp;

        var layout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        layout.SetPadding(30, 30, 30, 30);

        var icon = new ImageView(ctx);
        icon.SetImageResource(iconRes);
        icon.SetColorFilter(themeColor);
        icon.LayoutParameters = new LinearLayout.LayoutParams(60, 60) { BottomMargin = 16 };

        var valTxt = new TextView(ctx) { Text = value, TextSize = 20, Typeface = Typeface.DefaultBold };
        var labTxt = new TextView(ctx) { Text = label, TextSize = 12, Alpha = 0.6f };

        layout.AddView(icon);
        layout.AddView(valTxt);
        layout.AddView(labTxt);
        card.AddView(layout);

        // Animation on Tap
        card.Click += (s, e) => {
            card.Animate().ScaleX(0.95f).ScaleY(0.95f).SetDuration(100).WithEndAction(new Java.Lang.Runnable(() => {
                card.Animate().ScaleX(1f).ScaleY(1f).SetDuration(100);
            }));
        };

        return card;
    }

    private View CreateDnaRow(Context ctx, string label, string val)
    {
        var row = new LinearLayout(ctx) { Orientation = Orientation.Horizontal, WeightSum = 1 };
        row.SetPadding(0, 8, 0, 8);
        var t1 = new TextView(ctx) { Text = label, LayoutParameters = new LinearLayout.LayoutParams(0, -2, 0.4f), Alpha = 0.6f };
        var t2 = new TextView(ctx) { Text = val, LayoutParameters = new LinearLayout.LayoutParams(0, -2, 0.6f), Typeface = Typeface.Monospace };
        row.AddView(t1);
        row.AddView(t2);
        return row;
    }

    private TextView CreateHeader(Context ctx, string text)
    {
        var tv = new TextView(ctx) { Text = text.ToUpper(), TextSize = 12, Typeface = Typeface.DefaultBold };
        tv.LetterSpacing=0.1f;
        tv.SetPadding(10, 40, 0, 10);
        tv.SetTextColor(Color.ParseColor("#808080"));
        return tv;
    }
    public override async void OnResume()
    {
        base.OnResume();
        //titleText.Click += PlaySong;
    _=    statisticsViewModel.LoadSongStatsAsync(SelectedSong);
    }

    private async void PlaySong(object? sender, EventArgs e)
    {
        await MyViewModel.PlayNextSongsImmediately(new List<SongModelView> { MyViewModel.SelectedSong! });
        
    }
    public override void OnDestroy()
    {
        base.OnDestroy();
        titleText.Click -= PlaySong;
    }

    private LinearLayout CreateLyricsView(Context context, string? lyrics)
    {
        lyrics ??= "No lyrics available for this song.";

        LinearLayout? form = new LinearLayout(context) { Orientation = Orientation.Vertical };

        var syncLyricsResult = new TextView(context!) { TextSize = 12 };
        syncLyricsResult.Text = lyrics;
        form.AddView(syncLyricsResult);
        return form;
    }

    private View CreateStat(Context ctx, string label, string val)
    {
        var ly = new LinearLayout(ctx) { Orientation = Orientation.Vertical};
        ly.SetPadding(10, 10, 10, 10);
        ly.AddView(new TextView(ctx) { Text = val, TextSize = 18, Typeface = Typeface.DefaultBold });
        ly.AddView(new TextView(ctx) { Text = label, TextSize = 12 });
        return ly;
    }
}