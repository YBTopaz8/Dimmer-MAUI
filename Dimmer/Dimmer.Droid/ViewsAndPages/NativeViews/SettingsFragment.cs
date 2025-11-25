using Android.Graphics;

using Google.Android.Material.Button;
using Google.Android.Material.Card;
using Google.Android.Material.Chip;
using Google.Android.Material.MaterialSwitch;
using Google.Android.Material.TextField;
using Google.Android.Material.TextView;

using ScrollView = Android.Widget.ScrollView;

namespace Dimmer.ViewsAndPages.NativeViews;

internal class SettingsFragment  : Fragment, IOnBackInvokedCallback
{
    private readonly string _transitionName;
    private BaseViewModelAnd MyViewModel;
    private RecyclerView _folderRecycler;
    private Button _addFolderButton;
    private FolderAdapter _adapter;
    private LinearLayout.LayoutParams cardLayoutParam;
    private MaterialTextView lastFMMessageTextView;
    private TextInputEditText lastFMUnameTxtField;
    private TextInputEditText lastFMPwdTxtField;
    private MaterialSwitch rememberMeSwitch;
    private MaterialButton lastFMSubmitButton;

    public SettingsFragment(string transitionName, BaseViewModelAnd myViewModel)
    {
        this.MyViewModel = myViewModel;
        _transitionName = transitionName;
    }
    public SettingsFragment()
    {
        
    }
    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context!;
        var root = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };
        var pageScrollViewer = new ScrollView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };
        var scrollLinearLayout = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent)
        };
        scrollLinearLayout.SetPadding(20, AppUtil.DpToPx(20), AppUtil.DpToPx(20)
            , AppUtil.DpToPx(20));

        ImageView pageIcon = new ImageView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(AppUtil.DpToPx(20), AppUtil.DpToPx(20))
            {
                Gravity = GravityFlags.CenterVertical | GravityFlags.Right,
            }
        };
        pageIcon.SetImageResource(Resource.Drawable.settings);
        ColorFilter iconColFil = new ColorFilter();
        iconColFil = new PorterDuffColorFilter(Color.DarkSlateBlue, PorterDuff.Mode.SrcIn);
        pageIcon.SetColorFilter(iconColFil);

        pageIcon.TransitionName = _transitionName;

        cardLayoutParam = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                AppUtil.DpToPx(50));
        
        cardLayoutParam.SetMargins(AppUtil.DpToPx(8), AppUtil.DpToPx(8), AppUtil.DpToPx(8), AppUtil.DpToPx(8));
        MaterialCardView firstCard = new MaterialCardView(ctx)
        {
            LayoutParameters = cardLayoutParam,
            Radius = 12f,
            CardElevation = 2f,
        };
        
        firstCard.StrokeColor = Color.DarkSlateBlue;
        ColorStateList rippleColorStateList = ColorStateList.ValueOf(Color.Pink);
        firstCard.RippleColor = rippleColorStateList;
        firstCard.Clickable = true;
        firstCard.SetCardBackgroundColor(Color.Transparent);
        

        LinearLayout firstLinear = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent)
        };

        TextView cardTitleText = new TextView(ctx)
        {
            Text = "Settings Page",
            TextSize = 20f,
            Gravity = GravityFlags.Center,
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };
        firstLinear.AddView(cardTitleText);

        firstCard.AddView(firstLinear);

        var HorizontalLayout = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent)
        };
        var backMaterialChip = new Chip(ctx)
        {
            Text = "back",
            ChipStrokeWidth = 2f,
            ChipStrokeColor = ColorStateList.ValueOf(Color.DarkSlateBlue),
            RippleColor = ColorStateList.ValueOf(Color.DarkSlateBlue),
            Checkable = false,            
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent)
        };
        backMaterialChip.Click += BackMaterialChip_Click;
        
        HorizontalLayout.AddView(backMaterialChip);
        HorizontalLayout.AddView(pageIcon);

        scrollLinearLayout.AddView(HorizontalLayout);
        scrollLinearLayout.AddView(firstCard);
        var layoutParams = new LinearLayout.LayoutParams(
    ViewGroup.LayoutParams.MatchParent,
    ViewGroup.LayoutParams.WrapContent);

        // Set Margins (Left, Top, Right, Bottom) in Pixels
        layoutParams.SetMargins(0, 20, 0, 20);

        LinearLayout secondLinear = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent)
        };

        var gridOfTwoColumns = new GridLayout(ctx)
        {
            ColumnCount = 2,
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent)
        };
        var columnOneText = new TextView(ctx)
        {
            Text = "Music Folders:",
            TextSize = 18f,
            Gravity = GravityFlags.Left,
            LayoutParameters = new GridLayout.LayoutParams()
            {
                Width = ViewGroup.LayoutParams.WrapContent,
                Height = ViewGroup.LayoutParams.WrapContent,
                ColumnSpec = GridLayout.InvokeSpec(0),
                RowSpec = GridLayout.InvokeSpec(0)
            }
        };
        gridOfTwoColumns.AddView(columnOneText);

        var gridLayoutParam = new GridLayout.LayoutParams()
        {
            Width = ViewGroup.LayoutParams.WrapContent,
            Height = ViewGroup.LayoutParams.WrapContent,
            ColumnSpec = GridLayout.InvokeSpec(1),
            RowSpec = GridLayout.InvokeSpec(0)
        };
        var columnTwoCardView = new MaterialCardView(ctx)
        {
            
            LayoutParameters = gridLayoutParam,
            Radius =20f,
            CardElevation=10f,
        };
        columnTwoCardView.SetCardBackgroundColor(Color.LightGray);
        columnTwoCardView.StrokeColor = Color.DarkSlateBlue;
        ColorStateList rippleColorStateList2 = ColorStateList.ValueOf(Color.Transparent);
        columnTwoCardView.RippleColor = rippleColorStateList2;
        columnTwoCardView.Checkable = false;
        

        gridOfTwoColumns.AddView(columnTwoCardView);

        secondLinear.AddView(gridOfTwoColumns);

        scrollLinearLayout.AddView(secondLinear);
        


        // last fm section, so card view having linear layout of textview, 2 textedits and button
        lastFMMessageTextView = new MaterialTextView(ctx)
        {
            Text = "Last.fm Section",
            TextSize = 21f, 
            Gravity = GravityFlags.Center,
            
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent)
        };
        lastFMUnameTxtField = new TextInputEditText(ctx)
        {
            Hint = "Last.fm Username",
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent)
        };
        
        
        lastFMPwdTxtField = new TextInputEditText(ctx)
        {
            
            Hint = "Last.fm Password",
            Gravity = GravityFlags.Left,
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent)
        };
        
        var materialContext = new ContextThemeWrapper(ctx, Resource.Style.Theme_Material3_DayNight_NoActionBar);
        
        rememberMeSwitch = new MaterialSwitch(materialContext)
        {
            Text = "Remember Me",
            TextSize = 18f,
            Gravity = GravityFlags.Left,
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent)
        };
        
        rememberMeSwitch.CheckedChange += RememberMeSwitch_CheckedChange;


        var materialContextBtn = new ContextThemeWrapper(ctx, Resource.Style.Theme_Material3_DayNight_NoActionBar);
        
        lastFMSubmitButton = new MaterialButton(ctx)
        {
            Text = "Submit",
            CornerRadius = 22,
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                100)
            ,
            RippleColor = ColorStateList.ValueOf(Color.Red)
        };

        lastFMSubmitButton.SetBackgroundColor(Color.DarkRed);

        var lastFMLogo = new ImageView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(100, 100)
            {
                Gravity = GravityFlags.Center
            }
        };
        lastFMLogo.SetImageResource(Resource.Drawable.lastfm);
        ColorFilter lastFMIconColFil = new ColorFilter();
        lastFMIconColFil = new PorterDuffColorFilter(Color.Red, PorterDuff.Mode.SrcIn);
        lastFMLogo.SetColorFilter(lastFMIconColFil);

        var layParam = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent);
        layParam.SetMargins(0, AppUtil.DpToPx(10), 0, AppUtil.DpToPx(10));
        var lastFMCard = new MaterialCardView(ctx)
        {
            LayoutParameters = layParam,
            
            Radius = 12f,
            CardElevation = 4f,
        };
        

        

        var cardContextLinearLayout = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent)
        };

        cardContextLinearLayout.SetPadding(AppUtil.DpToPx(20), AppUtil.DpToPx(20), AppUtil.DpToPx(20)
            , 20);
        cardContextLinearLayout.AddView(lastFMLogo);
        cardContextLinearLayout.AddView(lastFMMessageTextView);
        cardContextLinearLayout.AddView(lastFMUnameTxtField);
        cardContextLinearLayout.AddView(lastFMPwdTxtField);
        cardContextLinearLayout.AddView(rememberMeSwitch);
        cardContextLinearLayout.AddView(lastFMSubmitButton);
        

        lastFMCard.StrokeColor = Color.DarkRed;
        lastFMCard.StrokeWidth = 2;
        
        lastFMCard.AddView(cardContextLinearLayout);

        scrollLinearLayout.AddView(lastFMCard);


        _folderRecycler = new RecyclerView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                0,
                1f)
        };
        _folderRecycler.SetLayoutManager(new LinearLayoutManager(ctx));

        MyViewModel.ReloadFolderPathsCommand.Execute(null);
        _adapter = new FolderAdapter(MyViewModel.FolderPaths);
        _folderRecycler.SetAdapter(_adapter);


        var musicFoldersCard = new MaterialCardView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent),
            Radius = 12f,
            CardElevation = 8f,
        };
        musicFoldersCard.SetCardBackgroundColor(Color.White);
        musicFoldersCard.StrokeColor = Color.DarkSlateBlue;
        musicFoldersCard.StrokeWidth = 2;
        ColorStateList rippleColorStateList3 = ColorStateList.ValueOf(Color.Transparent);
        musicFoldersCard.RippleColor = rippleColorStateList3;
        musicFoldersCard.Clickable = true;

        
        _addFolderButton = new MaterialButton(ctx) { Text = "Add Folder",
             CornerRadius = 22,
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                80)
            ,
            RippleColor = ColorStateList.ValueOf(Color.Transparent)
        };
        _addFolderButton.SetPadding(AppUtil.DpToPx(5), AppUtil.DpToPx(5), AppUtil.DpToPx(5), AppUtil.DpToPx(5));
        _addFolderButton.SetBackgroundColor(Color.DarkRed);

        _addFolderButton.Click += AddFolderButton_Click;

        _addFolderButton.SetBackgroundColor(Color.DarkSlateBlue);


        
        var musicCardLayoutParam = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent);
        musicCardLayoutParam.SetMargins(0, AppUtil.DpToPx(10), 0, AppUtil.DpToPx(10));
        var musicFoldersCardLinLayout = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = musicCardLayoutParam
        };
        musicFoldersCardLinLayout.SetPadding(AppUtil.DpToPx(10), AppUtil.DpToPx(10), AppUtil.DpToPx(10)
            , 10);


        var musicFoldersTitleText = new MaterialTextView(ctx)
        {
            Text = "Music Folders",
            TextSize = AppUtil.DpToPx(20),
            Gravity = GravityFlags.Center,
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };
        musicFoldersCardLinLayout.AddView(musicFoldersTitleText);

        musicFoldersCardLinLayout.AddView(_addFolderButton);


        musicFoldersCardLinLayout.AddView(_folderRecycler);

        musicFoldersCard.AddView(musicFoldersCardLinLayout);


        var utilitiesCard = new MaterialCardView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent),
            Radius = 12f,
            CardElevation = 8f,
        };
        var gridOfTwoColumnsUtil = new GridLayout(ctx)
        {
            ColumnCount = 2,
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent)
        };
        var colOneTextUtil = new TextView(ctx)
        {
            Text = "Reload All Album Covers",
            TextSize = 18f,
            Gravity = GravityFlags.Left,
            LayoutParameters = new GridLayout.LayoutParams()
            {
                Width = ViewGroup.LayoutParams.WrapContent,
                Height = ViewGroup.LayoutParams.WrapContent,
                ColumnSpec = GridLayout.InvokeSpec(0),
                RowSpec = GridLayout.InvokeSpec(0)
            }
        };
        var colTwoButtonUtil = new MaterialButton(ctx)
        {
            Text = "Reload",
            Gravity = GravityFlags.Right,
            LayoutParameters = new GridLayout.LayoutParams()
            {
                Width = ViewGroup.LayoutParams.WrapContent,
                Height = ViewGroup.LayoutParams.WrapContent,
                ColumnSpec = GridLayout.InvokeSpec(1),
                RowSpec = GridLayout.InvokeSpec(0)
            }
        };
        colTwoButtonUtil.Click += ColTwoButtonUtil_Click;
        gridOfTwoColumnsUtil.AddView(colOneTextUtil);
        gridOfTwoColumnsUtil.AddView(colTwoButtonUtil);
        utilitiesCard.AddView(gridOfTwoColumnsUtil);
        scrollLinearLayout.AddView(utilitiesCard);

        scrollLinearLayout.AddView(musicFoldersCard);
        
        pageScrollViewer.AddView(scrollLinearLayout);
        root.AddView(pageScrollViewer);
        return root;
    }

    private async void ColTwoButtonUtil_Click(object? sender, EventArgs e)
    {
      await MyViewModel.EnsureAllCoverArtCachedForSongsCommand.ExecuteAsync(null);
    }

    private void BackMaterialChip_Click(object? sender, EventArgs e)
    {

        if (Activity is TransitionActivity mainActivity)
        {
            mainActivity.OnBackPressedDispatcher.OnBackPressed();
        }
    }

    private void RememberMeSwitch_CheckedChange(object? sender, CompoundButton.CheckedChangeEventArgs e)
    {
        Toast.MakeText(Context!, $"Remember Me Switch is now {(e.IsChecked ? "ON" : "OFF")}", ToastLength.Short)?.Show();
    }

    private async void AddFolderButton_Click(object? sender, EventArgs e)
    {
        
        await MyViewModel.AddMusicFolderViaPickerAsync();
    }

    // Simple string adapter
    class FolderAdapter : RecyclerView.Adapter
    {
        private readonly IList<string> _items;

        public FolderAdapter(IList<string> items)
        {
            _items = items;
        }

        public override int ItemCount => _items.Count;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var context = parent.Context!;
            // 1. The Container (Grid)
            var rootLayout = new LinearLayout(parent.Context!)
            {
                Orientation = Orientation.Horizontal,
                LayoutParameters = new RecyclerView.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent),
                WeightSum = 1f // Setup for weights
            };
            // 2. TEXT VIEW: Width = 0, Weight = 1
            // This tells the text to "fill all remaining space" but leave room for buttons
            var txtParams = new LinearLayout.LayoutParams(
                0, // Width must be 0 for weight to work
                ViewGroup.LayoutParams.WrapContent,
                1f // Weight 1
            );
            txtParams.Gravity = GravityFlags.CenterVertical; // Center text vertically

            var txt = new MaterialTextView(context)
            {
                LayoutParameters = txtParams,
                TextSize = 16f,
            };
            // Adjust padding to your liking
            txt.SetPadding(20, 20, 20, 20);

            // 3. BUTTONS CONTAINER: Wrap Content
            var buttonsLayout = new LinearLayout(context)
            {
                Orientation = Orientation.Horizontal,
                LayoutParameters = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.WrapContent,
                    ViewGroup.LayoutParams.WrapContent)
            };
            // Optional: Add gravity to center buttons vertically
            buttonsLayout.SetGravity(GravityFlags.CenterVertical);

            // Edit Button
            var materialEditBtn = new MaterialButton(context)
            {
                Text = "Edit",
                LayoutParameters = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.WrapContent,
                    ViewGroup.LayoutParams.WrapContent)
            };

            // Delete Button
            var materialDeleteBtn = new MaterialButton(context)
            {
                Text = "Delete",
                LayoutParameters = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.WrapContent,
                    ViewGroup.LayoutParams.WrapContent)
            };

            // Add some margin between buttons if you want
            ((LinearLayout.LayoutParams)materialDeleteBtn.LayoutParameters).LeftMargin = 10;

            buttonsLayout.AddView(materialEditBtn);
            buttonsLayout.AddView(materialDeleteBtn);

            // 4. Add everything to Root
            rootLayout.AddView(txt);
            rootLayout.AddView(buttonsLayout);

            return new SimpleVH(rootLayout);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (holder is SimpleVH vh)
            {
                vh.Label.Text = _items[position];
            }
        }

        class SimpleVH : RecyclerView.ViewHolder
        {
            public MaterialTextView Label { get; }
            public SimpleVH(View itemView) : base(itemView)
            {
                // Since we know the structure, we can find the views here once.
                // The Grid is 'itemView'. Child 0 is the Text.
                if (itemView is ViewGroup vg)
                {
                    Label = (MaterialTextView)vg.GetChildAt(0)!;
                }
            }
        }
    }

    public void OnBackInvoked()
    {
        Toast.MakeText(Context!, "Back invoked in Settings Fragment", ToastLength.Short)?.Show();
    }

}