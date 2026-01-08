

using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

using Android.Content.Res;
using Android.Text;
using Android.Views;
using Android.Widget;

using AndroidX.Lifecycle;

using Bumptech.Glide;

using DynamicData.Binding;

using Kotlin;

namespace Dimmer.ViewsAndPages.NativeViews;


internal class LyricsViewFragment : Fragment
{
    private BaseViewModelAnd viewModel;
    private RecyclerView _lyricsRecyclerView;
    private LyricsAdapter _adapter;
    private ImageView _backgroundImageView;
    private TextView _songTitleTv, _artistAlbumTv;
    private bool _isScreenKeepOnSetByThisFragment = false;

    public LyricsViewFragment(BaseViewModelAnd viewModel)
    {
        this.viewModel = viewModel;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var context = Context!;
        var root = new RelativeLayout(context) { LayoutParameters = new ViewGroup.LayoutParams(-1, -1) };

        // 1. Blurred Background Image
        _backgroundImageView = new ImageView(context)
        {
            LayoutParameters = new RelativeLayout.LayoutParams(-1, -1),
            
        };
        _backgroundImageView.SetScaleType(ImageView.ScaleType.CenterCrop);
        Glide.With(this)
            .Load(viewModel.CurrentPlayingSongView.CoverImagePath)
            .Into(_backgroundImageView);

        root.AddView(_backgroundImageView);

        // Dark Overlay for readability
        var overlay = new View(context)
        {
            LayoutParameters = new RelativeLayout.LayoutParams(-1, -1),
            Background = new ColorDrawable(Android.Graphics.Color.Argb(150, 0, 0, 0))
        };
        root.AddView(overlay);

        // 2. Main Container (Vertical)
        var mainContainer = new LinearLayout(context)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new RelativeLayout.LayoutParams(-1, -1)
        };
        mainContainer.SetPadding(40, 60, 40, 0);

        // 3. Header: Title (Marquee)
        _songTitleTv = new TextView(context)
        {
            TextSize = 24,
            Typeface = Typeface.DefaultBold,
            Ellipsize = TextUtils.TruncateAt.Marquee,
            HorizontalFadingEdgeEnabled = true,
            Selected = true // Required for Marquee
        };
        _songTitleTv.SetSingleLine(true);
        _songTitleTv.SetTextColor(Android.Graphics.Color.White);

        // 4. Header: Artist • Album (Marquee)
        _artistAlbumTv = new TextView(context)
        {
            TextSize = 16,
            Ellipsize = TextUtils.TruncateAt.Marquee,
            Selected = true
        };
        _artistAlbumTv.SetSingleLine(true);
        _artistAlbumTv.SetTextColor(Android.Graphics.Color.LightGray);

        mainContainer.AddView(_songTitleTv);
        mainContainer.AddView(_artistAlbumTv);

        // 5. Lyrics RecyclerView
        _lyricsRecyclerView = new RecyclerView(context)
        {
            LayoutParameters = new LinearLayout.LayoutParams(-1, -1) { TopMargin = 40 }
        };
        _lyricsRecyclerView.SetLayoutManager(new LinearLayoutManager(context));

        _adapter = new LyricsAdapter(viewModel.AllLines!);
        _lyricsRecyclerView.SetAdapter(_adapter);

        mainContainer.AddView(_lyricsRecyclerView);
        root.AddView(mainContainer);
       
        _songTitleTv.Text = viewModel.SelectedSong?.Title;
        _songTitleTv.Click += async (s, e) =>
        {
            await viewModel.PlaySongAsync(viewModel.SelectedSong);
        };
        _artistAlbumTv.Text = $"{viewModel.SelectedSong?.ArtistName}  •  {viewModel.SelectedSong?.AlbumName}";

        if (viewModel.CurrentPlayingSongView == viewModel.SelectedSong)
        { 
            SetupBindings();
        }
        ApplyBlur();

