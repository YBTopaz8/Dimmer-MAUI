using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Bumptech.Glide;
using Google.Android.Material.Tabs;

using Hqub.Lastfm.Entities;

namespace Dimmer.ViewsAndPages.NativeViews.DimmerLive.LastFMViews;


public class LastFmInfoFragment : Fragment
{
    private readonly BaseViewModelAnd _viewModel;
    private ImageView _userAvatar;
    private TextView _userName, _totalScrobbles;
    private TabLayout _tabLayout;
    private RecyclerView _tracksRecycler;
    private LastFmTrackAdapter _adapter;

    public LastFmInfoFragment(BaseViewModelAnd vm)
    {
        _viewModel = vm;
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context;
        var root = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        root.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);

        // --- 1. HEADER (Profile Info) ---
        var headerCard = new FrameLayout(ctx);
        headerCard.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, AppUtil.DpToPx(200));
        headerCard.SetBackgroundColor(Android.Graphics.Color.ParseColor("#202020"));

        // Background/Avatar
        _userAvatar = new ImageView(ctx);
        _userAvatar.SetScaleType(ImageView.ScaleType.CenterCrop);
        _userAvatar.LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
        _userAvatar.Alpha = 0.6f; // Dim background
        headerCard.AddView(_userAvatar);

        // Text Overlay
        var infoLay = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        infoLay.SetGravity(GravityFlags.Center);
        infoLay.LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent) { Gravity = GravityFlags.Center };

        _userName = new TextView(ctx) { Text = "Loading...", TextSize = 24, Typeface = Android.Graphics.Typeface.DefaultBold, Gravity = GravityFlags.Center };
        _userName.SetTextColor(Android.Graphics.Color.White);

        _totalScrobbles = new TextView(ctx) { Text = "0 Scrobbles", TextSize = 14, Gravity = GravityFlags.Center };
        _totalScrobbles.SetTextColor(Android.Graphics.Color.LightGray);

        infoLay.AddView(_userName);
        infoLay.AddView(_totalScrobbles);
        headerCard.AddView(infoLay);
        root.AddView(headerCard);

        // --- 2. TABS ---
        _tabLayout = new TabLayout(ctx);
        _tabLayout.AddTab(_tabLayout.NewTab().SetText("Recent"));
        _tabLayout.AddTab(_tabLayout.NewTab().SetText("Top Tracks"));
        _tabLayout.AddTab(_tabLayout.NewTab().SetText("Loved"));
        root.AddView(_tabLayout);

        // --- 3. TRACK LIST ---
        _tracksRecycler = new RecyclerView(ctx);
        _tracksRecycler.SetLayoutManager(new LinearLayoutManager(ctx));
        _tracksRecycler.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);

        // Initialize Adapter with empty list and click handler
        _adapter = new LastFmTrackAdapter(ctx, new List<Track>(), OnTrackClicked);
        _tracksRecycler.SetAdapter(_adapter);

        root.AddView(_tracksRecycler);

        return root;
    }

    public override void OnResume()
    {
        base.OnResume();
        _tabLayout.TabSelected += OnTabSelected;

        // Initial Data Load (Simulated or from VM)
        LoadUserData();
        LoadRecentTracks(); // Default tab
    }

    public override void OnPause()
    {
        base.OnPause();
        _tabLayout.TabSelected -= OnTabSelected;
    }

    private void OnTabSelected(object? sender, TabLayout.TabSelectedEventArgs? e)
    {
        switch (e?.Tab?.Position)
        {
            case 0: LoadRecentTracks(); break;
            case 1: LoadTopTracks(); break; // Implement in VM
            case 2: LoadLovedTracks(); break; // Implement in VM
        }
    }
    //private async Task LoadUserData()
    //{

    //    // 1. Get Data from VM
    //    //user = MyViewModel.CurrentUserLocal?.LastFMAccountInfo; // Assuming this property exists on your VM parity

    //    if (MyViewModel.LastFMService.IsAuthenticated)
    //    {
    //        LastFMGridNonAuth.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
    //        LastFMAuthedSection.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
    //        // User Info
    //        var userr = await MyViewModel.LastFMService.GetUserInfoAsync();
    //        if (userr is null)
    //        {
    //            var usr = MyViewModel.CurrentUserLocal.LastFMAccountInfo;
    //            if (usr is not null)
    //            {
    //                user = usr;
    //            }
    //            else
    //            {
    //                return;
    //            }
    //        }
    //        else
    //        {
    //            user = userr.ToLastFMUserView();
    //        }

    //        UserNameTxt.Text = user.Name;
    //        TotalScrobblesTxt.Text = $"{user.Playcount:N0} Scrobbles";
    //        scrobblingSince.Text = $"Scrobbling since {user.Registered:dd MMM yyyy}";
    //        // If user.Image is a string URL:
    //        if (!string.IsNullOrEmpty(user.Image.Url))
    //        {
    //            if (!string.IsNullOrEmpty(user.Image.Url))
    //            {
    //                UserAvatarImg.ProfilePicture
    //                    = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(user.Image.Url));
    //            }
    //            UserAvatarImg.DisplayName = user.Name;
    //        }

    //        if (Connectivity.NetworkAccess != NetworkAccess.Internet) return;
    //        await MyViewModel.LoadUserLastFMDataAsync(user);

    //    }
    //    else
    //    {
    //        LastFMGridNonAuth.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
    //        LastFMAuthedSection.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
    //        UserNameTxt.Text = "Not Connected";
    //        TotalScrobblesTxt.Text = "Log in via Settings";
    //    }
    //}
    private void LoadUserData()
    {
        // Assuming VM has this data
        var user = _viewModel.CurrentUserLocal?.LastFMAccountInfo; // Or specific LastFM property
        if (user != null)
        {
            _userName.Text = user.Name;
            _totalScrobbles.Text = $"{user.Playcount:N0} Scrobbles"; // Format number

            if (user.Image is not null)
            {
                Glide.With(this).Load(user.Image.Url)
                .Placeholder(Resource.Drawable.usercircle).Into(_userAvatar)
                ;
            }
        }
    }

    private async void LoadRecentTracks()
    {
        // Fetch from Service via ViewModel
        // var tracks = await _viewModel.LastFmService.GetRecentTracksAsync();
        // For demo, creating dummy data matching your class
        var dummyTracks = new List<Track>
        {
            new Track { Name = "Starlight", Artist = new Artist { Name = "Muse" }, Duration = 240000, UserLoved = true, IsNull = false },
            new Track { Name = "Time", Artist = new Artist { Name = "Pink Floyd" }, Duration = 420000, UserLoved = false, IsNull = false,
             NowPlaying = true }
        };

        _adapter.UpdateData(dummyTracks);
    }

    // Placeholders
    private void LoadTopTracks() 
    { 
        _adapter.UpdateData(new List<Track>()); 
    }
    private void LoadLovedTracks() { _adapter.UpdateData(new List<Track>()); }

    private void OnTrackClicked(Track track)
    {
        if (track?.IsOnPresentDevice == false)
        {
            // Show bottom sheet for tracks not on device
            var bottomSheet = new LastFmTrackInfoBottomSheet(track);
            bottomSheet.Show(ChildFragmentManager, "LastFmTrackInfo");
        }
        else
        {
            // TODO: Navigate to song detail page for tracks on device
            // This requires getting the SongModelView from the track's OnDeviceObjectId
            // and navigating to the appropriate detail page/fragment
            // For now, show the info bottom sheet for all tracks
            var bottomSheet = new LastFmTrackInfoBottomSheet(track);
            bottomSheet.Show(ChildFragmentManager, "LastFmTrackInfo");
        }
    }

    // --- ADAPTER ---
    class LastFmTrackAdapter : RecyclerView.Adapter
    {
        Context _ctx;
        List<Track> _items;
        Action<Track> _onItemClick;

        public LastFmTrackAdapter(Context ctx, List<Track> items, Action<Track> onItemClick)
        {
            _ctx = ctx;
            _items = items;
            _onItemClick = onItemClick;
        }

        public void UpdateData(List<Track> newItems)
        {
            _items = newItems;
            NotifyDataSetChanged();
        }

        public override int ItemCount => _items.Count;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            // Grid of 3: Image | Info | Status
            var root = new LinearLayout(_ctx) { Orientation = Orientation.Horizontal, WeightSum = 10 };
            root.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, AppUtil.DpToPx(70));
            root.SetPadding(10, 10, 10, 10);
            root.SetGravity(GravityFlags.CenterVertical);
            root.Clickable = true;
            root.Focusable = true;

            // 1. Image (Weight 2)
            var img = new ImageView(_ctx);
            img.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, 2f);
            img.SetScaleType(ImageView.ScaleType.CenterCrop);
            // img.SetBackgroundColor(Android.Graphics.Color.DarkGray); // Placeholder

            // 2. Info (Weight 6)
            var infoLay = new LinearLayout(_ctx) { Orientation = Orientation.Vertical};
            infoLay.SetGravity(GravityFlags.CenterVertical);
            infoLay.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, 6f);
            infoLay.SetPadding(20, 0, 0, 0);

            var title = new TextView(_ctx) { TextSize = 16, Typeface = Android.Graphics.Typeface.DefaultBold};
            title.Ellipsize = Android.Text.TextUtils.TruncateAt.End;
            var artist = new TextView(_ctx) { TextSize = 14, Alpha = 0.7f};

            infoLay.AddView(title);
            infoLay.AddView(artist);

            // 3. Status (Weight 2)
            var statusLay = new LinearLayout(_ctx) { Orientation = Orientation.Vertical};
            statusLay.SetGravity(GravityFlags.Center);
            statusLay.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, 2f);

            var loveIcon = new ImageView(_ctx); // Heart icon
            loveIcon.LayoutParameters = new LinearLayout.LayoutParams(40, 40);

            var duration = new TextView(_ctx) { TextSize = 12 };

            statusLay.AddView(loveIcon);
            statusLay.AddView(duration);

            root.AddView(img);
            root.AddView(infoLay);
            root.AddView(statusLay);

            var vh = new TrackVH(root, img, title, artist, loveIcon, duration);
            
            // Set up click handler once in ViewHolder creation
            root.Click += (sender, e) =>
            {
                var position = vh.BindingAdapterPosition;
                if (position != RecyclerView.NoPosition && position < _items.Count)
                {
                    _onItemClick?.Invoke(_items[position]);
                }
            };

            return vh;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = holder as TrackVH;
            var item = _items[position];

            // Bind Data
            vh.Title.Text = item.Name;
            vh.Artist.Text = item.Artist?.Name ?? "Unknown";

            // Duration Format (ms -> mm:ss)
            var ts = TimeSpan.FromMilliseconds(item.Duration);
            vh.Duration.Text = item.NowPlaying ? "Playing" : $"{ts.Minutes}:{ts.Seconds:D2}";

            // Love Icon
            if (item.UserLoved)
            {
                vh.LoveIcon.Visibility = ViewStates.Visible;
                vh.LoveIcon.SetImageResource(Resource.Drawable.heartlock); // Ensure drawable exists
                vh.LoveIcon.ImageTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Red);
            }
            else
            {
                vh.LoveIcon.Visibility = ViewStates.Invisible;
            }

            // Image Load (Assuming item.Images has a URL)
            // if (item.Images?.Count > 0) Glide.With(_ctx).Load(item.Images.Last().Url).Into(vh.Img);
            // else 
            vh.Img.SetImageResource(Resource.Drawable.musicnotess); // Fallback
        }

        class TrackVH : RecyclerView.ViewHolder
        {
            public ImageView Img, LoveIcon;
            public TextView Title, Artist, Duration;
            public TrackVH(View v, ImageView i, TextView t, TextView a, ImageView l, TextView d) : base(v)
            { Img = i; Title = t; Artist = a; LoveIcon = l; Duration = d; }
        }
    }
}