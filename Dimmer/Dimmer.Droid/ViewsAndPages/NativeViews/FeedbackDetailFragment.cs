using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Dimmer.DimmerLive.Models;
using Dimmer.ViewModel.DimmerLiveVM;
using Google.Android.Material.Button;
using Google.Android.Material.Card;
using Google.Android.Material.TextField;

namespace Dimmer.ViewsAndPages.NativeViews;

public class FeedbackDetailFragment : Fragment
{
    private readonly BaseViewModelAnd _viewModel;
    private readonly string _issueId;
    private FeedbackDetailViewModel _feedbackViewModel;
    private TextView _titleText;
    private TextView _descriptionText;
    private TextView _metaText;
    private TextView _statusBadge;
    private TextView _typeBadge;
    private TextView _upvoteCountText;
    private MaterialButton _upvoteButton;
    private MaterialButton _deleteIssueButton;
    private TextInputEditText _commentEditText;
    private MaterialButton _postCommentButton;
    private RecyclerView _commentsRecyclerView;
    private CommentAdapter _commentAdapter;
    private ProgressBar _progressBar;
    private CheckBox _notifyStatusCheckBox;
    private CheckBox _notifyCommentCheckBox;
    private LinearLayout _notificationLayout;

    public FeedbackDetailFragment(BaseViewModelAnd viewModel, string issueId)
    {
        _viewModel = viewModel;
        _issueId = issueId;
    }

    public override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _feedbackViewModel = IPlatformApplication.Current!.Services.GetRequiredService<FeedbackDetailViewModel>();
        
        // Apply query attributes
        var queryParams = new Dictionary<string, object>
        {
            { "IssueId", _issueId }
        };
        _feedbackViewModel.ApplyQueryAttributes(queryParams);
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context!;
        
