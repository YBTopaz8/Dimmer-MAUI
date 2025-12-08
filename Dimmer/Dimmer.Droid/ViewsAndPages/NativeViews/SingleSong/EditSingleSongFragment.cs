using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AndroidX.CoordinatorLayout.Widget;
using AndroidX.Core.Widget;

using Bumptech.Glide;

using Dimmer.ViewsAndPages.NativeViews.Misc;
using Dimmer.WinUI.UiUtils;

using Google.Android.Material.AppBar;
using Google.Android.Material.Button;
using Google.Android.Material.Card;
using Google.Android.Material.Carousel;
using Google.Android.Material.Chip;
using Google.Android.Material.TextField;
using Google.Android.Material.TextView;

using Microsoft.Maui.Controls.PlatformConfiguration;

using static System.TimeZoneInfo;

using ScrollView = Android.Widget.ScrollView;

namespace Dimmer.ViewsAndPages.NativeViews.SingleSong;

public partial class EditSingleSongFragment :Fragment
{
    private string transitionName;
    private string TransName2;
    BaseViewModelAnd MyViewModel { get; set; }
    ImageView SongImage { get; set; }
    TextInputEditText SongTitleInput { get; set; }
    TextInputEditText SongTrackNumberInput { get; set; }
    TextInputEditText SongYearsInput { get; set; }
    TextInputEditText SongConductorInput { get; set; }
    TextInputEditText SongComposerInput { get; set; }
    TextInputEditText SongDescriptionInput { get; set; }

    MaterialButton SaveButton { get; set; }
    MaterialButton CancelButton { get; set; }
    private const int PICK_IMAGE_REQUEST = 1001;

    public EditSingleSongFragment(BaseViewModelAnd vm, string transName1, string transName2)
    {
        MyViewModel = vm;
        this.transitionName = transName1;
        this.TransName2 = transName2;
    }

    public EditSingleSongFragment() { }

    // private EditSongViewModel _viewModel; 

    // UI References for binding
    private ImageView _coverImage;
    private TextInputLayout _titleInput, _trackInput, _yearInput, _conductorInput, _composerInput, _descInput;
    private ChipGroup _artistChipGroup;
    private LinearLayout _notesContainer;
    private MaterialButton _saveButton;

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var context = Context;

        // 1. Root Coordinator Layout (Standard for MD3 Screens)
        var root = new CoordinatorLayout(context);

        // 2. AppBar & Toolbar
        var appBar = new AppBarLayout(context);
        var toolbar = new MaterialToolbar(context);
        toolbar.Title = "Edit Song";
        toolbar.SetNavigationIcon(Resource.Drawable.ic_arrow_back_black_24); // Ensure you have a drawable
        toolbar.NavigationClick += (s, e) => ParentFragmentManager.PopBackStack();

        // Add Save Button to Toolbar menu logic would go here, 
        // but for C# views, we can just add a button to the right or use Menu logic.
        // Let's keep the "Save" button at the bottom or floating for better reachability.

