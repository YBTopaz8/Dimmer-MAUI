using AndroidX.Lifecycle.ViewModels;

using Bumptech.Glide;

using Dimmer.Data.Models.LyricsModels;
using Dimmer.WinUI.UiUtils;

using Google.Android.Material.ProgressIndicator;

using ScrollView = Android.Widget.ScrollView;

namespace Dimmer.ViewsAndPages.NativeViews.SingleSong;

public class LyricsEditorFragment : Fragment
{
    private TextInputLayout _searchTitle, _searchArtist, _searchAlbum;
    private RecyclerView _resultsRecycler;
    BaseViewModelAnd _viewModel;
    SongModelView selectedSong;
    CircularProgressIndicator progressIndicator;

    public LyricsEditorFragment(BaseViewModelAnd viewModel, SongModelView selectedSong)
    {
        _viewModel = viewModel;
        this.selectedSong = selectedSong;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var context = Context;
        var scrollView = new ScrollView(context)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent),
            FillViewport = true
        };
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

        var headerTitle = new TextView(context) { Text = "Fetch Lyrics", TextSize = 24 };
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
        Glide.With(this)
            .Load(selectedSong.CoverImagePath)
            .Placeholder(Resource.Drawable.musicaba)
            .Into(cover);

        var infoTextLayout = new LinearLayout(context) { Orientation = Orientation.Vertical };
        infoTextLayout.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent) { LeftMargin = 24 };
        infoTextLayout.AddView(new TextView(context) { Text = selectedSong.Title, TextSize = 18, Typeface = Android.Graphics.Typeface.DefaultBold });
        infoTextLayout.AddView(new TextView(context) { Text = selectedSong.OtherArtistsName , Ellipsize = Android.Text.TextUtils.TruncateAt.Marquee });

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

         progressIndicator = new CircularProgressIndicator(context);
        progressIndicator.Indeterminate = true;
        progressIndicator.Visibility = ViewStates.Gone;
        progressIndicator.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent) { Gravity = GravityFlags.CenterHorizontal };
        root.AddView(progressIndicator);



        _resultsRecycler = new RecyclerView(context);
        _resultsRecycler.SetLayoutManager(new LinearLayoutManager(context));
        _resultsRecycler.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, 1f); // Weight 1 to fill space

        
        _resultsRecycler.SetAdapter(new LyricsResultAdapter(context, _viewModel, progressIndicator));

        root.AddView(_resultsRecycler);

        scrollView.AddView(root);
        return scrollView;
    }

    private LinearLayout CreateSearchForm(Context context)
    {
        LinearLayout? form = new LinearLayout(context) { Orientation = Orientation.Vertical };

        _searchTitle = UiBuilder.CreateInput(context, "Title", "");
        _searchArtist = UiBuilder.CreateInput(context, "Artist", "");
        _searchAlbum = UiBuilder.CreateInput(context, "Album", "");

        var btnRow = new LinearLayout(context) { Orientation = Orientation.Horizontal };
        var searchBtn = UiBuilder.CreateMaterialButton(context,this.Resources.Configuration, clickAction: (s, e) => 
        { 
            _viewModel.SearchLyricsCommand.ExecuteAsync(null);
            progressIndicator.Visibility = ViewStates.Visible;
        }, iconRes: Resource.Drawable.searchd);
        var pasteBtn = UiBuilder.CreateMaterialButton(context, this.Resources.Configuration, clickAction: (s, e) => 
        {
            _viewModel.AutoFillSearchFields();

        }, true,iconRes:Resource.Drawable.clipboard);



        _viewModel.AutoFillSearchFields();
        _searchTitle.EditText?.Text = _viewModel.LyricsTrackNameSearch;

        _searchArtist.EditText?.Text = _viewModel.LyricsArtistNameSearch;

        _searchAlbum.EditText?.Text = _viewModel.LyricsAlbumNameSearch;

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
        private Button viewBtnResult;
        private Button applyBtnResult;
        private readonly ObservableCollection<LrcLibLyrics> _items;
        readonly BaseViewModelAnd viewModel;
        readonly CircularProgressIndicator progressIndicator;
        public LyricsResultAdapter(Context ctx, BaseViewModelAnd baseVM, CircularProgressIndicator progressIndicator) 
        {
            _ctx = ctx;
             this.progressIndicator = progressIndicator;

            viewModel = baseVM;
            _items = baseVM.LyricsSearchResults;


            _items.CollectionChanged += (s, e) =>
            {
                if (!baseVM.IsLyricsSearchBusy)
                {
                    this.progressIndicator.Visibility = ViewStates.Gone;                    
                }


                NotifyDataSetChanged();
            }; 
        }
        public override int ItemCount => _items.Count;

        public TextView titleResult { get; private set; }
        public TextView syncLyricsResult { get; private set; }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = holder as LyricsResultViewHolder;
            if (vh != null)
            {
                var lyrObj = _items[position];
                vh.Bind(lyrObj);
               
            }
        }

      

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var card = new MaterialCardView(parent.Context) { Elevation = 2, Radius = 12 };
            var lp = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            lp.SetMargins(16, 8, 16, 8);
            card.LayoutParameters = lp;

            var layout = new LinearLayout(parent.Context) { Orientation = Orientation.Vertical };
            layout.SetPadding(24, 24, 24, 24);

            titleResult = new TextView(parent.Context!) { TextSize = 16, Typeface = Android.Graphics.Typeface.DefaultBold };
            syncLyricsResult = new TextView(parent.Context!) { TextSize = 12 };

            var btnPanel = new LinearLayout(parent.Context) { Orientation = Orientation.Horizontal};
            btnPanel.SetGravity(GravityFlags.Right);
            
            viewBtnResult = UiBuilder.CreateMaterialButton(_ctx, parent.Resources?.Configuration
                 , null, sizeDp:30, iconRes:Resource.Drawable.eye);

            applyBtnResult = UiBuilder.CreateMaterialButton(parent.Context!, parent.Resources?.Configuration
                ,null, sizeDp:30, iconRes:Resource.Drawable.savea);



            btnPanel.AddView(viewBtnResult);
            btnPanel.AddView(applyBtnResult);

            layout.AddView(titleResult);
            layout.AddView(syncLyricsResult);
            layout.AddView(btnPanel);
            card.AddView(layout);

            return new LyricsResultViewHolder(viewModel, card, titleResult, syncLyricsResult, applyBtnResult);
        }

        class LyricsResultViewHolder : RecyclerView.ViewHolder
        {
            private readonly BaseViewModelAnd _vm;
            public TextView Title;
            public TextView Duration, IsSyncLyrics;
            public TextView ArtistName, SyncedLyrics;
            public TextView TrackName, PlainLyrics;
            public TextView AlbumName, Instrumental;
            public Button ApplyBtn; 
            public LyricsResultViewHolder(BaseViewModelAnd viewModel, View ContainerView, TextView t, TextView s, Button b) : base(ContainerView) 
            {
                _vm = viewModel;
                Title = t; SyncedLyrics = s; ApplyBtn = b;
                ApplyBtn.Click += (sender, args) =>
                {
                    
                    int pos = BindingAdapterPosition;

                    var selectedLyric = _vm.LyricsSearchResults.ElementAt(pos);
                    _vm.SelectLyricsCommand.ExecuteAsync(selectedLyric);

                    Toast.MakeText(ContainerView.Context, $"Applied at {pos}", ToastLength.Short)?.Show();
                };
            }

            public void Bind(LrcLibLyrics lyrObj)
            {
                SyncedLyrics.Text = lyrObj.SyncedLyrics;
               //IsSyncLyrics.Text  = (!(lyrObj.SyncedLyrics?.Length <1)).ToString();
                //ArtistName.Text = lyrObj.ArtistName;
                //AlbumName.Text = lyrObj.AlbumName;
                //Duration.Text = lyrObj.Duration.ToString();
                //Instrumental.Text = lyrObj.Instrumental.ToString();
                Title.Text = lyrObj.TrackName;
                //PlainLyrics.Text = lyrObj.PlainLyrics;
            }

        }
    }
}