using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using Google.Android.Material.Button;
using Google.Android.Material.Chip;
using Google.Android.Material.Dialog;
using Google.Android.Material.TextField;
using Google.Android.Material.TextView;
using Dimmer.ViewModel;
using Dimmer.Data.ModelView;

namespace Dimmer.ViewsAndPages.NativeViews.SingleSong;

/// <summary>
/// Dialog for creating or editing song comments with public/private toggle and timestamp picker
/// </summary>
public class SongCommentDialogFragment : DialogFragment
{
    private readonly BaseViewModelAnd _viewModel;
    private readonly UserNoteModelView? _existingNote;
    private readonly bool _isEditMode;

    private TextInputEditText? _commentInput;
    private Chip? _publicChip;
    private Chip? _privateChip;
    private MaterialTextView? _timestampDisplay;
    private MaterialButton? _setTimestampBtn;
    private MaterialButton? _clearTimestampBtn;
    private int? _selectedTimestampMs;

    public SongCommentDialogFragment(BaseViewModelAnd viewModel, UserNoteModelView? existingNote = null)
    {
        _viewModel = viewModel;
        _existingNote = existingNote;
        _isEditMode = existingNote != null;
        _selectedTimestampMs = existingNote?.TimestampMs;
    }

    public override Dialog OnCreateDialog(Bundle? savedInstanceState)
    {
        var ctx = RequireContext();
        var builder = new MaterialAlertDialogBuilder(ctx);

        // Create main layout
        var mainLayout = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical
        };
        mainLayout.SetPadding(AppUtil.DpToPx(20), AppUtil.DpToPx(20), AppUtil.DpToPx(20), AppUtil.DpToPx(20));

