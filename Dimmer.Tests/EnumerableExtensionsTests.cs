using Dimmer.Utilities.Extensions;
using Xunit;

namespace Dimmer.Tests;

public class EnumerableExtensionsTests
{
    [Fact]
    public void WeightedShuffleInPlace_EmptyList_DoesNotThrow()
    {
        // Arrange
        var list = new List<int>();

        // Act
        list.WeightedShuffleInPlace(x => 1);

        // Assert
        Assert.Empty(list);
    }

    [Fact]
    public void WeightedShuffleInPlace_SingleItem_RemainsUnchanged()
    {
        // Arrange
        var list = new List<int> { 42 };

        // Act
        list.WeightedShuffleInPlace(x => 1);

        // Assert
        Assert.Single(list);
        Assert.Equal(42, list[0]);
    }

    [Fact]
    public void WeightedShuffleInPlace_AllZeroWeights_ProducesEmptyList()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3, 4, 5 };

        // Act
        list.WeightedShuffleInPlace(x => 0);

        // Assert
        Assert.Empty(list);
    }

    [Fact]
    public void WeightedShuffleInPlace_MixedWeights_IncludesOnlyPositiveWeights()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3, 4, 5 };

        // Act - Only even numbers get positive weight
        list.WeightedShuffleInPlace(x => x % 2 == 0 ? x : 0);

        // Assert
        Assert.Equal(2, list.Count);
        Assert.All(list, x => Assert.True(x % 2 == 0));
    }

    [Fact]
    public void WeightedShuffleInPlace_EqualWeights_Randomizes()
    {
        // Arrange
        var list1 = new List<int> { 1, 2, 3, 4, 5 };
        var list2 = new List<int> { 1, 2, 3, 4, 5 };

        // Act
        list1.WeightedShuffleInPlace(x => 10);
        list2.WeightedShuffleInPlace(x => 10);

        // Assert - Both lists should be shuffled, likely different
        Assert.Equal(5, list1.Count);
        Assert.Equal(5, list2.Count);
        
        // Lists should contain same elements (just reordered)
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }.OrderBy(x => x), list1.OrderBy(x => x));
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }.OrderBy(x => x), list2.OrderBy(x => x));
    }

    [Fact]
    public void WeightedShuffleInPlace_DifferentWeights_FavorsHigherWeights()
    {
        // Arrange - Run multiple times to check statistical behavior
        int iterations = 100;
        int highWeightItemFirst = 0;
        
        for (int i = 0; i < iterations; i++)
        {
            var list = new List<int> { 1, 2, 3, 4, 100 }; // 100 has much higher weight
            
            // Act
            list.WeightedShuffleInPlace(x => x); // Weight equals value
            
            // Assert - Check if high-weight item appears first
            if (list[0] == 100)
            {
                highWeightItemFirst++;
            }
        }

        // The item with weight 100 should appear first much more than 20% (random chance)
        double percentage = (double)highWeightItemFirst / iterations;
        Assert.True(percentage > 0.5, 
            $"High weight item appeared first only {percentage:P} of the time");
    }

    [Fact]
    public void WeightedShuffleInPlace_PreservesAllItems()
    {
        // Arrange
        var list = new List<string> { "a", "b", "c", "d", "e" };
        var originalSet = new HashSet<string>(list);

        // Act
        list.WeightedShuffleInPlace(s => s[0]); // Weight by character value

        // Assert - All original items should be present
        Assert.Equal(5, list.Count);
        Assert.Equal(originalSet, new HashSet<string>(list));
    }

    [Fact]
    public void ShuffleInPlace_Randomizes()
    {
        // Arrange
        var list1 = new List<int> { 1, 2, 3, 4, 5 };
        var list2 = new List<int> { 1, 2, 3, 4, 5 };

        // Act
        list1.ShuffleInPlace();
        list2.ShuffleInPlace();

        // Assert
        Assert.Equal(5, list1.Count);
        Assert.Equal(5, list2.Count);
        
        // Both should contain same elements
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }.OrderBy(x => x), list1.OrderBy(x => x));
    }

    [Fact]
    public void Shuffled_ReturnsNewList()
    {
        // Arrange
        var original = new List<int> { 1, 2, 3, 4, 5 };

        // Act
        var shuffled = original.Shuffled();

        // Assert
        Assert.NotSame(original, shuffled);
        Assert.Equal(5, shuffled.Count);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }.OrderBy(x => x), shuffled.OrderBy(x => x));
    }
}
