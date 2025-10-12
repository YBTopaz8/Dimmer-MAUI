using System.Data;

namespace Dimmer.DimmerSearch.TQL;
public class RuleBasedPlaybackManager
{
    private readonly IRealmFactory _realmFactory;
    private readonly IMapper _mapper;
    private readonly Random _random = new();
    private readonly HashSet<ObjectId> _sessionPlayHistory = new();

    public RuleBasedPlaybackManager(IRealmFactory realmFactory, IMapper mapper)
    {
        _realmFactory = realmFactory;
        _mapper = mapper;
    }
    private int _recursionDepth = 0;
    private const int MAX_RECURSION_DEPTH = 3;
    /// <summary>
    /// The core method. Finds the next song to play based on the provided rule set.
    /// </summary>
    /// <param name="rules">The collection of playback rules to evaluate.</param>
    /// <returns>The SongModelView to play next, or null if no song matches any rule.</returns>
    public SongModelView? FindNextSong(IEnumerable<PlaybackRule> rules) // <-- MODIFIED SIGNATURE
    {
        if (_recursionDepth >= MAX_RECURSION_DEPTH)
        {
            _recursionDepth = 0;
            return null; // Prevent infinite recursion
        }
        var realm = _realmFactory.GetRealmInstance();

        // --- FIXED: The loop now iterates over the 'rules' parameter ---
        foreach (var rule in rules.Where(r => r.IsEnabled).OrderBy(r => r.Priority))
        {
            try
            {
                var plan = MetaParser.Parse(rule.Query);
                if (plan.ErrorMessage != null)
                    continue;

                IQueryable<SongModel> candidatesQuery = realm.All<SongModel>().Filter(plan.RqlFilter);

                var candidateSongs = candidatesQuery.ToList()
                    .Where(s => !_sessionPlayHistory.Contains(s.Id))
                    .ToList();

                if (candidateSongs.Count!=0)
                {
                    var songToPlayModel = candidateSongs[_random.Next(candidateSongs.Count)];
                    _recursionDepth = 0;
                    return _mapper.Map<SongModelView>(songToPlayModel);
                }
            }
            catch (Exception ex)
            {
                // Silently continue to the next rule
                System.Diagnostics.Debug.WriteLine($"Error processing playback rule '{rule.Query}': {ex.Message}");
            }
        }
        var anyUnplayedIds = realm.All<SongModel>()
            .Select(s => s.Id)
            .ToList()
            .Except(_sessionPlayHistory)
            .ToList();

        if (anyUnplayedIds.Count!=0)
        {
            var randomId = anyUnplayedIds[_random.Next(anyUnplayedIds.Count)];
            var songToPlay = realm.Find<SongModel>(randomId);
            return _mapper.Map<SongModelView>(songToPlay);
        }

        ClearSessionHistory();
        return FindNextSong(rules); // Pass the rules along in the recursive call
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