        // Title
        var title = new MaterialTextView(ctx)
        {
            Text = _isEditMode ? "Edit Comment" : "New Comment",
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent
            )
        };
        title.SetTextSize(Android.Util.ComplexUnitType.Sp, 20);
        title.SetTextColor(Android.Graphics.Color.White);
        mainLayout.AddView(title);

        // Spacer
        mainLayout.AddView(new Space(ctx) { LayoutParameters = new LinearLayout.LayoutParams(-1, AppUtil.DpToPx(16)) });

        // Comment text input
        var textInputLayout = new TextInputLayout(ctx)
        {
            Hint = "Your comment...",
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent
            )
        };

        _commentInput = new TextInputEditText(ctx)
        {
            Text = _existingNote?.UserMessageText ?? string.Empty,
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent
            )
        };
        _commentInput.SetLines(3);
        _commentInput.SetMaxLines(5);
        textInputLayout.AddView(_commentInput);
        mainLayout.AddView(textInputLayout);

        // Spacer
        mainLayout.AddView(new Space(ctx) { LayoutParameters = new LinearLayout.LayoutParams(-1, AppUtil.DpToPx(16)) });

        // Visibility section
        var visibilityLabel = new MaterialTextView(ctx)
        {
            Text = "Visibility",
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent
            )
        };
        visibilityLabel.SetTextSize(Android.Util.ComplexUnitType.Sp, 14);
        mainLayout.AddView(visibilityLabel);

        // Visibility chip group
        var visibilityGroup = new ChipGroup(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent
            )
        };
        visibilityGroup.SingleSelection = true;

        _publicChip = new Chip(ctx)
        {
            Text = "Public",
            Checkable = true,
            Checked = _existingNote?.IsPublic ?? true
        };
        _publicChip.SetChipIconResource(Resource.Drawable.exo_icon_play);

        _privateChip = new Chip(ctx)
        {
            Text = "Private",
            Checkable = true,
            Checked = !(_existingNote?.IsPublic ?? true)
        };
        _privateChip.SetChipIconResource(Resource.Drawable.exo_ic_pause);

        visibilityGroup.AddView(_publicChip);
        visibilityGroup.AddView(_privateChip);
        mainLayout.AddView(visibilityGroup);

        // Spacer
        mainLayout.AddView(new Space(ctx) { LayoutParameters = new LinearLayout.LayoutParams(-1, AppUtil.DpToPx(16)) });

        // Timestamp section
        var timestampLabel = new MaterialTextView(ctx)
        {
            Text = "Timestamp (optional)",
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent
            )
        };
        timestampLabel.SetTextSize(Android.Util.ComplexUnitType.Sp, 14);
        mainLayout.AddView(timestampLabel);

        // Timestamp display and buttons
        var timestampRow = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent
            )
        };

        _timestampDisplay = new MaterialTextView(ctx)
        {
            Text = FormatTimestamp(_selectedTimestampMs),
            LayoutParameters = new LinearLayout.LayoutParams(
                0,
                ViewGroup.LayoutParams.WrapContent,
                1.0f
            )
        };
        _timestampDisplay.SetTextSize(Android.Util.ComplexUnitType.Sp, 16);
        _timestampDisplay.Gravity = GravityFlags.CenterVertical;
        timestampRow.AddView(_timestampDisplay);

        _setTimestampBtn = new MaterialButton(ctx)
        {
            Text = "Use Current",
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent
            )
        };
        _setTimestampBtn.Click += SetTimestampBtn_Click;
        timestampRow.AddView(_setTimestampBtn);

        _clearTimestampBtn = new MaterialButton(ctx)
        {
            Text = "Clear",
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent
            )
        };
        _clearTimestampBtn.Click += ClearTimestampBtn_Click;
        timestampRow.AddView(_clearTimestampBtn);

        mainLayout.AddView(timestampRow);

        // Set the dialog content
        builder.SetView(mainLayout);

        // Set dialog buttons
        builder.SetPositiveButton(_isEditMode ? "Update" : "Create", SaveComment);
        builder.SetNegativeButton("Cancel", (s, e) => Dismiss());

        return builder.Create();
    }

    private void SetTimestampBtn_Click(object? sender, EventArgs e)
    {
        try
        {
            // Get current playback position from audio service
            var currentPosition = _viewModel.AudioService.CurrentPosition;
            _selectedTimestampMs = (int)(currentPosition * 1000);
            
            if (_timestampDisplay != null)
            {
                _timestampDisplay.Text = FormatTimestamp(_selectedTimestampMs);
            }

            Toast.MakeText(Context, $"Set timestamp to {FormatTimestamp(_selectedTimestampMs)}", ToastLength.Short)?.Show();
        }
        catch (Exception ex)
        {
            Toast.MakeText(Context, "Failed to set timestamp", ToastLength.Short)?.Show();
        }
    }

    private void ClearTimestampBtn_Click(object? sender, EventArgs e)
    {
        _selectedTimestampMs = null;
        
        if (_timestampDisplay != null)
        {
            _timestampDisplay.Text = FormatTimestamp(null);
        }

        Toast.MakeText(Context, "Timestamp cleared", ToastLength.Short)?.Show();
    }

    private async void SaveComment(object? sender, DialogClickEventArgs e)
    {
        try
        {
            var commentText = _commentInput?.Text;
            if (string.IsNullOrWhiteSpace(commentText))
            {
                Toast.MakeText(Context, "Comment text cannot be empty", ToastLength.Short)?.Show();
                return;
            }

            var isPublic = _publicChip?.Checked ?? true;

            var note = _existingNote ?? new UserNoteModelView();
            note.UserMessageText = commentText;
            note.IsPublic = isPublic;
            note.TimestampMs = _selectedTimestampMs;
            note.ModifiedAt = DateTimeOffset.UtcNow;

            if (!_isEditMode)
            {
                note.CreatedAt = DateTimeOffset.UtcNow;
                note.Id = $"UNote_{Guid.NewGuid():N}";
            }

            // Save through ViewModel
            await _viewModel.UpdateSongNoteWithGivenNoteModelView(_viewModel.SelectedSong, note);

            Toast.MakeText(Context, _isEditMode ? "Comment updated" : "Comment created", ToastLength.Short)?.Show();
            Dismiss();
        }
        catch (Exception ex)
        {
            Toast.MakeText(Context, $"Failed to save comment: {ex.Message}", ToastLength.Long)?.Show();
        }
    }

    private string FormatTimestamp(int? timestampMs)
    {
        if (timestampMs == null)
            return "No timestamp";

        var ts = TimeSpan.FromMilliseconds(timestampMs.Value);
        return ts.Hours > 0
            ? ts.ToString(@"hh\:mm\:ss")
            : ts.ToString(@"mm\:ss");
    }

    public override void OnStart()
    {
        base.OnStart();
        
        // Make dialog wider
        Dialog?.Window?.SetLayout(
            (int)(Resources.DisplayMetrics.WidthPixels * 0.9),
            ViewGroup.LayoutParams.WrapContent
        );
    }
}
