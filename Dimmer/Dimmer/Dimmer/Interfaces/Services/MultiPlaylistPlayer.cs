using Dimmer.Interfaces.Services.Interfaces;

namespace Dimmer.Interfaces.Services;

public class MultiPlaylistPlayer<T> : IDisposable
{
    private readonly List<IQueueManager<T>> _playlists = new();
    private readonly Random _random = new();
    private IQueueManager<T>? _lastSuccessfullyPlayedPlaylist; // Playlist that last yielded a song
    private int _lastSuccessfullyPlayedPlaylistGlobalIndex = -1; // Index of the above

    // Event for when a new item is selected for playback
    public event Action<int, T, int, bool>? ItemSelectedForPlayback; // playlistIndex, item, batchId (from QM)

    // Event for when a batch is enqueued by an individual QueueManager
    public event Action<int, int, IReadOnlyList<T>>? BatchEnqueuedByQueueManager; // playlistIndex, batchId, batch

    // Event for when all queues are exhausted (no more items to play)
    public event Action? AllQueuesExhausted;

    public MultiPlaylistPlayer() { }


    public int AddPlaylist(IQueueManager<T> playlistManager)
    {
        if (playlistManager == null)
            throw new ArgumentNullException(nameof(playlistManager));
        if (_playlists.Count >= 8)
        {
            Console.WriteLine("MultiPlaylistPlayer: Max playlists reached."); // Replace with proper logging
            return -1;
        }
        _playlists.Add(playlistManager);

        playlistManager.BatchEnqueued += HandleIndividualQueueBatchEnqueued; // Subscribe

        if (_playlists.Count == 1 && playlistManager.Count > 0)
        {
            _lastSuccessfullyPlayedPlaylist = playlistManager;
            _lastSuccessfullyPlayedPlaylistGlobalIndex = 0;
        }
        return _playlists.Count - 1;
    }
    private void HandleIndividualQueueBatchEnqueued(IQueueManager<T> sender, int batchId, IReadOnlyList<T> batch)
    {
        int playlistIndex = _playlists.IndexOf(sender);
        if (playlistIndex != -1)
        {
            BatchEnqueuedByQueueManager?.Invoke(playlistIndex, batchId, batch);
        }
    }

    public void InitializePlaylists(IEnumerable<IEnumerable<T>> playlistSources, int defaultBatchSize = 175)
    {
        Clear();
        if (playlistSources == null)
            return;

        foreach (var sourceItems in playlistSources.Take(8))
        {
            var qm = new QueueManager<T>(defaultBatchSize); // Using concrete class
            qm.Initialize(sourceItems?.ToList() ?? new List<T>()); // Handle null sourceItems or its contents
            AddPlaylist(qm);
        }
    }

    public T? Next(bool randomizeSourcePlaylist = false)
    {
        var activePlaylists = _playlists.Where(p => p.Count > 0).ToList();

        if (activePlaylists.Count==0)
        {
            AllQueuesExhausted?.Invoke();

            return default;
        }

        IQueueManager<T> selectedPlaylist;
        int selectedPlaylistGlobalIndex;

        if (randomizeSourcePlaylist)
        {
            IQueueManager<T>? chosenRandom = null;
            if (activePlaylists.Count > 1 && _lastSuccessfullyPlayedPlaylist != null && activePlaylists.Contains(_lastSuccessfullyPlayedPlaylist))
            {
                var eligibleForRandom = activePlaylists.Where(p => p != _lastSuccessfullyPlayedPlaylist).ToList();
                if (eligibleForRandom.Count!=0)
                    chosenRandom = eligibleForRandom[_random.Next(eligibleForRandom.Count)];
            }
            selectedPlaylist = chosenRandom ?? activePlaylists[_random.Next(activePlaylists.Count)];
            selectedPlaylistGlobalIndex = _playlists.IndexOf(selectedPlaylist);
        }
        else
        {
            int startIndex = (_lastSuccessfullyPlayedPlaylistGlobalIndex == -1 ? 0 : _lastSuccessfullyPlayedPlaylistGlobalIndex + 1) % _playlists.Count;
            selectedPlaylist = activePlaylists[0]; // Fallback
            selectedPlaylistGlobalIndex = _playlists.IndexOf(selectedPlaylist); // Fallback

            for (int i = 0; i < _playlists.Count; i++)
            {
                int currentIndexToTry = (startIndex + i) % _playlists.Count;
                if (currentIndexToTry >= 0 && currentIndexToTry < _playlists.Count && _playlists[currentIndexToTry].Count > 0)
                {
                    selectedPlaylist = _playlists[currentIndexToTry];
                    selectedPlaylistGlobalIndex = currentIndexToTry;
                    break;
                }
            }
        }

        T? item = selectedPlaylist.Next();

        if (item != null)
        {
            _lastSuccessfullyPlayedPlaylist = selectedPlaylist;
            _lastSuccessfullyPlayedPlaylistGlobalIndex = selectedPlaylistGlobalIndex;
            ItemSelectedForPlayback?.Invoke(selectedPlaylistGlobalIndex, item, selectedPlaylist.CurrentBatchId, true);
        }
        else if (!activePlaylists.Any(p => p.Count > 0)) // Check if ALL are now empty
        {
            AllQueuesExhausted?.Invoke();
        }
        return item;
    }

