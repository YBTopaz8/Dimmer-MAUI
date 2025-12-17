using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Diagnostics;

using Microsoft.Maui;

namespace Dimmer.ViewsAndPages.NativeViews.SingleSong;

public class SongOverviewFragment : Fragment
{
    private BaseViewModelAnd _vm;

    public SongModelView
        SelectedSong { get; }

    public SongOverviewFragment(BaseViewModelAnd vm) { _vm = vm;

        if(vm.SelectedSong == null)
            throw new ArgumentNullException(nameof(vm.SelectedSong),"Specifically Selected Song");
        SelectedSong = _vm.SelectedSong!;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context;
        var scroll = new Android.Widget.ScrollView(ctx);
        var root = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        root.SetPadding(20, 20, 20, 20);


        // Title/Artist
        var titleText = new TextView(ctx) 
        {
            Text = SelectedSong.Title, TextSize = 24, Typeface = Typeface.DefaultBold
        };
        
        titleText.SetForegroundGravity(GravityFlags.CenterHorizontal);


        root.AddView(titleText);
        var artistBtn = new MaterialButton(ctx, null, Resource.Attribute.borderlessButtonStyle) { Text = SelectedSong.ArtistName, TextSize = 18 };
        // Parity: Navigate to Artist
        artistBtn.Click += (s, e) =>
        {
            _vm.NavigateToArtistPage(ParentFragment, "artist_trans", SelectedSong.OtherArtistsName, artistBtn);
        };
        root.AddView(artistBtn);

        // Stats Card
        var statsCard = new MaterialCardView(ctx) { Radius = AppUtil.DpToPx(12), Elevation = 4 };
        var statsGrid = new GridLayout(ctx);
        statsGrid.SetPadding(20, 20, 20, 20);
        statsGrid.AddView(CreateStat(ctx, "Plays", SelectedSong.PlayCount.ToString()));
        statsGrid.AddView(CreateStat(ctx, "Skips", SelectedSong.SkipCount.ToString()));
        statsCard.AddView(statsGrid);
        root.AddView(statsCard);


        // --- ACHIEVEMENTS SECTION ---
        // Basic implementation to match WinUI's "AllAchievementsIR"
        var achTitle = new TextView(ctx) { Text = "Achievements", TextSize = 18, Typeface = Typeface.DefaultBold };
        //((LinearLayout.LayoutParams)achTitle.LayoutParameters).TopMargin = AppUtil.DpToPx(24);
        root.AddView(achTitle);

        
            root.AddView(new TextView(ctx) { Text = "No achievements yet...", Alpha = 0.6f });
        scroll.SetBackgroundColor(Color.Transparent);


        scroll.AddView(root);



        return scroll;
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