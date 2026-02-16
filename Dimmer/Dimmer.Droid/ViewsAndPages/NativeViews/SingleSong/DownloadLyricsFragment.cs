using AndroidX.Lifecycle;
using Dimmer.Data.Models.LyricsModels;
using Dimmer.UiUtils;
using DynamicData.Binding;
using Google.Android.Material.Dialog;
using Google.Android.Material.ProgressIndicator;
using ProgressBar = Android.Widget.ProgressBar;
using ScrollView = Android.Widget.ScrollView;

namespace Dimmer.ViewsAndPages.NativeViews.SingleSong;

public partial class DownloadLyricsFragment : Fragment
{
    private BaseViewModelAnd MyViewModel;
    private TextInputEditText titleInput, artistInput, albumInput;

    public Button searchBtn { get; private set; }
    public bool isSearchClicked { get; private set; }

    private RecyclerView resultsRecycler;
    private ProgressBar loadingBar;

    public DownloadLyricsFragment(BaseViewModelAnd vm)
    {
        MyViewModel = vm;
    }

    public override void OnResume()
    {
        base.OnResume();
       
               MyViewModel.WhenPropertyChange(nameof(MyViewModel.HasLyricsSearchResults), x=>MyViewModel.LyricsSearchResults)
                   .ObserveOn(RxSchedulers.UI)
                   .Subscribe(obsColLyrics =>
                   {
                       if (obsColLyrics is null || obsColLyrics.Count < 1)
                       {
                           loadingBar.Visibility = ViewStates.Gone;
                           Toast.MakeText(this.Context, "No Lyrics found", ToastLength.Short);
                           isSearchClicked = false;
                           searchBtn.Enabled = true;
                           return;
                       }
                       resultsRecycler.SetAdapter(new LyricsAdapter(MyViewModel.LyricsSearchResults, OnLyricsSelected));
                       loadingBar.Visibility = ViewStates.Gone;
                       isSearchClicked = false;
                       searchBtn.Enabled = true;
                   });
       
               MyViewModel.WhenPropertyChange(nameof(MyViewModel.IsLyricsSearchBusy), x=>MyViewModel.IsLyricsSearchBusy)
                   .ObserveOn(RxSchedulers.UI)
                   .Subscribe(isBusy =>
                   {
                       if (isBusy)
                       {
                           loadingBar.Visibility = ViewStates.Visible;
                           searchBtn.Enabled = false;
                           return;
                       }
                       loadingBar.Visibility = ViewStates.Gone;
                       searchBtn.Enabled = true;
                           return;
                       
                   });
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
        titleInput.SetTextColor(UiBuilder.IsDark(ctx) ? Color.White : Color.Black);

        artistInput = new TextInputEditText(ctx) { Hint = "Artist", Text = MyViewModel.SelectedSong?.ArtistName };
        artistInput.SetTextColor(UiBuilder.IsDark(ctx) ? Color.White : Color.Black);

        albumInput = new TextInputEditText(ctx) { Hint = "Album", Text = MyViewModel.SelectedSong?.AlbumName };
        albumInput.SetTextColor(UiBuilder.IsDark(ctx) ? Color.White : Color.Black);

        searchBtn = new MaterialButton(ctx) { Text = "Search Lyrics" };

        searchBtn.Click += SearchBtn_Click;
        searchBtn.SetTextColor(Color.White);
        root.AddView(titleInput);
        root.AddView(artistInput);
        root.AddView(albumInput);
        root.AddView(searchBtn);

        // 2. Loading Indicator
        loadingBar = new CircularProgressIndicator(ctx) { Indeterminate = true, Visibility = ViewStates.Gone };
        root.AddView(loadingBar);

        // 3. Results List
        resultsRecycler = new RecyclerView(ctx);
        resultsRecycler.SetLayoutManager(new LinearLayoutManager(ctx));
        
        resultsRecycler.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
        root.AddView(resultsRecycler);

        return root;
    }


    private async void SearchBtn_Click(object? sender, EventArgs e)
    {
        isSearchClicked = true;
        searchBtn.Enabled = false;
        loadingBar.Visibility = ViewStates.Visible;

        await MyViewModel.SearchLyricsCommand.ExecuteAsync(null);

    }

    private void OnLyricsSelected(LrcLibLyrics lyrics)
    {
        // Show preview dialog before applying
        ShowLyricsPreviewDialog(lyrics);
    }

