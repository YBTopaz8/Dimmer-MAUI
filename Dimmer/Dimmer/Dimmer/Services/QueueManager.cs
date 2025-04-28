namespace Dimmer.Services;
public class QueueManager<T> : IQueueManager<T>
{
    private readonly List<T> _source = new();
    private readonly int _batchSize;
    private int _position;
    private int _currentBatchId;

    public event Action<int, IReadOnlyList<T>>? BatchEnqueued;
    public event Action<int, T>? ItemDequeued;

    public QueueManager(int batchSize = 25)
    {
        if (batchSize <= 0)
            throw new ArgumentException("Queue Manager threw an Arg Exception", nameof(batchSize));
        _batchSize = batchSize;
    }

    public void Initialize(IEnumerable<T> items, int startIndex = 0)
    {
        _source.Clear();
        _source.AddRange(items);
        if (_source.Count > 0)
            _position = Math.Clamp(startIndex, 0, _source.Count - 1);
        else
            _position = 0;
        _currentBatchId = 0;
        EnqueueBatchIfNeeded(_position);
    }

    private void EnqueueBatchIfNeeded(int position)
    {
        if (_source.Count == 0)
            return;
        if (position % _batchSize == 0)
        {
            var batch = _source.Skip(position).Take(_batchSize).ToList();
            if (batch.Count > 0)
            {
                _currentBatchId++;
                BatchEnqueued?.Invoke(_currentBatchId, batch);
            }
        }
    }

    // move forward, wrap to 0, then fire events
    public T? Next()
    {
        if (_source.Count == 0)
            return default;
        _position = (_position + 1) % _source.Count;
        EnqueueBatchIfNeeded(_position);
        var item = _source[_position];
        ItemDequeued?.Invoke(_currentBatchId, item);
        return item;
    }

    // move backward, wrap to last, then fire events
    public T? Previous()
    {
        if (_source.Count == 0)
            return default;
        _position = (_position - 1 + _source.Count) % _source.Count;
        EnqueueBatchIfNeeded(_position);
        var item = _source[_position];
        ItemDequeued?.Invoke(_currentBatchId, item);
        return item;
    }

    // the currently “pointed‐at” item
    public T? Current =>
        (_source.Count > 0 && _position < _source.Count)
        ? _source[_position]
        : default;

    // look ahead/behind without changing state
    public T? PeekNext()
    {
        return _source.Count == 0
        ? default
        : _source[(_position + 1) % _source.Count];
    }

    public T? PeekPrevious()
    {
        return _source.Count == 0
        ? default
        : _source[(_position - 1 + _source.Count) % _source.Count];
    }

    public bool HasNext => _source.Count > 0;
    public int Count => _source.Count;

    // reset completely
    public void Clear()
    {
        _source.Clear();
        _position = 0;
        _currentBatchId = 0;
    }
}