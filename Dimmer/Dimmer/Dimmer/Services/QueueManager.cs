using Dimmer.Utilities.Extensions;

namespace Dimmer.Services;
/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
/// <seealso cref="Dimmer.Interfaces.IQueueManager&lt;T&gt;" />
public class QueueManager<T> : IQueueManager<T>
{
    /// <summary>
    /// The source
    /// </summary>
    private readonly List<T> _source = new();
    /// <summary>
    /// The batch size
    /// </summary>
    private readonly int _batchSize=175;
    /// <summary>
    /// The position
    /// </summary>
    private int _position;
    /// <summary>
    /// The current batch identifier
    /// </summary>
    private int _currentBatchId;

    /// <summary>
    /// Occurs when [batch enqueued].
    /// </summary>
    public event Action<int, IReadOnlyList<T>>? BatchEnqueued;
    /// <summary>
    /// Occurs when [item dequeued].
    /// </summary>
    public event Action<int, T>? ItemDequeued;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueueManager{T}"/> class.
    /// </summary>
    /// <param name="batchSize">Size of the batch.</param>
    /// <exception cref="System.ArgumentException">Queue Manager threw an Arg Exception - batchSize</exception>
    public QueueManager(int batchSize = 175)
    {
        if (batchSize <= 0)
            throw new ArgumentException("Queue Manager threw an Arg Exception", nameof(batchSize));
        _batchSize = batchSize;
    }

    /// <summary>
    /// Initializes the specified items.
    /// </summary>
    /// <param name="items">The items.</param>
    /// <param name="startIndex">The start index.</param>
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
    
    public List<T> ShuffleQueueInPlace()
    {
        _source.ShuffleInPlace();
        var startIndex = _source.FindIndex(s =>
            s.Equals(Current));

        EnqueueBatchIfNeeded(startIndex);
        return _source;
    }

    /// <summary>
    /// Enqueues the batch if needed.
    /// </summary>
    /// <param name="position">The position.</param>
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
    /// <summary>
    /// Nexts this instance.
    /// </summary>
    /// <returns></returns>
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
    /// <summary>
    /// Previouses this instance.
    /// </summary>
    /// <returns></returns>
    public T? Previous()
    {
        if (_source.Count == 0)
            return Current;
        _position = (_position - 1 + _source.Count) % _source.Count;
        EnqueueBatchIfNeeded(_position);
        var item = _source[_position];
        ItemDequeued?.Invoke(_currentBatchId, item);
        return item;
    }

    // the currently “pointed‐at” item
    /// <summary>
    /// Gets the current.
    /// </summary>
    /// <value>
    /// The current.
    /// </value>
    public T? Current =>
        (_source.Count > 0 && _position < _source.Count)
        ? _source[_position]
        : default;

    // look ahead/behind without changing state
    /// <summary>
    /// Peeks the next.
    /// </summary>
    /// <returns></returns>
    public T? PeekNext()
    {
        return _source.Count == 0
        ? default
        : _source[(_position + 1) % _source.Count];
    }

    /// <summary>
    /// Peeks the previous.
    /// </summary>
    /// <returns></returns>
    public T? PeekPrevious()
    {
        return _source.Count == 0
        ? default
        : _source[(_position - 1 + _source.Count) % _source.Count];
    }

    /// <summary>
    /// Gets a value indicating whether this instance has next.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance has next; otherwise, <c>false</c>.
    /// </value>
    public bool HasNext => _source.Count > 0;
    /// <summary>
    /// Gets the count.
    /// </summary>
    /// <value>
    /// The count.
    /// </value>
    public int Count => _source.Count;

    // reset completely
    /// <summary>
    /// Clears this instance.
    /// </summary>
    public void Clear()
    {
        _source.Clear();
        _position = 0;
        _currentBatchId = 0;
    }
}