using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Interfaces;
public interface IPlayerStateService
{
    public BehaviorSubject<SongModel> CurrentSong { get; }
    public BehaviorSubject<List<SongModel>> AllSongs { get; }
    public void LoadAllSongs(IEnumerable<SongModel> songs);
    public void Dispose();
}
public interface IQueueManager<T>
{
    event Action<int, IReadOnlyList<T>>? BatchEnqueued;
    event Action<int, T>? ItemDequeued;

    void Initialize(IEnumerable<T> items, int startIndex = 0);
    T? Next();
    bool HasNext { get; }
}
