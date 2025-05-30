using Dimmer.Utilities.Extensions; // For ShuffleInPlace
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dimmer.Interfaces.Services; // Or your actual namespace for implementations

public class QueueManager<T> : IQueueManager<T>
{
    private readonly List<T> _source = new();
    private readonly int _batchSize;
    private int _position;
    private int _currentBatchIdValue; // Renamed to avoid conflict with property

    // Implement the corrected event signatures
    public event Action<IQueueManager<T>, int, IReadOnlyList<T>>? BatchEnqueued;
    public event Action<IQueueManager<T>, int, T>? ItemDequeued;

    public QueueManager(int batchSize = 175)
    {
        if (batchSize <= 0)
            throw new ArgumentException("Batch size must be positive.", nameof(batchSize));
        _batchSize = batchSize;
        _position = -1; // Initialize to -1 to indicate no valid position yet
        _currentBatchIdValue = 0;
    }

    public void Initialize(IEnumerable<T> items, int startIndex = 0)
    {
        ClearSourceAndPosition();
        _source.AddRange(items ?? Enumerable.Empty<T>()); // Handle null items

        if (_source.Any())
        {
            _position = Math.Clamp(startIndex, 0, _source.Count - 1);
            // Initial batch enqueue based on the starting position
            int batchStart = (_position / _batchSize) * _batchSize;
            _currentBatchIdValue = (batchStart / _batchSize) + 1; // Set initial batch ID
            var batch = _source.Skip(batchStart).Take(_batchSize).ToList();
            if (batch.Any())
            {
                // Directly invoke, as this is the first batch for this initialization
                BatchEnqueued?.Invoke(this, _currentBatchIdValue, batch);
            }
        }
        else
        {
            _position = -1; // No items, no valid position
            _currentBatchIdValue = 0;
        }
    }

    public List<T> ShuffleQueueInPlace()
    {
        if (!_source.Any())
            return _source;

        T? currentItem = Current; // Preserve current item if possible
        _source.ShuffleInPlace();

        if (currentItem != null)
        {
            // Find the new index of the (previously) current item
            int newIndexOfCurrent = -1;
            for (int i = 0; i < _source.Count; ++i)
            {
                if (EqualityComparer<T>.Default.Equals(_source[i], currentItem))
                {
                    newIndexOfCurrent = i;
                    break;
                }
            }

            if (newIndexOfCurrent != -1)
            {
                _position = newIndexOfCurrent;
            }
            else // Current item was somehow lost (e.g., if list contained duplicates and only one instance of currentItem was found before shuffle)
            {
                _position = 0; // Default to the start of the shuffled list
            }
        }
        else
        {
            _position = 0; // If no current item before, point to start
        }

        // After shuffle, the batching context changes, re-evaluate current batch
        if (_source.Any())
        {
            int batchStart = (_position / _batchSize) * _batchSize;
            _currentBatchIdValue = (batchStart / _batchSize) + 1;
            var batch = _source.Skip(batchStart).Take(_batchSize).ToList();
            if (batch.Any())
            {
                BatchEnqueued?.Invoke(this, _currentBatchIdValue, batch);
            }
        }
        else
        {
            _currentBatchIdValue = 0;
        }
        return _source;
    }

    private void EnqueueBatchIfNeeded(int oldPosition, int newPosition)
    {
        if (!_source.Any())
            return;

        int oldBatchNumber = oldPosition == -1 ? -1 : (oldPosition / _batchSize);
        int newBatchNumber = newPosition / _batchSize;

        if (newBatchNumber != oldBatchNumber || _currentBatchIdValue == 0) // If moved to a new batch or first time
        {
            int batchStart = newBatchNumber * _batchSize;
            var batch = _source.Skip(batchStart).Take(_batchSize).ToList();
            if (batch.Any())
            {
                _currentBatchIdValue = newBatchNumber + 1; // Batch IDs are 1-based
                BatchEnqueued?.Invoke(this, _currentBatchIdValue, batch);
            }
        }
    }

    public T? Next()
    {
        if (!_source.Any())
            return default;

        int oldPosition = _position;
        _position = (_position + 1) % _source.Count;
        EnqueueBatchIfNeeded(oldPosition, _position);
        var item = _source[_position];
        ItemDequeued?.Invoke(this, _currentBatchIdValue, item);
        return item;
    }

    public T? Previous()
    {
        if (!_source.Any())
            return default;

        int oldPosition = _position;
        _position = (_position - 1 + _source.Count) % _source.Count;
        EnqueueBatchIfNeeded(oldPosition, _position);
        var item = _source[_position];
        ItemDequeued?.Invoke(this, _currentBatchIdValue, item);
        return item;
    }

    public T? Current => (_position >= 0 && _position < _source.Count) ? _source[_position] : default;

    public T? PeekNext() => _source.Any() ? _source[(_position + 1) % _source.Count] : default;

    public T? PeekPrevious() => _source.Any() ? _source[(_position - 1 + _source.Count) % _source.Count] : default;

    public bool HasNext => _source.Any();

    public int Count => _source.Count;

    public IReadOnlyList<T> Items => _source.AsReadOnly();

    public int CurrentBatchId => _currentBatchIdValue;


    private void ClearSourceAndPosition()
    {
        _source.Clear();
        _position = -1;
        _currentBatchIdValue = 0;
    }

    public void Clear()
    {
        ClearSourceAndPosition();
        // Clear event subscribers
        BatchEnqueued = null;
        ItemDequeued = null;
    }

    public void Dispose()
    {
        Clear(); // Ensures event handlers are cleared, preventing leaks
        GC.SuppressFinalize(this);
    }
}