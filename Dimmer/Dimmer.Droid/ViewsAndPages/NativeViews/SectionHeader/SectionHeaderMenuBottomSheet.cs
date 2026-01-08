using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.Button;
using Dimmer.WinUI.UiUtils;

namespace Dimmer.ViewsAndPages.NativeViews.SectionHeader;

/// <summary>
/// BottomSheet dialog that shows actions for section headers.
/// </summary>
internal class SectionHeaderMenuBottomSheet : BottomSheetDialogFragment
{
    private readonly BaseViewModelAnd _viewModel;
    private readonly SectionHeaderModel _section;
    private readonly RecyclerView _recyclerView;
    private readonly SongAdapter _adapter;
    private readonly Action? _onExitShuffle;
    private readonly Action<string>? _onChangeSortMode;

    public SectionHeaderMenuBottomSheet(
        BaseViewModelAnd viewModel,
        SectionHeaderModel section,
        RecyclerView recyclerView,
        SongAdapter adapter,
        Action? onExitShuffle = null,
        Action<string>? onChangeSortMode = null)
    {
        _viewModel = viewModel;
        _section = section;
        _recyclerView = recyclerView;
        _adapter = adapter;
        _onExitShuffle = onExitShuffle;
        _onChangeSortMode = onChangeSortMode;
    }

    public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
    {
        var ctx = Context!;

        var root = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical
        };
        root.SetPadding(AppUtil.DpToPx(16), AppUtil.DpToPx(16), AppUtil.DpToPx(16), AppUtil.DpToPx(24));
        root.SetBackgroundColor(UiBuilder.ThemedBGColor(ctx));

        // Title
        var titleView = new TextView(ctx)
        {
            Text = $"Section: {_section.Title}",
            TextSize = 18,
            Typeface = Android.Graphics.Typeface.DefaultBold
        };
        titleView.SetPadding(0, 0, 0, AppUtil.DpToPx(16));
        root.AddView(titleView);

        // Scroll to Section button
        var scrollToSectionBtn = CreateActionButton(ctx, "Jump to Section Start", Resource.Drawable.arrowdown);
        scrollToSectionBtn.Click += (s, e) =>
        {
            // Scroll to the header position (flattened list includes headers)
            var flatPos = _adapter.GetFlatPositionForSongIndex(_section.SongStartIndex);
            if (flatPos >= 0)
                _recyclerView.SmoothScrollToPosition(flatPos);
            Dismiss();
        };
        root.AddView(scrollToSectionBtn);

        // Scroll to Top button
        var scrollToTopBtn = CreateActionButton(ctx, "Scroll to Top", Resource.Drawable.arrowtotopleft);
        scrollToTopBtn.Click += (s, e) =>
        {
            _recyclerView.SmoothScrollToPosition(0);
            Dismiss();
        };
        root.AddView(scrollToTopBtn);

        // Scroll to Current Playing Song button
        var scrollToPlayingBtn = CreateActionButton(ctx, "Scroll to Current Song", Resource.Drawable.musicaba);
        scrollToPlayingBtn.Click += (s, e) =>
        {
            if (_viewModel.CurrentPlayingSongView != null)
            {
                var index = _viewModel.SearchResults.IndexOf(_viewModel.CurrentPlayingSongView);
                if (index >= 0)
                {
                    var flatPos = _adapter.GetFlatPositionForSongIndex(index);
                    if (flatPos >= 0)
                        _recyclerView.SmoothScrollToPosition(flatPos);
                }
            }
            Dismiss();
        };
        root.AddView(scrollToPlayingBtn);

        // Change Sort Mode button
        if (_onChangeSortMode != null)
        {
            var changeSortBtn = CreateActionButton(ctx, "Change Sort Mode", Resource.Drawable.sorta);
            changeSortBtn.Click += (s, e) =>
            {
                ShowSortModeDialog();
            };
            root.AddView(changeSortBtn);
        }

        // Exit Shuffle button (only show in shuffle mode)
        if (_section.Type == SectionType.Shuffle && _onExitShuffle != null)
        {
            var exitShuffleBtn = CreateActionButton(ctx, "Exit Shuffle", Resource.Drawable.shuffle);
            exitShuffleBtn.Click += (s, e) =>
            {
                _onExitShuffle?.Invoke();
                Dismiss();
            };
            root.AddView(exitShuffleBtn);
        }

        return root;
    }

    private MaterialButton CreateActionButton(Context ctx, string text, int iconRes)
    {
        var btn = new MaterialButton(ctx, null, Resource.Attribute.materialButtonOutlinedStyle)
        {
            Text = text
        };
        btn.SetIconResource(iconRes);
        btn.IconGravity = MaterialButton.IconGravityStart;
        
        var lp = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
        lp.SetMargins(0, 0, 0, AppUtil.DpToPx(8));
        btn.LayoutParameters = lp;

        return btn;
    }

    private void ShowSortModeDialog()
    {
        var sortOptions = new[] 
        { 
            "Title (A-Z)", 
            "Title (Z-A)", 
            "Date Added (Newest)", 
            "Date Added (Oldest)",
            "Last Played (Recent)", 
            "Last Played (Oldest)"
        };

        var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(Context!);
        builder.SetTitle("Change Sort Mode");
        builder.SetItems(sortOptions, (sender, args) =>
        {
            var selectedSort = args.Which switch
            {
                0 => "sort:title asc",
                1 => "sort:title desc",
                2 => "sort:added desc",
                3 => "sort:added asc",
                4 => "sort:played desc",
                5 => "sort:played asc",
                _ => "sort:added desc"
            };

            _onChangeSortMode?.Invoke(selectedSort);
            Dismiss();
        });
        builder.Show();
    }
}
