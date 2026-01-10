namespace Dimmer.DimmerLive.Interfaces.Implementations;

public class ParseFeedbackService : IFeedbackService
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<ParseFeedbackService> _logger;
    private const string GitHubIssuesUrl = "https://github.com/YBTopaz8/Dimmer-MAUI/issues";

    public ParseFeedbackService(IAuthenticationService authService, ILogger<ParseFeedbackService> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public async Task<FeedbackIssue> CreateIssueAsync(string title, string type, string description, string platform)
    {
        try
        {
            if (!_authService.IsLoggedIn || _authService.CurrentUserValue == null)
            {
                throw new InvalidOperationException("User must be logged in to create feedback");
            }

            var issue = new FeedbackIssue
            {
                Title = title,
                Type = type,
                Description = description,
                Status = FeedbackIssueStatus.Open,
                Platform = platform,
                AppVersion = AppInfo.Current.VersionString,
                Author = _authService.CurrentUserValue,
                AuthorUsername = _authService.CurrentUserValue.Username ?? "Unknown",
                UpvoteCount = 0,
                CommentCount = 0
            };

            await issue.SaveAsync();
            _logger.LogInformation("Created feedback issue: {Title}", title);
            return issue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating feedback issue");
            throw;
        }
    }

    public async Task<IEnumerable<FeedbackIssue>> GetIssuesAsync(string? type = null, string? status = null, string? sortBy = "recent")
    {
        try
        {
            var query = ParseClient.Instance.GetQuery<FeedbackIssue>();

            if (!string.IsNullOrEmpty(type))
            {
                query = query.WhereEqualTo(nameof(FeedbackIssue.Type), type);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.WhereEqualTo(nameof(FeedbackIssue.Status), status);
            }

            // Apply sorting
            query = sortBy?.ToLower() switch
            {
                "upvotes" => query.OrderByDescending(nameof(FeedbackIssue.UpvoteCount)),
                "status" => query.OrderBy(nameof(FeedbackIssue.Status)),
                _ => query.OrderByDescending("createdAt")
            };

            var results = await query.FindAsync();
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching feedback issues");
            throw;
        }
    }

    public async Task<FeedbackIssue> GetIssueByIdAsync(string issueId)
    {
        try
        {
            var query = ParseClient.Instance.GetQuery<FeedbackIssue>()
                .WhereEqualTo("objectId", issueId);

            var issue = await query.FirstOrDefaultAsync();
            if (issue == null)
            {
                throw new InvalidOperationException($"Issue with ID {issueId} not found");
            }

            return issue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching feedback issue {IssueId}", issueId);
            throw;
        }
    }

    public async Task<IEnumerable<FeedbackIssue>> SearchIssuesAsync(string searchTerm, string? type = null)
    {
        try
        {
            var query = ParseClient.Instance.GetQuery<FeedbackIssue>();

            if (!string.IsNullOrEmpty(type))
            {
                query = query.WhereEqualTo(nameof(FeedbackIssue.Type), type);
            }

            // Search in title and description
            var titleQuery = query.WhereContains(nameof(FeedbackIssue.Title), searchTerm);
            var descQuery = query.WhereContains(nameof(FeedbackIssue.Description), searchTerm);

            var combinedQuery = ParseQuery<FeedbackIssue>.Or(new[] { titleQuery, descQuery });
            combinedQuery = combinedQuery.OrderByDescending("createdAt");

            var results = await combinedQuery.FindAsync();
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching feedback issues");
            throw;
        }
    }

    public async Task<bool> DeleteIssueAsync(string issueId)
    {
        try
        {
            if (!_authService.IsLoggedIn || _authService.CurrentUserValue == null)
            {
                return false;
            }

            var issue = await GetIssueByIdAsync(issueId);
            
            // Only allow author to delete their own issues
            if (issue.Author?.ObjectId != _authService.CurrentUserValue.ObjectId)
            {
                _logger.LogWarning("User {UserId} attempted to delete issue they don't own", _authService.CurrentUserValue.ObjectId);
                return false;
            }

            await issue.DeleteAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting feedback issue {IssueId}", issueId);
            return false;
        }
    }

    public async Task<bool> UpvoteIssueAsync(string issueId)
    {
        try
        {
            if (!_authService.IsLoggedIn || _authService.CurrentUserValue == null)
            {
                return false;
            }

            // Check if already upvoted
            if (await HasUserUpvotedAsync(issueId))
            {
                return false;
            }

            var vote = new FeedbackVote
            {
                Issue = await GetIssueByIdAsync(issueId),
                User = _authService.CurrentUserValue,
                UserId = _authService.CurrentUserValue.ObjectId
            };

            await vote.SaveAsync();

            // Increment upvote count on the issue
            var issue = await GetIssueByIdAsync(issueId);
            issue.UpvoteCount++;
            await issue.SaveAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upvoting issue {IssueId}", issueId);
            return false;
        }
    }

    public async Task<bool> RemoveUpvoteAsync(string issueId)
    {
        try
        {
            if (!_authService.IsLoggedIn || _authService.CurrentUserValue == null)
            {
                return false;
            }

            var query = ParseClient.Instance.GetQuery<FeedbackVote>()
                .WhereEqualTo(nameof(FeedbackVote.UserId), _authService.CurrentUserValue.ObjectId)
                .Include(nameof(FeedbackVote.Issue));

            var votes = await query.FindAsync();
            var vote = votes.FirstOrDefault(v => v.Issue?.ObjectId == issueId);

            if (vote == null)
            {
                return false;
            }

            await vote.DeleteAsync();

            // Decrement upvote count
            var issue = await GetIssueByIdAsync(issueId);
            issue.UpvoteCount = Math.Max(0, issue.UpvoteCount - 1);
            await issue.SaveAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing upvote from issue {IssueId}", issueId);
            return false;
        }
    }

    public async Task<bool> HasUserUpvotedAsync(string issueId)
    {
        try
        {
            if (!_authService.IsLoggedIn || _authService.CurrentUserValue == null)
            {
                return false;
            }

            var query = ParseClient.Instance.GetQuery<FeedbackVote>()
                .WhereEqualTo(nameof(FeedbackVote.UserId), _authService.CurrentUserValue.ObjectId)
                .Include(nameof(FeedbackVote.Issue));

            var votes = await query.FindAsync();
            return votes.Any(v => v.Issue?.ObjectId == issueId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking upvote status for issue {IssueId}", issueId);
            return false;
        }
    }

    public async Task<FeedbackComment> AddCommentAsync(string issueId, string text)
    {
        try
        {
            if (!_authService.IsLoggedIn || _authService.CurrentUserValue == null)
            {
                throw new InvalidOperationException("User must be logged in to comment");
            }

            var issue = await GetIssueByIdAsync(issueId);

            var comment = new FeedbackComment
            {
                Issue = issue,
                Text = text,
                Author = _authService.CurrentUserValue,
                AuthorUsername = _authService.CurrentUserValue.Username ?? "Unknown"
            };

            await comment.SaveAsync();

            // Increment comment count
            issue.CommentCount++;
            await issue.SaveAsync();

            return comment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment to issue {IssueId}", issueId);
            throw;
        }
    }

    public async Task<IEnumerable<FeedbackComment>> GetCommentsAsync(string issueId)
    {
        try
        {
            var query = ParseClient.Instance.GetQuery<FeedbackComment>()
                .Include(nameof(FeedbackComment.Issue));

            var comments = await query.FindAsync();
            return comments.Where(c => c.Issue?.ObjectId == issueId)
                          .OrderBy(c => c.CreatedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching comments for issue {IssueId}", issueId);
            throw;
        }
    }

    public async Task<bool> DeleteCommentAsync(string commentId)
    {
        try
        {
            if (!_authService.IsLoggedIn || _authService.CurrentUserValue == null)
            {
                return false;
            }

            var query = ParseClient.Instance.GetQuery<FeedbackComment>()
                .WhereEqualTo("objectId", commentId)
                .Include(nameof(FeedbackComment.Issue));

            var comment = await query.FirstOrDefaultAsync();
            if (comment == null)
            {
                return false;
            }

            // Only allow author to delete their own comments
            if (comment.Author?.ObjectId != _authService.CurrentUserValue.ObjectId)
            {
                return false;
            }

            // Decrement comment count on issue
            if (comment.Issue != null)
            {
                comment.Issue.CommentCount = Math.Max(0, comment.Issue.CommentCount - 1);
                await comment.Issue.SaveAsync();
            }

            await comment.DeleteAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
            return false;
        }
    }

    public async Task<bool> SetNotificationPreferenceAsync(string issueId, bool notifyOnStatusChange, bool notifyOnComment)
    {
        try
        {
            if (!_authService.IsLoggedIn || _authService.CurrentUserValue == null)
            {
                return false;
            }

            var userId = _authService.CurrentUserValue.ObjectId;

            // Check if preference already exists
            var query = ParseClient.Instance.GetQuery<FeedbackNotificationSettings>()
                .WhereEqualTo(nameof(FeedbackNotificationSettings.UserId), userId)
                .WhereEqualTo(nameof(FeedbackNotificationSettings.IssueId), issueId);

            var existing = await query.FirstOrDefaultAsync();

            if (existing != null)
            {
                existing.NotifyOnStatusChange = notifyOnStatusChange;
                existing.NotifyOnComment = notifyOnComment;
                await existing.SaveAsync();
            }
            else
            {
                var preference = new FeedbackNotificationSettings
                {
                    User = _authService.CurrentUserValue,
                    UserId = userId,
                    Issue = await GetIssueByIdAsync(issueId),
                    IssueId = issueId,
                    NotifyOnStatusChange = notifyOnStatusChange,
                    NotifyOnComment = notifyOnComment
                };
                await preference.SaveAsync();
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting notification preference for issue {IssueId}", issueId);
            return false;
        }
    }

    public async Task<FeedbackNotificationSettings?> GetNotificationPreferenceAsync(string issueId)
    {
        try
        {
            if (!_authService.IsLoggedIn || _authService.CurrentUserValue == null)
            {
                return null;
            }

            var query = ParseClient.Instance.GetQuery<FeedbackNotificationSettings>()
                .WhereEqualTo(nameof(FeedbackNotificationSettings.UserId), _authService.CurrentUserValue.ObjectId)
                .WhereEqualTo(nameof(FeedbackNotificationSettings.IssueId), issueId);

            return await query.FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching notification preference for issue {IssueId}", issueId);
            return null;
        }
    }

    public string GetGitHubIssuesUrl()
    {
        return GitHubIssuesUrl;
    }
}