        appBar.AddView(toolbar);
        root.AddView(appBar, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

        // 3. Main Scroll Content
        var scrollView = new NestedScrollView(context);
        var scrollingParams = new CoordinatorLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
        scrollingParams.Behavior = new AppBarLayout.ScrollingViewBehavior();
        scrollView.LayoutParameters = scrollingParams;

        var mainStack = new LinearLayout(context) { Orientation = Orientation.Vertical };
        mainStack.SetPadding(0, 16, 0, 150); // Bottom padding for FAB/Bottom buttons

        // --- SECTION 1: CORE EDITS ---
        var coreContent = new LinearLayout(context) { Orientation = Orientation.Vertical };

        // Image
        _coverImage = new Google.Android.Material.ImageView.ShapeableImageView(context);
        _coverImage.LayoutParameters = new LinearLayout.LayoutParams(400, 400) { Gravity = GravityFlags.CenterHorizontal };
        _coverImage.SetBackgroundColor(Android.Graphics.Color.LightGray); // Placeholder
        _coverImage.Click += OnCoverImageClicked;
        coreContent.AddView(_coverImage);

        var editImgBtn = UiBuilder.CreateButton(context, "Edit Image", OnCoverImageClicked, true);
        coreContent.AddView(editImgBtn);

        // Fields
        _titleInput = UiBuilder.CreateInput(context, "Title", "My Song Title");
        _trackInput = UiBuilder.CreateInput(context, "Track #", "1");
        _yearInput = UiBuilder.CreateInput(context, "Year", "2023");
        _conductorInput = UiBuilder.CreateInput(context, "Conductor", "");
        _composerInput = UiBuilder.CreateInput(context, "Composer", "");
        _descInput = UiBuilder.CreateInput(context, "Description", "", true);

        var instrumentalSwitch = new Google.Android.Material.MaterialSwitch.MaterialSwitch(context);
        instrumentalSwitch.Text = "Is Instrumental";

        coreContent.AddView(_titleInput);
        coreContent.AddView(_trackInput);
        coreContent.AddView(_yearInput);
        coreContent.AddView(_conductorInput);
        coreContent.AddView(_composerInput);
        coreContent.AddView(_descInput);
        coreContent.AddView(instrumentalSwitch);

        mainStack.AddView(UiBuilder.CreateSectionCard(context, "Core Details", coreContent));


        // --- SECTION 2: ARTISTS ---
        var artistContent = new LinearLayout(context) { Orientation = Orientation.Vertical };

        _artistChipGroup = new ChipGroup(context);
        // Populate initial artists
        AddArtistChip(context, "Adele");
        AddArtistChip(context, "Featured Artist");

        var addArtistBtn = UiBuilder.CreateButton(context, "Add Artist", (s, e) => {
            var bottomSheet = new ArtistPickerBottomSheet();
            bottomSheet.Show(ChildFragmentManager, "ArtistPicker");
        }, true);

        artistContent.AddView(_artistChipGroup);
        artistContent.AddView(addArtistBtn);
        mainStack.AddView(UiBuilder.CreateSectionCard(context, "Artists", artistContent));


        // --- SECTION 3: ALBUM & GENRE ---
        var metaContent = new LinearLayout(context) { Orientation = Orientation.Vertical };

        // Album
        var albumRow = new LinearLayout(context) { Orientation = Orientation.Horizontal };
        var albumTxt = new TextView(context) { Text = "Album: 21", TextSize = 18 };
        var updateAlbumBtn = UiBuilder.CreateButton(context, "Update", (s, e) => { /* Logic */ }, true);
        albumRow.AddView(albumTxt, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f));
        albumRow.AddView(updateAlbumBtn);

