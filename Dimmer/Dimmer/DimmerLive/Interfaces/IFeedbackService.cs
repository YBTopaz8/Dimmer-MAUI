namespace Dimmer.DimmerLive.Interfaces;

public interface IFeedbackService
{
    // Issue operations
    Task<FeedbackIssue> CreateIssueAsync(string title, string type, string description, string platform);
    Task<IEnumerable<FeedbackIssue>> GetIssuesAsync(string? type = null, string? status = null, string? sortBy = "recent");
    Task<FeedbackIssue> GetIssueByIdAsync(string issueId);
    Task<IEnumerable<FeedbackIssue>> SearchIssuesAsync(string searchTerm, string? type = null);
    Task<bool> DeleteIssueAsync(string issueId);

    // Voting operations
    Task<bool> UpvoteIssueAsync(string issueId);
    Task<bool> RemoveUpvoteAsync(string issueId);
    Task<bool> HasUserUpvotedAsync(string issueId);

    // Comment operations
    Task<FeedbackComment> AddCommentAsync(string issueId, string text);
    Task<IEnumerable<FeedbackComment>> GetCommentsAsync(string issueId);
    Task<bool> DeleteCommentAsync(string commentId);

    // Notification settings
    Task<bool> SetNotificationPreferenceAsync(string issueId, bool notifyOnStatusChange, bool notifyOnComment);
    Task<FeedbackNotificationSettings?> GetNotificationPreferenceAsync(string issueId);

    // Helper
    string GetGitHubIssuesUrl();
}
