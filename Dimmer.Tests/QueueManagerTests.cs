using Dimmer.Interfaces;
using Dimmer.Interfaces.Services;
using Xunit;

namespace Dimmer.Tests;

public class QueueManagerTests
{
    [Fact]
    public void Initialize_ShouldSetCorrectPosition()
    {
        // Arrange
        var queueManager = new QueueManager<int>();
        var items = new[] { 1, 2, 3, 4, 5 };

        // Act
        queueManager.Initialize(items, startIndex: 2);

        // Assert
        Assert.Equal(3, queueManager.Current);
        Assert.Equal(5, queueManager.Count);
    }

    [Fact]
    public void Move_ShouldReorderItems()
    {
        // Arrange
        var queueManager = new QueueManager<int>();
        var items = new[] { 1, 2, 3, 4, 5 };
        queueManager.Initialize(items, startIndex: 0);

        // Act - Move item from index 1 to index 3
        queueManager.Move(1, 3);

        // Assert
        var currentItems = queueManager.CurrentItems;
        Assert.Equal(new[] { 1, 3, 4, 2, 5 }, currentItems);
    }

    [Fact]
    public void Move_ShouldUpdateCurrentPosition_WhenCurrentItemMoved()
    {
        // Arrange
        var queueManager = new QueueManager<int>();
        var items = new[] { 1, 2, 3, 4, 5 };
        queueManager.Initialize(items, startIndex: 1); // Current is 2

        // Act - Move current item from index 1 to index 3
        queueManager.Move(1, 3);

        // Assert - Current should still be 2, but at new position
        Assert.Equal(2, queueManager.Current);
    }

    [Fact]
    public void Move_ShouldAdjustCurrentPosition_WhenMovingBeforeCurrent()
    {
        // Arrange
        var queueManager = new QueueManager<int>();
        var items = new[] { 1, 2, 3, 4, 5 };
        queueManager.Initialize(items, startIndex: 3); // Current is 4 at index 3

        // Act - Move item from index 1 to index 4 (after current)
        queueManager.Move(1, 4);

        // Assert - Current position should be decremented
        Assert.Equal(4, queueManager.Current); // Still pointing to same item
    }

    [Fact]
    public void InsertRange_ShouldAddItemsAtCorrectPosition()
    {
        // Arrange
        var queueManager = new QueueManager<int>();
        var items = new[] { 1, 2, 5, 6 };
        queueManager.Initialize(items);

        // Act - Insert 3 and 4 at index 2
        queueManager.InsertRange(new[] { 3, 4 }, 2);

        // Assert
        var currentItems = queueManager.CurrentItems;
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, currentItems);
        Assert.Equal(6, queueManager.Count);
    }

    [Fact]
    public void InsertRange_ShouldAdjustCurrentPosition_WhenInsertingBeforeCurrent()
    {
        // Arrange
        var queueManager = new QueueManager<int>();
        var items = new[] { 1, 2, 5, 6 };
        queueManager.Initialize(items, startIndex: 2); // Current is 5 at index 2

        // Act - Insert items before current
        queueManager.InsertRange(new[] { 3, 4 }, 2);

        // Assert - Current should still be 5, but at new position (index 4)
        Assert.Equal(5, queueManager.Current);
    }

    [Fact]
    public void RemoveAt_ShouldRemoveItemAtIndex()
    {
        // Arrange
        var queueManager = new QueueManager<int>();
        var items = new[] { 1, 2, 3, 4, 5 };
        queueManager.Initialize(items);

        // Act
        queueManager.RemoveAt(2); // Remove 3

        // Assert
        var currentItems = queueManager.CurrentItems;
        Assert.Equal(new[] { 1, 2, 4, 5 }, currentItems);
        Assert.Equal(4, queueManager.Count);
    }

    [Fact]
    public void RemoveAt_ShouldAdjustCurrentPosition_WhenRemovingBeforeCurrent()
    {
        // Arrange
        var queueManager = new QueueManager<int>();
        var items = new[] { 1, 2, 3, 4, 5 };
        queueManager.Initialize(items, startIndex: 3); // Current is 4 at index 3

        // Act - Remove item before current
        queueManager.RemoveAt(1);

        // Assert - Current should still be 4, but at new position (index 2)
        Assert.Equal(4, queueManager.Current);
    }

    [Fact]
    public void RemoveAt_ShouldHandleRemovingCurrentItem()
    {
        // Arrange
        var queueManager = new QueueManager<int>();
        var items = new[] { 1, 2, 3, 4, 5 };
        queueManager.Initialize(items, startIndex: 2); // Current is 3 at index 2

        // Act - Remove current item
        queueManager.RemoveAt(2);

        // Assert - Should move to next available item
        Assert.Equal(4, queueManager.Current);
    }

    [Fact]
    public void Next_ShouldMoveToNextItem()
    {
        // Arrange
        var queueManager = new QueueManager<int>();
        var items = new[] { 1, 2, 3, 4, 5 };
        queueManager.Initialize(items, startIndex: 0);

        // Act
        var next = queueManager.Next();

        // Assert
        Assert.Equal(2, next);
        Assert.Equal(2, queueManager.Current);
    }

    [Fact]
    public void Previous_ShouldMoveToPreviousItem()
    {
        // Arrange
        var queueManager = new QueueManager<int>();
        var items = new[] { 1, 2, 3, 4, 5 };
        queueManager.Initialize(items, startIndex: 2);

        // Act
        var prev = queueManager.Previous();

        // Assert
        Assert.Equal(2, prev);
        Assert.Equal(2, queueManager.Current);
    }

    [Fact]
    public void Clear_ShouldResetQueue()
    {
        // Arrange
        var queueManager = new QueueManager<int>();
        var items = new[] { 1, 2, 3, 4, 5 };
        queueManager.Initialize(items);

        // Act
        queueManager.Clear();

        // Assert
        Assert.Equal(0, queueManager.Count);
        Assert.Equal(default(int), queueManager.Current);
    }

    [Fact]
    public void Move_ShouldThrowException_WhenIndicesOutOfRange()
    {
        // Arrange
        var queueManager = new QueueManager<int>();
        var items = new[] { 1, 2, 3 };
        queueManager.Initialize(items);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => queueManager.Move(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => queueManager.Move(0, 5));
    }

    [Fact]
    public void InsertRange_ShouldThrowException_WhenIndexOutOfRange()
    {
        // Arrange
        var queueManager = new QueueManager<int>();
        var items = new[] { 1, 2, 3 };
        queueManager.Initialize(items);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            queueManager.InsertRange(new[] { 4, 5 }, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            queueManager.InsertRange(new[] { 4, 5 }, 10));
    }

    [Fact]
    public void RemoveAt_ShouldThrowException_WhenIndexOutOfRange()
    {
        // Arrange
        var queueManager = new QueueManager<int>();
        var items = new[] { 1, 2, 3 };
        queueManager.Initialize(items);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => queueManager.RemoveAt(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => queueManager.RemoveAt(5));
    }
}
