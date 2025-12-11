using System.Linq;
using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using Bumptech.Glide;
using Google.Android.Material.Button;
using Google.Android.Material.Chip;
using Google.Android.Material.Dialog;
using Google.Android.Material.TextField;
using Google.Android.Material.TextView;
using Dimmer.ViewModel; // Adjust namespace

namespace Dimmer.ViewsAndPages.NativeViews.SingleSong;

public class EditSingleSongFragment : Fragment
{
    private readonly BaseViewModelAnd _viewModel;
    private readonly string _transitionName;
    private SongModelView _song;

    // UI References
    private ImageView _coverImage;
    private TextInputEditText _titleInput, _albumInput, _yearInput, _genreInput, _trackInput;
    private ChipGroup _artistChipGroup;
    private LinearLayout _notesContainer;
    private MaterialButton _saveBtn;

    public EditSingleSongFragment(BaseViewModelAnd vm, string transitionName)
    {
        _viewModel = vm;
        _transitionName = transitionName;
        _song = vm.SelectedSong; // Ensure this is set
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context;
        var scroll = new Android.Widget.ScrollView(ctx) { FillViewport = true };
        var root = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        root.SetPadding(40, 40, 40, 40);

        // --- 1. COVER IMAGE PICKER ---
        var imgContainer = new FrameLayout(ctx);
        imgContainer.LayoutParameters = new LinearLayout.LayoutParams(AppUtil.DpToPx(200), AppUtil.DpToPx(200)) { Gravity = GravityFlags.Center };

        _coverImage = new ImageView(ctx) {  TransitionName = _transitionName };
        _coverImage.SetScaleType(ImageView.ScaleType.CenterCrop);
        if (!string.IsNullOrEmpty(_song?.CoverImagePath)) Glide.With(this).Load(_song.CoverImagePath).Into(_coverImage);
        else _coverImage.SetImageResource(Resource.Drawable.musicnotess);

        var editIcon = new ImageView(ctx);
        editIcon.SetImageResource(Resource.Drawable.exo_ic_default_album_image); // Use your icon
        editIcon.LayoutParameters = new FrameLayout.LayoutParams(60, 60) { Gravity = GravityFlags.Bottom | GravityFlags.Right };

        imgContainer.AddView(_coverImage);
        imgContainer.AddView(editIcon);
        imgContainer.Click += async (s, e) =>
        {
            ShowImageSourceOptions(ctx);
        }; // VM Call

        root.AddView(imgContainer);

        // --- 2. CORE METADATA INPUTS ---
        root.AddView(CreateSectionTitle(ctx, "Metadata"));

        _titleInput = CreateInput(ctx, "Title", _song?.Title, root);
        _albumInput = CreateInput(ctx, "Album", _song?.AlbumName, root);
        _artistChipGroup = CreateArtistEditor(ctx, root); // Special Artist Handler

        var row1 = new LinearLayout(ctx) { Orientation = Orientation.Horizontal, WeightSum = 2 };
        _yearInput = CreateInput(ctx, "Year", _song?.ReleaseYear?.ToString(), row1, 1);
        _genreInput = CreateInput(ctx, "Genre", _song?.GenreName, row1, 1);
        root.AddView(row1);

        _trackInput = CreateInput(ctx, "Track #", _song?.TrackNumber?.ToString(), root);

        // --- 3. NOTES ---
        root.AddView(CreateSectionTitle(ctx, "User Notes"));
        var addNoteBtn = new MaterialButton(ctx, null, Resource.Attribute.borderlessButtonStyle) { Text = "+ Add Note" };
        addNoteBtn.Click += ShowAddNoteDialog;
        root.AddView(addNoteBtn);

        _notesContainer = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        LoadNotes(ctx); // Populate existing
        root.AddView(_notesContainer);

        // --- 4. ACTIONS ---
        var space = new Space(ctx) { LayoutParameters = new LinearLayout.LayoutParams(-1, 60) };
        root.AddView(space);

        _saveBtn = new MaterialButton(ctx) { Text = "Save Changes" };
        _saveBtn.SetBackgroundColor(Android.Graphics.Color.DarkSlateBlue);
        _saveBtn.Click += SaveChanges;
        root.AddView(_saveBtn);

        scroll.AddView(root);
        return scroll;
    }

    // Add Helper Method
    private void ShowImageSourceOptions(Context ctx)
    {
        var bottomSheet = new Google.Android.Material.BottomSheet.BottomSheetDialog(ctx);
        var sheetView = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        sheetView.SetPadding(40, 40, 40, 40);
        // Option 1: File System
        var btnFile = new MaterialButton(ctx) { Text = "Pick from Device Storage" };
        btnFile.Click += async (s, e) => {
            bottomSheet.Dismiss();
            await _viewModel.PickAndApplyImageToSong(_song);
        };
        sheetView.AddView(btnFile);

        // Option 2: From Album Peers (Parity Feature)
        var btnAlbum = new MaterialButton(ctx) { Text = "Pick from Album Peers" };
        btnAlbum.Click += (s, e) => {
            bottomSheet.Dismiss();
            ShowAlbumPeerPicker(ctx);
        };
        sheetView.AddView(btnAlbum);

        bottomSheet.SetContentView(sheetView);
        bottomSheet.Show();
    }