        var scrollView = new ScrollView(ctx)
        {
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, 
                ViewGroup.LayoutParams.MatchParent)
        };

        var root = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical
        };
        root.SetPadding(AppUtil.DpToPx(16), AppUtil.DpToPx(16), AppUtil.DpToPx(16), AppUtil.DpToPx(16));

        // Progress Bar
        _progressBar = new ProgressBar(ctx) { Indeterminate = true };
        root.AddView(_progressBar);

        // Issue Card
        var issueCard = new MaterialCardView(ctx)
        {
            CardElevation = AppUtil.DpToPx(12),
            Radius = AppUtil.DpToPx(8)
        };
        issueCard.SetCardBackgroundColor(Android.Graphics.Color.White);
        issueCard.SetPadding(AppUtil.DpToPx(20), AppUtil.DpToPx(20), AppUtil.DpToPx(20), AppUtil.DpToPx(20));

        var issueContent = new LinearLayout(ctx) { Orientation = Orientation.Vertical };

        // Title and badges
        var titleLayout = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        _titleText = new TextView(ctx) { TextSize = 24, Typeface = Android.Graphics.Typeface.DefaultBold };
        _titleText.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1);
        titleLayout.AddView(_titleText);

        var badgeLayout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        _statusBadge = new TextView(ctx) { TextSize = 12, Typeface = Android.Graphics.Typeface.DefaultBold };
        _statusBadge.SetPadding(AppUtil.DpToPx(10), AppUtil.DpToPx(5), AppUtil.DpToPx(10), AppUtil.DpToPx(5));
        _statusBadge.SetTextColor(Android.Graphics.Color.White);
        _typeBadge = new TextView(ctx) { TextSize = 11, Typeface = Android.Graphics.Typeface.DefaultBold };
        _typeBadge.SetPadding(AppUtil.DpToPx(8), AppUtil.DpToPx(4), AppUtil.DpToPx(8), AppUtil.DpToPx(4));
        _typeBadge.SetTextColor(Android.Graphics.Color.White);
        badgeLayout.AddView(_statusBadge);
        badgeLayout.AddView(_typeBadge);
        titleLayout.AddView(badgeLayout);

        issueContent.AddView(titleLayout);

        // Meta info
        _metaText = new TextView(ctx) { TextSize = 14 };
        _metaText.SetTextColor(Android.Graphics.Color.Gray);
        _metaText.SetPadding(0, AppUtil.DpToPx(10), 0, AppUtil.DpToPx(10));
        issueContent.AddView(_metaText);

        // Description
        _descriptionText = new TextView(ctx) { TextSize = 15 };
        _descriptionText.SetPadding(0, 0, 0, AppUtil.DpToPx(15));
        issueContent.AddView(_descriptionText);

        // Action buttons
        var actionLayout = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        _upvoteButton = new MaterialButton(ctx);
        _upvoteCountText = new TextView(ctx) { TextSize = 16, Typeface = Android.Graphics.Typeface.DefaultBold };
        _upvoteButton.Click += OnUpvoteClick;
        var upvoteLayout = new LinearLayout(ctx) { Orientation = Orientation.Horizontal };
        upvoteLayout.AddView(new TextView(ctx) { Text = "↑", TextSize = 18 });
        upvoteLayout.AddView(_upvoteCountText);
        _upvoteButton.AddView(upvoteLayout);
        actionLayout.AddView(_upvoteButton);

        _deleteIssueButton = new MaterialButton(ctx) { Text = "Delete", Visibility = ViewStates.Gone };
        _deleteIssueButton.Click += OnDeleteIssueClick;
        actionLayout.AddView(_deleteIssueButton);

        var githubButton = new MaterialButton(ctx) { Text = "View on GitHub" };
        githubButton.Click += OnOpenGitHubClick;
        actionLayout.AddView(githubButton);

        issueContent.AddView(actionLayout);

        // Notification Settings
        _notificationLayout = new LinearLayout(ctx) { Orientation = Orientation.Vertical, Visibility = ViewStates.Gone };
        _notificationLayout.SetPadding(0, AppUtil.DpToPx(15), 0, 0);
        var notifLabel = new TextView(ctx) { Text = "Notifications", TextSize = 14, Typeface = Android.Graphics.Typeface.DefaultBold };
        _notificationLayout.AddView(notifLabel);
        _notifyStatusCheckBox = new CheckBox(ctx) { Text = "Notify me when the status changes" };
        _notifyStatusCheckBox.CheckedChange += OnNotificationChanged;
        _notifyCommentCheckBox = new CheckBox(ctx) { Text = "Notify me about new comments" };
        _notifyCommentCheckBox.CheckedChange += OnNotificationChanged;
        _notificationLayout.AddView(_notifyStatusCheckBox);
        _notificationLayout.AddView(_notifyCommentCheckBox);
        issueContent.AddView(_notificationLayout);

        issueCard.AddView(issueContent);
        root.AddView(issueCard);

        // Comments Section
        var commentsHeader = new TextView(ctx)
        {
            Text = "Comments",
            TextSize = 22,
            Typeface = Android.Graphics.Typeface.DefaultBold
        };
        commentsHeader.SetPadding(0, AppUtil.DpToPx(20), 0, AppUtil.DpToPx(10));
        root.AddView(commentsHeader);

        // Add Comment (for authenticated users)
        var addCommentCard = new MaterialCardView(ctx)
        {
            CardElevation = AppUtil.DpToPx(12),
            Radius = AppUtil.DpToPx(8),
            Visibility = ViewStates.Gone
        };
        addCommentCard.SetCardBackgroundColor(Android.Graphics.Color.White);
        addCommentCard.SetPadding(AppUtil.DpToPx(15), AppUtil.DpToPx(15), AppUtil.DpToPx(15), AppUtil.DpToPx(15));

        var addCommentLayout = new LinearLayout(ctx) { Orientation = Orientation.Vertical };
        var commentInputLayout = new TextInputLayout(ctx) { Hint = "Add a comment..." };
        _commentEditText = new TextInputEditText(ctx);
        _commentEditText.SetMinHeight(AppUtil.DpToPx(100));
        commentInputLayout.AddView(_commentEditText);
        addCommentLayout.AddView(commentInputLayout);

        _postCommentButton = new MaterialButton(ctx) { Text = "Post Comment" };
        _postCommentButton.Click += OnPostCommentClick;
        addCommentLayout.AddView(_postCommentButton);

        addCommentCard.AddView(addCommentLayout);
        root.AddView(addCommentCard);

        // Comments RecyclerView
        _commentsRecyclerView = new RecyclerView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, 
                ViewGroup.LayoutParams.WrapContent)
        };
        _commentsRecyclerView.SetLayoutManager(new LinearLayoutManager(ctx));
        _commentAdapter = new CommentAdapter(new List<FeedbackComment>(), OnDeleteCommentClick);
        _commentsRecyclerView.SetAdapter(_commentAdapter);
        root.AddView(_commentsRecyclerView);

        scrollView.AddView(root);

        // Subscribe to ViewModel changes
        ObserveViewModel();

        return scrollView;
    }

    private void ObserveViewModel()
    {
        // Update UI when issue is loaded
        if (_feedbackViewModel.Issue != null)
        {
            UpdateUI();
        }

        // Check authentication state
        if (_feedbackViewModel.IsAuthenticated)
        {
            _notificationLayout.Visibility = ViewStates.Visible;
            var addCommentCard = (MaterialCardView)_commentsRecyclerView.Parent;
            // Find addCommentCard and make it visible
            var parent = (LinearLayout)_commentsRecyclerView.Parent;
            for (int i = 0; i < parent.ChildCount; i++)
            {
                if (parent.GetChildAt(i) is MaterialCardView card)
                {
                    card.Visibility = ViewStates.Visible;
                    break;
                }
            }
        }
    }

    private void UpdateUI()
    {
        var issue = _feedbackViewModel.Issue;
        if (issue == null) return;

        _progressBar.Visibility = ViewStates.Gone;
        _titleText.Text = issue.Title;
        _descriptionText.Text = issue.Description;
        _metaText.Text = $"{issue.AuthorUsername} • {issue.Platform} • v{issue.AppVersion}";
        _upvoteCountText.Text = issue.UpvoteCount.ToString();
        _statusBadge.Text = issue.Status;
        _typeBadge.Text = issue.Type;

        _statusBadge.SetBackgroundColor(GetStatusColor(issue.Status));
        _typeBadge.SetBackgroundColor(GetTypeColor(issue.Type));

        if (_feedbackViewModel.IsAuthor)
        {
            _deleteIssueButton.Visibility = ViewStates.Visible;
        }

        _notifyStatusCheckBox.Checked = _feedbackViewModel.NotifyOnStatusChange;
        _notifyCommentCheckBox.Checked = _feedbackViewModel.NotifyOnComment;

        _commentAdapter.UpdateComments(_feedbackViewModel.Comments.ToList());
    }

    private async void OnUpvoteClick(object? sender, EventArgs e)
    {
        await _feedbackViewModel.ToggleUpvoteCommand.ExecuteAsync(null);
        UpdateUI();
    }

    private async void OnDeleteIssueClick(object? sender, EventArgs e)
    {
        var dialog = new AndroidX.AppCompat.App.AlertDialog.Builder(Context!)
            .SetTitle("Delete Issue")
            .SetMessage("Are you sure you want to delete this issue? This action cannot be undone.")
            .SetPositiveButton("Delete", async (s, args) =>
            {
                await _feedbackViewModel.DeleteIssueCommand.ExecuteAsync(null);
                ParentFragmentManager.PopBackStack();
            })
            .SetNegativeButton("Cancel", (s, args) => { })
            .Create();
        dialog.Show();
    }

    private async void OnOpenGitHubClick(object? sender, EventArgs e)
    {
        await _feedbackViewModel.OpenGitHubIssueCommand.ExecuteAsync(null);
    }

    private async void OnPostCommentClick(object? sender, EventArgs e)
    {
        await _feedbackViewModel.AddCommentCommand.ExecuteAsync(null);
        _commentEditText.Text = string.Empty;
        UpdateUI();
    }

    private async void OnDeleteCommentClick(FeedbackComment comment)
    {
        var dialog = new AndroidX.AppCompat.App.AlertDialog.Builder(Context!)
            .SetTitle("Delete Comment")
            .SetMessage("Are you sure you want to delete this comment?")
            .SetPositiveButton("Delete", async (s, args) =>
            {
                await _feedbackViewModel.DeleteCommentCommand.ExecuteAsync(comment);
                UpdateUI();
            })
            .SetNegativeButton("Cancel", (s, args) => { })
            .Create();
        dialog.Show();
    }

    private async void OnNotificationChanged(object? sender, CompoundButton.CheckedChangeEventArgs e)
    {
        await _feedbackViewModel.UpdateNotificationPreferencesCommand.ExecuteAsync(null);
    }

    private Android.Graphics.Color GetStatusColor(string status)
    {
        return status.ToLower() switch
        {
            "open" => Android.Graphics.Color.Rgb(59, 130, 246),
            "planned" => Android.Graphics.Color.Rgb(168, 85, 247),
            "in-progress" => Android.Graphics.Color.Rgb(251, 146, 60),
            "shipped" => Android.Graphics.Color.Rgb(34, 197, 94),
            "rejected" => Android.Graphics.Color.Rgb(239, 68, 68),
            _ => Android.Graphics.Color.Gray
        };
    }

    private Android.Graphics.Color GetTypeColor(string type)
    {
        return type.ToLower() switch
        {
            "bug" => Android.Graphics.Color.Rgb(239, 68, 68),
            "feature" => Android.Graphics.Color.Rgb(34, 197, 94),
            _ => Android.Graphics.Color.Gray
        };
    }
}

