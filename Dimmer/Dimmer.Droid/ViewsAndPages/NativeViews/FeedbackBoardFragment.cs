using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Dimmer.DimmerLive.Models;
using Dimmer.ViewModel.DimmerLiveVM;
using Google.Android.Material.Button;
using Google.Android.Material.Card;
using Google.Android.Material.Chip;
using Google.Android.Material.TextField;

namespace Dimmer.ViewsAndPages.NativeViews;

public class FeedbackBoardFragment : Fragment
{
    private readonly BaseViewModelAnd _viewModel;
    private FeedbackBoardViewModel _feedbackViewModel;
    private RecyclerView _recyclerView;
    private FeedbackIssueAdapter _adapter;
    private TextInputEditText _searchEditText;
    private ChipGroup _typeFilterGroup;
    private ChipGroup _statusFilterGroup;
    private MaterialButton _submitButton;
    private ProgressBar _progressBar;

    public FeedbackBoardFragment(BaseViewModelAnd viewModel)
    {
        _viewModel = viewModel;
    }

    public override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _feedbackViewModel = IPlatformApplication.Current!.Services.GetRequiredService<FeedbackBoardViewModel>();
    }

    public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        var ctx = Context!;
        
        // Root ScrollView
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

        // Header
        var headerText = new TextView(ctx)
        {
            Text = "Feedback Board",
            TextSize = 24,
            Typeface = Android.Graphics.Typeface.DefaultBold
        };
        root.AddView(headerText);

        var subHeaderText = new TextView(ctx)
        {
            Text = "Submit bugs, request features, and see what others are asking for",
            TextSize = 14
        };
        subHeaderText.SetTextColor(Android.Graphics.Color.Gray);
        subHeaderText.SetPadding(0, 0, 0, AppUtil.DpToPx(16));
        root.AddView(subHeaderText);

        // Search Box
        var searchInputLayout = new TextInputLayout(ctx)
        {
            Hint = "Search feedback..."
        };
        _searchEditText = new TextInputEditText(ctx);
        _searchEditText.TextChanged += OnSearchTextChanged;
        searchInputLayout.AddView(_searchEditText);
        root.AddView(searchInputLayout);

        // Submit Button
        _submitButton = new MaterialButton(ctx)
        {
            Text = "Submit Feedback"
        };
        _submitButton.SetPadding(0, AppUtil.DpToPx(8), 0, AppUtil.DpToPx(8));
        _submitButton.Click += OnSubmitButtonClick;
        root.AddView(_submitButton);

        // Type Filter
        var typeLabel = new TextView(ctx)
        {
            Text = "Type",
            TextSize = 16,
            Typeface = Android.Graphics.Typeface.DefaultBold
        };
        typeLabel.SetPadding(0, AppUtil.DpToPx(16), 0, AppUtil.DpToPx(8));
        root.AddView(typeLabel);

        _typeFilterGroup = new ChipGroup(ctx) { SingleSelection = true };
        AddFilterChip(_typeFilterGroup, "All", true);
        AddFilterChip(_typeFilterGroup, "Bug", false);
        AddFilterChip(_typeFilterGroup, "Feature", false);
        root.AddView(_typeFilterGroup);

        // Status Filter
        var statusLabel = new TextView(ctx)
        {
            Text = "Status",
            TextSize = 16,
            Typeface = Android.Graphics.Typeface.DefaultBold
        };
        statusLabel.SetPadding(0, AppUtil.DpToPx(16), 0, AppUtil.DpToPx(8));
        root.AddView(statusLabel);

        _statusFilterGroup = new ChipGroup(ctx) { SingleSelection = true };
        AddFilterChip(_statusFilterGroup, "All", true);
        AddFilterChip(_statusFilterGroup, "open", false);
        AddFilterChip(_statusFilterGroup, "planned", false);
        AddFilterChip(_statusFilterGroup, "in-progress", false);
        AddFilterChip(_statusFilterGroup, "shipped", false);
        AddFilterChip(_statusFilterGroup, "rejected", false);
        root.AddView(_statusFilterGroup);

        // Progress Bar
        _progressBar = new ProgressBar(ctx)
        {
            Indeterminate = true
        };
        _progressBar.SetPadding(0, AppUtil.DpToPx(16), 0, AppUtil.DpToPx(16));
        root.AddView(_progressBar);

        // RecyclerView for issues
        _recyclerView = new RecyclerView(ctx)
        {
            LayoutParameters = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, 
                ViewGroup.LayoutParams.WrapContent)
        };
        _recyclerView.SetLayoutManager(new LinearLayoutManager(ctx));
        _adapter = new FeedbackIssueAdapter(new List<FeedbackIssue>(), OnIssueClicked);
        _recyclerView.SetAdapter(_adapter);
        root.AddView(_recyclerView);

        scrollView.AddView(root);

        // Load initial data
        LoadIssues();

        return scrollView;
    }

    private void AddFilterChip(ChipGroup group, string label, bool isChecked)
    {
        var chip = new Chip(Context!)
        {
            Text = label,
            Checkable = true,
            Checked = isChecked
        };
        chip.CheckedChange += OnFilterChanged;
        group.AddView(chip);
    }

    private async void OnSearchTextChanged(object? sender, Android.Text.TextChangedEventArgs e)
    {
        _feedbackViewModel.SearchText = _searchEditText.Text ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(_feedbackViewModel.SearchText))
        {
            await _feedbackViewModel.SearchIssuesCommand.ExecuteAsync(null);
            UpdateIssuesList();
        }
        else
        {
            await LoadIssues();
        }
    }

    private async void OnFilterChanged(object? sender, CompoundButton.CheckedChangeEventArgs e)
    {
        if (!e.IsChecked) return;

        // Update ViewModel filters
        var typeChip = _typeFilterGroup.CheckedChipId;
        if (typeChip != -1)
        {
            var chip = _typeFilterGroup.FindViewById<Chip>(typeChip);
            _feedbackViewModel.SelectedType = chip?.Text ?? "All";
        }

        var statusChip = _statusFilterGroup.CheckedChipId;
        if (statusChip != -1)
        {
            var chip = _statusFilterGroup.FindViewById<Chip>(statusChip);
            _feedbackViewModel.SelectedStatus = chip?.Text ?? "All";
        }

        await LoadIssues();
    }

    private async void OnSubmitButtonClick(object? sender, EventArgs e)
    {
        if (!_feedbackViewModel.IsAuthenticated)
        {
            // Show dialog and open GitHub
            var dialog = new AndroidX.AppCompat.App.AlertDialog.Builder(Context!)
                .SetTitle("Authentication Required")
                .SetMessage("You need a Dimmer account to submit feedback in-app.\n\nWould you like to open GitHub Issues instead?")
                .SetPositiveButton("Open GitHub", async (s, args) =>
                {
                    await _feedbackViewModel.OpenGitHubIssuesCommand.ExecuteAsync(null);
                })
                .SetNegativeButton("Cancel", (s, args) => { })
                .Create();
            dialog.Show();
            return;
        }

        // Navigate to submission fragment
        var submissionFragment = new FeedbackSubmissionFragment(_viewModel);
        ParentFragmentManager.BeginTransaction()
            .Replace(Android.Resource.Id.Content, submissionFragment)
            .AddToBackStack(null)
            .Commit();
    }

    private void OnIssueClicked(FeedbackIssue issue)
    {
        var detailFragment = new FeedbackDetailFragment(_viewModel, issue.ObjectId);
        ParentFragmentManager.BeginTransaction()
            .Replace(Android.Resource.Id.Content, detailFragment)
            .AddToBackStack(null)
            .Commit();
    }

    private async Task LoadIssues()
    {
        _progressBar.Visibility = ViewStates.Visible;
        await _feedbackViewModel.LoadIssuesCommand.ExecuteAsync(null);
        UpdateIssuesList();
        _progressBar.Visibility = ViewStates.Gone;
    }

    private void UpdateIssuesList()
    {
        var issues = _feedbackViewModel.FilteredIssues.ToList();
        _adapter.UpdateIssues(issues);
    }
}

