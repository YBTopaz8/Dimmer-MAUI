using Dimmer.Utilities.Enums;
using Xunit;

namespace Dimmer.Tests;

/// <summary>
/// Tests for the PlaybackAction enum to ensure all expected values are defined
/// and can be used for playback queue management.
/// </summary>
public class PlaybackActionTests
{
    [Fact]
    public void PlaybackAction_ShouldHavePlayNextValue()
    {
        // Arrange & Act
        var action = PlaybackAction.PlayNext;

        // Assert
        Assert.Equal(PlaybackAction.PlayNext, action);
        Assert.Equal(0, (int)action);
    }

    [Fact]
    public void PlaybackAction_ShouldHavePlayNowValue()
    {
        // Arrange & Act
        var action = PlaybackAction.PlayNow;

        // Assert
        Assert.Equal(PlaybackAction.PlayNow, action);
        Assert.Equal(1, (int)action);
    }

    [Fact]
    public void PlaybackAction_ShouldHaveAddToQueueValue()
    {
        // Arrange & Act
        var action = PlaybackAction.AddToQueue;

        // Assert
        Assert.Equal(PlaybackAction.AddToQueue, action);
        Assert.Equal(2, (int)action);
    }

    [Fact]
    public void PlaybackAction_ShouldHaveJumpInQueueValue()
    {
        // Arrange & Act
        var action = PlaybackAction.JumpInQueue;

        // Assert
        Assert.Equal(PlaybackAction.JumpInQueue, action);
        Assert.Equal(3, (int)action);
    }

    [Fact]
    public void PlaybackAction_ShouldHaveReplaceQueueValue()
    {
        // Arrange & Act
        var action = PlaybackAction.ReplaceQueue;

        // Assert
        Assert.Equal(PlaybackAction.ReplaceQueue, action);
        Assert.Equal(4, (int)action);
    }

    [Fact]
    public void PlaybackAction_ShouldHaveExpectedNumberOfValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<PlaybackAction>();

        // Assert
        Assert.Equal(5, values.Length);
    }

    [Theory]
    [InlineData(PlaybackAction.PlayNext, "PlayNext")]
    [InlineData(PlaybackAction.PlayNow, "PlayNow")]
    [InlineData(PlaybackAction.AddToQueue, "AddToQueue")]
    [InlineData(PlaybackAction.JumpInQueue, "JumpInQueue")]
    [InlineData(PlaybackAction.ReplaceQueue, "ReplaceQueue")]
    public void PlaybackAction_ShouldHaveCorrectStringRepresentation(PlaybackAction action, string expected)
    {
        // Act
        var result = action.ToString();

        // Assert
        Assert.Equal(expected, result);
    }
}
