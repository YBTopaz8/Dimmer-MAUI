namespace Dimmer.ViewModel.DimmerLiveVM;

public partial class FeedbackDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly IFeedbackService _feedbackService;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<FeedbackDetailViewModel> _logger;

    [ObservableProperty]
    private FeedbackIssue? _issue;

    [ObservableProperty]
    private ObservableCollection<FeedbackComment> _comments = [];

    [ObservableProperty]
    private string _newCommentText = string.Empty;

    [ObservableProperty]
    private bool _hasUpvoted;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isAuthenticated;

    [ObservableProperty]
    private bool _isAuthor;

    [ObservableProperty]
    private bool _notifyOnStatusChange;

    [ObservableProperty]
    private bool _notifyOnComment;

    private string? _issueId;

    public FeedbackDetailViewModel(
        IFeedbackService feedbackService,
        IAuthenticationService authService,
        ILogger<FeedbackDetailViewModel> logger)
    {
        _feedbackService = feedbackService;
        _authService = authService;
        _logger = logger;
        _isAuthenticated = _authService.IsLoggedIn;

        // Subscribe to authentication changes
        _authService.CurrentUser.Subscribe(user =>
        {
            IsAuthenticated = user != null;
            UpdateIsAuthor();
        });
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("IssueId", out var issueId))
        {
            _issueId = issueId.ToString();
            _ = LoadIssueDetailsAsync();
        }
    }

    [RelayCommand]
    private async Task LoadIssueDetailsAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_issueId))
                return;

            IsLoading = true;

            Issue = await _feedbackService.GetIssueByIdAsync(_issueId);
            var comments = await _feedbackService.GetCommentsAsync(_issueId);
            Comments = new ObservableCollection<FeedbackComment>(comments);

            if (_authService.IsLoggedIn)
            {
                HasUpvoted = await _feedbackService.HasUserUpvotedAsync(_issueId);
                var notificationPref = await _feedbackService.GetNotificationPreferenceAsync(_issueId);
                NotifyOnStatusChange = notificationPref?.NotifyOnStatusChange ?? false;
                NotifyOnComment = notificationPref?.NotifyOnComment ?? false;
            }

            UpdateIsAuthor();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading issue details");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ToggleUpvoteAsync()
    {
        try
        {
            if (!_authService.IsLoggedIn || string.IsNullOrEmpty(_issueId))
                return;

            bool success;
            if (HasUpvoted)
            {
                success = await _feedbackService.RemoveUpvoteAsync(_issueId);
            }
            else
            {
                success = await _feedbackService.UpvoteIssueAsync(_issueId);
            }

            if (success)
            {
                HasUpvoted = !HasUpvoted;
                // Reload to get updated upvote count
                await LoadIssueDetailsAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling upvote");
        }
    }

    [RelayCommand]
    private async Task AddCommentAsync()
    {
        try
        {
            if (!_authService.IsLoggedIn || string.IsNullOrEmpty(_issueId))
                return;

            if (string.IsNullOrWhiteSpace(NewCommentText))
                return;

            var comment = await _feedbackService.AddCommentAsync(_issueId, NewCommentText);
            Comments.Add(comment);
            NewCommentText = string.Empty;

            // Reload to update comment count
            await LoadIssueDetailsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment");
        }
    }

    [RelayCommand]
    private async Task DeleteCommentAsync(FeedbackComment comment)
    {
        try
        {
            if (comment == null) return;

            var success = await _feedbackService.DeleteCommentAsync(comment.ObjectId);
            if (success)
            {
                Comments.Remove(comment);
                // Reload to update comment count
                await LoadIssueDetailsAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment");
        }
    }

    [RelayCommand]
    private async Task DeleteIssueAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_issueId))
                return;

            var success = await _feedbackService.DeleteIssueAsync(_issueId);
            if (success)
            {
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting issue");
        }
    }

    [RelayCommand]
    private async Task UpdateNotificationPreferencesAsync()
    {
        try
        {
            if (!_authService.IsLoggedIn || string.IsNullOrEmpty(_issueId))
                return;

            await _feedbackService.SetNotificationPreferenceAsync(
                _issueId,
                NotifyOnStatusChange,
                NotifyOnComment
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preferences");
        }
    }

    [RelayCommand]
    private async Task OpenGitHubIssueAsync()
    {
        try
        {
            var url = _feedbackService.GetGitHubIssuesUrl();
            await Browser.Default.OpenAsync(url, BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening GitHub issues");
        }
    }

    private void UpdateIsAuthor()
    {
        IsAuthor = _authService.IsLoggedIn &&
                   _authService.CurrentUserValue != null &&
                   Issue?.Author?.ObjectId == _authService.CurrentUserValue.ObjectId;
    }
}
