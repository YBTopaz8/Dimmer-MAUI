using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AndroidX.Lifecycle;

using CommunityToolkit.Diagnostics;
using Dimmer.UiUtils;
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
    public TextView AlbumText { get; private set; }

    public SongOverviewFragment(BaseViewModelAnd vm) { MyViewModel = vm;

        if(vm.SelectedSong == null)
            throw new ArgumentNullException(nameof(vm.SelectedSong),"Specifically Selected Song");
        SelectedSong = MyViewModel.SelectedSong!;
        statisticsViewModel = MainApplication.ServiceProvider.GetService<StatisticsViewModel>()!;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context;
        var scroll = new Android.Widget.ScrollView(ctx);
        scroll.LayoutParameters = new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
        var root = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        root.LayoutParameters = new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);

        root.SetPadding(20, 20, 20, 20);


        // Title/Artist
         titleText = new Chip(ctx) 
        {
            Text = SelectedSong.Title, TextSize = 24, Typeface = Typeface.DefaultBold
        };
        titleText.Click += async (s, e) =>
        {
            MyViewModel.AddToNext(new List<SongModelView>() { SelectedSong });
            await MyViewModel.NextTrackAsync();
        };
        titleText.SetForegroundGravity(GravityFlags.CenterHorizontal);



        root.AddView(titleText);





         List<ArtistModelView?> artists = MyViewModel.SelectedSong.ArtistsInDB(MyViewModel.RealmFactory)!;
        foreach (var art in artists)
        {
            if (art is null) continue;
            var artistBtn = new MaterialButton(ctx, null, Resource.Attribute.borderlessButtonStyle) { Text = art.Name, TextSize = 18 };
            artistBtn.Click += async (s, e) =>
            {
                MyViewModel.SelectedArtist = art;
               
                MyViewModel.NavigateToArtistPage(this, art.Id.ToString(), art, (MaterialButton)s!);
            };
            artistBtn.Tag = $"artist{art.Id}";
            root.AddView(artistBtn);
        }


        // Album
        AlbumText = new TextView(ctx)
        {
            Text = SelectedSong.AlbumName,
            TextSize = 14,
            Typeface = Typeface.DefaultBold
        };

        AlbumText.SetForegroundGravity(GravityFlags.CenterHorizontal);



        root.AddView(AlbumText);

        

        var genreBtn = new MaterialButton(ctx, null, Resource.Attribute.borderlessButtonStyle) { Text = SelectedSong.GenreName, TextSize = 10 };
        // Parity: Navigate to Artist
        genreBtn.Click += (s, e) =>
        {
            //viewModel.NavigateToArtistPage(ParentFragment, "artist_trans", SelectedSong.OtherArtistsName, artistBtn);
        };
        root.AddView(genreBtn);

        // Stats Card
        var statsCard = new MaterialCardView(ctx) { Radius = AppUtil.DpToPx(12), Elevation = 4 };
        var statsGrid = new GridLayout(ctx);
        statsGrid.SetPadding(20, 20, 20, 20);
        statsGrid.AddView(CreateStat(ctx, "Plays", SelectedSong.PlayCount.ToString()));
        statsGrid.AddView(CreateStat(ctx, "Skips", SelectedSong.SkipCount.ToString()));
        statsCard.AddView(statsGrid);
        root.AddView(statsCard);


        var lyricsCardView = UiBuilder.CreateSectionCard(ctx
            ,"Lyrics",(CreateLyricsView(ctx, MyViewModel.SelectedSong?.SyncLyrics))
            );

        root.AddView(lyricsCardView);

        var achTitle = new TextView(ctx) { Text = "Achievements", TextSize = 18, Typeface = Typeface.DefaultBold };
        //((LinearLayout.LayoutParams)achTitle.LayoutParameters).TopMargin = AppUtil.DpToPx(24);
        //root.AddView(achTitle);

        
            //root.AddView(new TextView(ctx) { Text = "No achievements yet...", Alpha = 0.6f });
        scroll.SetBackgroundColor(Color.Transparent);




        scroll.AddView(root);



        return scroll;

    }

    public override async void OnResume()
    {
        base.OnResume();
        titleText.Click += PlaySong;
        await statisticsViewModel.LoadSongStatsAsync(SelectedSong);
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