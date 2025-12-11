using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

using Bumptech.Glide;

using Google.Android.Material.MaterialSwitch;

using ScrollView = Android.Widget.ScrollView;

namespace Dimmer.ViewsAndPages.NativeViews;

public class SettingsFragment  : Fragment, IOnBackInvokedCallback
{
    private readonly string _transitionName;
    private BaseViewModelAnd MyViewModel;
    private RecyclerView _folderRecycler;
    private Button _addFolderButton;
    //private FolderAdapter _adapter;
    private LinearLayout.LayoutParams cardLayoutParam;
    private MaterialTextView lastFMMessageTextView;
    private TextInputEditText lastFMUnameTxtField;
    private TextInputEditText lastFMPwdTxtField;
    private MaterialSwitch rememberMeSwitch;
    private MaterialButton lastFMSubmitButton;
    private TextView appStatusText;
    private CardView systemStatusView;

    public SettingsFragment(string transitionName, BaseViewModelAnd myViewModel)
    {
        this.MyViewModel = myViewModel;
        _transitionName = transitionName;
        EnterTransition = new Google.Android.Material.Transition.MaterialSharedAxis(Google.Android.Material.Transition.MaterialSharedAxis.Z, true);
        ReturnTransition = new Google.Android.Material.Transition.MaterialSharedAxis(Google.Android.Material.Transition.MaterialSharedAxis.Z, false);
    }
    public SettingsFragment()
    {

    }

    private CompositeDisposable _disposables = new CompositeDisposable();

    public override void OnStart()
    {
        base.OnStart();

        MyViewModel.LogStream

            .ObserveOn(RxSchedulers.UI)
            .Subscribe(log =>
            {
                appStatusText.Text = log.Log;
            })
            .DisposeWith(_disposables);
    }