    private void ShowLyricsPreviewDialog(LrcLibLyrics lyrics)
    {
        if (Context == null) return;

        var dialog = new MaterialAlertDialogBuilder(Context);
        
        // Set title
        dialog.SetTitle($"{lyrics.TrackName} - {lyrics.ArtistName}");

        // Create dialog content
        var scrollView = new ScrollView(Context);
        var layout = new LinearLayout(Context)
        {
            Orientation = Orientation.Vertical
        };
        layout.SetPadding(40, 20, 40, 20);

        // Metadata section
        var metadataLayout = new LinearLayout(Context) { Orientation = Orientation.Vertical };
        metadataLayout.SetPadding(0, 0, 0, 20);

        bool hasSyncedLyrics = !string.IsNullOrWhiteSpace(lyrics.SyncedLyrics);
        bool hasPlainLyrics = !string.IsNullOrWhiteSpace(lyrics.PlainLyrics);

        var typeText = new TextView(Context)
        {
            Text = $"Type: {(hasSyncedLyrics ? "Synced" : "Plain")}",
            TextSize = 12
        };
        typeText.SetTextColor(Android.Graphics.Color.Gray);
        metadataLayout.AddView(typeText);

        var durationText = new TextView(Context)
        {
            Text = $"Duration: {TimeSpan.FromSeconds(lyrics.Duration):mm\\:ss}",
            TextSize = 12
        };
        durationText.SetTextColor(Android.Graphics.Color.Gray);
        metadataLayout.AddView(durationText);

        if (lyrics.Instrumental)
        {
            var instrumentalText = new TextView(Context)
            {
                Text = "⚠ Marked as Instrumental",
                TextSize = 12
            };
            instrumentalText.SetTextColor(Android.Graphics.Color.Orange);
            metadataLayout.AddView(instrumentalText);
        }

        layout.AddView(metadataLayout);

        // Lyrics content - show synced lyrics first if available, otherwise plain
        var lyricsText = new TextView(Context)
        {
            Text = hasSyncedLyrics ? lyrics.SyncedLyrics : (hasPlainLyrics ? lyrics.PlainLyrics : "No lyrics available"),
            TextSize = 14
        };
        lyricsText.SetHeight(AppUtil.DpToPx(150));
        lyricsText.SetTextIsSelectable(true);
        if (hasSyncedLyrics)
        {
            lyricsText.SetTypeface(Android.Graphics.Typeface.Monospace, Android.Graphics.TypefaceStyle.Normal);
        }
        layout.AddView(lyricsText);

        scrollView.AddView(layout);
        dialog.SetView(scrollView);

        // Apply button
        dialog.SetPositiveButton("Apply", async (sender, args) =>
        {
            await ApplyLyrics(lyrics);
        });

        // Edit button
        dialog.SetNeutralButton("Edit", (sender, args) =>
        {
            LoadLyricsForEditing(lyrics);
        });

        // Timestamp button (only for plain lyrics)
        if (hasPlainLyrics && !hasSyncedLyrics)
        {
            dialog.SetNegativeButton("Timestamp", (sender, args) =>
            {
                StartTimestampingSession(lyrics);
            });
        }
        else
        {
            dialog.SetNegativeButton("Close", (sender, args) => { });
        }

        dialog.Show();
    }

    private async System.Threading.Tasks.Task ApplyLyrics(LrcLibLyrics lyrics)
    {
        if (MyViewModel.SelectedSong != null)
        {
            // Use synced lyrics if available, otherwise fall back to plain lyrics
            MyViewModel.SelectedSong.UnSyncLyrics = lyrics.SyncedLyrics ?? lyrics.PlainLyrics ?? string.Empty;
            await MyViewModel.ApplyNewSongEdits(MyViewModel.SelectedSong);
            Toast.MakeText(Context, "Lyrics Applied!", ToastLength.Short)?.Show();
            ParentFragmentManager.PopBackStack();
        }
    }

    private void LoadLyricsForEditing(LrcLibLyrics lyrics)
    {
        // Load lyrics into editor using ViewModel command
        // Check if we have lyrics to edit (either synced or plain)
        bool hasLyricsToEdit = !string.IsNullOrWhiteSpace(lyrics.SyncedLyrics) || !string.IsNullOrWhiteSpace(lyrics.PlainLyrics);
        
        if (hasLyricsToEdit && MyViewModel?.LoadLyricsForEditingCommand != null)
        {
            //MyViewModel.LoadLyricsForEditingCommand.Execute(lyrics);
            Toast.MakeText(Context, "Lyrics loaded for editing", ToastLength.Short)?.Show();
        }
    }

    private void StartTimestampingSession(LrcLibLyrics lyrics)
    {
        // Start timestamping session with plain lyrics (preferred) or synced lyrics as fallback
        string lyricsToTimestamp = !string.IsNullOrWhiteSpace(lyrics.PlainLyrics) 
            ? lyrics.PlainLyrics 
            : lyrics.SyncedLyrics ?? string.Empty;
            
        if (!string.IsNullOrWhiteSpace(lyricsToTimestamp) && MyViewModel?.StartLyricsEditingSessionCommand != null)
        {
            MyViewModel.StartLyricsEditingSessionCommand.Execute(lyricsToTimestamp);
            Toast.MakeText(Context, "Timestamping session started", ToastLength.Short)?.Show();
        }
    }

    // --- Inner Adapter Class ---
    private class LyricsAdapter : RecyclerView.Adapter
    {
        private ObservableCollection<LrcLibLyrics> items;
        private Action<LrcLibLyrics> onClick;

        public LyricsAdapter(ObservableCollection<LrcLibLyrics> items, Action<LrcLibLyrics> onClick)
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