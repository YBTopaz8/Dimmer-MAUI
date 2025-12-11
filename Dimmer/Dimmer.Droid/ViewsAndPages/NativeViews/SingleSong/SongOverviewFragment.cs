using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Maui;

namespace Dimmer.ViewsAndPages.NativeViews.SingleSong;

public class SongOverviewFragment : Fragment
{
    private BaseViewModelAnd _vm;
    public SongOverviewFragment(BaseViewModelAnd vm) { _vm = vm; }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context;
        var scroll = new Android.Widget.ScrollView(ctx);
        var root = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        root.SetPadding(20, 20, 20, 20);

        // Title/Artist
        root.AddView(new TextView(ctx) { Text = _vm.SelectedSong?.Title, TextSize = 24, Typeface = Typeface.DefaultBold });
        var artistBtn = new MaterialButton(ctx, null, Resource.Attribute.borderlessButtonStyle) { Text = _vm.SelectedSong?.ArtistName, TextSize = 18 };
        // Parity: Navigate to Artist
        artistBtn.Click += (s, e) => _vm.NavigateToArtistPage(ParentFragment, "artist_trans", _vm.SelectedSong?.ArtistName, null);
        root.AddView(artistBtn);

        // Stats Card
        var statsCard = new MaterialCardView(ctx) { Radius = AppUtil.DpToPx(12), Elevation = 4 };
        var statsGrid = new GridLayout(ctx);
        statsGrid.SetPadding(20, 20, 20, 20);
        statsGrid.AddView(CreateStat(ctx, "Plays", _vm.SelectedSong?.PlayCount.ToString()));
        statsGrid.AddView(CreateStat(ctx, "Skips", _vm.SelectedSong?.SkipCount.ToString()));
        statsCard.AddView(statsGrid);
        root.AddView(statsCard);

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