using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Bumptech.Glide;

using Google.Android.Material.Button;
using Google.Android.Material.Card;
using Google.Android.Material.Carousel;
using Google.Android.Material.TextField;
using Google.Android.Material.TextView;

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

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
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