    public override void OnStop()
    {
        base.OnStop();
        // Cleans up the subscription when the Fragment is not visible
        _disposables.Clear();
    }



    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context;
        var scroll = new ScrollView(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent),
            FillViewport = true,
            Background = new Android.Graphics.Drawables.ColorDrawable(IsDark() ? Android.Graphics.Color.ParseColor("#121212") : Android.Graphics.Color.ParseColor("#F5F5F5"))
        };

        var root = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        root.SetPadding(40, 60, 40, 200); // Bottom padding for player sheet

        // Header
        var header = new TextView(ctx)
        {
            Text = "Settings",
            TextSize = 32,
            Typeface = Android.Graphics.Typeface.DefaultBold
        };
        header.SetPadding(20, 0, 0, 40);
        header.TransitionName = _transitionName;
        root.AddView(header);

        systemStatusView = new MaterialCardView(ctx)
            ;
        systemStatusView.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
        systemStatusView.SetPadding(20, 20, 20, 20);

        appStatusText = new TextView(ctx)
        {
            Text = "App Status: All systems operational",
            TextSize = 14
        };
        
        systemStatusView.AddView(appStatusText);
        root.AddView(systemStatusView);



        // --- SECTIONS ---
        root.AddView(CreateSectionHeader(ctx, "Appearance & Behavior"));
        root.AddView(CreateAppearanceSection(ctx));

        root.AddView(CreateSectionHeader(ctx, "Audio & Playback"));
        root.AddView(CreateAudioSection(ctx));

        root.AddView(CreateSectionHeader(ctx, "Library & Storage"));
        root.AddView(CreateLibrarySection(ctx));

        root.AddView(CreateSectionHeader(ctx, "Lyrics & Metadata"));
        root.AddView(CreateLyricsSection(ctx));

        root.AddView(CreateSectionHeader(ctx, "System"));
        root.AddView(CreateSystemSection(ctx));

        // Version Info
        var verText = new TextView(ctx)
        {
            Text = "Dimmer Audio v1.0.0",
            Gravity = GravityFlags.Center,
            Alpha = 0.5f
        };
        verText.SetPadding(0, 60, 0, 20);
        root.AddView(verText);

        scroll.AddView(root);
        return scroll;
    }

    // --- SECTION BUILDERS ---

    private View CreateAppearanceSection(Context ctx)
    {
        var layout = CreateCardLayout(ctx);

        layout.AddView(CreateSwitchRow(ctx, "Dark Mode", "Use dark theme application-wide",
            MyViewModel.IsDarkModeOn, (v) => MyViewModel.ToggleAppTheme()));

        layout.AddView(CreateDivider(ctx));

        //layout.AddView(CreateSwitchRow(ctx, "Minimize to Tray", "Keep playing in background when closed",
        //    MyViewModel.AppState.MinimizeToTrayPreference, (v) => MyViewModel.AppState.MinimizeToTrayPreference = v));

        layout.AddView(CreateDivider(ctx));

        //layout.AddView(CreateSwitchRow(ctx, "Stick to Top", "Keep window always on top",
        //    MyViewModel.AppState.IsStickToTop, (v) => MyViewModel.AppState.IsStickToTop = v));

        return WrapInCard(ctx, layout);
    }

    private View CreateAudioSection(Context ctx)
    {
        var layout = CreateCardLayout(ctx);

        MaterialButton reloadAllAlbumCovers = new MaterialButton(ctx) { Text = "Reload All Album Covers" };

        reloadAllAlbumCovers.SetBackgroundColor(Color.Transparent);
        reloadAllAlbumCovers.SetTextColor(IsDark()? Color.White : Color.Black);
        reloadAllAlbumCovers.Click += async (s, e) =>
        {
            await MyViewModel.EnsureAllCoverArtCachedForSongsCommand.ExecuteAsync(null);
            Toast.MakeText(ctx, "Reloading all album covers...", ToastLength.Short)?.Show();
        };

        layout.AddView(reloadAllAlbumCovers);

        layout.AddView(CreateDivider(ctx));

        return WrapInCard(ctx, layout);
    }

    private View CreateLibrarySection(Context ctx)
    {
        var layout = CreateCardLayout(ctx);
        layout.SetPadding(0, 0, 0, 0); // Remove padding for list look

        // Folder List Header
        var titleContainer = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        titleContainer.SetPadding(40, 40, 40, 20);
        var title = new TextView(ctx) { Text = "Music Folders", TextSize = 16, Typeface = Android.Graphics.Typeface.DefaultBold };
        titleContainer.AddView(title);
        layout.AddView(titleContainer);

        // Existing Folders
        if (MyViewModel.FolderPaths!= null)
        {
            foreach (var folder in MyViewModel.FolderPaths)
            {
                layout.AddView(CreateFolderRow(ctx, folder));
            }
        }

        // Add Button
        _addFolderButton = new MaterialButton(ctx)
        {
            Text = "Add Folder",
            Icon = AndroidX.Core.Content.ContextCompat.GetDrawable(ctx, Android.Resource.Drawable.IcInputAdd)
        };
        _addFolderButton.SetBackgroundColor(Android.Graphics.Color.Transparent);
        _addFolderButton.SetTextColor(IsDark() ? Android.Graphics.Color.White : Android.Graphics.Color.Black);
        _addFolderButton.Click += async (s, e) =>
        {
            if (Activity is TransitionActivity act)
            {
                var path = await act.PickFolderAsync();
                if (!string.IsNullOrEmpty(path))
                {
                    MyViewModel.AddMusicFoldersByPassingToService(new List<string> { path });
                }
            }
        };
        layout.AddView(_addFolderButton);

        return WrapInCard(ctx, layout);
    }

    private View CreateLyricsSection(Context ctx)
    {
        var layout = CreateCardLayout(ctx);

        //layout.AddView(CreateSwitchRow(ctx, "Mini Lyrics View", "Show floating lyrics on desktop",
        //    MyViewModel.AppState.IsMiniLyricsViewEnabled, (v) => MyViewModel.AppState.IsMiniLyricsViewEnabled = v));

        //layout.AddView(CreateDivider(ctx));

        // Lyrics Source Dropdown Simulation
        //var sourceRow = CreateActionRow(ctx, "Lyrics Source", MyViewModel.PreferredLyricsSource ?? "Auto");
        //sourceRow.Click += (s, e) => { /* Show Dialog to pick source */ };
        //layout.AddView(sourceRow);

        layout.AddView(CreateDivider(ctx));

        //layout.AddView(CreateSwitchRow(ctx, "Contribute Lyrics", "Allow uploading synced lyrics",
        //     MyViewModel.AppState.AllowLyricsContribution == "True",
        //     (v) => MyViewModel.AppState.AllowLyricsContribution = v ? "True" : "False"));

        return WrapInCard(ctx, layout);
    }

    private View CreateSystemSection(Context ctx)
    {
        var layout = CreateCardLayout(ctx);

        //layout.AddView(CreateSwitchRow(ctx, "Mouse Back Nav", "Use Mouse Button 4 to go back"
        //    //MyViewModel.AppState.AllowBackNavigationWithMouseFour,
        //    //(v) => MyViewModel.AppState.AllowBackNavigationWithMouseFour = v)
        //    );

        layout.AddView(CreateDivider(ctx));

        var resetBtn = new MaterialButton(ctx) { Text = "Reset Onboarding" };
        resetBtn.SetBackgroundColor(Android.Graphics.Color.ParseColor("#8B0000")); // Dark Red
        resetBtn.Click += (s, e) =>
        {
            //MyViewModel.AppState.IsFirstTimeUser = true;
        };

        var btnContainer = new LinearLayout(ctx);
        btnContainer.SetPadding(30, 30, 30, 30);
        btnContainer.AddView(resetBtn);
        layout.AddView(btnContainer);

        return WrapInCard(ctx, layout);
    }

    // --- HELPER WIDGETS ---

    private View CreateFolderRow(Context ctx, string path)
    {
        var row = new LinearLayout(ctx) { Orientation = Orientation.Horizontal, WeightSum = 10 };
        row.SetPadding(40, 20, 20, 20);
        row.SetGravity(GravityFlags.CenterVertical);

        var txt = new TextView(ctx) { Text = path, TextSize = 14 };
        txt.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 6);

        var delBtn = new ImageView(ctx);
        delBtn.SetImageResource(Android.Resource.Drawable.IcMenuDelete);
        delBtn.ImageTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Gray);
        delBtn.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 2);
        delBtn.Click += (s, e) =>
        {
            //MyViewModel.AppState.UserMusicFoldersPreference.Remove(path);
            ParentFragmentManager.BeginTransaction().Detach(this).Attach(this).Commit();
        };

        var rescanBtn = new ImageView(ctx);
        Glide.With(ctx).Load(Resource.Drawable.reset).Into(rescanBtn);
        rescanBtn.ImageTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Gray);
        rescanBtn.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1);
        rescanBtn.SetMaxHeight(AppUtil.DpToPx(10));
        rescanBtn.SetMaxWidth(AppUtil.DpToPx(10));
        rescanBtn.Click += async (s, e) =>
        {

            _= MyViewModel.ReScanMusicFolderByPassingToService(path);
            Toast.MakeText(ctx, $"Rescanning {path}", ToastLength.Short)?.Show();
        };


        row.AddView(txt);
        row.AddView(rescanBtn);
        row.AddView(delBtn);
        return row;
    }

    private View CreateSwitchRow(Context ctx, string title, string subtitle, bool isChecked, Action<bool> onToggle)
    {
        var row = new LinearLayout(ctx) { Orientation = Orientation.Horizontal, WeightSum = 10 };
        row.SetPadding(30, 30, 30, 30);
        row.SetGravity(GravityFlags.CenterVertical);
        
        row.Clickable = true;
        

        var textLayout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        textLayout.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 8);

        var t1 = new TextView(ctx) { Text = title, TextSize = 16, Typeface = Android.Graphics.Typeface.DefaultBold };
        var t2 = new TextView(ctx) { Text = subtitle, TextSize = 12};
        t2.SetTextColor(Color.Gray);
        textLayout.AddView(t1);
        textLayout.AddView(t2);

        var sw = new MaterialSwitch(ctx) { Checked = isChecked };
        sw.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 2);
        sw.CheckedChange += (s, e) => onToggle(e.IsChecked);

        row.Click += (s, e) => sw.Checked = !sw.Checked; // Clicking row toggles switch

        row.AddView(textLayout);
        row.AddView(sw);
        return row;
    }

    private View CreateActionRow(Context ctx, string title, string value)
    {
        var row = new LinearLayout(ctx) { Orientation = Orientation.Horizontal, WeightSum = 10 };
        row.SetPadding(30, 30, 30, 30);
        row.SetGravity(GravityFlags.CenterVertical);
        

        var t1 = new TextView(ctx) { Text = title, TextSize = 16, Typeface = Android.Graphics.Typeface.DefaultBold };
        t1.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 6);

        var t2 = new TextView(ctx) { Text = value, TextSize = 14,  Gravity = GravityFlags.End };
        t2.SetTextColor(Android.Graphics.Color.DarkSlateBlue);
        t2.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 4);

        row.AddView(t1);
        row.AddView(t2);
        return row;
    }

    private MaterialCardView WrapInCard(Context ctx, View child)
    {
        var card = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(16),
            CardElevation = AppUtil.DpToPx(2),
            UseCompatPadding = true,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        card.SetBackgroundColor(IsDark() ? Android.Graphics.Color.ParseColor("#202020") : Android.Graphics.Color.White);
        card.AddView(child);
        return card;
    }

    private LinearLayout CreateCardLayout(Context ctx)
    {
        return new LinearLayout(ctx) { Orientation = Orientation.Vertical, LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent) };
    }

    private View CreateDivider(Context ctx)
    {
        var v = new View(ctx) { LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, 1) };
        v.SetBackgroundColor(IsDark() ? Android.Graphics.Color.DarkGray : Android.Graphics.Color.LightGray);
        v.Alpha = 0.2f;
        return v;
    }

    private TextView CreateSectionHeader(Context ctx, string text)
    {
        var tv = new TextView(ctx) { Text = text, TextSize = 14 };
        tv.SetTextColor(Android.Graphics.Color.DarkSlateBlue);
        tv.SetTypeface(null, Android.Graphics.TypefaceStyle.Bold);
        tv.SetPadding(40, 40, 0, 10);
        return tv;
    }

    private bool IsDark() => (Resources.Configuration.UiMode & Android.Content.Res.UiMode.NightMask) == Android.Content.Res.UiMode.NightYes;
    public void OnBackInvoked()
    {
        Toast.MakeText(Context!, "Back invoked in Settings Fragment", ToastLength.Short)?.Show();
    }

}