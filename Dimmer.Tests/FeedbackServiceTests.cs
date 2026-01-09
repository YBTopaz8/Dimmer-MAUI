using Dimmer.DimmerLive.Interfaces;
using Dimmer.DimmerLive.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Dimmer.Tests;

public class FeedbackServiceTests
{
    [Fact]
    public void FeedbackIssue_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var issue = new FeedbackIssue
        {
            Title = "Test Issue",
            Type = FeedbackIssueType.Bug,
            Description = "Test Description",
            Status = FeedbackIssueStatus.Open,
            Platform = "Windows",
            AppVersion = "1.0.0"
        };

        // Assert
        Assert.Equal("Test Issue", issue.Title);
        Assert.Equal(FeedbackIssueType.Bug, issue.Type);
        Assert.Equal("Test Description", issue.Description);
        Assert.Equal(FeedbackIssueStatus.Open, issue.Status);
        Assert.Equal("Windows", issue.Platform);
        Assert.Equal("1.0.0", issue.AppVersion);
    }

    [Fact]
    public void FeedbackIssueType_Constants_ShouldHaveCorrectValues()
    {
        // Assert
        Assert.Equal("Bug", FeedbackIssueType.Bug);
        Assert.Equal("Feature", FeedbackIssueType.Feature);
    }

    [Fact]
    public void FeedbackIssueStatus_Constants_ShouldHaveCorrectValues()
    {
        // Assert
        Assert.Equal("open", FeedbackIssueStatus.Open);
        Assert.Equal("planned", FeedbackIssueStatus.Planned);
        Assert.Equal("in-progress", FeedbackIssueStatus.InProgress);
        Assert.Equal("shipped", FeedbackIssueStatus.Shipped);
        Assert.Equal("rejected", FeedbackIssueStatus.Rejected);
    }

    [Fact]
    public void FeedbackComment_ShouldStoreTextAndAuthor()
    {
        // Arrange & Act
        var comment = new FeedbackComment
        {
            Text = "This is a test comment",
            AuthorUsername = "testuser"
        };

        // Assert
        Assert.Equal("This is a test comment", comment.Text);
        Assert.Equal("testuser", comment.AuthorUsername);
    }

    [Fact]
    public void FeedbackVote_ShouldStoreUserId()
    {
        // Arrange & Act
        var vote = new FeedbackVote
        {
            UserId = "user123"
        };

        // Assert
        Assert.Equal("user123", vote.UserId);
    }

    [Fact]
    public void FeedbackNotificationSettings_ShouldStorePreferences()
    {
        // Arrange & Act
        var settings = new FeedbackNotificationSettings
        {
            UserId = "user123",
            IssueId = "issue456",
            NotifyOnStatusChange = true,
            NotifyOnComment = false
        };

        // Assert
        Assert.Equal("user123", settings.UserId);
        Assert.Equal("issue456", settings.IssueId);
        Assert.True(settings.NotifyOnStatusChange);
        Assert.False(settings.NotifyOnComment);
    }

    [Theory]
    [InlineData("Bug", true)]
    [InlineData("Feature", true)]
    [InlineData("Invalid", false)]
    public void FeedbackIssueType_ShouldValidateCorrectly(string type, bool isValid)
    {
        // Assert
        var validTypes = new[] { FeedbackIssueType.Bug, FeedbackIssueType.Feature };
        Assert.Equal(isValid, validTypes.Contains(type));
    }

    [Theory]
    [InlineData("open", true)]
    [InlineData("planned", true)]
    [InlineData("in-progress", true)]
    [InlineData("shipped", true)]
    [InlineData("rejected", true)]
    [InlineData("invalid", false)]
    public void FeedbackIssueStatus_ShouldValidateCorrectly(string status, bool isValid)
    {
        // Assert
        var validStatuses = new[]
        {
            FeedbackIssueStatus.Open,
            FeedbackIssueStatus.Planned,
            FeedbackIssueStatus.InProgress,
            FeedbackIssueStatus.Shipped,
            FeedbackIssueStatus.Rejected
        };
        Assert.Equal(isValid, validStatuses.Contains(status));
    }
}
