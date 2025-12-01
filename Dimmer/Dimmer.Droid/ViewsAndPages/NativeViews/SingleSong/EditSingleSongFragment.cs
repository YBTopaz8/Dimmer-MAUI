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
    public EditSingleSongFragment()
    {
        
    }

    public EditSingleSongFragment(BaseViewModelAnd baseViewModelAnd, string transitionName1, string transName2)
    {
        MyViewModel = baseViewModelAnd;
        this.transitionName = transitionName1;
        this.TransName2 = transName2;
    }

    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        if (Context is null)
            return null;
        var ctx = Context;

        var mainScrollView = new ScrollView(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(
               ViewGroup.LayoutParams.MatchParent,
               ViewGroup.LayoutParams.MatchParent),
            FillViewport = true
        };

        var root = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent)
        };
        root.SetPadding(AppUtil.DpToPx(5), AppUtil.DpToPx(5), AppUtil.DpToPx(5), AppUtil.DpToPx(5));

        //verticalLinearLayout.AddView(TopPartOfPageRowOne(ctx));

        root.AddView(TopPartOfPageRowOne(ctx));


        
        #region Middle Part Two region
        var middleCard = new MaterialCardView(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent),
            Radius = 16f,
            CardElevation = 8f,
            UseCompatPadding = true,
            PreventCornerOverlap = true,
        };
        var middleCardLayout = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent),
        };
        middleCardLayout.SetPadding(16, 16, 16, 16);

        //i'll put a grid of two here, one 20% width with label "album" and other 80% width with album name in a button
        var gridLayout = new GridLayout(ctx)
        {
            ColumnCount = 2,
            RowCount = 1,
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent),
        };
        var albumLabelTwentyPercent = new TextView(ctx)
        {

            LayoutParameters = new ViewGroup.LayoutParams(AppUtil.DpToPx(60), ViewGroup.LayoutParams.WrapContent),
            Text = "Album:",
            TextSize = 16f,
            Gravity = GravityFlags.CenterVertical|GravityFlags.Left,
        };
        var albumNameEightyPercent = new MaterialButton(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(AppUtil.DpToPx(250), ViewGroup.LayoutParams.WrapContent),
            Text = MyViewModel.SelectedSong.AlbumName,
            Gravity = GravityFlags.CenterVertical | GravityFlags.CenterHorizontal,
        };
        albumNameEightyPercent.Click += AlbumNameEightyPercent_Click;

        gridLayout.AddView(albumLabelTwentyPercent);
        gridLayout.AddView(albumNameEightyPercent);
        middleCardLayout.AddView(gridLayout);

        middleCard.AddView(middleCardLayout);

        root.AddView(middleCard);
