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
        _playEvents = _realm.All<DimmerPlayEvent>();

        _playEventToken = _playEvents.SubscribeForNotifications((sender, changes) =>
        {
            if (changes == null || changes.InsertedIndices.Length == 0) return;

            foreach (var index in changes.InsertedIndices)
            {
                if (index < 0 || index >= sender.Count) continue;

                // Grab the event safely
                var newEvent = sender[index];
                ProcessNewEvent(newEvent);
            }
        });
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
            // Optimization: Skip if already earned
            if (IsSongSpecific((AchievementCategory)rule.Category))
            {
                if (songEarnedSet.Contains(rule)) continue;
            }
            else
            {
                if (globalEarnedSet.Contains(rule)) continue;
            }

            bool passed = false;

            // --- DISPATCHER ---
            try
            {
                switch (rule.Category)
                {
                    case (int)AchievementCategory.SongCount:
                        passed = (type == PlayType.Completed) && song.PlayCount >= rule.Threshold;
                        break;
                    case (int)AchievementCategory.SongSkip:
                        passed = (type == PlayType.Skipped) && song.SkipCount >= rule.Threshold;
                        break;
                    case (int)AchievementCategory.SongFav:
                        passed = (type == PlayType.Favorited) && song.IsFavorite;
                        break;
                    case (int)AchievementCategory.GlobalCount:

                        if (rule.Id.Contains("SKIP")) passed = stats.TotalSkips >= rule.Threshold;
                        else if (rule.Id.Contains("FAV")) passed = stats.TotalFavorites >= rule.Threshold;
                        else passed = stats.TotalPlays >= rule.Threshold;

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
                if (IsSongSpecific((AchievementCategory)rule.Category)) songEarnedSet.Add(rule);
                else globalEarnedSet.Add(rule);
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
    // =======================================================================
    // 3. THE DEFINITIONS (THE "SURPRISE ME" IMPLEMENTATION)
    // =======================================================================
    private List<AchievementRule> BuildAllRules()
    {
        var rules = new List<AchievementRule>();

        AchievementTier CalculateTier(int threshold)
        {
            if (threshold <= 5) return AchievementTier.Bronze;
            if (threshold <= 25) return AchievementTier.Silver;
            if (threshold <= 99) return AchievementTier.Gold;
            return AchievementTier.Platinum;
        }

        // ==============================================================================
        // 1️⃣ SINGLE SONG MILESTONES
        // ==============================================================================
        #region Single Song

        var songPlays = new Dictionary<string, int> { { "SONG_FIRST_PLAY", 1 }, { "SONG_PLAY_5", 5 }, { "SONG_PLAY_10", 10 }, { "SONG_PLAY_25", 25 }, { "SONG_PLAY_50", 50 }, { "SONG_PLAY_100", 100 } };
        foreach (var kvp in songPlays) rules.Add(new AchievementRule { Id = kvp.Key, Name = "Song Milestone", Description = $"Play this song {kvp.Value} times", Category = (int)AchievementCategory.SongCount, Threshold = kvp.Value, Tier = (int)CalculateTier(kvp.Value) });

        var songSkips = new Dictionary<string, int> { { "SONG_SKIP_5", 5 }, { "SONG_SKIP_10", 10 }, { "SONG_SKIP_20", 20 } };
        foreach (var kvp in songSkips) rules.Add(new AchievementRule { Id = kvp.Key, Name = "Skipper", Description = $"Skip this song {kvp.Value} times", Category = (int)AchievementCategory.SongSkip, Threshold = kvp.Value, Tier = (int)CalculateTier(kvp.Value) });

        rules.Add(new AchievementRule { Id = "SONG_REPLAY_2", Name = "Replay", Description = "Replay a song immediately 2 times", Category = (int)AchievementCategory.Streak, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => CheckSongHistoryStreak(m, 2) });
        rules.Add(new AchievementRule { Id = "SONG_REPLAY_4", Name = "Replay Addict", Description = "Replay a song immediately 4 times", Category = (int)AchievementCategory.Streak, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => CheckSongHistoryStreak(m, 4) });
        rules.Add(new AchievementRule { Id = "SONG_REPLAY_7", Name = "Replay Maniac", Description = "Replay a song immediately 7 times", Category = (int)AchievementCategory.Streak, Tier = (int)AchievementTier.Gold, CustomCheck = (r, s, m) => CheckSongHistoryStreak(m, 7) });

        rules.Add(new AchievementRule { Id = "SONG_FAV_1", Name = "First Love", Description = "Favorite your first song", Category = (int)AchievementCategory.SongFav, Threshold = 1, Tier = (int)AchievementTier.Bronze });

        rules.Add(new AchievementRule { Id = "SONG_FAV_5", Name = "Collector", Description = "Favorite 5 total songs", Category = (int)AchievementCategory.GlobalCount, Threshold = 5, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => s.TotalFavorites >= 5 });
        rules.Add(new AchievementRule { Id = "SONG_FAV_10", Name = "Super Collector", Description = "Favorite 10 total songs", Category = (int)AchievementCategory.GlobalCount, Threshold = 10, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => s.TotalFavorites >= 10 });
        rules.Add(new AchievementRule { Id = "SONG_FAV_25", Name = "Mega Collector", Description = "Favorite 25 total songs", Category = (int)AchievementCategory.GlobalCount, Threshold = 25, Tier = (int)AchievementTier.Gold, CustomCheck = (r, s, m) => s.TotalFavorites >= 25 });

        rules.Add(new AchievementRule { Id = "SONG_DURATION_1H", Name = "Hour of Power", Description = "Listen to 1 hour of this song’s repeats", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Gold, CustomCheck = (r, s, m) => (m.PlayCount * m.DurationInSeconds) >= 3600 });
        rules.Add(new AchievementRule { Id = "SONG_REPEAT_DAY_10", Name = "Daily Repeat", Description = "Listen to same song 10 times in a day", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => GetTodayPlayCountForSong(m) >= 10 });

        rules.Add(new AchievementRule { Id = "SONG_PLAY_MORNING", Name = "Early Bird", Description = "Play this song before 9 AM", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => DateTime.Now.Hour < 9 });
        rules.Add(new AchievementRule { Id = "SONG_PLAY_EVENING", Name = "Night Owl", Description = "Play this song after 9 PM", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => DateTime.Now.Hour >= 21 });
        #endregion

        // ==============================================================================
        // 2️⃣ ALBUM MILESTONES
        // ==============================================================================
        #region Album
        rules.Add(new AchievementRule { Id = "ALBUM_COMPLETE_FAV", Name = "Album Fan", Description = "Favorite all songs in this album", Category = (int)AchievementCategory.AlbumFav, Tier = (int)AchievementTier.Gold, CustomCheck = (r, s, m) => CheckAlbumCondition(r, m.AlbumName, songs => songs.All(x => x.IsFavorite)) });
        rules.Add(new AchievementRule { Id = "ALBUM_PLAY_ALL", Name = "Full Experience", Description = "Play all songs in this album", Category = (int)AchievementCategory.AlbumCount, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => CheckAlbumCondition(r, m.AlbumName, songs => songs.All(x => x.PlayCount > 0)) });

        rules.Add(new AchievementRule { Id = "ALBUM_PLAY_3", Name = "Triple Threat", Description = "Play all songs in album 3 times", Category = (int)AchievementCategory.AlbumCount, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => CheckAlbumCondition(r, m.AlbumName, songs => songs.All(x => x.PlayCount >= 3)) });
        rules.Add(new AchievementRule { Id = "ALBUM_PLAY_10", Name = "Decade Listener", Description = "Play all songs in album 10 times", Category = (int)AchievementCategory.AlbumCount, Tier = (int)AchievementTier.Gold, CustomCheck = (r, s, m) => CheckAlbumCondition(r, m.AlbumName, songs => songs.All(x => x.PlayCount >= 10)) });
        rules.Add(new AchievementRule { Id = "ALBUM_SKIP_50", Name = "Skipping Madness", Description = "Skip 50% of songs in this album", Category = (int)AchievementCategory.AlbumCount, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => CheckAlbumCondition(r, m.AlbumName, songs => songs.Count(x => x.SkipCount > 0) >= (songs.Count / 2)) });

        rules.Add(new AchievementRule { Id = "ALBUM_FAV_5", Name = "Album Collector", Description = "Favorite 5 full albums", Category = (int)AchievementCategory.GlobalCount, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => CountAlbumsWithCondition(r, songs => songs.All(x => x.IsFavorite)) >= 5 });
        rules.Add(new AchievementRule { Id = "ALBUM_FAV_10", Name = "Super Album Collector", Description = "Favorite 10 full albums", Category = (int)AchievementCategory.GlobalCount, Tier = (int)AchievementTier.Gold, CustomCheck = (r, s, m) => CountAlbumsWithCondition(r, songs => songs.All(x => x.IsFavorite)) >= 10 });

        rules.Add(new AchievementRule { Id = "ALBUM_1H_PLAY", Name = "Hour Album", Description = "Play this album for 1 hour total", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => r.All<SongModel>().Where(x => x.AlbumName == m.AlbumName).ToList().Sum(x => x.PlayCount * x.DurationInSeconds) >= 3600 });

        rules.Add(new AchievementRule { Id = "ALBUM_NIGHT", Name = "Evening Album", Description = "Play all songs in album after 9 PM", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => DateTime.Now.Hour >= 21 && CheckAlbumCondition(r, m.AlbumName, songs => songs.All(x => x.PlayCount > 0)) });
        rules.Add(new AchievementRule { Id = "ALBUM_MORNING", Name = "Morning Album", Description = "Play all songs in album before 9 AM", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => DateTime.Now.Hour < 9 && CheckAlbumCondition(r, m.AlbumName, songs => songs.All(x => x.PlayCount > 0)) });

        rules.Add(new AchievementRule { Id = "ALBUM_REPEAT_3", Name = "Album Maniac", Description = "Replay whole album 3 times", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Gold, CustomCheck = (r, s, m) => CheckAlbumCondition(r, m.AlbumName, songs => songs.Sum(x => x.PlayCount) >= (songs.Count * 3)) });
        rules.Add(new AchievementRule { Id = "ALBUM_GENRE_COMPLETE", Name = "Genre Collector", Description = "Favorite all songs in a genre", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Platinum, CustomCheck = (r, s, m) => { var g = r.All<SongModel>().Where(x => x.GenreName == m.GenreName).ToList(); return g.Count > 5 && g.All(x => x.IsFavorite); } });
        #endregion

        // ==============================================================================
        // 3️⃣ ARTIST MILESTONES
        // ==============================================================================
        #region Artist
        Func<Realm, SongModel, int> getArtistPlays = (r, m) => r.All<SongModel>().Where(x => x.ArtistName == m.ArtistName).ToList().Sum(x => x.PlayCount);

        rules.Add(new AchievementRule { Id = "ARTIST_PLAY_1", Name = "First Artist", Description = "Play any song from this artist", Category = (int)AchievementCategory.ArtistCount, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => getArtistPlays(r, m) >= 1 });
        rules.Add(new AchievementRule { Id = "ARTIST_PLAY_5", Name = "Artist Fan", Description = "Play 5 songs from this artist", Category = (int)AchievementCategory.ArtistCount, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => getArtistPlays(r, m) >= 5 });
        rules.Add(new AchievementRule { Id = "ARTIST_PLAY_10", Name = "Super Fan", Description = "Play 10 songs from this artist", Category = (int)AchievementCategory.ArtistCount, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => getArtistPlays(r, m) >= 10 });
        rules.Add(new AchievementRule { Id = "ARTIST_PLAY_25", Name = "Diehard Fan", Description = "Play 25 songs from this artist", Category = (int)AchievementCategory.ArtistCount, Tier = (int)AchievementTier.Gold, CustomCheck = (r, s, m) => getArtistPlays(r, m) >= 25 });
        rules.Add(new AchievementRule { Id = "ARTIST_PLAY_50", Name = "Mega Fan", Description = "Play 50 songs from this artist", Category = (int)AchievementCategory.ArtistCount, Tier = (int)AchievementTier.Platinum, CustomCheck = (r, s, m) => getArtistPlays(r, m) >= 50 });

        rules.Add(new AchievementRule { Id = "ARTIST_FAV_1", Name = "First Artist Fav", Description = "Favorite one song from this artist", Category = (int)AchievementCategory.ArtistFav, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => r.All<SongModel>().Any(x => x.ArtistName == m.ArtistName && x.IsFavorite) });
        rules.Add(new AchievementRule { Id = "ARTIST_FAV_5", Name = "Artist Collector", Description = "Favorite 5 songs from this artist", Category = (int)AchievementCategory.ArtistFav, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => r.All<SongModel>().Count(x => x.ArtistName == m.ArtistName && x.IsFavorite) >= 5 });
        rules.Add(new AchievementRule { Id = "ARTIST_FAV_ALL", Name = "Artist Loyalist", Description = "Favorite all songs from this artist", Category = (int)AchievementCategory.ArtistFav, Tier = (int)AchievementTier.Platinum, CustomCheck = (r, s, m) => { var all = r.All<SongModel>().Where(x => x.ArtistName == m.ArtistName).ToList(); return all.Count > 2 && all.All(x => x.IsFavorite); } });

        rules.Add(new AchievementRule { Id = "ARTIST_SKIP_10", Name = "Skipping Artist", Description = "Skip 10 songs from this artist", Category = (int)AchievementCategory.ArtistCount, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => r.All<SongModel>().Where(x => x.ArtistName == m.ArtistName).ToList().Sum(x => x.SkipCount) >= 10 });
        rules.Add(new AchievementRule { Id = "ARTIST_SKIP_50", Name = "Skipping Maniac", Description = "Skip 50 songs from this artist", Category = (int)AchievementCategory.ArtistCount, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => r.All<SongModel>().Where(x => x.ArtistName == m.ArtistName).ToList().Sum(x => x.SkipCount) >= 50 });

        rules.Add(new AchievementRule { Id = "ARTIST_PLAY_DAY_20", Name = "Daily Devotee", Description = "Play 20 songs from artist in one day", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Gold, CustomCheck = (r, s, m) => GetTodayEvents(r).Count(e => e.ArtistName == m.ArtistName) >= 20 });
        rules.Add(new AchievementRule { Id = "ARTIST_PLAY_REPEAT_3", Name = "Replay Artist", Description = "Replay 3 songs in a row from same artist", Category = (int)AchievementCategory.Streak, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => { var hist = GetRecentCompletedEvents(r, 3); return hist.Count == 3 && hist.All(x => x.ArtistName == m.ArtistName); } });
        rules.Add(new AchievementRule { Id = "ARTIST_PLAY_MORNING", Name = "Morning Devotee", Description = "Play songs from artist before 9 AM", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => DateTime.Now.Hour < 9 && getArtistPlays(r, m) > 5 });
        #endregion

        // ==============================================================================
        // 4️⃣ DAILY / SYSTEM
        // ==============================================================================
        #region Daily System
        rules.Add(new AchievementRule { Id = "DAY_PLAY_5", Name = "Quick Morning", Description = "Play 5 songs before 9 AM", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => DateTime.Now.Hour < 9 && GetTodayEvents(r).Count() >= 5 });
        rules.Add(new AchievementRule { Id = "DAY_PLAY_5_EVENING", Name = "Night Chill", Description = "Play 5 songs after 9 PM", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => DateTime.Now.Hour >= 21 && GetTodayEvents(r).AsEnumerable().Count(e => e.DatePlayed.Hour >= 21) >= 5 });

        var dailyCounts = new Dictionary<string, int> { { "DAY_PLAY_10", 10 }, { "DAY_PLAY_25", 25 }, { "DAY_PLAY_50", 50 }, { "DAY_PLAY_100", 100 } };
        foreach (var kvp in dailyCounts) rules.Add(new AchievementRule { Id = kvp.Key, Name = "Daily Driver", Description = $"Play {kvp.Value} songs in a day", Category = (int)AchievementCategory.GlobalCount, Threshold = kvp.Value, Tier = (int)CalculateTier(kvp.Value), CustomCheck = (r, s, m) => GetTodayEvents(r).Count(e => e.WasPlayCompleted) >= kvp.Value });

        rules.Add(new AchievementRule { Id = "DAY_REPEAT_5", Name = "Repeat Day", Description = "Repeat same song 5 times in a day", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => GetTodayPlayCountForSong(m) >= 5 });
        rules.Add(new AchievementRule { Id = "DAY_REPEAT_10", Name = "Repeat Maniac", Description = "Repeat same song 10 times in a day", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => GetTodayPlayCountForSong(m) >= 10 });

        rules.Add(new AchievementRule { Id = "WEEK_PLAY_100", Name = "Weekly Addict", Description = "Play 100 songs in a week", Category = (int)AchievementCategory.GlobalCount, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => GetLast7DaysEvents(r).Count >= 100 });
        rules.Add(new AchievementRule { Id = "WEEK_PLAY_250", Name = "Weekly Maniac", Description = "Play 250 songs in a week", Category = (int)AchievementCategory.GlobalCount, Tier = (int)AchievementTier.Gold, CustomCheck = (r, s, m) => GetLast7DaysEvents(r).Count >= 250 });
        rules.Add(new AchievementRule { Id = "WEEK_PLAY_500", Name = "Weekly Monster", Description = "Play 500 songs in a week", Category = (int)AchievementCategory.GlobalCount, Tier = (int)AchievementTier.Platinum, CustomCheck = (r, s, m) => GetLast7DaysEvents(r).Count >= 500 });

        rules.Add(new AchievementRule { Id = "SKIPPED_DAY_10", Name = "Skipper", Description = "Skip 10 songs in a day", Category = (int)AchievementCategory.GlobalCount, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => GetTodayEvents(r).Count(e => !e.WasPlayCompleted) >= 10 });
        rules.Add(new AchievementRule { Id = "TIME_MORNING", Name = "Early Bird", Description = "Play before 6 AM", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => DateTime.Now.Hour < 6 });
        rules.Add(new AchievementRule { Id = "TIME_NIGHT", Name = "Night Owl", Description = "Play after midnight", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => DateTime.Now.Hour == 0 });
        #endregion

        // ==============================================================================
        // 5️⃣ GENRE / MISC
        // ==============================================================================
        #region Genre Misc
        Func<Realm, SongModel, int> getGenrePlays = (r, m) => r.All<SongModel>().Where(x => x.GenreName == m.GenreName).ToList().Sum(x => x.PlayCount);

        rules.Add(new AchievementRule { Id = "GENRE_PLAY_1", Name = "First Genre", Description = "Play a song from this genre", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => getGenrePlays(r, m) >= 1 });
        rules.Add(new AchievementRule { Id = "GENRE_PLAY_5", Name = "Genre Fan", Description = "Play 5 songs from this genre", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => getGenrePlays(r, m) >= 5 });
        rules.Add(new AchievementRule { Id = "GENRE_PLAY_10", Name = "Genre Collector", Description = "Play 10 songs from this genre", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => getGenrePlays(r, m) >= 10 });

        rules.Add(new AchievementRule { Id = "GENRE_FAV_1", Name = "Genre Fav", Description = "Favorite a song in genre", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => r.All<SongModel>().Any(x => x.GenreName == m.GenreName && x.IsFavorite) });
        rules.Add(new AchievementRule { Id = "GENRE_FAV_5", Name = "Genre Collector", Description = "Favorite 5 songs in genre", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => r.All<SongModel>().Count(x => x.GenreName == m.GenreName && x.IsFavorite) >= 5 });
        rules.Add(new AchievementRule { Id = "GENRE_COMPLETE", Name = "Genre Loyalist", Description = "Favorite all songs in genre", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Platinum, CustomCheck = (r, s, m) => { var g = r.All<SongModel>().Where(x => x.GenreName == m.GenreName).ToList(); return g.Count > 5 && g.All(x => x.IsFavorite); } });

        rules.Add(new AchievementRule { Id = "VARIETY_5_GENRES", Name = "Explorer", Description = "Play songs from 5 different genres in a day", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => GetTodayEvents(r).ToList().Select(e => e.SongName).Distinct().Count() >= 5 });
        rules.Add(new AchievementRule { Id = "VARIETY_10_GENRES", Name = "Super Explorer", Description = "Play songs from 10 genres in a week", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Gold, CustomCheck = (r, s, m) => s.TotalPlays > 50 });
        #endregion

        // ==============================================================================
        // 6️⃣ TIME OF DAY / STREAKS
        // ==============================================================================
        #region Streaks & Time
        //rules.Add(new AchievementRule { Id = "MORNING_3_GENRES", Name = "Morning Explorer", Description = "Play songs from 3 genres before 9 AM", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => DateTime.Now.Hour < 9 && GetTodayEvents(r).Take(5).Select(x => x.SongId).Distinct().Count() >= 3 });
        rules.Add(new AchievementRule { Id = "NIGHT_REPEAT_3", Name = "Midnight Loop", Description = "Replay same song 3 times after midnight", Category = (int)AchievementCategory.Streak, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => DateTime.Now.Hour == 0 && CheckSongHistoryStreak(m, 3) });
        rules.Add(new AchievementRule { Id = "WEEKDAY_STREAK_5", Name = "Workday Tunes", Description = "Play 5 songs every weekday for a week", Category = (int)AchievementCategory.Streak, Tier = (int)AchievementTier.Gold, CustomCheck = (r, s, m) => s.DailyPlayStreak >= 5 && DateTime.Now.DayOfWeek != DayOfWeek.Saturday && DateTime.Now.DayOfWeek != DayOfWeek.Sunday });

        rules.Add(new AchievementRule { Id = "STREAK_SAME_SONG_3DAYS", Name = "Persistent", Description = "Play same song for 3 consecutive days", Category = (int)AchievementCategory.Streak, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => CheckDailyStreakGeneric(r, m, (e, model) => e.SongId == model.Id) });
        rules.Add(new AchievementRule { Id = "STREAK_SAME_ARTIST_3DAYS", Name = "Loyal Fan", Description = "Play same artist for 3 consecutive days", Category = (int)AchievementCategory.Streak, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => CheckDailyStreakGeneric(r, m, (e, model) => e.ArtistName == model.ArtistName) });

        rules.Add(new AchievementRule { Id = "EARLY_MORNING_ALBUM", Name = "Dawn Patrol", Description = "Play all songs of an album before 7 AM", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Gold, CustomCheck = (r, s, m) => DateTime.Now.Hour < 7 && CheckAlbumCondition(r, m.AlbumName, songs => songs.All(x => x.PlayCount > 0)) });
        rules.Add(new AchievementRule { Id = "SUNSET_ALBUM", Name = "Golden Hour", Description = "Complete an album between 6–7 PM", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => DateTime.Now.Hour == 18 && CheckAlbumCondition(r, m.AlbumName, songs => songs.All(x => x.PlayCount > 0)) });
        #endregion

        // ==============================================================================
        // 7️⃣ ALBUM / ARTIST COMBOS
        // ==============================================================================
        #region Combos
        rules.Add(new AchievementRule { Id = "CROSS_ALBUM_ARTIST", Name = "Album Hopper", Description = "Play song from album A, then B of same artist", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => { var last = GetRecentCompletedEvents(r, 2); if (last.Count < 2) return false; return last[0].ArtistName == last[1].ArtistName && last[0].AlbumName != last[1].AlbumName; } });
        rules.Add(new AchievementRule { Id = "ARTIST_GENRE_MIX", Name = "Genre Hopper", Description = "Play 3 songs from artist across genres", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Gold, CustomCheck = (r, s, m) => { var last = GetRecentCompletedEvents(r, 3); if (last.Count < 3) return false; return last.All(x => x.ArtistName == m.ArtistName) && last.Select(x => x.SongId).Distinct().Count() == 3; } });
        rules.Add(new AchievementRule { Id = "FAVORITE_FULL_ALBUM", Name = "Album Lover", Description = "Favorite all songs of an album", Category = (int)AchievementCategory.AlbumFav, Tier = (int)AchievementTier.Gold, CustomCheck = (r, s, m) => CheckAlbumCondition(r, m.AlbumName, songs => songs.All(x => x.IsFavorite)) });
        #endregion

        // ==============================================================================
        // 8️⃣ EXTREME / CHALLENGE
        // ==============================================================================
        //#region Extreme
        rules.Add(new AchievementRule { Id = "SONG_7_DAYS_REPEAT", Name = "Hardcore Loop", Description = "Play same song every day for 7 days", Category = (int)AchievementCategory.Streak, Tier = (int)AchievementTier.Platinum, CustomCheck = (r, s, m) => CheckDailyStreakGeneric(r, m, (e, model) => e.SongId == model.Id, 7) });
        //rules.Add(new AchievementRule { Id = "TOP_10_ALL", Name = "Global Listener", Description = "Play a top 10 most played song", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Gold, CustomCheck = (r, s, m) => { var last10 = GetRecentCompletedEvents(r, 10); if (last10.Count < 10) return false; var top10Ids = r.All<SongModel>().OrderByDescending(x => x.PlayCount).Take(10).ToList().Select(x => x.Id).ToList(); return top10Ids.Contains(m.Id); } });
        rules.Add(new AchievementRule { Id = "NIGHT_OWL_CENTURY", Name = "Late Night Century", Description = "Play 100 songs after 9 PM", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Platinum, CustomCheck = (r, s, m) => r.All<DimmerPlayEvent>().AsEnumerable().Count(e => e.DatePlayed.Hour >= 21) >= 100 });
        

        // ==============================================================================
        // 9️⃣ COMMON & OVERLOOKED
        // ==============================================================================
        #region Common
        rules.Add(new AchievementRule { Id = "FAV_FIRST_5", Name = "Early Collector", Description = "Favorite your first 5 songs", Category = (int)AchievementCategory.GlobalCount, Threshold = 5, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => s.TotalFavorites >= 5 });
        rules.Add(new AchievementRule { Id = "PLAY_FIRST_ALBUM", Name = "Album Beginner", Description = "Play your first full album", Category = (int)AchievementCategory.AlbumCount, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => CheckAlbumCondition(r, m.AlbumName, songs => songs.All(x => x.PlayCount > 0)) });
        rules.Add(new AchievementRule { Id = "SKIP_ONCE", Name = "First Skip", Description = "Skip a song for the first time", Category = (int)AchievementCategory.GlobalCount, Threshold = 1, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => s.TotalSkips >= 1 });
        rules.Add(new AchievementRule { Id = "REPLAY_ONCE", Name = "First Replay", Description = "Replay a song for the first time", Category = (int)AchievementCategory.Streak, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => CheckSongHistoryStreak(m, 2) });

        rules.Add(new AchievementRule { Id = "MORNING_3", Name = "Morning Jams", Description = "Play 3 songs before 9 AM", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => DateTime.Now.Hour < 9 && GetTodayEvents(r).Count() >= 3 });
        rules.Add(new AchievementRule { Id = "EVENING_3", Name = "Evening Chill", Description = "Play 3 songs after 9 PM", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => DateTime.Now.Hour >= 21 && GetTodayEvents(r).Count() >= 3 });
        
        rules.Add(new AchievementRule { 
            Id = "FAVORITE_SONG_DAY", 
            Name = "Day Collector",
            Description = $"Favorite a song in a single day {Environment.NewLine}", 
            Category = (int)AchievementCategory.Misc, 
            Tier = (int)AchievementTier.Bronze, 
            
            CustomCheck = (r, s, m) =>
            {
                return m.IsFavorite;
            }
        });
        rules.Add(new AchievementRule { Id = "WEEK_PLAY_10", Name = "Weekly Starter", Description = "Play 10 songs in a week", Category = (int)AchievementCategory.GlobalCount, Threshold = 10, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => GetLast7DaysEvents(r).Count >= 10 });
        rules.Add(new AchievementRule { Id = "ALL_SONGS_ALBUM", Name = "Album Finish", Description = "Play all songs of an album", Category = (int)AchievementCategory.AlbumCount, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => CheckAlbumCondition(r, m.AlbumName, songs => songs.All(x => x.PlayCount > 0)) });

        rules.Add(new AchievementRule { Id = "SKIP_5", Name = "Skipper", Description = "Skip 5 songs total", Category = (int)AchievementCategory.GlobalCount, Threshold = 5, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => s.TotalSkips >= 5 });
        rules.Add(new AchievementRule { Id = "FAVORITE_10", Name = "Collector", Description = "Favorite 10 songs total", Category = (int)AchievementCategory.GlobalCount, Threshold = 10, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => s.TotalFavorites >= 10 });
        rules.Add(new AchievementRule { Id = "PLAY_50", Name = "Milestone 50", Description = "Play 50 songs total", Category = (int)AchievementCategory.GlobalCount, Threshold = 50, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => s.TotalPlays >= 50 });
        rules.Add(new AchievementRule { Id = "PLAY_100", Name = "Milestone 100", Description = "Play 100 songs total", Category = (int)AchievementCategory.GlobalCount, Threshold = 100, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => s.TotalPlays >= 100 });
        #endregion

        // ==============================================================================
        // 🔟 AUDIOPHILE
        // ==============================================================================
        #region Audiophile
        rules.Add(new AchievementRule { Id = "LONG_STREAK_7D", Name = "Devoted Listener", Description = "Listen 7 days in a row", Category = (int)AchievementCategory.Streak, Tier = (int)AchievementTier.Bronze, CustomCheck = (r, s, m) => s.DailyPlayStreak >= 7 });
        rules.Add(new AchievementRule { Id = "LONG_STREAK_14D", Name = "Hardcore Streak", Description = "Listen 14 days in a row", Category = (int)AchievementCategory.Streak, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => s.DailyPlayStreak >= 14 });
        rules.Add(new AchievementRule { Id = "STREAK_30D", Name = "Month Streak", Description = "Listen 30 days in a row", Category = (int)AchievementCategory.Streak, Tier = (int)AchievementTier.Gold, CustomCheck = (r, s, m) => s.DailyPlayStreak >= 30 });
        rules.Add(new AchievementRule { Id = "STREAK_60D", Name = "Bi-Month Streak", Description = "Listen 60 days in a row", Category = (int)AchievementCategory.Streak, Tier = (int)AchievementTier.Platinum, CustomCheck = (r, s, m) => s.DailyPlayStreak >= 60 });
        rules.Add(new AchievementRule { Id = "STREAK_90D", Name = "Quarter Streak", Description = "Listen 90 days in a row", Category = (int)AchievementCategory.Streak, Tier = (int)AchievementTier.Platinum, CustomCheck = (r, s, m) => s.DailyPlayStreak >= 90 });

        rules.Add(new AchievementRule { Id = "LONG_PLAY_SESSION_10H", Name = "Listening Marathon", Description = "Listen 10 hours in a single session", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Platinum, CustomCheck = (r, s, m) => GetTodayEvents(r).Count() * 3.5 > 600 });
        rules.Add(new AchievementRule { Id = "EARLY_BIRD_10", Name = "Sunrise Session", Description = "Play 10 songs before 7 AM", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => DateTime.Now.Hour < 7 && GetTodayEvents(r).Count() >= 10 });
        rules.Add(new AchievementRule { Id = "NIGHT_OWL_10", Name = "Late Night Marathon", Description = "Play 10 songs after midnight", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => DateTime.Now.Hour == 0 && GetTodayEvents(r).AsEnumerable().Count(e => e.DatePlayed.Hour == 0) >= 10 });
        rules.Add(new AchievementRule { Id = "FAST_FAV_5", Name = "Speed Collector", Description = "Favorite 5 songs quickly", Category = (int)AchievementCategory.Misc, Tier = (int)AchievementTier.Silver, CustomCheck = (r, s, m) => s.TotalFavorites >= 5 });
        rules.Add(new AchievementRule { Id = "TOP_100", Name = "Music Collector", Description = "Play top 100 songs in library", Category = (int)AchievementCategory.GlobalCount, Threshold = 100, Tier = (int)AchievementTier.Gold, CustomCheck = (r, s, m) => s.TotalPlays >= 100 });
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