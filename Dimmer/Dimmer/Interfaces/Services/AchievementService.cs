using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Interfaces.Services;

public class AchievementService : IDisposable
{
    private readonly IRealmFactory _realmFactory;
    private IDisposable _playEventToken;
    private readonly Realm _realm;
    private IQueryable<DimmerPlayEvent> _playEvents;
    private List<AchievementRule> _rules;



        public AchievementService(IRealmFactory realmFactory)
        {
        //    _realmFactory = realmFactory;
        //    _realm = _realmFactory.GetRealmInstance();
        //    _rules = BuildAllRules(); // The big list of 40
        //    EnsureStatsExist();
        //InitializeReactiveListeners();
    }

    public List<AchievementRule> GetAllRules()
    {
        return _rules; // Return the master list
    }

    public void DEBUG_SimulateStreak(int days)
    {
        var stats = _realm.Find<UserStats>("Global");
        _realm.Write(() =>
        {
            stats.DailyPlayStreak = days;
            stats.LastPlayDate = DateTimeOffset.UtcNow;
        });
       
        
    }

    private void ProcessNewEvent(DimmerPlayEvent playEvent)
    {
        // 1. FILTER: Ignore irrelevant events immediately to save CPU
        var playType = (PlayType)playEvent.PlayType;
        if (playType != PlayType.Completed &&
            playType != PlayType.Skipped &&
            playType != PlayType.Favorited)
        {
            return;
        }

        // 2. DATA INTEGRITY: Ensure Song and Stats exist
        if (!playEvent.SongId.HasValue) return;

        var song = _realm.Find<SongModel>(playEvent.SongId.Value);
        var stats = _realm.Find<UserStats>("Global");

        if (song == null || stats == null) return;

        // 3. UPDATE STATS (Write Transaction)
        _realm.Write(() =>
        {
            UpdateStatsLogic(stats, playType);
        });

        // 4. CHECK ACHIEVEMENTS (Read Only Logic)
        CheckRules(stats, song, playType);
    }


    // Moved the logic here to keep the Write block clean
    private void UpdateStatsLogic(UserStats stats, PlayType type)
    {
        // 1. Global Counters
        if (type == PlayType.Completed) stats.TotalPlays++;
        if (type == PlayType.Skipped) stats.TotalSkips++;
        if (type == PlayType.Favorited) stats.TotalFavorites++;

        // 2. Daily Streak Logic
        if (type == PlayType.Completed)
        {
            var today = DateTimeOffset.UtcNow.Date;
            if (stats.LastPlayDate.HasValue)
            {
                var lastDate = stats.LastPlayDate.Value.Date;
                if (lastDate == today.AddDays(-1))
                {
                    // Continued from yesterday
                    stats.DailyPlayStreak++;
                }
                else if (lastDate < today.AddDays(-1))
                {
                    // Missed a day
                    stats.DailyPlayStreak = 1;
                }
                // If lastDate == today, do nothing (already counted)
            }
            else
            {
                stats.DailyPlayStreak = 1;
            }
            stats.LastPlayDate = DateTimeOffset.UtcNow;
        }
    }

