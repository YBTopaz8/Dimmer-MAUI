using Dimmer.Data.ModelView;
using Dimmer.DimmerLive.Interfaces;
using Dimmer.DimmerLive.Models;
using Dimmer.ViewModel;

using Microsoft.Extensions.Logging;

using Moq;

using Xunit;

namespace Dimmer.Tests;

/// <summary>
/// Unit tests for song comments functionality
/// </summary>
public class SongCommentsTests
{
    private readonly Mock<ISongCommentService> _mockCommentService;
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<BaseViewModel> _mockBaseViewModel;
    private readonly Mock<ILogger<SongCommentsViewModel>> _mockLogger;

    public SongCommentsTests()
    {
        _mockCommentService = new Mock<ISongCommentService>();
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockBaseViewModel = new Mock<BaseViewModel>();
        _mockLogger = new Mock<ILogger<SongCommentsViewModel>>();
    }

    [Fact]
    public void SongCommentView_TimestampDisplay_FormatsCorrectly()
    {
        // Arrange
        var comment = new Mock<SongComment>();
        comment.Setup(c => c.TimestampMs).Returns(92340); // 1:32.340
        comment.Setup(c => c.Text).Returns("Test comment");
        comment.Setup(c => c.IsPublic).Returns(true);
        comment.Setup(c => c.ObjectId).Returns("testId");
        comment.Setup(c => c.CreatedAt).Returns(DateTimeOffset.UtcNow);

        // Act
        var view = new SongCommentView(comment.Object);

        // Assert
        Assert.Equal("01:32", view.TimestampDisplay);
    }

    [Fact]
    public void SongCommentView_TimestampDisplay_HandlesHours()
    {
        // Arrange
        var comment = new Mock<SongComment>();
        comment.Setup(c => c.TimestampMs).Returns(3692340); // 1:01:32.340
        comment.Setup(c => c.Text).Returns("Test comment");
        comment.Setup(c => c.IsPublic).Returns(true);
        comment.Setup(c => c.ObjectId).Returns("testId");
        comment.Setup(c => c.CreatedAt).Returns(DateTimeOffset.UtcNow);

        // Act
        var view = new SongCommentView(comment.Object);

        // Assert
        Assert.Equal("01:01:32", view.TimestampDisplay);
    }

    [Fact]
    public void SongCommentView_TimestampDisplay_HandlesNull()
    {
        // Arrange
        var comment = new Mock<SongComment>();
        comment.Setup(c => c.TimestampMs).Returns((int?)null);
        comment.Setup(c => c.Text).Returns("Test comment");
        comment.Setup(c => c.IsPublic).Returns(true);
        comment.Setup(c => c.ObjectId).Returns("testId");
        comment.Setup(c => c.CreatedAt).Returns(DateTimeOffset.UtcNow);

        // Act
        var view = new SongCommentView(comment.Object);

        // Assert
        Assert.Equal(string.Empty, view.TimestampDisplay);
    }

    [Fact]
    public void SongCommentView_TotalReactions_SumsCorrectly()
    {
        // Arrange
        var comment = new Mock<SongComment>();
        comment.Setup(c => c.Reactions).Returns(new Dictionary<string, int>
        {
            { "like", 12 },
            { "fire", 3 },
            { "sad", 1 }
        });
        comment.Setup(c => c.Text).Returns("Test comment");
        comment.Setup(c => c.IsPublic).Returns(true);
        comment.Setup(c => c.ObjectId).Returns("testId");
        comment.Setup(c => c.CreatedAt).Returns(DateTimeOffset.UtcNow);

        // Act
        var view = new SongCommentView(comment.Object);

        // Assert
        Assert.Equal(16, view.TotalReactions);
    }

    [Fact]
    public void UserNoteModelView_TimestampDisplay_FormatsCorrectly()
    {
        // Arrange
        var note = new UserNoteModelView
        {
            TimestampMs = 92340, // 1:32.340
            UserMessageText = "Test note"
        };

        // Act
        var display = note.TimestampDisplay;

        // Assert
        Assert.Equal("01:32", display);
    }

    [Fact]
    public void UserNoteModelView_TotalReactions_HandlesNull()
    {
        // Arrange
        var note = new UserNoteModelView
        {
            Reactions = null,
            UserMessageText = "Test note"
        };

        // Act
        var total = note.TotalReactions;

        // Assert
        Assert.Equal(0, total);
    }

    [Fact]
    public void UserNoteModelView_TotalReactions_HandlesEmpty()
    {
        // Arrange
        var note = new UserNoteModelView
        {
            Reactions = new Dictionary<string, int>(),
            UserMessageText = "Test note"
        };

        // Act
        var total = note.TotalReactions;

        // Assert
        Assert.Equal(0, total);
    }
}
