using Bumptech.Glide;

using Google.Android.Material.Dialog;




namespace Dimmer.ViewsAndPages.NativeViews;

public class SongDetailPage : Fragment
{
    private ImageView? _sharedImage;
    internal TextView? TitleText;
    internal TextView? ArtistText;
    internal TextView? AlbumText;
    public static string TAG = "ArtistDetailFrag";
    public static string ArtistId;
    private string _transitionName;
    FloatingActionButton fab;
    SongModelView selectedSong;
    BaseViewModelAnd MyViewModel;
    public SongDetailPage(string transitionName, BaseViewModelAnd vm)
    {
        this._transitionName = transitionName;
        MyViewModel = vm;
        if(vm.SelectedSong != null)
        {
            selectedSong = vm.SelectedSong;
        }
    }
    public SongDetailPage()
    {
        
    }


    public override View OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context!;
        if (MyViewModel.SelectedSong is null)
            return base.OnCreateView(inflater, container, savedInstanceState);

        var root = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent),
            
        };
        RxSchedulers.UI.Schedule(async () =>
        {
            if(MyViewModel.SelectedSecondDominantColor is not null)
            {

                Color bgColor = MyViewModel.SelectedSecondDominantColor.ToPlatform();
                
                    root.SetBackgroundColor(bgColor);
                double brightness = (0.299 * bgColor.R + 0.587 * bgColor.G + 0.114 * bgColor.B);

                Color textColor = brightness > 0.5 ? Color.Black : Color.White;
                Color secondaryColor = brightness > 0.5 ? Color.DarkGray : Color.LightGray;


            }
            else
            {
                root.SetBackgroundColor(IsDark() ? Color.DimGray : Color.LightGray);
            }
            //var aColor= domColor.DominantColor.ToPlatform();

        });
        // TOP IMAGE - 20% of screen height
        var displayMetrics = ctx.Resources.DisplayMetrics;
        int screenHeight = displayMetrics.HeightPixels;
        _sharedImage = new ImageView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(AppUtil.DpToPx(150),
            AppUtil.DpToPx(150)),
            TransitionName = _transitionName,
        };
        _sharedImage.SetScaleType(ImageView.ScaleType.CenterCrop);
        if (selectedSong == null)
        {
            var popUpDialog = new MaterialAlertDialogBuilder(Context)
                .SetTitle("Error")
                .SetMessage("No song selected. Returning to previous screen.")
                .SetPositiveButton("OK", (sender, args) =>
                {
                    // Dismiss dialog and navigate back
                    Activity?.OnBackPressed();
                })
                .Create();
            popUpDialog.Show();
            return base.OnCreateView(inflater,container,savedInstanceState);
        }
        if (!string.IsNullOrEmpty(selectedSong.CoverImagePath) && System.IO.File.Exists(selectedSong.CoverImagePath))
        {
            RxSchedulers.UI.Schedule(async () =>
            {
                //var getfilteredImg = await ImageFilterUtils.ApplyFilter(MyViewModel.SelectedSong.CoverImagePath, FilterType.Blur);
                //works!
                Glide.With(ctx).Load(MyViewModel.SelectedSong.CoverImagePath).Placeholder(Resource.Drawable.musicnotess).Into(_sharedImage);
            });
        }

        // Vertical stack - Title, Artist, Album (10% more)
        var infoStack = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        };
        var titleTxt = new TextView(ctx) { Text = selectedSong.Title, TextSize = 26f };
        titleTxt.SetTextColor(IsDark()? Color.White : Color.Black);

        var artistTxt = new MaterialTextView(ctx) { Text = selectedSong.ArtistName ?? "Unknown Artist", TextSize = 18f };

        artistTxt.SetTextColor(Color.LightGray);

        var albumAndYearLL = new LinearLayout(ctx)
            ;
        albumAndYearLL.Orientation = Android.Widget.Orientation.Vertical;
        var albumAndYearLP =new ViewGroup.LayoutParams
            (ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
            ;
        
        albumAndYearLL.LayoutParameters = albumAndYearLP;
        var albumTxt = new TextView(ctx) { Text = selectedSong.AlbumName ?? "Unknown Album", TextSize = 16f};
        albumTxt.SetTextColor(Color.Gray);
        var yearTxt = new TextView(ctx) { Text = selectedSong.ReleaseYear.ToString(), TextSize = 16f};
        albumTxt.SetTextColor(Color.Gray);
        var yearTxtLayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
            ;
        yearTxt.LayoutParameters = yearTxtLayoutParameters;

        albumAndYearLL.AddView(albumTxt);
        if(yearTxt.Text is not null)
            albumAndYearLL.AddView (yearTxt);

        //genre and file type section
        var genreAndFileFormatLL = new LinearLayout(ctx)
            ;
        genreAndFileFormatLL.Orientation = Android.Widget.Orientation.Vertical;
        var genreAndFileFormatLP = new ViewGroup.LayoutParams
            (ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
            ;

        genreAndFileFormatLL.LayoutParameters = genreAndFileFormatLP;
        var genreTxt = new TextView(ctx) { Text = selectedSong.GenreName ?? "Unknown Genre", TextSize = 16f};
        genreTxt.SetTextColor(Color.Gray);
        var formatTxt = new TextView(ctx) { Text = selectedSong.FileFormat.ToString(), TextSize = 16f};
        genreTxt.SetTextColor(Color.Gray);
        var formatTxtLayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
            ;
        formatTxt.LayoutParameters = formatTxtLayoutParameters;

        genreAndFileFormatLL.AddView(genreTxt);
        if(formatTxt.Text is not null)
            albumAndYearLL.AddView (formatTxt);
        
        infoStack.AddView(titleTxt);
        infoStack.AddView(artistTxt);
        infoStack.AddView(albumAndYearLL);
        infoStack.AddView(genreAndFileFormatLL);

        MaterialButton IsSongFavBtn = new MaterialButton(ctx);
        var songLYParams = new LinearLayout.LayoutParams(
            AppUtil.DpToPx(60), ViewGroup.LayoutParams.MatchParent)
            ;
        IsSongFavBtn.LayoutParameters = songLYParams;
        IsSongFavBtn.Checkable = true;
        IsSongFavBtn.Checked = MyViewModel.SelectedSong.IsFavorite;
        IsSongFavBtn.Click += IsSongFavBtn_Click;
        IsSongFavBtn.LongClickable = MyViewModel.SelectedSong.IsFavorite;
        IsSongFavBtn.LongClick += IsSongFavBtn_LongClick;
        IsSongFavBtn.SetBackgroundColor(
            MyViewModel.SelectedSong.IsFavorite ? Color.DarkSlateBlue : Color.Transparent);
        //IsSongFavBtn.SetIconResource(MyViewModel.SelectedSong.IsFavorite
        //    ? Resource.Drawable.heartlock : Resource.Drawable.heartbroken);
        IsSongFavBtn.Text = MyViewModel.SelectedSong.NumberOfTimesFaved > 0 ? MyViewModel.SelectedSong.NumberOfTimesFaved.ToString() : string.Empty;
        infoStack.AddView(IsSongFavBtn);

        MaterialButton editBtn = new MaterialButton(ctx);
        var editBtnLYParams = new LinearLayout.LayoutParams(
            AppUtil.DpToPx(180),AppUtil.DpToPx(60));
        editBtn.LayoutParameters = editBtnLYParams;
        editBtn.Text = "Edit Song";
        editBtn.TextSize = 20;
        editBtn.StrokeWidth = 2;
        
        editBtn.StrokeColor = AppUtil.AnyColorCSL(Color.DarkSlateBlue);
        var colorListBtxtColor = new ColorStateList(
           new int[][] {
                new int[] { } // default
           },
           new int[] {
                IsDark() ? Color.White : Color.Black
           }
       );
        editBtn.SetTextColor(colorListBtxtColor);
        editBtn.SetBackgroundColor(Color.Transparent);
        editBtn.TransitionName = "ToEditSongPage";
        editBtn.Click += EditBtn_Click;
        infoStack.AddView(editBtn);

        infoStack.SetPadding(AppUtil.DpToPx(10), AppUtil.DpToPx(10), 0, 0);
        var gridOfTwoColumns = new GridLayout(ctx)
        {
            ColumnCount = 2,
        };
        gridOfTwoColumns.Orientation = GridOrientation.Horizontal;


        gridOfTwoColumns.AddView(_sharedImage);
        gridOfTwoColumns.AddView(infoStack);
        
        root.AddView(gridOfTwoColumns);



        return root;
    }

    private void EditBtn_Click(object? sender, EventArgs e)
    {
        var send = (MaterialButton?)sender;
        MyViewModel.NavigateToEditSongPage(this, send.TransitionName,
            new List<Android.Views.View>(2) { send  });
    }

    private async void IsSongFavBtn_LongClick(object? sender, View.LongClickEventArgs e)
    {
        MaterialButton? IsSongFavBtn = (MaterialButton?)sender;
        if (IsSongFavBtn != null && MyViewModel.SelectedSong is not null)
        {
            await MyViewModel.RemoveSongFromFavorite(MyViewModel.SelectedSong);
            if(!MyViewModel.SelectedSong.IsFavorite)
            {
                IsSongFavBtn.SetBackgroundColor(Color.Transparent);
                IsSongFavBtn.SetIconResource(Resource.Drawable.heartbroken);
                IsSongFavBtn.Text = string.Empty;
            }

        }
    }

    private async void IsSongFavBtn_Click(object? sender, EventArgs e)
    {
        MaterialButton? send = (MaterialButton?)sender;
        if (send != null && MyViewModel.SelectedSong is not null)
        {
            await MyViewModel.AddFavoriteRatingToSong(MyViewModel.SelectedSong);
            if (MyViewModel.SelectedSong.IsFavorite)
            {
                send.SetIconResource(Resource.Drawable.heartlock);
                send.SetBackgroundColor(Color.DarkSlateBlue);
                
                send.Text = MyViewModel.SelectedSong.NumberOfTimesFaved.ToString();
            }
           
        }
    }
    public bool IsDark()
    {
        return (Resources?.Configuration?.UiMode & Android.Content.Res.UiMode.NightYes) != 0;
    }
}