    private void CheckRules(UserStats stats, SongModel song, PlayType type)
    {
        var songEarnedSet = new HashSet<AchievementRule>(song.EarnedAchievementIds);
        var globalEarnedSet = new HashSet<AchievementRule>(stats.UnlockedGlobalAchievements);

        foreach (var rule in _rules)
        {
            // ---------------------------------------------------------
            // 1. OPTIMIZATION: Skip if already earned
            // ---------------------------------------------------------
            if (IsSongSpecific((AchievementCategory)rule.Category))
            {
                if (songEarnedSet.Contains(rule)) continue;
            }
            else
            {
                if (globalEarnedSet.Contains(rule)) continue;
            }

            // ---------------------------------------------------------
            // 2. LOGIC FIX: Gatekeeper - Ignore rules irrelevant to this event
            // ---------------------------------------------------------
            if (!IsRuleRelevantForEvent(rule, type))
            {
                continue;
            }

            bool passed = false;

            try
            {
                switch ((AchievementCategory)rule.Category)
                {
                    case AchievementCategory.SongCount:
                        // We already know type == Completed because of Gatekeeper
                        passed = song.PlayCount >= rule.Threshold;
                        break;

                    case AchievementCategory.SongSkip:
                        passed = song.SkipCount >= rule.Threshold;
                        break;

                    case AchievementCategory.SongFav:
                        passed = song.IsFavorite;
                        break;

                    case AchievementCategory.GlobalCount:
                        // Specific logic to distinguish Skips/Favs/Plays within "Global" category
                        if (rule.Id.Contains("SKIP"))
                        {
                            if (type == PlayType.Skipped) passed = stats.TotalSkips >= rule.Threshold;
                        }
                        else if (rule.Id.Contains("FAV") || rule.Id.Contains("COLLECTOR"))
                        {
                            if (type == PlayType.Favorited) passed = stats.TotalFavorites >= rule.Threshold;
                        }
                        else
                        {
                            // Default to Play Count
                            if (type == PlayType.Completed) passed = stats.TotalPlays >= rule.Threshold;
                        }

                        // Run Custom check if standard threshold passed (or if no threshold)
                        if (rule.CustomCheck != null)
                        {
                            // Only run custom check if the basic type check above didn't fail us
                            // (Logic: If it's a play rule, don't run custom check if we just favorited)
                            passed = passed && rule.CustomCheck(_realm, stats, song);
                        }
                        break;

                    default:
                        // Streak, Misc, Album, Artist
                        // The Gatekeeper ensures we only run expensive CustomChecks 
                        // on the correct event type.
                        if (rule.CustomCheck != null)
                        {
                            passed = rule.CustomCheck(_realm, stats, song);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking rule {rule.Id}: {ex.Message}");
            }

            if (passed)
            {
                Unlock(stats, song, rule);
                if (IsSongSpecific((AchievementCategory)rule.Category)) songEarnedSet.Add(rule);
                else globalEarnedSet.Add(rule);
            }
        }
    }
    private bool IsRuleRelevantForEvent(AchievementRule rule, PlayType type)
    {
        var cat = (AchievementCategory)rule.Category;

        switch (cat)
        {
            // --- PLAY EVENTS ---
            case AchievementCategory.SongCount:
            case AchievementCategory.AlbumCount:
            case AchievementCategory.ArtistCount:
            case AchievementCategory.Streak:
                return type == PlayType.Completed;

            // --- FAVORITE EVENTS ---
            case AchievementCategory.SongFav:
            case AchievementCategory.AlbumFav:
            case AchievementCategory.ArtistFav:
                return type == PlayType.Favorited;

            // --- SKIP EVENTS ---
            case AchievementCategory.SongSkip:
                return type == PlayType.Skipped;

            // --- MIXED / SPECIAL CASES ---
            case AchievementCategory.GlobalCount:
                // "Global" is messy, it contains Plays, Skips, and Favs.
                // We differentiate by ID string convention used in your BuildAllRules
                if (rule.Id.Contains("SKIP")) return type == PlayType.Skipped;
                if (rule.Id.Contains("FAV")) return type == PlayType.Favorited;
                return type == PlayType.Completed; // Default Global is Play Count

            case AchievementCategory.Misc:
                // Misc contains "Genre Collector" (Fav) and "Night Owl" (Play).
                // We have to inspect the definition or be permissive.

                // If the rule involves favorites (Naming convention check)
                if (rule.Id.Contains("FAV") || rule.Id.Contains("COLLECTOR") || rule.Name.Contains("Collector"))
                {
                    if (type == PlayType.Favorited) return true;
                    // Some collector rules might be play-based, but most are fav based.
                    // If you have "Play 5 Genres" (Explorer), that is a Play event.
                    if (rule.Id.Contains("EXPLORER") || rule.Id.Contains("GENRE_PLAY")) return type == PlayType.Completed;
                }

                // Default Misc to Completed (Time of day, etc)
                if (type == PlayType.Completed) return true;

                // If we are favoriting, only allow specific Misc Fav rules
                if (type == PlayType.Favorited && (rule.Id.Contains("GENRE_COMPLETE") || rule.Id.Contains("GENRE_FAV"))) return true;

                return false;

            default:
                return false;
        }
    }

    private bool IsSongSpecific(AchievementCategory cat) =>
        cat == AchievementCategory.SongCount || cat == AchievementCategory.SongSkip || cat == AchievementCategory.SongFav;

    private void Unlock(UserStats stats, SongModel song, AchievementRule rule)
    {
        // We are already inside a method triggered by a notification.
        // We need to perform a write.
        _realm.Write(() =>
        {
            if (IsSongSpecific((AchievementCategory)rule.Category))
            {
                var containsRule = stats.UnlockedGlobalAchievements.FirstOrDefault
                (x => x.Id == rule.Id);
                if (containsRule is  null)
                {


                }
            }
            else
            {
                var containsRule = stats.UnlockedGlobalAchievements.FirstOrDefault
                (x=>x.Id == rule.Id);
                if(containsRule is not null)
                {
                    // Already unlocked, do nothing
                }
                else
                {
                    stats.UnlockedGlobalAchievements.Add(rule);
                }
            }
        });

        _ruleSubject ??= new BehaviorSubject<(AchievementRule ruleUnlocked, SongModelView? concernedSong)>((default,null));
        _ruleSubject.OnNext((rule, song.ToSongModelView()));

        System.Diagnostics.Debug.WriteLine($"🏆 UNLOCKED: {rule.Name} - {rule.Description}");
        // TODO: Fire Toast Event
    }
    public IObservable<(AchievementRule ruleUnlocked, SongModelView? concernedSong)> UnlockedAchievement => _ruleSubject.AsObservable();
    private BehaviorSubject<(AchievementRule ruleUnlocked, SongModelView? concernedSong)> _ruleSubject = new BehaviorSubject<(AchievementRule,SongModelView?)>((default,null));

    public void Dispose()
    {
        // Stop listening
        _playEventToken?.Dispose();
        _playEventToken = null;

        // Close Realm connection
        if (!_realm.IsClosed)
        {
            _realm.Dispose();
        }
    }



    private int GetTodayPlayCountForSong(SongModel m)
    {
        var today = new DateTimeOffset(DateTimeOffset.UtcNow.Date, TimeSpan.Zero);

        return m.PlayHistory
               .Count(e => e.WasPlayCompleted && e.DatePlayed >= today);
    }

    private IQueryable<DimmerPlayEvent> GetTodayEvents(Realm r)
    {
        var today = new DateTimeOffset(DateTimeOffset.UtcNow.Date, TimeSpan.Zero);
        return r.All<DimmerPlayEvent>().Where(e => e.DatePlayed >= today);
    }

    private List<DimmerPlayEvent> GetLast7DaysEvents(Realm r)
    {
        var date = DateTimeOffset.UtcNow.AddDays(-7);
        return r.All<DimmerPlayEvent>().Where(e => e.DatePlayed >= date).ToList();
    }

    private List<DimmerPlayEvent> GetRecentCompletedEvents(Realm r, int count)
    {
        return r.All<DimmerPlayEvent>()
                .Where(e => e.WasPlayCompleted)
                .OrderByDescending(e => e.DatePlayed)
                .AsEnumerable()
                .Take(count)
                .ToList();
    }

    private bool CheckSongHistoryStreak(SongModel m, int count)
    {
        // Optimization: access the relation list directly
        if (m.PlayHistory.Count < count) return false;

        // Get last N events from this specific song's history
        // Note: PlayHistory is an IList, so we do LINQ in memory (fast for single song)
        var events = m.PlayHistory.OrderByDescending(e => e.DatePlayed).Take(count).ToList();

        return events.Count == count && events.All(e => e.WasPlayCompleted);
    }


    private bool CheckDailyStreakGeneric(Realm r, SongModel m, Func<DimmerPlayEvent, SongModel, bool> matchCondition, int days = 3)
    {
        DateTimeOffset endDate = DateTimeOffset.UtcNow.Date.AddDays(1); // Tomorrow midnight
        DateTimeOffset startDate = DateTimeOffset.UtcNow.Date.AddDays(-(days));

        // 1. Get all candidates in the date range for this song/artist context
        // If checking a specific song, use m.PlayHistory (Much Faster)
        // If checking artist, we must use Global Query (Slower, but necessary)

        IEnumerable<DimmerPlayEvent> candidates;

        // Heuristic: If the condition checks SongId, use the Backlink
        if (m.PlayHistory.Count > 0 && matchCondition(m.PlayHistory.First(), m))
        {
            candidates = m.PlayHistory
                          .Where(e => e.DatePlayed >= startDate && e.DatePlayed < endDate)
                          .OrderByDescending(e => e.DatePlayed);
        }
        else
        {
            // Fallback for Artist/Genre streaks
            candidates = r.All<DimmerPlayEvent>()
                          .Where(e => e.DatePlayed >= startDate && e.DatePlayed < endDate)
                          .ToList() // Force execution
                          .Where(e => matchCondition(e, m))
                          .OrderByDescending(e => e.DatePlayed);
        }

        // 2. Verify we have coverage for every day required
        var datesPlayed = candidates
            .Select(x => x.DatePlayed.Date)
            .Distinct()
            .ToList();

        // We need 'days' consecutive days ending today or yesterday
        int streak = 0;
        var checkDate = DateTimeOffset.UtcNow.Date;

        // Check if we played today to start the streak, otherwise start check from yesterday
        if (!datesPlayed.Contains(checkDate))
        {
            checkDate = checkDate.AddDays(-1);
        }

        for (int i = 0; i < days; i++)
        {
            if (datesPlayed.Contains(checkDate))
            {
                streak++;
                checkDate = checkDate.AddDays(-1);
            }
            else
            {
                break;
            }
        }

        return streak >= days;
    }
    // Checks if all songs in an album meet a condition (e.g., all Fav, all Played)
    private bool CheckAlbumCondition(Realm r, string albumName, Func<List<SongModel>, bool> condition)
    {
        if (string.IsNullOrEmpty(albumName)) return false;
        var songs = r.All<SongModel>().Where(x => x.AlbumName == albumName).ToList();
        if (songs.Count < 3) return false; // Ignore Singles
        return condition(songs);
    }

    private int CountAlbumsWithCondition(Realm r, Func<List<SongModel>, bool> condition)
    {
        // Expensive query warning - run rarely
        var allAlbums = r.All<SongModel>().ToList().GroupBy(x => x.AlbumName);
        return allAlbums.Count(grp => condition(grp.ToList()));
    }

    
    public List<AchievementRule> GetAchievementsByIds(IEnumerable<AchievementRule> earnedIds)
    {
        if (earnedIds is null || !earnedIds.Any())
            return new List<AchievementRule>();

        // Use a HashSet for O(1) lookup performance
        var idSet = new HashSet<AchievementRule>(earnedIds);

        // Filter the master list of rules to find the ones the user/song has earned
        return [.. _rules.Where(rule => idSet.Contains(rule))];
    }
}