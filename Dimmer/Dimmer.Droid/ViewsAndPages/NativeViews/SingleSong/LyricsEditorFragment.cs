using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dimmer.WinUI.UiUtils;

namespace Dimmer.ViewsAndPages.NativeViews.SingleSong;

public class LyricsEditorFragment : Fragment
{
    private TextInputLayout _searchTitle, _searchArtist, _searchAlbum;
    private RecyclerView _resultsRecycler;
    private TextView _plainLyricsTxt, _syncLyricsTxt;

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var context = Context;

        // Root
        var root = new LinearLayout(context) { Orientation = Orientation.Vertical };
        root.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);

        // --- HEADER ---
        var header = new LinearLayout(context) { Orientation = Orientation.Horizontal };
        header.SetPadding(24, 24, 24, 24);
        header.SetGravity(GravityFlags.CenterVertical);

        var backBtn = new MaterialButton(context, null, Resource.Style.Widget_Material3_Button_IconButton);
        backBtn.SetIconResource(Resource.Drawable.ic_arrow_back_black_24);
        backBtn.Click += (s, e) => ParentFragmentManager.PopBackStack();

        var headerTitle = new TextView(context) { Text = "Edit Lyrics", TextSize = 24 };
        headerTitle.SetTypeface(null, Android.Graphics.TypefaceStyle.Bold);
        headerTitle.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent) { LeftMargin = 24 };

        header.AddView(backBtn);
        header.AddView(headerTitle);
        root.AddView(header);


        // --- SONG INFO (Small Summary) ---
        var infoCard = new MaterialCardView(context);
        var infoLayout = new LinearLayout(context) { Orientation = Orientation.Horizontal };
        infoLayout.SetPadding(16, 16, 16, 16);

        var cover = new ImageView(context); // Set your image
        cover.LayoutParameters = new LinearLayout.LayoutParams(150, 150);
        cover.SetBackgroundColor(Android.Graphics.Color.DarkGray);

        var infoTextLayout = new LinearLayout(context) { Orientation = Orientation.Vertical };
        infoTextLayout.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent) { LeftMargin = 24 };
        infoTextLayout.AddView(new TextView(context) { Text = "Song Title", TextSize = 18, Typeface = Android.Graphics.Typeface.DefaultBold });
        infoTextLayout.AddView(new TextView(context) { Text = "Artist Name" });

        infoLayout.AddView(cover);
        infoLayout.AddView(infoTextLayout);
        infoCard.AddView(infoLayout);

        // Add card with margins
        var cardParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
        cardParams.SetMargins(24, 0, 24, 24);
        infoCard.LayoutParameters = cardParams;
        root.AddView(infoCard);


        // --- SEARCH SECTION (Expandable ideally, but static here for simplicity) ---
        var searchCard = UiBuilder.CreateSectionCard(context, "Find Lyrics", CreateSearchForm(context));
        root.AddView(searchCard);


        // --- RESULTS AREA ---
        var resultsLabel = new TextView(context) { Text = "Search Results", TextSize = 18 };
        resultsLabel.SetPadding(32, 16, 32, 0);
        root.AddView(resultsLabel);

        _resultsRecycler = new RecyclerView(context);
        _resultsRecycler.SetLayoutManager(new LinearLayoutManager(context));
        _resultsRecycler.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, 1f); // Weight 1 to fill space

        // Set Adapter (Mocking the LrcLibLyrics results)
        _resultsRecycler.SetAdapter(new LyricsResultAdapter(context));

        root.AddView(_resultsRecycler);

        return root;
    }

    private View CreateSearchForm(Context context)
    {
        var form = new LinearLayout(context) { Orientation = Orientation.Vertical };

        _searchTitle = UiBuilder.CreateInput(context, "Title", "");
        _searchArtist = UiBuilder.CreateInput(context, "Artist", "");
        _searchAlbum = UiBuilder.CreateInput(context, "Album", "");

        var btnRow = new LinearLayout(context) { Orientation = Orientation.Horizontal };
        var searchBtn = UiBuilder.CreateButton(context, "Search", (s, e) => { /* ViewModel Search Command */ });
        var pasteBtn = UiBuilder.CreateButton(context, "Paste", (s, e) => { /* ViewModel Paste Command */ }, true);

        btnRow.AddView(searchBtn);
        btnRow.AddView(pasteBtn);

        form.AddView(_searchTitle);
        form.AddView(_searchArtist);
        form.AddView(_searchAlbum);
        form.AddView(btnRow);
        return form;
    }

    // Adapter for Lyrics Results
    class LyricsResultAdapter : RecyclerView.Adapter
    {
        Context _ctx;
        public LyricsResultAdapter(Context ctx) { _ctx = ctx; }
        public override int ItemCount => 3; // Mock count

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = holder as ResultVH;
            vh.Title.Text = "Mock Lyric Result " + position;
            vh.Subtitle.Text = "Artist • Album • 3:45";
            vh.ApplyBtn.Click += (s, e) => Toast.MakeText(_ctx, "Applied!", ToastLength.Short).Show();
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var card = new MaterialCardView(parent.Context) { Elevation = 2, Radius = 12 };
            var lp = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            lp.SetMargins(16, 8, 16, 8);
            card.LayoutParameters = lp;

            var layout = new LinearLayout(parent.Context) { Orientation = Orientation.Vertical };
            layout.SetPadding(24, 24, 24, 24);

            var title = new TextView(parent.Context) { TextSize = 16, Typeface = Android.Graphics.Typeface.DefaultBold };
            var sub = new TextView(parent.Context) { TextSize = 12 };

            var btnPanel = new LinearLayout(parent.Context) { Orientation = Orientation.Horizontal};
            btnPanel.SetGravity(GravityFlags.Right);
            var viewBtn = new Button(parent.Context) { Text = "View" };
            var applyBtn = new Button(parent.Context) { Text = "Apply" };

            btnPanel.AddView(viewBtn);
            btnPanel.AddView(applyBtn);

            layout.AddView(title);
            layout.AddView(sub);
            layout.AddView(btnPanel);
            card.AddView(layout);

            return new ResultVH(card, title, sub, applyBtn);
        }

        class ResultVH : RecyclerView.ViewHolder
        {
            public TextView Title, Subtitle;
            public Button ApplyBtn;
            public ResultVH(View v, TextView t, TextView s, Button b) : base(v) { Title = t; Subtitle = s; ApplyBtn = b; }
        }
    }
}