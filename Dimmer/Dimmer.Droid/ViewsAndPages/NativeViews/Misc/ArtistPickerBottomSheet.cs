using Android.Content;
using AndroidX.Lifecycle;
using Dimmer.DimmerSearch;
using Dimmer.UiUtils;
using Google.Android.Material.Chip;

namespace Dimmer.ViewsAndPages.NativeViews.Misc;

public class ArtistPickerBottomSheet : BottomSheetDialogFragment
{
    public ArtistPickerBottomSheet(BaseViewModelAnd vm, string listOfArtistString)

    {
        MyViewModel = vm;
        ListOfArtistsInSong = listOfArtistString.Split(", ").ToList();
    }

    public BaseViewModelAnd MyViewModel { get; }
    public List<string> ListOfArtistsInSong { get; }
    public TextInputLayout searchInput { get; private set; }

    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var context = Context;
        if (context == null) return null;
        var layout = new LinearLayout(context) { Orientation = Orientation.Vertical };
        layout.SetPadding(32, 32, 32, 32);

        // Title
        var title = new TextView(context) { Text = $"Select Artist", TextSize = 22 };
        title.SetPadding(8, 8, 8, 8);
        title.SetTypeface(null, Android.Graphics.TypefaceStyle.Bold);
        layout.AddView(title);

        // Search Bar
        searchInput = UiBuilder.CreateInput(context, string.Empty, "");
        layout.AddView(searchInput);

        // RecyclerView for Artists
        var recyclerView = new RecyclerView(context);
        recyclerView.SetLayoutManager(new LinearLayoutManager(context));
        // In a real app, pass your ViewModel's Artist list here
        
        recyclerView.SetAdapter(new ArtistBottomSheetRecyclerViewAdapter(ListOfArtistsInSong, MyViewModel, this));

        layout.AddView(recyclerView, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 800)); // Fixed height or use weight

        return layout;
    }

    // Simple Adapter for the list
    class ArtistBottomSheetRecyclerViewAdapter : RecyclerView.Adapter
    {
        private List<string> _artistNames;

        public BaseViewModelAnd MyViewModel { get; }
        public ArtistPickerBottomSheet ArtistPickerBottomSheet { get; }

        public ArtistBottomSheetRecyclerViewAdapter(List<string> items
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
            
            hLL.AddView(artistBtn);
            hLL.AddView(TQLChip);
            hLL.AddView(FavArtistChip);
            hLL.AddView(ViewChip);
            card.AddView(hLL);

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

                    MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.PresetQueries.ByArtist(ArtistName)
                        );
                    parent?.Dismiss();
                    Toast.MakeText(container.Context, $"Selected {ArtistName}", ToastLength.Short)?.Show();
                };
            }
            ArtistPickerBottomSheet? parent;
            string? ArtistName;


            public void BindData ( ArtistPickerBottomSheet artistPickerBottomSheet, string artist)
            {
                parent = artistPickerBottomSheet;
                ArtistName=artist;
            }
        }
    }
}