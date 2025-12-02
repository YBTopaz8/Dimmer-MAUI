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
    public EventHandler AchievementUnlocked; 



        public AchievementService(IRealmFactory realmFactory)
        {
            _realmFactory = realmFactory;
            _realm = _realmFactory.GetRealmInstance();
            _rules = BuildAllRules(); // The big list of 40
            EnsureStatsExist();
        InitializeReactiveListeners();
    }

        private void EnsureStatsExist()
        {
            var realm = _realmFactory.GetRealmInstance();
            if (realm.Find<UserStats>("Global") == null)
            {
                realm.Write(() => realm.Add(new UserStats()));
            }
        }

    private void InitializeReactiveListeners()
    {
        // We watch the entire table of Play Events.
        _playEvents = _realm.All<DimmerPlayEvent>();

        _playEventToken = _playEvents.SubscribeForNotifications((sender, changes) =>
        {
            if (changes == null) return; // First load (optional: ignore or process history)

            // We only care about NEW events (Insertions)
            // If the user deletes history, we don't revoke achievements.
            if (changes.InsertedIndices.Length > 0)
            {
                // Process every new event found
                foreach (var index in changes.InsertedIndices)
                {
                    // SAFETY CHECK: Ensure index is valid
                    if (index < 0 || index >= sender.Count) continue;

                    var newEvent = sender[index];

                    // We run the logic based on this specific event
                    ProcessNewEvent(newEvent);
                }
            }
        });
    }
    private void ProcessNewEvent(DimmerPlayEvent playEvent)
    {

        var songId = playEvent.SongId;
        var playType = (PlayType)playEvent.PlayType; // Assuming you store int in DB

        // We need the Song and Stats to update them
        var song = _realm.Find<SongModel>(songId);
        var stats = _realm.Find<UserStats>("Global");

        if (song == null || stats == null) return;

        // A. UPDATE STATS (Reactive Side Effect)
        // We write the side effects of this event (Counts, Streaks)
        _realm.Write(() =>
        {
            UpdateStatsLogic(stats, playType);
        });

        // B. CHECK ACHIEVEMENTS
        // Now that Stats are updated, we check if any rules were met
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

    private void CheckRules(UserStats stats, SongModel song, PlayType type)
    {
        foreach (var rule in _rules)
        {
            // Optimization: Skip if already earned
            if (IsSongSpecific(rule.Category))
            {
                if (song.EarnedAchievementIds.Contains(rule.Id)) continue;
            }
            else
            {
                if (stats.UnlockedGlobalAchievementIds.Contains(rule.Id)) continue;
            }

            bool passed = false;

            // --- DISPATCHER ---
            try
            {
                switch (rule.Category)
                {
                    case AchievementCategory.SongCount:
                        passed = (type == PlayType.Completed) && song.PlayCount >= rule.Threshold;
                        break;
                    case AchievementCategory.SongSkip:
                        passed = (type == PlayType.Skipped) && song.SkipCount >= rule.Threshold;
                        break;
                    case AchievementCategory.SongFav:
                        passed = (type == PlayType.Favorited) && song.IsFavorite;
                        break;
                    case AchievementCategory.GlobalCount:
                        passed = stats.TotalPlays >= rule.Threshold; // Simplified check
                        // For specific thresholds (Favs/Skips), you might need specific logic in CustomCheck
                        if (rule.CustomCheck != null) passed = rule.CustomCheck(_realm, stats, song);
                        break;
                    default:
                        // Streak, Misc, Album, Artist
                        if (rule.CustomCheck != null) passed = rule.CustomCheck(_realm, stats, song);
                        break;
                }
            }
            catch (Exception ex)
            {
                // Logging
                System.Diagnostics.Debug.WriteLine($"Error checking rule {rule.Id}: {ex.Message}");
            }

            if (passed)
            {
                Unlock(stats, song, rule);
            }
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
            if (IsSongSpecific(rule.Category))
            {
                song.EarnedAchievementIds.Add(rule.Id);
            }
            else
            {
                stats.UnlockedGlobalAchievementIds.Add(rule.Id);
            }
        });

        _ruleSubject ??= new(rule);

        System.Diagnostics.Debug.WriteLine($"🏆 UNLOCKED: {rule.Name} - {rule.Description}");
        // TODO: Fire Toast Event
    }
    public IObservable<AchievementRule> UnlockedAchievement => _ruleSubject.AsObservable();
    private BehaviorSubject<AchievementRule> _ruleSubject = new BehaviorSubject<AchievementRule>(null);
    // =======================================================================
    // 3. THE DEFINITIONS (THE "SURPRISE ME" IMPLEMENTATION)
    // =======================================================================
    private List<AchievementRule> BuildAllRules()
    {
        var rules = new List<AchievementRule>();

        // ==============================================================================
        // 1️⃣ SINGLE SONG MILESTONES (Linked to Song)
        // ==============================================================================
        #region Single Song
        // --- Play Counts ---
        var songPlays = new Dictionary<string, int> {
        { "SONG_FIRST_PLAY", 1 }, { "SONG_PLAY_5", 5 }, { "SONG_PLAY_10", 10 },
        { "SONG_PLAY_25", 25 }, { "SONG_PLAY_50", 50 }, { "SONG_PLAY_100", 100 }
    };
        foreach (var kvp in songPlays) rules.Add(new AchievementRule { Id = kvp.Key, Name = "Song Milestone", Description = $"Play song {kvp.Value} times", Category = AchievementCategory.SongCount, Threshold = kvp.Value });

        // --- Skips ---
        var songSkips = new Dictionary<string, int> {
        { "SONG_SKIP_5", 5 }, { "SONG_SKIP_10", 10 }, { "SONG_SKIP_20", 20 }
    };
        foreach (var kvp in songSkips) rules.Add(new AchievementRule { Id = kvp.Key, Name = "Skipper", Description = $"Skip song {kvp.Value} times", Category = AchievementCategory.SongSkip, Threshold = kvp.Value });

        // --- Replays (Immediate) ---
        // Requires history check: Last N events must be this song AND completed
        rules.Add(new AchievementRule
        {
            Id = "SONG_REPLAY_2",
            Name = "Replay",
            Category = AchievementCategory.Streak,
            CustomCheck = (r, s, m) => CheckSongHistoryStreak(m, 2)
        });
        rules.Add(new AchievementRule
        {
            Id = "SONG_REPLAY_4",
            Name = "Replay Addict",
            Category = AchievementCategory.Streak,
            CustomCheck = (r, s, m) => CheckSongHistoryStreak(m, 7)
        });
        rules.Add(new AchievementRule
        {
            Id = "SONG_REPLAY_7",
            Name = "Replay Maniac",
            Category = AchievementCategory.Streak,
            CustomCheck = (r, s, m) => CheckSongHistoryStreak( m, 7)
        });

        // --- Favorites ---
        // Note: SONG_FAV_1 is "First Love" (Global or Song?). Assuming Song specific based on category.
        rules.Add(new AchievementRule { Id = "SONG_FAV_1", Name = "First Love", Category = AchievementCategory.SongFav, Threshold = 1 });
        // The others (5, 10, 25) are Global Counts of total favorites
        rules.Add(new AchievementRule { Id = "SONG_FAV_5", Name = "Collector", Category = AchievementCategory.GlobalCount, CustomCheck = (r, s, m) => s.TotalFavorites >= 5 });
        rules.Add(new AchievementRule { Id = "SONG_FAV_10", Name = "Super Collector", Category = AchievementCategory.GlobalCount, CustomCheck = (r, s, m) => s.TotalFavorites >= 10 });
        rules.Add(new AchievementRule { Id = "SONG_FAV_25", Name = "Mega Collector", Category = AchievementCategory.GlobalCount, CustomCheck = (r, s, m) => s.TotalFavorites >= 25 });

        // --- Misc Song ---
        rules.Add(new AchievementRule
        {
            Id = "SONG_DURATION_1H",
            Name = "Hour of Power",
            Category = AchievementCategory.Misc,
            CustomCheck = (r, s, m) => (m.PlayCount * m.DurationInSeconds) >= 3600
        });

        //rules.Add(new AchievementRule
        //{
        //    Id = "SONG_REPEAT_DAY_10",
        //    Name = "Daily Repeat",
        //    Category = AchievementCategory.Misc,
        //    CustomCheck = (r, s, m) => GetTodayPlayCountForSong(r, m.Id.ToString()) >= 10
        //});

        rules.Add(new AchievementRule { Id = "SONG_PLAY_MORNING", Name = "Early Bird", Category = AchievementCategory.Misc, CustomCheck = (r, s, m) => DateTime.Now.Hour < 9 });
        rules.Add(new AchievementRule { Id = "SONG_PLAY_EVENING", Name = "Night Owl", Category = AchievementCategory.Misc, CustomCheck = (r, s, m) => DateTime.Now.Hour >= 21 });
        #endregion

        // ==============================================================================
        // 2️⃣ ALBUM MILESTONES
        // ==============================================================================
        #region Album
        rules.Add(new AchievementRule
        {
            Id = "ALBUM_COMPLETE_FAV",
            Name = "Album Fan",
            Category = AchievementCategory.AlbumFav,
            CustomCheck = (r, s, m) => CheckAlbumCondition(r, m.AlbumName, songs => songs.All(x => x.IsFavorite))
        });

        rules.Add(new AchievementRule
        {
            Id = "ALBUM_PLAY_ALL",
            Name = "Full Experience",
            Category = AchievementCategory.AlbumCount,
            CustomCheck = (r, s, m) => CheckAlbumCondition(r, m.AlbumName, songs => songs.All(x => x.PlayCount > 0))
        });

        

        // Counts
        rules.Add(new AchievementRule
        {
            Id = "ALBUM_PLAY_3",
            Name = "Triple Threat",
            Category = AchievementCategory.AlbumCount,
            CustomCheck = (r, s, m) => CheckAlbumCondition(r, m.AlbumName, songs => songs.All(x => x.PlayCount >= 3))
        });
        rules.Add(new AchievementRule
        {
            Id = "ALBUM_PLAY_10",
            Name = "Decade Listener",
            Category = AchievementCategory.AlbumCount,
            CustomCheck = (r, s, m) => CheckAlbumCondition(r, m.AlbumName, songs => songs.All(x => x.PlayCount >= 10))
        });

        rules.Add(new AchievementRule
        {
            Id = "ALBUM_SKIP_50",
            Name = "Skipping Madness",
            Category = AchievementCategory.AlbumCount,
            CustomCheck = (r, s, m) => CheckAlbumCondition(r, m.AlbumName, songs => songs.Count(x => x.SkipCount > 0) >= (songs.Count / 2))
        });

        // Aggregates
        rules.Add(new AchievementRule
        {
            Id = "ALBUM_FAV_5",
            Name = "Album Collector",
            Category = AchievementCategory.GlobalCount,
            CustomCheck = (r, s, m) => CountAlbumsWithCondition(r, songs => songs.All(x => x.IsFavorite)) >= 5
        });
        rules.Add(new AchievementRule
        {
            Id = "ALBUM_FAV_10",
            Name = "Super Album Collector",
            Category = AchievementCategory.GlobalCount,
            CustomCheck = (r, s, m) => CountAlbumsWithCondition(r, songs => songs.All(x => x.IsFavorite)) >= 10
        });

        rules.Add(new AchievementRule
        {
            Id = "ALBUM_1H_PLAY",
            Name = "Hour Album",
            Category = AchievementCategory.Misc,
            CustomCheck = (r, s, m) => r.All<SongModel>().Where(x => x.AlbumName == m.AlbumName).ToList().Sum(x => x.PlayCount * x.DurationInSeconds) >= 3600
        });

        // Time of day specific to Album
        rules.Add(new AchievementRule
        {
            Id = "ALBUM_NIGHT",
            Name = "Evening Album",
            Category = AchievementCategory.Misc,
            CustomCheck = (r, s, m) => DateTime.Now.Hour >= 21 && CheckAlbumCondition(r, m.AlbumName, songs => songs.All(x => x.PlayCount > 0))
        });
        rules.Add(new AchievementRule
        {
            Id = "ALBUM_MORNING",
            Name = "Morning Album",
            Category = AchievementCategory.Misc,
            CustomCheck = (r, s, m) => DateTime.Now.Hour < 9 && CheckAlbumCondition(r, m.AlbumName, songs => songs.All(x => x.PlayCount > 0))
        });

        rules.Add(new AchievementRule
        {
            Id = "ALBUM_REPEAT_3",
            Name = "Album Maniac",
            Category = AchievementCategory.Misc,
            CustomCheck = (r, s, m) => CheckAlbumCondition(r, m.AlbumName, songs => songs.Sum(x => x.PlayCount) >= (songs.Count * 3))
        });

        rules.Add(new AchievementRule
        {
            Id = "ALBUM_GENRE_COMPLETE",
            Name = "Genre Collector",
            Category = AchievementCategory.Misc,
            CustomCheck = (r, s, m) => {
                var genreSongs = r.All<SongModel>().Where(x => x.GenreName == m.GenreName).ToList();
                return genreSongs.Count > 5 && genreSongs.All(x => x.IsFavorite);
            }
        });
        #endregion

        // ==============================================================================
        // 3️⃣ ARTIST MILESTONES
        // ==============================================================================
        #region Artist
        // Helper to get total artist plays
        Func<Realm, SongModel, int> getArtistPlays = (r, m) => r.All<SongModel>().Where(x => x.ArtistName == m.ArtistName).ToList().Sum(x => x.PlayCount);

        rules.Add(new AchievementRule { Id = "ARTIST_PLAY_1", Name = "First Artist", Category = AchievementCategory.ArtistCount, CustomCheck = (r, s, m) => getArtistPlays(r, m) >= 1 });
        rules.Add(new AchievementRule { Id = "ARTIST_PLAY_5", Name = "Artist Fan", Category = AchievementCategory.ArtistCount, CustomCheck = (r, s, m) => getArtistPlays(r, m) >= 5 });
        rules.Add(new AchievementRule { Id = "ARTIST_PLAY_10", Name = "Super Fan", Category = AchievementCategory.ArtistCount, CustomCheck = (r, s, m) => getArtistPlays(r, m) >= 10 });
        rules.Add(new AchievementRule { Id = "ARTIST_PLAY_25", Name = "Diehard Fan", Category = AchievementCategory.ArtistCount, CustomCheck = (r, s, m) => getArtistPlays(r, m) >= 25 });
        rules.Add(new AchievementRule { Id = "ARTIST_PLAY_50", Name = "Mega Fan", Category = AchievementCategory.ArtistCount, CustomCheck = (r, s, m) => getArtistPlays(r, m) >= 50 });

        rules.Add(new AchievementRule
        {
            Id = "ARTIST_FAV_1",
            Name = "First Artist Fav",
            Category = AchievementCategory.ArtistFav,
            CustomCheck = (r, s, m) => r.All<SongModel>().Any(x => x.ArtistName == m.ArtistName && x.IsFavorite)
        });
        rules.Add(new AchievementRule
        {
            Id = "ARTIST_FAV_5",
            Name = "Artist Collector",
            Category = AchievementCategory.ArtistFav,
            CustomCheck = (r, s, m) => r.All<SongModel>().Count(x => x.ArtistName == m.ArtistName && x.IsFavorite) >= 5
        });
        rules.Add(new AchievementRule
        {
            Id = "ARTIST_FAV_ALL",
            Name = "Artist Loyalist",
            Category = AchievementCategory.ArtistFav,
            CustomCheck = (r, s, m) => {
                var all = r.All<SongModel>().Where(x => x.ArtistName == m.ArtistName).ToList();
                return all.Count > 2 && all.All(x => x.IsFavorite);
            }
        });

        rules.Add(new AchievementRule
        {
            Id = "ARTIST_SKIP_10",
            Name = "Skipping Artist",
            Category = AchievementCategory.ArtistCount,
            CustomCheck = (r, s, m) => r.All<SongModel>().Where(x => x.ArtistName == m.ArtistName).ToList().ToList().Sum(x => x.SkipCount) >= 10
        });
        rules.Add(new AchievementRule
        {
            Id = "ARTIST_SKIP_50",
            Name = "Skipping Maniac",
            Category = AchievementCategory.ArtistCount,
            CustomCheck = (r, s, m) => r.All<SongModel>().Where(x => x.ArtistName == m.ArtistName).ToList().Sum(x => x.SkipCount) >= 50
        });

        // Time/Streak Artist
        rules.Add(new AchievementRule
        {
            Id = "ARTIST_PLAY_DAY_20",
            Name = "Daily Devotee",
            Category = AchievementCategory.Misc,
            CustomCheck = (r, s, m) => GetTodayEvents(r).Count(e => e.ArtistName == m.ArtistName) >= 20
        });
        rules.Add(new AchievementRule
        {
            Id = "ARTIST_PLAY_REPEAT_3",
            Name = "Replay Artist",
            Category = AchievementCategory.Streak,
            CustomCheck = (r, s, m) => {
                var hist = GetRecentCompletedEvents(r, 3);
                return hist.Count == 3 && hist.All(x => x.ArtistName == m.ArtistName);
            }
        });
        rules.Add(new AchievementRule
        {
            Id = "ARTIST_PLAY_MORNING",
            Name = "Morning Devotee",
            Category = AchievementCategory.Misc,
            CustomCheck = (r, s, m) => DateTime.Now.Hour < 9 && getArtistPlays(r, m) > 5
        }); // Implied count > 1
        #endregion

        // ==============================================================================
        // 4️⃣ DAILY / SYSTEM
        // ==============================================================================
        #region Daily System
        rules.Add(new AchievementRule
        {
            Id = "DAY_PLAY_5",
            Name = "Quick Morning",
            Category = AchievementCategory.Misc,
            CustomCheck = (r, s, m) => DateTime.Now.Hour < 9 && GetTodayEvents(r).Count() >= 5
        });
        //rules.Add(new AchievementRule
        //{
        //    Id = "DAY_PLAY_5_EVENING",
        //    Name = "Night Chill",
        //    Category = AchievementCategory.Misc,
        //    CustomCheck = (r, s, m) => DateTime.Now.Hour >= 21 && GetTodayEvents(r).Count(e => e.DatePlayed.Hour >= 21) >= 5
        //});

        var dailyCounts = new Dictionary<string, int> { { "DAY_PLAY_10", 10 }, { "DAY_PLAY_25", 25 }, { "DAY_PLAY_50", 50 }, { "DAY_PLAY_100", 100 } };
        foreach (var kvp in dailyCounts) rules.Add(new AchievementRule
        {
            Id = kvp.Key,
            Category = AchievementCategory.GlobalCount,
            CustomCheck = (r, s, m) => GetTodayEvents(r).Count(e => e.WasPlayCompleted) >= kvp.Value
        });

        rules.Add(new AchievementRule
        {
            Id = "DAY_REPEAT_5",
            Name = "Repeat Day",
            Category = AchievementCategory.Misc,
            CustomCheck = (r, s, m) => GetTodayPlayCountForSong(m) >= 5
        });
        rules.Add(new AchievementRule
        {
            Id = "DAY_REPEAT_10",
            Name = "Repeat Maniac",
            Category = AchievementCategory.Misc,
            CustomCheck = (r, s, m) => GetTodayPlayCountForSong(m) >= 10
        });

        // Weekly - Approximated by last 7 days
        rules.Add(new AchievementRule
        {
            Id = "WEEK_PLAY_100",
            Name = "Weekly Addict",
            Category = AchievementCategory.GlobalCount,
            CustomCheck = (r, s, m) => GetLast7DaysEvents(r).Count >= 100
        });
        rules.Add(new AchievementRule
        {
            Id = "WEEK_PLAY_250",
            Name = "Weekly Maniac",
            Category = AchievementCategory.GlobalCount,
            CustomCheck = (r, s, m) => GetLast7DaysEvents(r).Count >= 250
        });
        rules.Add(new AchievementRule
        {
            Id = "WEEK_PLAY_500",
            Name = "Weekly Monster",
            Category = AchievementCategory.GlobalCount,
            CustomCheck = (r, s, m) => GetLast7DaysEvents(r).Count >= 500
        });

        rules.Add(new AchievementRule
        {
            Id = "SKIPPED_DAY_10",
            Name = "Skipper",
            Category = AchievementCategory.GlobalCount,
            CustomCheck = (r, s, m) => GetTodayEvents(r).Count(e => !e.WasPlayCompleted) >= 10
        });

        rules.Add(new AchievementRule { Id = "TIME_MORNING", Name = "Early Bird", Category = AchievementCategory.Misc, CustomCheck = (r, s, m) => DateTime.Now.Hour < 6 });
        rules.Add(new AchievementRule { Id = "TIME_NIGHT", Name = "Night Owl", Category = AchievementCategory.Misc, CustomCheck = (r, s, m) => DateTime.Now.Hour == 0 });
        #endregion

        // ==============================================================================
        // 5️⃣ GENRE / MISC
        // ==============================================================================
        #region Genre Misc
        Func<Realm, SongModel, int> getGenrePlays = (r, m) => r.All<SongModel>().Where(x => x.GenreName == m.GenreName).ToList().Sum(x => x.PlayCount);

        rules.Add(new AchievementRule { Id = "GENRE_PLAY_1", Name = "First Genre", Category = AchievementCategory.Misc, CustomCheck = (r, s, m) => getGenrePlays(r, m) >= 1 });
        rules.Add(new AchievementRule { Id = "GENRE_PLAY_5", Name = "Genre Fan", Category = AchievementCategory.Misc, CustomCheck = (r, s, m) => getGenrePlays(r, m) >= 5 });
        rules.Add(new AchievementRule { Id = "GENRE_PLAY_10", Name = "Genre Collector", Category = AchievementCategory.Misc, CustomCheck = (r, s, m) => getGenrePlays(r, m) >= 10 });

        rules.Add(new AchievementRule
        {
            Id = "GENRE_FAV_1",
            Name = "Genre Fav",
            Category = AchievementCategory.Misc,
            CustomCheck = (r, s, m) => r.All<SongModel>().Any(x => x.GenreName == m.GenreName && x.IsFavorite)
        });
        rules.Add(new AchievementRule
        {
            Id = "GENRE_FAV_5",
            Name = "Genre Collector",
            Category = AchievementCategory.Misc,
            CustomCheck = (r, s, m) => r.All<SongModel>().Count(x => x.GenreName == m.GenreName && x.IsFavorite) >= 5
        });
        rules.Add(new AchievementRule
        {
            Id = "GENRE_COMPLETE",
            Name = "Genre Loyalist",
            Category = AchievementCategory.Misc,
            CustomCheck = (r, s, m) => { var g = r.All<SongModel>().Where(x => x.GenreName == m.GenreName).ToList(); return g.Count > 5 && g.All(x => x.IsFavorite); }
        });

        // Variety
        //rules.Add(new AchievementRule
        //{
        //    Id = "VARIETY_5_GENRES",
        //    Name = "Explorer",
        //    Category = AchievementCategory.Misc,
        //    CustomCheck = (r, s, m) => GetTodayEvents(r).Select(e => e.SongName).Distinct().Count() >= 5
        //}); // Approximation: Name->Song->Genre lookup is expensive. Assuming high variety play = variety.
            // Better Logic for Variety:
        rules.Add(new AchievementRule
        {
            Id = "VARIETY_10_GENRES",
            Name = "Super Explorer",
            Category = AchievementCategory.Misc,
            CustomCheck = (r, s, m) => {
                // Look at last 7 days song IDs, get distinct Genres. 
                // Optimization: Just check if UserStats.GlobalGenreCount > 10 (If you track that). Otherwise:
                // Skip heavy query for now or use simplified count
                return s.TotalPlays > 50;
            }
        });
        #endregion

        // ==============================================================================
        // 6️⃣ TIME OF DAY / STREAKS
        // ==============================================================================
        #region Streaks & Time
        //rules.Add(new AchievementRule
        //{
        //    Id = "MORNING_3_GENRES",
        //    Name = "Morning Explorer",
        //    Category = AchievementCategory.Misc,
        //    CustomCheck = (r, s, m) => DateTime.Now.Hour < 9 && GetTodayEvents(r).Take(5).Select(x => x.SongId).Distinct().Count() >= 3
        //}); // Approx

        rules.Add(new AchievementRule
        {
            Id = "NIGHT_REPEAT_3",
            Name = "Midnight Loop",
            Category = AchievementCategory.Streak,
            CustomCheck = (r, s, m) => DateTime.Now.Hour == 0 && CheckSongHistoryStreak( m, 3)
        });

        rules.Add(new AchievementRule
        {
            Id = "WEEKDAY_STREAK_5",
            Name = "Workday Tunes",
            Category = AchievementCategory.Streak,
            CustomCheck = (r, s, m) => s.DailyPlayStreak >= 5 && DateTime.Now.DayOfWeek != DayOfWeek.Saturday && DateTime.Now.DayOfWeek != DayOfWeek.Sunday
        });

        rules.Add(new AchievementRule
        {
            Id = "STREAK_SAME_SONG_3DAYS",
            Name = "Persistent",
            Category = AchievementCategory.Streak,
            CustomCheck = (r, s, m) => CheckDailyStreakGeneric(r, m, (e, model) => e.SongId == model.Id)
        });
        rules.Add(new AchievementRule
        {
            Id = "STREAK_SAME_ARTIST_3DAYS",
            Name = "Loyal Fan",
            Category = AchievementCategory.Streak,
            CustomCheck = (r, s, m) => CheckDailyStreakGeneric(r, m, (e, model) => e.ArtistName == model.ArtistName)
        });

        rules.Add(new AchievementRule
        {
            Id = "EARLY_MORNING_ALBUM",
            Name = "Dawn Patrol",
            Category = AchievementCategory.Misc,
            CustomCheck = (r, s, m) => DateTime.Now.Hour < 7 && CheckAlbumCondition(r, m.AlbumName, songs => songs.All(x => x.PlayCount > 0))
        });

        rules.Add(new AchievementRule
        {
            Id = "SUNSET_ALBUM",
            Name = "Golden Hour",
            Category = AchievementCategory.Misc,
            CustomCheck = (r, s, m) => DateTime.Now.Hour == 18 && CheckAlbumCondition(r, m.AlbumName, songs => songs.All(x => x.PlayCount > 0))
        });
        #endregion

        // ==============================================================================
        // 7️⃣ ALBUM / ARTIST COMBOS
        // ==============================================================================
        #region Combos
        rules.Add(new AchievementRule
        {
            Id = "CROSS_ALBUM_ARTIST",
            Name = "Album Hopper",
            Category = AchievementCategory.Misc,
            CustomCheck = (r, s, m) => {
                var last = GetRecentCompletedEvents(r, 2);
                if (last.Count < 2) return false;
                // Same Artist, Diff Album
                return last[0].ArtistName == last[1].ArtistName && last[0].AlbumName != last[1].AlbumName;
            }
        });

        //rules.Add(new AchievementRule
        //{
        //    Id = "ARTIST_GENRE_MIX",
        //    Name = "Genre Hopper",
        //    Category = AchievementCategory.Misc,
        //    CustomCheck = (r, s, m) => {
        //        var last = GetRecentCompletedEvents(r, 3);
        //        if (last.Count < 3) return false;
        //        return last.All(x => x.ArtistName == m.ArtistName) && last.Select(x => x.SongId).Distinct().Count() == 3; // Simplified as we can't easily get Genre from Event without lookup
        //    }
        //});

        rules.Add(new AchievementRule
        {
            Id = "FAVORITE_FULL_ALBUM",
            Name = "Album Lover",
            Category = AchievementCategory.AlbumFav,
            CustomCheck = (r, s, m) => CheckAlbumCondition(r, m.AlbumName, songs => songs.All(x => x.IsFavorite))
        });
        #endregion

        // ==============================================================================
        // 8️⃣ EXTREME / CHALLENGE
        // ==============================================================================
        #region Extreme
        rules.Add(new AchievementRule
        {
            Id = "SONG_7_DAYS_REPEAT",
            Name = "Hardcore Loop",
            Category = AchievementCategory.Streak,
            CustomCheck = (r, s, m) => CheckDailyStreakGeneric(r, m, (e, model) => e.SongId == model.Id, 7)
        });

        rules.Add(new AchievementRule
        {
            Id = "TOP_10_ALL",
            Name = "Global Listener",
            Category = AchievementCategory.Misc,
            CustomCheck = (r, s, m) => {
                var last10 = GetRecentCompletedEvents(r, 10);
                if (last10.Count < 10) return false;
                // Assuming we check if these 10 songs are in the Top 10 most played DB list
                // Optimization: Just check current song is in top 10 and stats.TotalPlays > 1000
                var top10Ids = r.All<SongModel>().OrderByDescending(x => x.PlayCount).Take(10).ToList().Select(x => x.Id).ToList();
                return top10Ids.Contains(m.Id);
            }
        });

        //rules.Add(new AchievementRule
        //{
        //    Id = "NIGHT_OWL_CENTURY",
        //    Name = "Late Night Century",
        //    Category = AchievementCategory.Misc,
        //    CustomCheck = (r, s, m) => r.All<DimmerPlayEvent>().Count(e => e.DatePlayed.Hour >= 21) >= 100
        //});
        #endregion

        // ==============================================================================
        // 9️⃣ COMMON & OVERLOOKED (From 45 list)
        // ==============================================================================
        #region Common
        rules.Add(new AchievementRule { Id = "FAV_FIRST_5", Category = AchievementCategory.GlobalCount, CustomCheck = (r, s, m) => s.TotalFavorites >= 5 });
        rules.Add(new AchievementRule
        {
            Id = "PLAY_FIRST_ALBUM",
            Category = AchievementCategory.AlbumCount,
            CustomCheck = (r, s, m) => CheckAlbumCondition(r, m.AlbumName, songs => songs.All(x => x.PlayCount > 0))
        });

        rules.Add(new AchievementRule { Id = "SKIP_ONCE", Category = AchievementCategory.GlobalCount, CustomCheck = (r, s, m) => s.TotalSkips >= 1 });
        rules.Add(new AchievementRule { Id = "REPLAY_ONCE", Category = AchievementCategory.Streak, CustomCheck = (r, s, m) => CheckSongHistoryStreak( m, 2) });

        rules.Add(new AchievementRule { Id = "MORNING_3", Category = AchievementCategory.Misc, CustomCheck = (r, s, m) => DateTime.Now.Hour < 9 && GetTodayEvents(r).Count() >= 3 });
        rules.Add(new AchievementRule { Id = "EVENING_3", Category = AchievementCategory.Misc, CustomCheck = (r, s, m) => DateTime.Now.Hour >= 21 && GetTodayEvents(r).Count() >= 3 });

        rules.Add(new AchievementRule
        {
            Id = "FAVORITE_SONG_DAY",
            Category = AchievementCategory.Misc,
            CustomCheck = (r, s, m) => m.IsFavorite
        }); // Triggered on fav

        rules.Add(new AchievementRule { Id = "WEEK_PLAY_10", Category = AchievementCategory.GlobalCount, CustomCheck = (r, s, m) => GetLast7DaysEvents(r).Count >= 10 });

        rules.Add(new AchievementRule
        {
            Id = "ALL_SONGS_ALBUM",
            Category = AchievementCategory.AlbumCount,
            CustomCheck = (r, s, m) => CheckAlbumCondition(r, m.AlbumName, songs => songs.All(x => x.PlayCount > 0))
        });

        rules.Add(new AchievementRule { Id = "SKIP_5", Category = AchievementCategory.GlobalCount, CustomCheck = (r, s, m) => s.TotalSkips >= 5 });
        rules.Add(new AchievementRule { Id = "FAVORITE_10", Category = AchievementCategory.GlobalCount, CustomCheck = (r, s, m) => s.TotalFavorites >= 10 });

        rules.Add(new AchievementRule { Id = "PLAY_50", Category = AchievementCategory.GlobalCount, CustomCheck = (r, s, m) => s.TotalPlays >= 50 });
        rules.Add(new AchievementRule { Id = "PLAY_100", Category = AchievementCategory.GlobalCount, CustomCheck = (r, s, m) => s.TotalPlays >= 100 });
        #endregion

        // ==============================================================================
        // 🔟 AUDIOPHILE (From 45 list)
        // ==============================================================================
        #region Audiophile
        rules.Add(new AchievementRule { Id = "LONG_STREAK_7D", Category = AchievementCategory.Streak, CustomCheck = (r, s, m) => s.DailyPlayStreak >= 7 });
        rules.Add(new AchievementRule { Id = "LONG_STREAK_14D", Category = AchievementCategory.Streak, CustomCheck = (r, s, m) => s.DailyPlayStreak >= 14 });
        rules.Add(new AchievementRule { Id = "STREAK_30D", Category = AchievementCategory.Streak, CustomCheck = (r, s, m) => s.DailyPlayStreak >= 30 });
        rules.Add(new AchievementRule { Id = "STREAK_60D", Category = AchievementCategory.Streak, CustomCheck = (r, s, m) => s.DailyPlayStreak >= 60 });
        rules.Add(new AchievementRule { Id = "STREAK_90D", Category = AchievementCategory.Streak, CustomCheck = (r, s, m) => s.DailyPlayStreak >= 90 });

        rules.Add(new AchievementRule
        {
            Id = "LONG_PLAY_SESSION_10H",
            Category = AchievementCategory.Misc,
            CustomCheck = (r, s, m) => GetTodayEvents(r).Count() * 3.5 > 600
        }); // Approx

        rules.Add(new AchievementRule { Id = "EARLY_BIRD_10", Category = AchievementCategory.Misc, CustomCheck = (r, s, m) => DateTime.Now.Hour < 7 && GetTodayEvents(r).Count() >= 10 });
        rules.Add(new AchievementRule { Id = "NIGHT_OWL_10", Category = AchievementCategory.Misc, CustomCheck = (r, s, m) => DateTime.Now.Hour == 0 && GetTodayEvents(r).Count(e => e.DatePlayed.Hour == 0) >= 10 });

        rules.Add(new AchievementRule
        {
            Id = "FAST_FAV_5",
            Category = AchievementCategory.Misc,
            CustomCheck = (r, s, m) => {
                // Check if 5 favorites happened in last hour
                // Note: DimmerPlayEvent tracks PLAY. To track FAV timestamps, we'd need a separate log or check Favorites Count change rate. 
                // Fallback: Total Favorites check
                return s.TotalFavorites >= 5;
            }
        });

        rules.Add(new AchievementRule { Id = "TOP_100", Category = AchievementCategory.GlobalCount, CustomCheck = (r, s, m) => s.TotalPlays >= 100 }); // Placeholder
        #endregion

        return rules;
    }



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
        var today = DateTimeOffset.UtcNow.Date;
        return m.PlayHistory.Where(e => e.DatePlayed >= today && e.WasPlayCompleted).Count();
    }

    private IQueryable<DimmerPlayEvent> GetTodayEvents(Realm r)
    {
        var today = DateTimeOffset.UtcNow.Date;
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
                .ToList()
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


    private bool CheckDailySongStreak(SongModel m, int days)
    {
        var now = DateTimeOffset.UtcNow.Date;
        for (int i = 0; i < days; i++)
        {
            var targetDate = now.AddDays(-i);
            var nextDate = targetDate.AddDays(1);

            // Fast: Look inside the specific song's history list
            bool hasMatch = m.PlayHistory.Any(e => e.DatePlayed >= targetDate && e.DatePlayed < nextDate && e.WasPlayCompleted);

            if (!hasMatch) return false;
        }
        return true;
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

    // Generic Streak Checker (Days in a row)
    private bool CheckDailyStreakGeneric(Realm r, SongModel m, Func<DimmerPlayEvent, SongModel, bool> matchCondition, int days = 3)
    {
        var now = DateTimeOffset.UtcNow.Date;
        bool streakAlive = true;

        for (int i = 0; i < days; i++)
        {
            var targetDate = now.AddDays(-i);
            var nextDate = targetDate.AddDays(1);

            // Check if ANY event on this specific day matches the condition
            bool hasMatch = r.All<DimmerPlayEvent>()
                .Any(e => e.DatePlayed >= targetDate && e.DatePlayed < nextDate && matchCondition(e, m));

            if (!hasMatch)
            {
                streakAlive = false;
                break;
            }
        }
        return streakAlive;
    }
}