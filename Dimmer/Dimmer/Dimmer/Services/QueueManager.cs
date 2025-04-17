using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Services;

public class QueueManager<T> : IQueueManager<T>
{
    private readonly Queue<T> _queue = new();
    private List<T> _source = new();
    private readonly int _batchSize;
    private int _nextBatchStart;
    private int _currentBatchId;

    public event Action<int, IReadOnlyList<T>>? BatchEnqueued;
    public event Action<int, T>? ItemDequeued;

    public QueueManager(int batchSize = 25)
    {
        if (batchSize <= 0)
            throw new ArgumentException(nameof(batchSize));
        _batchSize = batchSize;
    }

    public void Initialize(IEnumerable<T> items, int startIndex = 0)
    {
        _source = items.ToList();
        _queue.Clear();
        _nextBatchStart = Math.Clamp(startIndex, 0, _source.Count);
        EnqueueNextBatch();
    }

    private void EnqueueNextBatch()
    {
        if (_nextBatchStart >= _source.Count)
            return;

        var batchItems = _source
            .Skip(_nextBatchStart)
            .Take(_batchSize)
            .ToList();

        foreach (var item in batchItems)
            _queue.Enqueue(item);

        _currentBatchId++;
        _nextBatchStart += batchItems.Count;

        // notify that a new batch arrived
        BatchEnqueued?.Invoke(_currentBatchId, batchItems);
    }

    public T? Next()
    {
        if (_queue.Count == 0 && _nextBatchStart < _source.Count)
            EnqueueNextBatch();

        if (_queue.Count == 0)
            return default;

        var item = _queue.Dequeue();
        // notify that an item was dequeued (i.e. “now playing”)
        ItemDequeued?.Invoke(_currentBatchId, item);
        return item;
    }

    public bool HasNext =>
        _queue.Count > 0 || _nextBatchStart < _source.Count;
}