        // Genre
        var genreRow = new LinearLayout(context) { Orientation = Orientation.Horizontal };
        var genreTxt = new TextView(context) { Text = "Genre: Pop", TextSize = 18 };
        var updateGenreBtn = UiBuilder.CreateButton(context, "Update", (s, e) => { /* Logic */ }, true);
        genreRow.AddView(genreTxt, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f));
        genreRow.AddView(updateGenreBtn);

        metaContent.AddView(albumRow);
        metaContent.AddView(genreRow);
        mainStack.AddView(UiBuilder.CreateSectionCard(context, "Metadata", metaContent));

        // --- SECTION 4: NOTES ---
        var notesContent = new LinearLayout(context) { Orientation = Orientation.Vertical };
        var addNoteBtn = UiBuilder.CreateButton(context, "Add Note", OnAddNoteClicked, true);
        _notesContainer = new LinearLayout(context) { Orientation = Orientation.Vertical };

        notesContent.AddView(addNoteBtn);
        notesContent.AddView(_notesContainer);
        mainStack.AddView(UiBuilder.CreateSectionCard(context, "Notes", notesContent));


        // --- SECTION 5: LYRICS LINK ---
        var lyricsBtn = UiBuilder.CreateButton(context, "Edit Lyrics >", (s, e) => {
            ParentFragmentManager.BeginTransaction()
                .Replace(Id, new LyricsEditorFragment()) 
                .AddToBackStack(null)
                .Commit();
        });
        mainStack.AddView(lyricsBtn);


        scrollView.AddView(mainStack);
        root.AddView(scrollView);

        // Floating Save Button (or fixed bottom bar)
        _saveButton = new MaterialButton(context);
        _saveButton.Text = "Save Changes";
        var fabParams = new CoordinatorLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
        fabParams.Gravity = (int)GravityFlags.Bottom;
        fabParams.SetMargins(32, 32, 32, 32);
        _saveButton.LayoutParameters = fabParams;
        _saveButton.Click += SaveChanges;

        root.AddView(_saveButton);

        return root;
    }

    private void OnCoverImageClicked(object sender, EventArgs e)
    {
        // Show Popup Menu similar to Flyout
        var popup = new PopupMenu(Context, (View)sender);
        popup.Menu.Add("Choose From File System");
        popup.Menu.Add("Choose From Other Songs");
        popup.Menu.Add("Remove Image");
        popup.Show();
    }
    public class OnClickHelper : Java.Lang.Object, View.IOnClickListener
    {
        private readonly Action<View> _action;
        public OnClickHelper(Action<View> action) => _action = action;
        public void OnClick(View? v) => _action?.Invoke(v);
    }
    private void AddArtistChip(Context ctx, string name)
    {
        var chip = new Chip(ctx);
        chip.Text = name;
        chip.CloseIconVisible = true;
        chip.Clickable = true;
        //chip.CloseIconClicked += (s, e) => _artistChipGroup.RemoveView(chip);
        _artistChipGroup.AddView(chip);
    }

    private void OnAddNoteClicked(object sender, EventArgs e)
    {
        // MD3 Alert Dialog
        var input = new EditText(Context);
        new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(Context)
            .SetTitle("Add Note")
            .SetView(input)
            .SetPositiveButton("Save", (s, args) => {
                var noteTxt = new TextView(Context) { Text = "• " + input.Text, TextSize = 16 };
                noteTxt.SetPadding(8, 8, 8, 8);
                _notesContainer.AddView(noteTxt);
            })
            .SetNegativeButton("Cancel", (s, args) => { })
            .Show();
    }

    private void SaveChanges(object sender, EventArgs e)
    {
        // Invoke ViewModel Update
        Toast.MakeText(Context, "Changes Saved!", ToastLength.Short).Show();
    }
}

    /*
    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context;
        if (ctx == null) return null;

        var mainScrollView = new ScrollView(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent),
            FillViewport = true
        };

        var root = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        };
        root.SetPadding(AppUtil.DpToPx(10), AppUtil.DpToPx(10), AppUtil.DpToPx(10), AppUtil.DpToPx(10));

        // 1. Top Section (Image + Inputs)
        root.AddView(CreateTopSection(ctx));

        // 2. Middle Section (Album Picker)
        root.AddView(CreateMiddleSection(ctx));

        // 3. Bottom Section (Notes + Actions)
        root.AddView(CreateBottomSection(ctx));

        mainScrollView.AddView(root);
        return mainScrollView;
    }

    private View CreateTopSection(Context ctx)
    {
        var card = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(16),
            CardElevation = AppUtil.DpToPx(4),
            UseCompatPadding = true
        };

        var layout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        layout.SetPadding(AppUtil.DpToPx(16), AppUtil.DpToPx(16), AppUtil.DpToPx(16), AppUtil.DpToPx(16));

        // Header Title
        var titleText = new MaterialTextView(ctx) { Text = "Edit Details", TextSize = 24 };
        titleText.TransitionName = transitionName;
        layout.AddView(titleText);

        // Image Row
        var imageRow = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        SongImage = new ImageView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(AppUtil.DpToPx(100), AppUtil.DpToPx(100)),
            TransitionName = TransName2
        };
        SongImage.SetScaleType(ImageView.ScaleType.CenterCrop);

        if (!string.IsNullOrEmpty(MyViewModel.SelectedSong.CoverImagePath))
            Glide.With(ctx).Load(MyViewModel.SelectedSong.CoverImagePath).Into(SongImage);
        else
            SongImage.SetImageResource(Resource.Drawable.musicnotess); // Ensure you have this drawable

        var changeImgBtn = new MaterialButton(ctx) { Text = "Change Cover" };
        changeImgBtn.Click += EditImageView_Click;

        imageRow.AddView(SongImage);
        imageRow.AddView(changeImgBtn);
        layout.AddView(imageRow);

        // Inputs
        SongTitleInput = CreateInput(ctx, "Title", MyViewModel.SelectedSong.Title);
        SongTrackNumberInput = CreateInput(ctx, "Track #", MyViewModel.SelectedSong.TrackNumber?.ToString());
        SongYearsInput = CreateInput(ctx, "Year", MyViewModel.SelectedSong.ReleaseYear?.ToString());
        SongConductorInput = CreateInput(ctx, "Conductor", MyViewModel.SelectedSong.Conductor);
        SongComposerInput = CreateInput(ctx, "Composer", MyViewModel.SelectedSong.Composer);
        SongDescriptionInput = CreateInput(ctx, "Description", MyViewModel.SelectedSong.Description);

        layout.AddView(SongTitleInput);
        layout.AddView(SongTrackNumberInput);
        layout.AddView(SongYearsInput);
        layout.AddView(SongConductorInput);
        layout.AddView(SongComposerInput);
        layout.AddView(SongDescriptionInput);

        card.AddView(layout);
        return card;
    }

    private TextInputEditText CreateInput(Context ctx, string hint, string value)
    {
        var input = new TextInputEditText(ctx) { Hint = hint, Text = value };
        input.Background = null; // Remove underline for cleaner look if desired
        return input;
    }

    private View CreateMiddleSection(Context ctx)
    {
        var card = new MaterialCardView(ctx)
        {
            Radius = AppUtil.DpToPx(16),
            UseCompatPadding = true
        };
        var row = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        row.SetPadding(AppUtil.DpToPx(16), 0, AppUtil.DpToPx(16), 0);
        row.SetGravity(GravityFlags.CenterVertical);
        var label = new TextView(ctx) { Text = "Album: " };
        var albumBtn = new MaterialButton(ctx)
        {
            Text = MyViewModel.SelectedSong.AlbumName ?? "Select Album",
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
            { Weight = 1 }
        };
        albumBtn.Click += (s, e) =>
        {
            var albumNames = MyViewModel.SearchResults.Select(x => x.AlbumName).Distinct().ToList();
            new AlbumPickerDialogFragment(albumNames, MyViewModel.SelectedSong.AlbumName, MyViewModel)
                .Show(ParentFragmentManager, "albumPicker");
        };

        row.AddView(label);
        row.AddView(albumBtn);
        card.AddView(row);
        return card;
    }

    private View CreateBottomSection(Context ctx)
    {
        var layout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };

        SaveButton = new MaterialButton(ctx) { Text = "Save Changes" };
        SaveButton.SetBackgroundColor(Android.Graphics.Color.DarkSlateBlue);
        SaveButton.Click += SaveButton_Click;

        CancelButton = new MaterialButton(ctx) { Text = "Cancel" };
        CancelButton.SetBackgroundColor(Android.Graphics.Color.Gray);
        CancelButton.Click += (s, e) => ParentFragmentManager.PopBackStack();

        layout.AddView(SaveButton);
        layout.AddView(CancelButton);
        return layout;
    }

    private void EditImageView_Click(object sender, EventArgs e)
    {
        var intent = new Intent(Intent.ActionPick);
        intent.SetType("image/*");
        StartActivityForResult(intent, PICK_IMAGE_REQUEST);
    }

    public override void OnActivityResult(int requestCode, int resultCode, Intent data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        if (requestCode == PICK_IMAGE_REQUEST && resultCode == (int)Android.App.Result.Ok && data?.Data != null)
        {
            // In a real app, copy this URI to a local file accessible by the app
            var uri = data.Data;
            MyViewModel.SelectedSong.CoverImagePath = uri.ToString(); // Simplified
            SongImage.SetImageURI(uri);
        }
    }

    private void SaveButton_Click(object sender, EventArgs e)
    {
        // 1. Map Inputs to Model
        MyViewModel.SelectedSong.Title = SongTitleInput.Text;
        if (int.TryParse(SongTrackNumberInput.Text, out int track)) MyViewModel.SelectedSong.TrackNumber = track;
        if (int.TryParse(SongYearsInput.Text, out int year)) MyViewModel.SelectedSong.ReleaseYear = year;
        MyViewModel.SelectedSong.Conductor = SongConductorInput.Text;
        MyViewModel.SelectedSong.Composer = SongComposerInput.Text;
        MyViewModel.SelectedSong.Description = SongDescriptionInput.Text;

        // 2. Update DB
        MyViewModel.UpdateSongInDB(MyViewModel.SelectedSong);

        // 3. Return
        Toast.MakeText(Context, "Song Updated", ToastLength.Short).Show();
        ParentFragmentManager.PopBackStack();
    }

    public bool IsDark()
    {
        return (Resources?.Configuration?.UiMode & Android.Content.Res.UiMode.NightYes) != 0;
    }
}
    */