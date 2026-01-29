using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using Android.Nfc;
using Android.Text;
using Bumptech.Glide;
using Dimmer.ViewsAndPages.NativeViews.AlbumSection;
using Dimmer.ViewsAndPages.NativeViews.Misc;
using DynamicData;
using Google.Android.Material.Chip;
using Google.Android.Material.Loadingindicator;
using Google.Android.Material.ProgressIndicator;
using ScrollView = Android.Widget.ScrollView;

namespace Dimmer.ViewsAndPages.NativeViews.ArtistSection;

public partial class ArtistFragment : Fragment, IOnBackInvokedCallback
{

    private BaseViewModelAnd MyViewModel;
    private string _artistName;
    private string _artistId;
    private RecyclerView albumsRecycler;
    private ScrollView myScrollView;
    private LinearLayout root;
    private TextView songsLabel;
    private LoadingIndicator progressIndic;

    public ArtistFragment()
    {
        
    }
    public ArtistFragment(BaseViewModelAnd vm, string artistName, string artistId)
    {
        MyViewModel = vm;
        _artistName = artistName;
        _artistId = artistId;
    }

    private ChipGroup _albumChipGroup;
    private TextView nameTxt;
    private TextView albumLabel;

    public void OnBackInvoked()
    {
        TransitionActivity myAct = (Activity as TransitionActivity)!;
        myAct?.HandleBackPressInternal();
        //myAct.MoveTaskToBack
    }
    public ArtistModelView? SelectedArtist { get; private set; }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context;
        SelectedArtist = MyViewModel.SelectedArtist;
        var artAlbums = SelectedArtist.AlbumsInDB(MyViewModel.RealmFactory);
        SelectedArtist.SongsByArtist = SelectedArtist.SongsInDB(MyViewModel.RealmFactory).ToObservableCollection();