        return root;
    }
    public override void OnViewCreated(View view, Bundle? savedInstanceState)
    {
        base.OnViewCreated(view, savedInstanceState);
        viewModel.CurrentFragment = this;
    }
    
    public override void OnResume()
    {
        base.OnResume();
        UpdateScreenKeepOn();
    }

    public override void OnPause()
    {
        base.OnPause();
        ClearScreenKeepOn();
    }

    private void UpdateScreenKeepOn()
    {
        if (viewModel?.KeepScreenOnDuringLyrics == true && Activity?.Window != null && !_isScreenKeepOnSetByThisFragment)
        {
            Activity.Window.AddFlags(WindowManagerFlags.KeepScreenOn);
            _isScreenKeepOnSetByThisFragment = true;
        }
    }

    private void ClearScreenKeepOn()
    {
        if (_isScreenKeepOnSetByThisFragment && Activity?.Window != null)
        {
            Activity.Window.ClearFlags(WindowManagerFlags.KeepScreenOn);
            _isScreenKeepOnSetByThisFragment = false;
        }
    }
    private void SetupBindings()
    {
       
        // Listen for lyric changes from VM
        viewModel._lyricsMgtFlow.CurrentLyricIndex            
            //.WhenPropertyChange(nameof(viewModel.CurrentLine), newVal => viewModel.CurrentLine)
            .ObserveOn(RxSchedulers.UI)
            
            .Subscribe(currLineIndex =>
            {
                if (currLineIndex != -1)
                {
                    ScrollToCurrentLyric(currLineIndex);
                }
            })
            .DisposeWith(_disposables);
          
    }
    public override void OnDestroy()
    {
        base.OnDestroy();
        ClearScreenKeepOn();
        _disposables.Clear();
    }

    private readonly CompositeDisposable _disposables = new();
    private void ScrollToCurrentLyric(int currLineIndex)
    {
        try
        {

            _adapter.HighlightIndex(currLineIndex);

            // Smooth scroll to middle of screen

            var scroller = new CenterSmoothScroller(Context);
            scroller.TargetPosition = currLineIndex;
            _lyricsRecyclerView.GetLayoutManager()?.StartSmoothScroll(scroller);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private void ApplyBlur()
    {
        // Simple Android 12+ Blur (RenderEffect)
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.S)
        {
            _backgroundImageView.SetRenderEffect(RenderEffect.CreateBlurEffect(30f, 30f, Shader.TileMode.Clamp!));
        }
        // For older versions, you'd use a library like Glide or a custom StackBlur
    }
}

public class LyricsAdapter : RecyclerView.Adapter
{
    private IList<LyricPhraseModelView> _lyrics;
    private int _selectedIndex = -1;

    public LyricsAdapter(IList<LyricPhraseModelView> lyrics)
    {
        _lyrics = lyrics;
    }

    public void HighlightIndex(int index)
    {
        int oldIndex = _selectedIndex;
        _selectedIndex = index;
        NotifyItemChanged(oldIndex);
        NotifyItemChanged(_selectedIndex);
    }

    public override int ItemCount => _lyrics.Count;

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        var h = (LyricViewHolder)holder;
        var item = _lyrics[position];

        h.TextView.Text = item.Text;

        // Highlight logic
        if (position == _selectedIndex)
        {
            h.TextView.SetTextColor(Android.Graphics.Color.White);
            h.TextView.Alpha = 1.0f;
            h.TextView.Animate()?.ScaleX(1.05f)
                
                .ScaleY(1.05f).SetDuration(300).Start();
        }
        else
        {
            h.TextView.SetTextColor(Android.Graphics.Color.Gray);
            h.TextView.Alpha = 0.5f;
            h.TextView.Animate()?.ScaleX(1.0f).ScaleY(1.0f).SetDuration(300).Start();
        }
    }

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        var tv = new TextView(parent.Context!)
        {
            LayoutParameters = new ViewGroup.LayoutParams(-1, -2),
           
            TextSize = 22,
            Gravity = GravityFlags.CenterVertical,
            Typeface = Typeface.Create("sans-serif-medium", TypefaceStyle.Normal)
        };
        tv.SetPadding(0, 20, 0, 20);
        return new LyricViewHolder(tv);
    }

    class LyricViewHolder : RecyclerView.ViewHolder
    {
        public TextView TextView { get; }
        public LyricViewHolder(View v) : base(v)
        {
            TextView = (TextView)v;
        }
    }
}
public class CenterSmoothScroller : LinearSmoothScroller
{
    public CenterSmoothScroller(Android.Content.Context context) : base(context) { }
    public override int CalculateDtToFit(int viewStart, int viewEnd, int boxStart, int boxEnd, int snapPreference)
    {
        return (boxStart + (boxEnd - boxStart) / 2) - (viewStart + (viewEnd - viewStart) / 2);
    }
}