// Comment Adapter
public class CommentAdapter : RecyclerView.Adapter
{
    private List<FeedbackComment> _comments;
    private readonly Action<FeedbackComment> _onDeleteClick;

    public CommentAdapter(List<FeedbackComment> comments, Action<FeedbackComment> onDeleteClick)
    {
        _comments = comments;
        _onDeleteClick = onDeleteClick;
    }

    public void UpdateComments(List<FeedbackComment> comments)
    {
        _comments = comments;
        NotifyDataSetChanged();
    }

    public override int ItemCount => _comments.Count;

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        if (holder is CommentViewHolder commentHolder)
        {
            commentHolder.Bind(_comments[position]);
        }
    }

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        return new CommentViewHolder(parent.Context!, _onDeleteClick);
    }

    private class CommentViewHolder : RecyclerView.ViewHolder
    {
        private readonly MaterialCardView _cardView;
        private readonly TextView _authorText;
        private readonly TextView _timeText;
        private readonly TextView _commentText;
        private readonly MaterialButton _deleteButton;
        private FeedbackComment? _comment;
        private readonly Action<FeedbackComment> _onDeleteClick;

        public CommentViewHolder(Android.Content.Context context, Action<FeedbackComment> onDeleteClick) 
            : base(CreateCardView(context))
        {
            _onDeleteClick = onDeleteClick;
            _cardView = (MaterialCardView)ItemView;
            
            var contentLayout = (LinearLayout)_cardView.GetChildAt(0);
            var headerLayout = (LinearLayout)contentLayout.GetChildAt(0);
            var authorLayout = (LinearLayout)headerLayout.GetChildAt(0);
            _authorText = (TextView)authorLayout.GetChildAt(0);
            _timeText = (TextView)authorLayout.GetChildAt(1);
            _deleteButton = (MaterialButton)headerLayout.GetChildAt(1);
            _commentText = (TextView)contentLayout.GetChildAt(1);

            _deleteButton.Click += (s, e) =>
            {
                if (_comment != null)
                {
                    _onDeleteClick(_comment);
                }
            };
        }

        private static MaterialCardView CreateCardView(Android.Content.Context context)
        {
            var card = new MaterialCardView(context)
            {
                CardElevation = AppUtil.DpToPx(12),
                Radius = AppUtil.DpToPx(8)
            };
            card.SetCardBackgroundColor(Android.Graphics.Color.White);
            card.LayoutParameters = new ViewGroup.MarginLayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent)
            {
                BottomMargin = AppUtil.DpToPx(10)
            };

            var contentLayout = new LinearLayout(context) { Orientation = Orientation.Vertical };
            contentLayout.SetPadding(AppUtil.DpToPx(15), AppUtil.DpToPx(15), AppUtil.DpToPx(15), AppUtil.DpToPx(15));

            var headerLayout = new LinearLayout(context) { Orientation = Orientation.Horizontal };
            var authorLayout = new LinearLayout(context)
            {
                Orientation = Orientation.Vertical,
                LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1)
            };
            var author = new TextView(context) { TextSize = 14, Typeface = Android.Graphics.Typeface.DefaultBold };
            var time = new TextView(context) { TextSize = 12 };
            time.SetTextColor(Android.Graphics.Color.Gray);
            authorLayout.AddView(author);
            authorLayout.AddView(time);

            var deleteBtn = new MaterialButton(context) { Text = "Delete" };
            
            headerLayout.AddView(authorLayout);
            headerLayout.AddView(deleteBtn);

            var commentText = new TextView(context) { TextSize = 14 };
            commentText.SetPadding(0, AppUtil.DpToPx(10), 0, 0);

            contentLayout.AddView(headerLayout);
            contentLayout.AddView(commentText);
            card.AddView(contentLayout);

            return card;
        }

        public void Bind(FeedbackComment comment)
        {
            _comment = comment;
            _authorText.Text = comment.AuthorUsername;
            _timeText.Text = GetRelativeTime(comment.CreatedAt);
            _commentText.Text = comment.Text;
        }

        private string GetRelativeTime(DateTimeOffset? dateTime)
        {
            if (!dateTime.HasValue) return string.Empty;
            
            var timeSpan = DateTimeOffset.UtcNow - dateTime.Value;

            if (timeSpan.TotalSeconds < 60)
                return "just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays}d ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)}w ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)}mo ago";

            return $"{(int)(timeSpan.TotalDays / 365)}y ago";
        }
    }
}
