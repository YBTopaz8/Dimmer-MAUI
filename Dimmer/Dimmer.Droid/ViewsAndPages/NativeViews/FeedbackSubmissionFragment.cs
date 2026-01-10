using Android.Views;
using Android.Widget;
using Dimmer.DimmerLive.Models;
using Dimmer.ViewModel.DimmerLiveVM;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;

namespace Dimmer.ViewsAndPages.NativeViews;

public class FeedbackSubmissionFragment : Fragment
{
    private readonly BaseViewModelAnd _viewModel;
    private FeedbackSubmissionViewModel _feedbackViewModel;
    private TextInputEditText _titleEditText;
    private TextInputEditText _descriptionEditText;
    private Spinner _typeSpinner;
    private Spinner _platformSpinner;
    private MaterialButton _submitButton;
    private MaterialButton _cancelButton;
    private ProgressBar _progressBar;
    private LinearLayout _similarIssuesLayout;

    public FeedbackSubmissionFragment(BaseViewModelAnd viewModel)
    {
        _viewModel = viewModel;
    }

    public override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _feedbackViewModel = IPlatformApplication.Current!.Services.GetRequiredService<FeedbackSubmissionViewModel>();
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

        // Header
        var headerText = new TextView(ctx)
        {
            Text = "Submit Feedback",
            TextSize = 24,
            Typeface = Android.Graphics.Typeface.DefaultBold
        };
        root.AddView(headerText);

        var subHeaderText = new TextView(ctx)
        {
            Text = "Help us improve Dimmer by reporting bugs or requesting features",
            TextSize = 14
        };
        subHeaderText.SetTextColor(Android.Graphics.Color.Gray);
        subHeaderText.SetPadding(0, 0, 0, AppUtil.DpToPx(16));
        root.AddView(subHeaderText);

        // Type Spinner
        var typeLabel = new TextView(ctx) { Text = "Type *", TextSize = 16, Typeface = Android.Graphics.Typeface.DefaultBold };
        root.AddView(typeLabel);
        _typeSpinner = new Spinner(ctx);
        var typeAdapter = new ArrayAdapter<string>(ctx, Android.Resource.Layout.SimpleSpinnerItem, _feedbackViewModel.IssueTypes);
        typeAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
        _typeSpinner.Adapter = typeAdapter;
        _typeSpinner.ItemSelected += (s, e) =>
        {
            _feedbackViewModel.SelectedType = _feedbackViewModel.IssueTypes[e.Position];
        };
        root.AddView(_typeSpinner);

        // Platform Spinner
        var platformLabel = new TextView(ctx) { Text = "Platform *", TextSize = 16, Typeface = Android.Graphics.Typeface.DefaultBold };
        platformLabel.SetPadding(0, AppUtil.DpToPx(16), 0, 0);
        root.AddView(platformLabel);
        _platformSpinner = new Spinner(ctx);
        var platformAdapter = new ArrayAdapter<string>(ctx, Android.Resource.Layout.SimpleSpinnerItem, _feedbackViewModel.Platforms);
        platformAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
        _platformSpinner.Adapter = platformAdapter;
        _platformSpinner.ItemSelected += (s, e) =>
        {
            _feedbackViewModel.SelectedPlatform = _feedbackViewModel.Platforms[e.Position];
        };
        root.AddView(_platformSpinner);

        // Title
        var titleLabel = new TextView(ctx) { Text = "Title *", TextSize = 16, Typeface = Android.Graphics.Typeface.DefaultBold };
        titleLabel.SetPadding(0, AppUtil.DpToPx(16), 0, 0);
        root.AddView(titleLabel);
        var titleInputLayout = new TextInputLayout(ctx) { Hint = "Brief description of the issue or feature" };
        _titleEditText = new TextInputEditText(ctx);
        _titleEditText.TextChanged += OnTitleChanged;
        titleInputLayout.AddView(_titleEditText);
        root.AddView(titleInputLayout);

        // Similar Issues Warning
        _similarIssuesLayout = new LinearLayout(ctx)
        {
            Orientation = Orientation.Vertical,
            Visibility = ViewStates.Gone
        };
        _similarIssuesLayout.SetBackgroundColor(Android.Graphics.Color.Rgb(255, 243, 205));
        _similarIssuesLayout.SetPadding(AppUtil.DpToPx(15), AppUtil.DpToPx(15), AppUtil.DpToPx(15), AppUtil.DpToPx(15));
        
        var warningText = new TextView(ctx)
        {
            Text = "⚠️ Similar issues found. Please check before submitting:",
            TextSize = 14,
            Typeface = Android.Graphics.Typeface.DefaultBold
        };
        warningText.SetTextColor(Android.Graphics.Color.Rgb(133, 100, 4));
        _similarIssuesLayout.AddView(warningText);
        root.AddView(_similarIssuesLayout);

