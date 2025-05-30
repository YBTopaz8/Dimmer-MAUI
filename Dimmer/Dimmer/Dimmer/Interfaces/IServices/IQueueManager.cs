namespace Dimmer.Interfaces;


public interface IQueueManager<T> : IDisposable // 1. Implement IDisposable
{
    // 2. Corrected event signatures (passing sender)
    event Action<IQueueManager<T>, int, IReadOnlyList<T>>? BatchEnqueued; // sender, batchId, batch
    event Action<IQueueManager<T>, int, T>? ItemDequeued; // sender, batchId, item

    void Initialize(IEnumerable<T> items, int startIndex = 0);
    List<T> ShuffleQueueInPlace(); // Return type can be void if preferred, List<T> is for convenience
    T? Next();
    T? Previous();
    T? Current { get; }
    T? PeekNext();
    T? PeekPrevious();
    bool HasNext { get; } // True if Count > 0 for a circular queue
    int Count { get; }

    // 3. Add properties assumed/needed by MultiPlaylistPlayer or PlayListMgtFlow
    IReadOnlyList<T> Items { get; }
    int CurrentBatchId { get; } // To get the batch ID associated with the Current item

    void Clear();
}