using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

using Bumptech.Glide;

using Dimmer.UiUtils;

using DynamicData;
using DynamicData.Binding;

using Google.Android.Material.MaterialSwitch;

using ScrollView = Android.Widget.ScrollView;

namespace Dimmer.ViewsAndPages.NativeViews;

public class SettingsFragment  : Fragment, IOnBackInvokedCallback
{
    private readonly string _transitionName;
    private BaseViewModelAnd MyViewModel;
    private Button _addFolderButton;
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

        var horizontalLayout = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        var backBtn = new MaterialButton(ctx)
        { CornerRadius = AppUtil.DpToPx(8) };
        backBtn.SetIconResource(Resource.Drawable.backb);
        backBtn.SetBackgroundColor(Color.Transparent);
        backBtn.Click += (s,e) =>
        {
            if (Activity is TransitionActivity act)
            {
                act.OnBackPressedDispatcher.OnBackPressed();
            }
        };

        // Header
        var header = new TextView(ctx)
        {
            Text = "Settings",
            TextSize = 32,
            Typeface = Android.Graphics.Typeface.DefaultBold
        };
        header.SetPadding(20, 0, 0, 40);
        header.TransitionName = _transitionName;

        horizontalLayout.AddView(backBtn);
        horizontalLayout.AddView(header);

        root.AddView(horizontalLayout);

        systemStatusView = new MaterialCardView(ctx)
            ;
        systemStatusView.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
        systemStatusView.SetPadding(20, 20, 20, 20);

        appStatusText = new TextView(ctx)
        {
            Text = "App Status: All systems operational",
            TextSize = 14
        };
        appStatusText.SetPadding(15, 15, 15, 15);
        
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
            MyViewModel.IsDarkModeOn, (v) => MyViewModel.ToggleAppThemeAnd()));

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
           _= Task.Run(async ()=> await MyViewModel.EnsureAllCoverArtCachedForSongsCommand.ExecuteAsync(null));
            Toast.MakeText(ctx, "Reloading all album covers...", ToastLength.Short)?.Show();
        };

        MaterialButton FetchAllLyricsLrcLib = new MaterialButton(ctx) { Text = "Fetch Lyrics Online" };

        FetchAllLyricsLrcLib.SetBackgroundColor(Color.Transparent);
        FetchAllLyricsLrcLib.SetTextColor(IsDark()? Color.White : Color.Black);
        FetchAllLyricsLrcLib.Click += async (s, e) =>
        {
            CancellationTokenSource cts = new();
           _= Task.Run(async ()=> await MyViewModel.LoadAllSongsLyricsFromOnlineAsync(cts));
            Toast.MakeText(ctx, "Fetching lyrics...", ToastLength.Short)?.Show();
        };

        layout.AddView(reloadAllAlbumCovers);

        layout.AddView(CreateDivider(ctx));
        layout.AddView(FetchAllLyricsLrcLib);

        return WrapInCard(ctx, layout);
    }
    private LinearLayout _folderRowsContainer;
    private View CreateLibrarySection(Context ctx)
    {
        MusicFoldersLayout = CreateCardLayout(ctx);
        MusicFoldersLayout.SetPadding(0, 0, 0, 0); // Remove padding for list look

        // Folder List Header
        var titleContainer = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        titleContainer.SetPadding(40, 40, 40, 20);
        var title = new TextView(ctx) { Text = "Music Folders", TextSize = 16, Typeface = Android.Graphics.Typeface.DefaultBold };
        titleContainer.AddView(title);
        MusicFoldersLayout.AddView(titleContainer);


        _folderRowsContainer = new LinearLayout(ctx)
        { Orientation = Android.Widget.Orientation.Vertical };
        MusicFoldersLayout.AddView(_folderRowsContainer);
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
        MusicFoldersLayout.AddView(_addFolderButton);

        return WrapInCard(ctx, MusicFoldersLayout);
    }
    private HashSet<string> _currentFolderUris = new HashSet<string>();

    public override void OnResume()
    {
        base.OnResume();
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
        {
            Activity?.OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(
                (int)IOnBackInvokedDispatcher.PriorityDefault, this);
        }
        MyViewModel.WhenValueChanged(x=>x.FolderPaths)
                     .ObserveOn(RxSchedulers.UI) 
                     .Subscribe(folderPaths =>
                     {
                         
                         if (folderPaths == null) return;
                         var listOfStrFromUriPath = new List<string>();
                         foreach (var path in folderPaths)
                         {
                             var uriFromStr = Android.Net.Uri.Parse(path);
                             if (uriFromStr != null)
                             {
                                 var decodedStrFromUriPath = AndroidFolderPicker.GetPathFromUri(uri: uriFromStr);
                                 if (decodedStrFromUriPath is not null)
                                 { 
                                     listOfStrFromUriPath.Add(decodedStrFromUriPath); 
                                 }
                             }
                         }
                         var newSet = new HashSet<string>(listOfStrFromUriPath);

                         // Remove rows that no longer exist
                         for (int i = _folderRowsContainer.ChildCount - 1; i >= 0; i--)
                         {
                             var child = _folderRowsContainer.GetChildAt(i);
                             if (child != null)
                             {
                                 var tag = child.Tag?.GetType() == typeof(string);
                                 if (tag)
                                 {
                                     string stringTag = child.Tag.ToString();

                                     if (!newSet.Contains(stringTag))
                                     {
                                         _folderRowsContainer.RemoveViewAt(i);
                                         _currentFolderUris.Remove(stringTag);
                                     }
                                 }
                             }
                         }

                         // Add new rows
                         foreach (var folder in newSet)
                         {
                             if (!_currentFolderUris.Contains(folder))
                             {
                                 var row = CreateFolderRow(Context, folder);
                                 row.Tag = folder; // store URI for diff
                                 _folderRowsContainer.AddView(row);
                                 _currentFolderUris.Add(folder);
                             }
                         }
                     })
                     .DisposeWith(sessionDisposable);
    }
    public override void OnDestroy()
    {
        base.OnDestroy();
        sessionDisposable.Dispose();
    }
    CompositeDisposable sessionDisposable = new CompositeDisposable();

    public LinearLayout MusicFoldersLayout { get; private set; }

    private View CreateLyricsSection(Context ctx)
    {
        var layout = CreateCardLayout(ctx);

        var KeepScreenOnView = CreateSwitchRow(ctx, "Keep Screen On", "Keep screen on when viewing sync lyrics page",
            MyViewModel.KeepScreenOnDuringLyrics, (v) =>
            {
                MyViewModel.KeepScreenOnDuringLyrics = v;
            });

        layout.AddView(KeepScreenOnView);

        layout.AddView(CreateDivider(ctx));

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
            MyViewModel.DeleteFolderPath(path);
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

        row.Tag = path;
        row.AddView(txt);
        row.AddView(rescanBtn);
        row.AddView(delBtn);
        return row;
    }

    private static LinearLayout CreateSwitchRow(Context ctx, string title, string subtitle, bool isChecked, Action<bool> onToggle)
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

        row.Click += (s, e) =>
        {
            sw.Checked = !sw.Checked;
            //isChecked = !isChecked;
        }; // Clicking row toggles switch

        row.AddView(textLayout);
        row.AddView(sw);
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
        if (Activity is TransitionActivity act)
        {
            act.OnBackPressedDispatcher.OnBackPressed();
        }
    }
    //public void OnBackInvoked()
    //{
    //    Toast.MakeText(Context!, "Back invoked in Settings Fragment", ToastLength.Short)?.Show();
    //}

}