namespace Dimmer.ViewModel.DimmerLiveVM;

public partial class FeedbackSubmissionViewModel : ObservableObject, IQueryAttributable
{
    private readonly IFeedbackService _feedbackService;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<FeedbackSubmissionViewModel> _logger;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _selectedType = FeedbackIssueType.Feature;

    [ObservableProperty]
    private string _selectedPlatform = "All";

    [ObservableProperty]
    private bool _isSubmitting;

    [ObservableProperty]
    private ObservableCollection<FeedbackIssue> _similarIssues = [];

    [ObservableProperty]
    private bool _hasSimilarIssues;

    public List<string> IssueTypes { get; } = [FeedbackIssueType.Bug, FeedbackIssueType.Feature];
    public List<string> Platforms { get; } = ["All", "Windows", "Android"];

    public FeedbackSubmissionViewModel(
        IFeedbackService feedbackService,
        IAuthenticationService authService,
        ILogger<FeedbackSubmissionViewModel> logger)
    {
        _feedbackService = feedbackService;
        _authService = authService;
        _logger = logger;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        // Handle any query parameters if needed
    }

    [RelayCommand]
    private async Task CheckForDuplicatesAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Title) || Title.Length < 3)
            {
                SimilarIssues.Clear();
                HasSimilarIssues = false;
                return;
            }

            var similar = await _feedbackService.SearchIssuesAsync(Title, SelectedType);
            SimilarIssues = new ObservableCollection<FeedbackIssue>(similar.Take(5));
            HasSimilarIssues = SimilarIssues.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for duplicate issues");
        }
    }

    [RelayCommand]
    private async Task SubmitFeedbackAsync()
    {
        try
        {
            if (!_authService.IsLoggedIn)
            {
                _logger.LogWarning("Attempted to submit feedback without authentication");
                return;
            }

            if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Description))
            {
                _logger.LogWarning("Title or description is empty");
                return;
            }

            IsSubmitting = true;

            var issue = await _feedbackService.CreateIssueAsync(
                Title,
                SelectedType,
                Description,
                SelectedPlatform
            );

            _logger.LogInformation("Successfully created feedback issue: {Title}", Title);

            // Navigate back to the feedback board
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting feedback");
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task NavigateToSimilarIssueAsync(FeedbackIssue issue)
    {
        if (issue == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "IssueId", issue.ObjectId }
        };

        await Shell.Current.GoToAsync("FeedbackDetailPage", navigationParameter);
    }

    partial void OnTitleChanged(string value)
    {
        // Debounce the duplicate check
        _ = Task.Run(async () =>
        {
            await Task.Delay(500);
            await CheckForDuplicatesAsync();
        });
    }
}