        myScrollView = new ScrollView(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent),
            FillViewport = true
        };

        root = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };

        // --- 1. HEADER SECTION ---
        var headerLayout = new FrameLayout(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, AppUtil.DpToPx(150))
        };

        var artistImage = new ImageView(ctx)
        {
            LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent),
        };
        artistImage.SetScaleType(ImageView.ScaleType.CenterCrop);
        // Mock loading artist image (replace with actual logic)
        Glide.With(ctx).Load(Resource.Drawable.media3_icon_artist).Into(artistImage);

        // Gradient Overlay for text readability
        var overlay = new View(ctx) { LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent) };


        nameTxt = new MaterialTextView(ctx)
        {
            Text = _artistName,
            TextSize = 32,
            Typeface = Android.Graphics.Typeface.DefaultBold,
            LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
            { Gravity = GravityFlags.Bottom | GravityFlags.Left }
            ,
            TransitionName = _artistId
        };
        nameTxt.SetPadding(40, 0, 0, 40);
        nameTxt.SetTextColor(Android.Graphics.Color.White);

        headerLayout.AddView(artistImage);
        headerLayout.AddView(overlay);
        headerLayout.AddView(nameTxt);
        root.AddView(headerLayout);

        // --- 2. ALBUMS (Horizontal List) ---
        albumLabel = new MaterialTextView(ctx) { Text = $"{SelectedArtist!.AlbumsByArtist?.Count} Albums", TextSize = 20 };
        albumLabel.SetPadding(30, 30, 30, 10);
        root.AddView(albumLabel);



        _albumChipGroup = new ChipGroup(context: ctx);
       
        root.AddView(_albumChipGroup);


        // --- 4. STATS SECTION ---
        var statsCard = new Google.Android.Material.Card.MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(16),
            CardElevation = AppUtil.DpToPx(6),
            UseCompatPadding = true,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        var statsLayout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        statsLayout.SetPadding(40, 40, 40, 40);

        statsLayout.AddView(new TextView(ctx) { Text = "Artist Stats", TextSize = 18, Typeface = Android.Graphics.Typeface.DefaultBold });

        if(SelectedArtist.SongsByArtist is not null)
        {

            statsLayout.AddView(CreateStatRow(ctx, "Total Plays", SelectedArtist.SongsByArtist.Sum(x => x.PlayCompletedCount).ToString()));
            statsLayout.AddView(CreateStatRow(ctx, "Total Skips", SelectedArtist.SongsByArtist.Sum(x => x.SkipCount).ToString()));
        }
        statsLayout.AddView(CreateStatRow(ctx, "Library Tracks", SelectedArtist.SongsByArtist.Count.ToString()));

        statsCard.AddView(statsLayout);
        root.AddView(statsCard);




    progressIndic = new LoadingIndicator(ctx);
        progressIndic.IndicatorSize = AppUtil.DpToPx(40);
        progressIndic.SetForegroundGravity(GravityFlags.CenterHorizontal);
        root.AddView(progressIndic);

        myScrollView.AddView(root);


        return myScrollView;
    }

    private void SwapAdapterInRCView(RecyclerView.Adapter newAdapter)
    {
        var currAdapter = albumsRecycler.GetAdapter();
        if (currAdapter is not null)
        {
            albumsRecycler.SwapAdapter(newAdapter, true);
        }
        else
        {

            albumsRecycler.SetAdapter(newAdapter);
        }
    }

    public async override void OnResume()
    {
        base.OnResume();
        
        _albumChipGroup.RemoveAllViews();
        _albumChipGroup.CanScrollHorizontally(1);
        _albumChipGroup.SetChipSpacing(5);
        
        var albuInArtist = SelectedArtist.AlbumsByArtist;
        albuInArtist ??= SelectedArtist.AlbumsInDB(MyViewModel.RealmFactory).ToObservableCollection();
            if (albuInArtist is not null)
            {
                foreach (var album in albuInArtist)
                {
                    var chip = new Chip(Context) {
                        Typeface = Typeface.DefaultBold,
                        

                        HorizontalFadingEdgeEnabled = true,
                        
                        Text = album.Name };

                chip.TooltipText = album.Name;
                    chip.Click += (s, e) =>
                    {

                        MyViewModel.NavigateToAnyPageOfGivenType(this, new AlbumFragment(MyViewModel),
                            album.Id.ToString());
                        
                    };
              

                    _albumChipGroup.AddView(chip);
                }
            }

        

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
        {
            Activity?.OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(
                (int)IOnBackInvokedDispatcher.PriorityDefault, this);
        }
        MyViewModel.CurrentFragment = this;
        var ctx = Context;
        await Task.Delay(1000);
        if(ctx is not null)
        {

            // --- 3. SONGS (Vertical List) ---
            songsLabel = new MaterialTextView(ctx) { Text = $"{SelectedArtist?.SongsByArtist?.Count} Songs", TextSize = 20 };
            songsLabel.SetPadding(30, 30, 30, 10);
            root.AddView(songsLabel);

            var songsRecycler = new RecyclerView(ctx)
            {
                LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, AppUtil.DpToPx(400))
            };

            songsRecycler.NestedScrollingEnabled = true; // Important inside ScrollView
            songsRecycler.SetLayoutManager(new LinearLayoutManager(ctx));

            songsRecycler.SetAdapter(new SongAdapter(ctx, MyViewModel, this, "artist"));
            root.AddView(songsRecycler);
            progressIndic.Visibility = ViewStates.Gone;

        }
    }
    private LinearLayout CreateStatRow(Context ctx, string label, string value)
    {
        var row = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        row.SetPadding(0, 10, 0, 10);
        var t1 = new TextView(ctx) { Text = label + ": ", Typeface = Android.Graphics.Typeface.DefaultBold };
        var t2 = new TextView(ctx) { Text = value };
        row.AddView(t1);
        row.AddView(t2);
        return row;
    }

    // --- Simple Adapters for this View ---
    class AlbumsFromArtistAdapter : RecyclerView.Adapter
    {
        private ReadOnlyObservableCollection<AlbumModelView> _albums;

        public BaseViewModelAnd MyViewModel { get; }

        public AlbumsFromArtistAdapter(ArtistModelView art, BaseViewModelAnd vm) 
        {
            //_albums = a;
        
            
            this.MyViewModel = vm;

            IObservable<IChangeSet<AlbumModelView>> sourceStream;


            var selArt = art;
                var realm = MyViewModel.RealmFactory.GetRealmInstance();


                var artistEntry = realm.Find<ArtistModel>(selArt.Id);

            if (artistEntry != null)
            {
                sourceStream = artistEntry.Albums.AsObservableChangeSet()
                    .Transform(model => model.ToAlbumModelView())!; // Transforms DB Model -> View Model
            }
            else
            {
                // Handle edge case where artist isn't found
                sourceStream = Observable.Return(ChangeSet<AlbumModelView>.Empty);
            }
            // 3. The "DynamicData" Pipeline
            sourceStream
                .ObserveOn(scheduler: RxSchedulers.UI) // Must be on UI thread to update RecyclerView
                .Bind(out _albums)           // Automatically keeps _songs in sync with the source
                .Subscribe(changes =>
                {
                    // 4. Manual Dispatching (The Secret Sauce for Animations)
                    // Instead of NotifyDataSetChanged, we loop through the changes 
                    // DynamicData detected and tell Android exactly what happened.
                    foreach (var change in changes)
                    {
                        switch (change.Reason)
                        {
                            case ListChangeReason.AddRange:
                                NotifyItemRangeInserted(change.Range.Index, change.Range.Count);
                                break;
                            case ListChangeReason.RemoveRange:
                                NotifyItemRangeRemoved(change.Range.Index, change.Range.Count);
                                break;
                            case ListChangeReason.Refresh:

                                NotifyItemChanged(change.Item.CurrentIndex);

                                break;
                            case ListChangeReason.Add:
                                NotifyItemInserted(change.Item.CurrentIndex);
                                break;
                            case ListChangeReason.Remove:
                                NotifyItemRemoved(change.Item.CurrentIndex);
                                break;
                            case ListChangeReason.Moved:
                                NotifyItemMoved(change.Item.PreviousIndex, change.Item.CurrentIndex);
                                break;
                            case ListChangeReason.Replace:
                                NotifyItemChanged(change.Item.CurrentIndex);
                                break;
                            case ListChangeReason.Clear:
                                NotifyDataSetChanged();
                                break;
                        }
                    }


                })
                .DisposeWith(_disposables);

        }
        private readonly CompositeDisposable _disposables = new();
        public override int ItemCount => _albums.Count;
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            MaterialCardView? card = new Google.Android.Material.Card.MaterialCardView(parent.Context)
            {
                Radius = 20,
                LayoutParameters = new ViewGroup.LayoutParams(400, 400)
            };

            var t = card.LayoutParameters;
            if(t is (ViewGroup.MarginLayoutParams))
            {
                var s = (ViewGroup.MarginLayoutParams)t;
                s.SetMargins(10, 10, 10, 10);
            }
            var txt = new TextView(parent.Context) { Gravity = GravityFlags.Center, TextSize = 12 };
            card.AddView(txt);

            card.SetPadding(10, 10, 10, 10);

            return new AlbumAdapterViewHolder(card, txt);
        }
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = holder as AlbumAdapterViewHolder;
            
            vh.Text.Text = _albums[position].Name;
            
        }
        class AlbumAdapterViewHolder : RecyclerView.ViewHolder 
        { 
            public TextView Text; 
            public AlbumAdapterViewHolder(MaterialCardView v, TextView t) : base(v) 
            { 
                Text = t; 
            } 
        }
    }

    class SimpleSongListAdapter : RecyclerView.Adapter
    {
        List<SongModelView> _songs;
        BaseViewModelAnd _vm;
        public SimpleSongListAdapter(List<SongModelView> s, BaseViewModelAnd vm) { _songs = s; _vm = vm; }
        public override int ItemCount => _songs.Count;
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var tv = new TextView(parent.Context) { TextSize = 16 };
            tv.SetPadding(30, 20, 30, 20);
            return new VH(tv);
        }
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            (holder as VH)?.Text.Text = $"{position + 1}. {_songs[position].Title}";
            holder.ItemView.Click += async (s, e) => await _vm.PlaySongAsync(_songs[position], CurrentPage.AllSongs, _songs);
        }
        class VH : RecyclerView.ViewHolder { public TextView Text; public VH(View v) : base(v) { Text = (TextView)v; } }
    }
}