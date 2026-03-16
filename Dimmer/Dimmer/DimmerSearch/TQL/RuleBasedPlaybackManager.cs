using System.Data;

namespace Dimmer.DimmerSearch.TQL;
public class RuleBasedPlaybackManager
{
    private readonly IRealmFactory _realmFactory;
    private readonly Random _random = new();
    private readonly HashSet<ObjectId> _sessionPlayHistory = new();

    public RuleBasedPlaybackManager(IRealmFactory realmFactory)
    {
        _realmFactory = realmFactory;
    }
    private int _recursionDepth = 0;
    private const int MAX_RECURSION_DEPTH = 3;
    /// <summary>
    /// The core method. Finds the next song to play based on the provided rule set.
    /// </summary>
    /// <param name="rules">The collection of playback rules to evaluate.</param>
    /// <returns>The SongModelView to play next, or null if no song matches any rule.</returns>
    public SongModelView? FindNextSong(IEnumerable<PlaybackRule> rules)
    {
        if (_recursionDepth >= MAX_RECURSION_DEPTH)
        {
            _recursionDepth = 0;
            return null; // Prevent infinite recursion
        }
        var realm = _realmFactory.GetRealmInstance();

        foreach (var rule in rules.Where(r => r.IsEnabled).OrderBy(r => r.Priority))
        {
            try
            {
                var plan = MetaParser.Parse(rule.Query);
                if (plan.ErrorMessage != null)
                    continue;

                // Get candidates from Realm
                IQueryable<SongModel> candidatesQuery = realm.All<SongModel>().Filter(plan.RqlFilter);
                var candidates = candidatesQuery.ToList();

                // Apply shuffle if present
                if (plan.Shuffle != null)
                {
                    candidates = ApplyShuffle(candidates, plan.Shuffle);
                }

                // Apply in-memory predicate
                if (plan.InMemoryPredicate != null)
                {
                    candidates = candidates
                        .Where(s => plan.InMemoryPredicate(s.ToSongModelView()))
                        .ToList();
                }

                // Filter out session history
                var availableCandidates = candidates
                    .Where(s => !_sessionPlayHistory.Contains(s.Id))
                    .ToList();

                if (availableCandidates.Count != 0)
                {
                    var songToPlayModel = availableCandidates[_random.Next(availableCandidates.Count)];
                    _recursionDepth = 0;
                    return songToPlayModel.ToSongModelView();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing playback rule '{rule.Query}': {ex.Message}");
            }
        }

        // Fallback to any unplayed song
        var anyUnplayedIds = realm.All<SongModel>()
            .Select(s => s.Id)
            .ToList()
            .Except(_sessionPlayHistory)
            .ToList();

        if (anyUnplayedIds.Count != 0)
        {
            var randomId = anyUnplayedIds[_random.Next(anyUnplayedIds.Count)];
            var songToPlay = realm.Find<SongModel>(randomId);
            return songToPlay.ToSongModelView();
        }

        ClearSessionHistory();
        return FindNextSong(rules);
    }

    private List<SongModel> ApplyShuffle(List<SongModel> songs, ShuffleNode shuffleNode)
    {
        if (shuffleNode == null) return songs;

        if (shuffleNode.IsBiased && shuffleNode.BiasField != null)
        {
            // Biased shuffle - sort by bias field first, then randomize within groups
            var propertyName = shuffleNode.BiasField.PropertyName;
            var property = typeof(SongModel).GetProperty(propertyName);

            if (property != null)
            {
                // Group by the bias field's value and shuffle within groups
                var grouped = songs
                    .GroupBy(s => property.GetValue(s) ?? "null")
                    .SelectMany(g => g.OrderBy(_ => _random.Next()))
                    .ToList();

                // Apply direction if needed (simplified for now)
                if (shuffleNode.BiasDirection == SortDirection.Descending)
                {
                    grouped.Reverse();
                }

                return shuffleNode.Count == int.MaxValue
                    ? grouped
                    : grouped.Take(shuffleNode.Count).ToList();
            }
        }

        // Pure random shuffle
        var shuffled = songs.OrderBy(_ => _random.Next()).ToList();
        return shuffleNode.Count == int.MaxValue
            ? shuffled
            : shuffled.Take(shuffleNode.Count).ToList();
    }
  
    public void AddSongToHistory(SongModelView song)
    {
        if (song != null)
        {
            _sessionPlayHistory.Add(song.Id);
        }
    }

    public void ClearSessionHistory()
    {
        _sessionPlayHistory.Clear();
    }
}
