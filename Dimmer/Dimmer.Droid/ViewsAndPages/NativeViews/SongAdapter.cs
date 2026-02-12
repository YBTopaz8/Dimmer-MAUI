using Android.Text;
using AndroidX.Core.View;
using AndroidX.Lifecycle;
using Bumptech.Glide;
using Dimmer.DimmerSearch;
using Dimmer.UiUtils;
using Dimmer.Utils.Extensions;
using Dimmer.ViewsAndPages.NativeViews.Misc;
using Dimmer.ViewsAndPages.ViewUtils;
using DynamicData;
using Google.Android.Material.Chip;
using Google.Android.Material.Dialog;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using MongoDB.Bson; 
using Parse.LiveQuery;
using Realms;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using static Android.App.Assist.AssistStructure;

namespace Dimmer.ViewsAndPages.NativeViews;

internal partial class SongAdapter : RecyclerView.Adapter , IDisposable
{

    private readonly BehaviorSubject<AdapterMode> _mode = new(AdapterMode.Normal);
    public IObservable<AdapterMode> Mode => _mode.AsObservable();
    

    readonly HashSet<int> _selectedPositions = new HashSet<int>();
    public IObservable<Unit> SelectionChanged => _selectionChanged.AsObservable();
    public Subject<Unit> _selectionChanged => new();


