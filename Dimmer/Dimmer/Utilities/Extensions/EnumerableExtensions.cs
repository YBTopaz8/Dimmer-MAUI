namespace Dimmer.Utilities.Extensions;

public static class EnumerableExtensions
{
    // Thread-safe Random per thread
    private static readonly ThreadLocal<Random> _rng = new(() => new Random());

    /// <summary>
    /// Shuffles the list in place using Fisher–Yates.
    /// </summary>
    public static void ShuffleInPlace<T>(this IList<T> list)
    {
        var rng = _rng.Value;
        if (list == null || list.Count <= 1)
            return; // No need to shuffle if the list is null or has one or no elements
        if (rng is null)
            return; // Ensure rng is not null
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// Shuffles the list in place using weighted random sampling.
    /// Items with higher weights are more likely to appear earlier in the shuffled list.
    /// </summary>
    /// <typeparam name="T">Type of items in the list</typeparam>
    /// <param name="list">List to shuffle</param>
    /// <param name="weightCalculator">Function to calculate weight for each item</param>
    public static void WeightedShuffleInPlace<T>(this IList<T> list, Func<T, int> weightCalculator)
    {
        if (list == null || list.Count <= 1)
            return;

        var rng = _rng.Value;
        if (rng is null)
            return;

        // Create a temporary list with weights
        var itemsWithWeights = new List<(T item, int weight)>(list.Count);
        foreach (var item in list)
        {
            var weight = weightCalculator(item);
            if (weight > 0)
            {
                itemsWithWeights.Add((item, weight));
            }
        }

        // Clear the original list
        list.Clear();

        // Perform weighted random selection
        while (itemsWithWeights.Count > 0)
        {
            // Calculate total weight
            long totalWeight = 0;
            foreach (var (_, weight) in itemsWithWeights)
            {
                totalWeight += weight;
            }

            if (totalWeight == 0)
            {
                // All remaining items have 0 weight, add them in order
                foreach (var (item, _) in itemsWithWeights)
                {
                    list.Add(item);
                }
                break;
            }

            // Pick a random number in [0, totalWeight)
            var randomValue = (long)(rng.NextDouble() * totalWeight);

            // Find which item this maps to
            long cumulativeWeight = 0;
            int selectedIndex = -1;
            for (int i = 0; i < itemsWithWeights.Count; i++)
            {
                cumulativeWeight += itemsWithWeights[i].weight;
                if (randomValue < cumulativeWeight)
                {
                    selectedIndex = i;
                    break;
                }
            }

            // Add selected item to result and remove from remaining
            if (selectedIndex >= 0)
            {
                list.Add(itemsWithWeights[selectedIndex].item);
                itemsWithWeights.RemoveAt(selectedIndex);
            }
            else
            {
                // Fallback: shouldn't happen but take last if calculation error
                list.Add(itemsWithWeights[^1].item);
                itemsWithWeights.RemoveAt(itemsWithWeights.Count - 1);
            }
        }
    }

    /// <summary>
    /// Returns a new list containing the elements of source in shuffled order.
    /// </summary>
    public static List<T> Shuffled<T>(this IEnumerable<T> source)
    {
        var buffer = source.ToList();
        buffer.ShuffleInPlace();
        return buffer;
    }
}
