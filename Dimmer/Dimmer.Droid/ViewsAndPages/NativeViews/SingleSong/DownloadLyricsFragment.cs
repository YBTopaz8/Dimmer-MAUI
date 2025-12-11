using Dimmer.Data.Models.LyricsModels;

using ProgressBar = Android.Widget.ProgressBar;

namespace Dimmer.ViewsAndPages.NativeViews.SingleSong;

public partial class DownloadLyricsFragment : Fragment
{
    private BaseViewModelAnd MyViewModel;
    private TextInputEditText titleInput, artistInput, albumInput;
    private RecyclerView resultsRecycler;
    private ProgressBar loadingBar;

    public DownloadLyricsFragment(BaseViewModelAnd vm)
    {
        MyViewModel = vm;
    }


    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context;
        var root = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        };
        root.SetPadding(20, 20, 20, 20);

        // 1. Search Inputs
        titleInput = new TextInputEditText(ctx) { Hint = "Track Name", Text = MyViewModel.SelectedSong?.Title };
        artistInput = new TextInputEditText(ctx) { Hint = "Artist", Text = MyViewModel.SelectedSong?.ArtistName };
        albumInput = new TextInputEditText(ctx) { Hint = "Album", Text = MyViewModel.SelectedSong?.AlbumName };

        var searchBtn = new MaterialButton(ctx) { Text = "Search Lyrics" };
        searchBtn.Click += SearchBtn_Click;

        root.AddView(titleInput);
        root.AddView(artistInput);
        root.AddView(albumInput);
        root.AddView(searchBtn);

        // 2. Loading Indicator
        loadingBar = new ProgressBar(ctx) { Indeterminate = true, Visibility = ViewStates.Gone };
        root.AddView(loadingBar);

        // 3. Results List
        resultsRecycler = new RecyclerView(ctx);
        resultsRecycler.SetLayoutManager(new LinearLayoutManager(ctx));
        resultsRecycler.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
        root.AddView(resultsRecycler);

        return root;
    }

    private async void SearchBtn_Click(object sender, EventArgs e)
    {
        loadingBar.Visibility = ViewStates.Visible;

        // Mocking the Service Call - Replace with your actual Retrofit/HttpClient call
        // var results = await MyViewModel.LyricsService.GetLyrics(titleInput.Text, artistInput.Text, albumInput.Text);

        await System.Threading.Tasks.Task.Delay(1000); // Fake delay

        // Dummy Data for demonstration
        var results = new List<LrcLibLyrics>
        {
            new LrcLibLyrics { TrackName = titleInput.Text, ArtistName = artistInput.Text, SyncedLyrics = "[00:10] Hello world", PlainLyrics = "Hello world" },
            new LrcLibLyrics { TrackName = titleInput.Text + " (Remix)", ArtistName = artistInput.Text, PlainLyrics = "Lyrics here..." }
        };

        resultsRecycler.SetAdapter(new LyricsAdapter(results, OnLyricsSelected));
        loadingBar.Visibility = ViewStates.Gone;
    }

    private async void OnLyricsSelected(LrcLibLyrics lyrics)
    {
        // Save logic here
        if (MyViewModel.SelectedSong != null)
        {
            MyViewModel.SelectedSong.UnSyncLyrics = lyrics.SyncedLyrics ?? lyrics.PlainLyrics;

            await MyViewModel.ApplyNewSongEdits(MyViewModel.SelectedSong);
            Toast.MakeText(Context, "Lyrics Saved!", ToastLength.Short)?.Show();
            ParentFragmentManager.PopBackStack();
        }
    }

    // --- Inner Adapter Class ---
    private class LyricsAdapter : RecyclerView.Adapter
    {
        private List<LrcLibLyrics> items;
        private Action<LrcLibLyrics> onClick;

        public LyricsAdapter(List<LrcLibLyrics> items, Action<LrcLibLyrics> onClick)
        {
            this.items = items;
            this.onClick = onClick;
        }

        public override int ItemCount => items.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = holder as LyricsViewHolder;
            var item = items[position];
            vh.Title.Text = item.TrackName;
            vh.Subtitle.Text = $"{item.ArtistName} - {item.AlbumName}";

            vh.SyncedBadge.Visibility = string.IsNullOrEmpty(item.SyncedLyrics) ? ViewStates.Gone : ViewStates.Visible;
            vh.PlainBadge.Visibility = string.IsNullOrEmpty(item.PlainLyrics) ? ViewStates.Gone : ViewStates.Visible;

            vh.ItemView.Click -= vh.ClickHandler; // Remove old handler
            vh.ClickHandler = (s, e) => onClick(item);
            vh.ItemView.Click += vh.ClickHandler;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var card = new MaterialCardView(parent.Context) { Radius = 10, CardElevation = 4, UseCompatPadding = true };
            var layout = new LinearLayout(parent.Context) { Orientation = Orientation.Vertical };
            layout.SetPadding(20, 20, 20, 20);

            var title = new TextView(parent.Context) { TextSize = 18, Typeface = Android.Graphics.Typeface.DefaultBold };
            var sub = new TextView(parent.Context) { TextSize = 14 };
            var badges = new LinearLayout(parent.Context) { Orientation = Orientation.Horizontal };

            var syncChip = new Google.Android.Material.Chip.Chip(parent.Context) { Text = "Synced"};
            var plainChip = new Google.Android.Material.Chip.Chip(parent.Context) { Text = "Plain"};
            syncChip.SetMinHeight(0);
            plainChip.SetMinHeight(0);
            badges.AddView(syncChip);
            badges.AddView(plainChip);

            layout.AddView(title);
            layout.AddView(sub);
            layout.AddView(badges);
            card.AddView(layout);

            return new LyricsViewHolder(card, title, sub, syncChip, plainChip);
        }

        class LyricsViewHolder : RecyclerView.ViewHolder
        {
            public TextView Title, Subtitle;
            public View SyncedBadge, PlainBadge;
            public EventHandler ClickHandler;
            public LyricsViewHolder(View v, TextView t, TextView s, View sb, View pb) : base(v)
            { Title = t; Subtitle = s; SyncedBadge = sb; PlainBadge = pb; }
        }
    }
}