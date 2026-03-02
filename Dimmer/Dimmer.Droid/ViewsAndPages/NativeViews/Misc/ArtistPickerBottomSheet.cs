using Dimmer.DimmerSearch;

namespace Dimmer.ViewsAndPages.NativeViews.Misc;

public class ArtistPickerBottomSheet : BottomSheetDialogFragment
{
    public ArtistPickerBottomSheet(BaseViewModelAnd vm, List<ArtistModelView> listOfArtistmodelView)

    {
        MyViewModel = vm;
        ListOfArtistsInSong = listOfArtistmodelView;
    }

    private readonly SongModelView _song;
    private FrameLayout _rootScrim;
    public BaseViewModelAnd MyViewModel { get; }
    public List<ArtistModelView> ListOfArtistsInSong { get; }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context!;

        // 1. The Background Scrim (Full Screen)
        _rootScrim = new FrameLayout(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(-1, -1)
        };

        // API 31+ Real Blur, Pre-31 Dark Scrim
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.S)
        {
            _rootScrim.SetBackgroundColor(Color.ParseColor("#66000000"));


        }
        else
        {
            _rootScrim.SetBackgroundColor(Color.ParseColor("#AA000000")); // Just dark
        }

        // Fade in the scrim manually
        _rootScrim.Alpha = 0f;
        _rootScrim.Animate()?.Alpha(1f).SetDuration(300).Start();

        // Click background to dismiss (reverse animation)
        _rootScrim.Click += (s, e) =>
        {
            Dismiss();
        };

        // 2. The Centered Card (The popup content)
        var card = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(20),
            CardElevation = AppUtil.DpToPx(12),

        };
        card.SetCardBackgroundColor(Color.ParseColor("#1E1E1E")); // Dark theme parity
        var cardLp = new FrameLayout.LayoutParams(AppUtil.DpToPx(350), AppUtil.DpToPx(450))
        {
            Gravity = GravityFlags.Center
        };
        card.LayoutParameters = cardLp;

        // Prevent clicks on the card from dismissing the fragment
        card.Click += (s, e) =>
        {
            /* Consume click */
        };

        var cardContent = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new ViewGroup.LayoutParams(-1, -2)
        };
        cardContent.SetPadding(AppUtil.DpToPx(24), AppUtil.DpToPx(24), AppUtil.DpToPx(24), AppUtil.DpToPx(24));

     


        ChipGroup statisChipGroup = new ChipGroup(ctx);
        statisChipGroup.Clickable = false;

        foreach (var artist in ListOfArtistsInSong)
        {
            var artistChip = new Chip(ctx);
            artistChip.SetChipIconResource(Resource.Drawable.media3_icon_artist);
            artistChip.Clickable = true;
            artistChip.Text = artist.Name;
            artistChip.Click += (s, e)
            =>
            {


            };

            statisChipGroup.AddView(artistChip);
        }
        



        cardContent.AddView(statisChipGroup);


        card.AddView(cardContent);

        _rootScrim.AddView(card);


        return _rootScrim;
    }


    // Simple Adapter for the list
    class ArtistBottomSheetRecyclerViewAdapter : RecyclerView.Adapter
    {
        private List<ArtistModelView> _artistNames;

        public BaseViewModelAnd MyViewModel { get; }
        public ArtistPickerBottomSheet ArtistPickerBottomSheet { get; }

        public ArtistBottomSheetRecyclerViewAdapter(List<ArtistModelView> items
            ,BaseViewModelAnd vm,
ArtistPickerBottomSheet artistPickerBottomSheet) 
        { 
            _artistNames = items;
            MyViewModel = vm;
            ArtistPickerBottomSheet = artistPickerBottomSheet;
        }

        public override int ItemCount => _artistNames.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (holder is ArtistPickerVHolder songHolder)
            {
                var artist = _artistNames[position];

                songHolder.BindData(ArtistPickerBottomSheet, artist);

            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            MaterialCardView? card = UiBuilder.CreateCard(parent.Context);

            var hLL = new LinearLayout (parent.Context);
            
            hLL.Orientation = Android.Widget.Orientation.Horizontal;

            var artistBtn = new MaterialButton(parent.Context);

            artistBtn.SetPadding(24, 24, 24, 24);

            var TQLChip = new Chip(parent.Context);
            TQLChip.SetChipIconResource(Resource.Drawable.searchd);

            var FavArtistChip = new Chip(parent.Context);
            FavArtistChip.SetChipIconResource(Resource.Drawable.heart);
            

            var ViewChip = new Chip(parent.Context);
            ViewChip.SetChipIconResource(Resource.Drawable.eye);

            var lyParams = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent) { Gravity = GravityFlags.Right };
            lyParams.SetMargins(10, 10, 10, 10);


            hLL.AddView(artistBtn);
            //hLL.AddView(TQLChip, lyParams);
            //hLL.AddView(FavArtistChip, lyParams);
            //hLL.AddView(ViewChip, lyParams);
            card.AddView(hLL, lyParams);

            return new ArtistPickerVHolder(card,artistBtn,TQLChip,FavArtistChip,ViewChip, MyViewModel);
        }

        class ArtistPickerVHolder : RecyclerView.ViewHolder
        {
            public MaterialCardView ContainerView ;
            public MaterialButton ArtistBtnView;

            BaseViewModelAnd MyViewModel { get; }

            public ArtistPickerVHolder(MaterialCardView container, MaterialButton artistBtn, Chip tQLChip, Chip favArtistChip, Chip viewChip, BaseViewModelAnd myViewModel) : base(container)
            {
                MyViewModel = myViewModel;

                ContainerView = container;
                ArtistBtnView = artistBtn;




                ArtistBtnView.Click += (s, e) =>
                {

                    MyViewModel.SearchToTQL(TQlStaticMethods.PresetQueries.ByArtist(SelectedArtist!.Name!)
                        );
                    parent?.Dismiss();
                    UiBuilder.ShowSnackBar(parent!.View!,$"Selected {SelectedArtist}");
                };
            }
            ArtistPickerBottomSheet? parent;
            ArtistModelView SelectedArtist;


            public void BindData ( ArtistPickerBottomSheet artistPickerBottomSheet, ArtistModelView artist)
            {
                parent = artistPickerBottomSheet;
                SelectedArtist=artist;
                ArtistBtnView.Text = artist.Name;
            }
        }
    }
}