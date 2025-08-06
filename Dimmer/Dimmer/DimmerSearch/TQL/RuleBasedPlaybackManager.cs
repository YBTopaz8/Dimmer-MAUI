using Dimmer.Data.ModelView.DimmerSearch;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch.TQL;

public class RuleBasedPlaybackManager
{

    private readonly Random _random = new();

    // The list of rules defined by the user
    public ObservableCollection<PlaybackRule> Rules { get; } = new();

    // A history of songs played in this session to avoid repeats
    private readonly HashSet<ObjectId> _sessionPlayHistory = new();

    public RuleBasedPlaybackManager()
    {
    }

    /// <summary>
    /// The core method. Finds the next song to play based on the rule set.
    /// </summary>
    /// <param name="currentQueue">The full list of songs available for playback (the frozen snapshot).</param>
    /// <returns>The SongModelView to play next, or null if no song matches any rule.</returns>
    public SongModelView? FindNextSong(IList<SongModelView> currentQueue)
    {
        if (currentQueue == null || !currentQueue.Any())
            return null;

        // --- Iterate through rules by priority ---
        foreach (var rule in Rules.Where(r => r.IsEnabled).OrderBy(r => r.Priority))
        {
            try
            {
                // 1. Parse the rule's query into a predicate
                var ast = new AstParser(rule.Query).Parse();
                var predicate = new AstEvaluator().CreatePredicate(ast);

                // 2. Find all candidate songs in the entire queue that match this rule
                var candidateSongs = currentQueue.Where(predicate).ToList();

                // 3. Filter out songs that have already been played in this session
                var unplayedCandidates = candidateSongs
                    .Where(s => !_sessionPlayHistory.Contains(s.Id))
                    .ToList();

                if (unplayedCandidates.Count!=0)
                {
                    // 4. We found a match! Pick one and return it.
                    var songToPlay = unplayedCandidates[_random.Next(unplayedCandidates.Count)];

                    return songToPlay;
                }
            }
            catch (Exception ex)
            {

                // Silently continue to the next rule
            }
        }

        // --- Fallback: If no rules match, play any random unplayed song ---
        var anyUnplayed = currentQueue.Where(s => !_sessionPlayHistory.Contains(s.Id)).ToList();
        if (anyUnplayed.Count!=0)
        {
            var songToPlay = anyUnplayed[_random.Next(anyUnplayed.Count)];

            return songToPlay;
        }

        // If all songs have been played, clear the history to allow for a new "session"

        ClearSessionHistory();
        // And try one last time
        return FindNextSong(currentQueue);
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
