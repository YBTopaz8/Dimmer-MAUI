namespace Dimmer.ViewModel.DimmerLiveVM;

public partial class FeedbackBoardViewModel : ObservableObject
{
    private readonly IFeedbackService _feedbackService;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<FeedbackBoardViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<FeedbackIssue> _issues = [];

    [ObservableProperty]
    private ObservableCollection<FeedbackIssue> _filteredIssues = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedType = "All";

    [ObservableProperty]
    private string _selectedStatus = "All";

    [ObservableProperty]
    private string _sortBy = "recent";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isAuthenticated;

    public FeedbackBoardViewModel(
        IFeedbackService feedbackService,
        IAuthenticationService authService,
        ILogger<FeedbackBoardViewModel> logger)
    {
        _feedbackService = feedbackService;
        _authService = authService;
        _logger = logger;
        _isAuthenticated = _authService.IsLoggedIn;

        // Subscribe to authentication changes
        _authService.CurrentUser.Subscribe(user =>
        {
            IsAuthenticated = user != null;
        });
    }

    [RelayCommand]
    private async Task LoadIssuesAsync()
    {
        try
        {
            IsLoading = true;

            string? typeFilter = SelectedType == "All" ? null : SelectedType;
            string? statusFilter = SelectedStatus == "All" ? null : SelectedStatus;

            var issues = await _feedbackService.GetIssuesAsync(typeFilter, statusFilter, SortBy);
            Issues = new ObservableCollection<FeedbackIssue>(issues);
            
            ApplyFilters();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading feedback issues");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SearchIssuesAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await LoadIssuesAsync();
                return;
            }

            IsLoading = true;

            string? typeFilter = SelectedType == "All" ? null : SelectedType;
            var issues = await _feedbackService.SearchIssuesAsync(SearchText, typeFilter);
            Issues = new ObservableCollection<FeedbackIssue>(issues);
            
            ApplyFilters();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching feedback issues");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ApplyFilters()
    {
        var filtered = Issues.AsEnumerable();

        if (SelectedType != "All")
        {
            filtered = filtered.Where(i => i.Type == SelectedType);
        }

        if (SelectedStatus != "All")
        {
            filtered = filtered.Where(i => i.Status == SelectedStatus);
        }

        FilteredIssues = new ObservableCollection<FeedbackIssue>(filtered);
    }

    [RelayCommand]
    private async Task OpenGitHubIssuesAsync()
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

    [RelayCommand]
    private async Task NavigateToSubmitFeedbackAsync()
    {
        if (!IsAuthenticated)
        {
            await OpenGitHubIssuesAsync();
            return;
        }

        // Navigation will be handled by the UI layer
        await Shell.Current.GoToAsync("FeedbackSubmissionPage");
    }

    [RelayCommand]
    private async Task NavigateToIssueDetailAsync(FeedbackIssue issue)
    {
        if (issue == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "IssueId", issue.ObjectId }
        };

        await Shell.Current.GoToAsync("FeedbackDetailPage", navigationParameter);
    }

    partial void OnSelectedTypeChanged(string value)
    {
        _ = LoadIssuesAsync();
    }

    partial void OnSelectedStatusChanged(string value)
    {
        _ = LoadIssuesAsync();
    }

    partial void OnSortByChanged(string value)
    {
        _ = LoadIssuesAsync();
    }
}