// Adapter for RecyclerView
public class FeedbackIssueAdapter : RecyclerView.Adapter
{
    private List<FeedbackIssue> _issues;
    private readonly Action<FeedbackIssue> _onItemClick;

    public FeedbackIssueAdapter(List<FeedbackIssue> issues, Action<FeedbackIssue> onItemClick)
    {
        _issues = issues;
        _onItemClick = onItemClick;
    }

    public void UpdateIssues(List<FeedbackIssue> issues)
    {
        _issues = issues;
        NotifyDataSetChanged();
    }

    public override int ItemCount => _issues.Count;

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        if (holder is IssueViewHolder issueHolder)
        {
            issueHolder.Bind(_issues[position]);
        }
    }

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        return new IssueViewHolder(parent.Context!, _onItemClick);
    }

    private class IssueViewHolder : RecyclerView.ViewHolder
    {
        private readonly MaterialCardView _cardView;
        private readonly TextView _titleText;
        private readonly TextView _descriptionText;
        private readonly TextView _metaText;
        private readonly TextView _upvoteText;
        private readonly TextView _statusBadge;
        private readonly TextView _typeBadge;
        private FeedbackIssue? _issue;
        private readonly Action<FeedbackIssue> _onItemClick;

        public IssueViewHolder(Android.Content.Context context, Action<FeedbackIssue> onItemClick) 
            : base(CreateCardView(context))
        {
            _onItemClick = onItemClick;
            _cardView = (MaterialCardView)ItemView;
            
            var contentLayout = (LinearLayout)_cardView.GetChildAt(0);
            _upvoteText = (TextView)((LinearLayout)contentLayout.GetChildAt(0)).GetChildAt(1);
            
            var mainContent = (LinearLayout)contentLayout.GetChildAt(1);
            _titleText = (TextView)mainContent.GetChildAt(0);
            _descriptionText = (TextView)mainContent.GetChildAt(1);
            _metaText = (TextView)mainContent.GetChildAt(2);
            
            var badgeLayout = (LinearLayout)contentLayout.GetChildAt(2);
            _statusBadge = (TextView)badgeLayout.GetChildAt(0);
            _typeBadge = (TextView)badgeLayout.GetChildAt(1);

            _cardView.Click += (s, e) =>
            {
                if (_issue != null)
                {
                    _onItemClick(_issue);
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

            var contentLayout = new LinearLayout(context)
            {
                Orientation = Orientation.Horizontal
            };
            contentLayout.SetPadding(AppUtil.DpToPx(15), AppUtil.DpToPx(15), AppUtil.DpToPx(15), AppUtil.DpToPx(15));

            // Upvote section
            var upvoteLayout = new LinearLayout(context)
            {
                Orientation = Orientation.Vertical,
                LayoutParameters = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.WrapContent,
                    ViewGroup.LayoutParams.WrapContent)
                {
                    RightMargin = AppUtil.DpToPx(15)
                }
            };
            var upvoteIcon = new TextView(context) { Text = "↑", TextSize = 18 };
            var upvoteCount = new TextView(context) { TextSize = 16, Typeface = Android.Graphics.Typeface.DefaultBold };
            upvoteLayout.AddView(upvoteIcon);
            upvoteLayout.AddView(upvoteCount);

            // Main content
            var mainContent = new LinearLayout(context)
            {
                Orientation = Orientation.Vertical,
                LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1)
            };
            var title = new TextView(context) { TextSize = 18, Typeface = Android.Graphics.Typeface.DefaultBold };
            var description = new TextView(context) { TextSize = 14, MaxLines = 2 };
            description.SetTextColor(Android.Graphics.Color.Gray);
            var meta = new TextView(context) { TextSize = 12 };
            meta.SetTextColor(Android.Graphics.Color.Gray);
            mainContent.AddView(title);
            mainContent.AddView(description);
            mainContent.AddView(meta);

            // Badges
            var badgeLayout = new LinearLayout(context)
            {
                Orientation = Orientation.Vertical,
                LayoutParameters = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.WrapContent,
                    ViewGroup.LayoutParams.WrapContent)
            };
            var statusBadge = new TextView(context) { TextSize = 12, Typeface = Android.Graphics.Typeface.DefaultBold };
            statusBadge.SetPadding(AppUtil.DpToPx(10), AppUtil.DpToPx(5), AppUtil.DpToPx(10), AppUtil.DpToPx(5));
            var typeBadge = new TextView(context) { TextSize = 11, Typeface = Android.Graphics.Typeface.DefaultBold };
            typeBadge.SetPadding(AppUtil.DpToPx(8), AppUtil.DpToPx(4), AppUtil.DpToPx(8), AppUtil.DpToPx(4));
            badgeLayout.AddView(statusBadge);
            badgeLayout.AddView(typeBadge);

            contentLayout.AddView(upvoteLayout);
            contentLayout.AddView(mainContent);
            contentLayout.AddView(badgeLayout);
            card.AddView(contentLayout);

            return card;
        }

        public void Bind(FeedbackIssue issue)
        {
            _issue = issue;
            _titleText.Text = issue.Title;
            _descriptionText.Text = issue.Description;
            _metaText.Text = $"{issue.AuthorUsername} • {issue.Platform} • {issue.CommentCount} comments";
            _upvoteText.Text = issue.UpvoteCount.ToString();
            _statusBadge.Text = issue.Status;
            _typeBadge.Text = issue.Type;

            // Set badge colors
            _statusBadge.SetBackgroundColor(GetStatusColor(issue.Status));
            _statusBadge.SetTextColor(Android.Graphics.Color.White);
            _typeBadge.SetBackgroundColor(GetTypeColor(issue.Type));
            _typeBadge.SetTextColor(Android.Graphics.Color.White);
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
}