    private void ShowAlbumPeerPicker(Context ctx)
    {
        // Fetch songs in same album
        var realm = _viewModel.RealmFactory.GetRealmInstance();
        var albumName = _song.AlbumName;
        var peers = realm.All<SongModel>().Where(s => s.AlbumName == albumName && !string.IsNullOrEmpty(s.CoverImagePath)).ToList();

        // Show a Grid Dialog
        var dialog = new Google.Android.Material.BottomSheet.BottomSheetDialog(ctx);
        var grid = new GridLayout(ctx) { ColumnCount = 3 };
        grid.SetPadding(20, 20, 20, 20);
        foreach (var peer in peers.DistinctBy(p => p.CoverImagePath))
        {
            var img = new ImageView(ctx) { LayoutParameters = new ViewGroup.LayoutParams(200, 200) };
            img.SetScaleType(ImageView.ScaleType.CenterCrop);
            ((ViewGroup.MarginLayoutParams)img.LayoutParameters).SetMargins(10, 10, 10, 10);

            Glide.With(ctx).Load(peer.CoverImagePath).Into(img);
            img.Click += async (s, e) => {
                _song.CoverImagePath = peer.CoverImagePath;
                // Update UI
                Glide.With(this).Load(_song.CoverImagePath).Into(_coverImage);
                dialog.Dismiss();
            };
            grid.AddView(img);
        }

        dialog.SetContentView(grid);
        dialog.Show();
    }

    // --- Helper UI Builders ---

    private TextInputEditText CreateInput(Context ctx, string hint, string val, ViewGroup parent, float weight = 0)
    {
        var layout = new TextInputLayout(ctx)
        {
            Hint = hint,
            LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, weight == 0 ? 1 : weight)
        };
        // If adding to vertical root, set width match_parent
        if (weight == 0) layout.LayoutParameters.Width = ViewGroup.LayoutParams.MatchParent;
        else ((LinearLayout.LayoutParams)layout.LayoutParameters).RightMargin = 10; // Spacing for rows

        var edit = new TextInputEditText(ctx) { Text = val };
        layout.AddView(edit);
        parent.AddView(layout);
        return edit;
    }

    private TextView CreateSectionTitle(Context ctx, string text)
    {
        var tv = new TextView(ctx) { Text = text, TextSize = 18, Typeface = Android.Graphics.Typeface.DefaultBold };
        tv.SetPadding(0, 40, 0, 20);
        return tv;
    }

    private ChipGroup CreateArtistEditor(Context ctx, ViewGroup parent)
    {
        var label = new TextView(ctx) { Text = "Artists", TextSize = 12 };
        parent.AddView(label);

        var group = new ChipGroup(ctx);
        // Load initial artists
        if (!string.IsNullOrEmpty(_song?.ArtistName))
        {
            var artists = _song.ArtistName.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var art in artists) AddArtistChip(ctx, group, art.Trim());
        }

        // Add Button Chip
        var addChip = new Chip(ctx) { Text = "+ Add" };
        addChip.Click += (s, e) => ShowAddArtistDialog(ctx, group);
        group.AddView(addChip);

        parent.AddView(group);
        return group;
    }

    private void AddArtistChip(Context ctx, ChipGroup group, string name)
    {
        var chip = new Chip(ctx) { Text = name, CloseIconVisible = true };
        chip.LongClick += (s, e) => group.RemoveView(chip);
        chip.LongClickable = true;
        // Insert before the "+ Add" button (last index)
        group.AddView(chip, group.ChildCount - 1);
    }

    // --- Dialogs ---

    private void ShowAddArtistDialog(Context ctx, ChipGroup group)
    {
        var input = new TextInputEditText(ctx) { Hint = "Artist Name" };
        new MaterialAlertDialogBuilder(ctx)
            .SetTitle("Add Artist")
            .SetView(input)
            .SetPositiveButton("Add", (s, e) => {
                if (!string.IsNullOrWhiteSpace(input.Text))
                    AddArtistChip(ctx, group, input.Text);
            })
            .Show();
    }

    private void ShowAddNoteDialog(object sender, EventArgs e)
    {
        var ctx = Context;
        var input = new TextInputEditText(ctx) { Hint = "Note content..." };
        new MaterialAlertDialogBuilder(ctx)
            .SetTitle("New Note")
            .SetView(input)
            .SetPositiveButton("Save", async (s, a) => {
                // Call VM to add note
                await _viewModel.UpdateSongNoteWithGivenNoteModelView(_song, new UserNoteModelView { UserMessageText = input.Text });
                LoadNotes(ctx); // Refresh UI
            })
            .Show();
    }

    private void LoadNotes(Context ctx)
    {
        _notesContainer.RemoveAllViews();
        // Assuming _song.UserNotes is the collection
        /* 
        foreach (var note in _song.UserNotes) 
        {
            var card = new MaterialCardView(ctx);
            // ... build simple card with note.Text ...
            _notesContainer.AddView(card);
        } 
        */
    }

    // --- Save Logic ---

    private async void SaveChanges(object sender, EventArgs e)
    {
        // 1. Gather Data
        _song.Title = _titleInput.Text;
        _song.AlbumName = _albumInput.Text;
        if (int.TryParse(_yearInput.Text, out int y)) _song.ReleaseYear = y;
        if (int.TryParse(_trackInput.Text, out int t)) _song.TrackNumber = t;
        _song.GenreName = _genreInput.Text;

        // 2. Reconstruct Artist String
        var artists = new List<string>();
        for (int i = 0; i < _artistChipGroup.ChildCount - 1; i++) // Skip last "+ Add" chip
        {
            if (_artistChipGroup.GetChildAt(i) is Chip c) artists.Add(c.Text);
        }
        // This is a simple string join. If your VM requires `ArtistToSong` objects, 
        // you'd map these strings to that collection here.
        _song.ArtistName = string.Join(", ", artists);

        // 3. Call VM
        await _viewModel.ApplyNewSongEdits(_song);

        // 4. Notify & Exit
        Toast.MakeText(Context, "Changes Saved!", ToastLength.Short)?.Show();
        ParentFragmentManager.PopBackStack();
    }
}