        // Description
        var descLabel = new TextView(ctx) { Text = "Description *", TextSize = 16, Typeface = Android.Graphics.Typeface.DefaultBold };
        descLabel.SetPadding(0, AppUtil.DpToPx(16), 0, 0);
        root.AddView(descLabel);
        var descInputLayout = new TextInputLayout(ctx) { Hint = "Provide detailed information..." };
        _descriptionEditText = new TextInputEditText(ctx);
        _descriptionEditText.SetMinHeight(AppUtil.DpToPx(200));
        _descriptionEditText.Gravity = GravityFlags.Top;
        _descriptionEditText.TextChanged += (s, e) =>
        {
            _feedbackViewModel.Description = _descriptionEditText.Text ?? string.Empty;
        };
        descInputLayout.AddView(_descriptionEditText);
        root.AddView(descInputLayout);

        // Buttons
        var buttonLayout = new LinearLayout(ctx)
        {
            Orientation = Orientation.Horizontal
        };
        buttonLayout.SetPadding(0, AppUtil.DpToPx(16), 0, 0);

        _submitButton = new MaterialButton(ctx) { Text = "Submit" };
        _submitButton.Click += OnSubmitClick;
        buttonLayout.AddView(_submitButton);

        _cancelButton = new MaterialButton(ctx) { Text = "Cancel" };
        _cancelButton.Click += OnCancelClick;
        buttonLayout.AddView(_cancelButton);

        _progressBar = new ProgressBar(ctx) { Indeterminate = true, Visibility = ViewStates.Gone };
        buttonLayout.AddView(_progressBar);

        root.AddView(buttonLayout);
        scrollView.AddView(root);

        return scrollView;
    }

    private async void OnTitleChanged(object? sender, Android.Text.TextChangedEventArgs e)
    {
        _feedbackViewModel.Title = _titleEditText.Text ?? string.Empty;
        
        if (_feedbackViewModel.Title.Length >= 3)
        {
            await _feedbackViewModel.CheckForDuplicatesCommand.ExecuteAsync(null);
            UpdateSimilarIssues();
        }
        else
        {
            _similarIssuesLayout.Visibility = ViewStates.Gone;
        }
    }

    private void UpdateSimilarIssues()
    {
        // Clear previous similar issues
        while (_similarIssuesLayout.ChildCount > 1)
        {
            _similarIssuesLayout.RemoveViewAt(1);
        }

        if (_feedbackViewModel.HasSimilarIssues)
        {
            _similarIssuesLayout.Visibility = ViewStates.Visible;
            
            foreach (var issue in _feedbackViewModel.SimilarIssues)
            {
                var issueCard = new LinearLayout(Context!)
                {
                    Orientation = Orientation.Vertical
                };
                issueCard.SetBackgroundColor(Android.Graphics.Color.White);
                issueCard.SetPadding(AppUtil.DpToPx(10), AppUtil.DpToPx(10), AppUtil.DpToPx(10), AppUtil.DpToPx(10));

                var issueTitle = new TextView(Context!)
                {
                    Text = issue.Title,
                    TextSize = 14,
                    Typeface = Android.Graphics.Typeface.DefaultBold
                };
                issueCard.AddView(issueTitle);

                var issueMeta = new TextView(Context!)
                {
                    Text = $"{issue.Type} • ↑ {issue.UpvoteCount} upvotes",
                    TextSize = 12
                };
                issueMeta.SetTextColor(Android.Graphics.Color.Gray);
                issueCard.AddView(issueMeta);

                issueCard.Click += (s, e) =>
                {
                    var detailFragment = new FeedbackDetailFragment(_viewModel, issue.ObjectId);
                    ParentFragmentManager.BeginTransaction()
                        .Replace(Android.Resource.Id.Content, detailFragment)
                        .AddToBackStack(null)
                        .Commit();
                };

                _similarIssuesLayout.AddView(issueCard);
            }
        }
        else
        {
            _similarIssuesLayout.Visibility = ViewStates.Gone;
        }
    }

    private async void OnSubmitClick(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_feedbackViewModel.Title) || string.IsNullOrWhiteSpace(_feedbackViewModel.Description))
        {
            var dialog = new AndroidX.AppCompat.App.AlertDialog.Builder(Context!)
                .SetTitle("Missing Information")
                .SetMessage("Please provide both a title and description for your feedback.")
                .SetPositiveButton("OK", (s, args) => { })
                .Create();
            dialog.Show();
            return;
        }

        _progressBar.Visibility = ViewStates.Visible;
        _submitButton.Enabled = false;

        await _feedbackViewModel.SubmitFeedbackCommand.ExecuteAsync(null);

        _progressBar.Visibility = ViewStates.Gone;
        _submitButton.Enabled = true;

        // Go back to feedback board
        ParentFragmentManager.PopBackStack();
    }

    private void OnCancelClick(object? sender, EventArgs e)
    {
        ParentFragmentManager.PopBackStack();
    }
}
