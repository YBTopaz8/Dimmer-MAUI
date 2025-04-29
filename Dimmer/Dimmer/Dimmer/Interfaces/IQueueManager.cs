using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Interfaces;
public interface IQueueManager<T>
{
    /// <summary>
    /// Occurs when [batch enqueued].
    /// </summary>
    event Action<int, IReadOnlyList<T>>? BatchEnqueued;
    /// <summary>
    /// Occurs when [item dequeued].
    /// </summary>
    event Action<int, T>? ItemDequeued;

    /// <summary>
    /// Initializes the specified items.
    /// </summary>
    /// <param name="items">The items.</param>
    /// <param name="startIndex">The start index.</param>
    void Initialize(IEnumerable<T> items, int startIndex = 0);
    /// <summary>
    /// Nexts this instance.
    /// </summary>
    /// <returns></returns>
    T? Next();
    /// <summary>
    /// Previouses this instance.
    /// </summary>
    /// <returns></returns>
    T? Previous();
    /// <summary>
    /// Clears this instance.
    /// </summary>
    void Clear();
    /// <summary>
    /// Peeks the previous.
    /// </summary>
    /// <returns></returns>
    T? PeekPrevious();
    /// <summary>
    /// Peeks the next.
    /// </summary>
    /// <returns></returns>
    T? PeekNext();
    List<T> ShuffleQueueInPlace();

    /// <summary>
    /// Gets a value indicating whether this instance has next.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance has next; otherwise, <c>false</c>.
    /// </value>
    bool HasNext { get; }
    /// <summary>
    /// Gets the count.
    /// </summary>
    /// <value>
    /// The count.
    /// </value>
    int Count { get; }
    /// <summary>
    /// Gets the current.
    /// </summary>
    /// <value>
    /// The current.
    /// </value>
    T? Current { get; }
}