#endregion

        #region Bottom Region
        var bottomCard = new MaterialCardView(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent),
            Radius = 16f,
            CardElevation = 8f,
            UseCompatPadding = true,
            PreventCornerOverlap = true,
        };
        var bottomCardLayout = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent),
        };
        bottomCardLayout.SetPadding(16, 16, 16, 16);
        SaveButton = new MaterialButton(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(0, ViewGroup.LayoutParams.WrapContent)
            {
                Width = (int)(ctx.Resources.DisplayMetrics.WidthPixels * 0.45),
            },
            Text = "Save",
        };
        SaveButton.Click += SaveButton_Click;
        CancelButton = new MaterialButton(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(0, ViewGroup.LayoutParams.WrapContent)
            {
                Width = (int)(ctx.Resources.DisplayMetrics.WidthPixels * 0.45),
            },
            Text = "Cancel",
        };
        CancelButton.Click += (s, e) =>
        {

            ParentFragmentManager.PopBackStack();
        };

        var gridOfTwoRows = new GridLayout(ctx)
        {
            ColumnCount = 1,
            RowCount = 2,
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent),
        };
        var RowOneLayoutTwentyPercentOfGrid = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent),
        };
        var textViewInRowOne = new TextView(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent),
            Text = "Song Notes",
        };
        var buttonInRowOne = new MaterialButton(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent),
            Text = "Add Note",
        };
        RowOneLayoutTwentyPercentOfGrid.AddView(textViewInRowOne);
        RowOneLayoutTwentyPercentOfGrid.AddView(buttonInRowOne);

        var RowTwoLayoutEightyPercentOfGrid = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent),
        };
        var notesRecyclerView = new RecyclerView(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, 400),
        };
        var notesRecyclerViewAdapter = new SongNotesRecyclerViewAdapter(MyViewModel);
        notesRecyclerView.SetAdapter(notesRecyclerViewAdapter);

        RowTwoLayoutEightyPercentOfGrid.AddView(notesRecyclerView);
        gridOfTwoRows.AddView(RowOneLayoutTwentyPercentOfGrid);
        gridOfTwoRows.AddView(RowTwoLayoutEightyPercentOfGrid);



        bottomCardLayout.AddView(SaveButton);
        bottomCardLayout.AddView(CancelButton);
        bottomCard.AddView(bottomCardLayout);
        #endregion

        root.AddView(bottomCard);


        mainScrollView.AddView(root);
        return mainScrollView;
    }

    public View TopPartOfPageRowOne(Context ctx)
    {
        var topCard = new MaterialCardView(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent),
            Radius = 16f,
            CardElevation = 8f,
            UseCompatPadding = true,
            PreventCornerOverlap = true,
          
        };
        var topCardLayout = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent),
            
        };
        topCardLayout.SetPadding(16, 16, 16, 16);
        SongImage = new ImageView(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(300, 300),
          TransitionName = TransName2,
        };
        SongImage.SetScaleType(ImageView.ScaleType.CenterCrop);
        Glide.With(ctx).Load(MyViewModel.SelectedSong.CoverImagePath)
            .Placeholder(Resource.Drawable.musicnotess).Into(SongImage);
        var textInputsLayout = new LinearLayout(ctx)
            {
            Orientation = Orientation.Vertical,
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent),
        };
        SongTitleInput = new TextInputEditText(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent),
            Hint = "Song Title",
            Text = MyViewModel.SelectedSong.Title,
        };
        SongTitleInput.SetTextColor
            (IsDark() ? Color.White : Color.Black);
        SongTrackNumberInput = new TextInputEditText(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent),
            Hint = "Track Number",
            Text = MyViewModel.SelectedSong.TrackNumber.ToString(),
            
        };
        SongTrackNumberInput.SetTextColor
            (IsDark() ? Color.White : Color.Black);

        SongYearsInput = new TextInputEditText(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent),
            Hint = "Year",
            Text = MyViewModel.SelectedSong.ReleaseYear.ToString(),
        };
        SongYearsInput.SetTextColor
            (IsDark() ? Color.White : Color.Black);

        SongConductorInput = new TextInputEditText(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent),
            Hint = "Conductor",
            Text = MyViewModel.SelectedSong.Conductor,
        };
        SongConductorInput.SetTextColor
            (IsDark() ? Color.White : Color.Black);


        SongComposerInput = new TextInputEditText(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent),
            Hint = "Composer",
            Text = MyViewModel.SelectedSong.Composer,
        };
        SongComposerInput.SetTextColor
            (IsDark() ? Color.White : Color.Black);

        SongDescriptionInput = new TextInputEditText(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent),
            Hint = "Description",
            Text = MyViewModel.SelectedSong.Description,
        };
        SongDescriptionInput.SetTextColor
            (IsDark() ? Color.White : Color.Black);


        TextView titleText = new MaterialTextView(ctx);
        titleText.LayoutParameters = new ViewGroup.LayoutParams
            (ViewGroup.LayoutParams.MatchParent,
            AppUtil.DpToPx(60));
        titleText.Text = "Edit Song";
        titleText.TextSize = 28;
        
        titleText.TransitionName = transitionName;

        textInputsLayout.AddView(titleText);
        textInputsLayout.AddView(SongTitleInput);
        textInputsLayout.AddView(SongTrackNumberInput);
        textInputsLayout.AddView(SongYearsInput);
        textInputsLayout.AddView(SongConductorInput);
        textInputsLayout.AddView(SongComposerInput);
        textInputsLayout.AddView(SongDescriptionInput);
        topCardLayout.AddView(SongImage);
        topCardLayout.AddView(textInputsLayout);
        var editImageBtnView = new MaterialButton(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams
            (
             ViewGroup.LayoutParams.MatchParent,
             ViewGroup.LayoutParams.MatchParent
            ),
            Text = "Edit Image"
        };
        editImageBtnView.Click += EditImageView_Click;

        topCardLayout.AddView(editImageBtnView);

        topCard.AddView(topCardLayout);
        return topCard;
    }

    private void EditImageView_Click(object? sender, EventArgs e)
    {
        throw new NotImplementedException();
    }

    //public View MiddlePartOfPageRowTwo(Context ctx)
    //{
       
    //    //return middleCard;
    //}

    //public View BottomPartOfPageRowThree(Context ctx)
    //{
      
    //    return bottomCard;
    //}

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        MyViewModel.UpdateSongInDB(MyViewModel.SelectedSong);
        ParentFragmentManager.PopBackStack();
    }

    private void AlbumNameEightyPercent_Click(object? sender, EventArgs e)
    {
        var allAlbumNamesInDb = MyViewModel.SearchResults.Select(s => s.AlbumName).Distinct().ToList();
        var albumPickerDialog = new AlbumPickerDialogFragment(allAlbumNamesInDb, MyViewModel.SelectedSong.AlbumName,
            MyViewModel);
        albumPickerDialog.Show(ParentFragmentManager, "albumPicker");
    }
    public bool IsDark()
    {
        return (Resources?.Configuration?.UiMode & Android.Content.Res.UiMode.NightYes) != 0;
    }
}