    public T? Previous(bool randomizeSourcePlaylist = false)
    {
        var activePlaylists = _playlists.Where(p => p.Count > 0).ToList();

        if (activePlaylists.Count==0)
        {
            AllQueuesExhausted?.Invoke();
            return default;
        }
        if (_lastSuccessfullyPlayedPlaylist != null)
        {
            int playlistIndexOfLastPlayed = _playlists.IndexOf(_lastSuccessfullyPlayedPlaylist);
            if (playlistIndexOfLastPlayed == -1)
            { // Stale reference
                _lastSuccessfullyPlayedPlaylist = null;
                _lastSuccessfullyPlayedPlaylistGlobalIndex = -1;
                return Next(randomizeSourcePlaylist); // Fallback
            }

            T? item = _lastSuccessfullyPlayedPlaylist.Previous();
            if (item != null)
            {
                ItemSelectedForPlayback?.Invoke(playlistIndexOfLastPlayed, item, _lastSuccessfullyPlayedPlaylist.CurrentBatchId, true);
                return item;
            }
        }
        // Fallback: if no last played, or its Previous() was null, try Next.
        return Next(randomizeSourcePlaylist);
    }

    public T? PeekPrevious(bool randomizeSourcePlaylist = false)
    {
        return _lastSuccessfullyPlayedPlaylist.PeekPrevious();
    }

    public T? PeekNext(bool randomizeSourcePlaylist = false)
    {
        return _lastSuccessfullyPlayedPlaylist.PeekNext();
    }
    public void RemovePlaylistAt(int index)
    {
        if (index < 0 || index >= _playlists.Count)
            return;

        IQueueManager<T> playlistToRemove = _playlists[index];
        playlistToRemove.BatchEnqueued -= HandleIndividualQueueBatchEnqueued; // Unsubscribe

        if (playlistToRemove == _lastSuccessfullyPlayedPlaylist)
        {
            _lastSuccessfullyPlayedPlaylist = null;
            _lastSuccessfullyPlayedPlaylistGlobalIndex = -1;
        }
        else if (_lastSuccessfullyPlayedPlaylistGlobalIndex > index)
        {
            _lastSuccessfullyPlayedPlaylistGlobalIndex--;
        }

        _playlists.RemoveAt(index);
        playlistToRemove.Dispose(); // Call Dispose on the QueueManager

        if (_lastSuccessfullyPlayedPlaylist == null && _playlists.Count!=0)
        {
            int newPotentialIndex = Math.Clamp(_lastSuccessfullyPlayedPlaylistGlobalIndex, 0, Math.Max(0, _playlists.Count - 1));
            if (_playlists.Count > 0)
            { // ensure playlists is not empty after clamp
                _lastSuccessfullyPlayedPlaylist = _playlists[newPotentialIndex];
                _lastSuccessfullyPlayedPlaylistGlobalIndex = newPotentialIndex;
            }
            else
            {
                _lastSuccessfullyPlayedPlaylistGlobalIndex = -1; // no playlists left
            }
        }
    }


    public void ShuffleAllPlaylists()
    {
        foreach (var playlist in _playlists)
            playlist.ShuffleQueueInPlace();
    }


    public void ShufflePlaylist(int playlistIndex)
    {
        if (playlistIndex >= 0 && playlistIndex < _playlists.Count)
        {
            _playlists[playlistIndex].ShuffleQueueInPlace();
        }
    }

    public void Clear()
    {
        foreach (var qm in _playlists)
        {
            qm.BatchEnqueued -= HandleIndividualQueueBatchEnqueued;
            qm.Dispose(); // Calls QueueManager.Dispose which clears its own handlers
        }
        _playlists.Clear();
        _lastSuccessfullyPlayedPlaylist = null;
        _lastSuccessfullyPlayedPlaylistGlobalIndex = -1;
    }

    public int TotalCount => _playlists.Sum(p => p.Count);
    public IReadOnlyList<IQueueManager<T>> Playlists => _playlists.AsReadOnly();

    public void Dispose()
    {
        Clear();
        GC.SuppressFinalize(this);
    }
}