    public override int ItemCount => _songs?.Count ?? 0;

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _disposables.Dispose();     
                                   
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }



    private readonly BehaviorSubject<bool> _isSourceCleared = new(true);
    private readonly BehaviorSubject<bool> _isAdapterReady = new(false);
    public IObservable<bool> IsSourceCleared => _isSourceCleared.AsObservable();
    public IObservable<bool> IsAdapterReady => _isAdapterReady.AsObservable();

    private ReadOnlyObservableCollection<SongModelView> _songs;
  
    public static Action<View, string, int>? AdapterCallbacks;
    private Context ctx;
    public BaseViewModelAnd MyViewModel;
    private SongsToWatchSource songSource;
    private readonly IDimmerAudioService _audioService;
    public IList<SongModelView> Songs => _songs;
    private Fragment ParentFragement;

    // Accordion State
    private int _expandedPosition = -1;
    private int _previousExpandedPosition = -1;

    public SongModelView GetItem(int position) => Songs.ElementAt(position);

    IObservable<IChangeSet<SongModelView>> sourceStream;
    public SongAdapter(Context ctx, BaseViewModelAnd vm, Fragment pFragment,
        SongsToWatchSource songsToWatch = SongsToWatchSource.HomePage)
    {
        MyViewModel = vm;
        songSource = songsToWatch;
        _audioService = vm.AudioService;
        ParentFragement = pFragment;
        
        SetupReactivePipeline(vm, songsToWatch);
    }
    private async Task WaitForInitializationAsync()
    {
        // Wait for adapter to be ready
        await _isAdapterReady
            .FirstAsync(isReady => isReady)
            .Timeout(TimeSpan.FromSeconds(5)) // Add timeout for safety
            .Catch((TimeoutException ex) =>
            {
                return Observable.Return(true); // Continue anyway
            });
    }

    public static IObservable<SongAdapter>? CreateAsync(
        Context? ctx,
        BaseViewModelAnd myViewModel,
        Fragment pFramgent,
        SongsToWatchSource songsToWatch = SongsToWatchSource.HomePage)
    {
        if(ctx is null)
        {
            ctx = pFramgent.Context;
        }
        if(ctx is null)
        {
            return null;
        }
        return Observable.Create<SongAdapter>(async observer =>
        {
            var adapter = new SongAdapter(ctx, myViewModel, pFramgent, songsToWatch);


            await adapter.WaitForInitializationAsync();

            observer.OnNext(adapter);
            observer.OnCompleted();

            return Disposable.Create(() => adapter.Dispose());
        })
            .SubscribeOn(RxSchedulers.Background)
            .ObserveOn(RxSchedulers.UI);
    }


    private void SetupReactivePipeline(BaseViewModelAnd viewModel, SongsToWatchSource songsToWatch)
    {
        viewModel.SearchResultsHolder.Connect()
            .ObserveOn(RxSchedulers.UI)
            .Bind(out _songs)
            .Subscribe(changes => NotifyDataSetChanged())
            .DisposeWith(_disposables);



            IObservable<IChangeSet<SongModelView>> sourceStream = songsToWatch switch
            {
                SongsToWatchSource.QueuePage => viewModel.PlaybackQueueSource.Connect(),
                SongsToWatchSource.ArtistPage => GetArtistSongsStream(viewModel),
                SongsToWatchSource.AlbumPage => GetAlbumSongsStream(viewModel),

                SongsToWatchSource.HomePage => viewModel.SearchResultsHolder.Connect(),
                _ => viewModel.SearchResultsHolder.Connect(),
            };

                sourceStream.ObserveOn(RxSchedulers.UI)
                .Do(s => Debug.WriteLine($"Song Count in adapter: {viewModel.SearchResults.Count}"))
                .Bind(out _songs)
                .Subscribe(changes =>
                {
                    if (_isDisposed) return;
                    if (!_isAdapterReady.Value)
                    {
                        _isAdapterReady.OnNext(true);
                    }

                    foreach (var change in changes)
                    {
                        switch (change.Reason)
                        {
                            case ListChangeReason.AddRange:
                                NotifyItemRangeInserted(change.Range.Index, change.Range.Count);
                                _isSourceCleared.OnNext(true);
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
                                _isSourceCleared.OnNext(true);
                                NotifyDataSetChanged();
                                break;
                        }
                    }
                })
                
                .DisposeWith(_disposables);

           

            Observable.FromEventPattern<PlaybackEventArgs>(
                h => _audioService.PlaybackStateChanged += h,
                h => _audioService.PlaybackStateChanged -= h)
                .Select(evt => evt.EventArgs)
                .ObserveOn(RxSchedulers.UI)
                .Subscribe(
                     x => HandlePlaybackStateChange(x))
                .DisposeWith(_disposables);

        

      
    }

    private IObservable<IChangeSet<SongModelView>> GetAlbumSongsStream(BaseViewModelAnd viewModel)
    {
        var selAlb = viewModel.SelectedAlbum;
        selAlb = viewModel.SelectedAlbum = viewModel.SelectedSong!.Album;
        var realm = viewModel.RealmFactory.GetRealmInstance();

        // Realm relationships (like .Songs) return an IList<T> that implements INotifyCollectionChanged.
        // We can bind directly to that.
        var albumInDB = realm.Find<AlbumModel>(selAlb!.Id);

        if (albumInDB != null)
        {
           return sourceStream = albumInDB.SongsInAlbum!.AsObservableChangeSet()!
                .Transform(model => model.ToSongModelView())!; // Transforms DB Model -> View Model
        }
        else
        {
            // Handle edge case where artist isn't found
           return sourceStream = Observable.Return(ChangeSet<SongModelView>.Empty);
        }
    }

    private IObservable<IChangeSet<SongModelView>> GetArtistSongsStream(BaseViewModelAnd viewModel)
    {


        var selArt = viewModel.SelectedArtist;

        var realm = viewModel.RealmFactory.GetRealmInstance();

        // Realm relationships (like .Songs) return an IList<T> that implements INotifyCollectionChanged.
        // We can bind directly to that.
        var artistEntry = realm.Find<ArtistModel>(selArt.Id);

        if (artistEntry != null)
        {
           return sourceStream = artistEntry.Songs.AsObservableChangeSet()

                .Transform(model => model.ToSongModelView())
                .ObserveOn(RxSchedulers.Background)

                .ObserveOn(RxSchedulers.UI)!; // Transforms DB Model -> View Model
        }
        else
        {
            // Handle edge case where artist isn't found
           return sourceStream = Observable.Return(ChangeSet<SongModelView>.Empty);
        }
    }

    private void HandlePlaybackStateChange(PlaybackEventArgs x)
    {
        //throw new NotImplementedException();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (disposing) _disposables.Dispose();
            _disposables.Dispose();
        }
        base.Dispose(disposing);
    }
    private readonly CompositeDisposable _disposables = new();

    private bool _isDisposed;
    public Button moreBtn { get; private set; }
    public Button StatsBtn { get; private set; }
    public Button favBtn { get; private set; }

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        if (holder is SongViewHolder songHolder)
        {
            var song = _songs[position];
            bool isExpanded = position == _expandedPosition;
            
            songHolder.Bind(song, isExpanded, ToggleExpand, songSource);
        }
    }

    public class SimpleItemTouchHelperCallback : ItemTouchHelper.Callback
    {
        private readonly SongAdapter _adapter;
        public SimpleItemTouchHelperCallback(SongAdapter adapter) => _adapter = adapter;

        public override int GetMovementFlags(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
        {
            int dragFlags = ItemTouchHelper.Up | ItemTouchHelper.Down;
            int swipeFlags = ItemTouchHelper.Start | ItemTouchHelper.End; // Enable Swipe to Remove?
            return MakeMovementFlags(dragFlags, swipeFlags);
        }

        public override bool OnMove(RecyclerView recyclerView, RecyclerView.ViewHolder source, RecyclerView.ViewHolder target)
        {
            // Notify Adapter to swap items in the ObservableCollection
            _adapter.OnItemMove(source.BindingAdapterPosition, target.BindingAdapterPosition);
            return true;
        }

        public override void OnSwiped(RecyclerView.ViewHolder viewHolder, int direction)
        {
            _adapter.OnItemDismiss(viewHolder.BindingAdapterPosition);
        }
    }

    public bool OnItemMove(int fromPosition, int toPosition)
    {
        // 1. Move the item in the actual data list
        // Use the ViewModel's MoveSongInQueue to ensure proper queue index tracking
        MyViewModel.MoveSongInQueue(fromPosition, toPosition);

        // 2. Notify the RecyclerView that the item moved visually
        // IMPORTANT: Do NOT call NotifyDataSetChanged(), it breaks animations.
        NotifyItemMoved(fromPosition, toPosition);

        return true;
    }

    public void OnItemDismiss(int position)
    {
        // 1. Remove from data source
        MyViewModel.PlaybackQueueSource.Edit(upd=>
            {
                MyViewModel.PlaybackQueueSource.RemoveAt(position);
            });

        // 2. Notify RecyclerView
        NotifyItemRemoved(position);

        // 3. Optional: Notify Playback service that the queue changed?
        // _viewModel.UpdateQueueService(); 
    }
  

    private void ToggleExpand(int position)
    {
        _previousExpandedPosition = _expandedPosition;

        if (_expandedPosition == position)
        {
            // Collapse current
            _expandedPosition = -1;
        }
        else
        {
            // Expand new
            _expandedPosition = position;
        }

        // Only notify the rows that changed to animate/update efficiently
        if (_previousExpandedPosition != -1) NotifyItemChanged(_previousExpandedPosition);
        if (_expandedPosition != -1) NotifyItemChanged(_expandedPosition);
    }

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        if (ctx is null && parent.Context is not null)
            ctx = parent.Context;
        
        // Root Card
        var card = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(12),
            CardElevation = AppUtil.DpToPx(0), // Flat style is modern
            StrokeWidth = AppUtil.DpToPx(0),
            StrokeColor = Color.Transparent,
            Clickable = true,
            Focusable = true
        };


        var lp = new RecyclerView.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
        lp.SetMargins(0, 8, 0, 8);
        card.LayoutParameters = lp;

        // Main Vertical Layout (Holds TopRow + HiddenActions)
        var mainContainer = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        mainContainer.Clickable = true;
        mainContainer.LayoutTransition = new LayoutTransition();

        mainContainer.LayoutParameters = new ViewGroup.LayoutParams(-1, -2);
     
        // --- TOP ROW (Visible) ---
        var topRow = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        topRow.SetGravity(GravityFlags.CenterVertical);
        topRow.SetPadding(20, 20, 20, 20);
        topRow.LayoutParameters = new LinearLayout.LayoutParams(-1, -2);
        topRow.Clickable = true;
        topRow.Visibility = ViewStates.Visible;
        topRow.Click += (o, s) =>
        {
            card.PerformClick();
        };

        // Image
        var imgCard = new MaterialCardView(ctx) { Radius = AppUtil.DpToPx(8), CardElevation = 0 };
        var imgView = new ImageView(ctx);
        imgView.SetScaleType(ImageView.ScaleType.CenterCrop);
        imgCard.AddView(imgView, new ViewGroup.LayoutParams(AppUtil.DpToPx(56), AppUtil.DpToPx(56)));
        topRow.AddView(imgCard);

        // Texts
        var textLayout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        var txtLp = new LinearLayout.LayoutParams(0, -2, 1f); // Weight 1
        txtLp.LeftMargin = AppUtil.DpToPx(16);
        textLayout.LayoutParameters = txtLp;

        var title = new MaterialTextView(ctx) { TextSize = 16, Typeface = Typeface.DefaultBold };
        title.SetMaxLines(1);
        title.Ellipsize = Android.Text.TextUtils.TruncateAt.End;

        var artist = new TextView(ctx) { TextSize = 14 };
        var llyout = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
        llyout.SetMargins(0, 0, 0, 0);
        artist.TextAlignment  = Android.Views.TextAlignment.ViewStart;
        artist.SetTextColor(Color.Gray);
        artist.Ellipsize = TextUtils.TruncateAt.Marquee;
        artist.Selected = true;
        textLayout.AddView(title);
        textLayout.AddView(artist);
        topRow.AddView(textLayout);

        // More Button (The Trigger)
         moreBtn = new MaterialButton(ctx, null, Resource.Attribute.materialIconButtonStyle);
        moreBtn.SetIconResource(Resource.Drawable.more1); // Ensure this drawable exists
        moreBtn.IconTint = Android.Content.Res.ColorStateList.ValueOf(Color.Gray);
        moreBtn.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Color.Transparent);
        moreBtn.RippleColor = Android.Content.Res.ColorStateList.ValueOf(Color.LightGray);
        var durationView = new TextView(ctx) { TextSize = 18, Typeface = Typeface.DefaultBold };
        durationView.SetTextColor(UiBuilder.IsDark(durationView) ? Color.Gray : Color.Black);
        durationView.TextSize = 10;
        durationView.Gravity = GravityFlags.CenterHorizontal;

        var rightLinearLayout = new LinearLayout(ctx)
        { 
            Orientation = Android.Widget.Orientation.Vertical 
        };
        rightLinearLayout.AddView(moreBtn);
        rightLinearLayout.AddView(durationView);



        topRow.AddView(rightLinearLayout);


        // --- EXPANDABLE ROW (Hidden by default) ---
        var expandRow = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        expandRow.SetGravity(GravityFlags.Center);
        expandRow.Visibility = ViewStates.Gone; // Hidden initially
        expandRow.LayoutParameters = new LinearLayout.LayoutParams(-1, -2);
        expandRow.SetPadding(0, 0, 0, 20);

      


        favBtn = CreateActionButton("Fav", Resource.Drawable.heart);
        expandRow.AddView(favBtn);

        StatsBtn = CreateActionButton("Stats", Resource.Drawable.stats);

        MaterialButton? LyricsBtn = CreateActionButton("Note", Resource.Drawable.pen);
        
        expandRow.AddView(StatsBtn);
        expandRow.AddView(LyricsBtn);

        MaterialButton? InfoBtn = CreateActionButton("Info", Resource.Drawable.infocircle);


        InfoBtn.TooltipText = $"View Song Info {MyViewModel.CurrentPlayingSongView.Title}";
            //expandRow.AddView(insertBeforeBtn);

            expandRow.AddView(InfoBtn);



        // --- EXPANDABLE ROW Two (Hidden by default) ---
        var expandRowTwo = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        expandRowTwo.SetGravity(GravityFlags.Center);
        expandRowTwo.Visibility = ViewStates.Gone; // Hidden initially
        expandRowTwo.LayoutParameters = new LinearLayout.LayoutParams(-1, -2);
        expandRowTwo.SetPadding(0, 0, 0, 20);


        ChipGroup artistsChipGroup = new ChipGroup(ctx);

        expandRowTwo.AddView(artistsChipGroup);
        

        

        MaterialButton? albumBtn = CreateActionButton("album", Resource.Drawable.musicalbum);
        albumBtn.Ellipsize = Android.Text.TextUtils.TruncateAt.Marquee;

        expandRowTwo.AddView(albumBtn);





 

        // Assemble
        mainContainer.AddView(topRow);
        mainContainer.AddView(expandRow);
        mainContainer.AddView(expandRowTwo);




        card.AddView(mainContainer);

        return new SongViewHolder(MyViewModel, ParentFragement, card, imgView, title, artist, moreBtn, durationView,expandRow,  (Button)favBtn,
            LyricsBtn, InfoBtn, StatsBtn);
    }


    private MaterialButton CreateActionButton(string text, int iconId)
    {
        var btn = new MaterialButton(ctx, null, Resource.Attribute.materialButtonOutlinedStyle);
        btn.Text = text;
        
        btn.SetTextColor(UiBuilder.IsDark(btn) ? Color.Gray : Color.ParseColor("#294159"));
        btn.SetIconResource(iconId);
        //btn.IconGravity = (int)GravityFlags.st;
        btn.IconTint = Android.Content.Res.ColorStateList.ValueOf(UiBuilder.IsDark(btn) ? Color.Gray : Color.ParseColor("#294159"));
        btn.SetPadding(30, 0, 30, 0);
        var lp = new LinearLayout.LayoutParams(-2, -2);
        lp.RightMargin = 10;
        btn.LayoutParameters = lp;
        btn.IconSize = AppUtil.DpToPx(30);
        return btn;
    }


    class SongViewHolder : AndroidX.RecyclerView.Widget.RecyclerView.ViewHolder
    {
        private readonly SerialDisposable _itemSubscription = new SerialDisposable();


        private readonly BaseViewModelAnd MyViewModel;
        private readonly Fragment _parentFrag;
        private readonly ImageView _img;
        private readonly TextView _title;
            private readonly TextView _artist;
        private readonly View _expandRow;
        private readonly MaterialButton _moreBtn;
        private readonly TextView _durationView;
        private readonly MaterialCardView _container;
        public View ContainerView => base.ItemView;

        public SongContextMenuFragment SongContexFragment { get; private set; }

        private readonly Button _favBtn;
        private readonly Button _infoBtn;
        private readonly Button lyricsBtn;
        private readonly Button _statsBtn;
        private SongModelView? _currentSong;
        private Action<int>? _expandAction;

        public SongViewHolder(BaseViewModelAnd vm, Fragment parentFrag, MaterialCardView container, ImageView img, TextView title, TextView artist, MaterialButton moreBtn, TextView durationView,
 View expandRow,Button favBtn, Button lyrBtn, MaterialButton infoBtn, Button statsBtn)
            : base(container)
        {
            MyViewModel = vm;
            _parentFrag = parentFrag;
            _container = container;
            _img = img;
            _title = title;
            _artist = artist;
            _moreBtn = moreBtn;
            _durationView= durationView;
            _expandRow = expandRow;
            _favBtn = favBtn;
            _infoBtn = infoBtn;
            lyricsBtn = lyrBtn;
            _statsBtn = statsBtn;
            _moreBtn.Click += (s, e) =>
            {
                // Always invoke the latest action with the current position
                _expandAction?.Invoke(BindingAdapterPosition);
            };

            lyrBtn.Click += (s, e) =>
            {
                MyViewModel._lyricsMgtFlow.LoadLyrics(_currentSong?.SyncLyrics);
                MyViewModel.SelectedSong = _currentSong;
                MyViewModel.NavigateToAnyPageOfGivenType(this._parentFrag, new LyricsViewFragment(MyViewModel), "toLyricsFromNP");
                
            };

        

            if (_infoBtn != null)
            {
                _infoBtn.Click += (s, e) =>
                {
                    if (_currentSong != null)
                    {
                        MyViewModel.SelectedSong = _currentSong;
                    
                         SongContexFragment = new SongContextMenuFragment(MyViewModel);


                        SongContexFragment.Show(parentFrag.ParentFragmentManager,"infoDiag");
                        

                    }
                };
            }

            // 2. Container Click (Play)
            _container.Click += async (s, e) =>
            {
                if (_currentSong != null)
                    await MyViewModel.PlaySongAsync(_currentSong);
            };

            _moreBtn.LongClickable = true;
            _moreBtn.LongClick += (s, e) =>
            {
                _container.PerformHapticFeedback(FeedbackConstants.LongPress);
                
                // Long press shows context menu with options
                ShowPlaybackOptionsMenu();
            };

            _statsBtn.Click += (s, e) =>
            {
                
                var infoSheet = new SongInfoBottomSheetFragment(MyViewModel, _currentSong);
                infoSheet.Show(_parentFrag.ParentFragmentManager, "SongInfoSheet");
            };



            lyrBtn.Click += async (s, e) =>
            {
                if (_currentSong != null)
                {
                    await MyViewModel.ShareSongViewClipboard(_currentSong);
                }
            };




            // 4. Image Click (Navigate)
            _img.Click += (s, e) =>
            {
                if (_currentSong != null)
                {
                    
                    MyViewModel.SelectedSong = _currentSong;
                    // Note: Transition name must be updated in Bind, but we can read it from the view here
                    string? tName = ViewCompat.GetTransitionName(_img);

                    if (tName != null)
                    {

                        switch (_mode)
                        {
                            case SongsToWatchSource.HomePage:
                                MyViewModel.NavigateToSingleSongPageFromHome(_parentFrag, tName, _img); 
                                break;

                            default:
                                break;
                        }

                    }
                    else
                    {
                        TransitionActivity act = (this._parentFrag!.Activity as TransitionActivity)!;
                        act.HandleBackPressInternal();
                    }
                    
                }
            };

            // 5. Artist Long Click
            _artist.LongClickable= true;
            _artist.LongClick += (s, e) =>
            {
                if (_currentSong?.ArtistName != null)
                {
                    var artistPickBtmSheet = new ArtistPickerBottomSheet(MyViewModel,_currentSong.ArtistsInDB(MyViewModel.RealmFactory));

                    artistPickBtmSheet.Show(parentFrag.ParentFragmentManager, "QueueSheet");
                    
                }
            };

            // 6. Fav Button
            _favBtn.Click += async (s, e) =>
            {
                
                if (_currentSong != null)
                {
                    await MyViewModel.AddFavoriteRatingToSong(_currentSong);
                    // Instant visual feedback
                    _favBtn.Text = !_currentSong.IsFavorite ? "Unfav" : "Fav";
                    _favBtn.SetIconResource(_currentSong.IsFavorite ? Resource.Drawable.heartlock : Resource.Drawable.heart);
                    _favBtn.IconTint = _currentSong.IsFavorite ? AppUtil.ToColorStateList(Color.DarkSlateBlue) : AppUtil.ToColorStateList(Color.Gray) ;
                    UiBuilder.ShowSnackBar(
   parentFrag.View,
   _currentSong.IsFavorite ? $"Added {_currentSong.Title} by {_currentSong.ArtistName} to Favorites " : $"Removed {_currentSong.Title} by {_currentSong.ArtistName} from Favorites",
   textColor: Color.Black
   
);
                    _favBtn.PerformHapticFeedback(FeedbackConstants.Confirm);
                }
            };
            _favBtn.LongClick += async (s, e) =>
            {
                


                if (_currentSong != null)
                {
                    await MyViewModel.RemoveSongFromFavorite(_currentSong);
                    var iconRes = _currentSong.IsFavorite ? Resource.Drawable.heartlock : Resource.Drawable.heart;
                    // Instant visual feedback
                    _favBtn.Text = !_currentSong.IsFavorite ? "Unfav" : "Fav";
                    _favBtn.SetIconResource(iconRes);
                    _favBtn.PerformHapticFeedback(FeedbackConstants.Reject);
                    UiBuilder.ShowSnackBar(
    parentFrag.View, 
    _currentSong.IsFavorite ? $"Added {_currentSong.Title} by {_currentSong.ArtistName} to Favorites " : $"Removed {_currentSong.Title} by {_currentSong.ArtistName} from Favorites",
    textColor: Color.Black,
    iconResId: iconRes
);
                }
            };
        }

        private void SongContextBottomSheetDismissed()
        {
            // do something
        }
        SongsToWatchSource _mode;
        public void Bind(SongModelView song, bool isExpanded, Action<int> onExpandToggle, SongsToWatchSource mode)
        {
            _currentSong = song;
            _expandAction = onExpandToggle;
            var sessionDisposable = new CompositeDisposable();
            _title.Text = song.Title;
            _artist.Text = song.OtherArtistsName ?? "Unknown";
            _durationView.Text = $"{song.DurationFormatted}";
            _mode = mode;
            if (song.HasSyncedLyrics)
            {
                _container.StrokeWidth = 4;
                _container.SetStrokeColor(AppUtil.ToColorStateList(Color.DarkSlateBlue));
            }

            // Set Transition Name
            var tName = $"sharedImage_{song.Id}";
            ViewCompat.SetTransitionName(_img, tName);

            // Image Loading
            if (!string.IsNullOrEmpty(song.CoverImagePath))
            {
                var domCol = MyViewModel.CurrentPlaySongDominantColor;
                if (domCol is not null)
                {
                    var colorBGA = (int)domCol.Alpha;
                    var colorBGR = (int)domCol.Red;
                    var colorBGG = (int)domCol.Green;
                    var colorBGB = (int)domCol.Blue;

                    _container.SetBackgroundColor(Color.Argb(10, colorBGR, colorBGB, colorBGB));
                }
                _img.SetImageWithGlide(song.CoverImagePath);
                
            }
            else
            {
                _img.SetImageResource(Resource.Drawable.musicnotess);
            }


            _expandRow.Visibility = isExpanded ? ViewStates.Visible : ViewStates.Gone;
         


            _container.StrokeColor = isExpanded ? Color.DarkSlateBlue : Color.ParseColor("#E0E0E0");
            _container.StrokeWidth = isExpanded ? 3 : 0;


            song.WhenPropertyChange(nameof(SongModelView.IsFavorite), s=>s.IsFavorite)
                .ObserveOn(RxSchedulers.UI)
                .Subscribe(IsFavorite =>
                {
                    if (IsFavorite)
                    {
                        _moreBtn.CornerRadius = AppUtil.DpToPx(10);
                        _moreBtn.StrokeWidth = AppUtil.DpToPx(1);
                        _favBtn.StrokeWidth = 0;
                        _moreBtn.SetStrokeColorResource(Resource.Color.m3_ref_palette_pink80);
                    }
                    else
                    {
                        _moreBtn.StrokeWidth = AppUtil.DpToPx(0);

                    }
                });
            song.WhenPropertyChange(nameof(SongModelView.IsCurrentPlayingHighlight), s => s.IsCurrentPlayingHighlight)
                        .ObserveOn(RxSchedulers.UI) // Ensure UI Thread
                        .Subscribe(isPlaying =>
                        {
                            if (isPlaying)
                            {
                                _title.SetTextColor(Color.DarkSlateBlue); // Highlight Text
                                                                          // _img.SetImageResource(Resource.Drawable.equalizer_anim); // Maybe show animation?
                            }
                            else
                            {
                                // Reset to normal
                                var isDark = _container.Context.Resources.Configuration.UiMode.HasFlag(Android.Content.Res.UiMode.NightYes);
                                _title.SetTextColor(isDark ? Color.White : Color.Black);
                            }
                        })
                        .DisposeWith(sessionDisposable);

            song.WhenPropertyChange(nameof(SongModelView.HasSyncedLyrics), s => s.HasSyncedLyrics)
                        .ObserveOn(RxSchedulers.UI) // Ensure UI Thread
                        .Subscribe(hasSyncLyrics =>
                        {
                            if (hasSyncLyrics)
                            {
                                _container.StrokeWidth = 1;
                            }
                            else
                            {
                                //_container.StrokeWidth = 0;
                                

                            }
                        })
                        .DisposeWith(sessionDisposable);

            song.WhenPropertyChange(nameof(SongModelView.CoverImagePath), s => s.CoverImagePath)
                .ObserveOn(RxSchedulers.UI)
                .Subscribe(path =>
                {
                    if (!string.IsNullOrEmpty(path))
                    {
                        if(song.TitleDurationKey == MyViewModel.CurrentPlayingSongView.TitleDurationKey)
                        {
                            MyViewModel.CurrentCoverImagePath = path;
                            MyViewModel.CurrentPlayingSongView.CoverImagePath = path;
                        }
                        Glide.With(_img.Context).Load(path)
                             .Placeholder(Resource.Drawable.musicnotess)
                             .Into(_img);
                       
                    }
                    else
                    {
                        _img.SetImageResource(Resource.Drawable.musicnotess);
                    }
                })
                .DisposeWith(sessionDisposable);







            _itemSubscription.Disposable = sessionDisposable;

        }

        private void ShowPlaybackOptionsMenu()
        {
            if (_currentSong == null)
                return;

            var ctx = _container.Context;
            if (ctx == null)
                return;

            // Create a bottom sheet dialog with playback options
            var dialog = new BottomSheetDialog(ctx);
            
            // Create the layout programmatically
            var mainLayout = new LinearLayout(ctx) 
            { 
                Orientation = Orientation.Vertical,
                LayoutParameters = new ViewGroup.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.WrapContent)
            };
            mainLayout.SetPadding(AppUtil.DpToPx(24), AppUtil.DpToPx(16), AppUtil.DpToPx(24), AppUtil.DpToPx(24));

            // Title
            var titleView = new MaterialTextView(ctx)
            {
                Text = "Playback Options",
                TextSize = 20,
                Typeface = Typeface.DefaultBold
            };
            titleView.SetForegroundGravity(GravityFlags.CenterHorizontal | GravityFlags.CenterVertical);
            titleView.SetPadding(0, 0, 0, AppUtil.DpToPx(16));
            mainLayout.AddView(titleView);

            // Play Now button
            var playNowBtn = new MaterialButton(ctx)
            {
                Text = "Play Now",
                LayoutParameters = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.WrapContent)
                {
                    BottomMargin = AppUtil.DpToPx(8)
                }
            };
            playNowBtn.SetIconResource(Resource.Drawable.play);
            playNowBtn.Click += async (s, e) =>
            {
                await MyViewModel.PlaySongWithActionAsync(_currentSong, Dimmer.Utilities.Enums.PlaybackAction.PlayNow);
                Toast.MakeText(ctx, $"Playing {_currentSong.Title}", ToastLength.Short)?.Show();
                dialog.Dismiss();
            };
            mainLayout.AddView(playNowBtn);

            // Play Next button
            var playNextBtn = new MaterialButton(ctx)
            {
                Text = "Play Next",
                LayoutParameters = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.WrapContent)
                {
                    BottomMargin = AppUtil.DpToPx(8)
                }
            };
            playNextBtn.SetIconResource(Resource.Drawable.media3_icon_next);
            playNextBtn.Click += async (s, e) =>
            {
                await MyViewModel.PlaySongWithActionAsync(_currentSong, Dimmer.Utilities.Enums.PlaybackAction.PlayNext);
                Toast.MakeText(ctx, $"Added {_currentSong.Title} to play next", ToastLength.Short)?.Show();
                dialog.Dismiss();
            };
            mainLayout.AddView(playNextBtn);

            // Add to Queue button
            var addToQueueBtn = new MaterialButton(ctx)
            {
                Text = "Add to Queue",
                LayoutParameters = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.WrapContent)
                {
                    BottomMargin = AppUtil.DpToPx(8)
                }
            };
            addToQueueBtn.SetIconResource(Resource.Drawable.media3_icon_queue_add);
            addToQueueBtn.Click += async (s, e) =>
            {
                await MyViewModel.PlaySongWithActionAsync(_currentSong, Dimmer.Utilities.Enums.PlaybackAction.AddToQueue);
                Toast.MakeText(ctx, $"Added {_currentSong.Title} to queue", ToastLength.Short)?.Show();
                dialog.Dismiss();
            };
            mainLayout.AddView(addToQueueBtn);

            // View in Queue button
            var viewInQueueBtn = new MaterialButton(ctx)
            {
                Text = "View in Queue",
                LayoutParameters = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.WrapContent)
            };
            viewInQueueBtn.SetIconResource(Resource.Drawable.eye);
            viewInQueueBtn.Click += (s, e) =>
            {
                MyViewModel.SelectedSong = _currentSong;
                var queueSheet = new QueueBottomSheetFragment(MyViewModel, viewInQueueBtn);
                viewInQueueBtn.Enabled = false;
                queueSheet.Show(_parentFrag.ParentFragmentManager, "QueueSheet");
                queueSheet.ScrollToSong(_currentSong);
                dialog.Dismiss();
            };
            mainLayout.AddView(viewInQueueBtn);

            dialog.SetContentView(mainLayout);
            dialog.Show();
           
        }



    }

    ~SongAdapter()
    {
        AdapterCallbacks = null;
    }
    class SongDiff : DiffUtil.Callback
    {
        private readonly IList<SongModelView> _oldList;
        private readonly IList<SongModelView> _newList;

        public SongDiff(IList<SongModelView> oldList, IList<SongModelView> newList)
        {
            _oldList = oldList;
            _newList = newList;
        }

        public override int OldListSize => _oldList.Count;
        public override int NewListSize => _newList.Count;

        // Fast check: Are these the same object (ID)?
        public override bool AreItemsTheSame(int oldPos, int newPos)
        {
            return _oldList[oldPos].Id == _newList[newPos].Id;
        }

        // Slower check: Did the visual content change?
        public override bool AreContentsTheSame(int oldPos, int newPos)
        {
            var oldItem = _oldList[oldPos];
            var newItem = _newList[newPos];

            // Compare only what is visible in the ViewHolder to be fast
            return oldItem.Title == newItem.Title &&
                   oldItem.ArtistName == newItem.ArtistName &&
                   oldItem.IsFavorite == newItem.IsFavorite &&
                   oldItem.CoverImagePath == newItem.CoverImagePath;
        }
    }

    public class ItemGestureListener : GestureDetector.SimpleOnGestureListener
    {   
        public event Action<int,View, SongModelView>? SingleTap;
        public event Action<int,View>? LongPressStage1;
        public event Action<int,View>? LongPressStage2;

        private readonly RecyclerView recycler;
        private readonly Handler handler = new Handler(Looper.MainLooper!);

        public ItemGestureListener(RecyclerView rv)
        {
            recycler = rv;
        }
        public override bool OnSingleTapUp(MotionEvent e)
        {
            var child = recycler.FindChildViewUnder(e.GetX(), e.GetY());
            if (child == null) return false;

            int pos = recycler.GetChildAdapterPosition(child);

            Console.WriteLine(child.GetType().FullName);
            // get adapter
            var adapter = recycler.GetAdapter() as SongAdapter; // replace SongAdapter with your actual adapter type

            var song = adapter?.GetItem(pos); // this assumes your adapter has a GetItem method
            if (song == null) return false;

            SingleTap?.Invoke(pos, child, song); // pass song too if you want

            return true;
        }



        public void CancelTimers()
    {
        handler.RemoveCallbacksAndMessages(null);
    }
    }

    public class TouchListener : Java.Lang.Object, RecyclerView.IOnItemTouchListener
    {
        private readonly GestureDetector detector;
        private readonly ItemGestureListener listener;

        public TouchListener(Context ctx, RecyclerView rv)
        {
            
            listener = new ItemGestureListener(rv);
            detector = new GestureDetector(ctx, listener);
        }
        
        public event Action<int, View, SongModelView>? SingleTap
        {
            add => listener.SingleTap += value;
            remove => listener.SingleTap -= value;
        }

        public event Action<int, View>? LongPressStage1
        {
            add => listener.LongPressStage1 += value;
            remove => listener.LongPressStage1 -= value;
        }

        public event Action<int, View>? LongPressStage2
        {
            add => listener.LongPressStage2 += value;
            remove => listener.LongPressStage2 -= value;
        }

        public bool OnInterceptTouchEvent(RecyclerView rv, MotionEvent e)
        {
            detector.OnTouchEvent(e);

            if (e.Action == MotionEventActions.Cancel)
                listener.CancelTimers();

            return false;
        }

        public void OnTouchEvent(RecyclerView rv, MotionEvent e)
        {
            detector.OnTouchEvent(e);
            if (e.Action == MotionEventActions.Cancel)
                listener.CancelTimers();
        }

        public void OnRequestDisallowInterceptTouchEvent(bool disallowIntercept) { }
    }

    public enum AdapterMode
    {
        Normal,
        MultiSelect,
        SingleSelect
    }

    public enum SongsToWatchSource
    {
        HomePage,
        QueuePage,
        AlbumPage,
        ArtistPage,
        PlaylistPage
    }
}
